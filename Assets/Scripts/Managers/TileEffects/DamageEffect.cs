using UnityEngine;

[CreateAssetMenu(menuName = "Board/Effects/Damage")]
public class DamageEffect : TileEffect
{
    public int amount = 1;
    public override void Apply(PlayerMover player)
    {
        player.ModifyEnergy(-amount);
    }
}
