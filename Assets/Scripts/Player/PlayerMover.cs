using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    public int playerId;                // Unique player identifier (1 to 4)
    public int health = 10;             // Player health

    public int CurrentEnergy { get; private set; } = 10; // Player's energy
    private HashSet<PlayerMover> battledThisStep = new HashSet<PlayerMover>();
    public PlayerHUD playerHUD;         // Reference to the UI controller for this player
    public PartnerDialogueUI partnerDialogueUI;
    public BoardTile currentTile;
    private BoardTile lastTile;
    public PartnerPositionChoiceUI partnerChoiceUI;
    public float moveSpeed = 3f;
    public delegate void TurnOrderRollHandler(int result);
    public event TurnOrderRollHandler OnTurnOrderRoll;
    private bool isMoving = false;
    private Animator animator;
    private bool isPartnerSpawning = false;
    public TileChoiceUI tileChoiceUI;

    private bool waitingForChoice = false;
    private BoardTile nextChosenTile;
    private BoardTile selectedNextTile = null;

    public event Action<int> OnStep;
    public event Action<int> OnEnergyChanged;

    public PartnerInstance frontPartner;
    public PartnerInstance backPartner;

    public PartnerData testPartnerData;

    public Vector3Int LogicalFacing { get; private set; } = Vector3Int.forward;

    // Flag to prevent reassigning partner multiple times after passing start
    private bool hasAssignedPartnerAfterStart = false;

    public string PlayerName; // Assign in inspector or via script

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        UpdateStatsUI();
    }

    private void Start()
    {
        if (playerHUD != null)
        {
            playerHUD.Initialize(this);
        }
    }

    private void Update()
    {
        // Existing test partner assign code
        if (Input.GetKeyDown(KeyCode.P))
        {
            AssignPartner(testPartnerData, true);
        }

        // Turn order rolling input - only allow rolling during roll phase
        if (TurnManager.Instance != null && TurnManager.Instance.isRollingTurnOrder)
        {
            // For testing: use Space key (or assign per player)
            // You can customize this to different keys per player if needed
            if (Input.GetKeyDown(KeyCode.Space))
            {
                int roll = UnityEngine.Random.Range(1, 7);  // Roll 1-6 dice
                Debug.Log($"Player {playerId} rolled {roll} for turn order.");
                OnTurnOrderRoll?.Invoke(roll);
            }
        }

    }

    public void ModifyEnergy(int amount)
    {
        CurrentEnergy += amount;
        if (CurrentEnergy < 0) CurrentEnergy = 0;

        OnEnergyChanged?.Invoke(CurrentEnergy);
        UpdateStatsUI();

        Debug.Log($"{name} now has {CurrentEnergy} energy.");
    }

    public void ModifyHealth(int amount)
    {
        health += amount;
        if (health < 0) health = 0;

        UpdateStatsUI();

        Debug.Log($"{name} now has {health} health.");
    }

    private void UpdateStatsUI()
    {
        if (playerHUD != null)
        {
            playerHUD.UpdateEnergy(CurrentEnergy);
            playerHUD.UpdateHealth(health);
        }
        else
        {
            Debug.LogWarning($"{name} has no assigned StatsUI reference.");
        }
    }
    public void RollTurnOrderDice()
{
}
    public void ReceiveTileChoice(BoardTile chosenTile)
    {
        nextChosenTile = chosenTile;
        waitingForChoice = false;
    }

    public void AssignPartner(PartnerData partnerData, bool isFront)
    {
        float zOffset = isFront ? 1.5f : -1.5f; // use positive for front, negative for back

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

    public void MovePlayer(int steps, Action onComplete = null)
    {
        if (!isMoving)
            StartCoroutine(Move(steps, onComplete));
    }
    public void NotifyPartnerDeath(PartnerInstance partner)
    {
        Debug.Log($"{PlayerName}'s partner {partner.data.partnerName} died!");

        // Example UI update: highlight partner slot as empty or play effect
        playerHUD?.ShowPartnerDeath(partner);

        // You could also trigger some player-level feedback or UI update here
    }
    private IEnumerator Move(int steps, Action onComplete)
    {
        isMoving = true;
        if (animator) animator.SetBool("isMoving", true);

        while (steps > 0)
        {
            
            if (currentTile.nextTiles == null || currentTile.nextTiles.Count == 0)
            {
                Debug.Log($"{name} has no next tiles.");
                break;
            }

            List<BoardTile> validNextTiles = currentTile.nextTiles.Where(t => t != lastTile).ToList();
            if (validNextTiles.Count == 0)
                validNextTiles = new List<BoardTile>(currentTile.nextTiles);

            if (validNextTiles.Count == 1)
            {
                selectedNextTile = validNextTiles[0];
            }
            else
            {
                waitingForChoice = true;
                tileChoiceUI.ShowChoicesDirectional(this, validNextTiles, Vector3.forward);
                yield return new WaitUntil(() => !waitingForChoice);
                selectedNextTile = nextChosenTile;
            }

            lastTile = currentTile;
            Vector3Int moveDirection = Vector3Int.RoundToInt((selectedNextTile.transform.position - currentTile.transform.position).normalized);
            LogicalFacing = moveDirection;

            Vector3 start = transform.position;
            Vector3 end = selectedNextTile.transform.position + new Vector3(0, 1.1f, 0);

            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * moveSpeed;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t));
                Collider[] hits = Physics.OverlapSphere(transform.position, 0.6f);
                foreach (var hit in hits)
                {
                    PlayerMover other = hit.GetComponent<PlayerMover>();
                    if (other != null && other != this && !battledThisStep.Contains(other))
                    {
                        BattleSystem.ResolveBattle(this, other);
                        battledThisStep.Add(other);
                    }
                }
                // Update partners' positions during movement
                Vector3 offsetDirection = (Vector3)LogicalFacing;
                frontPartner?.SetTargetPosition(transform.position + offsetDirection * 1.2f + Vector3.up * 0.4f);
                backPartner?.SetTargetPosition(transform.position - offsetDirection * 1.2f + Vector3.up * 0.4f);

                yield return null;
            }
            bool aboutToHitStart = selectedNextTile != null && selectedNextTile.isStartTile;

            if (currentTile.isStartTile)
            {
                if (!hasAssignedPartnerAfterStart && !isPartnerSpawning && !partnerChoiceUI.IsOpen)
                {
                    hasAssignedPartnerAfterStart = true;
                    isPartnerSpawning = true;
                    Debug.Log($"{name} passed start tile and is spawning a partner.");
                    yield return StartCoroutine(SpawnPartnerRoutine(currentTile.partnerPool));
                    isPartnerSpawning = false;
                }
            }
            else
            {
                hasAssignedPartnerAfterStart = false;
            }

            currentTile = selectedNextTile;

            steps--;
            OnStep?.Invoke(steps);

            yield return new WaitForSeconds(0.2f);
        }

        currentTile.OnPlayerLand(this);

        isMoving = false;
        if (animator) animator.SetBool("isMoving", false);
        onComplete?.Invoke();
    }
    private static void BattleLog(string msg)
    {
        Debug.Log($"<color=yellow>[Battle]</color> {msg}");
    }
    // Coroutine to handle partner assign animation and partner assignment
    public IEnumerator SpawnPartnerRoutine(PartnerData[] partnerPool)
    {
        if (partnerPool == null || partnerPool.Length == 0 || partnerChoiceUI.IsOpen)
        {
            Debug.LogWarning("Skipping partner spawn: no pool or UI already open.");
            yield break;
        }

        PartnerData selectedPartner = partnerPool[UnityEngine.Random.Range(0, partnerPool.Length)];
        bool? choiceMade = null;

        partnerChoiceUI.Show(isFront => { choiceMade = isFront; });

        yield return new WaitUntil(() => choiceMade.HasValue);

        AssignPartner(selectedPartner, choiceMade.Value);

        Debug.Log($"Assigned {selectedPartner.partnerName} to {(choiceMade.Value ? "Front" : "Back")} slot.");

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
        {
            yield return partner.EndTurnCheckCoroutine();
        }
    }
    public void StartTurn()
    {
        battledThisStep.Clear();
        Debug.Log($"{PlayerName}'s turn started: battle history cleared.");
    }
    // Deduct partner salaries from player energy
    public void DeductPartnerSalaries()
    {
        if (frontPartner != null) frontPartner.TryPaySalary(this);
        if (backPartner != null) backPartner.TryPaySalary(this);
    }

    // ... other methods like OnTriggerEnter, ResolvePartnerCombat, TryPushForward, etc.
}