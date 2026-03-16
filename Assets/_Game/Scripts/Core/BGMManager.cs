using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// Singleton BGM manager. Crossfades between tracks when scenes change.
/// Place on a GameObject in the first scene (e.g. MainMenu).
[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Audio Mixer")]
    [Tooltip("Assign the MainMixer (needed to apply saved volume at startup).")]
    [SerializeField] private AudioMixer _audioMixer;
    [Tooltip("Assign the BGM mixer group from the MainMixer.")]
    [SerializeField] private AudioMixerGroup _bgmMixerGroup;

    [Header("Music Tracks")]
    [SerializeField] private SceneBGM[] _sceneTracks;

    [Header("Crossfade")]
    [SerializeField] private float _crossfadeDuration = 1f;

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private bool _aIsPlaying = true;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _sourceA = GetComponent<AudioSource>();
        _sourceA.loop = true;
        _sourceA.playOnAwake = false;

        _sourceB = gameObject.AddComponent<AudioSource>();
        _sourceB.loop = true;
        _sourceB.playOnAwake = false;

        if (_bgmMixerGroup != null)
        {
            _sourceA.outputAudioMixerGroup = _bgmMixerGroup;
            _sourceB.outputAudioMixerGroup = _bgmMixerGroup;
        }

        // Apply saved volume at startup (before BGM plays) so BGM/SFX respect SaveData
        if (_audioMixer != null)
        {
            var data = SaveManager.Data;
            ApplyVolume("BGMVolume", data.bgmVolume);
            ApplyVolume("SFXVolume", data.sfxVolume);
        }
    }

    private void ApplyVolume(string paramName, float linearValue)
    {
        if (_audioMixer == null) return;
        float dB = linearValue > 0.0001f ? Mathf.Log10(linearValue) * 20f : -80f;
        _audioMixer.SetFloat(paramName, dB);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Play track for current scene immediately
        PlayTrackForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayTrackForScene(scene.name);
    }

    private void PlayTrackForScene(string sceneName)
    {
        AudioClip clip = GetClipForScene(sceneName);
        AudioSource active = _aIsPlaying ? _sourceA : _sourceB;

        // Don't restart if already playing the same clip
        if (active.clip == clip && active.isPlaying) return;

        CrossfadeTo(clip);
    }

    private AudioClip GetClipForScene(string sceneName)
    {
        if (_sceneTracks == null) return null;

        foreach (var entry in _sceneTracks)
        {
            if (entry.sceneName == sceneName)
                return entry.clip;
        }
        return null;
    }

    private void CrossfadeTo(AudioClip newClip)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(CrossfadeRoutine(newClip));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        AudioSource fadeOut = _aIsPlaying ? _sourceA : _sourceB;
        AudioSource fadeIn = _aIsPlaying ? _sourceB : _sourceA;

        if (newClip != null)
        {
            fadeIn.clip = newClip;
            fadeIn.volume = 0f;
            fadeIn.Play();
        }

        float elapsed = 0f;
        float startVolume = fadeOut.volume;

        while (elapsed < _crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / _crossfadeDuration;

            fadeOut.volume = Mathf.Lerp(startVolume, 0f, t);
            if (newClip != null)
                fadeIn.volume = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;

        if (newClip != null)
            fadeIn.volume = 1f;

        _aIsPlaying = !_aIsPlaying;
        _fadeCoroutine = null;
    }

    [System.Serializable]
    public class SceneBGM
    {
        public string sceneName;
        public AudioClip clip;
    }
}
