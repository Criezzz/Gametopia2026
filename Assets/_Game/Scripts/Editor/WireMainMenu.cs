#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class WireMainMenu
{
    [MenuItem("Tools/Wire Main Menu")]
    public static void Wire()
    {
        var sl = GameObject.Find("SceneLoader");
        if (sl == null) return;
        var comp = sl.GetComponent<SceneLoader>();
        if (comp == null) return;

        var so = new SerializedObject(comp);
        var asset = AssetDatabase.LoadAssetAtPath<StringEventChannel>("Assets/_Game/Data/EventChannels/OnSceneTransition.asset");
        so.FindProperty("_onSceneTransition").objectReferenceValue = asset;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sl);
        Debug.Log("Wired SceneLoader in MainMenu");
    }
}
#endif