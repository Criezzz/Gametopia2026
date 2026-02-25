using UnityEngine;

/// <summary>
/// Nail Gun: Rapid fire small nails, 1 damage, 0.1s cooldown, no pierce.
/// </summary>
public class NailGunTool : BaseTool
{
    [Header("Nail Gun Settings")]
    [SerializeField] private GameObject _nailPrefab;
    [SerializeField] private float _nailSpeed = 12f;

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        if (data != null && data.attackParam > 0f)
            _nailSpeed = data.attackParam;
    }

    public override void Attack()
    {
        if (!CanAttack()) return;

        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        int damage = _toolData != null ? _toolData.damage : 1;

        // Spawn nail projectile
        if (_nailPrefab != null)
        {
            GameObject nail = Instantiate(_nailPrefab, origin, Quaternion.identity);
            var proj = nail.GetComponent<ToolProjectile>();
            if (proj != null)
            {
                proj.Initialize(dir, _nailSpeed, damage, false);
            }
        }

        StartCooldown();
    }
}
