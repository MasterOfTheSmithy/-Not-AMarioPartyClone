using UnityEngine;

[CreateAssetMenu(menuName = "Board/Effects/Heal")]
public class HealEffect : TileEffect
{
    public int amount = 2;
    public override void Apply(PlayerMover player)
    {
        
        player.ModifyEnergy(amount);
    }
}
