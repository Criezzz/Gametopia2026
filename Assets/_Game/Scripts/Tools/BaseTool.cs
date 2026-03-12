using UnityEngine;

/// Abstract base for all tools. Handles cooldown, subclasses implement attack behavior.
public abstract class BaseTool : MonoBehaviour
{
    protected ToolData _toolData;
    protected PlayerController _playerController;
    protected float _cooldownTimer;
    protected bool _isAttacking;

    // Callbacks for PlayerToolHandler animation control.
    public System.Action OnRequestSecondaryAnimation;
    public System.Action OnRequestPrimaryAnimation;
    public System.Action OnRequestStartHold;
    public System.Action OnRequestStopHold;

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

    public virtual bool CanAttack()
    {
        return _cooldownTimer <= 0f;
    }

    public abstract void Attack();

    protected void PlayAttackSFX()
    {
        if (_toolData != null && _toolData.attackSFX != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(_toolData.attackSFX);
    }

    protected void PlaySecondaryAttackSFX()
    {
        if (_toolData != null && _toolData.secondaryAttackSFX != null && SFXManager.Instance != null)
            SFXManager.Instance.Play(_toolData.secondaryAttackSFX);
    }

    public virtual void StopAttack()
    {
        _isAttacking = false;
    }

    public virtual void OnUnequip()
    {
        StopAttack();
        OnRequestSecondaryAnimation = null;
        OnRequestPrimaryAnimation = null;
        OnRequestStartHold = null;
        OnRequestStopHold = null;
    }

    protected void StartCooldown()
    {
        if (_toolData != null)
            _cooldownTimer = _toolData.cooldown;
    }

    protected Vector2 GetAttackDirection()
    {
        return _playerController != null ? _playerController.GetAimDirection() : Vector2.right;
    }

    protected Vector2 GetAttackOrigin()
    {
        Vector2 pos = transform.position;
        Vector2 dir = GetAttackDirection();
        return pos + dir * 0.5f; // Offset slightly forward
    }
}