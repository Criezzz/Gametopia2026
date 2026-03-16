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
    private const string LegacyHighScoreKey = "HighScore";

    public static GameManager Instance { get; private set; }

    [Header("Current State")]
    [SerializeField] private GameState _currentState = GameState.MainMenu;
    public GameState CurrentState => _currentState;

    [Header("Score")]
    [SerializeField] private int _score;
    public int Score => _score;
    public int HighScore { get; private set; }
    public int CurrentMapHighScore => SaveManager.GetMapHighScore(GetCurrentMapScoreKey());

    [Header("Game Mode")]
    [SerializeField] private GameModeData _currentMode;
    public GameModeData CurrentMode => _currentMode;

    private MapData _currentMap;
    public MapData CurrentMap => _currentMap;

    public void SetMap(MapData map)
    {
        _currentMap = map;
        PendingMap = map;
    }

    /// <summary>
    /// Temporary storage for the mode chosen in the Main Menu, 
    /// read by the Map Picker to know which scene to load.
    /// Static so it survives without an instance (MainMenu has no GameManager).
    /// </summary>
    public static GameModeData PendingGameMode { get; set; }

    /// <summary>
    /// Temporary storage for the map chosen in Map Picker when GameManager does not exist yet.
    /// </summary>
    public static MapData PendingMap { get; set; }

    /// <summary>
    /// When true the current run is a tutorial. Stats are not persisted
    /// (except first-box exception) and the restart target is the Tutorial scene.
    /// Static so it survives scene reloads, same pattern as PendingGameMode.
    /// </summary>
    public static bool IsTutorial { get; set; }

    /// <summary>
    /// Maximum pickup threshold allowed during a tutorial run.
    /// Set by TutorialManager from its _maxUnlockTool ToolData asset.
    /// Tools with unlockPickupCount above this are never added to the pool.
    /// </summary>
    public static int TutorialUnlockPickupCap { get; set; } = int.MaxValue;

    public string ActiveSceneName => IsTutorial ? SceneNames.Tutorial : ResolveSceneName();
    // Arena: per-player scores (index 0 = P1, index 1 = P2)
    private int[] _arenaScores = new int[2];
    public int GetArenaScore(int playerIndex) => _arenaScores[Mathf.Clamp(playerIndex, 0, 1)];

    [Header("Debug")]
    [Tooltip("If true, unlocks all tools and maps for testing.")]
    [SerializeField] private bool _devMode = false;
    public static bool DevMode => Instance != null && Instance._devMode;

    /// True when in Arena mode. Uses _currentMode when set; otherwise falls back to scene name
    /// (so Arena works when running the scene directly without going through Main Menu).
    public static bool IsArenaMode
    {
        get
        {
            if (Instance == null) return false;
            if (Instance._currentMode != null && Instance._currentMode.modeType == GameModeType.Arena)
                return true;
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            return scene == SceneNames.Arena || scene == SceneNames.Arena_Map2;
        }
    }

    [Header("Tool Pool")]
    [SerializeField] private ToolData[] _allTools;
    [SerializeField] private ToolData _firstPickupTool;
    public ToolData FirstPickupTool => _firstPickupTool;
    private readonly HashSet<ToolData> _unlockedTools = new();
    private ToolData[] _playerTools = new ToolData[2];
    private bool[] _hasAwardedFirstPickup = new bool[2];
    private bool[] _playerDead = new bool[2];
    public ToolData CurrentTool => _playerTools[0]; // Solo compat

    public ToolData GetPlayerTool(int playerIndex)
    {
        int idx = Mathf.Clamp(playerIndex, 0, 1);
        return _playerTools[idx];
    }

    [Header("Toolbox")]
    [SerializeField] private Toolbox _toolboxPrefab;
    private Toolbox _toolboxInstance;

    [Header("Event Channels")]
    [SerializeField] private IntEventChannel _onToolPickedUp;
    [SerializeField] private IntEventChannel _onScoreChanged;
    [SerializeField] private ToolDataEventChannel _onMilestoneReached;
    [SerializeField] private ToolDataEventChannel _onToolEquipped;
    [SerializeField] private IntEventChannel _onPlayerDied;
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

        // Apply pending map chosen before this instance existed (MapPicker → gameplay scene)
        if (PendingMap != null && _currentMap == null)
            _currentMap = PendingMap;

        // Load save data from JSON
        SaveManager.Load();
        HighScore = SaveManager.Data.highScore;

        // Ensure global high score is at least the max of all per-map highscores.
        if (SaveManager.Data.mapHighScores != null)
        {
            int maxMapScore = 0;
            for (int i = 0; i < SaveManager.Data.mapHighScores.Count; i++)
            {
                MapHighScoreEntry entry = SaveManager.Data.mapHighScores[i];
                if (entry != null && entry.highScore > maxMapScore)
                    maxMapScore = entry.highScore;
            }

            if (maxMapScore > HighScore)
            {
                HighScore = maxMapScore;
                SaveManager.Data.highScore = HighScore;
                SaveManager.Save();
            }
        }

        // Migrate legacy PlayerPrefs if present
        int legacyScore = PlayerPrefs.GetInt(LegacyHighScoreKey, 0);
        if (legacyScore > HighScore)
        {
            HighScore = legacyScore;
            SaveManager.Data.highScore = legacyScore;
            SaveManager.Save();
            PlayerPrefs.DeleteKey(LegacyHighScoreKey);
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
        if (_onPlayerDied != null) _onPlayerDied.Register(HandlePlayerDiedInt);
        if (_onGamePaused != null) _onGamePaused.Register(HandleGamePaused);
        if (_onGameRestart != null) _onGameRestart.Register(HandleGameRestart);
        if (_onEnemyKilled != null) _onEnemyKilled.Register(HandleEnemyKilled);
    }

    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (IsGameplayScene(sceneName) && _currentState != GameState.Playing)
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
        if (_onPlayerDied != null) _onPlayerDied.Unregister(HandlePlayerDiedInt);
        if (_onGamePaused != null) _onGamePaused.Unregister(HandleGamePaused);
        if (_onGameRestart != null) _onGameRestart.Unregister(HandleGameRestart);
        if (_onEnemyKilled != null) _onEnemyKilled.Unregister(HandleEnemyKilled);
    }

    /// Called by MapPickerUI before loading the game scene.
    public void SetGameMode(GameModeData mode)
    {
        _currentMode = mode;
        PendingGameMode = mode;
    }

    /// Resolves the gameplay scene from mode + map with safe fallbacks.
    public string ResolveSceneName(GameModeData mode = null, MapData map = null)
    {
        GameModeData effectiveMode = mode ?? _currentMode ?? PendingGameMode;
        MapData effectiveMap = map ?? _currentMap ?? PendingMap;

        if (effectiveMode != null && effectiveMap != null)
        {
            string mapScene = effectiveMap.GetSceneForMode(effectiveMode);
            if (!string.IsNullOrEmpty(mapScene))
                return mapScene;
        }

        if (effectiveMode != null)
        {
            if (!string.IsNullOrEmpty(effectiveMode.sceneName))
                return effectiveMode.sceneName;

            return effectiveMode.modeType == GameModeType.Arena
                ? SceneNames.Arena
                : SceneNames.Game;
        }

        return SceneNames.Game;
    }

    private bool IsGameplayScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        if (sceneName == SceneNames.MainMenu || sceneName == SceneNames.MapPicker ||
            sceneName == SceneNames.Settings || sceneName == "Settings" || sceneName == SceneNames.Achievement)
            return false;

        // Main path: selected map+mode resolution.
        if (sceneName == ActiveSceneName)
            return true;

        // Backward compatibility with existing default gameplay scenes.
        return sceneName == SceneNames.Game || sceneName == SceneNames.Arena
            || sceneName == SceneNames.Arena_Map2 || sceneName == SceneNames.Tutorial;
    }

    public int GetCurrentMapHighScore()
    {
        return SaveManager.GetMapHighScore(GetCurrentMapScoreKey());
    }

    private string GetCurrentMapScoreKey()
    {
        if (_currentMap != null)
            return _currentMap.GetPersistentId();

        string activeScene = SceneManager.GetActiveScene().name;
        return string.IsNullOrEmpty(activeScene) ? SceneNames.Game : activeScene;
    }

    private bool TryUpdateCurrentMapHighScore()
    {
        int currentMapBest = GetCurrentMapHighScore();
        if (_score <= currentMapBest)
            return false;

        SaveManager.SetMapHighScore(GetCurrentMapScoreKey(), _score);
        return true;
    }

    public void StartGame()
    {
        _score = 0;
        _arenaScores = new int[2];
        _unlockedTools.Clear();
        _hasAwardedFirstPickup = new bool[2];
        _playerTools = new ToolData[2];
        _playerDead = new bool[2];

        UpdateUnlockedTools(false);

        SetState(GameState.Playing);
        Time.timeScale = 1f;

        if (!IsTutorial)
            SpawnToolbox();

        if (!IsTutorial)
        {
            SaveManager.Data.totalGamesPlayed++;
            SaveManager.Save();
        }

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
        int effectivePickupCount = GetEffectiveToolUnlockPickupCount();
        if (_allTools == null) return;

        foreach (var tool in _allTools)
        {
            if (tool == null) continue;

            if (IsTutorial && tool.unlockPickupCount > TutorialUnlockPickupCap)
                continue;

            bool shouldUnlock = _devMode || (effectivePickupCount >= tool.unlockPickupCount);
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

    private int GetEffectiveToolUnlockPickupCount()
    {
        // Keep tutorial progression run-based as before.
        if (IsTutorial)
            return Mathf.Max(0, _score);

        return Mathf.Max(0, SaveManager.Data.totalToolPickups);
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
        bool isArenaMode = IsArenaMode;
        int clampedPlayerIndex = Mathf.Clamp(playerIndex, 0, 1);

        if (IsTutorial)
        {
            // First-box exception: count the pickup if the player has never collected any
            if (_score == 0 && SaveManager.Data.totalToolPickups == 0)
            {
                SaveManager.Data.totalToolPickups++;
                SaveManager.Save();
            }

            _score++;
            UpdateUnlockedTools(true);
            _onScoreChanged?.Raise(_score);
            return;
        }

        _score++;
        SaveManager.Data.totalToolPickups++;
        if (isArenaMode)
            _arenaScores[clampedPlayerIndex]++;

        TryUpdateCurrentMapHighScore();

        if (_score > HighScore)
        {
            HighScore = _score;
            SaveManager.Data.highScore = HighScore;
        }

        UpdateUnlockedTools(true);
        SaveManager.Save();

        _onScoreChanged?.Raise(isArenaMode ? _arenaScores[clampedPlayerIndex] : _score);
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

    private void HandlePlayerDiedInt(int playerIndex)
    {
        bool isArena = IsArenaMode;

        if (isArena)
        {
            int idx = Mathf.Clamp(playerIndex, 0, 1);
            _playerDead[idx] = true;
            if (!(_playerDead[0] && _playerDead[1]))
                return;
        }

        SetState(GameState.GameOver);
        Time.timeScale = 0f;

        if (!IsTutorial)
        {
            bool mapHighScoreImproved = TryUpdateCurrentMapHighScore();
            bool globalHighScoreImproved = false;

            if (_score > HighScore)
            {
                HighScore = _score;
                SaveManager.Data.highScore = HighScore;
                globalHighScoreImproved = true;
            }

            if (mapHighScoreImproved || globalHighScoreImproved)
                SaveManager.Save();
        }

        _onGameOver?.Raise();
    }

    private void HandleEnemyKilled()
    {
        if (IsTutorial) return;

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
        // UI handles actual scene load. This handler performs cleanup before reload.
        Time.timeScale = 1f;
        SetState(GameState.MainMenu); // Reset state so OnSceneLoaded → StartGame() fires
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsGameplayScene(scene.name) && _currentState != GameState.Playing)
            StartGame();
        else if (scene.name == SceneNames.MainMenu)
        {
            IsTutorial = false;
            TutorialUnlockPickupCap = int.MaxValue;
            SetState(GameState.MainMenu);
        }
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

    [ContextMenu("Reset Progress")]
    public void ResetHighScore()
    {
        SaveManager.ResetAll();
        HighScore = 0;
        Debug.Log("[GameManager] Save progress reset. Restart the game to re-lock tools.");
    }
}
