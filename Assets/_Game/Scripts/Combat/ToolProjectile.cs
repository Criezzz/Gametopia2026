using UnityEngine;

/// <summary>
/// Generic projectile for tools (Screwdriver, Nail Gun, Vacuum shot).
/// Flies in a direction, damages enemy on hit, then destroys itself (if no pierce).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class ToolProjectile : MonoBehaviour
{
    private Vector2 _direction;
    private float _speed;
    private int _damage;
    private bool _pierce;
    private bool _rotating;
    private float _lifetime = 5f;
    private float _timer;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.freezeRotation = !_rotating;

        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    /// <summary>
    /// Initialize projectile direction, speed, damage, and pierce.
    /// </summary>
    public void Initialize(Vector2 direction, float speed, int damage, bool pierce)
    {
        _direction = direction.normalized;
        _speed = speed;
        _damage = damage;
        _pierce = pierce;
        _timer = _lifetime;

        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();

        _rb.linearVelocity = _direction * _speed;

        // Rotate sprite to face direction
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Enable continuous rotation (for Vacuum enemy projectiles).
    /// </summary>
    public void SetRotating(bool rotating)
    {
        _rotating = rotating;
        if (_rb != null)
            _rb.freezeRotation = !rotating;

        if (rotating)
            _rb.angularVelocity = 720f; // 2 rotations per second
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<BaseEnemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(_damage);

            if (!_pierce)
            {
                Destroy(gameObject);
            }
        }
    }
}