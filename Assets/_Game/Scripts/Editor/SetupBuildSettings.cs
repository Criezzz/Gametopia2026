#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// Editor utility: configures Build Settings with all game scenes.
public class SetupBuildSettings
{
    [MenuItem("Tools/Setup Build Settings")]
    public static void Setup()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Game/Scenes" });
        List<string> allScenePaths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".unity"))
            .ToList();

        // Settings and Achievement are now in-scene panels inside MainMenu (slide transition).
        string[] excludedScenes =
        {
            "Assets/_Game/Scenes/Setting.unity",
            "Assets/_Game/Scenes/Achievement.unity",
        };
        foreach (string excluded in excludedScenes)
            allScenePaths.Remove(excluded);

        // Keep core scenes first for backward compatibility; append the rest alphabetically.
        string[] priority =
        {
            "Assets/_Game/Scenes/MainMenu.unity",
            "Assets/_Game/Scenes/Game.unity",
            "Assets/_Game/Scenes/MapPicker.unity",
            "Assets/_Game/Scenes/Arena.unity",
        };

        List<string> ordered = new();
        foreach (string path in priority)
        {
            if (allScenePaths.Remove(path))
                ordered.Add(path);
        }

        allScenePaths.Sort();
        ordered.AddRange(allScenePaths);

        EditorBuildSettings.scenes = ordered
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();

        Debug.Log($"[SetupBuildSettings] Build settings updated with {ordered.Count} scenes. " +
                  "Core scenes are prioritized, map scenes are auto-included.");
    }

    [MenuItem("Tools/Reset Save Data")]
    public static void ResetSaveData()
    {
        string path = Path.Combine(Application.persistentDataPath, "save_data.json");
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"[ResetSaveData] Deleted {path}");
        }
        else
        {
            Debug.Log($"[ResetSaveData] No save file found at {path}");
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[ResetSaveData] PlayerPrefs cleared. Restart Play mode to see fresh state.");
    }
}
#endif