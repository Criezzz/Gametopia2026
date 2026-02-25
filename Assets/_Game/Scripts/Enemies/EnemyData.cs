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

    [Header("Spawn")]
    [Tooltip("Minimum score before this enemy starts spawning")]
    public int minScoreToSpawn = 0;

    [Header("Bottom Respawn")]
    [Tooltip("Speed multiplier when respawning from bottom")]
    public float respawnSpeedMultiplier = 1.5f;
    [Tooltip("Color tint when respawned (angry version)")]
    public Color respawnTint = Color.red;

    [Header("Visuals")]
    public Sprite enemySprite;
    public RuntimeAnimatorController animatorController;
}

public enum EnemyMoveType
{
    Walker,     // Walks on platforms, falls off edges
    Flyer,      // Flies horizontally, bounces off walls
    Bouncer,    // Bounces around unpredictably
    Chaser,     // Slowly chases the player
    Splitter    // Splits into smaller enemies on death
}