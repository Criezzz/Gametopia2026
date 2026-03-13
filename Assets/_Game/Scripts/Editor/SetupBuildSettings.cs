#if UNITY_EDITOR
using System.Collections.Generic;
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

        // Keep core scenes first for backward compatibility; append the rest alphabetically.
        string[] priority =
        {
            "Assets/_Game/Scenes/MainMenu.unity",
            "Assets/_Game/Scenes/Game.unity",
            "Assets/_Game/Scenes/MapPicker.unity",
            "Assets/_Game/Scenes/Settings.unity",
            "Assets/_Game/Scenes/Achievement.unity",
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
}
#endif