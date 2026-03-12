using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton SFX manager. Plays one-shot sound effects via a shared AudioSource.
/// Attach to a persistent GameObject in the scene alongside an AudioSource.
///
/// Usage:
///   SFXManager.Instance.Play(clip);
///   SFXManager.Instance.Play(clip, 0.8f);  // custom volume
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("Assign the SFX mixer group from the MainMixer.")]
    [SerializeField] private AudioMixerGroup _sfxMixerGroup;

    [Header("Global SFX (not tied to any specific entity)")]
    [Tooltip("Played when the player picks up a toolbox")]
    [SerializeField] private AudioClip _pickupSFX;
    [Tooltip("Played when a new tool milestone is unlocked")]
    [SerializeField] private AudioClip _milestoneSFX;

    public AudioClip PickupSFX => _pickupSFX;
    public AudioClip MilestoneSFX => _milestoneSFX;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _audioSource = GetComponent<AudioSource>();

        if (_sfxMixerGroup != null)
            _audioSource.outputAudioMixerGroup = _sfxMixerGroup;
    }

    /// <summary>
    /// Play a one-shot clip at default volume.
    /// </summary>
    public void Play(AudioClip clip)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Play a one-shot clip at a specific volume (0-1).
    /// </summary>
    public void Play(AudioClip clip, float volume)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(clip, volume);
    }
}
