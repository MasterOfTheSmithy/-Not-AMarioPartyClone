using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class PartnerDialogueUI : MonoBehaviour
{
    [Header("UI Elements")]
    public CanvasGroup canvasGroup;      // For fade in/out
    public TMP_Text dialogueText;        // TextMeshPro text component
    public Image partnerPortrait;        // Optional: partner image
    public TMP_Text partnerNameText;

    [Header("Settings")]
    public float displayDuration = 3f;   // How long dialogue stays visible
    public float fadeDuration = 0.5f;    // Fade in/out time

    private Coroutine currentCoroutine;
    
    private void Awake()
    {
        HideImmediate();
    }
    
    public void ShowDialogue(string line, Sprite portrait = null, string partnerName = null)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        dialogueText.text = line;

        if (partnerPortrait != null)
        {
            if (portrait != null)
            {
                partnerPortrait.sprite = portrait;
                partnerPortrait.gameObject.SetActive(true);
            }
            else
            {
                partnerPortrait.gameObject.SetActive(false);
            }
        }

        if (partnerNameText != null)
        {
            if (!string.IsNullOrEmpty(partnerName))
            {
                partnerNameText.text = partnerName;
                partnerNameText.gameObject.SetActive(true);
            }
            else
            {
                partnerNameText.gameObject.SetActive(false);
            }
        }

        currentCoroutine = StartCoroutine(ShowAndHideCoroutine());
    }

    public IEnumerator ShowAndHideCoroutine()
    {
        yield return Fade(0f, 1f);           // Fade in
        yield return new WaitForSeconds(displayDuration);
        yield return Fade(1f, 0f);           // Fade out
    }
    public IEnumerator ShowDialogueAndWait(string line, Sprite portrait = null, string partnerName = null)
    {
        ShowDialogue(line, portrait, partnerName);
        // Wait for the entire fade-in, displayDuration, fade-out cycle
        yield return ShowAndHideCoroutine();
    }
    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            canvasGroup.alpha = alpha;
            yield return null;
        }

        canvasGroup.alpha = to;

        if (to == 1f)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void HideImmediate()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

    }
}
