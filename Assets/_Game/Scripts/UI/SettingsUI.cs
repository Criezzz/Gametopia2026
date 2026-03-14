using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Settings screen. Resolution dropdown, fullscreen toggle, volume sliders, keybind rebinding.
public class SettingsUI : MonoBehaviour
{
    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;
    [SerializeField] private Toggle _fullscreenToggle;

    [Header("Audio")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("Keybind Rebinding")]
    [SerializeField] private RebindManager _rebindManager;
    [SerializeField] private InputActionAsset _inputActions;

    [Header("Rebind UI - Player 1")]
    [SerializeField] private Button _rebindP1MoveLeft;
    [SerializeField] private Button _rebindP1MoveRight;
    [SerializeField] private Button _rebindP1Jump;
    [SerializeField] private Button _rebindP1Attack;
    [SerializeField] private TMP_Text _p1MoveLeftLabel;
    [SerializeField] private TMP_Text _p1MoveRightLabel;
    [SerializeField] private TMP_Text _p1JumpLabel;
    [SerializeField] private TMP_Text _p1AttackLabel;

    [Header("Rebind UI - Player 2")]
    [SerializeField] private Button _rebindP2MoveLeft;
    [SerializeField] private Button _rebindP2MoveRight;
    [SerializeField] private Button _rebindP2Jump;
    [SerializeField] private Button _rebindP2Attack;
    [SerializeField] private TMP_Text _p2MoveLeftLabel;
    [SerializeField] private TMP_Text _p2MoveRightLabel;
    [SerializeField] private TMP_Text _p2JumpLabel;
    [SerializeField] private TMP_Text _p2AttackLabel;

    [Header("Misc")]
    [SerializeField] private Button _resetDefaultsButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private TMP_Text _conflictWarning;

    [Header("Event Channels")]
    [SerializeField] private StringEventChannel _onLoadScene;

    private void Start()
    {
        SetupResolutionDropdown();
        SetupVolumeSliders();
        SetupRebindButtons();
        RefreshAllBindingLabels();

        if (_resetDefaultsButton != null)
            _resetDefaultsButton.onClick.AddListener(OnResetDefaults);
        if (_backButton != null)
            _backButton.onClick.AddListener(OnBackClicked);

        if (_conflictWarning != null)
            _conflictWarning.gameObject.SetActive(false);

        // Subscribe to rebind events
        if (_rebindManager != null)
        {
            _rebindManager.OnRebindCompleted += OnRebindDone;
            _rebindManager.OnRebindConflict += OnRebindConflict;
            _rebindManager.OnRebindStarted += OnRebindStarted;
        }
    }

    private void OnDestroy()
    {
        if (_rebindManager != null)
        {
            _rebindManager.OnRebindCompleted -= OnRebindDone;
            _rebindManager.OnRebindConflict -= OnRebindConflict;
            _rebindManager.OnRebindStarted -= OnRebindStarted;
        }
    }

    #region Volume

    private const string BGMVolumeKey = "BGMVolume";
    private const string SFXVolumeKey = "SFXVolume";

