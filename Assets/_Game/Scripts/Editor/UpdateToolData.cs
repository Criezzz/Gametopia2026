#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class UpdateToolData
{
    [MenuItem("Tools/Update Tool Unlock Reqs")]
    public static void Update()
    {
        UpdateTool("Hammer", 0, 100f);
        UpdateTool("Screwdriver", 1, 100f);
        UpdateTool("TapeMeasure", 5, 100f);
        UpdateTool("NailGun", 10, 100f);
        UpdateTool("Blowtorch", 25, 95f);
        UpdateTool("Vacuum", 36, 95f);
        UpdateTool("Magnet", 50, 95f);
        UpdateTool("Chainsaw", 67, 90f);
        AssetDatabase.SaveAssets();
        Debug.Log("[UpdateToolData] Tool thresholds (unlockScore) and weights (baseWeight) updated!");
    }

    private static void UpdateTool(string name, int score, float weight)
    {
        var dt = AssetDatabase.LoadAssetAtPath<ToolData>($"Assets/_Game/Data/ToolData/{name}.asset");
        if(dt != null)
        {
            var so = new SerializedObject(dt);
            var scoreProp = so.FindProperty("unlockScore");
            if (scoreProp != null) scoreProp.intValue = score;
            
            var weightProp = so.FindProperty("baseWeight");
            if (weightProp != null) weightProp.floatValue = weight;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(dt);
        }
    }
}
#endif
