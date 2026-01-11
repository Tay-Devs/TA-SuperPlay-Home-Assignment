using UnityEngine;
using DG.Tweening;

// ScriptableObject configuration for blink sequence timing
[CreateAssetMenu(fileName = "TileBlinkConfig", menuName = "Game/Tile Blink Config")]
public class TileBlinkConfig : ScriptableObject
{
    [Header("Sequence Timing")]
    [Tooltip("Total duration of the blinking sequence in seconds")]
    [Range(1f, 10f)] public float totalDuration = 3f;
    
    [Tooltip("Starting interval between blinks (fast)")]
    [Range(0.05f, 0.3f)] public float startInterval = 0.08f;
    
    [Tooltip("Ending interval between blinks (slow)")]
    [Range(0.3f, 1f)] public float endInterval = 0.5f;
    
    [Tooltip("Delay before revealing the final selected tile")]
    [Range(0f, 2f)] public float finalRevealDelay = 0.75f;
    
    [Header("Blink Fade Timing")]
    [Tooltip("Duration of fade in (overlay to transparent)")]
    [Range(0.01f, 0.5f)] public float fadeInDuration = 0.05f;
    
    [Tooltip("Duration of fade out (transparent back to original)")]
    [Range(0.01f, 0.5f)] public float fadeOutDuration = 0.08f;
    
    [Tooltip("How long the tile stays highlighted before fading back")]
    [Range(0f, 0.5f)] public float holdDuration = 0.1f;
    
    [Header("Easing")]
    [Tooltip("Ease type for fade in")]
    public Ease fadeInEase = Ease.OutQuad;
    
    [Tooltip("Ease type for fade out")]
    public Ease fadeOutEase = Ease.InQuad;
    
    [Tooltip("Controls how quickly the blinking slows down (higher = slower start)")]
    [Range(1f, 5f)] public float easingPower = 2f;
    
    // Used to know when last blink completes before reveal
    public float TotalBlinkDuration => fadeInDuration + holdDuration + fadeOutDuration;
}