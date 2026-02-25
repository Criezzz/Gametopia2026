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

    private bool _isDead;
    private Camera _mainCamera;

    private void Start()
    {
        _isDead = false;
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!_isDead && IsBelowCameraPit())
        {
            Die();
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        Die();
    }

    private void Die()
    {
        _isDead = true;
        Debug.Log("[PlayerHealth] Player died in 1 hit!");
        _onPlayerDied?.Raise();
    }

    public void ResetHealth()
    {
        _isDead = false;
        var r = GetComponentInChildren<SpriteRenderer>();
        if (r != null) r.enabled = true;
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
