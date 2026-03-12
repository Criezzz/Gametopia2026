using UnityEngine;

/// <summary>
/// ScriptableObject defining a playable Map's information and unlock milestone.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Identity")]
    public string mapName;
    public Sprite previewSprite;
    
    [Header("Unlocks")]
    [Tooltip("The High Score required to permanently unlock this map. 0 means always unlocked.")]
    public int unlockScore = 0;

    [Header("Scene Routing")]
    [Tooltip("Optional: Leave empty to use the default scene from GameMode. Fill to override (e.g., 'TutorialMap').")]
    public string sceneOverride;

    /// <summary>
    /// Checks if the map is available for the given high score.
    /// </summary>
    public bool IsUnlocked(int currentHighScore)
    {
        return currentHighScore >= unlockScore;
    }
}
