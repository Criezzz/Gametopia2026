#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [MenuItem("Tools/Add Player Tag to Player Prefab")]
    public static void AddPlayerTagToPrefab()
    {
        string prefabPath = "Assets/_Game/Prefabs/Player/Player.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[AddPlayerTag] Prefab not found: {prefabPath}");
            return;
        }

        string path = AssetDatabase.GetAssetPath(prefab);
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        Transform existing = root.transform.Find("PlayerTag");
        if (existing != null)
        {
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[AddPlayerTag] PlayerTag already exists.");
            return;
        }

        GameObject tagObj = new GameObject("PlayerTag");
        tagObj.transform.SetParent(root.transform, false);
        tagObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        tagObj.transform.localScale = Vector3.one;

        var canvas = tagObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        tagObj.AddComponent<CanvasScaler>();
        tagObj.AddComponent<GraphicRaycaster>();

        RectTransform tagRT = tagObj.GetComponent<RectTransform>();
        tagRT.sizeDelta = new Vector2(2f, 1f);
        tagRT.localScale = new Vector3(0.02f, 0.02f, 0.02f);

        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(tagObj.transform, false);
        var tmp = labelObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "P1";
        tmp.fontSize = 48;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        RectTransform labelRT = labelObj.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 0.5f);
        labelRT.anchorMax = new Vector2(0.5f, 0.5f);
        labelRT.pivot = new Vector2(0.5f, 0.5f);
        labelRT.anchoredPosition = Vector2.zero;
        labelRT.sizeDelta = new Vector2(200f, 50f);

        var tagUI = tagObj.AddComponent<PlayerTagUI>();
        var so = new SerializedObject(tagUI);
        so.FindProperty("_labelText").objectReferenceValue = tmp;
        so.FindProperty("_heightOffset").floatValue = 1.2f;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[AddPlayerTag] PlayerTag added to Player prefab.");
    }
}
#endif