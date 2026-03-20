using UnityEngine;

/// <summary>
/// Magnet: Directional beam, 1 damage/tick, 0.1s cooldown, pierces all enemies.
/// Hold to keep damaging.
/// </summary>
public class MagnetTool : BaseTool
{
    [Header("Magnet Settings")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _beamWidth = 0.5f;
    [SerializeField] private float _beamHeight = 2f;
    [SerializeField] private float _beamLength = 50f;
    [SerializeField] private float _verticalOffset = 0.5f;

    private bool _isHolding;

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        _isHolding = false;

        if (data != null && data.attackParam > 0f)
            _beamLength = data.attackParam;

        if (data != null && data.behaviorConfig is MagnetToolConfig cfg)
        {
            if (cfg.beamWidth > 0f) _beamWidth = cfg.beamWidth;
            if (cfg.beamHeight > 0f) _beamHeight = cfg.beamHeight;
            if (cfg.beamLength > 0f) _beamLength = cfg.beamLength;
            _verticalOffset = cfg.verticalOffset;
        }
    }

    public override void Attack()
    {
        if (!CanAttack()) return;

        if (!_isHolding)
        {
            _isHolding = true;
            OnRequestStartHold?.Invoke();
        }

        PlayAttackSFX();
        _isAttacking = true;

        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        int damage = _toolData != null ? _toolData.damage : 1;

        // Origin shifted up slightly to cover player level and level above.
        Vector2 boxCenter = origin + new Vector2(0f, _verticalOffset);
        Vector2 boxSize = new Vector2(_beamWidth, _beamHeight);
        LayerMask enemyMask = ResolveEnemyLayerMask(_enemyLayer);

        // Raycast forward — hits ALL enemies (pierce) using BoxCast for volume
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            boxCenter, boxSize, 0f,
            dir, _beamLength, enemyMask);

        foreach (var hit in hits)
        {
            var enemy = hit.collider.GetComponentInParent<BaseEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage, _toolData);
            }
        }

        StartCooldown();
    }

    public override void StopAttack()
    {
        if (_isHolding)
        {
            _isHolding = false;
            OnRequestStopHold?.Invoke();
        }
        base.StopAttack();
    }

    public override void OnUnequip()
    {
        _isHolding = false;
        base.OnUnequip();
    }
}
