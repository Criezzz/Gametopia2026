#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time editor utility: adds PlayerController + all tool scripts to the Player GameObject, creates GroundCheck child.
/// Then sets up enemy and toolbox prefabs. Run via: Tools > Setup Prefabs.
/// </summary>
public class PrefabSetupUtility
{
    [MenuItem("Tools/Setup Prefabs")]
    public static void SetupPrefabs()
    {
        SetupPlayer();
        SetupEnemyPrefab("WalkerEnemy", "Assets/_Game/Art/Sprites/Placeholder_EnemyWalker.png", "Assets/_Game/Data/EnemyData/Walker.asset");
        SetupEnemyPrefab("FlyerEnemy", "Assets/_Game/Art/Sprites/Placeholder_EnemyFlyer.png", "Assets/_Game/Data/EnemyData/Flyer.asset");
        SetupToolbox();
        SaveAllAsPrefabs();
        Debug.Log("[PrefabSetupUtility] All prefabs created successfully!");
    }

    static void SetupPlayer()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("Player not found in scene!"); return; }

        // Add PlayerController if missing
        if (player.GetComponent<PlayerController>() == null)
            player.AddComponent<PlayerController>();

        // Add tool scripts if missing
        if (player.GetComponent<HammerTool>() == null) player.AddComponent<HammerTool>();
        if (player.GetComponent<ScrewdriverTool>() == null) player.AddComponent<ScrewdriverTool>();
        if (player.GetComponent<TapeMeasureTool>() == null) player.AddComponent<TapeMeasureTool>();
        if (player.GetComponent<NailGunTool>() == null) player.AddComponent<NailGunTool>();
        if (player.GetComponent<BlowtorchTool>() == null) player.AddComponent<BlowtorchTool>();
        if (player.GetComponent<VacuumTool>() == null) player.AddComponent<VacuumTool>();
        if (player.GetComponent<MagnetTool>() == null) player.AddComponent<MagnetTool>();
        if (player.GetComponent<ChainsawTool>() == null) player.AddComponent<ChainsawTool>();

        // Create GroundCheck child if missing
        var groundCheck = player.transform.Find("GroundCheck");
        if (groundCheck == null)
        {
            var go = new GameObject("GroundCheck");
            go.transform.SetParent(player.transform);
            go.transform.localPosition = new Vector3(0, -0.5f, 0);
        }

        // Set sprite
        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Sprites/Placeholder_Player.png");
            if (sprite != null) sr.sprite = sprite;
        }

        // Wire serialized fields
        var so = new SerializedObject(player.GetComponent<PlayerController>());
        // PlayerData
        var playerData = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/_Game/Data/PlayerData/DefaultPlayerData.asset");
        if (playerData != null)
            so.FindProperty("_data").objectReferenceValue = playerData;
        // Ground check point
        var gcp = player.transform.Find("GroundCheck");
        if (gcp != null)
            so.FindProperty("_groundCheckPoint").objectReferenceValue = gcp;
        // Ground layer
        so.FindProperty("_groundLayer").intValue = 1 << LayerMask.NameToLayer("Ground");
        so.ApplyModifiedProperties();

        // Wire PlayerHealth
        WirePlayerHealth(player);

        // Wire PlayerToolHandler
        WireToolHandler(player);

        Debug.Log("[PrefabSetupUtility] Player setup done.");
    }

    static void WirePlayerHealth(GameObject player)
    {
        var health = player.GetComponent<PlayerHealth>();
        if (health == null) return;

        var so = new SerializedObject(health);

        // Wire event channels
        var onDied = AssetDatabase.LoadAssetAtPath<VoidEventChannel>("Assets/_Game/Data/EventChannels/OnPlayerDied.asset");
        if (onDied != null)
        {
            var prop = so.FindProperty("_onPlayerDied");
            if (prop != null) prop.objectReferenceValue = onDied;
        }

        var onHPChanged = AssetDatabase.LoadAssetAtPath<FloatEventChannel>("Assets/_Game/Data/EventChannels/OnPlayerHPChanged.asset");
        if (onHPChanged != null)
        {
            var prop = so.FindProperty("_onPlayerHPChanged");
            if (prop != null) prop.objectReferenceValue = onHPChanged;
        }

        // Wire player data
        var playerData = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/_Game/Data/PlayerData/DefaultPlayerData.asset");
        if (playerData != null)
        {
            var prop = so.FindProperty("_data");
            if (prop != null) prop.objectReferenceValue = playerData;
        }

        so.ApplyModifiedProperties();
    }

    static void WireToolHandler(GameObject player)
    {
        var handler = player.GetComponent<PlayerToolHandler>();
        if (handler == null) return;

        var so = new SerializedObject(handler);

        // Default tool = Hammer
        var hammerData = AssetDatabase.LoadAssetAtPath<ToolData>("Assets/_Game/Data/ToolData/Hammer.asset");
        if (hammerData != null)
        {
            var prop = so.FindProperty("_defaultToolData");
            if (prop != null) prop.objectReferenceValue = hammerData;
        }

        // Wire all tool component references
        SetToolRef(so, "_hammerTool", player.GetComponent<HammerTool>());
        SetToolRef(so, "_screwdriverTool", player.GetComponent<ScrewdriverTool>());
        SetToolRef(so, "_tapeMeasureTool", player.GetComponent<TapeMeasureTool>());
        SetToolRef(so, "_nailGunTool", player.GetComponent<NailGunTool>());
        SetToolRef(so, "_blowtorchTool", player.GetComponent<BlowtorchTool>());
        SetToolRef(so, "_vacuumTool", player.GetComponent<VacuumTool>());
        SetToolRef(so, "_magnetTool", player.GetComponent<MagnetTool>());
        SetToolRef(so, "_chainsawTool", player.GetComponent<ChainsawTool>());

        var onToolEquipped = AssetDatabase.LoadAssetAtPath<ToolDataEventChannel>("Assets/_Game/Data/EventChannels/OnToolEquipped.asset");
        if (onToolEquipped != null)
        {
            var prop = so.FindProperty("_onToolEquipped");
            if (prop != null) prop.objectReferenceValue = onToolEquipped;
        }

        so.ApplyModifiedProperties();
    }

    static void SetToolRef(SerializedObject so, string propName, Object component)
    {
        if (component == null) return;
        var prop = so.FindProperty(propName);
        if (prop != null) prop.objectReferenceValue = component;
    }

    static void SetupEnemyPrefab(string name, string spritePath, string dataPath)
    {
        var obj = GameObject.Find(name);
        if (obj == null) { Debug.LogWarning($"{name} not found in scene!"); return; }

        // Set sprite
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null) sr.sprite = sprite;
        }

        // Wire enemy data
        var enemy = obj.GetComponent<BaseEnemy>();
        if (enemy != null)
        {
            var so = new SerializedObject(enemy);
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
            if (data != null)
            {
                var prop = so.FindProperty("_data");
                if (prop != null) prop.objectReferenceValue = data;
            }

            // OnEnemyKilled event
            var onKilled = AssetDatabase.LoadAssetAtPath<VoidEventChannel>("Assets/_Game/Data/EventChannels/OnEnemyKilled.asset");
            if (onKilled != null)
            {
                var prop = so.FindProperty("_onEnemyKilled");
                if (prop != null) prop.objectReferenceValue = onKilled;
            }

            so.ApplyModifiedProperties();
        }
    }

    static void SetupToolbox()
    {
        var obj = GameObject.Find("Toolbox");
        if (obj == null) { Debug.LogWarning("Toolbox not found in scene!"); return; }

        // Set sprite
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/Sprites/Placeholder_Toolbox.png");
            if (sprite != null) sr.sprite = sprite;
        }

        // Wire Toolbox
        var toolbox = obj.GetComponent<Toolbox>();
        if (toolbox != null)
        {
            var so = new SerializedObject(toolbox);
            var onPickup = AssetDatabase.LoadAssetAtPath<VoidEventChannel>("Assets/_Game/Data/EventChannels/OnToolPickedUp.asset");
            if (onPickup != null)
            {
                var prop = so.FindProperty("_onToolPickedUp");
                if (prop != null) prop.objectReferenceValue = onPickup;
            }
            so.ApplyModifiedProperties();
        }

        // Make collider trigger
        var col = obj.GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    static void SaveAllAsPrefabs()
    {
        string prefabFolder = "Assets/_Game/Prefabs";

        SaveAsPrefab("Player", prefabFolder);
        SaveAsPrefab("WalkerEnemy", prefabFolder);
        SaveAsPrefab("FlyerEnemy", prefabFolder);
        SaveAsPrefab("Toolbox", prefabFolder);
    }

    static void SaveAsPrefab(string name, string folder)
    {
        var obj = GameObject.Find(name);
        if (obj == null) return;

        string path = $"{folder}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(obj, path, InteractionMode.AutomatedAction);
        Debug.Log($"[PrefabSetupUtility] Saved {name} as prefab at {path}");
    }
}
#endif
