using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Carousel-style map picker. Left/Right arrows cycle maps with a fade transition.
public class MapPickerUI : MonoBehaviour
{
    [Header("Map Data")]
    [SerializeField] private MapData[] _maps;

    [Header("UI References")]
    [SerializeField] private Image _mapPreviewImage;
    [SerializeField] private CanvasGroup _previewCanvasGroup;
    [SerializeField] private TextMeshProUGUI _mapNameText;
    [SerializeField] private GameObject _lockedOverlay;

    [Header("Buttons")]
    [SerializeField] private Button _leftArrow;
    [SerializeField] private Button _rightArrow;
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _backButton;

    [Header("Transition")]
    [SerializeField] private float _fadeDuration = 0.25f;

    [Header("Event Channels")]
    [SerializeField] private StringEventChannel _onLoadScene;

    private int _currentIndex;
    private bool _isTransitioning;

    private void Start()
    {
        if (_leftArrow != null) _leftArrow.onClick.AddListener(OnLeftClicked);
        if (_rightArrow != null) _rightArrow.onClick.AddListener(OnRightClicked);
        if (_startButton != null) _startButton.onClick.AddListener(OnStartClicked);
        if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);

        _currentIndex = 0;
        ShowMap(_currentIndex, false);
    }

    private void OnLeftClicked()
    {
        if (_isTransitioning || _maps == null || _maps.Length <= 1) return;
        int newIndex = (_currentIndex - 1 + _maps.Length) % _maps.Length;
        StartCoroutine(TransitionToMap(newIndex));
    }

    private void OnRightClicked()
    {
        if (_isTransitioning || _maps == null || _maps.Length <= 1) return;
        int newIndex = (_currentIndex + 1) % _maps.Length;
        StartCoroutine(TransitionToMap(newIndex));
    }

    private IEnumerator TransitionToMap(int newIndex)
    {
        _isTransitioning = true;

        // Fade out
        yield return FadePreview(1f, 0f);

        // Swap content
        _currentIndex = newIndex;
        ShowMap(_currentIndex, false);

        // Fade in
        yield return FadePreview(0f, 1f);

        _isTransitioning = false;
    }

    private IEnumerator FadePreview(float from, float to)
    {
        if (_previewCanvasGroup == null) yield break;

        float elapsed = 0f;
        _previewCanvasGroup.alpha = from;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _previewCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / _fadeDuration);
            yield return null;
        }

        _previewCanvasGroup.alpha = to;
    }

    private void ShowMap(int index, bool animated)
    {
        if (_maps == null || _maps.Length == 0) return;

        MapData map = _maps[index];
        bool isLocked = !map.IsUnlocked(SaveManager.Data.highScore);

        if (_mapPreviewImage != null && map.previewSprite != null)
            _mapPreviewImage.sprite = map.previewSprite;

        if (_mapNameText != null)
            _mapNameText.text = map.mapName;

        if (_lockedOverlay != null)
            _lockedOverlay.SetActive(isLocked);

        if (_startButton != null)
            _startButton.interactable = !isLocked;
    }

    private void OnStartClicked()
    {
        if (_maps == null || _maps.Length == 0) return;

        MapData map = _maps[_currentIndex];
        bool isLocked = !map.IsUnlocked(SaveManager.Data.highScore);
        if (isLocked) return;

        if (GameManager.PendingGameMode != null)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetGameMode(GameManager.PendingGameMode);

            string scene = !string.IsNullOrEmpty(map.sceneOverride)
                ? map.sceneOverride
                : GameManager.PendingGameMode.sceneName;

            if (string.IsNullOrEmpty(scene))
                scene = SceneNames.Game;

            LoadScene(scene);
        }
        else
        {
            Debug.LogWarning("[MapPickerUI] No PendingGameMode set!");
            LoadScene(SceneNames.MainMenu);
        }
    }

    private void OnBackClicked()
    {
        LoadScene(SceneNames.MainMenu);
    }

    private void LoadScene(string sceneName)
    {
        if (_onLoadScene != null && _onLoadScene.HasListeners)
            _onLoadScene.Raise(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
