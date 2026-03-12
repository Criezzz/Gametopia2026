/// Serializable data container for player save.
/// New fields are default-initialized automatically by JsonUtility for older saves.
[System.Serializable]
public class SaveData
{
    public int highScore = 0;

    // Lifetime stats (tracked across all runs)
    public int totalToolPickups = 0;
    public int totalEnemiesKilled = 0;
    public int totalGamesPlayed = 0;

    // Input binding overrides (JSON from InputSystem)
    public string inputBindingOverrides = "";

    // Resolution preferences
    public int resolutionWidth = 0;
    public int resolutionHeight = 0;
    public int fullscreenMode = -1; // -1 = use default
}
