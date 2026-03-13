using System.IO;
using UnityEngine;

/// <summary>
/// Simple JSON save/load for player progress.
/// File is saved at Application.persistentDataPath/save_data.json
/// — easily inspectable and editable for testing.
///
/// Usage:
///   SaveManager.Load();            // reads from disk (call once at startup)
///   SaveManager.Data.highScore     // read value
///   SaveManager.Data.highScore = 5 // write value
///   SaveManager.Save();            // flush to disk
///   SaveManager.ResetAll();        // delete file + reset to defaults
///
/// File location (Windows):
///   C:\Users\{user}\AppData\LocalLow\{company}\{product}\save_data.json
/// </summary>
public static class SaveManager
{
    private const string FileName = "save_data.json";

    private static SaveData _data;

    /// <summary>Current save data (auto-loads on first access if not loaded yet).</summary>
    public static SaveData Data
    {
        get
        {
            if (_data == null) Load();
            return _data;
        }
    }

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>
    /// Load save data from JSON file. Creates default data if file doesn't exist.
    /// </summary>
    public static void Load()
    {
        string path = FilePath;
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            _data = JsonUtility.FromJson<SaveData>(json);
            SanitizeData();
            Debug.Log($"[SaveManager] Loaded from {path} — highScore: {_data.highScore}");
        }
        else
        {
            _data = new SaveData();
            SanitizeData();
            Debug.Log($"[SaveManager] No save file found, using defaults. Path: {path}");
        }
    }

    /// <summary>
    /// Write current data to JSON file on disk.
    /// </summary>
    public static void Save()
    {
        if (_data == null)
            _data = new SaveData();

        SanitizeData();

        string json = JsonUtility.ToJson(_data, true); // prettyPrint for easy editing
        File.WriteAllText(FilePath, json);
        Debug.Log($"[SaveManager] Saved to {FilePath}");
    }

    /// <summary>
    /// Returns high score for a specific map id. Unknown map returns 0.
    /// </summary>
    public static int GetMapHighScore(string mapId)
    {
        string key = NormalizeMapId(mapId);
        if (string.IsNullOrEmpty(key)) return 0;

        var entries = Data.mapHighScores;
        for (int i = 0; i < entries.Count; i++)
        {
            MapHighScoreEntry entry = entries[i];
            if (entry != null && string.Equals(entry.mapId, key, System.StringComparison.Ordinal))
                return Mathf.Max(0, entry.highScore);
        }

        return 0;
    }

    /// <summary>
    /// Sets high score for a map if the new score is higher than the existing value.
    /// </summary>
    public static void SetMapHighScore(string mapId, int score)
    {
        string key = NormalizeMapId(mapId);
        if (string.IsNullOrEmpty(key)) return;

        score = Mathf.Max(0, score);
        var entries = Data.mapHighScores;

        for (int i = 0; i < entries.Count; i++)
        {
            MapHighScoreEntry entry = entries[i];
            if (entry == null) continue;
            if (!string.Equals(entry.mapId, key, System.StringComparison.Ordinal)) continue;

            if (score > entry.highScore)
                entry.highScore = score;
            return;
        }

        entries.Add(new MapHighScoreEntry { mapId = key, highScore = score });
    }

    /// <summary>
    /// Delete save file and reset to defaults. Use for testing milestones.
    /// </summary>
    public static void ResetAll()
    {
        string path = FilePath;
        if (File.Exists(path))
            File.Delete(path);

        // Also clear legacy PlayerPrefs key
        PlayerPrefs.DeleteKey("HighScore");
        PlayerPrefs.Save();

        _data = new SaveData();
        Save();
        Debug.Log($"[SaveManager] Save data RESET. highScore: 0");
    }

    private static void SanitizeData()
    {
        if (_data == null)
            _data = new SaveData();

        if (_data.mapHighScores == null)
            _data.mapHighScores = new System.Collections.Generic.List<MapHighScoreEntry>();

        if (_data.highScore < 0)
            _data.highScore = 0;
    }

    private static string NormalizeMapId(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId))
            return string.Empty;

        return mapId.Trim();
    }
}
