using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartnerPositionChoiceUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public GameObject rootPanel;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private GameObject partnerChoiceButtonPrefab;
    [SerializeField] private GameObject frontBackPromptPanel;
    [SerializeField] private Button frontButton;
    [SerializeField] private Button backButton;

    private Action<PartnerData, bool> onPartnerChosen;
    private PartnerData selectedPartner;

    public bool IsOpen => rootPanel != null && rootPanel.activeSelf;

    private void Awake()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (frontBackPromptPanel != null) frontBackPromptPanel.SetActive(false);

        // Set correct text on buttons
        TMP_Text frontText = frontButton?.GetComponentInChildren<TMP_Text>();
        TMP_Text backText = backButton?.GetComponentInChildren<TMP_Text>();

        if (frontText != null) frontText.text = "Front";
        if (backText != null) backText.text = "Back";

        frontButton?.onClick.AddListener(() => OnFrontBackChosen(true));
        backButton?.onClick.AddListener(() => OnFrontBackChosen(false));
    }

    public void Show(PartnerData[] choices, Action<PartnerData, bool> callback)
    {
        Debug.Log("[Partner UI] Show() called");

        ClearPreviousButtons();
        selectedPartner = null;
        onPartnerChosen = callback;

        if (rootPanel == null)
        {
            Debug.LogError("[Partner UI] rootPanel is null!");
        }
        else
        {
            Debug.Log("[Partner UI] Activating root panel: " + rootPanel.name);
            rootPanel.SetActive(true);
        }

        foreach (var partner in choices)
        {
            var buttonObj = Instantiate(partnerChoiceButtonPrefab, choiceButtonContainer);
            var ui = buttonObj.GetComponent<PartnerChoiceButton>();
            ui.Setup(partner, () => OnPartnerSelected(partner));
        }
    }

    private void OnPartnerSelected(PartnerData partner)
    {
        selectedPartner = partner;

        if (frontBackPromptPanel != null)
            frontBackPromptPanel.SetActive(true);
        else
            Debug.LogError("❌ frontBackPromptPanel is not assigned!");
    }

    private void OnFrontBackChosen(bool isFront)
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (frontBackPromptPanel != null) frontBackPromptPanel.SetActive(false);

        if (onPartnerChosen != null)
        {
            onPartnerChosen.Invoke(selectedPartner, isFront);
        }
        else
        {
            Debug.LogWarning("⚠️ No partner selection callback set.");
        }

        onPartnerChosen = null;
        selectedPartner = null;
    }

    private void ClearPreviousButtons()
    {
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    // Optional cleanup
    private void OnDestroy()
    {
        frontButton?.onClick.RemoveAllListeners();
        backButton?.onClick.RemoveAllListeners();
    }
}
