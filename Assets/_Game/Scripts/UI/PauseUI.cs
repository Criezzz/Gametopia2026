using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Pause menu. Toggle with Pause action (default: Escape).
public class PauseUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Input")]
    [SerializeField] private InputActionReference _pauseAction;

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

        DisableNonInteractiveTextRaycasts();
        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        if (_pauseAction != null && _pauseAction.action != null)
        {
            _pauseAction.action.Enable();
            _pauseAction.action.performed += OnPausePerformed;
        }
    }

    private void OnDisable()
    {
        if (_pauseAction != null && _pauseAction.action != null)
            _pauseAction.action.performed -= OnPausePerformed;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        TogglePause();
    }

    public void TogglePause()
    {
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

        if (_onGameRestart != null && _onGameRestart.HasListeners)
            _onGameRestart.Raise();

        string sceneToLoad = GameManager.Instance != null 
            ? GameManager.Instance.ActiveSceneName 
            : SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneToLoad);
    }

    public void GoToMainMenu()
    {
        SetPanelVisible(false);
        _isPaused = false;
        Time.timeScale = 1f;

        if (_onLoadScene != null && _onLoadScene.HasListeners)
        {
            _onLoadScene.Raise(SceneNames.MainMenu);
        }
        else
        {
            SceneManager.LoadScene(SceneNames.MainMenu);
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

    private void DisableNonInteractiveTextRaycasts()
    {
        if (_panel == null) return;

        var texts = _panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                texts[i].raycastTarget = false;
        }
    }
}
