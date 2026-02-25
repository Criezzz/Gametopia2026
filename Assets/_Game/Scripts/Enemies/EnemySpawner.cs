using UnityEngine;

/// <summary>
/// Temporary test spawner: spawns WalkerEnemy every fixed interval.
/// Advanced spawn logic (difficulty scaling, enemy mix, etc.) will be added later.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject _walkerPrefab;
    [SerializeField] private float _spawnInterval = 3f;

    [Header("Spawn Area")]
    [SerializeField] private float _spawnYOffset = 1f; // Above camera top
    [SerializeField] private float _spawnXLeft = -5f;
    [SerializeField] private float _spawnXRight = 5f;

    private float _spawnTimer;

    private void Start()
    {
        _spawnTimer = _spawnInterval;
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
}
