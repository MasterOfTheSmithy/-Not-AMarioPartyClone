using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Board/Effects/Damage")]
public class DamageEffect : TileEffect
{
    public int amount = 1;
    public override IEnumerator Apply(PlayerMover player)
    {
        player.ModifyHealth(-amount);
        yield break;
    }
}
