using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game state enum for Tool Crate.
/// </summary>
public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}

/// <summary>
/// Singleton GameManager for Tool Crate.
/// Manages score, tool pool (weighted by ToolData.baseWeight), unlocks, and game state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Current State")]
    [SerializeField] private GameState _currentState = GameState.MainMenu;
    public GameState CurrentState => _currentState;

    [Header("Score")]
    [SerializeField] private int _score;
    public int Score => _score;
    public int HighScore { get; private set; }

    [Header("Debug")]
    [Tooltip("If true, unlocks all tools immediately ignoring the High Score requirement.")]
    [SerializeField] private bool _devMode = false;

    [Header("Tool Pool")]
    [SerializeField] private ToolData[] _allTools;
    [SerializeField] private ToolData _firstPickupTool;
    private readonly HashSet<ToolData> _unlockedTools = new();
    private ToolData _currentTool;
    private bool _hasAwardedFirstPickupTool;
    public ToolData CurrentTool => _currentTool;

    [Header("Toolbox")]
    [SerializeField] private Toolbox _toolboxPrefab;
    private Toolbox _toolboxInstance;

    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _onToolPickedUp;
    [SerializeField] private IntEventChannel _onScoreChanged;
    [SerializeField] private ToolDataEventChannel _onMilestoneReached;
    [SerializeField] private ToolDataEventChannel _onToolEquipped;
    [SerializeField] private VoidEventChannel _onPlayerDied;
    [SerializeField] private VoidEventChannel _onGameOver;
    [SerializeField] private VoidEventChannel _onGamePaused;
    [SerializeField] private VoidEventChannel _onGameRestart;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Load save data from JSON
        SaveManager.Load();
        HighScore = SaveManager.Data.highScore;

        // Migrate legacy PlayerPrefs if present
        int legacyScore = PlayerPrefs.GetInt("HighScore", 0);
        if (legacyScore > HighScore)
        {
            HighScore = legacyScore;
            SaveManager.Data.highScore = legacyScore;
            SaveManager.Save();
            PlayerPrefs.DeleteKey("HighScore");
            PlayerPrefs.Save();
            Debug.Log($"[GameManager] Migrated legacy PlayerPrefs highScore: {legacyScore}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnEnable()
    {
        if (_onToolPickedUp != null) _onToolPickedUp.Register(HandleToolPickedUp);
        if (_onPlayerDied != null) _onPlayerDied.Register(HandlePlayerDied);
        if (_onGamePaused != null) _onGamePaused.Register(HandleGamePaused);
        if (_onGameRestart != null) _onGameRestart.Register(HandleGameRestart);
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "Game" && _currentState != GameState.Playing)
            StartGame();
    }

    private void Update()
    {
        bool pressed1 = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
        bool pressed2 = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
        bool pressed3 = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
        bool pressed4 = Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4);
        bool pressed5 = Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5);
        bool pressed6 = Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6);
        bool pressed7 = Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7);
        bool pressed8 = Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8);

        if (!_devMode)
        {
            if (pressed1 || pressed2 || pressed3 || pressed4 || pressed5 || pressed6 || pressed7 || pressed8)
                Debug.LogWarning("[GameManager][DEV] Dev Mode is OFF. Enable it to use 1..8 tool hotkeys.");
            return;
        }

        if (pressed1) DevEquipFirstTool();
        if (pressed2) DevEquipToolByIndex(1);
        if (pressed3) DevEquipToolByIndex(2);
        if (pressed4) DevEquipToolByIndex(3);
        if (pressed5) DevEquipToolByIndex(4);
        if (pressed6) DevEquipToolByIndex(5);
        if (pressed7) DevEquipToolByIndex(6);
        if (pressed8) DevEquipToolByIndex(7);
    }

    private void OnDisable()
    {
        if (_onToolPickedUp != null) _onToolPickedUp.Unregister(HandleToolPickedUp);
        if (_onPlayerDied != null) _onPlayerDied.Unregister(HandlePlayerDied);
        if (_onGamePaused != null) _onGamePaused.Unregister(HandleGamePaused);
        if (_onGameRestart != null) _onGameRestart.Unregister(HandleGameRestart);
    }

    /// <summary>
    /// Initialize the run, unlock tools based on score, and start gameplay.
    /// </summary>
    public void StartGame()
    {
        _score = 0;
        _unlockedTools.Clear();
        _hasAwardedFirstPickupTool = false;

        UpdateUnlockedTools(false);

        _currentTool = null;
        SetState(GameState.Playing);
        Time.timeScale = 1f;

        // Spawn toolbox at a random platform position
        SpawnToolbox();

        _onScoreChanged?.Raise(_score);
        _onToolEquipped?.Raise(_currentTool);
    }

    private void SpawnToolbox()
    {
        if (_toolboxInstance != null)
            Destroy(_toolboxInstance.gameObject);

        if (_toolboxPrefab != null)
        {
            _toolboxInstance = Instantiate(_toolboxPrefab);
            Debug.Log("[GameManager] Toolbox spawned at runtime.");
        }
        else
        {
            Debug.LogWarning("[GameManager] _toolboxPrefab is not assigned! Toolbox will not spawn.");
        }
    }

    private void UpdateUnlockedTools(bool raiseMilestoneEvent)
    {
        int effectiveScore = Mathf.Max(HighScore, _score);
        foreach (var tool in _allTools)
        {
            bool shouldUnlock = _devMode || (effectiveScore >= tool.unlockScore);
            if (shouldUnlock && !_unlockedTools.Contains(tool))
            {
                _unlockedTools.Add(tool);
                Debug.Log($"[GameManager] Unlocked tool: {tool.toolName}!");

                if (raiseMilestoneEvent)
                {
                    _onMilestoneReached?.Raise(tool);

                    // Milestone unlock SFX
                    if (SFXManager.Instance != null && SFXManager.Instance.MilestoneSFX != null)
                        SFXManager.Instance.Play(SFXManager.Instance.MilestoneSFX);
                }
            }
        }
    }

    /// <summary>
    /// Get a random tool from the unlocked pool (different from current if possible), using baseWeight.
    /// </summary>
    public ToolData GetRandomTool()
    {
        if (_unlockedTools.Count == 0) return null;

        var candidates = _unlockedTools.Where(t => t != _currentTool).ToList();
        
        if (candidates.Count == 0)
            return _unlockedTools.First();

        float totalWeight = candidates.Sum(t => t.baseWeight);
        float rand = Random.Range(0f, totalWeight);

        float cumulative = 0f;
        foreach (var tool in candidates)
        {
            cumulative += tool.baseWeight;
            if (rand <= cumulative)
                return tool;
        }

        return candidates.Last();
    }

    private void HandleToolPickedUp()
    {
        _score++;

        // Save high score immediately so unlocked tools apply immediately if reached during a run
        if (_score > HighScore)
        {
            HighScore = _score;
            SaveManager.Data.highScore = HighScore;
            SaveManager.Save();
            
            UpdateUnlockedTools(true);
        }

        if (!_hasAwardedFirstPickupTool)
        {
            _currentTool = ResolveFirstPickupTool();
            _hasAwardedFirstPickupTool = true;
        }
        else
        {
            _currentTool = GetRandomTool();
        }

        _onScoreChanged?.Raise(_score);
        _onToolEquipped?.Raise(_currentTool);
    }

    private void HandlePlayerDied()
    {
        SetState(GameState.GameOver);
        Time.timeScale = 0f;

        // Update high score
        if (_score > HighScore)
        {
            HighScore = _score;
            SaveManager.Data.highScore = HighScore;
            SaveManager.Save();
        }

        _onGameOver?.Raise();
    }

    private void HandleGamePaused()
    {
        if (_currentState == GameState.Paused)
        {
            SetState(GameState.Playing);
            Time.timeScale = 1f;
        }
        else if (_currentState == GameState.Playing)
        {
            SetState(GameState.Paused);
            Time.timeScale = 0f;
        }
    }

    private void HandleGameRestart()
    {
        // GameOverUI already calls SceneManager.LoadScene("Game") directly.
        // This handler is for any additional cleanup GameManager needs to do before reload.
        Time.timeScale = 1f;
        SetState(GameState.MainMenu); // Reset state so OnSceneLoaded → StartGame() fires
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game" && _currentState != GameState.Playing)
            StartGame();
        else if (scene.name == "MainMenu")
            SetState(GameState.MainMenu);
    }

    private void DevEquipToolByIndex(int index)
    {
        if (_allTools == null || index < 0 || index >= _allTools.Length)
            return;

        ToolData selectedTool = _allTools[index];
        if (selectedTool == null)
            return;

        _currentTool = selectedTool;
        _onToolEquipped?.Raise(_currentTool);
        Debug.Log($"[GameManager][DEV] Equipped slot {index + 1}: {selectedTool.toolName}");
    }

    private void DevEquipFirstTool()
    {
        ToolData firstTool = ResolveFirstPickupTool();
        if (firstTool == null)
        {
            Debug.LogWarning("[GameManager][DEV] Slot 1 failed: no first pickup tool configured/found.");
            return;
        }

        _currentTool = firstTool;
        _onToolEquipped?.Raise(_currentTool);
        Debug.Log($"[GameManager][DEV] Equipped slot 1: {firstTool.toolName}");
    }

    private ToolData ResolveFirstPickupTool()
    {
        if (_firstPickupTool != null)
            return _firstPickupTool;

        if (_allTools != null)
        {
            ToolData hammer = _allTools.FirstOrDefault(t =>
                t != null && string.Equals(t.toolName, "Hammer", System.StringComparison.OrdinalIgnoreCase));
            if (hammer != null)
                return hammer;
        }

        return _unlockedTools.FirstOrDefault();
    }

    public void SetState(GameState newState)
    {
        _currentState = newState;
        Debug.Log($"[GameManager] State changed to: {newState}");
    }

    /// <summary>
    /// Reset high score to 0. Milestones will re-lock on next StartGame().
    /// Call from console or debug UI for testing.
    /// </summary>
    [ContextMenu("Reset High Score")]
    public void ResetHighScore()
    {
        SaveManager.ResetAll();
        HighScore = 0;
        Debug.Log("[GameManager] High score reset to 0. Restart the game to re-lock tools.");
    }
}
