using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generic ScriptableObject event channel with typed payload.
/// Uses HashSet to prevent duplicate listener registration.
/// Concrete subclasses required for Unity serialization.
/// </summary>
public abstract class EventChannel<T> : ScriptableObject
{
    private readonly HashSet<Action<T>> _listeners = new();

#if UNITY_EDITOR
    [TextArea(2, 5)]
    [SerializeField] private string _description;
#endif

    public bool HasListeners => _listeners.Count > 0;

    public void Register(Action<T> listener)
    {
        if (listener != null)
            _listeners.Add(listener);
    }

    public void Unregister(Action<T> listener)
    {
        if (listener != null)
            _listeners.Remove(listener);
    }

    public void Raise(T value)
    {
        foreach (var listener in _listeners.ToArray())
        {
            try
            {
                listener?.Invoke(value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
