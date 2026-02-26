using UnityEngine;

/// <summary>
/// ScriptableObject event channel carrying EnemyFallData.
/// Raised when an enemy falls off the bottom of the screen.
/// EnemySpawner listens to respawn the enemy as angry.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Events/Enemy Fall Channel")]
public class EnemyFallChannel : EventChannel<EnemyFallData> { }
