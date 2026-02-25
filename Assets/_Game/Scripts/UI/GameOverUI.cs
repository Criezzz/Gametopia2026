using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Game Over screen. Shows final score, high score, and restart button.
/// Listens to OnGameOver event to activate.
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

    private void Awake()
    {
        if (_panel == null)
            _panel = gameObject;

        _canvasGroup = _panel.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            Debug.LogWarning("[GameOverUI] CanvasGroup is missing on game over panel. Using SetActive fallback.");

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

    private void Start()
    {
        SetPanelVisible(false);
    }

    private void ShowGameOver()
    {
        SetPanelVisible(true);

        Time.timeScale = 0f; // Pause game

        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        if (_finalScoreText != null)
            _finalScoreText.text = $"SCORE: {_latestScore}";

        if (_highScoreText != null)
            _highScoreText.text = $"BEST: {highScore}";

        if (_newHighScoreLabel != null)
            _newHighScoreLabel.gameObject.SetActive(_latestScore >= highScore && _latestScore > 0);
    }

    private void OnScoreChanged(int score)
    {
        _latestScore = score;
    }

    public void OnRestartClicked()
    {
        SetPanelVisible(false);

        Time.timeScale = 1f;
        _onGameRestart?.Raise();
    }

    public void OnMainMenuClicked()
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
