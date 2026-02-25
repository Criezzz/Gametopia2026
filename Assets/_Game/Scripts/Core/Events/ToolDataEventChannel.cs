using UnityEngine;

/// <summary>
/// Event channel carrying ToolData payloads for equip/unequip flows.
/// </summary>
[CreateAssetMenu(menuName = "ToolCrate/Events/Tool Data Event Channel")]
public class ToolDataEventChannel : EventChannel<ToolData> { }
