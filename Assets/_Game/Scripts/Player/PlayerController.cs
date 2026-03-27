using UnityEngine;

/// Player platformer controller. Variable jump height via PlayerData.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerData _data;

    [Header("Identity")]
    [Tooltip("0 = Player 1, 1 = Player 2. Set per prefab.")]
    [SerializeField] private int _playerIndex = 0;
    public int PlayerIndex => _playerIndex;

    [Header("Input")]
    [SerializeField] private PlayerInputHandler _inputHandler;

    [Header("VFX")]
    [Tooltip("Optional one-shot VFX prefab spawned when jump starts.")]
    [SerializeField] private GameObject _jumpVFXPrefab;

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
    private bool _isPhysicallyGrounded;
    private bool _jumpConsumedUntilGrounded;
    private bool _jumpCut;
    private float _velocityY;
    private float _jumpCooldownTimer;
    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private int _facingDirection = 1; // 1 = right, -1 = left
    public int FacingDirection => _facingDirection;

    // Computed jump physics (cached from PlayerData on Awake)
    private float _gravity;
    private float _initialJumpVelocity;

    private const float CEILING_CHECK_DISTANCE = 0.05f;
    private const float DEFAULT_COYOTE_TIME = 0.10f;
    private const float DEFAULT_JUMP_BUFFER_TIME = 0.10f;
    private const float JUMP_REPEAT_COOLDOWN = 0.15f;
    private Collider2D[] _cachedColliders;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _boxCollider = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        if (_inputHandler == null)
            _inputHandler = GetComponent<PlayerInputHandler>();

        ApplyPlayerCollisionSetup();

        CacheJumpPhysics();
    }

    private void Start()
    {
        // Ensure all PlayerController instances are present before pair-wise ignore setup.
        ApplyPlayerCollisionSetup();
    }

    private void OnEnable()
    {
        ApplyPlayerCollisionSetup();
    }

    // g = 2h / t²  |  v₀ = 2h / t
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
        if (_inputHandler == null) return;

        _moveInput = _inputHandler.MoveInput;

        // Jump request (consumed in FixedUpdate)
        // JumpPressed = tap; JumpHeld only refreshes buffer while physically grounded.
        if (_jumpCooldownTimer > 0f) _jumpCooldownTimer -= Time.deltaTime;
        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;
        if (_inputHandler.JumpPressed)
        {
            _jumpBufferTimer = GetJumpBufferTime();
        }
        else if (_inputHandler.JumpHeld && _isPhysicallyGrounded)
        {
            _jumpBufferTimer = GetJumpBufferTime();
        }

        // Variable jump: releasing early cuts upward velocity
        if (_inputHandler.JumpReleased && _velocityY > 0f && !_isGrounded)
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
        _isPhysicallyGrounded = IsPhysicallyOnGround();

        // Reset jump-consumed only after a real landing (not while still rising at jump start).
        if (_isPhysicallyGrounded && _velocityY <= 0f)
            _jumpConsumedUntilGrounded = false;

        UpdateCoyoteTimer();

        // Horizontal
        float moveSpeed = _data != null ? _data.moveSpeed : 6f;
        float targetVelX = _moveInput * moveSpeed;

        // --- Jump start ---
        if (!_jumpConsumedUntilGrounded
            && _jumpBufferTimer > 0f
            && _coyoteTimer > 0f
            && _jumpCooldownTimer <= 0f)
        {
            StartJump();
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
        if (_velocityY < 0f && _isPhysicallyGrounded)
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

    private void UpdateCoyoteTimer()
    {
        if (_isPhysicallyGrounded)
        {
            _coyoteTimer = GetCoyoteTime();
            return;
        }

        if (_coyoteTimer > 0f)
            _coyoteTimer -= Time.fixedDeltaTime;
    }

    private void StartJump()
    {
        _velocityY = _initialJumpVelocity;
        _jumpCut = false;
        _jumpConsumedUntilGrounded = true;
        _coyoteTimer = 0f;
        _jumpBufferTimer = 0f;
        _jumpCooldownTimer = JUMP_REPEAT_COOLDOWN;

        // Jump VFX
        if (_jumpVFXPrefab != null)
            VFXSpawner.Spawn(_jumpVFXPrefab, transform.position, _jumpVFXPrefab.transform.rotation);

        // Jump SFX
        if (_data != null && _data.jumpSFX != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(_data.jumpSFX);
    }

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

    private float GetCoyoteTime()
    {
        if (_data != null)
            return Mathf.Max(0f, _data.coyoteTime);
        return DEFAULT_COYOTE_TIME;
    }

    private float GetJumpBufferTime()
    {
        if (_data != null)
            return Mathf.Max(0f, _data.jumpBufferTime);
        return DEFAULT_JUMP_BUFFER_TIME;
    }

    private bool IsGroundHit(RaycastHit2D hit)
    {
        return hit.collider != null && hit.normal.y > 0.7f;
    }

    // Check actual physics contacts for ground below (more accurate than raycasts).
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

    private void ApplyPlayerCollisionSetup()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
        {
            SetLayerRecursively(transform, playerLayer);
            Physics2D.IgnoreLayerCollision(playerLayer, playerLayer, true);
        }

        // Fallback that does not rely on project layer names.
        IgnoreCollisionsWithOtherPlayers();
    }

    private void IgnoreCollisionsWithOtherPlayers()
    {
        _cachedColliders = GetComponentsInChildren<Collider2D>(true);
        if (_cachedColliders == null || _cachedColliders.Length == 0)
            return;

        var allPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerController other = allPlayers[i];
            if (other == null || other == this)
                continue;

            var otherColliders = other.GetComponentsInChildren<Collider2D>(true);
            for (int a = 0; a < _cachedColliders.Length; a++)
            {
                Collider2D mine = _cachedColliders[a];
                if (mine == null) continue;

                for (int b = 0; b < otherColliders.Length; b++)
                {
                    Collider2D theirs = otherColliders[b];
                    if (theirs == null) continue;
                    Physics2D.IgnoreCollision(mine, theirs, true);
                }
            }
        }
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        if (root == null) return;
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }
}

