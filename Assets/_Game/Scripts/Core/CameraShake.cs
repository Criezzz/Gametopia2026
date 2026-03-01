using System.Collections;
using UnityEngine;

/// <summary>
/// Simple camera shake utility. Attach to Main Camera.
/// Call Shake() from anywhere via the static Instance.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 _originalLocalPos;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        Instance = this;
        _originalLocalPos = transform.localPosition;
    }

    /// <summary>
    /// Trigger a camera shake.
    /// </summary>
    /// <param name="duration">How long to shake (seconds)</param>
    /// <param name="magnitude">Max offset in units</param>
    public void Shake(float duration = 0.15f, float magnitude = 0.1f)
    {
        if (_shakeCoroutine != null)
            StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = _originalLocalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = _originalLocalPos;
        _shakeCoroutine = null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
