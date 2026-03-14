using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

/// Reads input directly from an InputActionAsset filtered by control scheme.
/// Does NOT use PlayerInput component — avoids device auto-pairing conflicts in local multiplayer.
public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Asset")]
    [SerializeField] private InputActionAsset _inputActions;

    [Header("Control Scheme")]
    [Tooltip("KeyboardLeft for P1, KeyboardRight for P2.")]
    [SerializeField] private string _controlScheme = "KeyboardLeft";

    private InputActionMap _playerMap;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _attackAction;
    private InputAction _pauseAction;

    // Buffered input state (read by PlayerController / PlayerToolHandler each frame)
    public float MoveInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool AttackHeld { get; private set; }
    public bool AttackReleased { get; private set; }
    public bool PausePressed { get; private set; }

    private void Awake()
    {
        if (_inputActions == null)
        {
            Debug.LogError($"[PlayerInputHandler] No InputActionAsset assigned on {gameObject.name}!");
            enabled = false;
            return;
        }

        // Clone asset so each player gets independent action state
        _inputActions = Instantiate(_inputActions);

        _playerMap = _inputActions.FindActionMap("Player");
        if (_playerMap == null)
        {
            Debug.LogError("[PlayerInputHandler] 'Player' action map not found!");
            enabled = false;
            return;
        }

        _moveAction = _playerMap.FindAction("Move");
        _jumpAction = _playerMap.FindAction("Jump");
        _attackAction = _playerMap.FindAction("Attack");
        _pauseAction = _playerMap.FindAction("Pause");

        // Apply only the bindings for our control scheme
        ApplyControlSchemeFilter();
    }

    private void OnEnable()
    {
        _playerMap?.Enable();
    }

    private void OnDisable()
    {
        _playerMap?.Disable();
    }

    private void OnDestroy()
    {
        if (_inputActions != null)
            Destroy(_inputActions);
    }

    private void Update()
    {
        if (_moveAction == null) return;

        MoveInput = _moveAction.ReadValue<Vector2>().x;

        JumpPressed = _jumpAction.WasPressedThisFrame();
        JumpHeld = _jumpAction.IsPressed();
        JumpReleased = _jumpAction.WasReleasedThisFrame();

        AttackHeld = _attackAction.IsPressed();
        AttackReleased = _attackAction.WasReleasedThisFrame();

        PausePressed = _pauseAction.WasPressedThisFrame();
    }

    /// Disable all bindings that don't belong to our control scheme.
    private void ApplyControlSchemeFilter()
    {
        if (string.IsNullOrEmpty(_controlScheme)) return;

        foreach (var action in _playerMap.actions)
        {
            var bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];

                // Skip composites themselves (they have no group)
                if (binding.isComposite) continue;

                // If binding has groups, check if our scheme is included
                if (!string.IsNullOrEmpty(binding.groups))
                {
                    bool belongsToScheme = false;
                    foreach (var group in binding.groups.Split(';'))
                    {
                        if (group.Trim() == _controlScheme)
                        {
                            belongsToScheme = true;
                            break;
                        }
                    }

                    if (!belongsToScheme)
                    {
                        // Override with empty path to disable this binding
                        action.ApplyBindingOverride(i, new InputBinding { overridePath = "" });
                    }
                }
            }
        }
    }

    /// Load saved binding overrides (call on startup).
    public void LoadBindingOverrides(string json)
    {
        if (!string.IsNullOrEmpty(json))
            _inputActions.LoadBindingOverridesFromJson(json);
    }

    /// Save current binding overrides as JSON.
    public string SaveBindingOverrides()
    {
        return _inputActions.SaveBindingOverridesAsJson();
    }

    /// Returns a human-readable display name for a bound key.
    /// For composite actions (e.g. Move), pass the part name ("left", "right", "up", "down").
    public string GetKeyDisplayName(string actionName, string compositePart = null)
    {
        InputAction action = _playerMap?.FindAction(actionName);
        if (action == null) return "?";

        ReadOnlyArray<InputBinding> bindings = action.bindings;
        for (int i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];
            if (binding.isComposite) continue;
            if (string.IsNullOrEmpty(binding.effectivePath)) continue;

            if (!string.IsNullOrEmpty(compositePart))
            {
                if (!binding.isPartOfComposite ||
                    !string.Equals(binding.name, compositePart, System.StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            else if (binding.isPartOfComposite)
            {
                continue;
            }

            return InputControlPath.ToHumanReadableString(
                binding.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        return "?";
    }
}
