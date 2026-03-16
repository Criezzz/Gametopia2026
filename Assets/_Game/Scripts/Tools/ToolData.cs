using UnityEngine;
using UnityEngine.Serialization;

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
    [FormerlySerializedAs("unlockScore")]
    [Tooltip("Total crate pickups required to permanently unlock this tool.")]
    public int unlockPickupCount = 0;
    [Tooltip("The weight of this tool appearing in the random pool. Higher weight = more likely.")]
    public float baseWeight = 10f;

    [Header("Combat Stats")]
    public int damage = 10;
    [Tooltip("Secondary damage for multi-phase tools (e.g. Tape retract, Vacuum shoot)")]
    public int secondaryDamage = 0;
    public float cooldown = 0.5f;
    [Tooltip("Range in units (melee hitbox length, beam length, etc.)")]
    public float attackParam = 2f;
    public bool pierce;

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
    [Tooltip("Sound played at Phase 2 shoot — fires once per enemy shot (used by Vacuum)")]
    public AudioClip secondaryAttackSFX;
    [Tooltip("Sound played when this tool hits an enemy")]
    public AudioClip hitSFX;

    [Header("VFX")]
    [Tooltip("Particle prefab spawned at enemy position on hit")]
    public GameObject hitVFXPrefab;
    [Tooltip("Minimum interval between hit VFX spawns on the same enemy (seconds). 0 = every hit.")]
    [Min(0f)] public float hitVFXMinInterval = 0f;
    [Tooltip("Optional one-shot VFX spawned when this tool lands the killing blow.")]
    public GameObject killVFXPrefab;
    [Tooltip("Prefab with HitTextPopup + SpriteRenderer (white sprite). Spawned on hit.")]
    public GameObject hitTextPrefab;
    [Tooltip("White pixel-art text sprites (WHACK, POW, BAM…). One picked at random per hit.")]
    public Sprite[] hitTextSprites;
}
