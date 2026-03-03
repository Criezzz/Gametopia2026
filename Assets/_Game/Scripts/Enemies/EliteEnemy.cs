using UnityEngine;

/// <summary>
/// Elite enemy: bounces around unpredictably off walls and platforms.
/// When respawned from bottom (angry), moves faster.
/// Uses EnemyMoveType.Bouncer.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class EliteEnemy : BaseEnemy
{
    [Header("Bounce Settings")]
    [Tooltip("Initial launch speed when spawned")]
    [SerializeField] private float _launchSpeed = 4f;
    [Tooltip("Minimum bounce speed — prevents the enemy from losing all momentum")]
    [SerializeField] private float _minBounceSpeed = 2f;

    private Vector2 _moveDirection;

    protected override void Awake()
    {
        base.Awake();

        // Apply gravity scale from EnemyData SO
        _rb.gravityScale = _data != null ? _data.gravityScale : 1f;

        // Random initial direction (angled downward)
        float angle = Random.Range(30f, 60f);
        float sign = Random.value > 0.5f ? 1f : -1f;
        _moveDirection = new Vector2(
            sign * Mathf.Cos(angle * Mathf.Deg2Rad),
            -Mathf.Sin(angle * Mathf.Deg2Rad)
        ).normalized;
    }

    protected override void Start()
    {
        base.Start();

        // Launch with initial velocity using configured launch speed
        _rb.linearVelocity = _moveDirection * _launchSpeed;
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        float speed = _data != null ? _data.moveSpeed : 3f;

        if (_isRespawned && _data != null)
            speed *= _data.respawnSpeedMultiplier;

        // Maintain minimum speed — prevent bouncer from stalling
        if (_rb.linearVelocity.magnitude < _minBounceSpeed && _rb.linearVelocity.magnitude > 0.01f)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * speed;
        }

        // Flip sprite to face movement direction
        if (_spriteRenderer != null && Mathf.Abs(_rb.linearVelocity.x) > 0.1f)
            _spriteRenderer.flipX = _rb.linearVelocity.x < 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float speed = _data != null ? _data.moveSpeed : 3f;

        if (_isRespawned && _data != null)
            speed *= _data.respawnSpeedMultiplier;

        // Reflect velocity off contact normal for unpredictable bouncing
        foreach (var contact in collision.contacts)
        {
            Vector2 reflected = Vector2.Reflect(_rb.linearVelocity.normalized, contact.normal);
            _rb.linearVelocity = reflected * speed;
            break; // Only use first contact point
        }
    }
}
