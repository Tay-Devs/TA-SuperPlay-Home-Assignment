using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using VInspector;
using Random = UnityEngine.Random;

// Controls the entrance effect sequence & Celebration effect
public class BoardEntranceController : MonoBehaviour
{
    [Header("Target Board")]
    [SerializeField] private BoardModel targetBoard;
    
    [Foldout("Reveal Settings")]
    [SerializeField] private float delayBetweenReveals = 0.1f;
    [EndFoldout]
    
    [Foldout("Celebration Blink Settings")]
    [SerializeField] private float celebrationBlinkDuration = 2f;
    [SerializeField] private float minInterval = 0.1f;
    [SerializeField] private float maxInterval = 0.2f;
    [SerializeField] private int minSimultaneousBlinks = 1;
    [SerializeField] private int maxSimultaneousBlinks = 2;
    [SerializeField] private float fadeInDuration = 0.03f;
    [SerializeField] private float holdDuration = 0.05f;
    [SerializeField] private float fadeOutDuration = 0.08f;
    [SerializeField] private Ease fadeInEase = Ease.OutQuad;
    [SerializeField] private Ease fadeOutEase = Ease.InQuad;
    [EndFoldout]
    
    [Foldout("Celebration Audio")]
    [SerializeField] private BlinkSFXConfig celebrationSFXConfig;
    [EndFoldout]
    
    [Header("Events")]
    [SerializeField] private UnityEvent onCelebrationFinished;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    public event Action OnEntranceStarted;
    public event Action OnAllTilesRevealed;
    public event Action OnCelebrationStarted;
    public event Action OnEntranceCompleted;
    
    private Coroutine entranceCoroutine;
    private bool isRunning;
    private float entranceElapsedTime;
    
    public bool IsRunning => isRunning;
    
    private void OnEnable()
    {
        if (targetBoard != null)
        {
            StartEntranceEffect();
        }
    }
    
    private void OnDisable()
    {
        StopEntranceEffect();
    }
    
    // Sets the target board
    public void SetTargetBoard(BoardModel board, bool autoStart = true)
    {
        targetBoard = board;
        if (autoStart && board != null)
        {
            StartEntranceEffect();
        }
    }
    
    // Reveals tiles in random order then runs celebration blinks
    public void StartEntranceEffect()
    {
        if (isRunning || targetBoard == null) return;
        entranceCoroutine = StartCoroutine(EntranceRoutine());
    }
    
    // Stops entrance effect and resets all tiles
    public void StopEntranceEffect()
    {
        if (entranceCoroutine != null)
        {
            StopCoroutine(entranceCoroutine);
            entranceCoroutine = null;
        }
        
        isRunning = false;
        ResetAllTiles();
    }
    
    // Starts the entrance logic
    private IEnumerator EntranceRoutine()
    {
        isRunning = true;
        entranceElapsedTime = 0f;
        
        yield return null;
        
        OnEntranceStarted?.Invoke();
        // Starting the celebration music at the entrance 
        PlayCelebrationMusic();
        
        // Phase 1: Reveal tiles
        yield return RevealTilesPhase();
        
        OnAllTilesRevealed?.Invoke();
        
        // Phase 2: Celebration blinks
        yield return CelebrationPhase();
        
        isRunning = false;
        OnEntranceCompleted?.Invoke();
    }
    
    // Reveals tiles one by one in random order with delay between each
    private IEnumerator RevealTilesPhase()
    {
        var tiles = targetBoard.Tiles;
        List<int> revealOrder = CreateShuffledIndices(tiles.Count);
        float longestDuration = 0f;
        
        // Reveal the tiles in random order
        for (int i = 0; i < revealOrder.Count; i++)
        {
            int index = revealOrder[i];
            var tile = tiles[index];
            
            if (tile == null) continue;
            
            float duration = tile.GetRevealDuration();
            longestDuration = Mathf.Max(longestDuration, duration);
            
            tile.TriggerReveal();
            
            yield return new WaitForSeconds(delayBetweenReveals);
            entranceElapsedTime += delayBetweenReveals;
        }
        
        // Wait for final reveal animation to complete
        yield return new WaitForSeconds(longestDuration);
        entranceElapsedTime += longestDuration;
    }
    
