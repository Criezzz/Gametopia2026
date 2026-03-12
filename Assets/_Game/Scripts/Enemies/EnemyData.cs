using UnityEngine;

/// <summary>
/// ScriptableObject defining an enemy type's stats, behavior, and spawn timing.
/// Each enemy type manages its own independent spawn schedule.
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

    [Header("Spawn Timing")]
    [Tooltip("Seconds after game start before the first enemy of this type spawns")]
    public float firstSpawnDelay = 0f;
    [Tooltip("Base interval between spawns (seconds)")]
    public float spawnInterval = 3f;
    [Tooltip("Every this many seconds the spawn interval decreases")]
    public float difficultyTickInterval = 10f;
    [Tooltip("How much to reduce spawn interval each difficulty tick (seconds)")]
    public float intervalReductionPerTick = 0.15f;
    [Tooltip("Minimum spawn interval — will never go below this value")]
    public float minSpawnInterval = 0.8f;

    [Header("Bottom Respawn")]
    [Tooltip("Speed multiplier when respawning from bottom (angry)")]
    public float respawnSpeedMultiplier = 1.5f;

    [Header("Visuals")]
    public Sprite enemySprite;
    [Tooltip("Sprite used when enemy becomes angry (after falling off screen)")]
    public Sprite angrySprite;
    public RuntimeAnimatorController animatorController;

    [Header("VFX")]
    [Tooltip("Particle prefab spawned when this enemy first appears")]
    public GameObject spawnVFXPrefab;
    [Tooltip("Particle prefab spawned when this enemy dies")]
    public GameObject deathVFXPrefab;

    [System.Obsolete("No longer used. Each normalAnimator.controller now contains both normal and angry states with an IsAngry bool transition.")]
    [HideInInspector]
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