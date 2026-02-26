using UnityEngine;

/// <summary>
/// Chainsaw: Continuous melee, 5 damage per 0.1s tick, 2u range.
/// Hold to keep damaging.
/// </summary>
public class ChainsawTool : BaseTool
{
    [Header("Chainsaw Settings")]
    [SerializeField] private LayerMask _enemyLayer;

    private float _range = 2f;

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        _range = data != null ? data.attackParam : 2f;
    }

    public override void Attack()
    {
        if (!CanAttack()) return;

        _isAttacking = true;

        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        int damage = _toolData != null ? _toolData.damage : 5;

        // Hitbox in front, 2 units
        Vector2 boxSize = new Vector2(_range, 0.8f);
        Vector2 boxCenter = origin + dir * (_range * 0.5f);

        Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, _enemyLayer);

        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }

        StartCooldown();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red half-transparent
        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        Vector2 boxSize = new Vector2(_range, 0.8f);
        Vector2 boxCenter = origin + dir * (_range * 0.5f);
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}