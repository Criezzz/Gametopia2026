using UnityEngine;

/// <summary>
/// Spawns enemies at timed intervals and handles angry respawns
/// when enemies fall off the bottom of the screen.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject _walkerPrefab;
    [SerializeField] private GameObject _flyerPrefab;
    [SerializeField] private float _spawnInterval = 3f;

    [Header("Difficulty Scaling")]
    [Tooltip("Every this many seconds, spawn interval decreases")]
    [SerializeField] private float _difficultyTickInterval = 10f;
    [Tooltip("How much to reduce spawn interval each tick (seconds)")]
    [SerializeField] private float _intervalReductionPerTick = 0.15f;
    [Tooltip("Minimum spawn interval (won't go below this)")]
    [SerializeField] private float _minSpawnInterval = 0.8f;

    [Header("Spawn Area")]
    [SerializeField] private float _spawnYOffset = 1f; // Above camera top
    [SerializeField] private float _spawnXLeft = -4f;
    [SerializeField] private float _spawnXRight = 4f;

    [Header("Camera Shake")]
    [Tooltip("Shake duration when an angry enemy drops from the sky")]
    [SerializeField] private float _angryDropShakeDuration = 0.15f;
    [Tooltip("Shake magnitude when an angry enemy drops from the sky")]
    [SerializeField] private float _angryDropShakeMagnitude = 0.1f;

    [Header("Event Channels")]
    [SerializeField] private EnemyFallChannel _onEnemyFell;

    private float _spawnTimer;
    private float _difficultyTimer;
    private float _currentSpawnInterval;

    private void Start()
    {
        _currentSpawnInterval = _spawnInterval;
        _spawnTimer = _currentSpawnInterval;
        _difficultyTimer = _difficultyTickInterval;
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
        // Difficulty scaling
        _difficultyTimer -= Time.deltaTime;
        if (_difficultyTimer <= 0f)
        {
            _difficultyTimer = _difficultyTickInterval;
            _currentSpawnInterval = Mathf.Max(
                _minSpawnInterval,
                _currentSpawnInterval - _intervalReductionPerTick
            );
            Debug.Log($"[EnemySpawner] Difficulty up! Spawn interval: {_currentSpawnInterval:F2}s");
        }

        // Spawn timer
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnWalker();
            _spawnTimer = _currentSpawnInterval;
        }
    }

    private void SpawnWalker()
    {
        if (_walkerPrefab == null || Camera.main == null)
            return;

        float x = Random.value > 0.5f ? _spawnXRight : _spawnXLeft;
        float y = Camera.main.orthographicSize + _spawnYOffset;
        Instantiate(_walkerPrefab, new Vector2(x, y), Quaternion.identity);
    }

    /// <summary>
    /// Called when an enemy falls off the screen via the EnemyFallChannel.
    /// Respawns the enemy at the top of the screen as angry (if not already).
    /// </summary>
    private void HandleEnemyFell(EnemyFallData data)
    {
        if (data.enemyData == null || Camera.main == null) return;

        // Pick the right prefab based on enemy move type
        GameObject prefab = GetPrefabForMoveType(data.enemyData.moveType);
        if (prefab == null)
        {
            Debug.LogWarning($"[EnemySpawner] No prefab for move type: {data.enemyData.moveType}");
            return;
        }

        // Spawn at random X, just above camera top
        float x = Random.Range(_spawnXLeft, _spawnXRight);
        float y = Camera.main.transform.position.y + Camera.main.orthographicSize + _spawnYOffset;

        GameObject newEnemy = Instantiate(prefab, new Vector2(x, y), Quaternion.identity);
        BaseEnemy baseEnemy = newEnemy.GetComponent<BaseEnemy>();

        if (baseEnemy != null)
        {
            // Always mark as angry on respawn
            baseEnemy.MarkAsRespawned();

            // Camera shake when angry enemy drops from the sky
            if (CameraShake.Instance != null)
                CameraShake.Instance.Shake(_angryDropShakeDuration, _angryDropShakeMagnitude);

            Debug.Log($"[EnemySpawner] Respawned angry {data.enemyData.enemyName} at ({x:F1}, {y:F1}). Was already angry: {data.wasAngry}");
        }
    }

    private GameObject GetPrefabForMoveType(EnemyMoveType moveType)
    {
        return moveType switch
        {
            EnemyMoveType.Walker => _walkerPrefab,
            EnemyMoveType.Flyer => _flyerPrefab,
            _ => _walkerPrefab // Fallback
        };
    }
}
