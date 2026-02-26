using UnityEngine;

/// <summary>
/// Toolbox pickup. When player touches it, raises OnToolPickedUp event
/// (GameManager handles score increment + new tool assignment).
/// Then respawns at a random position on a platform.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Toolbox : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _onToolPickedUp;

    private BoxCollider2D _collider;
    private int _lastZoneIndex = -1;

    /// <summary>
    /// Platform spawn zones: each defined by (xMin, xMax, y).
    /// X is picked randomly within the range; Y is fixed per zone.
    /// </summary>
    private static readonly (float xMin, float xMax, float y)[] SpawnZones =
    {
        (  6.823f,  8.495f,  0.124f ),   // right mid-platform
        ( -8.495f, -6.823f,  0.124f ),   // left mid-platform
        ( -8.507f, -1.672f, -3.317f ),   // bottom left
        (  1.672f,  8.507f, -3.317f ),   // bottom right
        ( -2.739f,  2.739f, -0.065f ),   // center platform
        (  3.181f,  5.502f,  3.363f ),   // top right
        ( -5.502f, -3.181f,  3.363f ),   // top left
    };

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
        if (other.GetComponent<PlayerController>() != null)
        {
            PickUp();
        }
    }

    private void PickUp()
    {
        _onToolPickedUp?.Raise();
        Respawn();
    }

    /// <summary>
    /// Move toolbox to a random platform zone (never the same zone twice in a row).
    /// </summary>
    public void Respawn()
    {
        int zoneIndex = PickRandomZoneExcluding(_lastZoneIndex);
        _lastZoneIndex = zoneIndex;

        var zone = SpawnZones[zoneIndex];
        float x = Random.Range(zone.xMin, zone.xMax);
        transform.position = new Vector2(x, zone.y);
    }

    private int PickRandomZoneExcluding(int excludeIndex)
    {
        int count = SpawnZones.Length;
        if (count <= 1) return 0;

        int pick;
        do
        {
            pick = Random.Range(0, count);
        } while (pick == excludeIndex);

        return pick;
    }
}