using UnityEngine;

/// <summary>
/// Custom pixel-perfect camera scaler.
/// Sets orthographic size based on screen resolution, scale factor, and PPU.
/// Formula: orthoSize = (Screen.currentResolution.height / (scale * PPU)) * 0.5f
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    [Header("Pixel Settings")]
    [SerializeField] private int _ppu = 16;
    [SerializeField] private int _scale = 6; // 1080 / 180 = 6x scale for Full HD

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.orthographic = true;
        UpdateOrthoSize();
    }

    private void Update()
    {
        // Recalculate if resolution changes (e.g. window resize)
        UpdateOrthoSize();
    }

    private void UpdateOrthoSize()
    {
        float orthoSize = (Screen.currentResolution.height / (float)(_scale * _ppu)) * 0.5f;
        _camera.orthographicSize = orthoSize;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_ppu <= 0) _ppu = 16;
        if (_scale <= 0) _scale = 1;
    }
#endif
}