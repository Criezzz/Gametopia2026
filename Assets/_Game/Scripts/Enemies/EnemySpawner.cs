using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies using a single global timer. Each lane/horde has its own
/// availability cooldown; when the global timer fires it picks from whatever
/// is available following these rules:
///   - If horde is available: spawn horde + at most 1 other lane.
///   - Otherwise: spawn up to 3 available lanes.
/// The global interval decreases over time for incremental pacing.
/// Handles angry respawns when enemies fall off the bottom of the screen.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Lanes")]
    [Tooltip("Each entry is an independent spawn lane driven by its EnemyData SO timing fields.")]
    [SerializeField] private SpawnLane[] _lanes;

    [Header("Horde")]
    [SerializeField] private HordeConfig _hordeConfig;

    [Header("Global Spawn Timer")]
    [Tooltip("Initial delay before the first global spawn event")]
    [SerializeField] private float _globalFirstDelay = 2f;
    [Tooltip("Starting interval between global spawn events (seconds)")]
    [SerializeField] private float _globalSpawnInterval = 3f;
    [Tooltip("Every this many seconds the global interval decreases")]
    [SerializeField] private float _globalDifficultyTickInterval = 15f;
    [Tooltip("How much to reduce global interval each difficulty tick")]
    [SerializeField] private float _globalIntervalReduction = 0.2f;
    [Tooltip("Minimum global spawn interval — will never go below this")]
    [SerializeField] private float _globalMinInterval = 1f;

    [Header("Map Spawn Config")]
    [Tooltip("Direct scene-level override. If set, takes priority over GameManager.CurrentMap.spawnConfig.")]
    [SerializeField] private MapSpawnConfig _mapSpawnConfig;

    [Header("Spawn Area (fallback defaults if no MapSpawnConfig is resolved)")]
    [SerializeField] private float _spawnYOffset = 1f;
    [SerializeField] private float _spawnXLeft = -4f;
    [SerializeField] private float _spawnXRight = 4f;

    [Header("Camera Shake")]
    [Tooltip("Shake duration when an angry enemy drops from the sky")]
    [SerializeField] private float _angryDropShakeDuration = 0.15f;
    [Tooltip("Shake magnitude when an angry enemy drops from the sky")]
    [SerializeField] private float _angryDropShakeMagnitude = 0.1f;

    public float AngryDropShakeDuration => _angryDropShakeDuration;
    public float AngryDropShakeMagnitude => _angryDropShakeMagnitude;

    [Header("Event Channels")]
    [SerializeField] private EnemyFallChannel _onEnemyFell;

    // Global spawn timer runtime
    private float _globalTimer;
    private float _globalCurrentInterval;
    private float _globalDifficultyTimer;

    // Per-lane availability cooldown (counts down; <= 0 means available)
    private float[] _laneCooldowns;

    // Horde availability cooldown
    private float _hordeCooldown;

    private void Start()
    {
        ApplyMapSpawnConfig();
        InitLanes();
        InitGlobalTimer();
    }

    private void OnEnable()
    {
        if (_onEnemyFell != null)
            _onEnemyFell.Register(HandleEnemyFell);
    }

    private void OnDisable()
    {
        if (_onEnemyFell != null)
            _onEnemyFell.Unregister(HandleEnemyFell);
    }

    private void Update()
    {
        UpdateCooldowns();
        UpdateGlobalTimer();
    }

    #region Initialization

    private void ApplyMapSpawnConfig()
    {
        MapSpawnConfig config = _mapSpawnConfig;

        if (config == null && GameManager.Instance != null)
            config = GameManager.Instance.CurrentMap?.spawnConfig;

        if (config == null) return;

        _spawnXLeft = config.spawnXLeft;
        _spawnXRight = config.spawnXRight;
        _spawnYOffset = config.spawnYOffset;
    }

    private void InitLanes()
    {
        if (_lanes == null) return;

        _laneCooldowns = new float[_lanes.Length];
        for (int i = 0; i < _lanes.Length; i++)
        {
            var data = _lanes[i].enemyData;
            if (data == null) continue;
            _laneCooldowns[i] = data.firstSpawnDelay;
        }

        // Horde cooldown
        _hordeCooldown = _hordeConfig != null ? _hordeConfig.firstHordeDelay : float.MaxValue;
    }

    private void InitGlobalTimer()
    {
        _globalTimer = _globalFirstDelay;
        _globalCurrentInterval = _globalSpawnInterval;
        _globalDifficultyTimer = _globalDifficultyTickInterval;
    }

    #endregion

    #region Update Loop

    private void UpdateCooldowns()
    {
        // Tick lane cooldowns
        if (_laneCooldowns != null)
        {
            for (int i = 0; i < _laneCooldowns.Length; i++)
            {
                if (_laneCooldowns[i] > 0f)
                    _laneCooldowns[i] -= Time.deltaTime;
            }
        }

        // Tick horde cooldown
        if (_hordeCooldown > 0f)
            _hordeCooldown -= Time.deltaTime;
    }

    private void UpdateGlobalTimer()
    {
        // Global difficulty scaling
        _globalDifficultyTimer -= Time.deltaTime;
        if (_globalDifficultyTimer <= 0f)
        {
            _globalDifficultyTimer = _globalDifficultyTickInterval;
            _globalCurrentInterval = Mathf.Max(
                _globalMinInterval,
                _globalCurrentInterval - _globalIntervalReduction
            );
        }

        // Global spawn event
        _globalTimer -= Time.deltaTime;
        if (_globalTimer <= 0f)
        {
            DoSpawnEvent();
            _globalTimer = _globalCurrentInterval;
        }
    }

    #endregion

    #region Spawn Event

    private void DoSpawnEvent()
    {
        bool hordeAvailable = _hordeConfig != null
            && _hordeConfig.enemyPrefab != null
            && _hordeCooldown <= 0f;

        // Gather available lane indices
        List<int> availableLanes = new();
        if (_lanes != null && _laneCooldowns != null)
        {
            for (int i = 0; i < _lanes.Length; i++)
            {
                if (_lanes[i].prefab == null || _lanes[i].enemyData == null) continue;
                if (_laneCooldowns[i] <= 0f)
                    availableLanes.Add(i);
            }
        }

        if (!hordeAvailable && availableLanes.Count == 0) return;

        // Shuffle available lanes for random selection
        ShuffleList(availableLanes);

        if (hordeAvailable)
        {
            // Spawn horde + at most 1 other lane
            StartCoroutine(SpawnHordeRoutine());
            _hordeCooldown = _hordeConfig.hordeInterval;

            if (availableLanes.Count > 0)
            {
                int idx = availableLanes[0];
                SpawnEnemy(_lanes[idx].prefab, _lanes[idx].enemyData);
                _laneCooldowns[idx] = _lanes[idx].enemyData.spawnInterval;
            }
        }
        else
        {
            // Spawn up to 3 available lanes
            int count = Mathf.Min(3, availableLanes.Count);
            for (int i = 0; i < count; i++)
            {
                int idx = availableLanes[i];
                SpawnEnemy(_lanes[idx].prefab, _lanes[idx].enemyData);
                _laneCooldowns[idx] = _lanes[idx].enemyData.spawnInterval;
            }
        }
    }

    private static void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion

    #region Horde

    private IEnumerator SpawnHordeRoutine()
    {
        if (_hordeConfig.enemyPrefab == null || Camera.main == null) yield break;

        float x = Random.value < 0.5f ? _spawnXLeft : _spawnXRight;
        float y = Camera.main.transform.position.y + Camera.main.orthographicSize + _spawnYOffset;
        Vector2 spawnPos = new Vector2(x, y);
        int sharedDirection = Random.value > 0.5f ? 1 : -1;

        for (int i = 0; i < _hordeConfig.enemyCount; i++)
        {
            GameObject enemy = Instantiate(_hordeConfig.enemyPrefab, spawnPos, Quaternion.identity);

            WalkerEnemy walker = enemy.GetComponent<WalkerEnemy>();
            if (walker != null) walker.SetDirection(sharedDirection);

            if (i < _hordeConfig.enemyCount - 1)
                yield return new WaitForSeconds(_hordeConfig.delayBetweenSpawns);
        }
    }

    #endregion

    #region Spawn Helpers

    private void SpawnEnemy(GameObject prefab, EnemyData data = null)
    {
        if (prefab == null || Camera.main == null) return;

        float x = Random.value < 0.5f ? _spawnXLeft : _spawnXRight;
        float y = Camera.main.transform.position.y + Camera.main.orthographicSize + _spawnYOffset;
        Vector3 pos = new Vector2(x, y);
        Instantiate(prefab, pos, Quaternion.identity);

        if (data != null)
            VFXSpawner.Spawn(data.spawnVFXPrefab, pos);
    }

    #endregion

    #region Angry Respawn

    private void HandleEnemyFell(EnemyFallData data)
    {
        if (data.enemyData == null || Camera.main == null) return;

        GameObject prefab = GetPrefabForMoveType(data.enemyData.moveType);
        if (prefab == null)
        {
            Debug.LogWarning($"[EnemySpawner] No prefab for move type: {data.enemyData.moveType}");
            return;
        }

        float x = Random.value < 0.5f ? _spawnXLeft : _spawnXRight;
        float y = Camera.main.transform.position.y + Camera.main.orthographicSize + _spawnYOffset;

        GameObject newEnemy = Instantiate(prefab, new Vector2(x, y), Quaternion.identity);
        BaseEnemy baseEnemy = newEnemy.GetComponent<BaseEnemy>();

        if (baseEnemy != null)
        {
            baseEnemy.MarkAsRespawned();

            if (CameraShake.Instance != null)
                CameraShake.Instance.Shake(_angryDropShakeDuration, _angryDropShakeMagnitude);

            Debug.Log($"[EnemySpawner] Respawned angry {data.enemyData.enemyName} at ({x:F1}, {y:F1}). Was already angry: {data.wasAngry}");
        }
    }

    private GameObject GetPrefabForMoveType(EnemyMoveType moveType)
    {
        if (_lanes != null)
        {
            foreach (var lane in _lanes)
            {
                if (lane.enemyData != null && lane.enemyData.moveType == moveType)
                    return lane.prefab;
            }
        }

        if (_lanes != null && _lanes.Length > 0 && _lanes[0].prefab != null)
            return _lanes[0].prefab;

        return null;
    }

    #endregion

    [System.Serializable]
    public class SpawnLane
    {
        public GameObject prefab;
        public EnemyData enemyData;
    }
}
