using UnityEngine;

/// Toolbox pickup. Player touches it, raises OnToolPickedUp, and respawns at random zone.
[RequireComponent(typeof(BoxCollider2D))]
public class Toolbox : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private IntEventChannel _onToolPickedUp;

    [Header("VFX")]
    [Tooltip("Particle prefab spawned when the toolbox appears at a new position")]
    [SerializeField] private GameObject _spawnVFXPrefab;

    [Header("Spawn Zones")]
    [Tooltip("Platform zones for respawning. X picked randomly within range, Y is fixed.")]
    [SerializeField] private SpawnZone[] _spawnZones = new SpawnZone[]
    {
        new( 6.823f,  8.495f,  0.124f),
        new(-8.495f, -6.823f,  0.124f),
        new(-8.507f, -2.023f, -3.496f),
        new( 2.023f,  8.507f, -3.496f),
        new(-2f,      2f,      0.124f),
        new( 3.181f,  5.502f,  3.363f),
        new(-5.502f, -3.181f,  3.363f),
    };

    private BoxCollider2D _collider;
    private int _lastZoneIndex = -1;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
    }

    private void Start()
    {
        Respawn();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
            PickUp(player);
    }

    private void PickUp(PlayerController player)
    {
        if (SFXManager.Instance != null && SFXManager.Instance.PickupSFX != null)
            SFXManager.Instance.Play(SFXManager.Instance.PickupSFX);

        // Score tracking (GameManager handles per-player scoring)
        _onToolPickedUp?.Raise(player.PlayerIndex);

        // Equip this specific player with a tool
        if (GameManager.Instance != null)
        {
            ToolData tool = GameManager.Instance.GetToolForPlayer(player.PlayerIndex);
            if (tool != null)
            {
                var toolHandler = player.GetComponent<PlayerToolHandler>();
                if (toolHandler != null)
                    toolHandler.EquipTool(tool);
            }
        }

        Respawn();
    }

    public void Respawn()
    {
        if (_spawnZones == null || _spawnZones.Length == 0) return;

        int zoneIndex = PickRandomZoneExcluding(_lastZoneIndex);
        _lastZoneIndex = zoneIndex;

        var zone = _spawnZones[zoneIndex];
        float x = Random.Range(zone.xMin, zone.xMax);
        transform.position = new Vector2(x, zone.y);

        VFXSpawner.Spawn(_spawnVFXPrefab, transform.position);
    }

    private int PickRandomZoneExcluding(int excludeIndex)
    {
        int count = _spawnZones.Length;
        if (count <= 1) return 0;

        int pick;
        do
        {
            pick = Random.Range(0, count);
        } while (pick == excludeIndex);

        return pick;
    }

    [System.Serializable]
    public struct SpawnZone
    {
        public float xMin;
        public float xMax;
        public float y;

        public SpawnZone(float xMin, float xMax, float y)
        {
            this.xMin = xMin;
            this.xMax = xMax;
            this.y = y;
        }
    }
}