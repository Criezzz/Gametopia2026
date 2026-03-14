using UnityEngine;
using UnityEngine.UI;

/// Pulses an Image's alpha between min and max for a shining/glow effect.
public class GlowPulse : MonoBehaviour
{
    [SerializeField] private float _speed = 2.5f;
    [SerializeField] private float _minAlpha = 0.2f;
    [SerializeField] private float _maxAlpha = 0.7f;

    private Image _image;
    private float _offset;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _offset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (_image == null) return;
        float t = (Mathf.Sin(Time.time * _speed + _offset) + 1f) * 0.5f;
        var c = _image.color;
        c.a = Mathf.Lerp(_minAlpha, _maxAlpha, t);
        _image.color = c;
    }
}
