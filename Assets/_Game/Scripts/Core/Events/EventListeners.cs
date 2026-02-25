using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MonoBehaviour helper for VoidEventChannel.
/// Drag-drop an event channel in Inspector, auto Register/Unregister.
/// Wire responses via UnityEvent in Inspector.
/// </summary>
public class VoidEventListener : MonoBehaviour
{
    [SerializeField] private VoidEventChannel _channel;
    [SerializeField] private UnityEvent _onEventRaised;

    private void OnEnable()
    {
        if (_channel != null)
            _channel.Register(OnEventRaised);
    }

    private void OnDisable()
    {
        if (_channel != null)
            _channel.Unregister(OnEventRaised);
    }

    private void OnEventRaised()
    {
        _onEventRaised?.Invoke();
    }
}

/// <summary>
/// MonoBehaviour helper for IntEventChannel.
/// </summary>
public class IntEventListener : MonoBehaviour
{
    [SerializeField] private IntEventChannel _channel;
    [SerializeField] private UnityEvent<int> _onEventRaised;

    private void OnEnable()
    {
        if (_channel != null)
            _channel.Register(OnEventRaised);
    }

    private void OnDisable()
    {
        if (_channel != null)
            _channel.Unregister(OnEventRaised);
    }

    private void OnEventRaised(int value)
    {
        _onEventRaised?.Invoke(value);
    }
}

/// <summary>
/// MonoBehaviour helper for FloatEventChannel.
/// </summary>
public class FloatEventListener : MonoBehaviour
{
    [SerializeField] private FloatEventChannel _channel;
    [SerializeField] private UnityEvent<float> _onEventRaised;

    private void OnEnable()
    {
        if (_channel != null)
            _channel.Register(OnEventRaised);
    }

    private void OnDisable()
    {
        if (_channel != null)
            _channel.Unregister(OnEventRaised);
    }

    private void OnEventRaised(float value)
    {
        _onEventRaised?.Invoke(value);
    }
}

/// <summary>
/// MonoBehaviour helper for StringEventChannel.
/// </summary>
public class StringEventListener : MonoBehaviour
{
    [SerializeField] private StringEventChannel _channel;
    [SerializeField] private UnityEvent<string> _onEventRaised;

    private void OnEnable()
    {
        if (_channel != null)
            _channel.Register(OnEventRaised);
    }

    private void OnDisable()
    {
        if (_channel != null)
            _channel.Unregister(OnEventRaised);
    }

    private void OnEventRaised(string value)
    {
        _onEventRaised?.Invoke(value);
    }
}