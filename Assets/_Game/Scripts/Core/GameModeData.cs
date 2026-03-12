using UnityEngine;

public enum GameModeType
{
    Solo,
    Arena
}

/// Defines a game mode (Solo or Arena) with player count and target scene.
[CreateAssetMenu(menuName = "ToolCrate/Game Mode")]
public class GameModeData : ScriptableObject
{
    public GameModeType modeType = GameModeType.Solo;
    public int playerCount = 1;
    public string sceneName;
}
