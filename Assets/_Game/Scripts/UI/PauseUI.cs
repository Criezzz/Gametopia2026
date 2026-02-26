using UnityEngine;
using UnityEngine.SceneManagement;
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
            _canvasGroup = _panel.AddComponent<CanvasGroup>();

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
        // Don't allow pausing during GameOver
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
            return;

        _isPaused = !_isPaused;

        SetPanelVisible(_isPaused);

        Time.timeScale = _isPaused ? 0f : 1f;
        _onGamePaused?.Raise();
    }

    public void OnResumeClicked()
    {
        if (!_isPaused) return;
        TogglePause();
    }

    public void OnRestartClicked()
    {
        SetPanelVisible(false);
        _isPaused = false;
        Time.timeScale = 1f;

        // Notify GameManager (optional cleanup), then always load directly
        if (_onGameRestart != null && _onGameRestart.HasListeners)
            _onGameRestart.Raise();

        SceneManager.LoadScene("Game");
    }

    public void GoToMainMenu()
    {
        SetPanelVisible(false);
        _isPaused = false;
        Time.timeScale = 1f;

        if (_onLoadScene != null && _onLoadScene.HasListeners)
        {
            _onLoadScene.Raise("MainMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
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
