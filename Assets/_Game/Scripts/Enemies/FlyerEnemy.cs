using UnityEngine;

/// <summary>
/// Flyer enemy: homing flight toward the player with soft bounce off platforms.
/// Bounces lightly off obstacles but quickly re-orients toward player.
/// Inspired by Super Crate Box flying enemy behavior.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class FlyerEnemy : BaseEnemy
{
    [Header("Tracking")]
    [Tooltip("Seconds between re-targeting the player (normal flight)")]
    [SerializeField] private float _retargetInterval = 1.5f;
    [Tooltip("Seconds before re-targeting after bouncing off something")]
    [SerializeField] private float _retargetAfterBounce = 0.3f;

    [Header("Flight")]
    [Tooltip("How quickly the flyer turns toward target direction (degrees/sec)")]
    [SerializeField] private float _turnSpeed = 250f;
    [Tooltip("How much of the bounce reflection to keep (0 = ignore bounce, 1 = full reflect)")]
    [Range(0f, 1f)]
    [SerializeField] private float _bounceStrength = 0.4f;
   
    private Vector2 _moveDirection;
    private float _retargetTimer;
    private Transform _playerTarget;

    protected override void Awake()
    {
        base.Awake();

        // No gravity — flyer moves entirely via velocity
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;

        // Random initial direction (downward bias so it enters from top)
        float angle = Random.Range(20f, 70f);
        float sign = Random.value > 0.5f ? 1f : -1f;
        _moveDirection = new Vector2(
            sign * Mathf.Cos(angle * Mathf.Deg2Rad),
            -Mathf.Sin(angle * Mathf.Deg2Rad)
        ).normalized;
    }

    protected override void Start()
    {
        base.Start();

        // Find player once
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            _playerTarget = player.transform;

        _retargetTimer = _retargetInterval;
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        float speed = _data != null ? _data.moveSpeed : 3f;
        if (_isRespawned && _data != null)
            speed *= _data.respawnSpeedMultiplier;

        // Re-target toward player periodically
        _retargetTimer -= Time.fixedDeltaTime;
        if (_retargetTimer <= 0f)
        {
            _retargetTimer = _retargetInterval;
            RetargetPlayer();
        }

        // Smooth turn toward desired direction
        float maxRotation = _turnSpeed * Time.fixedDeltaTime;
        Vector2 currentDir = _rb.linearVelocity.magnitude > 0.1f
            ? _rb.linearVelocity.normalized
            : _moveDirection;
        float currentAngle = Mathf.Atan2(currentDir.y, currentDir.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxRotation);
        Vector2 newDir = new Vector2(
            Mathf.Cos(newAngle * Mathf.Deg2Rad),
            Mathf.Sin(newAngle * Mathf.Deg2Rad)
        );

        _rb.linearVelocity = newDir * speed;

        // Flip sprite to face movement direction
        if (_spriteRenderer != null && Mathf.Abs(_rb.linearVelocity.x) > 0.1f)
            _spriteRenderer.flipX = _rb.linearVelocity.x < 0;
    }

    private void RetargetPlayer()
    {
        if (_playerTarget == null) return;
        Vector2 toPlayer = ((Vector2)_playerTarget.position - (Vector2)transform.position);
        if (toPlayer.magnitude > 0.5f)
            _moveDirection = toPlayer.normalized;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Don't bounce off the player
        if (collision.collider.GetComponent<PlayerController>() != null)
            return;

        float speed = _data != null ? _data.moveSpeed : 3f;
        if (_isRespawned && _data != null)
            speed *= _data.respawnSpeedMultiplier;

        foreach (var contact in collision.contacts)
        {
            // Blend: partial bounce + bias toward player
            Vector2 reflected = Vector2.Reflect(_rb.linearVelocity.normalized, contact.normal);
            Vector2 toPlayer = _playerTarget != null
                ? ((Vector2)_playerTarget.position - (Vector2)transform.position).normalized
                : reflected;

            // Mix reflected direction with player direction
            _moveDirection = Vector2.Lerp(toPlayer, reflected, _bounceStrength).normalized;
            _rb.linearVelocity = _moveDirection * speed;

            // Short delay before re-targeting (let bounce play out briefly)
            _retargetTimer = _retargetAfterBounce;
            break;
        }
    }
}