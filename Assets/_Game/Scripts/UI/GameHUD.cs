using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// In-game HUD: displays current score, high score, current tool name/icon.
/// Listens to event channels for updates.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private TextMeshProUGUI _toolNameText;
    [SerializeField] private Image _toolIcon;
    [Header("Event Channels")]
    [SerializeField] private IntEventChannel _onScoreChanged;
    [SerializeField] private ToolDataEventChannel _onMilestoneReached;
    [SerializeField] private ToolDataEventChannel _onToolEquipped;

    [Header("Milestone Flash")]
    [SerializeField] private TextMeshProUGUI _milestoneText;
    [SerializeField] private float _milestoneDisplayDuration = 2f;
    private float _milestoneTimer;

    private void OnEnable()
    {
        if (_onScoreChanged != null) _onScoreChanged.Register(OnScoreChanged);
        if (_onMilestoneReached != null) _onMilestoneReached.Register(OnMilestoneReached);
        if (_onToolEquipped != null) _onToolEquipped.Register(OnToolEquipped);
    }

    private void OnDisable()
    {
        if (_onScoreChanged != null) _onScoreChanged.Unregister(OnScoreChanged);
        if (_onMilestoneReached != null) _onMilestoneReached.Unregister(OnMilestoneReached);
        if (_onToolEquipped != null) _onToolEquipped.Unregister(OnToolEquipped);
    }

    private void Start()
    {
        UpdateHighScore();
        UpdateToolDisplay(null);
        if (_milestoneText != null)
            _milestoneText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Milestone flash timer
        if (_milestoneTimer > 0f)
        {
            _milestoneTimer -= Time.deltaTime;
            if (_milestoneTimer <= 0f && _milestoneText != null)
                _milestoneText.gameObject.SetActive(false);
        }
    }

    private void OnScoreChanged(int score)
    {
        if (_scoreText != null)
            _scoreText.text = $"SCORE: {score}";
    }

    private void OnMilestoneReached(ToolData tool)
    {
        if (_milestoneText != null)
        {
            string toolName = tool != null ? tool.toolName : "???";
            _milestoneText.text = $"{toolName} UNLOCKED!";
            _milestoneText.gameObject.SetActive(true);
            _milestoneTimer = _milestoneDisplayDuration;
        }
    }

    private void OnToolEquipped(ToolData tool)
    {
        UpdateToolDisplay(tool);
    }

    private void UpdateHighScore()
    {
        if (_highScoreText != null)
        {
            int highScore = SaveManager.Data.highScore;
            _highScoreText.text = $"BEST: {highScore}";
        }
    }

    private void UpdateToolDisplay(ToolData tool)
    {
        if (_toolNameText != null)
            _toolNameText.text = tool != null ? tool.toolName : string.Empty;

        if (_toolIcon != null)
        {
            bool hasIcon = tool != null && tool.toolIcon != null;
            _toolIcon.sprite = hasIcon ? tool.toolIcon : null;
            _toolIcon.enabled = hasIcon;
        }
    }
}
