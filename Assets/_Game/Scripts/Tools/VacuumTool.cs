using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vacuum: Suck small enemies (1u) in a 3u column for 1s,
/// then auto-shoot them as rotating projectiles dealing 10 damage each.
/// Cooldown resets after all enemies are shot.
/// </summary>
public class VacuumTool : BaseTool
{
    [Header("Vacuum Settings")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private GameObject _enemyProjectilePrefab;
    [SerializeField] private float _suckDuration = 1f;
    [SerializeField] private float _suckRange = 3f;
    [SerializeField] private float _shootSpeed = 10f;
    [SerializeField] private int _shootDamage = 10;
    [SerializeField] private float _shootInterval = 0.15f;

    private bool _isSucking;
    private bool _isShooting;
    private float _suckTimer;
    private readonly List<GameObject> _suckedEnemies = new();

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);

        if (data != null)
        {
            if (data.attackParam > 0f)
                _suckRange = data.attackParam;
            if (data.damage > 0)
                _shootDamage = data.damage;

            if (data.behaviorConfig is VacuumToolConfig cfg)
            {
                if (cfg.suckDuration > 0f) _suckDuration = cfg.suckDuration;
                if (cfg.suckRange > 0f) _suckRange = cfg.suckRange;
                if (cfg.shootSpeed > 0f) _shootSpeed = cfg.shootSpeed;
                if (cfg.shootDamage > 0) _shootDamage = cfg.shootDamage;
                if (cfg.shootInterval > 0f) _shootInterval = cfg.shootInterval;
            }
        }
    }

    public override bool CanAttack()
    {
        return !_isSucking && !_isShooting;
    }

    public override void Attack()
    {
        if (!CanAttack()) return;

        _isSucking = true;
        _suckTimer = _suckDuration;
        _suckedEnemies.Clear();
        Debug.Log("[Vacuum] Start sucking!");
    }

    protected override void Update()
    {
        base.Update();

        if (_isSucking)
        {
            _suckTimer -= Time.deltaTime;

            // Check for small enemies in the column
            Vector2 origin = GetAttackOrigin();
            Vector2 dir = GetAttackDirection();

            Vector2 boxSize = new Vector2(_suckRange, 1f);
            Vector2 boxCenter = origin + dir * (_suckRange * 0.5f);

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, _enemyLayer);

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<BaseEnemy>();
                if (enemy != null && enemy.IsSmall && !_suckedEnemies.Contains(enemy.gameObject))
                {
                    // Suck this enemy in
                    _suckedEnemies.Add(enemy.gameObject);
                    enemy.GetSucked(transform.position);
                }
            }

            if (_suckTimer <= 0f)
            {
                _isSucking = false;
                StartShooting();
            }
        }
    }

    private void StartShooting()
    {
        if (_suckedEnemies.Count == 0)
        {
            // Nothing sucked, ready to use again
            return;
        }

        _isShooting = true;
        StartCoroutine(ShootEnemiesSequence());
    }

    private IEnumerator ShootEnemiesSequence()
    {
        Vector2 dir = GetAttackDirection();

        foreach (var enemyObj in _suckedEnemies)
        {
            if (enemyObj != null)
            {
                // Convert enemy to projectile
                Vector2 origin = GetAttackOrigin();

                if (_enemyProjectilePrefab != null)
                {
                    GameObject proj = Instantiate(_enemyProjectilePrefab, origin, Quaternion.identity);
                    var toolProj = proj.GetComponent<ToolProjectile>();
                    if (toolProj != null)
                    {
                        toolProj.Initialize(dir, _shootSpeed, _shootDamage, false);
                        toolProj.SetRotating(true);
                    }

                    // Copy enemy sprite if possible
                    var enemySR = enemyObj.GetComponentInChildren<SpriteRenderer>();
                    var projSR = proj.GetComponentInChildren<SpriteRenderer>();
                    if (enemySR != null && projSR != null)
                    {
                        projSR.sprite = enemySR.sprite;
                        projSR.color = enemySR.color;
                    }
                }

                // Destroy the sucked enemy
                Destroy(enemyObj);

                yield return new WaitForSeconds(_shootInterval); // Stagger shots
            }
        }

        _suckedEnemies.Clear();
        _isShooting = false;
        Debug.Log("[Vacuum] All enemies shot!");
    }

    public override void OnUnequip()
    {
        base.OnUnequip();
        StopAllCoroutines();
        _isSucking = false;
        _isShooting = false;
        _suckedEnemies.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // Green half-transparent
        Vector2 origin = GetAttackOrigin();
        Vector2 dir = GetAttackDirection();
        Vector2 boxSize = new Vector2(_suckRange, 1f);
        Vector2 boxCenter = origin + dir * (_suckRange * 0.5f);
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
