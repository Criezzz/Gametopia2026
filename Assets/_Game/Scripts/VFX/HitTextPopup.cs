using UnityEngine;

/// Sprite-based hit text popup. Spawns, arcs in a random direction with
/// scale punch + color cycling + fade out, then self-destructs.
/// Prefab: SpriteRenderer + this script. Use white sprites for full tint control.
[RequireComponent(typeof(SpriteRenderer))]
public class HitTextPopup : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float _launchSpeed = 3.5f;
    [SerializeField] private float _gravity = 6f;
    [SerializeField] private float _minAngle = 30f;
    [SerializeField] private float _maxAngle = 150f;

    [Header("Scale Punch")]
    [SerializeField] private float _punchScale = 1.5f;
    [SerializeField] private float _settleScale = 1f;
    [SerializeField] private float _punchDuration = 0.15f;

    [Header("Color Cycle")]
    [SerializeField] private Color[] _colors = new[]
    {
        Color.white,
        Color.yellow,
        new Color(1f, 0.4f, 0.1f), // orange
        Color.cyan
    };
    [SerializeField] private float _colorInterval = 0.06f;

    [Header("Lifetime")]
    [SerializeField] private float _lifetime = 0.7f;
    [SerializeField] private float _fadeStartRatio = 0.6f;

    [Header("Rendering")]
    [Tooltip("Sorting layer name for the popup sprite (must exist in project)")]
    [SerializeField] private string _sortingLayerName = "UI";
    [Tooltip("Sorting order within the layer")]
    [SerializeField] private int _sortingOrder = 20;

    private SpriteRenderer _sr;
    private Vector2 _velocity;
    private float _elapsed;
    private float _colorTimer;
    private int _colorIndex;

    /// Call once right after Instantiate to set the sprite.
    public void Setup(Sprite sprite)
    {
        if (_sr == null) _sr = GetComponent<SpriteRenderer>();
        _sr.sprite = sprite;
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        // Ensure this renders on top of everything
        _sr.sortingLayerName = _sortingLayerName;
        _sr.sortingOrder = _sortingOrder;

        // Random launch direction (arc upward, biased left/right)
        float angle = Random.Range(_minAngle, _maxAngle) * Mathf.Deg2Rad;
        _velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _launchSpeed;

        // Start at punch scale
        transform.localScale = Vector3.one * _punchScale;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed >= _lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float t = _elapsed / _lifetime;

        // --- Arc motion ---
        _velocity.y -= _gravity * Time.deltaTime;
        transform.position += (Vector3)_velocity * Time.deltaTime;

        // --- Scale punch → settle ---
        float scaleT = Mathf.Clamp01(_elapsed / _punchDuration);
        float scale;
        if (scaleT < 0.5f)
            scale = Mathf.Lerp(_punchScale, _settleScale * 0.85f, scaleT * 2f);
        else
            scale = Mathf.Lerp(_settleScale * 0.85f, _settleScale, (scaleT - 0.5f) * 2f);
        transform.localScale = Vector3.one * scale;

        // --- Color cycle ---
        _colorTimer += Time.deltaTime;
        if (_colors.Length > 0 && _colorTimer >= _colorInterval)
        {
            _colorTimer -= _colorInterval;
            _colorIndex = (_colorIndex + 1) % _colors.Length;
        }

        // --- Fade out ---
        Color c = _colors.Length > 0 ? _colors[_colorIndex] : Color.white;
        if (t > _fadeStartRatio)
        {
            float fadeT = (t - _fadeStartRatio) / (1f - _fadeStartRatio);
            c.a = Mathf.Lerp(1f, 0f, fadeT);
        }
        _sr.color = c;
    }
}
