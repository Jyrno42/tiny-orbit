using UnityEngine;

/// <summary>
/// Applies the planet's inverse-square gravity to this Rigidbody each physics
/// step. Disables Unity's flat global gravity so the two never fight.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityReceiver : MonoBehaviour
{
    [Tooltip("Planet providing gravity. Found automatically if left empty.")]
    [SerializeField] private PlanetBody planet;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        if (planet == null)
            planet = Object.FindFirstObjectByType<PlanetBody>();
        if (planet == null)
            Debug.LogError("GravityReceiver: no PlanetBody found in scene.", this);
    }

    void FixedUpdate()
    {
        if (planet == null)
            return;
        rb.AddForce(planet.GravityAt(rb.position), ForceMode.Acceleration);
    }
}
