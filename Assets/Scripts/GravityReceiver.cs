using UnityEngine;

/// <summary>
/// Applies the planet's inverse-square gravity to this object's Rigidbody every
/// FixedUpdate. Unity's flat global gravity is disabled (see Awake) so it cannot
/// fight the radial pull that makes closed orbits possible.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityReceiver : MonoBehaviour
{
    /// <summary>The planet to fall toward. Auto-found in Awake if left unset.</summary>
    [Tooltip("The planet to fall toward. Auto-found in Awake if left unset.")]
    public PlanetBody planet;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // our radial gravity replaces Unity's flat vector
        if (planet == null) planet = FindObjectOfType<PlanetBody>();
    }

    void FixedUpdate()
    {
        if (planet == null) return;
        Vector3 accel = planet.GravityAt(rb.position);
        rb.AddForce(accel, ForceMode.Acceleration);
    }
}
