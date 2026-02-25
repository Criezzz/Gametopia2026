using UnityEngine;

/// <summary>
/// Screwdriver: Thrown projectile, 4 damage, 0.5s cooldown, no pierce.
/// </summary>
public class ScrewdriverTool : BaseTool
{
    [Header("Screwdriver Settings")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 10f;

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        if (data != null && data.attackParam > 0f)
            _projectileSpeed = data.attackParam;
    }

    public override void Attack()
    {
        if (!CanAttack()) return;

        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        int damage = _toolData != null ? _toolData.damage : 4;

        // Spawn projectile
        if (_projectilePrefab != null)
        {
            GameObject proj = Instantiate(_projectilePrefab, origin, Quaternion.identity);
            var toolProjectile = proj.GetComponent<ToolProjectile>();
            if (toolProjectile != null)
            {
                toolProjectile.Initialize(dir, _projectileSpeed, damage, false);
            }
        }

        StartCooldown();
    }
}
