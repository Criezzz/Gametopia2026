using UnityEngine;

/// <summary>
/// Blowtorch: Continuous damage, 2 dmg per tick, 0.1s cooldown, 3u range hitbox.
/// Hold to keep damaging.
/// </summary>
public class BlowtorchTool : BaseTool
{
    [Header("Blowtorch Settings")]
    [SerializeField] private LayerMask _enemyLayer;

    private float _range = 3f;

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        _range = data != null ? data.attackParam : 3f;
    }

    public override void Attack()
    {
        if (!CanAttack()) return;
        PlayAttackSFX();

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
                enemy.TakeDamage(damage);
            }
        }

        StartCooldown();
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