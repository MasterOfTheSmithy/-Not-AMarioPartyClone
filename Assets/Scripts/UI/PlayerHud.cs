using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text energyText;
    // Optionally add partnerText or avatarImage here if needed

    private PlayerMover player;
    public Image backgroundImage;

    public void Initialize(PlayerMover assignedPlayer)
    {
        player = assignedPlayer;
        playerNameText.text = player.PlayerName;  // Use PlayerName or "Player " + id+1

        UpdateEnergy(player.CurrentEnergy);
        UpdateHealth(player.health);

        player.OnEnergyChanged += UpdateEnergy;
        // If you add health changed event, subscribe here as well
    }
    public void ShowPartnerDeath(PartnerInstance partner)
    {
        // Example: Disable partner icon, play UI animation, flash red, etc.
        Debug.Log($"Partner {partner.data.partnerName} death notified in HUD.");
    }
    public void UpdateEnergy(int newEnergy)
    {
        if (energyText != null)
            energyText.text = $"Energy: {newEnergy}";
    }

    public void UpdateHealth(int newHealth)
    {
        if (healthText != null)
            healthText.text = $"Health: {newHealth}";
    }
    public void SetHighlight(bool isActive)
    {
        if (backgroundImage != null)
        {
            Color highlightColor = new Color(1f, 1f, 0f, 1f); // Yellow with 30% opacity
            Color normalColor = new Color(1f, 1f, 1f, 0f); // Transparent (or original color)

            backgroundImage.color = isActive ? highlightColor : normalColor;
        }
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnEnergyChanged -= UpdateEnergy;
            // Unsubscribe health event if implemented
        }
    }
}