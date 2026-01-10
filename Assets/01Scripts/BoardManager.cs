using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class BoardManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<BoardData> boards = new List<BoardData>();
    [SerializeField] private TileBlinkController blinkController;
    
    [Header("State")]
    [SerializeField] private int currentBoardIndex = 0;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Events
    public event Action<int> OnBoardChanged;
    public event Action<BoardTileView> OnRewardWon;
    public event Action OnUpgradeStarted;
    public event Action OnUpgradeCompleted;
    public event Action OnAllBoardsCompleted;
    
    public int CurrentBoardIndex => currentBoardIndex;
    public BoardData CurrentBoard => GetBoardAt(currentBoardIndex);
    public bool IsLastBoard => currentBoardIndex >= boards.Count - 1;
    public bool IsSequenceRunning => blinkController != null && blinkController.IsRunning;
    
    private void OnEnable()
    {
        if (blinkController != null)
        {
            blinkController.OnTileSelected += HandleTileSelected;
            blinkController.OnBlinkSequenceCompleted += HandleSequenceCompleted;
        }
    }
    
    private void OnDisable()
    {
        if (blinkController != null)
        {
            blinkController.OnTileSelected -= HandleTileSelected;
            blinkController.OnBlinkSequenceCompleted -= HandleSequenceCompleted;
        }
    }
    
    // Starts the reward blink sequence on the current board
    // Uses rigged winner index from current BoardData
    public void StartRewardSequence()
    {
        BoardData board = CurrentBoard;
        
        if (board == null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[BoardManager] No current board available");
            }
            return;
        }
        
        if (!board.IsValid())
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BoardManager] Board {currentBoardIndex} has invalid configuration");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardManager] Starting reward sequence on board {currentBoardIndex}, rigged index: {board.RiggedWinnerIndex}");
        }


        blinkController.StartBlinkSequence(board.Tiles, board.RiggedWinnerIndex);
    }
    
    // Called when blink sequence lands on the winning tile
    // Triggers Win animation and fires event
    private void HandleTileSelected(BoardTileView winningTile)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardManager] Tile selected: {winningTile.name}");
        }
        
        winningTile.TriggerWin();
        OnRewardWon?.Invoke(winningTile);
    }
    
    // Called when entire blink sequence completes
    // Automatically triggers upgrade to next level
    private void HandleSequenceCompleted()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[BoardManager] Sequence completed, initiating upgrade");
        }

        StartCoroutine(DelayedNextLevel());
    }

    private IEnumerator DelayedNextLevel()
    {
        yield return new WaitForSeconds(1f);
        UpgradeToNextLevel();
    }
    // Upgrades to the next board level by playing the upgrade timeline
    // Does nothing if already on last board or no upgrade timeline
    public void UpgradeToNextLevel()
    {
        BoardData currentBoard = CurrentBoard;
        
        if (currentBoard == null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[BoardManager] No current board to upgrade from");
            }
            return;
        }
        
        if (!currentBoard.HasUpgrade)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[BoardManager] Current board has no upgrade timeline (last board)");
            }
            
            OnAllBoardsCompleted?.Invoke();
            return;
        }
        
        if (IsLastBoard)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[BoardManager] Already on last board");
            }
            
            OnAllBoardsCompleted?.Invoke();
            return;
        }
        
        OnUpgradeStarted?.Invoke();
        
        PlayableDirector timeline = currentBoard.UpgradeTimeline;
        timeline.stopped += OnTimelineStopped;
        timeline.Play();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardManager] Playing upgrade timeline for board {currentBoardIndex}");
        }
    }
    
    // Called when upgrade timeline finishes playing
    // Increments board index and fires completion event
    private void OnTimelineStopped(PlayableDirector director)
    {
        director.stopped -= OnTimelineStopped;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardManager] Upgraded to board {currentBoardIndex}");
        }
        
        OnBoardChanged?.Invoke(currentBoardIndex);
        OnUpgradeCompleted?.Invoke();
    }
    
    // Gets board data at specified index with bounds checking
    // Returns null if index is out of range
    private BoardData GetBoardAt(int index)
    {
        if (index >= 0 && index < boards.Count)
        {
            return boards[index];
        }
        return null;
    }
    
    // Resets board progression to first board
    // Useful for testing or new game
    public void ResetToFirstBoard()
    {
        currentBoardIndex = 0;
        OnBoardChanged?.Invoke(currentBoardIndex);
        
        if (enableDebugLogs)
        {
            Debug.Log("[BoardManager] Reset to first board");
        }
    }
    
    // Forces a specific board index for save/load or debugging
    // Open for future extensibility
    public void IncreaseBoardIndex()
    {
        currentBoardIndex++;
        OnBoardChanged?.Invoke(currentBoardIndex);
            
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardManager] Board index set to {currentBoardIndex}");
        }
        
    }
    
    // Auto-finds all BoardData components in children
    // Useful for quick setup - call from context menu
    [ContextMenu("Auto Find Boards In Children")]
    private void AutoFindBoards()
    {
        boards.Clear();
        boards.AddRange(GetComponentsInChildren<BoardData>(true));
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardManager] Found {boards.Count} boards in children");
        }
    }
}