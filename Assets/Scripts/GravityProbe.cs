using UnityEngine;

/// <summary>
/// Debug logger for gravity tests: once per second logs distance to the planet
/// centre, altitude, speed, and how velocity splits into radial (toward centre)
/// and tangential parts. Optionally applies an initial velocity kick so a
/// sideways nudge test needs no manual input.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GravityProbe : MonoBehaviour
{
    [Tooltip("Velocity applied on the first physics step (for curve tests).")]
    [SerializeField] private Vector3 initialVelocity = Vector3.zero;

    [SerializeField] private float logInterval = 1f;

    private Rigidbody rb;
    private PlanetBody planet;
    private float nextLog;
    private float startTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        planet = Object.FindFirstObjectByType<PlanetBody>();
        if (initialVelocity != Vector3.zero)
            rb.linearVelocity = initialVelocity;
        startTime = Time.time;
    }

    void FixedUpdate()
    {
        if (planet == null || Time.time < nextLog)
            return;
        nextLog = Time.time + logInterval;

        Vector3 toCentre = planet.transform.position - rb.position;
        float r = toCentre.magnitude;
        Vector3 down = toCentre / Mathf.Max(r, 1e-6f);
        Vector3 v = rb.linearVelocity;
        float radial = Vector3.Dot(v, down);
        Vector3 tangential = v - down * radial;
        Debug.Log($"[GravityProbe] t={Time.time - startTime:F1}s r={r:F1} alt={r - planet.Radius:F1} speed={v.magnitude:F2} radialIn={radial:F2} tangential={tangential.magnitude:F2}");
    }
}
