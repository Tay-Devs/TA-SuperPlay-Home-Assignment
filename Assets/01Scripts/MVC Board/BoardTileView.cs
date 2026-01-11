using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public class BoardTileView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image blackOverlay;
    [SerializeField] private Animator animator;
    [SerializeField] private DOTweenAnimation revealAnimation;
    
    [Header("Animation")]
    [SerializeField] private string winTriggerName = "Win";
    
    [Header("Events")]
    public UnityEvent OnBlinkStarted;
    public UnityEvent OnBlinkCompleted;
    public UnityEvent OnWinTriggered;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private float originalAlpha;
    private Sequence blinkSequence;
    private Tween highlightTween;
    private int winTriggerHash;
    
    private void Awake()
    {
        if (blackOverlay != null)
        {
            originalAlpha = blackOverlay.color.a;
        }
        
        winTriggerHash = Animator.StringToHash(winTriggerName);
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (revealAnimation == null)
        {
            revealAnimation = GetComponent<DOTweenAnimation>();
        }
    }
    
    // Triggers the reveal DOTweenAnimation on this tile
    // Called by BoardData during entrance sequence in random order
    public void TriggerReveal()
    {
        if (revealAnimation == null)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BoardTileView] {gameObject.name} has no reveal animation assigned");
            }
            
            return;
        }
    
        // Ensure animation is ready and play from start
        revealAnimation.DORewind();
        revealAnimation.DOPlay();
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardTileView] {gameObject.name} reveal triggered");
        }
    }
    // Gets the duration of the reveal animation
    // Used by BoardData to know when reveal is complete
    public float GetRevealDuration()
    {
        if (revealAnimation != null)
        {
            return revealAnimation.duration;
        }
        return 0f;
    }
    
    // Performs a complete blink cycle: fade in -> hold -> fade out
    // Creates a DOTween Sequence that runs independently allowing overlapping blinks
    public void Blink(float fadeInDuration, float holdDuration, float fadeOutDuration, Ease fadeInEase, Ease fadeOutEase)
    {
        if (blackOverlay == null) return;
        
        blinkSequence?.Kill();
        
        Color color = blackOverlay.color;
        color.a = originalAlpha;
        blackOverlay.color = color;
        
        blinkSequence = DOTween.Sequence();
        blinkSequence.Append(blackOverlay.DOFade(0f, fadeInDuration).SetEase(fadeInEase));
        blinkSequence.AppendInterval(holdDuration);
        blinkSequence.Append(blackOverlay.DOFade(originalAlpha, fadeOutDuration).SetEase(fadeOutEase));
        blinkSequence.OnStart(() => OnBlinkStarted?.Invoke());
        blinkSequence.OnComplete(() => OnBlinkCompleted?.Invoke());
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardTileView] {gameObject.name} started blink cycle");
        }
    }
    
    // Sets tile to permanent highlighted state for final reveal
    // Kills any running blink and fades overlay to transparent
    public void SetHighlighted(float fadeDuration, Ease ease)
    {
        if (blackOverlay == null) return;
        
        blinkSequence?.Kill();
        highlightTween?.Kill();
        
        highlightTween = blackOverlay.DOFade(0f, fadeDuration).SetEase(ease);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BoardTileView] {gameObject.name} set to highlighted");
        }
    }
    
    // Triggers the Win animation on this tile's Animator
    // Called by controller when this tile is the final selected one
    public void TriggerWin()
    {
        if (animator != null)
        {
            animator.SetTrigger(winTriggerHash);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BoardTileView] {gameObject.name} Win trigger fired");
            }
        }
        
        OnWinTriggered?.Invoke();
    }
    
    // Instantly resets overlay to original alpha without animation
    // Used for cleanup or initialization
    public void ResetToOriginal()
    {
        blinkSequence?.Kill();
        highlightTween?.Kill();
        
        if (blackOverlay != null)
        {
            Color color = blackOverlay.color;
            color.a = originalAlpha;
            blackOverlay.color = color;
        }
    }
    
    // Resets with fade animation for smoother visual transition
    // Useful when stopping sequence mid-way
    public void ResetToOriginal(float fadeDuration, Ease ease)
    {
        blinkSequence?.Kill();
        highlightTween?.Kill();
        
        if (blackOverlay != null)
        {
            highlightTween = blackOverlay.DOFade(originalAlpha, fadeDuration).SetEase(ease);
        }
    }
    
    public bool IsBlinking => blinkSequence != null && blinkSequence.IsActive() && blinkSequence.IsPlaying();
    
    private void OnDestroy()
    {
        blinkSequence?.Kill();
        highlightTween?.Kill();
    }
}