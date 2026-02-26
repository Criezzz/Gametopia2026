using UnityEngine;

/// <summary>
/// Manages player death. One hit = die game over.
/// Raises OnPlayerDied event.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _onPlayerDied;
    [SerializeField] private float _pitDeathOffset = 0.5f;
    [SerializeField] private float _spawnGraceDuration = 0.5f;

    private bool _isDead;
    private float _graceTimer;
    private Camera _mainCamera;

    private void Start()
    {
        _isDead = false;
        _graceTimer = _spawnGraceDuration;
        _mainCamera = Camera.main;
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
        if (_isDead || _graceTimer > 0f) return;

        Die();
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log("[PlayerHealth] Player died in 1 hit!");

        // Freeze the player: stop movement, disable physics
        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        // Hide weapon
        var toolHandler = GetComponent<PlayerToolHandler>();
        if (toolHandler != null) toolHandler.OnPlayerDied();

        _onPlayerDied?.Raise();
    }

    public void ResetHealth()
    {
        _isDead = false;
        _graceTimer = _spawnGraceDuration;

        var controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = true;

        var toolHandler = GetComponent<PlayerToolHandler>();
        if (toolHandler != null) toolHandler.OnPlayerReset();
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
