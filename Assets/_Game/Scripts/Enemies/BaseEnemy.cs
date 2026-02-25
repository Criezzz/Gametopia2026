using System.Collections;
using UnityEngine;

/// <summary>
/// Base enemy class. Handles HP, damage, death, and vacuum suck.
/// Subclasses (WalkerEnemy, FlyerEnemy) implement specific movement.
/// </summary>
public class BaseEnemy : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] protected EnemyData _data;

    [Header("State")]
    protected int _currentHP;
    protected bool _isDead;
    protected bool _isRespawned; // True if fell to bottom and came back stronger

    [Header("Vacuum")]
    [SerializeField] private bool _isSmall = true; // Can be sucked by Vacuum
    public bool IsSmall => _isSmall;
    private bool _beingSucked;

    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _onEnemyKilled;

    [Header("Animation")]
    [Tooltip("Optional Animator for hit/death animations.")]
    [SerializeField] protected Animator _animator;
    [SerializeField] private Color _hitFlashColor = Color.white;
    [SerializeField] private float _hitFlashDuration = 0.1f;

    protected SpriteRenderer _spriteRenderer;
    protected Rigidbody2D _rb;
    private Coroutine _flashCoroutine;

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        if (_data != null)
        {
            _currentHP = _data.maxHP;
        }
        else
        {
            _currentHP = 1;
        }
        _isDead = false;
    }

    public virtual void TakeDamage(int damage)
    {
        if (_isDead) return;

        _currentHP -= damage;

        // Visual Hit Feedback
        if (_animator != null)
        {
            _animator.SetTrigger("Hit"); // Trigger animation if you have one
        }

        if (_spriteRenderer != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        if (_currentHP <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        Color originalColor = _isRespawned && _data != null ? _data.respawnTint : Color.white;
        _spriteRenderer.color = _hitFlashColor;
        yield return new WaitForSeconds(_hitFlashDuration);
        _spriteRenderer.color = originalColor;
    }

    protected virtual void Die()
    {
        _isDead = true;
        
        // Broadcast Death Event
        _onEnemyKilled?.Raise();

        if (_animator != null)
        {
            _animator.SetTrigger("Die");
            // If using animator, you might want to delay Destroy or handle it via Animation Event.
            // For now, we destroy after a tiny delay so the audio/particles can trigger.
            // Or just destroy immediately if it's a fast-paced arcade style:
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Called by Vacuum tool — suck this enemy toward a point.
    /// </summary>
    public virtual void GetSucked(Vector2 targetPosition)
    {
        if (_beingSucked) return;
        _beingSucked = true;

        // Disable normal movement
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0f;
        }

        // Move toward target
        transform.position = targetPosition;
        gameObject.SetActive(false); // Hide until shot out
    }

    /// <summary>
    /// Mark this enemy as a respawned (angrier) version.
    /// </summary>
    public void MarkAsRespawned()
    {
        _isRespawned = true;
        if (_data != null && _spriteRenderer != null)
        {
            _spriteRenderer.color = _data.respawnTint;
        }

        // Increase speed
        // Subclasses should check _isRespawned for speed multiplier
    }
}