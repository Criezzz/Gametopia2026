using UnityEngine;

/// Manages the player's active tool. Handles attack input delegation and tool swapping.
public class PlayerToolHandler : MonoBehaviour
{
    [System.Serializable]
    public struct ToolBinding
    {
        [Tooltip("Drag the ToolData SO for this tool.")]
        public ToolData data;
        [Tooltip("Drag the matching BaseTool component from this GameObject.")]
        public BaseTool component;
    }

    [Header("Default Tool")]
    [SerializeField] private ToolData _defaultToolData; // Legacy fallback, not auto-equipped at start.

    [Header("Tool Bindings")]
    [Tooltip("Map each ToolData SO to its BaseTool component. Assign in Inspector.")]
    [SerializeField] private ToolBinding[] _toolBindings;

    [Header("Event Channels")]
    [SerializeField] private ToolDataEventChannel _onToolEquipped;

    [Header("Weapon Visual")]
    [SerializeField] private Transform _weaponVisualRoot;
    [SerializeField] private SpriteRenderer _weaponSpriteRenderer;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private int _weaponSortingOrderOffset = 3;

    private PlayerController _playerController;
    private PlayerInputHandler _inputHandler;
    private BaseTool _currentTool;
    private ToolData _currentToolData;

    public ToolData CurrentToolData => _currentToolData;
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SecondaryPhaseHash = Animator.StringToHash("SecondaryPhase");
    private static readonly int IsHoldingHash = Animator.StringToHash("IsHolding");

    public void OnPlayerDied()
    {
        if (_weaponSpriteRenderer != null) _weaponSpriteRenderer.enabled = false;
        enabled = false;
    }

