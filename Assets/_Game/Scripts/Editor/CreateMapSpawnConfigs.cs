#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateMapSpawnConfigs
{
    [MenuItem("Tools/Create Map Spawn Configs")]
    public static void Create()
    {
        CreateMap1Config();
        CreateMap2Config();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateMapSpawnConfigs] Map spawn configs created and assigned to MapData assets.");
    }

    private static void CreateMap1Config()
    {
        const string path = "Assets/_Game/Data/MapData/Map1_SpawnConfig.asset";

        var config = AssetDatabase.LoadAssetAtPath<MapSpawnConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<MapSpawnConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.spawnXLeft = -4f;
        config.spawnXRight = 4f;
        config.spawnYOffset = 1f;
        config.toolboxZones = new MapSpawnConfig.ToolboxSpawnZone[]
        {
            new( 6.823f,  8.495f,  0.124f),
            new(-8.495f, -6.823f,  0.124f),
            new(-8.507f, -2.023f, -3.496f),
            new( 2.023f,  8.507f, -3.496f),
            new(-2f,      2f,      0.124f),
            new( 3.181f,  5.502f,  3.363f),
            new(-5.502f, -3.181f,  3.363f),
        };
        EditorUtility.SetDirty(config);

        AssignToMapData("Assets/_Game/Data/MapData/Map1_Data.asset", config);
    }

    private static void CreateMap2Config()
    {
        const string path = "Assets/_Game/Data/MapData/Map2_SpawnConfig.asset";

        var config = AssetDatabase.LoadAssetAtPath<MapSpawnConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<MapSpawnConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.spawnXLeft = -6f;
        config.spawnXRight = 6f;
        config.spawnYOffset = 1f;
        config.toolboxZones = new MapSpawnConfig.ToolboxSpawnZone[]
        {
            // Placeholder zones for Map 2 — adjust in Inspector to match Map 2 platforms
            new(-6f, -3f,  0f),
            new( 3f,  6f,  0f),
            new(-4f,  4f, -3f),
        };
        EditorUtility.SetDirty(config);

        AssignToMapData("Assets/_Game/Data/MapData/Map2_Data.asset", config);
    }

    private static void AssignToMapData(string mapDataPath, MapSpawnConfig config)
    {
        var mapData = AssetDatabase.LoadAssetAtPath<MapData>(mapDataPath);
        if (mapData == null)
        {
            Debug.LogWarning($"[CreateMapSpawnConfigs] MapData not found at {mapDataPath}");
            return;
        }

        var so = new SerializedObject(mapData);
        var prop = so.FindProperty("spawnConfig");
        if (prop != null)
        {
            prop.objectReferenceValue = config;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(mapData);
        }
    }
}
#endif
