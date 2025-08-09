using System.Collections.Generic;
using UnityEngine;

public class BoardTile : MonoBehaviour
{
    [Header("Tile Properties")]
    public Vector2Int gridPosition;                     // Logical grid coordinates (optional)
    public List<BoardTile> nextTiles = new List<BoardTile>(); // Tiles connected ahead
    public bool isSpecialTile = false;                  // For special behaviors
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
        switch (tileType)
        {
            case TileType.Positive:  // Positive tile effect
                player.ModifyEnergy(3);
                Debug.Log($"{player.name} landed on a Positive tile! Gained 3 energy.");
                break;

            case TileType.Negative:  // Negative tile effect
                player.ModifyEnergy(-3);
                Debug.Log($"{player.name} landed on a Negative tile! Lost 3 energy.");
                break;

            case TileType.Battle:
                Debug.Log("Battle tile! Trigger battle event here.");
                break;

            case TileType.Chance:
                Debug.Log("Chance tile! Trigger chance event here.");
                break;

            case TileType.Event:
                Debug.Log("Event tile! Trigger event here.");
                break;

            case TileType.Swap:
                Debug.Log("Swap tile! Trigger swap event here.");
                break;

            case TileType.Store:
                Debug.Log("Store tile! Trigger store event here.");
                break;

            default:
                Debug.Log($"{player.name} landed on a neutral tile.");
                break;
        }
    }
}
