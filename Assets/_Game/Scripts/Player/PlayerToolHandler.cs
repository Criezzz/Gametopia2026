using UnityEngine;

/// <summary>
/// Manages the player's current tool.
/// Listens for attack input, delegates to the active BaseTool.
/// Handles tool swapping on toolbox pickup.
/// </summary>
public class PlayerToolHandler : MonoBehaviour
{
    [Header("Default Tool")]
    [SerializeField] private ToolData _defaultToolData; // Legacy fallback, not auto-equipped at start.

    [Header("Tool Prefab Mapping")]
    [Tooltip("Prefab containing all tool scripts. Tools are enabled/disabled on swap.")]
    [SerializeField] private HammerTool _hammerTool;
    [SerializeField] private ScrewdriverTool _screwdriverTool;
    [SerializeField] private TapeMeasureTool _tapeMeasureTool;
    [SerializeField] private NailGunTool _nailGunTool;
    [SerializeField] private BlowtorchTool _blowtorchTool;
    [SerializeField] private VacuumTool _vacuumTool;
    [SerializeField] private MagnetTool _magnetTool;
    [SerializeField] private ChainsawTool _chainsawTool;

    [Header("Event Channels")]
    [SerializeField] private ToolDataEventChannel _onToolEquipped;

    [Header("Weapon Visual")]
    [SerializeField] private Transform _weaponVisualRoot;
    [SerializeField] private SpriteRenderer _weaponSpriteRenderer;
    [SerializeField] private Animator _weaponAnimator;
    [SerializeField] private int _weaponSortingOrderOffset = 3;

    private PlayerController _playerController;
    private BaseTool _currentTool;
    private ToolData _currentToolData;

    public ToolData CurrentToolData => _currentToolData;
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SecondaryPhaseHash = Animator.StringToHash("SecondaryPhase");

    /// <summary>
    /// Called by PlayerHealth when the player dies. Hides weapon and stops updates.
    /// </summary>
    public void OnPlayerDied()
    {
        if (_weaponSpriteRenderer != null) _weaponSpriteRenderer.enabled = false;
        enabled = false;
    }

    /// <summary>
    /// Called by PlayerHealth when the player resets (scene reload etc).
    /// </summary>
    public void OnPlayerReset()
    {
        enabled = true;
    }

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        ValidateWeaponVisualSetup();
    }

    private void OnEnable()
    {
        if (_onToolEquipped != null)
            _onToolEquipped.Register(HandleToolEquipped);
    }

    private void OnDisable()
    {
        if (_onToolEquipped != null)
            _onToolEquipped.Unregister(HandleToolEquipped);
    }

    private void Start()
    {
        // Run starts with no weapon until first toolbox pickup.
        ClearTool();
    }

    private void Update()
    {
        UpdateWeaponVisualTransform();

        if (_currentTool == null)
            return;

        if (Input.GetKey(KeyCode.J))
        {
            if (_currentTool.CanAttack())
            {
                _currentTool.Attack();
                TriggerWeaponAttackAnimation();
            }
        }

        if (Input.GetKeyUp(KeyCode.J))
        {
            _currentTool.StopAttack();
        }
    }

    /// <summary>
    /// Swap to a new tool. Called by GameManager when toolbox is picked up.
    /// </summary>
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
            tool.Initialize(newToolData, _playerController);
            _currentTool = tool;
            Debug.Log($"[PlayerToolHandler] Equipped: {newToolData.toolName}");
        }
        else
        {
            Debug.LogWarning($"[PlayerToolHandler] No tool component found for: {newToolData.toolName}");
        }
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

    /// <summary>
    /// Set SecondaryPhase = true so the animator transitions to the phase-2 state.
    /// Called by multi-phase tools (Tape retract, Vacuum shoot).
    /// </summary>
    private void PlaySecondaryAnimation()
    {
        if (_weaponAnimator == null || _weaponAnimator.runtimeAnimatorController == null) return;
        _weaponAnimator.SetBool(SecondaryPhaseHash, true);
    }

    /// <summary>
    /// Set SecondaryPhase = false so the animator returns to idle / phase-1 ready.
    /// Called when multi-phase attack completes.
    /// </summary>
    private void PlayPrimaryAnimation()
    {
        if (_weaponAnimator == null || _weaponAnimator.runtimeAnimatorController == null) return;
        _weaponAnimator.SetBool(SecondaryPhaseHash, false);
    }

    private BaseTool GetToolComponent(ToolData data)
    {
        // Match by tool type + name
        switch (data.toolType)
        {
            case ToolType.Melee:
                if (data.toolName == "Hammer") return _hammerTool;
                if (data.toolName == "Chainsaw") return _chainsawTool;
                break;
            case ToolType.Ranged:
                if (data.toolName == "Screwdriver") return _screwdriverTool;
                if (data.toolName == "Tape Measure") return _tapeMeasureTool;
                if (data.toolName == "Nail Gun") return _nailGunTool;
                break;
            case ToolType.Beam:
                if (data.toolName == "Blowtorch") return _blowtorchTool;
                if (data.toolName == "Magnet") return _magnetTool;
                break;
            case ToolType.Utility:
                if (data.toolName == "Vacuum") return _vacuumTool;
                break;
        }
        return null;
    }
}
