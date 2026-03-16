using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// In-game HUD: displays current score, high score, current tool name/icon.
/// In Arena mode: shows separate P1/P2 score and tool for each player.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Solo UI Elements")]
    [SerializeField] private GameObject _soloContainer;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _highScoreText;
    [SerializeField] private TextMeshProUGUI _toolNameText;
    [SerializeField] private Image _toolIcon;

    [Header("Arena UI Elements")]
    [SerializeField] private GameObject _arenaContainer;
    [SerializeField] private TextMeshProUGUI _p1ScoreText;
    [SerializeField] private TextMeshProUGUI _p2ScoreText;
    [SerializeField] private TextMeshProUGUI _p1ToolText;
    [SerializeField] private TextMeshProUGUI _p2ToolText;
    [SerializeField] private Image _p1ToolIcon;
    [SerializeField] private Image _p2ToolIcon;

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
        bool isArena = GameManager.IsArenaMode;

        if (_soloContainer != null) _soloContainer.SetActive(!isArena);
        if (_arenaContainer != null) _arenaContainer.SetActive(isArena);

        UpdateHighScore();
        if (isArena)
            RefreshArenaDisplay();
        else
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
        bool isArena = GameManager.IsArenaMode;

        if (isArena)
        {
            RefreshArenaDisplay();
        }
        else
        {
            if (_scoreText != null)
                _scoreText.text = $"SCORE: {score}";
        }

        UpdateHighScore();
    }

    private void RefreshArenaDisplay()
    {
        if (GameManager.Instance == null) return;

        if (_p1ScoreText != null) _p1ScoreText.text = $"P1: {GameManager.Instance.GetArenaScore(0)}";
        if (_p2ScoreText != null) _p2ScoreText.text = $"P2: {GameManager.Instance.GetArenaScore(1)}";

        ToolData p1Tool = GameManager.Instance.GetPlayerTool(0);
        ToolData p2Tool = GameManager.Instance.GetPlayerTool(1);
        if (_p1ToolText != null) _p1ToolText.text = p1Tool != null ? p1Tool.toolName : string.Empty;
        if (_p2ToolText != null) _p2ToolText.text = p2Tool != null ? p2Tool.toolName : string.Empty;
        if (_p1ToolIcon != null)
        {
            bool has1 = p1Tool != null && p1Tool.toolIcon != null;
            _p1ToolIcon.sprite = has1 ? p1Tool.toolIcon : null;
            _p1ToolIcon.enabled = has1;
        }
        if (_p2ToolIcon != null)
        {
            bool has2 = p2Tool != null && p2Tool.toolIcon != null;
            _p2ToolIcon.sprite = has2 ? p2Tool.toolIcon : null;
            _p2ToolIcon.enabled = has2;
        }
    }

    private void OnMilestoneReached(ToolData tool)
    {
        Debug.Log($"[GameHUD] OnMilestoneReached fired — tool: {(tool != null ? tool.toolName : "null")}");

        if (_milestoneText != null)
        {
            string toolName = tool != null ? tool.toolName : "???";
            _milestoneText.text = $"{toolName} UNLOCKED!";
            _milestoneText.gameObject.SetActive(true);
            _milestoneTimer = _milestoneDisplayDuration;
            Debug.Log($"[GameHUD] Milestone text shown: {toolName} UNLOCKED!");
        }
        else
        {
            Debug.LogWarning("[GameHUD] _milestoneText is null! Assign it in the Inspector.");
        }
    }

    private void OnToolEquipped(ToolData tool)
    {
        bool isArena = GameManager.IsArenaMode;

        if (isArena)
            RefreshArenaDisplay();
        else
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
