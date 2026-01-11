using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using VInspector;

public class BoardModel : MonoBehaviour
{
    [Header("Board Tiles")]
    [SerializeField] private List<BoardTileView> tiles = new List<BoardTileView>();
    
    [Header("Rigged Outcome")]
    [SerializeField] private int riggedWinnerIndex;
    
    [Header("Upgrade")]
    [SerializeField] private PlayableDirector upgradeTimeline;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Entrence")]
    [SerializeField] private bool hasEntranceEffect = true;

    public bool HasEntranceEffect => hasEntranceEffect;
    // Public accessors for Controller to read data
    public IReadOnlyList<BoardTileView> Tiles => tiles;
    public int RiggedWinnerIndex => riggedWinnerIndex;
    public PlayableDirector UpgradeTimeline => upgradeTimeline;
    public AudioSource AudioSource => audioSource;
    public bool HasUpgrade => upgradeTimeline != null;
    public int TileCount => tiles.Count;
    

    public bool IsValid()
    {
        // Validates that the board isn't empty of tiles
        if (tiles.Count <= 0)
        {
            Debug.LogError("[TileBlinkController] No tiles provided");
        }
        // Validates that rigged index is within bounds
        else if (riggedWinnerIndex < 0 || riggedWinnerIndex > tiles.Count)
        {
            Debug.LogError($"[TileBlinkController] Invalid target index (out of range): {riggedWinnerIndex}");
        }
        return tiles != null && 
               tiles.Count > 0 && 
               riggedWinnerIndex >= 0 && 
               riggedWinnerIndex < tiles.Count;
    }
    
    // Returns tile at index with null check
    public BoardTileView GetTile(int index)
    {
        if (index >= 0 && index < tiles.Count)
        {
            return tiles[index];
        }
        return null;
    }
    
    // Auto-populates tiles list from children with BoardTileView component
    [Button("Auto Find Tiles In Children")]
    private void AutoFindTiles()
    {
        tiles.Clear();
        tiles.AddRange(GetComponentsInChildren<BoardTileView>(true));
    }
}