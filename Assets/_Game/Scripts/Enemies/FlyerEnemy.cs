using UnityEngine;

/// <summary>
/// Flyer enemy: flies horizontally, bounces off walls.
/// 1 unit size, 8 HP, flies in the air.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class FlyerEnemy : BaseEnemy
{
    private int _direction = 1; // 1 = right, -1 = left
    [SerializeField] private float _floatAmplitude = 0.3f;
    [SerializeField] private float _floatFrequency = 2f;

    private float _baseY;
    private float _timeOffset;

    protected override void Awake()
    {
        base.Awake();

        // Random initial direction and phase
        _direction = Random.value > 0.5f ? 1 : -1;
        _timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    protected override void Start()
    {
        base.Start();
        _baseY = transform.position.y;
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        float speed = _data != null ? _data.moveSpeed : 2f;

        // Apply respawn speed multiplier
        if (_isRespawned && _data != null)
            speed *= _data.respawnSpeedMultiplier;

        // Horizontal movement
        float velX = _direction * speed;

        // Sinusoidal vertical float
        float sinY = Mathf.Sin((Time.time + _timeOffset) * _floatFrequency) * _floatAmplitude;
        float targetY = _baseY + sinY;
        float velY = (targetY - transform.position.y) * 5f; // Smooth follow

        _rb.linearVelocity = new Vector2(velX, velY);

        // Flip sprite
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = _direction < 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Bounce off walls
        foreach (var contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.x) > 0.5f)
            {
                _direction *= -1;
                break;
            }
        }
    }
}