#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class FixEventChannels
{
    [MenuItem("Tools/Fix Event Channels")]
    public static void Fix()
    {
        string[] voidEvents = { "OnEnemyKilled", "OnGameOver", "OnGamePaused", "OnGameRestart", "OnPlayerDied", "OnToolPickedUp" };
        string[] intEvents = { "OnMilestoneReached", "OnScoreChanged" };
        string[] floatEvents = { "OnPlayerHPChanged" };
        string[] stringEvents = { "OnSceneTransition" };

        foreach (var name in voidEvents) Recreate<VoidEventChannel>(name);
        foreach (var name in intEvents) Recreate<IntEventChannel>(name);
        foreach (var name in floatEvents) Recreate<FloatEventChannel>(name);
        foreach (var name in stringEvents) Recreate<StringEventChannel>(name);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FixEventChannels] All Event Channels have been regenerated to fix missing scripts.");
    }

    private static void Recreate<T>(string name) where T : ScriptableObject
    {
        string path = $"Assets/_Game/Data/EventChannels/{name}.asset";
        
        T newAsset = ScriptableObject.CreateInstance<T>();
        
        // This will overwrite the asset but keep the .meta file, preserving scene references
        AssetDatabase.CreateAsset(newAsset, path);
    }
}
#endif