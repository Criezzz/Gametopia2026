using UnityEngine;

/// <summary>
/// Tape Measure: Two-phase melee weapon.
///   Phase 1 (Extend): Tape shoots forward dealing ToolData.damage (pierce).
///   Phase 2 (Retract): Tape retracts dealing ToolData.secondaryDamage (pierce, higher).
/// Each phase plays its own animation clip via the dual-animator system.
/// </summary>
public class TapeMeasureTool : BaseTool
{
    [Header("Tape Measure Settings")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _extendSpeed = 16f;

    private bool _isExtending;
    private bool _isRetracting;
    private float _currentLength;
    private float _maxLength = 4f;
    private int _extendDamage = 3;
    private int _retractDamage = 5;

    // Track already-hit enemies per phase to avoid double-hit
    private readonly System.Collections.Generic.HashSet<int> _hitThisExtend = new();
    private readonly System.Collections.Generic.HashSet<int> _hitThisRetract = new();

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        _maxLength = data != null ? data.attackParam : 4f;
        _extendDamage = data != null ? data.damage : 3;
        _retractDamage = data != null && data.secondaryDamage > 0 ? data.secondaryDamage : 5;
    }

    /// <summary>
    /// Block new attacks while extending or retracting — prevents PlayerToolHandler
    /// from re-triggering the Attack animation every frame.
    /// </summary>
    public override bool CanAttack()
    {
        return base.CanAttack() && !_isExtending && !_isRetracting;
    }

    public override void Attack()
    {
        if (!CanAttack() || _isExtending || _isRetracting) return;
        PlayAttackSFX();

        _isExtending = true;
        _isRetracting = false;
        _currentLength = 0f;
        _hitThisExtend.Clear();
        _hitThisRetract.Clear();
        // Phase 1 animation is already triggered by PlayerToolHandler
    }

    protected override void Update()
    {
        base.Update();

        if (_isExtending)
        {
            _currentLength += _extendSpeed * Time.deltaTime;
            CheckHits(_hitThisExtend, _extendDamage);

            if (_currentLength >= _maxLength)
            {
                _currentLength = _maxLength;
                _isExtending = false;
                _isRetracting = true;

                // >>> Phase 2: switch to secondary (retract) animation
                OnRequestSecondaryAnimation?.Invoke();
            }
        }
        else if (_isRetracting)
        {
            _currentLength -= _extendSpeed * Time.deltaTime;
            CheckHits(_hitThisRetract, _retractDamage);

            if (_currentLength <= 0f)
            {
                _currentLength = 0f;
                _isRetracting = false;

                // >>> Return to primary (idle) animation
                OnRequestPrimaryAnimation?.Invoke();
                StartCooldown();
            }
        }
    }

    private void CheckHits(System.Collections.Generic.HashSet<int> hitSet, int damageToApply)
    {
        Vector2 origin = (Vector2)transform.position;
        Vector2 dir = GetAttackDirection();

        // Thin box along the tape
        RaycastHit2D[] hits = Physics2D.BoxCastAll(
            origin, new Vector2(0.2f, 0.5f), 0f, dir, _currentLength, _enemyLayer);

        foreach (var hit in hits)
        {
            int id = hit.collider.GetInstanceID();
            if (!hitSet.Contains(id))
            {
                hitSet.Add(id);
                var enemy = hit.collider.GetComponent<BaseEnemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damageToApply, _toolData);
                }
            }
        }
    }

    public override void OnUnequip()
    {
        base.OnUnequip();
        _isExtending = false;
        _isRetracting = false;
        _currentLength = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.5f); // Cyan half-transparent
        Vector2 origin = (Vector2)transform.position;
        Vector2 dir = GetAttackDirection();
        float length = _currentLength > 0 ? _currentLength : (_maxLength > 0 ? _maxLength : 4f);
        
        Vector2 boxSize = new Vector2(0.2f, 0.5f);
        // Gizmos.DrawWireCube takes center. BoxCast goes from origin to origin + dir * length
        Vector2 center = origin + dir * (length * 0.5f);
        Gizmos.DrawWireCube(center, new Vector3(dir.x != 0 ? length : boxSize.x, dir.y != 0 ? length : boxSize.y, 1f));
    }
}
