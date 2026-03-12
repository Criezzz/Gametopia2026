using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// Game Over screen. Shows final score, high score, and restart button.
public class GameOverUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private GameObject _gameOverTitle;
    [SerializeField] private TextMeshProUGUI _finalScoreText;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private TextMeshProUGUI _newHighScoreLabel;

    [Header("Arena UI Elements")]
    [SerializeField] private GameObject _arenaScoreContainer;
    [SerializeField] private TextMeshProUGUI _p1ScoreText;
    [SerializeField] private TextMeshProUGUI _p2ScoreText;
    [SerializeField] private TextMeshProUGUI _winnerText;

    [Header("Buttons")]
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

        bool isArena = GameManager.Instance != null &&
                       GameManager.Instance.CurrentMode != null &&
                       GameManager.Instance.CurrentMode.modeType == GameModeType.Arena;

        // Toggle Solo vs Arena containers
        if (_gameOverTitle != null) _gameOverTitle.SetActive(!isArena);
        if (_finalScoreText != null) _finalScoreText.gameObject.SetActive(!isArena);
        if (_highScoreText != null) _highScoreText.gameObject.SetActive(!isArena);
        if (_newHighScoreLabel != null) _newHighScoreLabel.gameObject.SetActive(false); // Default to off
        if (_arenaScoreContainer != null) _arenaScoreContainer.SetActive(isArena);

        int highScore = SaveManager.Data.highScore;

        if (isArena)
        {
            int p1Score = GameManager.Instance.GetArenaScore(0);
            int p2Score = GameManager.Instance.GetArenaScore(1);

            if (_p1ScoreText != null) _p1ScoreText.text = $"P1 SCORE: {p1Score}";
            if (_p2ScoreText != null) _p2ScoreText.text = $"P2 SCORE: {p2Score}";

            if (_winnerText != null)
            {
                if (p1Score > p2Score)
                    _winnerText.text = "PLAYER 1 WINS!";
                else if (p2Score > p1Score)
                    _winnerText.text = "PLAYER 2 WINS!";
                else
                    _winnerText.text = "DRAW!";
            }
        }
        else
        {
            if (_finalScoreText != null)
                _finalScoreText.text = $"SCORE: {_latestScore}";

            if (_highScoreText != null)
                _highScoreText.text = $"BEST: {highScore}";

            if (_newHighScoreLabel != null)
                _newHighScoreLabel.gameObject.SetActive(_latestScore >= highScore && _latestScore > 0);
        }

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

        // Direct mouse click detection (bypasses EventSystem for reliability)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;

            if (_restartRT != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_restartRT, mousePos, null))
            {
                DoRestart();
                return;
            }

            if (_mainMenuRT != null &&
                RectTransformUtility.RectangleContainsScreenPoint(_mainMenuRT, mousePos, null))
            {
                DoMainMenu();
                return;
            }
        }
    }

    public void OnRestartClicked()
    {
        DoRestart();
    }

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

        string sceneToLoad = GameManager.Instance != null 
            ? GameManager.Instance.ActiveSceneName 
            : SceneNames.Game;
        SceneManager.LoadScene(sceneToLoad);
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
}
