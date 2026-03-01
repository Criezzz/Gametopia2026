using UnityEngine;

/// <summary>
/// ScriptableObject holding player stats for the platformer.
/// Jump physics are derived from maxJumpHeight and timeToMaxHeight:
///   gravity        = 2 · maxJumpHeight / timeToMaxHeight²
///   initialJumpVel = 2 · maxJumpHeight / timeToMaxHeight
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Jump")]
    [Tooltip("Maximum jump height in world units when holding the jump button.")]
    public float maxJumpHeight = 3.5f;

    [Tooltip("Time in seconds to reach max jump height. Lower = snappier arc.")]
    public float timeToMaxHeight = 0.25f;

    [Tooltip("Y-velocity multiplier when the player releases jump early (0 = instant stop, 1 = no cut). "
           + "Short hop height ≈ maxJumpHeight × multiplier².")]
    [Range(0f, 1f)]
    public float jumpCutMultiplier = 0.5f;

    [Header("Gravity")]
    [Tooltip("Gravity multiplier when falling (> 1 makes the player fall faster than they rise).")]
    public float fallGravityMultiplier = 1.5f;

    [Tooltip("Maximum downward speed (terminal velocity).")]
    public float maxFallSpeed = 25f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Audio")]
    public AudioClip jumpSFX;
}
