#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public static class TempFieldInspector
{
    [MenuItem("Tools/Inspect UI Fields")]
    public static void InspectUIFields()
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SERIALIZED FIELD INSPECTION REPORT ===");
            sb.AppendLine("Time: " + System.DateTime.Now.ToString());
            sb.AppendLine();

            InspectByName(sb, "GameOverPanel", "GameOverUI");
            InspectByName(sb, "PausePanel", "PauseUI");
            InspectByName(sb, "HUDPanel", "GameHUD");
            InspectByName(sb, "GameManager", "GameManager");

            string outputPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "..", "field_inspection_report.txt"));
            System.IO.File.WriteAllText(outputPath, sb.ToString());
            Debug.Log("[FieldInspector] Report saved to: " + outputPath);
            Debug.Log("[FieldInspector] DONE");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FieldInspector] Exception: " + ex.Message + "\n" + ex.StackTrace);
        }
    }

    private static void InspectByName(StringBuilder sb, string goName, string componentTypeName)
    {
        var obj = GameObject.Find(goName);
        if (obj == null)
        {
            // Try finding inactive objects
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var g in allObjects)
            {
                if (g.name == goName && g.scene.isLoaded)
                {
                    obj = g;
                    break;
                }
            }
        }

        if (obj == null)
        {
            sb.AppendLine("[ERROR] GameObject '" + goName + "' not found in scene.");
            sb.AppendLine();
            return;
        }

        var comp = obj.GetComponent(componentTypeName);
        if (comp == null)
        {
            sb.AppendLine("[ERROR] Component '" + componentTypeName + "' not found on " + obj.name + ".");
            sb.AppendLine();
            return;
        }

        var so = new SerializedObject(comp);
        var iterator = so.GetIterator();
        bool enterChildren = true;

        sb.AppendLine("===== [" + obj.name + "] " + componentTypeName + " (InstanceID:" + obj.GetInstanceID() + ") =====");

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            if (iterator.name == "m_Script") continue;
            if (iterator.name == "m_ObjectHideFlags") continue;
            if (iterator.name == "m_Enabled") continue;

            string fieldName = iterator.name;
            string status;

            switch (iterator.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    if (iterator.objectReferenceValue == null)
                    {
                        bool hasRef = iterator.objectReferenceInstanceIDValue != 0;
                        status = hasRef
                            ? "MISSING REF (broken link, instanceID=" + iterator.objectReferenceInstanceIDValue + ")"
                            : "NULL (NOT WIRED)";
                    }
                    else
                    {
                        status = "SET -> \"" + iterator.objectReferenceValue.name +
                            "\" (" + iterator.objectReferenceValue.GetType().Name + ")";
                    }
                    break;
                case SerializedPropertyType.Integer:
                    status = "int = " + iterator.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    status = "bool = " + iterator.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    status = "float = " + iterator.floatValue;
                    break;
                case SerializedPropertyType.String:
                    status = "string = \"" + iterator.stringValue + "\"";
                    break;
                case SerializedPropertyType.Enum:
                    status = "enum = " + iterator.enumDisplayNames[iterator.enumValueIndex] +
                        " (" + iterator.enumValueIndex + ")";
                    break;
                case SerializedPropertyType.ArraySize:
                    status = "array.size = " + iterator.intValue;
                    break;
                default:
                    status = "(" + iterator.propertyType + ")";
                    break;
            }

            sb.AppendLine("  " + fieldName + ": " + status);
        }
        sb.AppendLine();
    }
}
#endif
