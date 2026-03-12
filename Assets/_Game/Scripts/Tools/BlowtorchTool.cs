using UnityEngine;

/// <summary>
/// Blowtorch: Hold to attack. 3-phase animation:
///   1. Press  → fire extends out  (FireStart clip)
///   2. Hold   → fire loops        (FireLoop clip, repeats)
///   3. Release→ fire retracts     (FireEnd clip)
/// Continuous damage, 2 dmg per tick, 0.1s cooldown, 3u range hitbox.
/// </summary>
public class BlowtorchTool : BaseTool
{
    [Header("Blowtorch Settings")]
    [SerializeField] private LayerMask _enemyLayer;

    private float _range = 3f;
    private bool _isHolding;

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        _range = data != null ? data.attackParam : 3f;
        _isHolding = false;
    }

    public override void Attack()
    {
        if (!CanAttack()) return;

        // First frame of hold — start the fire animation
        if (!_isHolding)
        {
            _isHolding = true;
            PlayAttackSFX();
            OnRequestStartHold?.Invoke();
        }

        _isAttacking = true;

        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        int damage = _toolData != null ? _toolData.damage : 2;

        // Hitbox in front, 3 units long, 2 units high
        Vector2 boxSize = new Vector2(_range, 2.0f);
        // Shift up slightly by 0.5 to hit player level and exactly one level above
        Vector2 boxCenter = origin + dir * (_range * 0.5f) + new Vector2(0f, 0.5f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, _enemyLayer);

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<BaseEnemy>();
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange half-transparent
        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        Vector2 boxSize = new Vector2(_range, 2.0f);
        Vector2 boxCenter = origin + dir * (_range * 0.5f) + new Vector2(0f, 0.5f);
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}