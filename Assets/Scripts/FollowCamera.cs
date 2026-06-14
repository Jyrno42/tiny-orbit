using UnityEngine;

/// <summary>
/// Smooth follow camera that frames the rocket planet-relative: the camera "up"
/// is the planet->rocket radial direction, so the horizon stays level and the
/// planet stays framed below the rocket. Zoom is a pure dolly along the view
/// axis. The azimuth reference is parallel-transported each frame so the view
/// does not flip 180 degrees when the rocket passes over a pole (the launch
/// site IS a pole, and an orbit recrosses it every lap).
/// </summary>
[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Distance / zoom")]
    public float distance = 25f;
    public float minDistance = 8f;
    public float maxDistance = 400f;
    [Tooltip("Scroll-wheel zoom strength (multiplicative, so it feels even across the range).")]
    public float zoomSpeed = 4f;

    [Header("Framing")]
    [Tooltip("How high above the local horizon the camera sits, in degrees.")]
    public float elevationDeg = 18f;
    [Tooltip("Position smoothing time. Smaller = snappier.")]
    public float followSmoothTime = 0.1f;

    [Header("Optional orbit (hold right mouse)")]
    public float orbitSensitivity = 4f;

    Transform planet;
    Vector3 vel;                       // SmoothDamp state
    Vector3 forwardRef = Vector3.forward; // parallel-transported tangent reference
    float azimuth;                     // deg, right-mouse yaw around the radial
    float elevation;                   // deg, current pitch above horizon
    bool initialized;

    void Start()
    {
        var pb = FindObjectOfType<PlanetBody>();
        if (pb) planet = pb.transform;
        elevation = elevationDeg;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 planetPos = planet ? planet.position : Vector3.zero;
        Vector3 radialUp = (target.position - planetPos).normalized;
        if (radialUp.sqrMagnitude < 1e-6f) radialUp = Vector3.up;

        // Zoom: pure dolly, multiplicative for even feel from pad to high orbit.
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (!Mathf.Approximately(scroll, 0f))
            distance = Mathf.Clamp(distance * Mathf.Exp(-scroll * zoomSpeed), minDistance, maxDistance);

        // Optional orbit while holding right mouse.
        if (Input.GetMouseButton(1))
        {
            azimuth += Input.GetAxis("Mouse X") * orbitSensitivity;
            elevation = Mathf.Clamp(elevation - Input.GetAxis("Mouse Y") * orbitSensitivity, -10f, 80f);
        }

        // Parallel-transport the tangent reference onto the plane perpendicular to
        // the current radial. Re-projecting the previous frame's vector keeps the
        // azimuth continuous across the pole instead of cross()-flipping.
        forwardRef = Vector3.ProjectOnPlane(forwardRef, radialUp);
        if (forwardRef.sqrMagnitude < 1e-6f)
        {
            forwardRef = Vector3.ProjectOnPlane(Vector3.forward, radialUp);
            if (forwardRef.sqrMagnitude < 1e-6f) forwardRef = Vector3.ProjectOnPlane(Vector3.right, radialUp);
        }
        forwardRef.Normalize();

        // Horizontal look direction (tangent), rotated by the orbit azimuth.
        Vector3 h = Quaternion.AngleAxis(azimuth, radialUp) * forwardRef;

        // Camera sits behind (-h) and above (radialUp) the rocket by the elevation.
        float e = elevation * Mathf.Deg2Rad;
        Vector3 offset = (-h * Mathf.Cos(e) + radialUp * Mathf.Sin(e)) * distance;
        Vector3 desiredPos = target.position + offset;

        if (!initialized) { transform.position = desiredPos; initialized = true; }
        else transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref vel, followSmoothTime);

        transform.LookAt(target.position, radialUp);
    }
}
