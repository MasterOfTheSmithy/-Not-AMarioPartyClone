using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DiceRoller : MonoBehaviour
{
    [SerializeField] private float diceFollowHeight = 2f;
    [SerializeField] private float diceFollowDistance = 1.5f;

    private bool isTurnOrderActive = false;
    private System.Action<int> onTurnOrderRolledCallback = null;

    [Header("Turn Logic")]
    public PlayerMover currentPlayer;
    public TurnManager turnManager;

    [Header("UI")]
    public TMP_Text diceResultText;
    public TMP_Text turnIndicatorText;
    public Button rollButton;

    [Header("3D Dice")]
    public GameObject dicePrefab;
    public float heightAbovePlayer = 2.2f;
    public float spinSpeed = 360f;
    public float followSmoothSpeed = 5f;

    private GameObject activeDice;
    private Transform diceTransform;
    private TextMeshPro faceText;
    private bool isSpinning = false;
    private bool isRolling = false;
    private int rollResult = 0;

    private Vector3 followOffset = Vector3.zero;
    private void Awake()
    {
        // Preload a few dice instances for pooling at scene start
        SimplePool.Preload(dicePrefab, 4);
    }

    public void BeginTurn()
    {
        if (activeDice != null)
        {
            SimplePool.Return(dicePrefab, activeDice);
            diceTransform = null;
            faceText = null;
            activeDice = null;
        }

        currentPlayer = turnManager.CurrentPlayer;

        turnIndicatorText.text = $"{currentPlayer.PlayerName}'s Turn";
        diceResultText.text = "";
        rollButton.interactable = true;

        SpawnAndSpinDice();

        // Subscribe to movement feedback
        currentPlayer.OnStep += OnPlayerStep;
        currentPlayer.OnMovementComplete = OnMoveComplete;
    }

    private void Update()
    {
        if (diceTransform == null || currentPlayer == null)
            return;

        Vector3 baseFollowPos = currentPlayer.transform.position + Vector3.up * heightAbovePlayer;
        Vector3 targetPos = baseFollowPos + followOffset;

        diceTransform.position = Vector3.Lerp(diceTransform.position, targetPos, Time.deltaTime * followSmoothSpeed);

        if (isSpinning)
        {
            float angle = spinSpeed * Time.deltaTime;
            diceTransform.Rotate(Vector3.up, angle, Space.World);
            diceTransform.Rotate(Vector3.right, angle / 2f, Space.World);
        }

        if (faceText != null)
        {
            faceText.transform.rotation = Quaternion.LookRotation(
                faceText.transform.position - Camera.main.transform.position
            );
        }
    }

    private void SpawnAndSpinDice()
    {
        if (activeDice != null)
            SimplePool.Return(dicePrefab, activeDice);

        Vector3 spawnPos = currentPlayer.transform.position + Vector3.up * heightAbovePlayer;
        activeDice = SimplePool.Get(dicePrefab);
        diceTransform = activeDice.transform;

        faceText = activeDice.GetComponentInChildren<TextMeshPro>();
        if (faceText != null)
            faceText.text = "";
        diceTransform.position = spawnPos;
        diceTransform.rotation = Quaternion.identity;
        isSpinning = true;
        isRolling = false;
        rollResult = 0;
        followOffset = Vector3.zero;
    }

    public void RollDice()
    {
        if (turnManager.CurrentPhase != TurnPhase.WaitingForRoll || !isSpinning || isRolling)
            return;

        rollButton.interactable = false;
        isSpinning = false;
        isRolling = true;

        rollResult = Random.Range(1, 11); // Roll 1–10

        if (isTurnOrderActive)
        {
            StartCoroutine(CleanupTurnOrderRoll(rollResult));
            return;
        }

        StartCoroutine(StopDiceAndMovePlayer());
    }

    private IEnumerator StopDiceAndMovePlayer()
    {
        turnManager.SetPhase(TurnPhase.Moving);

        Vector3 camDir = (Camera.main.transform.position - diceTransform.position).normalized;
        diceTransform.rotation = Quaternion.LookRotation(-camDir, Vector3.up);

        yield return new WaitForSeconds(0.2f);

        if (faceText != null)
            faceText.text = rollResult.ToString();

        diceResultText.text = "Rolled: " + rollResult;

        yield return new WaitForSeconds(0.5f);

        followOffset = new Vector3(1f, 1f, 0f);

        currentPlayer.MovePlayer(rollResult);
    }

    private void OnPlayerStep(int stepsRemaining)
    {
        if (faceText != null)
            faceText.text = stepsRemaining.ToString();

        diceResultText.text = $"Moving... {stepsRemaining} steps left";
    }

    private void OnMoveComplete()
    {
        if (currentPlayer != null)
        {
            currentPlayer.OnStep -= OnPlayerStep;
            currentPlayer.OnMovementComplete = null;
        }

        if (activeDice != null)
        {
            SimplePool.Return(dicePrefab, activeDice);
            activeDice = null;
            diceTransform = null;
            faceText = null;
        }

        followOffset = Vector3.zero;

        StartCoroutine(turnManager.ResolveTilePhase());
    }

    public void BeginTurnOrderRoll(PlayerMover player, System.Action<int> onRolled)
    {
        currentPlayer = player;
        isTurnOrderActive = true;
        onTurnOrderRolledCallback = onRolled;

        if (dicePrefab != null && activeDice == null)
        {
            activeDice = SimplePool.Get(dicePrefab);
            Vector3 startPos = player.transform.position + new Vector3(0, diceFollowHeight, -diceFollowDistance);
            activeDice.transform.position = startPos;
            activeDice.transform.rotation = Quaternion.identity;
        }
    }

    private IEnumerator CleanupTurnOrderRoll(int roll)
    {
        yield return new WaitForSeconds(0.2f);

        if (activeDice != null)
        {
            SimplePool.Return(dicePrefab, activeDice);
            activeDice = null;
        }

        var cb = onTurnOrderRolledCallback;
        onTurnOrderRolledCallback = null;
        isTurnOrderActive = false;

        cb?.Invoke(roll);
    }
}
