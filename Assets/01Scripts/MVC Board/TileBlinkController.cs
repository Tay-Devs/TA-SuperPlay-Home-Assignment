using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class TileBlinkController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private TileBlinkConfig config;
    [SerializeField] private TileBlinkSFXManager sfxManager;
    
    public event Action OnBlinkSequenceStarted;
    public event Action<BoardTileView> OnTileSelected;
    public event Action OnBlinkSequenceCompleted;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private List<BoardTileView> currentTiles;
    private Coroutine blinkCoroutine;
    private bool isRunning;
    
    public bool IsRunning => isRunning;
    
    // Starts blink sequence with provided tiles and target index
    // Called by BoardManager with current board's data
    public void StartBlinkSequence(IReadOnlyList<BoardTileView> tiles, int targetTileIndex)
    {
        if (isRunning)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[TileBlinkController] Sequence already running, ignoring start request");
            }
            return;
        }
        
        if (tiles == null || tiles.Count == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[TileBlinkController] No tiles provided");
            }
            return;
        }
        
        if (targetTileIndex < 0 || targetTileIndex >= tiles.Count)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[TileBlinkController] Invalid target index: {targetTileIndex}");
            }
            return;
        }
        
        // Reset SFX pitch at the start of each new sequence
        if (sfxManager != null)
        {
            sfxManager.ResetPitch();
        }
        
        currentTiles = new List<BoardTileView>(tiles);
        blinkCoroutine = StartCoroutine(BlinkSequenceRoutine(targetTileIndex));
    }
    
    // Stops the sequence and resets all current tiles
    // Kills coroutine and clears tile visuals
    public void StopBlinkSequence()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        ResetAllTiles();
        isRunning = false;
        
        // Reset pitch when sequence is stopped early
        if (sfxManager != null)
        {
            sfxManager.ResetPitch();
        }
    }
    
    // Main sequence coroutine - fires blinks at interval rate without waiting
    // Each tile handles its own blink cycle independently allowing overlap
    private IEnumerator BlinkSequenceRoutine(int targetTileIndex)
    {
        isRunning = true;
        OnBlinkSequenceStarted?.Invoke();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TileBlinkController] Starting blink sequence, target index: {targetTileIndex}");
        }
        
        float elapsedTime = 0f;
        List<int> availableIndices = new List<int>();
        
        for (int i = 0; i < currentTiles.Count; i++)
        { 
            availableIndices.Add(i);
        }
        
        int lastRandomIndex = -1;
        
        while (elapsedTime < config.totalDuration)
        {
            float progress = elapsedTime / config.totalDuration;
            float currentInterval = CalculateInterval(progress);
            
            int randomIndex = GetRandomIndex(availableIndices, lastRandomIndex);
            lastRandomIndex = randomIndex;
            
            FireBlink(randomIndex);
            
            yield return new WaitForSeconds(currentInterval);
            elapsedTime += currentInterval;
        }
        
        yield return new WaitForSeconds(config.TotalBlinkDuration);
        
        ResetAllTiles();
        
        yield return new WaitForSeconds(config.finalRevealDelay);
        
        // Final reveal - play selection SFX
        currentTiles[targetTileIndex].SetHighlighted(config.fadeInDuration, config.fadeInEase);
        
        if (sfxManager != null)
        {
            sfxManager.PlaySelectionSFX();
        }
        
        isRunning = false;
        OnTileSelected?.Invoke(currentTiles[targetTileIndex]);
        OnBlinkSequenceCompleted?.Invoke();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TileBlinkController] Sequence completed, selected tile: {targetTileIndex}");
        }
    }
    
    // Calculates interval using ease-out curve for slot-machine effect
    // Returns lerped value between start and end intervals based on eased progress
    private float CalculateInterval(float progress)
    {
        float easedProgress = Mathf.Pow(progress, config.easingPower);
        return Mathf.Lerp(config.startInterval, config.endInterval, easedProgress);
    }
    
    // Gets random index avoiding the last picked one for visual variety
    // Loops until finding a different index than the excluded one
    private int GetRandomIndex(List<int> availableIndices, int excludeIndex)
    {
        if (availableIndices.Count == 0) return 0;
        if (availableIndices.Count == 1) return availableIndices[0];
        
        int randomIndex;
        do
        {
            randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        } while (randomIndex == excludeIndex && availableIndices.Count > 1);
        
        return randomIndex;
    }
    
    // Triggers a blink on the specified tile using config settings
    // Also plays blink SFX with escalating pitch via the SFX manager
    private void FireBlink(int index)
    {
        if (index >= 0 && index < currentTiles.Count)
        {
            currentTiles[index].Blink(
                config.fadeInDuration,
                config.holdDuration,
                config.fadeOutDuration,
                config.fadeInEase,
                config.fadeOutEase
            );
            
            // Play blink SFX with escalating pitch
            if (sfxManager != null)
            {
                sfxManager.PlayBlinkSFX();
            }
        }
    }
    
    // Instantly resets all current tiles to original state
    // Used before final reveal and when stopping sequence
    private void ResetAllTiles()
    {
        if (currentTiles == null) return;
        
        foreach (var tile in currentTiles)
        {
            if (tile != null)
            {
                tile.ResetToOriginal();
            }
        }
    }
}