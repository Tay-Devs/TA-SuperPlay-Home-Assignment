using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using DG.Tweening;
using UnityEngine.Events;
using VInspector;
using Random = UnityEngine.Random;

public class BoardData : MonoBehaviour
{
    [Header("Board Tiles")]
    [SerializeField] private List<BoardTileView> tiles = new List<BoardTileView>();
    
    [Header("Rigged Outcome")]
    [SerializeField] private int riggedWinnerIndex;
    
    [Header("Upgrade")]
    [SerializeField] private PlayableDirector upgradeTimeline;
    
    [Foldout("Entrance Effect - Celebration Blink")]
    [Tooltip("Delay between each tile reveal")]
    [SerializeField] private float delayBetweenReveals = 0.1f;
    
    [Tooltip("Fallback duration if no celebration clip assigned")]
    [SerializeField] private float celebrationBlinkDuration = 2f;

    [Tooltip("Minimum interval between blink bursts")]
    [SerializeField] private float celebrationMinInterval = 0.1f;

    [Tooltip("Maximum interval between blink bursts")]
    [SerializeField] private float celebrationMaxInterval = 0.2f;

    [Tooltip("Minimum tiles to blink at once")]
    [SerializeField] private int minSimultaneousBlinks = 1;

    [Tooltip("Maximum tiles to blink at once")]
    [SerializeField] private int maxSimultaneousBlinks = 2;

    [SerializeField] private float celebrationFadeInDuration = 0.03f;
    
    [SerializeField] private float celebrationHoldDuration = 0.05f;
    
    [SerializeField] private float celebrationFadeOutDuration = 0.08f;
    private Ease celebrationFadeInEase = Ease.OutQuad;
    private Ease celebrationFadeOutEase = Ease.InQuad;
    [EndFoldout]
    
    [Foldout("Celebration Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private BlinkSFXConfig celebrationSFXConfig;
    [EndFoldout]
   
    [SerializeField] private UnityEvent onCelebrationFinished;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Events
    public event Action OnEntranceStarted;
    public event Action OnAllTilesRevealed;
    public event Action OnCelebrationStarted;
    public event Action OnEntranceCompleted;
    
    private Coroutine entranceCoroutine;
    private bool isEntranceRunning;
    private float entranceElapsedTime;
    
    public IReadOnlyList<BoardTileView> Tiles => tiles;
    public int RiggedWinnerIndex => riggedWinnerIndex;
    public PlayableDirector UpgradeTimeline => upgradeTimeline;
    public bool HasUpgrade => upgradeTimeline != null;
    public bool IsEntranceRunning => isEntranceRunning;
    
    private void OnEnable()
    {
        StartEntranceEffect();
    }
    
    private void OnDisable()
    {
        StopEntranceEffect();
    }
    
    // Starts the entrance effect sequence
    // Called automatically on enable, reveals tiles then celebrates
    public void StartEntranceEffect()
    {
        if (isEntranceRunning) return;
        
        entranceCoroutine = StartCoroutine(EntranceEffectRoutine());
    }
    
    // Stops the entrance effect if running
    // Resets all tiles to original state
    public void StopEntranceEffect()
    {
        if (entranceCoroutine != null)
        {
            StopCoroutine(entranceCoroutine);
            entranceCoroutine = null;
        }
        
        isEntranceRunning = false;
        ResetAllTiles();
    }
    
    // Main entrance coroutine: plays music, reveals tiles, then celebrates
    // Music starts here and celebration ends when music finishes
    private IEnumerator EntranceEffectRoutine()
    {
        isEntranceRunning = true;
        entranceElapsedTime = 0f;
    
        yield return null;
    
        OnEntranceStarted?.Invoke();
        
        // Play celebration music at the start of entrance
        PlayCelebrationSFX();
        
        if (enableDebugLogs)
        {
            float musicDuration = GetMusicDuration();
            Debug.Log($"[BoardData] {gameObject.name} entrance started, music duration: {musicDuration:F2}s");
        }
    
        List<int> revealOrder = CreateRandomOrder(tiles.Count);
        float longestRevealDuration = 0f;
    
        for (int i = 0; i < revealOrder.Count; i++)
        {
            int index = revealOrder[i];
            BoardTileView tile = tiles[index];
        
            if (tile == null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[BoardData] Tile at index {index} is null, skipping");
                }
                continue;
            }
        
            float duration = tile.GetRevealDuration();
            if (duration > longestRevealDuration)
            {
                longestRevealDuration = duration;
            }
        
            tile.TriggerReveal();
        
            if (enableDebugLogs)
            {
                Debug.Log($"[BoardData] Revealing tile {i + 1}/{revealOrder.Count} (index {index}): {tile.gameObject.name}");
            }
        
            yield return new WaitForSeconds(delayBetweenReveals);
            entranceElapsedTime += delayBetweenReveals;
        }
    
        yield return new WaitForSeconds(longestRevealDuration);
        entranceElapsedTime += longestRevealDuration;
    
        OnAllTilesRevealed?.Invoke();
    
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardData] All {tiles.Count} tiles revealed in {entranceElapsedTime:F2}s, starting celebration");
        }
    
        yield return StartCoroutine(CelebrationBlinkRoutine());
    
        isEntranceRunning = false;
    
