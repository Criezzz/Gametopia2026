using UnityEngine;

/// Pixel-perfect camera scaler. Uses a fixed reference height for consistent ortho size.
[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    [Header("Pixel Settings")]
    [SerializeField] private int _ppu = 16;
    [SerializeField] private int _scale = 6;

    [Header("Reference Resolution")]
    [SerializeField] private int _referenceHeight = 1080;

    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.orthographic = true;
        UpdateOrthoSize();
    }

    private void UpdateOrthoSize()
    {
        float orthoSize = (_referenceHeight / (float)(_scale * _ppu)) * 0.5f;
        _camera.orthographicSize = orthoSize;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_ppu <= 0) _ppu = 16;
        if (_scale <= 0) _scale = 1;
        if (_referenceHeight <= 0) _referenceHeight = 1080;
    }
#endif
}