using UnityEngine;

/// Enforces target aspect ratio (letterbox/pillarbox) and loads saved resolution.
[RequireComponent(typeof(Camera))]
public class AspectRatioEnforcer : MonoBehaviour
{
    [Header("Target Aspect Ratio")]
    [SerializeField] private float _targetWidth = 16f;
    [SerializeField] private float _targetHeight = 9f;

    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        ApplySavedResolution();
        EnforceAspectRatio();
    }

    private void Update()
    {
        EnforceAspectRatio();
    }

    private void ApplySavedResolution()
    {
        var data = SaveManager.Data;
        if (data.resolutionWidth > 0 && data.resolutionHeight > 0)
        {
            FullScreenMode mode = data.fullscreenMode >= 0
                ? (FullScreenMode)data.fullscreenMode
                : FullScreenMode.FullScreenWindow;
            Screen.SetResolution(data.resolutionWidth, data.resolutionHeight, mode);
        }
    }

    private void EnforceAspectRatio()
    {
        float targetAspect = _targetWidth / _targetHeight;
        float screenAspect = (float)Screen.width / Screen.height;

        if (Mathf.Approximately(screenAspect, targetAspect))
        {
            _cam.rect = new Rect(0f, 0f, 1f, 1f);
            return;
        }

        if (screenAspect > targetAspect)
        {
            // Pillarbox (wider than target)
            float viewportWidth = targetAspect / screenAspect;
            float x = (1f - viewportWidth) * 0.5f;
            _cam.rect = new Rect(x, 0f, viewportWidth, 1f);
        }
        else
        {
            // Letterbox (taller than target)
            float viewportHeight = screenAspect / targetAspect;
            float y = (1f - viewportHeight) * 0.5f;
            _cam.rect = new Rect(0f, y, 1f, viewportHeight);
        }
    }
}

