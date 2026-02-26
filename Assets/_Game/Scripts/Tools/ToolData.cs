using UnityEngine;

/// <summary>
/// Enum for tool behavior type — determines how the tool attacks.
/// </summary>
public enum ToolType
{
    Melee,      // Swing/stab — hitbox in front of player (Hammer, Chainsaw)
    Ranged,     // Fires projectile (Screwdriver, NailGun, TapeMeasure)
    Utility,    // Special behavior (Vacuum)
    Beam        // Continuous damage in a direction (Blowtorch, Magnet)
}


/// <summary>
/// ScriptableObject defining a single tool's stats and visuals.
/// Each tool in the game has one ToolData asset.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Tool Data")]
public class ToolData : ScriptableObject
{
    [Header("Identity")]
    public string toolName;
    [TextArea(1, 3)]
    public string description;
    public ToolType toolType;
    
    [Header("Unlocks & Pool")]
    [Tooltip("The High Score required to permanently unlock this tool.")]
    public int unlockScore = 0;
    [Tooltip("The weight of this tool appearing in the random pool. Higher weight = more likely.")]
    public float baseWeight = 10f;

    [Header("Combat Stats")]
    public int damage = 10;
    public float cooldown = 0.5f;
    [Tooltip("Range in units (melee hitbox length, beam length, etc.)")]
    public float attackParam = 2f;
    public bool pierce;
    [Tooltip("Knockback force applied to enemies on hit")]
    public float knockback = 2f;

    [Header("Visuals")]
    public Sprite toolIcon;
    public Sprite toolSprite;
    public GameObject attackPrefab;    // Projectile, hitbox, or effect prefab
    public RuntimeAnimatorController attackAnimator;

    [Header("Weapon Hold Position")]
    [Tooltip("Horizontal distance from player center when held")]
    public float weaponHoldDistance = 0f;
    [Tooltip("Vertical offset from player center when held")]
    public float weaponYOffset = 0f;

    [Header("Advanced Config")]
    [Tooltip("Optional per-tool behavior config (Magnet, Vacuum, etc.).")]
    public ToolBehaviorConfig behaviorConfig;

    [Header("Audio")]
    public AudioClip attackSFX;
}
