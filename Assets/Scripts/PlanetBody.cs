using UnityEngine;

/// <summary>
/// Holds the world constants for the single planet and answers gravity queries.
/// Toy scale: real planet radii cause floating-point jitter, so we use metres in
/// the hundreds. Surface gravity is matched by deriving the gravitational
/// parameter mu = g0 * R^2, giving an inverse-square field a = mu / r^2.
/// </summary>
public class PlanetBody : MonoBehaviour
{
    [Tooltip("Planet radius in metres (world units).")]
    public float radius = 600f;

    [Tooltip("Gravitational acceleration at the surface, m/s^2.")]
    public float surfaceGravity = 9.81f;

    /// <summary>
    /// Standard gravitational parameter mu = g0 * R^2. With the defaults this is
    /// 9.81 * 600^2 = 3,531,600. Surface gravity equals mu / R^2 by construction.
    /// </summary>
    public float Mu => surfaceGravity * radius * radius;

    /// <summary>
    /// Inverse-square gravitational acceleration at a world position, pointing
    /// toward this body's centre. Returns zero at the exact centre to avoid a
    /// divide-by-zero singularity.
    /// </summary>
    public Vector3 GravityAt(Vector3 worldPos)
    {
        Vector3 toCentre = transform.position - worldPos;
        float r2 = toCentre.sqrMagnitude;
        if (r2 < 1e-6f) return Vector3.zero;
        return toCentre.normalized * (Mu / r2);
    }
}
