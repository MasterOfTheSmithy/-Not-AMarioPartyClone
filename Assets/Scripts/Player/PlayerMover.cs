using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    public int playerId;
    public int health = 10;
    public int CurrentEnergy { get; private set; } = 10;

    private HashSet<PlayerMover> battledThisStep = new();
    public PlayerHUD playerHUD;
    public PartnerDialogueUI partnerDialogueUI;
    public BoardTile currentTile;
    private BoardTile lastTile;
    public PartnerPositionChoiceUI partnerChoiceUI;
    public TileChoiceUI tileChoiceUI;

    public float moveSpeed = 3f;
    private Animator animator;

    private bool waitingForChoice = false;
    private BoardTile nextChosenTile;
    private BoardTile selectedNextTile = null;

    public Vector3Int LogicalFacing { get; private set; } = Vector3Int.forward;
    private bool hasAssignedPartnerAfterStart = false;
    private bool isPartnerSpawning = false;
    private bool isMoving = false;

    public PartnerInstance frontPartner;
    public PartnerInstance backPartner;
    public PartnerData testPartnerData;
    public string PlayerName;

    public System.Action OnMovementComplete;
    public delegate void TurnOrderRollHandler(int result);
    public event TurnOrderRollHandler OnTurnOrderRoll;
    public event Action<int> OnStep;
    public event Action<int> OnEnergyChanged;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        UpdateStatsUI();

        if (partnerChoiceUI == null)
        {
            partnerChoiceUI = FindObjectOfType<PartnerPositionChoiceUI>();
            if (partnerChoiceUI == null)
                Debug.LogError("❌ PartnerPositionChoiceUI not found in scene! Make sure it's in the scene and active.");
        }
    }

    private void Start()
    {
        playerHUD?.Initialize(this);

        if (partnerChoiceUI == null)
            partnerChoiceUI = FindObjectOfType<PartnerPositionChoiceUI>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            AssignPartner(testPartnerData, true);

        if (TurnManager.Instance?.isRollingTurnOrder == true &&
            TurnManager.Instance.CurrentRollingPlayer == this &&
            Input.GetKeyDown(KeyCode.Space))
        {
            int roll = UnityEngine.Random.Range(1, 7);
            Debug.Log($"Player {playerId} rolled {roll} for turn order.");
            OnTurnOrderRoll?.Invoke(roll);
        }
    }

    public void ModifyEnergy(int amount)
    {
        CurrentEnergy = Mathf.Max(CurrentEnergy + amount, 0);
        OnEnergyChanged?.Invoke(CurrentEnergy);
        UpdateStatsUI();
    }

    public void ModifyHealth(int amount)
    {
        health = Mathf.Max(health + amount, 0);
        UpdateStatsUI();
    }

    private void UpdateStatsUI()
    {
        if (playerHUD != null)
        {
            playerHUD.UpdateEnergy(CurrentEnergy);
            playerHUD.UpdateHealth(health);
        }
    }

    public void MovePlayer(int steps, Action onComplete = null)
    {
        if (!isMoving)
            StartCoroutine(Move(steps, onComplete));
    }

    private IEnumerator Move(int steps, Action onComplete)
    {
        isMoving = true;
        animator?.SetBool("isMoving", true);

        while (steps > 0)
        {
            if (currentTile?.nextTiles == null || currentTile.nextTiles.Count == 0)
                break;

            List<BoardTile> validNextTiles = currentTile.nextTiles.Where(t => t != lastTile).ToList();
            if (validNextTiles.Count == 0)
                validNextTiles = new List<BoardTile>(currentTile.nextTiles);

            if (validNextTiles.Count == 1)
            {
                selectedNextTile = validNextTiles[0];
            }
            else
            {
                yield return RequestTileChoice(validNextTiles);
                selectedNextTile = nextChosenTile;
            }

            lastTile = currentTile;
            LogicalFacing = Vector3Int.RoundToInt((selectedNextTile.transform.position - currentTile.transform.position).normalized);

            yield return MoveToTile(selectedNextTile);

            currentTile = selectedNextTile;

            if (currentTile.isStartTile && !hasAssignedPartnerAfterStart && !isPartnerSpawning && !partnerChoiceUI.IsOpen)
            {
                hasAssignedPartnerAfterStart = true;
                isPartnerSpawning = true;
                yield return StartCoroutine(SpawnPartnerRoutine(currentTile.partnerPool));
                isPartnerSpawning = false;
            }
            else if (!currentTile.isStartTile)
            {
                hasAssignedPartnerAfterStart = false;
            }

            steps--;
            OnStep?.Invoke(steps);
            yield return new WaitForSeconds(0.2f);
        }

        isMoving = false;
        animator?.SetBool("isMoving", false);
        onComplete?.Invoke();
        OnMovementComplete?.Invoke();
    }

    private IEnumerator RequestTileChoice(List<BoardTile> options)
    {
        waitingForChoice = true;
        tileChoiceUI.ShowChoicesDirectional(this, options, Vector3.forward);
        yield return new WaitUntil(() => !waitingForChoice);
        yield return nextChosenTile;
    }

    private IEnumerator MoveToTile(BoardTile destination)
    {
        Vector3 start = transform.position;
        Vector3 end = destination.transform.position + Vector3.up * 1.1f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t));

            Collider[] hits = Physics.OverlapSphere(transform.position, 0.6f);
            foreach (var hit in hits)
            {
                PlayerMover other = hit.GetComponent<PlayerMover>();
                if (other != null && other != this && !battledThisStep.Contains(other) && !other.battledThisStep.Contains(this))
                {
                    battledThisStep.Add(other);
                    other.battledThisStep.Add(this);

                    bool isFrontAttack = Vector3.Dot(transform.forward, (other.transform.position - transform.position).normalized) > 0;
                    bool resolved = false;

                    TurnManager.Instance.TriggerCombat(this, other, isFrontAttack, () => resolved = true);
                    yield return new WaitUntil(() => resolved);
                    yield break; // resume movement after combat
                }
            }

            Vector3 offset = (Vector3)LogicalFacing;
            frontPartner?.SetTargetPosition(transform.position + offset * 1.2f + Vector3.up * 0.4f);
            backPartner?.SetTargetPosition(transform.position - offset * 1.2f + Vector3.up * 0.4f);

            yield return null;
        }
    }

    public void ReceiveTileChoice(BoardTile chosenTile)
    {
        nextChosenTile = chosenTile;
        waitingForChoice = false;
    }

    public void AssignPartner(PartnerData partnerData, bool isFront)
    {
        float zOffset = isFront ? 1.5f : -1.5f;
        GameObject partnerObj = new GameObject($"Partner_{partnerData.partnerName}");
        PartnerInstance instance = partnerObj.AddComponent<PartnerInstance>();
        instance.Initialize(partnerData, this, new Vector3(0, 0, zOffset), partnerDialogueUI);

        if (isFront)
        {
            if (frontPartner != null) Destroy(frontPartner.gameObject);
            frontPartner = instance;
        }
        else
        {
            if (backPartner != null) Destroy(backPartner.gameObject);
            backPartner = instance;
        }
    }

    public IEnumerator SpawnPartnerRoutine(PartnerData[] pool)
    {
        if (pool == null || pool.Length == 0) yield break;

        partnerChoiceUI ??= FindObjectOfType<PartnerPositionChoiceUI>();
        if (partnerChoiceUI == null || partnerChoiceUI.IsOpen) yield break;

        PartnerData[] choices = pool.OrderBy(_ => UnityEngine.Random.value).Take(Mathf.Min(pool.Length, 3)).ToArray();
        bool? isFrontSlot = null;
        PartnerData selectedPartner = null;

        partnerChoiceUI.rootPanel?.SetActive(true);
        partnerChoiceUI.Show(choices, (partner, isFront) =>
        {
            selectedPartner = partner;
            isFrontSlot = isFront;
        });

        yield return new WaitUntil(() => isFrontSlot.HasValue && selectedPartner != null);

        AssignPartner(selectedPartner, isFrontSlot.Value);

        yield return new WaitUntil(() => !partnerChoiceUI.IsOpen);
    }

    public IEnumerable<PartnerInstance> GetPartners()
    {
        if (frontPartner != null) yield return frontPartner;
        if (backPartner != null) yield return backPartner;
    }

    public IEnumerator CheckPartnersEndTurnCoroutine()
    {
        foreach (var partner in GetPartners())
            yield return partner.EndTurnCheckCoroutine();
    }

    public void StartTurn() => battledThisStep.Clear();
    public void DeductPartnerSalaries()
    {
        frontPartner?.TryPaySalary(this);
        backPartner?.TryPaySalary(this);
    }

    public void NotifyPartnerDeath(PartnerInstance partner)
    {
        Debug.Log($"{PlayerName}'s partner {partner.data.partnerName} died!");
        playerHUD?.ShowPartnerDeath(partner);
    }
}
