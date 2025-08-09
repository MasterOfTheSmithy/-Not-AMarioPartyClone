using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
public class PartnerInstance : MonoBehaviour
{
    public PartnerData data;
    public AudioSource audioSource;
    public AudioClip deathSound;
    private int currentHealth;
    private PlayerMover owner;
    private Vector3 localOffset;
    public PlayerMover Owner => owner;
    private Vector3 targetPosition;
    private float followSpeed = 8f;
    private bool isDying = false;
    private int unpaidTurns = 0;
    // Reference to partner dialogue UI component
    public PartnerDialogueUI dialogueUI;
    private Animator animator;

    public int CurrentHP => currentHealth;

    public void SetLocalOffset(Vector3 newOffset)
    {
        localOffset = newOffset;
    }

    public void Initialize(PartnerData partnerData, PlayerMover ownerMover, Vector3 offset, PartnerDialogueUI dialogueRef = null)
    {
        this.data = partnerData;
        this.owner = ownerMover;
        this.localOffset = offset;
        this.dialogueUI = dialogueRef ?? FindFirstObjectByType<PartnerDialogueUI>();
        currentHealth = data.maxHealth;

        Instantiate(data.modelPrefab, transform);

        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        deathSound = data.deathSoundClip;  // Assign here
        targetPosition = transform.position;

    }

    private void Start()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindFirstObjectByType<PartnerDialogueUI>();

            if (dialogueUI == null)
            {
                Debug.LogWarning($"PartnerDialogueUI not found in scene. Make sure it's enabled and active at runtime.");
            }
            else
            {
                Debug.Log("PartnerDialogueUI successfully found at runtime.");
            }
        }
    }

    private void Update()
    {
        if (owner == null) return;

        Vector3 facingDir = (Vector3)owner.LogicalFacing; // assuming LogicalFacing is normalized
        Vector3 worldOffset = facingDir * localOffset.z + Vector3.up * 0.1f;  // vertical offset

        Vector3 desiredPos = owner.transform.position + worldOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * followSpeed);
    }

    public bool TryPaySalary(PlayerMover owner)
    {
        int cost = GetSalaryCost();

        if (owner.CurrentEnergy >= cost)
        {
            unpaidTurns = 0; // Reset unpaid turn count on payment
            owner.ModifyEnergy(-cost);
            Debug.Log($"{data.partnerName} was paid {cost} energy.");
            return true;
        }
        else
        {
            unpaidTurns++;

            if (unpaidTurns == 1)
            {
                ShowDialogue(data.firstWarningDialogue);
                return true; // Survives first unpaid turn
            }
            else if (unpaidTurns == 2)
            {
                // Survives second unpaid turn but will be removed after turn ends
                return true;
            }
            else
            {
                // Should not happen here since removal is delayed until EndTurnCheck
                return false;
            }
        }
    }
    public IEnumerator EndTurnCheckCoroutine()
    {
        if (unpaidTurns >= 2)
        {
            if (dialogueUI != null)
                yield return dialogueUI.ShowDialogueAndWait(data.finalWarningDialogue, data.partnerPortrait, data.partnerName);
            else
                Debug.Log($"{data.partnerName} says final warning: {data.finalWarningDialogue}");

            Debug.Log($"{data.partnerName} removed due to second unpaid salary warning.");

            if (Owner.frontPartner == this) Owner.frontPartner = null;
            else if (Owner.backPartner == this) Owner.backPartner = null;

            Destroy(gameObject);
        }
        else
        {
            yield break; // No removal needed, end immediately
        }
    }
    private void ShowDialogue(string line)
    {
        if (dialogueUI != null)
        {
            // Use correct partner portrait sprite field from PartnerData
            dialogueUI.ShowDialogue(line, data.partnerPortrait, data.partnerName);
        }
        else
        {
            Debug.Log($"{data.partnerName} says: \"{line}\"");
        }
    }
    private IEnumerator HandleDeath()
    {
        if (isDying) yield break;
        isDying = true;

        if (dialogueUI != null)
            yield return dialogueUI.ShowDialogueAndWait($"{data.partnerName} has been defeated!", data.partnerPortrait, data.partnerName);
        else
            Debug.Log($"{data.partnerName} has been defeated!");

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
            yield return new WaitForSeconds(deathSound.length);
        }

        if (animator != null)
        {
            animator.SetTrigger("Die");
            // Optional: use animation length instead of hardcoded wait time
            yield return new WaitForSeconds(1f);
        }

        owner?.NotifyPartnerDeath(this);

        if (owner != null)
        {
            if (owner.frontPartner == this) owner.frontPartner = null;
            else if (owner.backPartner == this) owner.backPartner = null;
        }

        Destroy(gameObject);
    }
    public void SetTargetPosition(Vector3 pos)
    {
        targetPosition = pos; // Reserved for future precise control
    }

    public void TakeDamage(int amount)
    {
        if (isDying) return;  // Prevent re-entry if already dying

        currentHealth -= amount;
        Debug.Log($"{data.partnerName} took {amount} damage. Remaining HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            StartCoroutine(HandleDeath());
        }
    }

    public int GetAttackPower() => data.attack;
    public int GetSalaryCost() => data.salary;
}