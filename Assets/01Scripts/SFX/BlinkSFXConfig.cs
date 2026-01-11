using UnityEngine;

[CreateAssetMenu(fileName = "BlinkSFXConfig", menuName = "Audio/Blink SFX Config")]
public class BlinkSFXConfig : ScriptableObject
{
    [Header("Sound Layers")]
    [SerializeField] private SoundLayer[] soundLayers;
    
    [Header("Pitch Settings")]
    [SerializeField] private float startingPitch = 1f;
    [SerializeField] private float maxPitch = 2f;
    [SerializeField] private float pitchIncrement = 0.05f;
    
    public SoundLayer[] SoundLayers => soundLayers;
    public float StartingPitch => startingPitch;
    public float MaxPitch => maxPitch;
    public float PitchIncrement => pitchIncrement;
}

[System.Serializable]
public class SoundLayer
{
    [SerializeField] private string layerName;
    [SerializeField] private AudioClip clip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    
    public string LayerName => layerName;
    public AudioClip Clip => clip;
    public float Volume => volume;
}