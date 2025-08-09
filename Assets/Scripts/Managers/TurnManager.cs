using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TurnPhase
{
    RollingTurnOrder,
    StartingTurn,
    WaitingForRoll,
    Moving,
    ResolvingTile,
    Combat,
    EndingTurn
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [SerializeField] private List<PlayerMover> players = new();
    [SerializeField] private RectTransform canvasTransform;
    [SerializeField] private DiceRoller diceRoller;

    private List<PlayerTurnData> turnOrder = new();
    private int currentRollerIndex = 0;
    private int currentTurnIndex = 0;
    public bool isRollingTurnOrder = true;

    public TurnPhase CurrentPhase { get; private set; }
    public event System.Action<TurnPhase> OnPhaseChanged;

    public void SetPhase(TurnPhase newPhase)
    {
        CurrentPhase = newPhase;
        Debug.Log($"[TurnManager] Phase changed to {newPhase}");
        OnPhaseChanged?.Invoke(newPhase);
    }

    public PlayerMover CurrentRollingPlayer =>
        (players != null && currentRollerIndex >= 0 && currentRollerIndex < players.Count)
        ? players[currentRollerIndex]
        : null;

    public PlayerMover CurrentPlayer =>
        (turnOrder != null && turnOrder.Count > 0 && currentTurnIndex >= 0 && currentTurnIndex < turnOrder.Count)
        ? turnOrder[currentTurnIndex].player
        : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        diceRoller ??= FindFirstObjectByType<DiceRoller>();
        SetPhase(TurnPhase.RollingTurnOrder);
        StartCoroutine(PlayerRollsForTurnOrder());
    }

    private IEnumerator PlayerRollsForTurnOrder()
    {
        while (currentRollerIndex < players.Count)
        {
            var player = players[currentRollerIndex];
            bool hasRolled = false;
            int rollResult = 0;

            Debug.Log($"Player {player.playerId}, press your button to roll for turn order!");

            PlayerMover.TurnOrderRollHandler handler = null;
            handler = (int roll) =>
            {
                hasRolled = true;
                rollResult = roll;
                player.OnTurnOrderRoll -= handler;
            };
            player.OnTurnOrderRoll += handler;

            yield return new WaitUntil(() => hasRolled);

            turnOrder.Add(new PlayerTurnData { player = player, roll = rollResult });
            currentRollerIndex++;
        }

        turnOrder = turnOrder.OrderByDescending(x => x.roll).ToList();

        Debug.Log("Final Turn Order:");
        foreach (var item in turnOrder)
            Debug.Log($"Player {item.player.playerId} rolled {item.roll}");

        yield return AnimateHUDsIntoCorners();

        isRollingTurnOrder = false;
        currentTurnIndex = 0;

        StartTurn();
    }

    private IEnumerator AnimateHUDsIntoCorners()
    {
        Vector2[] anchors = new Vector2[]
        {
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(1, 0)
        };

        Vector2[] startOffsets = new Vector2[]
        {
            new Vector2(-200, 0), new Vector2(200, 0),
            new Vector2(-200, 0), new Vector2(200, 0)
        };

        float duration = 1.0f;

        for (int i = 0; i < turnOrder.Count && i < anchors.Length; i++)
        {
            PlayerHUD hud = turnOrder[i].player.playerHUD;
            RectTransform rt = hud.GetComponent<RectTransform>();

            Vector2 targetAnchor = anchors[i];
            rt.anchorMin = targetAnchor;
            rt.anchorMax = targetAnchor;
            rt.pivot = targetAnchor;
            rt.anchoredPosition = startOffsets[i];

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.anchoredPosition = Vector2.Lerp(startOffsets[i], Vector2.zero, t);
                yield return null;
            }

            rt.anchoredPosition = Vector2.zero;
        }
    }

    private void StartTurn()
    {
        if (CurrentPlayer == null)
        {
            Debug.LogWarning("StartTurn() - CurrentPlayer is null.");
            return;
        }

        SetPhase(TurnPhase.StartingTurn);

        var player = CurrentPlayer;
        player.StartTurn();
        diceRoller.BeginTurn();

        CameraFollow camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.target = player.transform;
        }

        foreach (var p in turnOrder)
            p.player.playerHUD.SetHighlight(p.player == player);

        player.DeductPartnerSalaries();
        SetPhase(TurnPhase.WaitingForRoll);
    }

    public IEnumerator ResolveTilePhase()
    {
        SetPhase(TurnPhase.ResolvingTile);

        yield return new WaitForSeconds(0.25f);

        BoardTile tile = CurrentPlayer?.currentTile;

        if (tile != null)
        {
            tile.OnPlayerLand(CurrentPlayer);
        }

        yield return new WaitForSeconds(0.5f);

        SetPhase(TurnPhase.EndingTurn);
        StartCoroutine(NextTurnCoroutine());
    }
    public void TriggerCombat(PlayerMover attacker, PlayerMover defender, bool isFront, System.Action onCombatResolved = null)
    {
        SetPhase(TurnPhase.Combat);
        BattleSystem.ResolveBattle(attacker, defender, isFront);
        StartCoroutine(ResumePostCombat(attacker, onCombatResolved));
    }

    private IEnumerator ResumePostCombat(PlayerMover actor, System.Action onCombatResolved = null)
    {
        yield return new WaitForSeconds(0.4f); // Short delay for VFX, etc.
        onCombatResolved?.Invoke(); // ✅ Resume movement
    }
    private IEnumerator FinishCombatAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);  // optional: wait for animations
        SetPhase(TurnPhase.EndingTurn);
        StartCoroutine(NextTurnCoroutine());
    }
    public IEnumerator NextTurnCoroutine()
    {
        if (CurrentPlayer == null)
        {
            Debug.LogWarning("NextTurnCoroutine - CurrentPlayer is null.");
            yield break;
        }

        yield return CurrentPlayer.CheckPartnersEndTurnCoroutine();

        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        StartTurn();
    }
}
