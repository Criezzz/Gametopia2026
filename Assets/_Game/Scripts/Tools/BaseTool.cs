using UnityEngine;

/// <summary>
/// Abstract base class for all tools.
/// Handles cooldown timing. Subclasses implement specific attack behavior.
/// </summary>
public abstract class BaseTool : MonoBehaviour
{
    protected ToolData _toolData;
    protected PlayerController _playerController;
    protected float _cooldownTimer;
    protected bool _isAttacking;

    /// <summary>
    /// Callback invoked by PlayerToolHandler so tools can request animation phase swaps.
    /// </summary>
    public System.Action OnRequestSecondaryAnimation;
    public System.Action OnRequestPrimaryAnimation;

    /// <summary>
    /// Initialize the tool with its data and the player.
    /// </summary>
    public virtual void Initialize(ToolData data, PlayerController player)
    {
        _toolData = data;
        _playerController = player;
        _cooldownTimer = 0f;
        _isAttacking = false;
    }

    protected virtual void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Can this tool attack right now?
    /// </summary>
    public virtual bool CanAttack()
    {
        return _cooldownTimer <= 0f;
    }

    /// <summary>
    /// Start or execute an attack. Called when player presses attack.
    /// </summary>
    public abstract void Attack();

    /// <summary>
    /// Play the tool's attack SFX (from ToolData.attackSFX) if assigned.
    /// Call this at the start of each tool's Attack() override.
    /// </summary>
    protected void PlayAttackSFX()
    {
        if (_toolData != null && _toolData.attackSFX != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(_toolData.attackSFX);
    }

    /// <summary>
    /// Stop attacking. Called when player releases attack (for continuous tools).
    /// </summary>
    public virtual void StopAttack()
    {
        _isAttacking = false;
    }

    /// <summary>
    /// Called when this tool is unequipped (player picks up toolbox).
    /// Clean up any active effects.
    /// </summary>
    public virtual void OnUnequip()
    {
        StopAttack();
        OnRequestSecondaryAnimation = null;
        OnRequestPrimaryAnimation = null;
    }

    /// <summary>
    /// Start the cooldown timer.
    /// </summary>
    protected void StartCooldown()
    {
        if (_toolData != null)
            _cooldownTimer = _toolData.cooldown;
    }

    /// <summary>
    /// Get the attack direction vector based on player facing.
    /// </summary>
    protected Vector2 GetAttackDirection()
    {
        return _playerController != null ? _playerController.GetAimDirection() : Vector2.right;
    }

    /// <summary>
    /// Get the attack origin (player position with slight forward offset).
    /// </summary>
    protected Vector2 GetAttackOrigin()
    {
        Vector2 pos = transform.position;
        Vector2 dir = GetAttackDirection();
        return pos + dir * 0.5f; // Offset slightly forward
    }
}