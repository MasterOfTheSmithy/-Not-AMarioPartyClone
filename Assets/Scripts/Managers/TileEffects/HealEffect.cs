using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Board/Effects/Heal")]
public class HealEffect : TileEffect
{
    public int amount = 2;
    public override IEnumerator Apply(PlayerMover player)
    {
        player.ModifyHealth(amount);
        yield break;
    }
}
