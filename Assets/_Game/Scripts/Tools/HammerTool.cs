using UnityEngine;

/// <summary>
/// Hammer: Melee swing, 2u range, 5 damage, 1s cooldown.
/// Default starting weapon.
/// </summary>
public class HammerTool : BaseTool
{
    [Header("Hammer Settings")]
    [SerializeField] private LayerMask _enemyLayer;

    public override void Attack()
    {
        if (!CanAttack()) return;
        PlayAttackSFX();

        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        float range = _toolData != null ? _toolData.attackParam : 2f;
        int damage = _toolData != null ? _toolData.damage : 5;

        // Raycast box in front of player for 2 units
        Vector2 boxSize = new Vector2(range, 1f);
        Vector2 boxCenter = origin + dir * (range * 0.5f);
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
        Debug.Log($"[Hammer] Swing! Hit {hits.Length} enemies");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow half-transparent
        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        float range = _toolData != null ? _toolData.attackParam : 2f;
        Vector2 boxSize = new Vector2(range, 1f);
        Vector2 boxCenter = origin + dir * (range * 0.5f);
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
