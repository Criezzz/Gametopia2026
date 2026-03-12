#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// Editor utility: configures Build Settings with all game scenes.
public class SetupBuildSettings
{
    [MenuItem("Tools/Setup Build Settings")]
    public static void Setup()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/_Game/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/_Game/Scenes/Game.unity", true),
            new EditorBuildSettingsScene("Assets/_Game/Scenes/MapPicker.unity", true),
            new EditorBuildSettingsScene("Assets/_Game/Scenes/Settings.unity", true),
            new EditorBuildSettingsScene("Assets/_Game/Scenes/Achievement.unity", true),
            new EditorBuildSettingsScene("Assets/_Game/Scenes/Arena.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[SetupBuildSettings] Build settings updated: MainMenu=0, Game=1, MapPicker=2, Settings=3, Achievement=4, Arena=5.");
    }
}
#endif