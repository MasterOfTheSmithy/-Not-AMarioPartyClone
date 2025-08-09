using UnityEngine;

public enum PartnerPersonality { Nice, Mean, Neutral }

[CreateAssetMenu(fileName = "NewPartner", menuName = "Partners/PartnerData")]
public class PartnerData : ScriptableObject
{
    public string partnerName;
    public GameObject modelPrefab;
    public PartnerData[] partnerPool;
    public Sprite partnerPortrait;
    public int maxHealth = 5;
    public int attack = 1;
    public int salary = 1;

    public PartnerPersonality personality;
    public AudioClip deathSoundClip;
    [TextArea] public string firstWarningDialogue;
    [TextArea] public string finalWarningDialogue;

   
}