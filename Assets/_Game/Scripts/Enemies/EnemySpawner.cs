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

    [Header("Spawn Area")]
    [SerializeField] private float _spawnYOffset = 1f; // Above camera top
    [SerializeField] private float _spawnXLeft = -4f;
    [SerializeField] private float _spawnXRight = 4f;

    [Header("Event Channels")]
    [SerializeField] private EnemyFallChannel _onEnemyFell;

    private float _spawnTimer;

    private void Start()
    {
        _spawnTimer = _spawnInterval;
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
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer <= 0f)
        {
            SpawnWalker();
            _spawnTimer = _spawnInterval;
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
