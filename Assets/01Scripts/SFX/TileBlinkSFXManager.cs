using UnityEngine;

// Manages audio playback for blink sequences and escalating pitch
public class TileBlinkSFXManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("Sound Configs")]
    [SerializeField] private BlinkSFXConfig blinkConfig;
    [SerializeField] private BlinkSFXConfig selectionConfig;
    
    private float currentPitch;
    
    public float CurrentPitch => currentPitch;
    
    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        ResetPitch();
    }
    
    // Plays all SFX in the layer at current pitch, then increments pitch for next one
    public void PlayBlinkSFX()
    {
        if (blinkConfig == null || audioSource == null) return;
        
        PlayLayers(blinkConfig, currentPitch);
        
        // Increment pitch for next blink
        currentPitch = Mathf.Min(currentPitch + blinkConfig.PitchIncrement, blinkConfig.MaxPitch);
    }
    
    // Plays all SFX in the layer at normal pitch
    // Used when winning tile is revealed
    public void PlaySelectionSFX()
    {
        if (selectionConfig == null || audioSource == null) return;
        
        PlayLayers(selectionConfig, 1f);

    }
    
    // Plays all SFX in the layer with PlayOneShot
    private void PlayLayers(BlinkSFXConfig config, float pitch)
    {
        if (config.SoundLayers == null) return;
        
        //sets pitch
        audioSource.pitch = pitch;
        //Plays all SFX in the layer
        foreach (var layer in config.SoundLayers)
        {
            if (layer.Clip != null)
            {
                audioSource.PlayOneShot(layer.Clip, layer.Volume);
            }
        }
    }
    
    // Resets pitch to starting value
    public void ResetPitch()
    {
        currentPitch = blinkConfig != null ? blinkConfig.StartingPitch : 1f;
        
    }
}