    public void OnPlayerReset()
    {
        enabled = true;
    }

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        ValidateWeaponVisualSetup();
    }

    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void Start()
    {
        // Run starts with no weapon until first toolbox pickup.
        ClearTool();
    }

    private void Update()
    {
        UpdateWeaponVisualTransform();

        if (_currentTool == null || _inputHandler == null)
            return;

        if (_inputHandler.AttackHeld)
        {
            if (_currentTool.CanAttack())
            {
                _currentTool.Attack();
                TriggerWeaponAttackAnimation();
            }
        }

        if (_inputHandler.AttackReleased)
        {
            _currentTool.StopAttack();
        }
    }

    public void EquipTool(ToolData newToolData)
    {
        if (newToolData == null)
        {
            ClearTool();
            return;
        }

        // Unequip current
        if (_currentTool != null)
        {
            _currentTool.OnUnequip();
            _currentTool.enabled = false;
        }

        _currentToolData = newToolData;
        ConfigureWeaponVisual(newToolData);

        // Find and activate the correct tool component
        BaseTool tool = GetToolComponent(newToolData);
        if (tool != null)
        {
            tool.enabled = true;
            tool.OnRequestSecondaryAnimation = PlaySecondaryAnimation;
            tool.OnRequestPrimaryAnimation = PlayPrimaryAnimation;
            tool.OnRequestStartHold = StartHoldAnimation;
            tool.OnRequestStopHold = StopHoldAnimation;
            tool.Initialize(newToolData, _playerController);
            _currentTool = tool;
            Debug.Log($"[PlayerToolHandler] Equipped: {newToolData.toolName}");
        }
        else
        {
            Debug.LogWarning($"[PlayerToolHandler] No tool component found for: {newToolData.toolName}");
        }

        _onToolEquipped?.Raise(newToolData);
    }

    private void ClearTool()
    {
        if (_currentTool != null)
        {
            _currentTool.OnUnequip();
            _currentTool.enabled = false;
        }

        _currentTool = null;
        _currentToolData = null;
        ConfigureWeaponVisual(null);
    }

    private void HandleToolEquipped(ToolData toolData)
    {
        EquipTool(toolData);
    }

    private void ValidateWeaponVisualSetup()
    {
        if (_weaponVisualRoot != null)
        {
            if (_weaponSpriteRenderer == null)
                _weaponSpriteRenderer = _weaponVisualRoot.GetComponent<SpriteRenderer>();
            if (_weaponAnimator == null)
                _weaponAnimator = _weaponVisualRoot.GetComponent<Animator>();
        }

        SpriteRenderer playerRenderer = GetComponentInChildren<SpriteRenderer>();
        if (playerRenderer != null && _weaponSpriteRenderer != null)
        {
            _weaponSpriteRenderer.sortingLayerID = playerRenderer.sortingLayerID;
            _weaponSpriteRenderer.sortingOrder = playerRenderer.sortingOrder + _weaponSortingOrderOffset;
        }

        if (_weaponVisualRoot == null || _weaponSpriteRenderer == null || _weaponAnimator == null)
            Debug.LogWarning("[PlayerToolHandler] Weapon visual references are missing. Assign WeaponVisualRoot + SpriteRenderer + Animator in Inspector.");
    }

    private void ConfigureWeaponVisual(ToolData data)
    {
        ValidateWeaponVisualSetup();

        if (_weaponVisualRoot == null || _weaponSpriteRenderer == null || _weaponAnimator == null)
            return;

        _weaponSpriteRenderer.sprite = data != null ? data.toolSprite : null;
        _weaponSpriteRenderer.enabled = _weaponSpriteRenderer.sprite != null;

        _weaponAnimator.runtimeAnimatorController = data != null ? data.attackAnimator : null;
        _weaponAnimator.Rebind();
        _weaponAnimator.ResetTrigger(AttackHash);
        _weaponAnimator.Update(0f);
    }

    private void UpdateWeaponVisualTransform()
    {
        if (_weaponVisualRoot == null)
            return;

        int facing = _playerController != null ? _playerController.FacingDirection : 1;
        float holdDist = _currentToolData != null ? _currentToolData.weaponHoldDistance : 0f;
        float yOffset = _currentToolData != null ? _currentToolData.weaponYOffset : 0f;
        float x = facing * holdDist;
        _weaponVisualRoot.localPosition = new Vector3(x, yOffset, 0f);

        // Counteract parent scale so the weapon keeps its original world-space size
        // even when the player GameObject is scaled (e.g. 0.7).
        if (_weaponVisualRoot.parent != null)
        {
            Vector3 ps = _weaponVisualRoot.parent.lossyScale;
            _weaponVisualRoot.localScale = new Vector3(
                ps.x != 0f ? 1f / ps.x : 1f,
                ps.y != 0f ? 1f / ps.y : 1f,
                ps.z != 0f ? 1f / ps.z : 1f
            );
        }

        if (_weaponSpriteRenderer != null)
            _weaponSpriteRenderer.flipX = facing < 0;
    }

    private void TriggerWeaponAttackAnimation()
    {
        if (_weaponAnimator == null || _weaponAnimator.runtimeAnimatorController == null)
            return;

        foreach (var param in _weaponAnimator.parameters)
        {
            if (param.nameHash == AttackHash && param.type == AnimatorControllerParameterType.Trigger)
            {
                _weaponAnimator.SetTrigger(AttackHash);
                return;
            }
        }
    }

    // Transition to secondary animation state (multi-phase tools).
    private void PlaySecondaryAnimation()
    {
        if (_weaponAnimator == null || _weaponAnimator.runtimeAnimatorController == null) return;
        _weaponAnimator.SetBool(SecondaryPhaseHash, true);
    }

    private void PlayPrimaryAnimation()
    {
        if (_weaponAnimator == null || _weaponAnimator.runtimeAnimatorController == null) return;
        _weaponAnimator.SetBool(SecondaryPhaseHash, false);
    }

    private void StartHoldAnimation()
    {
        if (_weaponAnimator == null || _weaponAnimator.runtimeAnimatorController == null) return;
        _weaponAnimator.SetBool(IsHoldingHash, true);
    }

    private void StopHoldAnimation()
    {
        if (_weaponAnimator == null || _weaponAnimator.runtimeAnimatorController == null) return;
        _weaponAnimator.SetBool(IsHoldingHash, false);
    }

    private BaseTool GetToolComponent(ToolData data)
    {
        if (_toolBindings == null) return null;

        foreach (var binding in _toolBindings)
        {
            if (binding.data == data && binding.component != null)
                return binding.component;
        }
        return null;
    }
}
