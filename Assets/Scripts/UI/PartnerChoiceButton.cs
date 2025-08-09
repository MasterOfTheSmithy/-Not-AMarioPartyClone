using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class PartnerChoiceButton : MonoBehaviour
{
    public TMP_Text nameText;
    public Image portraitImage;
    public Button button;

    private PartnerData partner;

    public void Setup(PartnerData data, Action onClick)
    {
        partner = data;

        if (nameText == null) Debug.LogError("❌ PartnerChoiceButton: 'nameText' is not assigned.");
        if (portraitImage == null) Debug.LogError("❌ PartnerChoiceButton: 'portraitImage' is not assigned.");
        if (button == null) Debug.LogError("❌ PartnerChoiceButton: 'button' is not assigned.");

        nameText.text = partner.partnerName;
        portraitImage.sprite = partner.partnerPortrait;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke());
    }
}
