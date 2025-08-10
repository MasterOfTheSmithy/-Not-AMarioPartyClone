using UnityEngine;

public abstract class TileEffect : ScriptableObject
{
    public abstract void Apply(PlayerMover player);
}
