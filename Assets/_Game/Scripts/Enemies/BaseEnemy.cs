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
    private Coroutine _suckCoroutine;

    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _onEnemyKilled;
    [SerializeField] private EnemyFallChannel _onEnemyFell;
    [SerializeField] private DeathDropChannel _onDeathDrop;

    [Header("Fall Detection")]
    [Tooltip("How far below the camera bottom before triggering fall respawn")]
    [SerializeField] private float _fallOffset = 3f;

    [Header("Animation")]
    [Tooltip("Optional Animator for hit/death animations.")]
    [SerializeField] protected Animator _animator;
    [SerializeField] private Color _hitFlashColor = Color.black;
    [SerializeField] private float _hitFlashDuration = 0.12f;
    [SerializeField] private int _hitFlashCount = 2;

    [Header("Angry VFX")]
    [Tooltip("Child GameObject with SpriteRenderer + Animator for angry VFX. Disabled by default, enabled on MarkAsRespawned.")]
    [SerializeField] private GameObject _angryVFX;

    private BoxCollider2D _boxCollider;

    protected SpriteRenderer _spriteRenderer;
    protected Rigidbody2D _rb;
    private Coroutine _flashCoroutine;
    private int _maxHP; // Runtime max HP (for health bar ratio)
    private EnemyHealthBar _healthBar;

    protected virtual void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();

        // Auto-create health bar if not present on prefab
        _healthBar = GetComponentInChildren<EnemyHealthBar>();
        if (_healthBar == null)
        {
            GameObject hbObj = new GameObject("HealthBar");
            hbObj.transform.SetParent(transform, false);
            _healthBar = hbObj.AddComponent<EnemyHealthBar>();
        }
    }

    protected virtual void Start()
    {
        if (_data != null)
        {
            _maxHP = _data.maxHP;
            _currentHP = _maxHP;

            // Auto-assign animator controller from EnemyData SO if not already set
            if (_animator != null && _data.animatorController != null
                && _animator.runtimeAnimatorController == null)
            {
                _animator.runtimeAnimatorController = _data.animatorController;
            }
        }
        else
        {
            _maxHP = 1;
            _currentHP = 1;
        }
        _isDead = false;
        UpdateHealthBar();
    }

    protected virtual void Update()
    {
        if (_isDead || _beingSucked) return;
        CheckFallOffScreen();
        CheckContactDamage();
    }

    /// <summary>
    /// Overlap-based contact damage (player has 1 HP, so just check once per frame).
    /// Works even though Player↔Enemy physics collision is disabled.
    /// </summary>
    private void CheckContactDamage()
    {
        if (_boxCollider == null) return;

        Collider2D hit = Physics2D.OverlapBox(
            _boxCollider.bounds.center,
            _boxCollider.bounds.size,
            0f,
            1 << 0 // Default layer (Player)
        );

        if (hit != null)
        {
            var playerHealth = hit.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                int dmg = _data != null ? _data.contactDamage : 1;
                playerHealth.TakeDamage(dmg);
            }
        }
    }

    /// <summary>
    /// Checks if the enemy has fallen below the camera's visible area.
    /// If so, raises the fall event for respawn and destroys itself.
    /// </summary>
    private void CheckFallOffScreen()
    {
        if (Camera.main == null) return;

        float cameraBottom = Camera.main.transform.position.y - Camera.main.orthographicSize;
        if (transform.position.y < cameraBottom - _fallOffset)
        {
            HandleFellOffScreen();
        }
    }

    private void HandleFellOffScreen()
    {
        Debug.Log($"[BaseEnemy] {(_data != null ? _data.enemyName : "Unknown")} fell off screen! Angry={_isRespawned}");

        if (_onEnemyFell != null && _data != null)
        {
            _onEnemyFell.Raise(new EnemyFallData
            {
                enemyData = _data,
                wasAngry = _isRespawned
            });
        }

        Destroy(gameObject);
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

        UpdateHealthBar();
    }

    private IEnumerator HitFlashRoutine()
    {
        Color originalColor = _isRespawned && _data != null ? _data.respawnTint : Color.white;

        for (int i = 0; i < _hitFlashCount; i++)
        {
            _spriteRenderer.color = _hitFlashColor;
            yield return new WaitForSeconds(_hitFlashDuration * 0.5f);
            _spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(_hitFlashDuration * 0.5f);
        }
    }

    protected virtual void Die()
    {
        _isDead = true;

        // Disable collider immediately to prevent phantom contact damage
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        
        // Broadcast Death Event
        _onEnemyKilled?.Raise();

        // Raise death-drop event so a falling sprite is spawned
        if (_onDeathDrop != null && _onDeathDrop.HasListeners && _spriteRenderer != null)
        {
            _onDeathDrop.Raise(new DeathDropData
            {
                sprite = _spriteRenderer.sprite,
                position = transform.position
            });
        }

        // Hide health bar on death
        if (_healthBar != null) _healthBar.gameObject.SetActive(false);

        // Destroy immediately — death drop handles the visual
        Destroy(gameObject);
    }

    /// <summary>
    /// Called by Vacuum tool — smooth pull enemy toward target + scale down.
    /// Enemy manages its own pull animation via coroutine.
    /// </summary>
    /// <param name="target">Transform to pull toward (updates each frame as player moves)</param>
    /// <param name="duration">How long the pull + scale-down animation takes</param>
    /// <param name="pullSpeed">Movement speed toward target (units/s)</param>
    /// <param name="onAbsorbed">Callback when enemy is fully absorbed</param>
    public virtual void GetSucked(Transform target, float duration, float pullSpeed, System.Action onAbsorbed)
    {
        if (_beingSucked) return;
        _beingSucked = true;

        // Disable physics & collision immediately
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.gravityScale = 0f;
        }
        if (_boxCollider != null)
            _boxCollider.enabled = false;

        _suckCoroutine = StartCoroutine(SuckAnimationRoutine(target, duration, pullSpeed, onAbsorbed));
    }

    /// <summary>
    /// Legacy overload — instant teleport. Kept for backward compatibility.
    /// </summary>
    [System.Obsolete("Use GetSucked(Transform, float, float, Action) for smooth pull")]
    public virtual void GetSucked(Vector2 targetPosition)
    {
        if (_beingSucked) return;
        _beingSucked = true;
        if (_rb != null) { _rb.linearVelocity = Vector2.zero; _rb.gravityScale = 0f; }
        if (_boxCollider != null) _boxCollider.enabled = false;
        transform.position = targetPosition;
        gameObject.SetActive(false);
    }

    private IEnumerator SuckAnimationRoutine(Transform target, float duration, float pullSpeed, System.Action onAbsorbed)
    {
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        // Hide health bar during suck
        if (_healthBar != null)
            _healthBar.gameObject.SetActive(false);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Move toward target (read position each frame — player moves)
            if (target != null)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    target.position,
                    pullSpeed * Time.deltaTime
                );
            }

            // Scale down: original → zero
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

            yield return null;
        }

        // Absorb complete
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
        onAbsorbed?.Invoke();
    }

    /// <summary>
    /// Mark this enemy as a respawned (angrier) version.
    /// Applies angry sprite, animation controller, tint, and enables VFX.
    /// </summary>
    public void MarkAsRespawned()
    {
        _isRespawned = true;

        if (_data == null) return;

        // Heal to full HP on angry respawn
        _currentHP = _maxHP;
        UpdateHealthBar();

        // Apply angry sprite if available
        if (_data.angrySprite != null && _spriteRenderer != null)
            _spriteRenderer.sprite = _data.angrySprite;

        // Apply tint
        if (_spriteRenderer != null)
            _spriteRenderer.color = _data.respawnTint;

        // Trigger angry animation state via IsAngry bool parameter
        // The single animator controller handles the movingNormal -> movingAngry transition
        if (_animator != null)
            _animator.SetBool("IsAngry", true);

        // Enable angry VFX child (looping animation on top of enemy's head)
        if (_angryVFX != null)
            _angryVFX.SetActive(true);
    }

    /// <summary>
    /// Update the floating health bar (if present).
    /// </summary>
    private void UpdateHealthBar()
    {
        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, _maxHP);
    }
}