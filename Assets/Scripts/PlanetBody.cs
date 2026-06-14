using UnityEngine;

/// <summary>
/// Holds the world constants for the single planet and answers gravity queries.
/// Toy scale: radius in metres, surface gravity in m/s^2. Other scripts query
/// <see cref="GravityAt"/> so all the gravity math lives in one place.
/// </summary>
public class PlanetBody : MonoBehaviour
{
    /// <summary>Planet radius in metres (visual surface).</summary>
    [Tooltip("Planet radius in metres (visual surface).")]
    public float radius = 600f;

    /// <summary>Acceleration due to gravity at the surface, m/s^2.</summary>
    [Tooltip("Acceleration due to gravity at the surface, m/s^2.")]
    public float surfaceGravity = 9.81f;

    /// <summary>
    /// Gravitational parameter mu = g0 * R^2, chosen so that gravity at the
    /// surface equals <see cref="surfaceGravity"/>. With defaults this is 3,531,600.
    /// </summary>
    public float Mu => surfaceGravity * radius * radius;

    /// <summary>
    /// Inverse-square gravitational acceleration at a world position, pointing
    /// toward this body's centre. Returns zero exactly at the centre to guard
    /// against divide-by-zero.
    /// </summary>
    /// <param name="worldPos">World-space position to evaluate gravity at.</param>
    /// <returns>Acceleration vector in m/s^2 (magnitude mu / r^2).</returns>
    public Vector3 GravityAt(Vector3 worldPos)
    {
        Vector3 toCentre = transform.position - worldPos;
        float r2 = toCentre.sqrMagnitude;
        if (r2 < 1e-6f) return Vector3.zero;
        float r = Mathf.Sqrt(r2);
        return toCentre / r * (Mu / r2);
    }
}
