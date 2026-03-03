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
            Debug.Log($"[SaveManager] Loaded from {path} — highScore: {_data.highScore}");
        }
        else
        {
            _data = new SaveData();
            Debug.Log($"[SaveManager] No save file found, using defaults. Path: {path}");
        }
    }

    /// <summary>
    /// Write current data to JSON file on disk.
    /// </summary>
    public static void Save()
    {
        string json = JsonUtility.ToJson(_data, true); // prettyPrint for easy editing
        File.WriteAllText(FilePath, json);
        Debug.Log($"[SaveManager] Saved to {FilePath}");
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
}
