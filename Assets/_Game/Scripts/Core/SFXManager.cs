using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton SFX manager with two AudioSources:
///   _audioSource  – global one-shots (pickup, milestone, etc.) – never interrupted.
///   _toolSource   – tool loop / attack SFX – stopped on tool switch via StopToolSFX().
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
    private AudioSource _toolSource;

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

        _toolSource = gameObject.AddComponent<AudioSource>();
        _toolSource.playOnAwake = false;

        if (_sfxMixerGroup != null)
        {
            _audioSource.outputAudioMixerGroup = _sfxMixerGroup;
            _toolSource.outputAudioMixerGroup = _sfxMixerGroup;
        }
    }

    public void Play(AudioClip clip)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(clip);
    }

    public void Play(AudioClip clip, float volume)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(clip, volume);
    }

    public void PlayToolSFX(AudioClip clip)
    {
        if (clip == null || _toolSource == null) return;
        _toolSource.PlayOneShot(clip);
    }

    public void PlayToolSFX(AudioClip clip, float volume)
    {
        if (clip == null || _toolSource == null) return;
        _toolSource.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Stop tool SFX only. Global one-shots (pickup, milestone) are unaffected.
    /// </summary>
    public void StopToolSFX()
    {
        if (_toolSource == null) return;
        _toolSource.Stop();
    }

    /// <summary>
    /// Stop everything (both sources). Use sparingly.
    /// </summary>
    public void StopAll()
    {
        if (_audioSource != null) _audioSource.Stop();
        if (_toolSource != null) _toolSource.Stop();
    }
}
