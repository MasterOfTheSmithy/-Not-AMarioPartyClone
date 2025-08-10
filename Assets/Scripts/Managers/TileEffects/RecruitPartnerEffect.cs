using UnityEngine;

[CreateAssetMenu(menuName = "Board/Effects/Recruit Partner")]
public class RecruitPartnerEffect : TileEffect
{
    public PartnerData partnerToRecruit;
    public bool toFront = true;

    public override void Apply(PlayerMover player)
    {
        if (partnerToRecruit != null)
            player.AssignPartner(partnerToRecruit, toFront);
        
    }
}
