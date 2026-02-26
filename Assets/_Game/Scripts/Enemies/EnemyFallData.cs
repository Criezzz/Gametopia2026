/// <summary>
/// Data passed when an enemy falls off the bottom of the screen.
/// Used by EnemyFallChannel to request respawn.
/// </summary>
[System.Serializable]
public struct EnemyFallData
{
    /// <summary>The enemy's ScriptableObject data (type, speed, etc.).</summary>
    public EnemyData enemyData;

    /// <summary>True if the enemy was already angry before falling.</summary>
    public bool wasAngry;
}
