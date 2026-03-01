using UnityEngine;

/// <summary>
/// Player platformer controller with gravity-based jump physics.
/// Variable jump height: hold jump for full height, tap for short hop.
/// Physics derived from PlayerData (maxJumpHeight, timeToMaxHeight).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerData _data;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheckPoint;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckRadius = 0.15f;

    // Components
    private Rigidbody2D _rb;
    private BoxCollider2D _boxCollider;
    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private readonly RaycastHit2D[] _ceilingHits = new RaycastHit2D[8];
    private readonly ContactPoint2D[] _groundContacts = new ContactPoint2D[8];

    // Animator params for movement state machine
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    // State
    private float _moveInput;
    private bool _isGrounded;
    private bool _jumpRequested;
    private bool _jumpCut;
    private float _velocityY;
    private int _facingDirection = 1; // 1 = right, -1 = left
    public int FacingDirection => _facingDirection;

    // Computed jump physics (cached from PlayerData on Awake)
    private float _gravity;
    private float _initialJumpVelocity;

    private const float CEILING_CHECK_DISTANCE = 0.05f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        CacheJumpPhysics();
    }

    /// <summary>
    /// Derive gravity and initial jump velocity from designer-friendly SO values.
    /// g = 2h / t²   |   v₀ = 2h / t
    /// </summary>
    private void CacheJumpPhysics()
    {
        float h = _data != null ? _data.maxJumpHeight : 3.5f;
        float t = _data != null ? _data.timeToMaxHeight : 0.25f;
        t = Mathf.Max(0.01f, t);

        _gravity = 2f * h / (t * t);
        _initialJumpVelocity = 2f * h / t;
    }

    private void Update()
    {
        _moveInput = Input.GetAxisRaw("Horizontal");

        // Jump request (consumed in FixedUpdate)
        bool jumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        if (jumpDown && _isGrounded)
            _jumpRequested = true;

        // Variable jump: releasing the button early cuts upward velocity once
        bool jumpUp = Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);
        if (jumpUp && _velocityY > 0f && !_isGrounded)
            _jumpCut = true;

        // Facing direction
        if (_moveInput > 0)
        {
            _facingDirection = 1;
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = false;
        }
        else if (_moveInput < 0)
        {
            _facingDirection = -1;
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = true;
        }
    }

    private void FixedUpdate()
    {
        GroundCheck();

        // Horizontal
        float moveSpeed = _data != null ? _data.moveSpeed : 6f;
        float targetVelX = _moveInput * moveSpeed;

        // --- Jump start ---
        if (_jumpRequested)
        {
            _velocityY = _initialJumpVelocity;
            _jumpCut = false;
            _jumpRequested = false;

            // Jump SFX
            if (_data != null && _data.jumpSFX != null && SFXManager.Instance != null)
                SFXManager.Instance.Play(_data.jumpSFX);
        }

        // --- Jump cut (variable height) ---
        if (_jumpCut)
        {
            if (_velocityY > 0f)
            {
                float cut = _data != null ? _data.jumpCutMultiplier : 0.5f;
                _velocityY *= cut;
            }
            _jumpCut = false;
        }

        // --- Gravity ---
        float gravMul = (_velocityY <= 0f)
            ? (_data != null ? _data.fallGravityMultiplier : 1.5f)
            : 1f;
        _velocityY -= _gravity * gravMul * Time.fixedDeltaTime;

        // Terminal velocity
        float maxFall = _data != null ? _data.maxFallSpeed : 25f;
        if (_velocityY < -maxFall)
            _velocityY = -maxFall;

        // Ceiling
        if (_velocityY > 0f && IsTouchingCeiling())
            _velocityY = 0f;

        // Ground snap — only when physically resting on ground (not just raycast)
        if (_velocityY < 0f && IsPhysicallyOnGround())
            _velocityY = 0f;

        _rb.linearVelocity = new Vector2(targetVelX, _velocityY);

        UpdateMovementAnimation(targetVelX);
    }

    private void GroundCheck()
    {
        LayerMask layer = ResolveGroundLayerMask();
        float probeDistance = _data != null ? _data.groundCheckRadius : _groundCheckRadius;
        probeDistance = Mathf.Max(0.02f, probeDistance);

        float sideOffset = _boxCollider != null ? _boxCollider.bounds.extents.x * 0.35f : 0.12f;
        Vector2 center = _groundCheckPoint != null
            ? (Vector2)_groundCheckPoint.position
            : (_boxCollider != null
                ? new Vector2(_boxCollider.bounds.center.x, _boxCollider.bounds.min.y + 0.02f)
                : (Vector2)transform.position);

        RaycastHit2D centerHit = Physics2D.Raycast(center, Vector2.down, probeDistance, layer);
        RaycastHit2D leftHit = Physics2D.Raycast(center + Vector2.left * sideOffset, Vector2.down, probeDistance, layer);
        RaycastHit2D rightHit = Physics2D.Raycast(center + Vector2.right * sideOffset, Vector2.down, probeDistance, layer);

        _isGrounded = IsGroundHit(centerHit) || IsGroundHit(leftHit) || IsGroundHit(rightHit);
    }

    /// <summary>
    /// Get the attack direction based on facing.
    /// </summary>
    public Vector2 GetAimDirection()
    {
        return new Vector2(_facingDirection, 0);
    }

    private void UpdateMovementAnimation(float horizontalVelocity)
    {
        if (_animator == null) return;

        _animator.SetFloat(SpeedHash, Mathf.Abs(horizontalVelocity));
        _animator.SetBool(IsGroundedHash, _isGrounded);
    }

    private LayerMask ResolveGroundLayerMask()
    {
        if (_data != null && _data.groundLayer.value != 0)
            return _data.groundLayer;

        if (_groundLayer.value != 0)
            return _groundLayer;

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0)
            return 1 << groundLayer;

        return ~0;
    }

    private bool IsGroundHit(RaycastHit2D hit)
    {
        return hit.collider != null && hit.normal.y > 0.7f;
    }

    /// <summary>
    /// Check actual physics contacts (not raycasts) for ground below.
    /// Used for velocity snapping — more accurate than raycasts for position.
    /// </summary>
    private bool IsPhysicallyOnGround()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(ResolveGroundLayerMask());

        int count = _rb.GetContacts(filter, _groundContacts);
        for (int i = 0; i < count; i++)
        {
            if (_groundContacts[i].normal.y > 0.7f)
                return true;
        }
        return false;
    }

    private bool IsTouchingCeiling()
    {
        if (_boxCollider == null)
            return false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(ResolveGroundLayerMask());

        int hitCount = _boxCollider.Cast(Vector2.up, filter, _ceilingHits, CEILING_CHECK_DISTANCE);
        for (int i = 0; i < hitCount; i++)
        {
            if (_ceilingHits[i].collider == null)
                continue;

            if (_ceilingHits[i].normal.y < -0.2f)
                return true;
        }

        return false;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Draw the actual BoxCollider2D bounds so you can see where it really is.
    /// Green = collider, Yellow = sprite bounds.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider2D>();
        if (_boxCollider != null)
        {
            Gizmos.color = Color.green;
            Vector2 center = (Vector2)transform.position + _boxCollider.offset;
            Vector2 size = _boxCollider.size;
            Gizmos.DrawWireCube(center, size);
        }

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(sr.bounds.center, sr.bounds.size);
        }
    }
#endif

}

