using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pause menu. Toggle with Escape key.
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _onGamePaused;
    [SerializeField] private VoidEventChannel _onGameRestart;
    [SerializeField] private StringEventChannel _onLoadScene;

    private bool _isPaused;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (_panel == null)
            _panel = gameObject;

        _canvasGroup = _panel.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            Debug.LogWarning("[PauseUI] CanvasGroup is missing on pause panel. Using SetActive fallback.");

        SetPanelVisible(false);
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void Start()
    {
        SetPanelVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;

        SetPanelVisible(_isPaused);

        Time.timeScale = _isPaused ? 0f : 1f;
        _onGamePaused?.Raise();
    }

    public void OnRestartClicked()
    {
        Time.timeScale = 1f;
        _onGameRestart?.Raise();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        _onLoadScene?.Raise("MainMenu");
    }

    private void SetPanelVisible(bool visible)
    {
        if (_panel == null) return;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            _panel.SetActive(visible);
        }
    }
}
