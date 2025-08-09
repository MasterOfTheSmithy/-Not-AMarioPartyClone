using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Board/Effects/Recruit Partner")]
public class RecruitPartnerEffect : TileEffect
{
    public PartnerData partnerToRecruit;
    public bool toFront = true;

    public override IEnumerator Apply(PlayerMover player)
    {
        if (partnerToRecruit != null)
            player.AssignPartner(partnerToRecruit, toFront);
        yield break;
    }
}
