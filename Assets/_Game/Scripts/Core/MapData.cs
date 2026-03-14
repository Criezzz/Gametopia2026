using UnityEngine;

/// <summary>
/// ScriptableObject defining a playable Map's information and unlock milestone.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Identity")]
    public string mapName;
    [Tooltip("Stable ID for save data (recommended: map_01, map_02...). Leave empty to fallback to mapName.")]
    public string mapId;
    public Sprite previewSprite;
    
    [Header("Unlocks")]
    [Tooltip("The High Score required to permanently unlock this map. 0 means always unlocked.")]
    public int unlockScore = 0;
    [Tooltip("Total enemies killed required to unlock. Takes priority over unlockScore when > 0.")]
    public int unlockKills = 0;

    [Header("Spawn Configuration")]
    [Tooltip("Per-map spawn positions for enemies and toolbox. Leave null to use spawner defaults.")]
    public MapSpawnConfig spawnConfig;

    [Header("Scene Routing")]
    [Tooltip("Solo scene override. Leave empty to use GameModeData.sceneName (default: Game).")]
    public string soloSceneOverride;
    [Tooltip("Arena scene override. Leave empty to use GameModeData.sceneName (default: Arena).")]
    public string arenaSceneOverride;

    [System.Obsolete("Use soloSceneOverride / arenaSceneOverride instead.")]
    [HideInInspector]
    public string sceneOverride;

    /// Returns the correct scene name for the given mode, falling back to mode default.
    public string GetSceneForMode(GameModeData mode)
    {
        if (mode == null) return null;

        string over = mode.modeType == GameModeType.Arena
            ? arenaSceneOverride
            : soloSceneOverride;

        if (!string.IsNullOrEmpty(over)) return over;
        return mode.sceneName;
    }

    /// Checks if the map is unlocked given saved progress.
    public bool IsUnlocked(SaveData data)
    {
        if (data == null) return unlockScore <= 0 && unlockKills <= 0;
        if (unlockKills > 0) return data.totalEnemiesKilled >= unlockKills;
        return data.highScore >= unlockScore;
    }

    /// Backwards-compatible overload for callers that only have high score.
    public bool IsUnlocked(int currentHighScore)
    {
        if (unlockKills > 0) return SaveManager.Data.totalEnemiesKilled >= unlockKills;
        return currentHighScore >= unlockScore;
    }

    /// Human-readable description of the unlock condition.
    public string GetUnlockDescription()
    {
        if (unlockKills > 0) return $"Kill {unlockKills} enemies";
        if (unlockScore > 0) return $"Score {unlockScore}";
        return "Always unlocked";
    }

    /// <summary>
    /// Stable key used by save data for per-map progress.
    /// </summary>
    public string GetPersistentId()
    {
        if (!string.IsNullOrWhiteSpace(mapId))
            return mapId.Trim();

        if (!string.IsNullOrWhiteSpace(mapName))
            return mapName.Trim();

        return name;
    }
}
