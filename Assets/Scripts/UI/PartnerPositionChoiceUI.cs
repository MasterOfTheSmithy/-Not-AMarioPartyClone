using System;
using UnityEngine;
using UnityEngine.UI;

public class PartnerPositionChoiceUI : MonoBehaviour
{
    public Button frontButton;
    public Button backButton;
    private bool isOpen = false;
    public bool IsOpen => isOpen;
    private Action<bool> onChoiceMade;

    public void Show(Action<bool> callback)
    {
        gameObject.SetActive(true);
        isOpen = true;
        onChoiceMade = callback;

        frontButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();

        frontButton.onClick.AddListener(() => Choose(true));
        backButton.onClick.AddListener(() => Choose(false));
    }

    private void Choose(bool isFront)
    {
        gameObject.SetActive(false);
        onChoiceMade?.Invoke(isFront);
        isOpen = false;
    }
}
