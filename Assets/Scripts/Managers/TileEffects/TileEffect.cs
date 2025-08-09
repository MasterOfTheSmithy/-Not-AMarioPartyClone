using System.Collections;
using UnityEngine;

public abstract class TileEffect : ScriptableObject
{
    public abstract IEnumerator Apply(PlayerMover player);
}
