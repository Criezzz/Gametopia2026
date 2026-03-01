using UnityEngine;

/// <summary>
/// Screwdriver: Thrown projectile, 4 damage, 0.5s cooldown, no pierce.
/// </summary>
public class ScrewdriverTool : BaseTool
{
    [Header("Screwdriver Settings")]
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
        PlayAttackSFX();

        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        int damage = _toolData != null ? _toolData.damage : 4;
        GameObject prefab = _toolData != null ? _toolData.attackPrefab : null;

        if (prefab != null)
        {
            GameObject proj = Instantiate(prefab, origin, Quaternion.identity);
            var toolProjectile = proj.GetComponent<ToolProjectile>();
            toolProjectile.Initialize(dir, _projectileSpeed, damage, false);
        }

        StartCooldown();
    }
}
