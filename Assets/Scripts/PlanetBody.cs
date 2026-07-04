using UnityEngine;

/// <summary>
/// Holds the planet's physical constants and answers gravity queries.
/// Toy scale: radius 600 m with surface gravity 9.81 m/s^2 gives mu = 3,531,600.
/// </summary>
public class PlanetBody : MonoBehaviour
{
    [Tooltip("Planet radius in metres (visual sphere scale must match).")]
    [SerializeField] private float radius = 600f;

    [Tooltip("Gravitational acceleration at the surface, m/s^2.")]
    [SerializeField] private float surfaceGravity = 9.81f;

    /// <summary>Planet radius in metres.</summary>
    public float Radius => radius;

    /// <summary>Surface gravity in m/s^2.</summary>
    public float SurfaceGravity => surfaceGravity;

    /// <summary>Gravitational parameter mu = g0 * R^2 (m^3/s^2).</summary>
    public float Mu => surfaceGravity * radius * radius;

    /// <summary>
    /// Gravitational acceleration at a world position: a = mu / r^2, pointing
    /// toward the planet centre. Returns zero at the exact centre.
    /// </summary>
    public Vector3 GravityAt(Vector3 worldPos)
    {
        Vector3 toCentre = transform.position - worldPos;
        float sqrDist = toCentre.sqrMagnitude;
        if (sqrDist < 1e-6f)
            return Vector3.zero;
        return toCentre / Mathf.Sqrt(sqrDist) * (Mu / sqrDist);
    }
}
