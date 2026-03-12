using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// Handles interactive keybind rebinding with conflict detection.
/// Used by the Settings scene UI.
public class RebindManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset _inputActions;

    private InputActionRebindingExtensions.RebindingOperation _rebindOp;

    public event Action<InputAction, int> OnRebindStarted;
    public event Action<InputAction, int> OnRebindCompleted;
    public event Action<InputAction, int, string> OnRebindConflict;

    private void OnEnable()
    {
        LoadOverrides();
    }

    private void OnDestroy()
    {
        CleanupRebind();
    }

    /// Start interactive rebind for a specific action binding.
    public void StartRebind(InputAction action, int bindingIndex)
    {
        if (action == null || _rebindOp != null) return;

        action.Disable();
        OnRebindStarted?.Invoke(action, bindingIndex);

        _rebindOp = action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => FinishRebind(action, bindingIndex))
            .OnCancel(op =>
            {
                CleanupRebind();
                action.Enable();
                OnRebindCompleted?.Invoke(action, bindingIndex);
            })
            .Start();
    }

    private void FinishRebind(InputAction action, int bindingIndex)
    {
        string newPath = action.bindings[bindingIndex].effectivePath;

        // Check for conflicts across all bindings
        string conflict = CheckConflicts(action, bindingIndex, newPath);
        if (conflict != null)
        {
            // Revert and notify
            action.RemoveBindingOverride(bindingIndex);
            CleanupRebind();
            action.Enable();
            OnRebindConflict?.Invoke(action, bindingIndex, conflict);
            return;
        }

        CleanupRebind();
        action.Enable();
        SaveOverrides();
        OnRebindCompleted?.Invoke(action, bindingIndex);
    }

    /// Check if the new binding path conflicts with any other binding.
    private string CheckConflicts(InputAction reboundAction, int reboundIndex, string newPath)
    {
        foreach (var map in _inputActions.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (action == reboundAction && i == reboundIndex)
                        continue;

                    var binding = action.bindings[i];
                    if (binding.isComposite || binding.effectivePath != newPath)
                        continue;

                    return $"{action.actionMap.name}/{action.name}";
                }
            }
        }
        return null;
    }

    public void ResetToDefaults()
    {
        foreach (var map in _inputActions.actionMaps)
        {
            map.RemoveAllBindingOverrides();
        }
        SaveOverrides();
    }

    public void SaveOverrides()
    {
        string json = _inputActions.SaveBindingOverridesAsJson();
        SaveManager.Data.inputBindingOverrides = json;
        SaveManager.Save();
    }

    public void LoadOverrides()
    {
        string json = SaveManager.Data.inputBindingOverrides;
        if (!string.IsNullOrEmpty(json))
            _inputActions.LoadBindingOverridesFromJson(json);
    }

    private void CleanupRebind()
    {
        _rebindOp?.Dispose();
        _rebindOp = null;
    }
}