    private void SetupVolumeSliders()
    {
        if (_bgmSlider != null)
        {
            _bgmSlider.value = PlayerPrefs.GetFloat(BGMVolumeKey, 0.75f);
            ApplyVolume("BGMVolume", _bgmSlider.value);
            _bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.value = PlayerPrefs.GetFloat(SFXVolumeKey, 0.75f);
            ApplyVolume("SFXVolume", _sfxSlider.value);
            _sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void OnBGMVolumeChanged(float value)
    {
        ApplyVolume("BGMVolume", value);
        PlayerPrefs.SetFloat(BGMVolumeKey, value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        ApplyVolume("SFXVolume", value);
        PlayerPrefs.SetFloat(SFXVolumeKey, value);
    }

    private void ApplyVolume(string paramName, float linearValue)
    {
        if (_audioMixer == null) return;
        // Convert linear 0-1 to decibels (-80 to 0)
        float dB = linearValue > 0.0001f ? Mathf.Log10(linearValue) * 20f : -80f;
        _audioMixer.SetFloat(paramName, dB);
    }

    #endregion

    #region Resolution

    private void SetupResolutionDropdown()
    {
        if (_resolutionDropdown == null || ResolutionManager.Instance == null) return;

        _resolutionDropdown.ClearOptions();
        _resolutionDropdown.AddOptions(new List<string>(ResolutionManager.Instance.GetResolutionLabels()));
        _resolutionDropdown.value = ResolutionManager.Instance.CurrentIndex;
        _resolutionDropdown.RefreshShownValue();
        _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (_fullscreenToggle != null)
        {
            _fullscreenToggle.isOn = Screen.fullScreen;
            _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }
    }

    private void OnResolutionChanged(int index)
    {
        bool fs = _fullscreenToggle != null ? _fullscreenToggle.isOn : Screen.fullScreen;
        ResolutionManager.Instance?.ApplyResolution(index, fs);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        int idx = _resolutionDropdown != null ? _resolutionDropdown.value : 0;
        ResolutionManager.Instance?.ApplyResolution(idx, isFullscreen);
    }

    #endregion

    #region Keybind Rebinding

    private void SetupRebindButtons()
    {
        // P1 bindings (KeyboardLeft control scheme)
        WireRebindButton(_rebindP1MoveLeft, "Player", "Move", "KeyboardLeft", 3); // left composite part
        WireRebindButton(_rebindP1MoveRight, "Player", "Move", "KeyboardLeft", 4); // right composite part
        WireRebindButton(_rebindP1Jump, "Player", "Jump", "KeyboardLeft", 0);
        WireRebindButton(_rebindP1Attack, "Player", "Attack", "KeyboardLeft", 0);

        // P2 bindings (KeyboardRight control scheme)
        WireRebindButton(_rebindP2MoveLeft, "Player", "Move", "KeyboardRight", 8); // left composite part
        WireRebindButton(_rebindP2MoveRight, "Player", "Move", "KeyboardRight", 9); // right composite part
        WireRebindButton(_rebindP2Jump, "Player", "Jump", "KeyboardRight", 1);
        WireRebindButton(_rebindP2Attack, "Player", "Attack", "KeyboardRight", 1);
    }

    private void WireRebindButton(Button button, string mapName, string actionName, string scheme, int bindingIndex)
    {
        if (button == null || _inputActions == null || _rebindManager == null) return;

        var action = _inputActions.FindActionMap(mapName)?.FindAction(actionName);
        if (action == null) return;

        button.onClick.AddListener(() => _rebindManager.StartRebind(action, bindingIndex));
    }

    private void OnRebindStarted(InputAction action, int bindingIndex)
    {
        // Update the label to show "Press a key..."
        UpdateBindingLabel(action, bindingIndex, "...");
        if (_conflictWarning != null) _conflictWarning.gameObject.SetActive(false);
    }

    private void OnRebindDone(InputAction action, int bindingIndex)
    {
        RefreshAllBindingLabels();
    }

    private void OnRebindConflict(InputAction action, int bindingIndex, string conflictWith)
    {
        RefreshAllBindingLabels();
        if (_conflictWarning != null)
        {
            _conflictWarning.text = $"Key already used by {conflictWith}!";
            _conflictWarning.gameObject.SetActive(true);
        }
    }

    private void RefreshAllBindingLabels()
    {
        if (_inputActions == null) return;

        var playerMap = _inputActions.FindActionMap("Player");
        if (playerMap == null) return;

        var move = playerMap.FindAction("Move");
        var jump = playerMap.FindAction("Jump");
        var attack = playerMap.FindAction("Attack");

        // P1
        UpdateBindingLabel(move, 3, null, _p1MoveLeftLabel);
        UpdateBindingLabel(move, 4, null, _p1MoveRightLabel);
        UpdateBindingLabel(jump, 0, null, _p1JumpLabel);
        UpdateBindingLabel(attack, 0, null, _p1AttackLabel);

        // P2
        UpdateBindingLabel(move, 8, null, _p2MoveLeftLabel);
        UpdateBindingLabel(move, 9, null, _p2MoveRightLabel);
        UpdateBindingLabel(jump, 1, null, _p2JumpLabel);
        UpdateBindingLabel(attack, 1, null, _p2AttackLabel);
    }

    private void UpdateBindingLabel(InputAction action, int bindingIndex, string overrideText, TMP_Text label = null)
    {
        if (action == null || bindingIndex >= action.bindings.Count) return;

        string displayString = overrideText ?? InputControlPath.ToHumanReadableString(
            action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);

        if (label != null) label.text = displayString;
    }

    #endregion

    private void OnResetDefaults()
    {
        _rebindManager?.ResetToDefaults();
        RefreshAllBindingLabels();
        if (_conflictWarning != null) _conflictWarning.gameObject.SetActive(false);

        // Reset volume to defaults
        if (_bgmSlider != null) _bgmSlider.value = 0.75f;
        if (_sfxSlider != null) _sfxSlider.value = 0.75f;
    }

    private void OnBackClicked()
    {
        if (MenuSlideController.Instance != null)
            MenuSlideController.Instance.ShowMainMenu();
        else if (_onLoadScene != null && _onLoadScene.HasListeners)
            _onLoadScene.Raise(SceneNames.MainMenu);
        else
            SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
