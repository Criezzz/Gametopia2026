using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}

/// Singleton GameManager. Manages score, tool pool, unlocks, and game state.
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

    [Header("Game Mode")]
    [SerializeField] private GameModeData _currentMode;
    public GameModeData CurrentMode => _currentMode;

    /// <summary>
    /// Temporary storage for the mode chosen in the Main Menu, 
    /// read by the Map Picker to know which scene to load.
    /// Static so it survives without an instance (MainMenu has no GameManager).
    /// </summary>
    public static GameModeData PendingGameMode { get; set; }

    public string ActiveSceneName => (_currentMode != null && !string.IsNullOrEmpty(_currentMode.sceneName)) 
        ? _currentMode.sceneName 
        : SceneNames.Game;
    // Arena: per-player scores (index 0 = P1, index 1 = P2)
    private int[] _arenaScores = new int[2];
    public int GetArenaScore(int playerIndex) => _arenaScores[Mathf.Clamp(playerIndex, 0, 1)];

    [Header("Debug")]
    [Tooltip("If true, unlocks all tools immediately ignoring the High Score requirement.")]
    [SerializeField] private bool _devMode = false;

    [Header("Tool Pool")]
    [SerializeField] private ToolData[] _allTools;
    [SerializeField] private ToolData _firstPickupTool;
    private readonly HashSet<ToolData> _unlockedTools = new();
    private ToolData[] _playerTools = new ToolData[2];
    private bool[] _hasAwardedFirstPickup = new bool[2];
    public ToolData CurrentTool => _playerTools[0]; // Solo compat

    [Header("Toolbox")]
    [SerializeField] private Toolbox _toolboxPrefab;
    private Toolbox _toolboxInstance;

    [Header("Event Channels")]
    [SerializeField] private IntEventChannel _onToolPickedUp;
    [SerializeField] private IntEventChannel _onScoreChanged;
    [SerializeField] private ToolDataEventChannel _onMilestoneReached;
    [SerializeField] private ToolDataEventChannel _onToolEquipped;
    [SerializeField] private VoidEventChannel _onPlayerDied;
    [SerializeField] private VoidEventChannel _onGameOver;
    [SerializeField] private VoidEventChannel _onGamePaused;
    [SerializeField] private VoidEventChannel _onGameRestart;
    [SerializeField] private VoidEventChannel _onEnemyKilled;

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

        // Apply pending mode chosen before this instance existed (MainMenu → MapPicker → Game)
        if (PendingGameMode != null && _currentMode == null)
            _currentMode = PendingGameMode;

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
        if (_onEnemyKilled != null) _onEnemyKilled.Register(HandleEnemyKilled);
    }

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if ((sceneName == SceneNames.Game || sceneName == SceneNames.Arena) && _currentState != GameState.Playing)
            StartGame();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!_devMode) return;

        for (int i = 0; i < 8; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                if (i == 0) DevEquipFirstTool();
                else DevEquipToolByIndex(i);
            }
        }
    }
#endif

    private void OnDisable()
    {
        if (_onToolPickedUp != null) _onToolPickedUp.Unregister(HandleToolPickedUp);
        if (_onPlayerDied != null) _onPlayerDied.Unregister(HandlePlayerDied);
        if (_onGamePaused != null) _onGamePaused.Unregister(HandleGamePaused);
        if (_onGameRestart != null) _onGameRestart.Unregister(HandleGameRestart);
        if (_onEnemyKilled != null) _onEnemyKilled.Unregister(HandleEnemyKilled);
    }

    /// Called by MapPickerUI before loading the game scene.
    public void SetGameMode(GameModeData mode)
    {
        _currentMode = mode;
    }

    public void StartGame()
    {
        _score = 0;
        _arenaScores = new int[2];
        _unlockedTools.Clear();
        _hasAwardedFirstPickup = new bool[2];
        _playerTools = new ToolData[2];

        UpdateUnlockedTools(false);

        SetState(GameState.Playing);
        Time.timeScale = 1f;

        SpawnToolbox();

        SaveManager.Data.totalGamesPlayed++;
        SaveManager.Save();

        _onScoreChanged?.Raise(_score);
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

    public ToolData GetRandomTool()
    {
        if (_unlockedTools.Count == 0) return null;

        var candidates = _unlockedTools.ToList();

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

    private void HandleToolPickedUp(int playerIndex)
    {
        bool isArena = _currentMode != null && _currentMode.modeType == GameModeType.Arena;

        _score++;
        SaveManager.Data.totalToolPickups++;
        if (isArena)
            _arenaScores[Mathf.Clamp(playerIndex, 0, 1)]++;

        if (_score > HighScore)
        {
            HighScore = _score;
            SaveManager.Data.highScore = HighScore;
            SaveManager.Save();

            UpdateUnlockedTools(true);
        }

        _onScoreChanged?.Raise(isArena ? _arenaScores[playerIndex] : _score);
    }

    /// Called by Toolbox to get the tool for a specific player.
    public ToolData GetToolForPlayer(int playerIndex)
    {
        int idx = Mathf.Clamp(playerIndex, 0, 1);

        ToolData tool;
        if (!_hasAwardedFirstPickup[idx])
        {
            tool = ResolveFirstPickupTool();
            _hasAwardedFirstPickup[idx] = true;
        }
        else
        {
            tool = GetRandomTool();
        }

        _playerTools[idx] = tool;
        return tool;
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

    private void HandleEnemyKilled()
    {
        SaveManager.Data.totalEnemiesKilled++;
        SaveManager.Save();
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
        if ((scene.name == SceneNames.Game || scene.name == SceneNames.Arena) && _currentState != GameState.Playing)
            StartGame();
        else if (scene.name == SceneNames.MainMenu)
            SetState(GameState.MainMenu);
    }

    private void DevEquipToolByIndex(int index)
    {
        if (_allTools == null || index < 0 || index >= _allTools.Length)
            return;

        ToolData selectedTool = _allTools[index];
        if (selectedTool == null)
            return;

        _playerTools[0] = selectedTool;
        Object.FindFirstObjectByType<PlayerToolHandler>()?.EquipTool(selectedTool);
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

        _playerTools[0] = firstTool;
        Object.FindFirstObjectByType<PlayerToolHandler>()?.EquipTool(firstTool);
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

    [ContextMenu("Reset High Score")]
    public void ResetHighScore()
    {
        SaveManager.ResetAll();
        HighScore = 0;
        Debug.Log("[GameManager] High score reset to 0. Restart the game to re-lock tools.");
    }
}
