using UnityEngine;

/// <summary>
/// Moves a death-drop sprite in a pop-up arc then falls with gravity.
/// Gives a satisfying "launch up then drop" feel.
/// </summary>
public class DeathDropFall : MonoBehaviour
{
    private Vector2 _velocity;
    private float _gravity;
    private float _destroyY;
    private float _rotateSpeed;

    /// <summary>
    /// Set up the drop physics.
    /// </summary>
    /// <param name="initialVelocity">Initial velocity (positive Y = pop up, X = sideways drift)</param>
    /// <param name="gravity">Downward acceleration (positive value, e.g. 25)</param>
    /// <param name="destroyY">Y position below which the object is destroyed</param>
    /// <param name="rotateSpeed">Spin speed in degrees/sec (0 = no spin)</param>
    public void Initialize(Vector2 initialVelocity, float gravity, float destroyY, float rotateSpeed = 0f)
    {
        _velocity = initialVelocity;
        _gravity = gravity;
        _destroyY = destroyY;
        _rotateSpeed = rotateSpeed;
    }

    /// <summary>
    /// Legacy overload — straight fall (backward compat).
    /// </summary>
    public void Initialize(float fallSpeed, float destroyY)
    {
        _velocity = new Vector2(0f, 0f);
        _gravity = fallSpeed * 2f;
        _destroyY = destroyY;
    }

    private void Update()
    {
        // Apply gravity
        _velocity.y -= _gravity * Time.deltaTime;

        // Move
        transform.position += (Vector3)_velocity * Time.deltaTime;

        // Spin
        if (_rotateSpeed != 0f)
            transform.Rotate(0f, 0f, _rotateSpeed * Time.deltaTime);

        if (transform.position.y < _destroyY)
            Destroy(gameObject);
    }
}
