using UnityEngine;

/// <summary>
/// Chainsaw: Hold to attack. 3-phase animation:
///   1. Press   → chainsaw starts up  (Start clip)
///   2. Hold    → chainsaw loops      (Loop clip, repeats)
///   3. Release → chainsaw winds down (End clip)
/// Continuous melee, 5 damage per 0.1s tick, 2u range.
/// </summary>
public class ChainsawTool : BaseTool
{
    [Header("Chainsaw Settings")]
    [SerializeField] private LayerMask _enemyLayer;

    private float _range = 2f;
    private bool _isHolding;

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        _range = data != null ? data.attackParam : 2f;
        _isHolding = false;
    }

    public override void Attack()
    {
        if (!CanAttack()) return;

        // First frame of hold — start the animation
        if (!_isHolding)
        {
            _isHolding = true;
            OnRequestStartHold?.Invoke();
        }

        PlayAttackSFX();
        _isAttacking = true;

        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        int damage = _toolData != null ? _toolData.damage : 5;

        // Hitbox in front, 2 units
        Vector2 boxSize = new Vector2(_range, 0.8f);
        Vector2 boxCenter = origin + dir * (_range * 0.5f);
        LayerMask enemyMask = ResolveEnemyLayerMask(_enemyLayer);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyMask);

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponentInParent<BaseEnemy>();
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
