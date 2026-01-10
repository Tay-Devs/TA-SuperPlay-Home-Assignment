using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
public class BoardData : MonoBehaviour
{
    [Header("Board Tiles")]
    [Tooltip("All tiles belonging to this board")]
    [SerializeField] private List<BoardTileView> tiles = new List<BoardTileView>();
    
    [Header("Rigged Outcome")]
    [Tooltip("Index of the tile that will always win")]
    [SerializeField] private int riggedWinnerIndex;
    
    [Header("Upgrade")]
    [Tooltip("Timeline to play when upgrading to next board (null for last board)")]
    [SerializeField] private PlayableDirector upgradeTimeline;
    
    public IReadOnlyList<BoardTileView> Tiles => tiles;
    public int RiggedWinnerIndex => riggedWinnerIndex;
    public PlayableDirector UpgradeTimeline => upgradeTimeline;
    public bool HasUpgrade => upgradeTimeline != null;
    
    // Validates that rigged index is within bounds
    // Called in editor to prevent configuration errors
    public bool IsValid()
    {
        return tiles != null && tiles.Count > 0 && riggedWinnerIndex >= 0 && riggedWinnerIndex < tiles.Count;
    }
    
    // Gets the winning tile directly for convenience
    // Returns null if index is out of bounds
    public BoardTileView GetWinningTile()
    {
        if (riggedWinnerIndex >= 0 && riggedWinnerIndex < tiles.Count)
        {
            return tiles[riggedWinnerIndex];
        }
        return null;
    }
}