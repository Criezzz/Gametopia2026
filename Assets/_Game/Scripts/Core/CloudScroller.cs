using UnityEngine;

/// <summary>
/// Scrolls a material's UV offset continuously to create a parallax cloud effect.
/// Attach to a Quad with a material that has Wrap Mode = Repeat.
/// Default: scrolls clouds from left to right.
/// </summary>
public class CloudScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private float _scrollSpeed = 0.02f;

    private Material _material;

    private void Awake()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Use instance material to avoid modifying shared material
            _material = renderer.material;
        }
    }

    private void Update()
    {
        if (_material == null) return;

        Vector2 offset = _material.mainTextureOffset;
        // Negative X = clouds visually move to the right
        offset.x -= _scrollSpeed * Time.deltaTime;
        _material.mainTextureOffset = offset;
    }

    private void OnDestroy()
    {
        // Clean up instance material
        if (_material != null)
            Destroy(_material);
    }
}