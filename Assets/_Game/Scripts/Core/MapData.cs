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

    /// <summary>
    /// Checks if the map is available for the given high score.
    /// </summary>
    public bool IsUnlocked(int currentHighScore)
    {
        return currentHighScore >= unlockScore;
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
