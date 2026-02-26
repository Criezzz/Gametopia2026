using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject event channel with no payload.
/// Uses HashSet to prevent duplicate listener registration.
/// </summary>
[CreateAssetMenu(menuName = "BossRush/Events/Void Event Channel")]
public class VoidEventChannel : ScriptableObject
{
    private readonly HashSet<Action> _listeners = new();

#if UNITY_EDITOR
    [TextArea(2, 5)]
    [SerializeField] private string _description;
#endif

    public bool HasListeners => _listeners.Count > 0;

    public void Register(Action listener)
    {
        if (listener != null)
            _listeners.Add(listener);
    }

    public void Unregister(Action listener)
    {
        if (listener != null)
            _listeners.Remove(listener);
    }

    public void Raise()
    {
        // Iterate over a snapshot to avoid modification during enumeration
        foreach (var listener in _listeners.ToArray())
        {
            try
            {
                listener?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}