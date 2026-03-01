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
    private bool _hasLanded;      // No horizontal movement until first ground contact

    protected override void Awake()
    {
        base.Awake();

        // Apply gravity scale from EnemyData SO (runtime config)
        _rb.gravityScale = _data != null ? _data.gravityScale : 3f;

        // Random initial direction
        _direction = Random.value > 0.5f ? 1 : -1;
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        float speed = _data != null ? _data.moveSpeed : 3f;

        if (_isRespawned && _data != null)
            speed *= _data.respawnSpeedMultiplier;

        float velX = _hasLanded ? _direction * speed : 0f;
        _rb.linearVelocity = new Vector2(velX, _rb.linearVelocity.y);

        // Flip sprite to face movement direction
        if (_hasLanded && _spriteRenderer != null)
            _spriteRenderer.flipX = _direction < 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        foreach (var contact in collision.contacts)
        {
            // Landed on a surface (normal pointing up)
            if (!_hasLanded && contact.normal.y > 0.5f)
            {
                _hasLanded = true;
            }

            // Bounce off walls — only after landing
            if (_hasLanded && Mathf.Abs(contact.normal.x) > 0.5f)
            {
                _direction *= -1;
                break;
            }
        }
    }
}
