using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

// View component for individual board tiles
// Handles visual states: reveal, blink, highlight, and win animations
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
    
    public bool IsBlinking => blinkSequence != null && blinkSequence.IsActive() && blinkSequence.IsPlaying();
    
    private void Awake()
    {
        CacheReferences();
    }
    
    // Caches component & null check for references
    private void CacheReferences()
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
    public void TriggerReveal()
    {
        if (revealAnimation == null)
        {
         
            Debug.LogError($"[BoardTileView] {gameObject.name} has no reveal animation");
            
            return;
        }
        
        revealAnimation.DORewind();
        revealAnimation.DOPlay();
    }
    
    // Returns reveal animation duration for timing coordination
    public float GetRevealDuration()
    {    //                                                  Returns 0 if no animation assigned
        return revealAnimation != null ? revealAnimation.duration : 0f;
    }
    
    // Performs complete blink cycle
    public void Blink(float fadeInDuration, float holdDuration, float fadeOutDuration, Ease fadeInEase, Ease fadeOutEase)
    {
        if (blackOverlay == null) return;
        
        blinkSequence?.Kill();
        
        // Reset to original alpha before starting
        SetOverlayAlpha(originalAlpha);
        
        blinkSequence = DOTween.Sequence()
            .Append(blackOverlay.DOFade(0f, fadeInDuration).SetEase(fadeInEase))
            .AppendInterval(holdDuration)
            .Append(blackOverlay.DOFade(originalAlpha, fadeOutDuration).SetEase(fadeOutEase))
            .OnStart(() => OnBlinkStarted?.Invoke())
            .OnComplete(() => OnBlinkCompleted?.Invoke());
    }
    
    // Sets tile to permanent as highlighted state for winning tile
    public void SetHighlighted(float fadeDuration, Ease ease)
    {
        if (blackOverlay == null) return;
        
        KillActiveTweens();
        highlightTween = blackOverlay.DOFade(0f, fadeDuration).SetEase(ease);
    }
    
    // Triggers Win animation via Animator
    public void TriggerWin()
    {
        if (animator != null)
        {
            animator.SetTrigger(winTriggerHash);
        }
        
        OnWinTriggered?.Invoke();
    }
    
    // Instantly resets overlay to original alpha
    public void ResetToOriginal()
    {
        KillActiveTweens();
        SetOverlayAlpha(originalAlpha);
    }
    
    // Resets with fade animation for smoother transition
    public void ResetToOriginal(float fadeDuration, Ease ease)
    {
        KillActiveTweens();
        
        if (blackOverlay != null)
        {
            highlightTween = blackOverlay.DOFade(originalAlpha, fadeDuration).SetEase(ease);
        }
    }
    
    // Helper to set overlay alpha directly
    private void SetOverlayAlpha(float alpha)
    {
        if (blackOverlay == null) return;
        
        Color color = blackOverlay.color;
        color.a = alpha;
        blackOverlay.color = color;
    }
    
    // Kills all active tweens on this tile
    private void KillActiveTweens()
    {
        blinkSequence?.Kill();
        highlightTween?.Kill();
    }
    
    private void OnDestroy()
    {
        KillActiveTweens();
    }
}