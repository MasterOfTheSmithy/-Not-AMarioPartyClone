using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class DiceRoller : MonoBehaviour
{
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

    // Persistent follow offset to keep dice up and right after roll
    private Vector3 followOffset = Vector3.zero;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => !TurnManager.Instance.isRollingTurnOrder);
        BeginTurn();
    }

    private void Update()
    {
        if (diceTransform == null || currentPlayer == null)
            return;

        // Smooth follow above the player + extra persistent offset
        Vector3 baseFollowPos = currentPlayer.transform.position + Vector3.up * heightAbovePlayer;
        Vector3 targetPos = baseFollowPos + followOffset;

        diceTransform.position = Vector3.Lerp(diceTransform.position, targetPos, Time.deltaTime * followSmoothSpeed);

        // Rotate the dice if spinning
        if (isSpinning)
        {
            float angle = spinSpeed * Time.deltaTime;
            diceTransform.Rotate(Vector3.up, angle, Space.World);
            diceTransform.Rotate(Vector3.right, angle / 2f, Space.World);
        }

        // Ensure text always faces camera (billboard)
        if (faceText != null)
        {
            faceText.transform.rotation = Quaternion.LookRotation(faceText.transform.position - Camera.main.transform.position);
        }
    }

    public void BeginTurn()
    {
        currentPlayer = turnManager.CurrentPlayer;
        turnIndicatorText.text = $"{currentPlayer.name}'s Turn";
        diceResultText.text = "";
        rollButton.interactable = true;

        SpawnAndSpinDice();

        currentPlayer.OnStep += OnPlayerStep;
    }

    private void SpawnAndSpinDice()
    {
        if (activeDice != null)
            Destroy(activeDice);

        Vector3 spawnPos = currentPlayer.transform.position + Vector3.up * heightAbovePlayer;
        activeDice = Instantiate(dicePrefab, spawnPos, Quaternion.identity);
        diceTransform = activeDice.transform;

        faceText = activeDice.GetComponentInChildren<TextMeshPro>();
        if (faceText != null)
            faceText.text = "";

        diceTransform.rotation = Quaternion.identity;
        isSpinning = true;
        isRolling = false;
        rollResult = 0;

        // Reset follow offset for new dice spawn
        followOffset = Vector3.zero;
    }

    public void RollDice()
    {
        if (!isSpinning || isRolling) return;

        rollButton.interactable = false;
        isSpinning = false;
        isRolling = true;

        rollResult = Random.Range(1, 11); // Roll 1-10

        StartCoroutine(StopDiceAndMovePlayer());
    }

    private IEnumerator StopDiceAndMovePlayer()
    {
        // Rotate the entire dice to face the camera (not just the text)
        Vector3 camDir = (Camera.main.transform.position - diceTransform.position).normalized;
        diceTransform.rotation = Quaternion.LookRotation(-camDir, Vector3.up);

        yield return new WaitForSeconds(0.2f);

        if (faceText != null)
            faceText.text = rollResult.ToString();

        diceResultText.text = "Rolled: " + rollResult;

        yield return new WaitForSeconds(0.5f);

        // Set the persistent offset to (1,1,0) so dice stays up and right
        followOffset = new Vector3(1f, 1f, 0f);

        // Now move player
        currentPlayer.MovePlayer(rollResult, OnMoveComplete);
    }

    private void OnPlayerStep(int stepsRemaining)
    {
        if (faceText != null)
            faceText.text = stepsRemaining.ToString();

        diceResultText.text = $"Moving... {stepsRemaining} steps left";
    }

    private void OnMoveComplete()
    {
        currentPlayer.OnStep -= OnPlayerStep;

        if (activeDice != null)
            Destroy(activeDice);

        // Reset offset so next dice spawn starts fresh
        followOffset = Vector3.zero;

        // Start the coroutine to advance turn and start the next one,
        // but don't call BeginTurn here! It'll be called by TurnManager
        StartCoroutine(TurnManager.Instance.NextTurnCoroutine());
    }
}