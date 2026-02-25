using UnityEngine;

/// <summary>
/// Walker enemy: walks on platforms, falls off edges.
/// When respawned from bottom, moves faster.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class WalkerEnemy : BaseEnemy
{
    private int _direction = 1; // 1 = right, -1 = left

    protected override void Awake()
    {
        base.Awake();

        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();

        _rb.freezeRotation = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Random initial direction
        _direction = Random.value > 0.5f ? 1 : -1;
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        float speed = _data != null ? _data.moveSpeed : 2f;

        if (_isRespawned && _data != null)
            speed *= _data.respawnSpeedMultiplier;

        _rb.linearVelocity = new Vector2(_direction * speed, _rb.linearVelocity.y);

        // Flip sprite to face movement direction
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = _direction < 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Bounce off walls
        foreach (var contact in collision.contacts)
        {
            // If we hit a wall (contact normal is horizontal)
            if (Mathf.Abs(contact.normal.x) > 0.5f)
            {
                _direction *= -1;
                break;
            }
        }
    }

    /// <summary>
    /// Deals contact damage to player.
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        var playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            int dmg = _data != null ? _data.contactDamage : 1;
            playerHealth.TakeDamage(dmg);
        }
    }
}
