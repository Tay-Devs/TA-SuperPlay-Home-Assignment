using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Controller for the blink sequence
// Fires rapid blinks that slow down over time, then reveals winner

public class TileBlinkController : MonoBehaviour
{
    [Header("Configuration")]
    // Uses properties for all timing parameters
    [SerializeField] private TileBlinkConfig config;
    [SerializeField] private TileBlinkSFXManager sfxManager;
    
    // Events for sequence lifecycle
    public event Action OnBlinkSequenceStarted;
    public event Action<BoardTileView> OnTileSelected;
    public event Action OnBlinkSequenceCompleted;
    
    private List<BoardTileView> currentTiles;
    private Coroutine blinkCoroutine;
    private bool isRunning;
    
    public bool IsRunning => isRunning;
    
    // Starts blink sequence with provided tiles and target index
    public void StartBlinkSequence(IReadOnlyList<BoardTileView> tiles, int targetTileIndex)
    {
        //Resets pitch
        sfxManager?.ResetPitch();
        currentTiles = new List<BoardTileView>(tiles);
        blinkCoroutine = StartCoroutine(BlinkSequenceRoutine(targetTileIndex));
    }
    
    
    // Stops sequence and resets all tiles
    public void StopBlinkSequence()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        
        ResetAllTiles();
        isRunning = false;
        sfxManager?.ResetPitch();
    }
    
    // Main sequence routine - fires blinks with decreasing frequency
    private IEnumerator BlinkSequenceRoutine(int targetTileIndex)
    {
        isRunning = true;
        OnBlinkSequenceStarted?.Invoke();
        int lastIndex = -1;
        float elapsed = 0f;
        
        // Blink phase - rapid blinks that slow down
        while (elapsed < config.totalDuration)
        {
            float progress = elapsed / config.totalDuration;
            float interval = CalculateInterval(progress);
            
            int blinkIndex = GetRandomIndexExcluding(currentTiles.Count, lastIndex);
            lastIndex = blinkIndex;
            
            FireBlink(blinkIndex);
            
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        
        ResetAllTiles();
        
        // Delay final reveal
        yield return new WaitForSeconds(config.finalRevealDelay);
        
        RevealWinner(targetTileIndex);
        
        isRunning = false;
        OnBlinkSequenceCompleted?.Invoke();
    }

    // Calculate the start speed and slows down near the end
    private float CalculateInterval(float progress)
    {
        float easedProgress = Mathf.Pow(progress, config.easingPower);
        return Mathf.Lerp(config.startInterval, config.endInterval, easedProgress);
    }
    
    // Gets random index different from excluded one
    private int GetRandomIndexExcluding(int count, int exclude)
    {
        if (count <= 1) return 0;
        
        int result;
        do
        {
            result = Random.Range(0, count);
        } while (result == exclude);
        
        return result;
    }
    
    // Triggers blink on specified tile with SFX
    private void FireBlink(int index)
    {
        if (index < 0 || index >= currentTiles.Count) return;
        
        currentTiles[index].Blink(
            config.fadeInDuration,
            config.holdDuration,
            config.fadeOutDuration,
            config.fadeInEase,
            config.fadeOutEase
        );
        
        sfxManager?.PlayBlinkSFX();
    }
    
    // Reveals winning tile with highlight and selection SFX
 
    private void RevealWinner(int index)
    {
        var winningTile = currentTiles[index];
        winningTile.SetHighlighted(config.fadeInDuration, config.fadeInEase);
        
        sfxManager?.PlaySelectionSFX();
        
        // Fires OnTileSelected event for external handling
        OnTileSelected?.Invoke(winningTile);
    }
    
    // Resets all tiles to original state
    private void ResetAllTiles()
    {
        if (currentTiles == null) return;
        
        foreach (var tile in currentTiles)
        {
            tile?.ResetToOriginal();
        }
    }
}