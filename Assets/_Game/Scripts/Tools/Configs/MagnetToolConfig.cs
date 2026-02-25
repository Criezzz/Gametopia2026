using UnityEngine;

/// <summary>
/// Config data for MagnetTool behavior.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Tools/Configs/Magnet")]
public class MagnetToolConfig : ToolBehaviorConfig
{
    public float beamWidth = 0.5f;
    public float beamHeight = 2f;
    public float beamLength = 50f;
    public float verticalOffset = 0.5f;
}
