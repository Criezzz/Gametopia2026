using UnityEngine;

/// Static utility for spawning one-shot particle VFX.
public static class VFXSpawner
{
    /// Instantiate a VFX prefab at position. Auto-destroys non-looping ParticleSystems.
    public static void Spawn(GameObject prefab, Vector3 position)
    {
        Spawn(prefab, position, Quaternion.identity);
    }

    /// Instantiate a VFX prefab at position with explicit rotation.
    public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return;

        var vfx = Object.Instantiate(prefab, position, rotation);
        var ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null && !ps.main.loop)
        {
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Object.Destroy(vfx, lifetime);
        }
    }
}
