using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using VInspector;

// Responsible for board progression, reward sequences, and board upgrade
// Acts as the main coordinator between BoardModel, TileBlinkController, and BoardEntranceController
public class BoardManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private List<BoardModel> boards = new List<BoardModel>();
    [SerializeField] private TileBlinkController blinkController;
    [SerializeField] private BoardEntranceController entranceController;
    
    [Header("Animation")]
    [SerializeField] private AnimationClip winAnimationClip;
    [SerializeField] private float additionalWinDelay = 0.2f;
    
    [Header("State")]
    [SerializeField] private int currentBoardIndex = 0;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Events for external systems to subscribe
    public event Action<int> OnBoardChanged;
    public event Action<BoardTileView> OnRewardWon;
    public event Action OnUpgradeStarted;
    public event Action OnUpgradeCompleted;
    public event Action OnAllBoardsCompleted;
    
    // Public accessors
    public int CurrentBoardIndex => currentBoardIndex;
    public BoardModel CurrentBoard => GetBoardAt(currentBoardIndex);
    public bool IsLastBoard => currentBoardIndex >= boards.Count - 1;
    public bool IsSequenceRunning => blinkController != null && blinkController.IsRunning;
    
    private void OnEnable()
    {
        SubscribeToEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    // Subscribes to blink controller events
    private void SubscribeToEvents()
    {
        if (blinkController == null) return;
        
        blinkController.OnTileSelected += HandleTileSelected;
        blinkController.OnBlinkSequenceCompleted += HandleSequenceCompleted;
    }
    
    // Unsubscribes from events to prevent memory leaks
    private void UnsubscribeFromEvents()
    {
        if (blinkController == null) return;
        
        blinkController.OnTileSelected -= HandleTileSelected;
        blinkController.OnBlinkSequenceCompleted -= HandleSequenceCompleted;
    }
    
    // Starts the reward blink sequence on the current board
    
    public void StartRewardSequence()
    {
        BoardModel board = CurrentBoard;
        
        if (!ValidateBoard(board)) return;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardManager] Starting reward sequence on board {currentBoardIndex}");
        }
        
        blinkController.StartBlinkSequence(board.Tiles, board.RiggedWinnerIndex);
    }
    
    // Validates board exists and has valid configuration
    private bool ValidateBoard(BoardModel board)
    {
        if (board == null)
        {
            Debug.LogError("[BoardManager] No current board available");
            
            return false;
        }
        
        if (!board.IsValid())
        {
            Debug.LogError($"[BoardManager] Board {currentBoardIndex} has invalid configuration");
          
            return false;
        }
        
        return true;
    }
    
    // Handles winning tile selection from blink controller
    private void HandleTileSelected(BoardTileView winningTile)
    {
        // Triggers win animation and fires event for external listeners
        winningTile.TriggerWin();
        OnRewardWon?.Invoke(winningTile);
    }
    
    // Called when blink sequence completes
    private void HandleSequenceCompleted()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[BoardManager] Sequence completed, initiating upgrade");
        }
        
        // Adds delay according to the length of the winning tile animation clip + Additional Win Delay Var
        float delay = (winAnimationClip != null ? winAnimationClip.length : 0f) + additionalWinDelay;
        
        StartCoroutine(DelayedUpgrade(delay));
    }
    
    // Purely for delay to not start the timeline before the animation of winning tile ends
    private IEnumerator DelayedUpgrade(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpgradeToNextBoard();
    }
    
    // Upgrades to next board by playing upgrade timeline
    public void UpgradeToNextBoard()
    {
        BoardModel currentBoard = CurrentBoard;
        
        if (currentBoard == null || !currentBoard.HasUpgrade || IsLastBoard)
        {
           
            Debug.LogWarning("[BoardManager] No Board upgrade available or last board reached");
            
            OnAllBoardsCompleted?.Invoke();
            return;
        }
        
        OnUpgradeStarted?.Invoke();
        
        // Play the timeline of the current board
        PlayableDirector timeline = currentBoard.UpgradeTimeline;
        timeline.stopped += HandleTimelineStopped;
        timeline.Play();
    }
    
    // Handles upgrade timeline completion
    private void HandleTimelineStopped(PlayableDirector director)
    {
        director.stopped -= HandleTimelineStopped;
        
        // Increments board index
        currentBoardIndex++;
    
        // Only trigger entrance if board supports it
        if (entranceController != null && CurrentBoard != null && CurrentBoard.HasEntranceEffect)
        {
            entranceController.SetTargetBoard(CurrentBoard);
        }
    
        // Notifies listeners
        OnBoardChanged?.Invoke(currentBoardIndex);
        OnUpgradeCompleted?.Invoke();
    }
    
    // Gets board at specified index with bounds checking
    private BoardModel GetBoardAt(int index)
    {
        if (index >= 0 && index < boards.Count)
        {
            return boards[index];
        }
        return null;
    }
    
    // Resets board progression to first board
    // This is just in case I make this loop
    public void ResetToFirstBoard()
    {
        currentBoardIndex = 0;
        
        if (entranceController != null && CurrentBoard != null)
        {
            entranceController.SetTargetBoard(CurrentBoard);
        }
        
        OnBoardChanged?.Invoke(currentBoardIndex);
    }
}