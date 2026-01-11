using UnityEngine;

public class TileBlinkSFXManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Sound Configs")]
    [SerializeField] private BlinkSFXConfig blinkConfig;
    [SerializeField] private BlinkSFXConfig selectionConfig;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private float currentPitch;
    
    private void Awake()
    {
        if (blinkConfig != null)
        {
            currentPitch = blinkConfig.StartingPitch;
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    
    // Plays all sound layers from blink config simultaneously at current pitch
    // Each layer has its own volume, pitch is shared across all layers
    public void PlayBlinkSFX()
    {
        if (blinkConfig == null || audioSource == null) return;
        
        PlayConfigLayers(blinkConfig, currentPitch);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TileBlinkSFXManager] Playing {blinkConfig.SoundLayers.Length} blink layers at pitch: {currentPitch:F2}");
        }
        
        currentPitch = Mathf.Min(currentPitch + blinkConfig.PitchIncrement, blinkConfig.MaxPitch);
    }
    
    // Plays all sound layers from selection config at normal pitch
    // Used when winning tile is revealed
    public void PlaySelectionSFX()
    {
        if (selectionConfig == null || audioSource == null) return;
        
        PlayConfigLayers(selectionConfig, 1f);
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TileBlinkSFXManager] Playing {selectionConfig.SoundLayers.Length} selection layers");
        }
    }
    
    // Iterates through all layers in config and plays them via PlayOneShot
    // Sets pitch once on AudioSource, then fires all clips with individual volumes
    private void PlayConfigLayers(BlinkSFXConfig config, float pitch)
    {
        if (config.SoundLayers == null) return;
        
        audioSource.pitch = pitch;
        
        foreach (var layer in config.SoundLayers)
        {
            if (layer.Clip != null)
            {
                audioSource.PlayOneShot(layer.Clip, layer.Volume);
            }
        }
    }
    
    // Resets pitch back to starting value for next sequence
    // Call this when blink sequence ends or before starting new one
    public void ResetPitch()
    {
        if (blinkConfig != null)
        {
            currentPitch = blinkConfig.StartingPitch;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[TileBlinkSFXManager] Pitch reset to: {currentPitch:F2}");
        }
    }
    
    public float CurrentPitch => currentPitch;
}