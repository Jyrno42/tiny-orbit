using UnityEngine;

/// <summary>
/// Applies the planet's inverse-square gravity to this Rigidbody every physics
/// step. Unity's built-in gravity is a constant flat vector and would never
/// curve an orbit, so each body that should be pulled by the planet uses this
/// instead and sets rb.useGravity = false.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityReceiver : MonoBehaviour
{
    [Tooltip("The planet whose gravity acts on this body. Auto-found if left empty.")]
    public PlanetBody planet;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // our manual field replaces Unity's flat gravity
        if (planet == null) planet = FindObjectOfType<PlanetBody>();
    }

    void FixedUpdate()
    {
        if (planet == null) return;
        Vector3 accel = planet.GravityAt(rb.position);
        rb.AddForce(accel, ForceMode.Acceleration);
    }
}
