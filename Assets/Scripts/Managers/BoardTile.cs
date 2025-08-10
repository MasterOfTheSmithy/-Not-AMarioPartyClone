using System.Collections.Generic;
using UnityEngine;

public class BoardTile : MonoBehaviour
{
    [Header("Tile Properties")]
    public Vector2Int gridPosition;                     // Logical grid coordinates (optional)
    public List<BoardTile> nextTiles = new List<BoardTile>(); // Tiles connected ahead
    public bool isSpecialTile = false;                  // For special behaviors
    public List<TileEffect> tileEffects = new List<TileEffect>();
    public TileType tileType = TileType.Normal;
    public bool isStartTile = false;
    public PartnerData[] partnerPool;
    public enum TileType
    {
        Normal,
        Start,
        Battle,
        Chance,
        Event,
        Swap,
        Positive,
        Negative,
        Store,
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = tileType switch
        {
            TileType.Start => Color.green,
            TileType.Battle => Color.red,
            TileType.Chance => Color.yellow,
            TileType.Event => Color.cyan,
            TileType.Swap => Color.magenta,
            _ => Color.white
        };

        Gizmos.DrawSphere(transform.position + Vector3.up * 0.2f, 0.2f);
    }

    public void OnPlayerLand(PlayerMover player)
    {
        if (tileType == TileType.Start)
        {
            // NO LONGER NEEDED - handled in PlayerMover
            // player.StartCoroutine(player.SpawnPartnerRoutine(partnerPool));
            return;  // Skip other tile logic for start tile if you want
        }
        foreach (TileEffect effect in tileEffects)
        {
            {
                if (effect != null)
                {
                    effect.Apply(player);
                }
            }
        }
    }
}

