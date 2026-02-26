using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Game Over screen. Shows final score, high score, and restart button.
/// Listens to OnGameOver event to activate.
/// Uses direct mouse/touch detection as primary input (bypasses EventSystem
/// entirely for maximum reliability).
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private TextMeshProUGUI _newHighScoreLabel;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _onGameOver;
    [SerializeField] private IntEventChannel _onScoreChanged;
    [SerializeField] private VoidEventChannel _onGameRestart;
    [SerializeField] private StringEventChannel _onLoadScene;

    private CanvasGroup _canvasGroup;
    private int _latestScore;
    private bool _isVisible;
    private bool _isTransitioning;

    // Cached RectTransforms for direct click detection
    private RectTransform _restartRT;
    private RectTransform _mainMenuRT;

    private void Awake()
    {
        if (_panel == null)
            _panel = gameObject;

        _canvasGroup = _panel.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = _panel.AddComponent<CanvasGroup>();

        // Cache RectTransforms for manual click detection
        if (_restartButton != null)
            _restartRT = _restartButton.GetComponent<RectTransform>();
        if (_mainMenuButton != null)
            _mainMenuRT = _mainMenuButton.GetComponent<RectTransform>();

        // Wire button listeners as secondary mechanism (in case EventSystem works)
        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestartClicked);
        if (_mainMenuButton != null)
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        SetPanelVisible(false);
    }

    private void OnEnable()
    {
        if (_onGameOver != null) _onGameOver.Register(ShowGameOver);
        if (_onScoreChanged != null) _onScoreChanged.Register(OnScoreChanged);
    }

    private void OnDisable()
    {
        if (_onGameOver != null) _onGameOver.Unregister(ShowGameOver);
        if (_onScoreChanged != null) _onScoreChanged.Unregister(OnScoreChanged);
    }

    private void ShowGameOver()
    {
        Debug.Log("[GameOverUI] ShowGameOver called!");
        _isVisible = true;
        _isTransitioning = false;
        SetPanelVisible(true);

        Time.timeScale = 0f;

        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        if (_finalScoreText != null)
            _finalScoreText.text = $"SCORE: {_latestScore}";

        if (_highScoreText != null)
            _highScoreText.text = $"BEST: {highScore}";

        if (_newHighScoreLabel != null)
            _newHighScoreLabel.gameObject.SetActive(_latestScore >= highScore && _latestScore > 0);

        // Force EventSystem to recognize the restart button (secondary mechanism)
        if (_restartButton != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(_restartButton.gameObject);
        }
    }

    private void OnScoreChanged(int score)
    {
        _latestScore = score;
    }

    private void Update()
    {
        if (!_isVisible || _isTransitioning) return;

        // ===== PRIMARY: Direct mouse click detection (bypasses EventSystem) =====
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;

            // Screen Space Overlay canvas: camera parameter = null
            if (_restartRT != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_restartRT, mousePos, null))
            {
                Debug.Log("[GameOverUI] Direct click detected on RESTART.");
                DoRestart();
                return;
            }

            if (_mainMenuRT != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_mainMenuRT, mousePos, null))
            {
                Debug.Log("[GameOverUI] Direct click detected on MAIN MENU.");
                DoMainMenu();
                return;
            }
        }

        // ===== SECONDARY: Keyboard shortcuts =====
        if (Input.GetKeyDown(KeyCode.R))
        {
            DoRestart();
        }
        else if (Input.GetKeyDown(KeyCode.M))
        {
            DoMainMenu();
        }
    }

    /// <summary>
    /// Called by Button.onClick (persistent or runtime). Kept as fallback.
    /// </summary>
    public void OnRestartClicked()
    {
        DoRestart();
    }

    /// <summary>
    /// Called by Button.onClick (persistent or runtime). Kept as fallback.
    /// </summary>
    public void OnMainMenuClicked()
    {
        DoMainMenu();
    }

    private void DoRestart()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        _isVisible = false;

        Debug.Log("[GameOverUI] DoRestart — loading Game scene.");
        SetPanelVisible(false);
        Time.timeScale = 1f;

        if (_onGameRestart != null && _onGameRestart.HasListeners)
            _onGameRestart.Raise();

        SceneManager.LoadScene("Game");
    }

    private void DoMainMenu()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        _isVisible = false;

        Debug.Log("[GameOverUI] DoMainMenu — loading MainMenu scene.");
        SetPanelVisible(false);
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
