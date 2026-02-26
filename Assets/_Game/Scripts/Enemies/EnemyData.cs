using UnityEngine;

/// <summary>
/// ScriptableObject defining an enemy type's stats and behavior.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public EnemyMoveType moveType;

    [Header("Stats")]
    public int maxHP = 1;
    public float moveSpeed = 2f;
    public int contactDamage = 1;
    [Tooltip("Rigidbody2D gravity scale. Higher = falls faster.")]
    public float gravityScale = 3f;

    [Header("Spawn")]
    [Tooltip("Minimum score before this enemy starts spawning")]
    public int minScoreToSpawn = 0;

    [Header("Bottom Respawn")]
    [Tooltip("Speed multiplier when respawning from bottom (angry)")]
    public float respawnSpeedMultiplier = 2f;
    [Tooltip("Color tint when respawned (angry version)")]
    public Color respawnTint = Color.red;

    [Header("Visuals")]
    public Sprite enemySprite;
    [Tooltip("Sprite used when enemy becomes angry (after falling off screen)")]
    public Sprite angrySprite;
    public RuntimeAnimatorController animatorController;
    [Tooltip("Animator controller used when enemy becomes angry")]
    public RuntimeAnimatorController angryAnimatorController;
}

public enum EnemyMoveType
{
    Walker,     // Walks on platforms, falls off edges
    Flyer,      // Flies horizontally, bounces off walls
    Bouncer,    // Bounces around unpredictably
    Chaser,     // Slowly chases the player
    Splitter    // Splits into smaller enemies on death
}