using UnityEngine;

/// <summary>
/// Per-map spawn configuration. Holds enemy spawn positions and toolbox
/// spawn zones so each map can define its own layout without duplicating scripts.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Map Spawn Config")]
public class MapSpawnConfig : ScriptableObject
{
    [Header("Enemy Spawn Area")]
    [Tooltip("X position for left-side enemy spawns")]
    public float spawnXLeft = -4f;
    [Tooltip("X position for right-side enemy spawns")]
    public float spawnXRight = 4f;
    [Tooltip("Offset above camera top edge where enemies appear")]
    public float spawnYOffset = 1f;

    [Header("Toolbox Spawn Zones")]
    [Tooltip("Platform zones the toolbox can respawn on. X picked randomly within range, Y is fixed.")]
    public ToolboxSpawnZone[] toolboxZones;

    [System.Serializable]
    public struct ToolboxSpawnZone
    {
        public float xMin;
        public float xMax;
        public float y;

        public ToolboxSpawnZone(float xMin, float xMax, float y)
        {
            this.xMin = xMin;
            this.xMax = xMax;
            this.y = y;
        }
    }
}
