using UnityEngine;

/// <summary>
/// Generic projectile for tools (Screwdriver, Nail Gun, Vacuum shot).
/// Flies in a direction, damages enemy on hit, then destroys itself (if no pierce).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class ToolProjectile : MonoBehaviour
{
    private int _damage;
    private bool _pierce;
    private float _timer;
    private float _lifetime = 5f;
    private Rigidbody2D _rb;
    private ToolData _toolData;
    private static readonly int GroundLayer = 8;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction, float speed, int damage, bool pierce, ToolData toolData = null)
    {
        _damage = damage;
        _pierce = pierce;
        _toolData = toolData;
        _timer = _lifetime;

        Vector2 dir = direction.normalized;
        _rb.linearVelocity = dir * speed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetRotating(bool rotating)
    {
        _rb.freezeRotation = !rotating;
        if (rotating)
            _rb.angularVelocity = 720f;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == GroundLayer)
        {
            Destroy(gameObject);
            return;
        }

        var enemy = other.GetComponentInParent<BaseEnemy>();
        if (enemy != null)
        {
            Vector2 hitPoint = other.ClosestPoint(transform.position);
            Vector2 hitDirection = (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.0001f)
                ? _rb.linearVelocity.normalized
                : (Vector2)transform.right;

            enemy.TakeDamage(_damage, _toolData, hitPoint, hitDirection);
            if (!_pierce)
                Destroy(gameObject);
        }
    }
}
