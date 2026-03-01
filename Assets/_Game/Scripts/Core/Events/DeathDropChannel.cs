using UnityEngine;

/// <summary>
/// Data passed when an enemy dies and a death-drop sprite should be spawned.
/// </summary>
[System.Serializable]
public struct DeathDropData
{
    /// <summary>The sprite of the dead enemy.</summary>
    public Sprite sprite;

    /// <summary>World position where the enemy died.</summary>
    public Vector2 position;
}

/// <summary>
/// ScriptableObject event channel carrying DeathDropData.
/// Raised when an enemy dies. A listener spawns the falling sprite visual.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Events/Death Drop Channel")]
public class DeathDropChannel : EventChannel<DeathDropData> { }
