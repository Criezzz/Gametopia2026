using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns enemies on independent per-type timers driven by EnemyData SOs.
/// Also triggers horde events (bursts of enemies) on a separate timer.
/// Handles angry respawns when enemies fall off the bottom of the screen.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Lanes")]
    [Tooltip("Each entry is an independent spawn lane driven by its EnemyData SO timing fields.")]
    [SerializeField] private SpawnLane[] _lanes;

    [Header("Horde")]
    [SerializeField] private HordeConfig _hordeConfig;

    [Header("Spawn Area")]
    [SerializeField] private float _spawnYOffset = 1f;
    [SerializeField] private float _spawnXLeft = -4f;
    [SerializeField] private float _spawnXRight = 4f;

    [Header("Camera Shake")]
    [Tooltip("Shake duration when an angry enemy drops from the sky")]
    [SerializeField] private float _angryDropShakeDuration = 0.15f;
    [Tooltip("Shake magnitude when an angry enemy drops from the sky")]
    [SerializeField] private float _angryDropShakeMagnitude = 0.1f;

    [Header("Event Channels")]
    [SerializeField] private EnemyFallChannel _onEnemyFell;

    // Runtime state per lane (not serialized)
    private float[] _spawnTimers;
    private float[] _currentIntervals;
    private float[] _difficultyTimers;

    // Horde runtime state
    private float _hordeSpawnTimer;
    private float _hordeCurrentInterval;
    private float _hordeDifficultyTimer;

    private void Start()
    {
        InitLanes();
        InitHorde();
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
        UpdateLanes();
        UpdateHorde();
    }

    #region Lane System

    private void InitLanes()
    {
        if (_lanes == null) return;

        _spawnTimers = new float[_lanes.Length];
        _currentIntervals = new float[_lanes.Length];
        _difficultyTimers = new float[_lanes.Length];

        for (int i = 0; i < _lanes.Length; i++)
        {
            var data = _lanes[i].enemyData;
            if (data == null) continue;

            // First spawn uses firstSpawnDelay; after that uses interval
            _spawnTimers[i] = data.firstSpawnDelay;
            _currentIntervals[i] = data.spawnInterval;
            _difficultyTimers[i] = data.difficultyTickInterval;
        }
    }

    private void UpdateLanes()
    {
        if (_lanes == null) return;

        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane.prefab == null || lane.enemyData == null) continue;

            var data = lane.enemyData;

            // Difficulty scaling per lane
            _difficultyTimers[i] -= Time.deltaTime;
            if (_difficultyTimers[i] <= 0f)
            {
                _difficultyTimers[i] = data.difficultyTickInterval;
                _currentIntervals[i] = Mathf.Max(
                    data.minSpawnInterval,
                    _currentIntervals[i] - data.intervalReductionPerTick
                );
            }

            // Spawn timer per lane
            _spawnTimers[i] -= Time.deltaTime;
            if (_spawnTimers[i] <= 0f)
            {
                SpawnEnemy(lane.prefab, lane.enemyData);
                _spawnTimers[i] = _currentIntervals[i];
            }
        }
    }

    #endregion

    #region Horde System

    private void InitHorde()
    {
        if (_hordeConfig == null) return;

        _hordeSpawnTimer = _hordeConfig.firstHordeDelay;
        _hordeCurrentInterval = _hordeConfig.hordeInterval;
        _hordeDifficultyTimer = _hordeConfig.difficultyTickInterval;
    }

    private void UpdateHorde()
    {
        if (_hordeConfig == null || _hordeConfig.enemyPrefab == null) return;

        // Difficulty scaling for horde
        _hordeDifficultyTimer -= Time.deltaTime;
        if (_hordeDifficultyTimer <= 0f)
        {
            _hordeDifficultyTimer = _hordeConfig.difficultyTickInterval;
            _hordeCurrentInterval = Mathf.Max(
                _hordeConfig.minHordeInterval,
                _hordeCurrentInterval - _hordeConfig.intervalReductionPerTick
            );
        }

        // Horde timer
        _hordeSpawnTimer -= Time.deltaTime;
        if (_hordeSpawnTimer <= 0f)
        {
            StartCoroutine(SpawnHordeRoutine());
            _hordeSpawnTimer = _hordeCurrentInterval;
        }
    }

    private IEnumerator SpawnHordeRoutine()
    {
        if (_hordeConfig.enemyPrefab == null || Camera.main == null) yield break;

        // All horde enemies share the same spawn position and direction
        float x = Random.value < 0.5f ? _spawnXLeft : _spawnXRight;
        float y = Camera.main.transform.position.y + Camera.main.orthographicSize + _spawnYOffset;
        Vector2 spawnPos = new Vector2(x, y);
        int sharedDirection = Random.value > 0.5f ? 1 : -1;

        for (int i = 0; i < _hordeConfig.enemyCount; i++)
        {
            GameObject enemy = Instantiate(_hordeConfig.enemyPrefab, spawnPos, Quaternion.identity);

            // Force same walk direction so they form a chain
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

    /// <summary>
    /// Called when an enemy falls off the screen via the EnemyFallChannel.
    /// Respawns the enemy at the top of the screen as angry.
    /// </summary>
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
        // Search lanes for the matching move type
        if (_lanes != null)
        {
            foreach (var lane in _lanes)
            {
                if (lane.enemyData != null && lane.enemyData.moveType == moveType)
                    return lane.prefab;
            }
        }

        // Fallback: return first lane prefab if available
        if (_lanes != null && _lanes.Length > 0 && _lanes[0].prefab != null)
            return _lanes[0].prefab;

        return null;
    }

    #endregion

    /// <summary>
    /// A spawn lane pairs a prefab with its EnemyData SO.
    /// Each lane runs its own independent timer.
    /// </summary>
    [System.Serializable]
    public class SpawnLane
    {
        public GameObject prefab;
        public EnemyData enemyData;
    }
}
