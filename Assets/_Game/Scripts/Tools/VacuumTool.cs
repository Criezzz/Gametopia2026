using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vacuum: Two-phase utility weapon.
///   Phase 1 (Suck):  Suck small enemies in a column for suckDuration.
///   Phase 2 (Shoot): Auto-shoot sucked enemies as rotating projectiles.
/// Each phase plays its own animation clip via the dual-animator system.
/// Shoot damage = ToolData.secondaryDamage (fallback to ToolData.damage).
/// </summary>
public class VacuumTool : BaseTool
{
    [Header("Vacuum Settings")]
    [SerializeField] private LayerMask _enemyLayer;

    [Header("VFX")]
    [Tooltip("Particle system for suck phase (looping, plays during Phase 1)")]
    [SerializeField] private ParticleSystem _suckVFX;
    [Tooltip("Particle system for burst phase (one-shot, plays at Phase 2 start)")]
    [SerializeField] private ParticleSystem _burstVFX;

    private float _suckDuration = 1f;
    private float _suckRange = 3f;
    private float _shootSpeed = 10f;
    private int _shootDamage = 10;
    private float _shootInterval = 0.15f;
    private float _suckAnimDuration = 0.5f;
    private float _suckPullSpeed = 8f;

    private bool _isSucking;
    private bool _isShooting;
    private float _suckTimer;
    private int _pendingAbsorbs;
    private readonly List<GameObject> _suckedEnemies = new();

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);

        if (data != null)
        {
            if (data.attackParam > 0f)
                _suckRange = data.attackParam;

            // Primary damage = suck contact (unused), secondary = projectile shoot damage
            _shootDamage = data.secondaryDamage > 0 ? data.secondaryDamage : data.damage;

            if (data.behaviorConfig is VacuumToolConfig cfg)
            {
                if (cfg.suckDuration > 0f) _suckDuration = cfg.suckDuration;
                if (cfg.suckRange > 0f) _suckRange = cfg.suckRange;
                if (cfg.shootSpeed > 0f) _shootSpeed = cfg.shootSpeed;
                if (cfg.shootDamage > 0) _shootDamage = cfg.shootDamage;
                if (cfg.shootInterval > 0f) _shootInterval = cfg.shootInterval;
                if (cfg.suckAnimDuration > 0f) _suckAnimDuration = cfg.suckAnimDuration;
                if (cfg.suckPullSpeed > 0f) _suckPullSpeed = cfg.suckPullSpeed;
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
        PlayAttackSFX();

        _isSucking = true;
        _suckTimer = _suckDuration;
        _suckedEnemies.Clear();
        _pendingAbsorbs = 0;

        // Start suck VFX
        if (_suckVFX != null) _suckVFX.Play();

        Debug.Log("[Vacuum] Start sucking!");
    }

    protected override void Update()
    {
        base.Update();

        // Keep VFX facing the same direction as the player
        FlipVFX();

        if (_isSucking)
        {
            _suckTimer -= Time.deltaTime;

            // Check for small enemies in the column (starts right at player)
            Vector2 pos = transform.position;
            Vector2 dir = GetAttackDirection();

            Vector2 boxSize = new Vector2(_suckRange, 1f);
            Vector2 boxCenter = pos + dir * (_suckRange * 0.5f);
            LayerMask enemyMask = ResolveEnemyLayerMask(_enemyLayer);

            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, enemyMask);

            foreach (var hit in hits)
            {
                var enemy = hit.GetComponentInParent<BaseEnemy>();
                if (enemy != null && enemy.IsSmall && !_suckedEnemies.Contains(enemy.gameObject))
                {
                    // Suck this enemy in — smooth pull toward player + scale down
                    _suckedEnemies.Add(enemy.gameObject);
                    _pendingAbsorbs++;

                    // Pass this tool's transform (on the player) — enemy pulls toward it
                    enemy.GetSucked(transform, _suckAnimDuration, _suckPullSpeed, () =>
                    {
                        _pendingAbsorbs--;
                    });
                }
            }

            if (_suckTimer <= 0f)
            {
                _isSucking = false;

                // Stop suck VFX
                if (_suckVFX != null) _suckVFX.Stop();

                // >>> Phase 2: switch to secondary (shoot) animation
                OnRequestSecondaryAnimation?.Invoke();
                StartShooting();
            }
        }
    }

    private void StartShooting()
    {
        if (_suckedEnemies.Count == 0)
        {
            // Nothing sucked — return to idle immediately
            OnRequestPrimaryAnimation?.Invoke();
            return;
        }

        _isShooting = true;

        // Burst VFX
        if (_burstVFX != null) _burstVFX.Play();

        StartCoroutine(ShootEnemiesSequence());
    }

    private IEnumerator ShootEnemiesSequence()
    {
        Vector2 dir = GetAttackDirection();

        foreach (var enemyObj in _suckedEnemies)
        {
            if (enemyObj != null)
            {
                Vector2 origin = GetAttackOrigin();

                GameObject prefab = _toolData != null ? _toolData.attackPrefab : null;
                if (prefab != null)
                {
                    GameObject proj = Instantiate(prefab, origin, Quaternion.identity);
                    var toolProj = proj.GetComponent<ToolProjectile>();
                    if (toolProj != null)
                    {
                        toolProj.Initialize(dir, _shootSpeed, _shootDamage, false, _toolData);
                        toolProj.SetRotating(true);
                    }

                    // Copy enemy sprite onto the projectile, scale down to half
                    var enemySR = enemyObj.GetComponentInChildren<SpriteRenderer>();
                    var projSR = proj.GetComponentInChildren<SpriteRenderer>();
                    if (enemySR != null && projSR != null)
                    {
                        projSR.sprite = enemySR.sprite;
                        projSR.color = enemySR.color;
                    }
                    proj.transform.localScale *= 0.5f;
                }
                else
                {
                    Debug.LogWarning("[Vacuum] ToolData.attackPrefab is not assigned! Cannot shoot.");
                }

                // Camera shake per shot
                if (CameraShake.Instance != null)
                    CameraShake.Instance.Shake(0.08f, 0.05f);

                // Phase 2 SFX: once per enemy shot
                PlaySecondaryAttackSFX();

                // Destroy the sucked enemy
                Destroy(enemyObj);

                yield return new WaitForSeconds(_shootInterval);
            }
        }

        _suckedEnemies.Clear();
        _isShooting = false;

        // >>> Return to primary (idle) animation
        OnRequestPrimaryAnimation?.Invoke();
        Debug.Log("[Vacuum] All enemies shot!");
    }

    public override void OnUnequip()
    {
        base.OnUnequip();
        StopAllCoroutines();
        _isSucking = false;
        _isShooting = false;
        _pendingAbsorbs = 0;
        _suckedEnemies.Clear();
        if (_suckVFX != null) _suckVFX.Stop();
        if (_burstVFX != null) _burstVFX.Stop();
    }

    /// <summary>
    /// Flips VFX GameObjects on X-axis so particles match the player facing direction.
    /// Uses localScale.x = 1 (right) or -1 (left).
    /// </summary>
    private void FlipVFX()
    {
        if (_playerController == null) return;
        float scaleX = _playerController.FacingDirection >= 0 ? 1f : -1f;

        if (_suckVFX != null)
        {
            var t = _suckVFX.transform;
            var s = t.localScale;
            if (!Mathf.Approximately(s.x, scaleX))
                t.localScale = new Vector3(scaleX, s.y, s.z);
        }

        if (_burstVFX != null)
        {
            var t = _burstVFX.transform;
            var s = t.localScale;
            if (!Mathf.Approximately(s.x, scaleX))
                t.localScale = new Vector3(scaleX, s.y, s.z);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f); // Green half-transparent
        Vector2 pos = transform.position;
        Vector2 dir = GetAttackDirection();
        Vector2 boxSize = new Vector2(_suckRange, 1f);
        Vector2 boxCenter = pos + dir * (_suckRange * 0.5f);
        Gizmos.DrawWireCube(boxCenter, boxSize);
    }
}
