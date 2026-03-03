using UnityEngine;

/// <summary>
/// ScriptableObject configuring a horde event — a burst of enemies
/// spawned in quick succession. Has its own independent timing schedule.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Horde Config")]
public class HordeConfig : ScriptableObject
{
    [Header("Horde Composition")]
    [Tooltip("Prefab to spawn during the horde")]
    public GameObject enemyPrefab;
    [Tooltip("Number of enemies per horde burst")]
    public int enemyCount = 3;
    [Tooltip("Delay between each enemy in the burst (seconds)")]
    public float delayBetweenSpawns = 0.35f;

    [Header("Horde Timing")]
    [Tooltip("Seconds after game start before the first horde triggers")]
    public float firstHordeDelay = 20f;
    [Tooltip("Base interval between horde events (seconds)")]
    public float hordeInterval = 25f;
    [Tooltip("Every this many seconds the horde interval decreases")]
    public float difficultyTickInterval = 15f;
    [Tooltip("How much to reduce horde interval each difficulty tick (seconds)")]
    public float intervalReductionPerTick = 1f;
    [Tooltip("Minimum horde interval — will never go below this value")]
    public float minHordeInterval = 10f;
}
