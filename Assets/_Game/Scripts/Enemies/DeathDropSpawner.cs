using UnityEngine;

/// <summary>
/// Listens for DeathDropChannel events and spawns a falling sprite copy of the dead enemy.
/// Attach to a persistent manager GameObject in the scene.
/// 
/// Setup:
/// 1. Create a "DeathDrop" Sorting Layer in Project Settings (above Default).
/// 2. Create a DeathDropChannel SO asset and assign it.
/// 3. Assign the same channel on BaseEnemy prefabs.
/// </summary>
public class DeathDropSpawner : MonoBehaviour
{
    [SerializeField] private DeathDropChannel _deathDropChannel;
    [SerializeField] private string _sortingLayerName = "DeathDrop";

    [Header("Pop-up Arc")]
    [Tooltip("Upward launch speed (units/s). Higher = enemy sprite pops higher before falling.")]
    [SerializeField] private float _popUpSpeed = 6f;
    [Tooltip("Random horizontal drift range. Adds variety so sprites don't all fall straight.")]
    [SerializeField] private float _horizontalDriftRange = 1.5f;
    [Tooltip("Gravity pulling the sprite back down (units/s²). Higher = falls faster.")]
    [SerializeField] private float _gravity = 27f;
    [Tooltip("Spin speed in degrees/second. 0 = no spin.")]
    [SerializeField] private float _rotateSpeed = 180f;

    [Header("Cleanup")]
    [SerializeField] private float _destroyY = -15f;

    private void OnEnable()
    {
        _deathDropChannel?.Register(OnDeathDrop);
    }

    private void OnDisable()
    {
        _deathDropChannel?.Unregister(OnDeathDrop);
    }

    private void OnDeathDrop(DeathDropData data)
    {
        if (data.sprite == null) return;

        var go = new GameObject("DeathDrop");
        go.transform.position = data.position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = data.sprite;
        sr.sortingLayerName = _sortingLayerName;

        // Random horizontal drift for variety
        float driftX = Random.Range(-_horizontalDriftRange, _horizontalDriftRange);
        Vector2 initialVelocity = new Vector2(driftX, _popUpSpeed);

        var drop = go.AddComponent<DeathDropFall>();
        drop.Initialize(initialVelocity, _gravity, _destroyY, _rotateSpeed, data.onComplete);
    }
}
