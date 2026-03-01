using UnityEngine;

/// <summary>
/// Simple floating health bar for enemies (debug).
/// Add as a child of the enemy with a SpriteRenderer.
/// Uses a scaled sprite (pixel white or similar) as the bar fill.
/// No Canvas or UI required — pure SpriteRenderer world-space bar.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Bar Settings")]
    [SerializeField] private float _barWidth = 0.6f;
    [SerializeField] private float _barHeight = 0.08f;
    [SerializeField] private float _yOffset = 0.7f;
    [SerializeField] private Color _fillColor = Color.green;
    [SerializeField] private Color _bgColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
    [SerializeField] private int _sortingOrderOffset = 10;

    private SpriteRenderer _bgRenderer;
    private SpriteRenderer _fillRenderer;
    private Transform _fillTransform;

    private void Awake()
    {
        CreateBar();
    }

    private void CreateBar()
    {
        // Get or create a 1x1 white pixel sprite for scaling
        Sprite pixel = CreatePixelSprite();

        // --- Background ---
        GameObject bg = new GameObject("HealthBar_BG");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, _yOffset, 0f);
        bg.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);

        _bgRenderer = bg.AddComponent<SpriteRenderer>();
        _bgRenderer.sprite = pixel;
        _bgRenderer.color = _bgColor;
        _bgRenderer.sortingOrder = _sortingOrderOffset;

        // --- Fill ---
        GameObject fill = new GameObject("HealthBar_Fill");
        fill.transform.SetParent(transform, false);
        fill.transform.localPosition = new Vector3(0f, _yOffset, 0f);
        fill.transform.localScale = new Vector3(_barWidth, _barHeight, 1f);

        _fillRenderer = fill.AddComponent<SpriteRenderer>();
        _fillRenderer.sprite = pixel;
        _fillRenderer.color = _fillColor;
        _fillRenderer.sortingOrder = _sortingOrderOffset + 1;

        _fillTransform = fill.transform;
    }

    /// <summary>
    /// Update the health bar fill. Called by BaseEnemy.
    /// </summary>
    public void SetHP(int current, int max)
    {
        if (_fillTransform == null) return;

        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        // Scale X by ratio, keep Y/Z
        _fillTransform.localScale = new Vector3(_barWidth * ratio, _barHeight, 1f);

        // Anchor fill to the left edge (shift left when shrinking)
        float xOffset = -(_barWidth * (1f - ratio)) * 0.5f;
        _fillTransform.localPosition = new Vector3(xOffset, _yOffset, 0f);

        // Color: green → yellow → red
        if (ratio > 0.5f)
            _fillRenderer.color = Color.Lerp(Color.yellow, _fillColor, (ratio - 0.5f) * 2f);
        else
            _fillRenderer.color = Color.Lerp(Color.red, Color.yellow, ratio * 2f);

        // Hide bar when full HP (no damage taken)
        bool show = ratio < 1f && ratio > 0f;
        _bgRenderer.enabled = show;
        _fillRenderer.enabled = show;
    }

    /// <summary>
    /// Prevents the health bar from flipping with the parent sprite.
    /// </summary>
    private void LateUpdate()
    {
        // Keep bar horizontal regardless of parent scale/flip
        transform.localScale = new Vector3(
            Mathf.Sign(transform.parent.lossyScale.x),
            1f, 1f);
    }

    private static Sprite _cachedPixel;

    private static Sprite CreatePixelSprite()
    {
        if (_cachedPixel != null) return _cachedPixel;

        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;

        _cachedPixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _cachedPixel;
    }
}