        OnEntranceCompleted?.Invoke();
    
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardData] {gameObject.name} entrance effect completed");
        }
    }
    
    // Celebration blink coroutine: blinks tiles until music ends
    // Duration is remaining music time after entrance phase
    private IEnumerator CelebrationBlinkRoutine()
    {
        OnCelebrationStarted?.Invoke();
        
        // Calculate remaining time from music duration minus entrance time
        float remainingDuration = GetRemainingCelebrationDuration();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardData] Celebration started, remaining duration: {remainingDuration:F2}s");
        }
    
        float elapsedTime = 0f;
        HashSet<int> lastBlinkedIndices = new HashSet<int>();
    
        while (elapsedTime < remainingDuration)
        {
            int blinkCount = Random.Range(minSimultaneousBlinks, maxSimultaneousBlinks + 1);
            blinkCount = Mathf.Min(blinkCount, tiles.Count);
        
            List<int> indicesToBlink = GetRandomIndices(blinkCount, lastBlinkedIndices);
        
            lastBlinkedIndices.Clear();
            foreach (int index in indicesToBlink)
            {
                tiles[index].Blink(
                    celebrationFadeInDuration,
                    celebrationHoldDuration,
                    celebrationFadeOutDuration,
                    celebrationFadeInEase,
                    celebrationFadeOutEase
                );
                lastBlinkedIndices.Add(index);
            
                if (enableDebugLogs)
                {
                    Debug.Log($"[BoardData] Celebration blink on tile {index}");
                }
            }
        
            float interval = Random.Range(celebrationMinInterval, celebrationMaxInterval);
            yield return new WaitForSeconds(interval);
            elapsedTime += interval;
        }
    
        float totalBlinkDuration = celebrationFadeInDuration + celebrationHoldDuration + celebrationFadeOutDuration;
        yield return new WaitForSeconds(totalBlinkDuration);
    
        ResetAllTiles();
        onCelebrationFinished?.Invoke();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardData] Celebration blink completed");
        }
    }
    
    // Returns the total music duration from the longest clip in config
    // Falls back to entrance time + celebrationBlinkDuration if no clip
    private float GetMusicDuration()
    {
        if (celebrationSFXConfig == null || celebrationSFXConfig.SoundLayers == null)
        {
            return 0f;
        }
        
        float maxClipDuration = 0f;
        
        foreach (var layer in celebrationSFXConfig.SoundLayers)
        {
            if (layer.Clip != null && layer.Clip.length > maxClipDuration)
            {
                maxClipDuration = layer.Clip.length;
            }
        }
        
        return maxClipDuration;
    }
    
    // Calculates remaining celebration time based on music duration minus entrance elapsed time
    // Falls back to celebrationBlinkDuration if no clip or if music already ended
    private float GetRemainingCelebrationDuration()
    {
        float musicDuration = GetMusicDuration();
        
        if (musicDuration <= 0f)
        {
            return celebrationBlinkDuration;
        }
        
        float remaining = musicDuration - entranceElapsedTime;
        
        // Ensure at least a small celebration even if entrance took longer than music
        return Mathf.Max(remaining, 0.5f);
    }
    
    // Plays all sound layers from celebration config once
    // Called at start of entrance so music spans both phases
    private void PlayCelebrationSFX()
    {
        if (audioSource == null || celebrationSFXConfig == null || celebrationSFXConfig.SoundLayers == null)
        {
            return;
        }
        
        foreach (var layer in celebrationSFXConfig.SoundLayers)
        {
            if (layer.Clip != null)
            {
                audioSource.PlayOneShot(layer.Clip, layer.Volume);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardData] Playing celebration music with {celebrationSFXConfig.SoundLayers.Length} layers");
        }
    }
    
    private List<int> GetRandomIndices(int count, HashSet<int> excludeIndices)
    {
        List<int> result = new List<int>(count);
        List<int> availableIndices = new List<int>();
    
        // Build list of available indices (prefer ones not in exclude set)
        for (int i = 0; i < tiles.Count; i++)
        {
            if (!excludeIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }
    
        // If not enough available, add back excluded ones
        if (availableIndices.Count < count)
        {
            availableIndices.Clear();
            for (int i = 0; i < tiles.Count; i++)
            {
                availableIndices.Add(i);
            }
        }
    
        // Pick random unique indices
        for (int i = 0; i < count && availableIndices.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableIndices.Count);
            result.Add(availableIndices[randomIndex]);
            availableIndices.RemoveAt(randomIndex);
        }
    
        return result;
    }

    // Creates a list of indices 0 to count-1 in random order
    private List<int> CreateRandomOrder(int count)
    {
        List<int> order = new List<int>(count);
        
        for (int i = 0; i < count; i++)
        {
            order.Add(i);
        }
        
        // Shuffling in random order
        for (int i = count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (order[i], order[randomIndex]) = (order[randomIndex], order[i]);
        }
        
        return order;
    }
    
    // Resets all tiles' black overlay to original state
    // Called after celebration ends
    private void ResetAllTiles()
    {
        foreach (var tile in tiles)
        {
            tile.ResetToOriginal();
        }
    }
    
    // Validates that rigged index is within bounds
    // Called by BoardManager before starting sequence
    public bool IsValid()
    {
        bool valid = tiles != null && tiles.Count > 0 && riggedWinnerIndex >= 0 && riggedWinnerIndex < tiles.Count;
        
        if (!valid && enableDebugLogs)
        {
            Debug.Log($"[BoardData] {gameObject.name} validation failed - Tiles: {tiles?.Count ?? 0}, RiggedIndex: {riggedWinnerIndex}");
        }
        
        return valid;
    }
    
    // Auto-populates tiles list from children with BoardTileView component
    [Button("Auto Find Tiles In Children")]
    private void AutoFindTiles()
    {
        tiles.Clear();
        tiles.AddRange(GetComponentsInChildren<BoardTileView>(true));
    }
}