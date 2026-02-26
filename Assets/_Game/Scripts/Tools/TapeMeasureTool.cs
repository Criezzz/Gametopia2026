using UnityEngine;

/// <summary>
/// Tape Measure: Extends 4u forward dealing 5 damage (pierce), then retracts dealing 5 damage again.
/// Cooldown 0.7s.
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
    private int _damage = 3;

    // Track already hit enemies per swing to avoid double-hit in same direction
    private readonly System.Collections.Generic.HashSet<int> _hitThisExtend = new();
    private readonly System.Collections.Generic.HashSet<int> _hitThisRetract = new();

    public override void Initialize(ToolData data, PlayerController player)
    {
        base.Initialize(data, player);
        _maxLength = data != null ? data.attackParam : 4f;
        _damage = data != null ? data.damage : 3;
    }

    public override void Attack()
    {
        if (!CanAttack() || _isExtending || _isRetracting) return;

        _isExtending = true;
        _isRetracting = false;
        _currentLength = 0f;
        _hitThisExtend.Clear();
        _hitThisRetract.Clear();
    }

    protected override void Update()
    {
        base.Update();

        if (_isExtending)
        {
            _currentLength += _extendSpeed * Time.deltaTime;
            CheckHits(_hitThisExtend, _damage);

            if (_currentLength >= _maxLength)
            {
                _currentLength = _maxLength;
                _isExtending = false;
                _isRetracting = true;
            }
        }
        else if (_isRetracting)
        {
            _currentLength -= _extendSpeed * Time.deltaTime;
            CheckHits(_hitThisRetract, _damage);

            if (_currentLength <= 0f)
            {
                _currentLength = 0f;
                _isRetracting = false;
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
                    enemy.TakeDamage(damageToApply);
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
