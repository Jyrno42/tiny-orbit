using UnityEngine;

/// <summary>
/// Smooth third-person follow camera framed RELATIVE TO THE PLANET: the planet
/// stays behind/below the rocket wherever it flies, "up" points away from the
/// planet centre (so the horizon stays level and never rolls), and zooming dollies
/// straight in/out along the view axis without rotating. Scroll = zoom, hold right
/// mouse = orbit. Runs in LateUpdate so it reacts to the rocket's post-physics pose.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Tooltip("Transform to follow (the Rocket). Auto-found by name if unset.")]
    public Transform target;
    [Tooltip("Planet to frame against. Auto-found if unset.")]
    public PlanetBody planet;

    [Header("Distance / zoom")]
    public float distance = 30f;
    public float minDistance = 8f;
    public float maxDistance = 600f;
    [Tooltip("Zoom responsiveness per scroll notch.")]
    public float zoomStrength = 3f;

    [Header("Smoothing")]
    [Tooltip("Position smoothing time for the follow (smaller = snappier).")]
    public float followSmoothTime = 0.15f;

    [Header("Orbit (hold right mouse)")]
    public float orbitSpeed = 3f;
    [Tooltip("Azimuth around the rocket (degrees) in the planet's frame.")]
    public float yaw = 0f;
    [Tooltip("Elevation above the local horizon (degrees). 0 = level side view, 90 = straight above.")]
    public float elevation = 18f;

    Vector3 velocity; // SmoothDamp state
    Vector3 frameRef; // parallel-transported azimuth reference (perpendicular to "up")

    void Start()
    {
        if (target == null) { GameObject rk = GameObject.Find("Rocket"); if (rk != null) target = rk.transform; }
        if (planet == null) planet = FindObjectOfType<PlanetBody>();
        frameRef = Vector3.forward; // seed the azimuth reference; CamDir keeps it perpendicular to up
        if (target != null) { transform.position = DesiredPosition(); ApplyLook(); } // snap, no startup glide
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Scroll zoom (proportional -> consistent feel at any altitude). Pure dolly: distance
        // only scales position along CamDir, so the view direction and roll are unaffected.
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            distance = Mathf.Clamp(distance * Mathf.Exp(-scroll * zoomStrength), minDistance, maxDistance);

        // Right-mouse orbit, in the planet-relative frame.
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * orbitSpeed;
            elevation = Mathf.Clamp(elevation - Input.GetAxis("Mouse Y") * orbitSpeed, -10f, 85f);
        }

        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref velocity, followSmoothTime);
        ApplyLook();
    }

    // "Up" in this game is away from the planet centre, so the horizon stays level.
    Vector3 RadialUp()
    {
        if (planet != null)
        {
            Vector3 r = target.position - planet.transform.position;
            if (r.sqrMagnitude > 1e-4f) return r.normalized;
        }
        return Vector3.up;
    }

    // Direction from the rocket to the camera, built in the planet-relative frame from yaw + elevation.
    Vector3 CamDir()
    {
        Vector3 up = RadialUp();
        // Parallel-transport the azimuth reference onto the new horizon plane instead of rebuilding
        // it from world-up each frame. That keeps the frame continuous across the poles (rebuilding
        // from cross(up, worldUp) flips there and snaps the view around).
        Vector3 baseTangent = Vector3.ProjectOnPlane(frameRef, up);
        if (baseTangent.sqrMagnitude < 1e-5f)
            baseTangent = Vector3.ProjectOnPlane(Mathf.Abs(up.y) < 0.9f ? Vector3.up : Vector3.forward, up);
        baseTangent.Normalize();
        frameRef = baseTangent; // carry forward for the next frame

        Vector3 tangent = Quaternion.AngleAxis(yaw, up) * baseTangent;   // around the rocket at horizon level
        Vector3 tiltAxis = Vector3.Cross(tangent, up).normalized;        // axis to lift the camera up
        return Quaternion.AngleAxis(elevation, tiltAxis) * tangent;      // raise above the local horizon
    }

    Vector3 DesiredPosition() => target.position + CamDir() * distance;

    void ApplyLook() => transform.rotation = Quaternion.LookRotation(target.position - transform.position, RadialUp());
}
