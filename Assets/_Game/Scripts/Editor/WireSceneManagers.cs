#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time editor utility: wires GameManager and EnemySpawner serialized fields.
/// Run via: Tools > Wire Scene Managers.
/// </summary>
public class WireSceneManagers
{
    [MenuItem("Tools/Wire Scene Managers")]
    public static void Wire()
    {
        WireGameManager();
        WireEnemySpawner();
        EditorUtility.SetDirty(GameObject.Find("GameManager"));
        EditorUtility.SetDirty(GameObject.Find("EnemySpawner"));
        Debug.Log("[WireSceneManagers] Done! All references wired.");
    }

    static void WireGameManager()
    {
        var gm = GameObject.Find("GameManager");
        if (gm == null) { Debug.LogError("GameManager not found!"); return; }

        var comp = gm.GetComponent<GameManager>();
        if (comp == null) { Debug.LogError("GameManager component not found!"); return; }

        var so = new SerializedObject(comp);

        // Wire _allTools array (8 tools)
        string[] toolPaths = {
            "Assets/_Game/Data/ToolData/Hammer.asset",
            "Assets/_Game/Data/ToolData/Screwdriver.asset",
            "Assets/_Game/Data/ToolData/TapeMeasure.asset",
            "Assets/_Game/Data/ToolData/NailGun.asset",
            "Assets/_Game/Data/ToolData/Blowtorch.asset",
            "Assets/_Game/Data/ToolData/Vacuum.asset",
            "Assets/_Game/Data/ToolData/Magnet.asset",
            "Assets/_Game/Data/ToolData/Chainsaw.asset"
        };

        var allToolsProp = so.FindProperty("_allTools");
        if (allToolsProp != null)
        {
            allToolsProp.arraySize = toolPaths.Length;
            for (int i = 0; i < toolPaths.Length; i++)
            {
                var tool = AssetDatabase.LoadAssetAtPath<ToolData>(toolPaths[i]);
                allToolsProp.GetArrayElementAtIndex(i).objectReferenceValue = tool;
            }
        }

        // Wire event channels - VoidEventChannel
        SetRef(so, "_onToolPickedUp", AssetDatabase.LoadAssetAtPath<VoidEventChannel>("Assets/_Game/Data/EventChannels/OnToolPickedUp.asset"));
        SetRef(so, "_onPlayerDied", AssetDatabase.LoadAssetAtPath<VoidEventChannel>("Assets/_Game/Data/EventChannels/OnPlayerDied.asset"));
        SetRef(so, "_onGameOver", AssetDatabase.LoadAssetAtPath<VoidEventChannel>("Assets/_Game/Data/EventChannels/OnGameOver.asset"));
        SetRef(so, "_onGamePaused", AssetDatabase.LoadAssetAtPath<VoidEventChannel>("Assets/_Game/Data/EventChannels/OnGamePaused.asset"));
        SetRef(so, "_onGameRestart", AssetDatabase.LoadAssetAtPath<VoidEventChannel>("Assets/_Game/Data/EventChannels/OnGameRestart.asset"));

        // Wire event channels - IntEventChannel
        SetRef(so, "_onScoreChanged", AssetDatabase.LoadAssetAtPath<IntEventChannel>("Assets/_Game/Data/EventChannels/OnScoreChanged.asset"));
        SetRef(so, "_onMilestoneReached", AssetDatabase.LoadAssetAtPath<IntEventChannel>("Assets/_Game/Data/EventChannels/OnMilestoneReached.asset"));
        SetRef(so, "_onToolEquipped", AssetDatabase.LoadAssetAtPath<ToolDataEventChannel>("Assets/_Game/Data/EventChannels/OnToolEquipped.asset"));
        SetRef(so, "_firstPickupTool", AssetDatabase.LoadAssetAtPath<ToolData>("Assets/_Game/Data/ToolData/Hammer.asset"));

        so.ApplyModifiedProperties();
        Debug.Log("[WireSceneManagers] GameManager wired.");
    }

    static void WireEnemySpawner()
    {
        var spawner = GameObject.Find("EnemySpawner");
        if (spawner == null) { Debug.LogError("EnemySpawner not found!"); return; }

        var comp = spawner.GetComponent<EnemySpawner>();
        if (comp == null) { Debug.LogError("EnemySpawner component not found!"); return; }

        var so = new SerializedObject(comp);

        // Wire prefabs
        var walkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/WalkerEnemy.prefab");
        var walkerProp = so.FindProperty("_walkerPrefab");

        if (walkerProp != null) walkerProp.objectReferenceValue = walkerPrefab;

        so.ApplyModifiedProperties();
        Debug.Log("[WireSceneManagers] EnemySpawner wired.");
    }

    static void SetRef(SerializedObject so, string propName, Object asset)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) return;
        prop.objectReferenceValue = asset;
    }
}
#endif
