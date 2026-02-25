using UnityEngine;

/// <summary>
/// Config data for VacuumTool behavior.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Tools/Configs/Vacuum")]
public class VacuumToolConfig : ToolBehaviorConfig
{
    public float suckDuration = 1f;
    public float suckRange = 3f;
    public float shootSpeed = 10f;
    public int shootDamage = 10;
    public float shootInterval = 0.15f;
}
