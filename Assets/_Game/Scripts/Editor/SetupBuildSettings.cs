#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-time editor utility: configures Build Settings with MainMenu (0) and Game (1).
/// </summary>
public class SetupBuildSettings
{
    [MenuItem("Tools/Setup Build Settings")]
    public static void Setup()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/_Game/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/_Game/Scenes/Game.unity", true)
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[SetupBuildSettings] Build settings updated! MainMenu=0, Game=1.");
    }
}
#endif