    // Runs celebration blinks until music ends
    private IEnumerator CelebrationPhase()
    {
        OnCelebrationStarted?.Invoke();
        
        float remainingDuration = CalculateRemainingDuration();
        
        float elapsed = 0f;
        HashSet<int> lastBlinked = new HashSet<int>();
        var tiles = targetBoard.Tiles;
        
        // Blinks random tiles at random interval
        while (elapsed < remainingDuration)
        {
            int blinkCount = Random.Range(minSimultaneousBlinks, maxSimultaneousBlinks + 1);
            blinkCount = Mathf.Min(blinkCount, tiles.Count);
            
            List<int> indicesToBlink = GetRandomIndicesExcluding(tiles.Count, blinkCount, lastBlinked);
            
            lastBlinked.Clear();
            foreach (int index in indicesToBlink)
            {
                tiles[index].Blink(fadeInDuration, holdDuration, fadeOutDuration, fadeInEase, fadeOutEase);
                lastBlinked.Add(index);
            }
            
            float interval = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        
        // Wait for final blinks to complete
        float totalBlinkDuration = fadeInDuration + holdDuration + fadeOutDuration;
        yield return new WaitForSeconds(totalBlinkDuration);
        
        ResetAllTiles();
        onCelebrationFinished?.Invoke();
    }
    
    // Checks if the music is still playing to play the celebration animation
    private float CalculateRemainingDuration()
    {
        float musicDuration = GetMusicDuration();
        
        // Use default value if the music has ended
        if (musicDuration <= 0f)
        {
            return celebrationBlinkDuration;
        }
        
        float remaining = musicDuration - entranceElapsedTime;
        return Mathf.Max(remaining, 0.5f);
    }

    private float GetMusicDuration()
    {
        // Null check for music
        if (celebrationSFXConfig?.SoundLayers == null) return 0f;
        
        //Takes the time left from all sounds in the available layers
        float maxDuration = 0f;
        foreach (var layer in celebrationSFXConfig.SoundLayers)
        {
            if (layer.Clip != null)
            {
                maxDuration = Mathf.Max(maxDuration, layer.Clip.length);
            }
        }
        return maxDuration;
    }
    
    // Plays all celebration music layers
    private void PlayCelebrationMusic()
    {
        var audioSource = targetBoard.AudioSource;
        if (audioSource == null || celebrationSFXConfig?.SoundLayers == null) return;
        
        foreach (var layer in celebrationSFXConfig.SoundLayers)
        {
            if (layer.Clip != null)
            {
                audioSource.PlayOneShot(layer.Clip, layer.Volume);
            }
        }
    }
    
    // Creates shuffled list of indices from 0 to count-1
    // Uses Fisher-Yates shuffle for unbiased randomization
    private List<int> CreateShuffledIndices(int count)
    {
        List<int> indices = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            indices.Add(i);
        }
        
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        
        return indices;
    }
    
    // Gets random unique indices avoiding recently used ones
    private List<int> GetRandomIndicesExcluding(int totalCount, int pickCount, HashSet<int> exclude)
    {
        List<int> available = new List<int>();
        
        for (int i = 0; i < totalCount; i++)
        {
            if (!exclude.Contains(i))
            {
                available.Add(i);
            }
        }
        
        // If not enough available, use all indices
        if (available.Count < pickCount)
        {
            available.Clear();
            for (int i = 0; i < totalCount; i++)
            {
                available.Add(i);
            }
        }
        
        List<int> result = new List<int>(pickCount);
        for (int i = 0; i < pickCount && available.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, available.Count);
            result.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }
        
        return result;
    }
    
    // Resets all tiles to original overlay state
    private void ResetAllTiles()
    {
        if (targetBoard == null) return;
        
        foreach (var tile in targetBoard.Tiles)
        {
            tile?.ResetToOriginal();
        }
    }
}