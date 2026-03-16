using UnityEngine;

/// Player health. One-hit kill. Raises OnPlayerDied event with player index.
/// Triggers death fall animation (same as enemy) when dying.
public class PlayerHealth : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private IntEventChannel _onPlayerDied;
    [SerializeField] private DeathDropChannel _onDeathDrop;
    [SerializeField] private float _pitDeathOffset = 0.5f;
    [SerializeField] private float _spawnGraceDuration = 0.5f;
    [SerializeField] private bool _trailer = false;
    private bool _isDead;
    private bool _deathNotified;
    private float _graceTimer;
    private Camera _mainCamera;
    private int _cachedPlayerIndex;

    private void Start()
    {
        _isDead = false;
        _deathNotified = false;
        _graceTimer = _spawnGraceDuration;
        _mainCamera = Camera.main;
        var controller = GetComponent<PlayerController>();
        _cachedPlayerIndex = controller != null ? controller.PlayerIndex : 0;
    }

    private void Update()
    {
        if (_graceTimer > 0f)
        {
            _graceTimer -= Time.deltaTime;
            return;
        }

        if (!_isDead && IsBelowCameraPit())
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        if(_trailer) return;
        if (_isDead || _graceTimer > 0f) return;

        Die();
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log("[PlayerHealth] Player died in 1 hit!");

        bool hasDeathDrop = false;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (_onDeathDrop != null && sr != null && sr.sprite != null)
        {
            hasDeathDrop = true;
            _onDeathDrop.Raise(new DeathDropData
            {
                sprite = sr.sprite,
                position = transform.position,
                onComplete = HandleDeathDropFinished
            });
        }

        foreach (var r in GetComponentsInChildren<SpriteRenderer>())
            r.enabled = false;

        var tagUI = GetComponentInChildren<PlayerTagUI>(true);
        if (tagUI != null) tagUI.gameObject.SetActive(false);

        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        var toolHandler = GetComponent<PlayerToolHandler>();
        if (toolHandler != null) toolHandler.OnPlayerDied();

        if (!hasDeathDrop)
            HandleDeathDropFinished();
    }

    public void ResetHealth()
    {
        _isDead = false;
        _deathNotified = false;
        _graceTimer = _spawnGraceDuration;

        foreach (var r in GetComponentsInChildren<SpriteRenderer>())
            r.enabled = true;

        var tagUI = GetComponentInChildren<PlayerTagUI>(true);
        if (tagUI != null) tagUI.gameObject.SetActive(true);

        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = true;

        var toolHandler = GetComponent<PlayerToolHandler>();
        if (toolHandler != null) toolHandler.OnPlayerReset();
    }

    private void HandleDeathDropFinished()
    {
        if (!this) return;
        if (_deathNotified) return;
        _deathNotified = true;

        _onPlayerDied?.Raise(_cachedPlayerIndex);
    }

    private void OnDestroy()
    {
        _deathNotified = true;
    }

    private bool IsBelowCameraPit()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera != null)
        {
            float bottomY = _mainCamera.transform.position.y - _mainCamera.orthographicSize;
            return transform.position.y < (bottomY - _pitDeathOffset);
        }

        // Fallback if camera is missing
        return transform.position.y < -8f;
    }
}
