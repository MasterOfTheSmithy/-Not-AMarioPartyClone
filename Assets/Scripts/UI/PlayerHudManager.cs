using UnityEngine;

public class PlayerHUDManager : MonoBehaviour
{
    public PlayerHUD[] hudSlots;      // Assigned in inspector
    public PlayerMover[] players;     // PlayerMover references
    public RectTransform[] uiSlots;   // UI corner anchors for HUD placement

    public void Start()
    {
        int count = Mathf.Min(players.Length, hudSlots.Length);

        for (int i = 0; i < count; i++)
        {
            RectTransform uiAnchor = uiSlots[i];
            RectTransform hudRect = hudSlots[i].GetComponent<RectTransform>();

            // Parent HUD to the UI anchor without keeping world position
            hudRect.SetParent(uiAnchor, false);

            // Reset HUD transform to center inside the anchor
            hudRect.anchorMin = new Vector2(0.5f, 0.5f);
            hudRect.anchorMax = new Vector2(0.5f, 0.5f);
            hudRect.pivot = new Vector2(0.5f, 0.5f);
            hudRect.anchoredPosition = Vector2.zero;
            hudRect.localScale = Vector3.one;
            hudRect.localRotation = Quaternion.identity;

            // Initialize HUD with player reference and update display
            hudSlots[i].Initialize(players[i]);
            players[i].playerHUD = hudSlots[i];
        }
    }
}
