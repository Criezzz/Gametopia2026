using UnityEngine;

/// <summary>
/// Toolbox pickup. When player touches it, raises OnToolPickedUp event
/// (GameManager handles score increment + new tool assignment).
/// Then respawns at a random position on a platform.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Toolbox : MonoBehaviour
{
    [Header("Spawn Area")]
    [SerializeField] private float _spawnXMin = -9f;
    [SerializeField] private float _spawnXMax = 9f;
    [SerializeField] private float _spawnYMin = -3f;
    [SerializeField] private float _spawnYMax = 3f;

    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _onToolPickedUp;

    private BoxCollider2D _collider;

    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _collider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only player can pick up
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
    /// Move toolbox to a new random position.
    /// </summary>
    public void Respawn()
    {
        float x = Random.Range(_spawnXMin, _spawnXMax);
        float y = Random.Range(_spawnYMin, _spawnYMax);
        transform.position = new Vector2(x, y);

        Debug.Log($"[Toolbox] Respawned at ({x:F1}, {y:F1})");
    }
}