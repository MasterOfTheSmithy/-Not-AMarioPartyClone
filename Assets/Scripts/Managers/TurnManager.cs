using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }  // Singleton Instance

    public List<PlayerMover> players; // Assign players in inspector or dynamically
    private List<PlayerTurnData> turnOrder;
    private int currentRollerIndex = 0;
    public bool isRollingTurnOrder = true;
    private int currentTurnIndex = 0;
    public RectTransform canvasTransform;

    // Safe CurrentPlayer property with null and bounds checks
    public PlayerMover CurrentPlayer
    {
        get
        {
            if (turnOrder == null || turnOrder.Count == 0)
            {
                Debug.LogError("Turn order is empty!");
                return null;
            }
            if (currentTurnIndex < 0 || currentTurnIndex >= turnOrder.Count)
            {
                Debug.LogError($"currentTurnIndex {currentTurnIndex} out of range!");
                return null;
            }
            return turnOrder[currentTurnIndex].player;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Only one instance allowed
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        turnOrder = new List<PlayerTurnData>();
        StartCoroutine(PlayerRollsForTurnOrder());
    }

    private IEnumerator PlayerRollsForTurnOrder()
    {
        while (currentRollerIndex < players.Count)
        {
            PlayerMover player = players[currentRollerIndex];

            Debug.Log($"Player {player.playerId}, press your button to roll for turn order!");

            bool hasRolled = false;
            int rollResult = 0;

            // Subscribe to the roll event temporarily
            PlayerMover.TurnOrderRollHandler handler = null;
            handler = (int roll) =>
            {
                hasRolled = true;
                rollResult = roll;
                player.OnTurnOrderRoll -= handler; // Unsubscribe immediately
            };
            player.OnTurnOrderRoll += handler;

            yield return new WaitUntil(() => hasRolled);

            turnOrder.Add(new PlayerTurnData { player = player, roll = rollResult });
            currentRollerIndex++;
        }

        // Sort descending by roll
        turnOrder = turnOrder.OrderByDescending(x => x.roll).ToList();

        Debug.Log("Final Turn Order:");
        foreach (var item in turnOrder)
            Debug.Log($"Player {item.player.playerId} rolled {item.roll}");

        yield return AnimateHUDsIntoCorners();

        isRollingTurnOrder = false;
        currentTurnIndex = 0;  // Reset turn index safely

        StartTurn();
    }

    private IEnumerator AnimateHUDsIntoCorners()
    {
        Vector2[] anchors = new Vector2[]
        {
        new Vector2(0, 1), // top-left
        new Vector2(1, 1), // top-right
        new Vector2(0, 0), // bottom-left
        new Vector2(1, 0)  // bottom-right
        };

        float duration = 1.0f;

        // Define starting offsets for each corner to start HUD offscreen / away
        Vector2[] startOffsets = new Vector2[]
        {
        new Vector2(-200, 0),  // off left (for top-left)
        new Vector2(200, 0),   // off right (for top-right)
        new Vector2(-200, 0),  // off left (for bottom-left)
        new Vector2(200, 0)    // off right (for bottom-right)
        };

        for (int i = 0; i < turnOrder.Count; i++)
        {
            PlayerHUD hud = turnOrder[i].player.playerHUD;
            RectTransform rt = hud.GetComponent<RectTransform>();

            Vector2 targetAnchor = anchors[i];

            // Snap anchors and pivot instantly
            rt.anchorMin = targetAnchor;
            rt.anchorMax = targetAnchor;
            rt.pivot = targetAnchor;

            // Set anchoredPosition to start offset (offscreen start)
            rt.anchoredPosition = startOffsets[i];

            Vector2 startPos = startOffsets[i];
            Vector2 endPos = Vector2.zero;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            rt.anchoredPosition = endPos;
        }
    }

    private void AssignPlayerHUDsByTurnOrder()
    {
        Vector2[] anchors = new Vector2[]
        {
            new Vector2(0, 1), // top-left
            new Vector2(1, 1), // top-right
            new Vector2(0, 0), // bottom-left
            new Vector2(1, 0)  // bottom-right
        };

        Vector2[] pivots = new Vector2[]
        {
            new Vector2(0, 1), // top-left
            new Vector2(1, 1), // top-right
            new Vector2(0, 0), // bottom-left
            new Vector2(1, 0)  // bottom-right
        };

        Vector2[] offsets = new Vector2[]
        {
            new Vector2(10, -10),  // top-left
            new Vector2(-10, -10), // top-right
            new Vector2(10, 10),   // bottom-left
            new Vector2(-10, 10)   // bottom-right
        };

        for (int i = 0; i < turnOrder.Count && i < anchors.Length; i++)
        {
            PlayerMover player = turnOrder[i].player;
            PlayerHUD hud = player.playerHUD;

            if (hud != null)
            {
                RectTransform rt = hud.GetComponent<RectTransform>();
                rt.anchorMin = anchors[i];
                rt.anchorMax = anchors[i];
                rt.pivot = pivots[i];
                rt.anchoredPosition = offsets[i];

                hud.SetHighlight(i == currentTurnIndex);
            }
        }
    }

    private void StartTurn()
    {
        if (CurrentPlayer == null)
        {
            Debug.LogWarning("StartTurn called but CurrentPlayer is null, skipping.");
            return;
        }

        PlayerMover currentPlayer = CurrentPlayer;
        Debug.Log($"Player {currentPlayer.playerId}'s turn starts.");
        currentPlayer.StartTurn();
        CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
        if (camFollow != null)
        {
            camFollow.target = currentPlayer.transform;
        }

        foreach (var playerData in turnOrder)
        {
            bool isCurrent = playerData.player == currentPlayer;
            playerData.player.playerHUD.SetHighlight(isCurrent);
        }

        AssignPlayerHUDsByTurnOrder();

        currentPlayer.DeductPartnerSalaries();

        // TODO: Enable current player controls and disable others as needed
    }

    public IEnumerator NextTurnCoroutine()
    {
        if (CurrentPlayer == null)
        {
            Debug.LogWarning("NextTurnCoroutine called but CurrentPlayer is null.");
            yield break;
        }

        yield return CurrentPlayer.CheckPartnersEndTurnCoroutine();

        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;

        StartTurn();

        DiceRoller diceRoller = FindFirstObjectByType<DiceRoller>();
        if (diceRoller != null)
        {
            diceRoller.BeginTurn();
        }
    }
}