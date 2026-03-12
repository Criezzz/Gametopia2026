using UnityEngine;
using UnityEngine.UI; // Required for RawImage

/// <summary>
/// Scrolls a texture to create a parallax effect.
/// Supports both 3D Quads (Renderer) and UI Canvas elements (RawImage).
/// </summary>
public class BuildingScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Positive value scrolls right, negative scrolls left")]
    [SerializeField] private float _scrollSpeedX = 0.04f;
    [SerializeField] private float _scrollSpeedY = 0f;

    private Material _material;
    private RawImage _rawImage;

    private void Awake()
    {
        _rawImage = GetComponent<RawImage>();


        if (_rawImage == null)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                _material = renderer.material;
            }
        }
    }

    private void Update()
    {
        if (_rawImage != null)
        {
            Rect uvRect = _rawImage.uvRect;
            uvRect.x += _scrollSpeedX * Time.deltaTime;
            uvRect.y += _scrollSpeedY * Time.deltaTime;
            _rawImage.uvRect = uvRect;
        }
        else if (_material != null)
        {
            Vector2 offset = _material.mainTextureOffset;
            offset.x += _scrollSpeedX * Time.deltaTime;
            offset.y += _scrollSpeedY * Time.deltaTime;
            _material.mainTextureOffset = offset;
        }
    }

    private void OnDestroy()
    {
 
        if (_material != null)
            Destroy(_material);
    }
}