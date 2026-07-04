using UnityEngine;

/// <summary>
/// Planet-relative follow camera. Keeps the target framed with the camera "up"
/// derived from the planet-to-target radial direction, so the horizon never
/// rolls and the planet stays at the bottom of frame anywhere in the orbit.
/// The horizontal view heading is parallel-transported frame to frame instead
/// of being rebuilt from world up, so nothing degenerates or flips at the
/// poles (the launch site sits on one).
/// Manual input, active in both manual flight and autopilot: scroll wheel
/// zooms as a pure dolly along the view axis; holding the right mouse button
/// and dragging orbits the camera around the target (yaw spins the heading
/// around the radial axis, pitch adjusts the clamped view elevation).
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Tooltip("What to follow (the Rocket root).")]
    [SerializeField] private Transform target;

    [Tooltip("Camera distance from the target, metres.")]
    [SerializeField] private float distance = 25f;

    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 400f;

    [Tooltip("View elevation above the target's local horizon, degrees.")]
    [SerializeField] private float elevationDeg = 18f;

    [Tooltip("Orbit degrees per mouse-axis unit while right-dragging.")]
    [SerializeField] private float orbitSpeed = 3f;

    [SerializeField] private float minElevationDeg = -70f;
    [SerializeField] private float maxElevationDeg = 85f;

    [Tooltip("SmoothDamp time for position following.")]
    [SerializeField] private float smoothTime = 0.1f;

    /// <summary>Current follow distance (scroll wheel changes it in play).</summary>
    public float Distance
    {
        get => distance;
        set => distance = Mathf.Clamp(value, minDistance, maxDistance);
    }

    /// <summary>Follow target, assignable at runtime (e.g. after staging).</summary>
    public Transform Target
    {
        get => target;
        set => target = value;
    }

    private PlanetBody planet;
    private Rigidbody targetRb;
    private Vector3 smoothVelocity;
    private Vector3 tangentHeading = Vector3.forward;

    void Awake()
    {
        planet = Object.FindFirstObjectByType<PlanetBody>();
        if (target == null)
        {
            var rocket = GameObject.Find("Rocket");
            if (rocket != null) target = rocket.transform;
        }
        if (target != null)
            targetRb = target.GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (target == null) return;
        InitHeadingFromCurrentPose();
        transform.position = DesiredPosition();
        AimAtTarget();
    }

    void InitHeadingFromCurrentPose()
    {
        Vector3 radialUp = RadialUp();
        Vector3 fromTarget = transform.position - target.position;
        Vector3 tangential = fromTarget - radialUp * Vector3.Dot(fromTarget, radialUp);
        tangentHeading = tangential.sqrMagnitude > 1e-4f
            ? tangential.normalized
            : Vector3.Normalize(Vector3.Cross(radialUp, Vector3.right));
    }

    Vector3 RadialUp()
    {
        Vector3 center = planet != null ? planet.transform.position : Vector3.zero;
        Vector3 up = target.position - center;
        return up.sqrMagnitude > 1e-6f ? up.normalized : Vector3.up;
    }

    Vector3 DesiredPosition()
    {
        Vector3 radialUp = RadialUp();
        // parallel transport: keep only the component tangential to the new up
        Vector3 tangential = tangentHeading - radialUp * Vector3.Dot(tangentHeading, radialUp);
        if (tangential.sqrMagnitude > 1e-6f)
            tangentHeading = tangential.normalized;
        float e = elevationDeg * Mathf.Deg2Rad;
        // lead the anchor by one smoothing window so SmoothDamp has no
        // steady-state lag behind a fast-moving rocket
        Vector3 anchor = target.position;
        if (targetRb != null)
            anchor += targetRb.linearVelocity * smoothTime;
        return anchor
            + tangentHeading * (Mathf.Cos(e) * distance)
            + radialUp * (Mathf.Sin(e) * distance);
    }

    void AimAtTarget()
    {
        transform.rotation = Quaternion.LookRotation(target.position - transform.position, RadialUp());
    }

    /// <summary>
    /// Orbits the view around the target: yaw spins the transported heading
    /// around the radial axis, pitch adjusts the clamped elevation. Also the
    /// right-mouse-drag code path, public so tests can drive it directly.
    /// </summary>
    public void OrbitBy(float yawDeg, float pitchDeg)
    {
        if (target == null) return;
        tangentHeading = Quaternion.AngleAxis(yawDeg, RadialUp()) * tangentHeading;
        elevationDeg = Mathf.Clamp(elevationDeg + pitchDeg, minElevationDeg, maxElevationDeg);
    }

    void LateUpdate()
    {
        if (target == null) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
            Distance = distance * (1f - scroll * 0.1f);

        if (Input.GetMouseButton(1))
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            if (Mathf.Abs(mx) > 0.001f || Mathf.Abs(my) > 0.001f)
                OrbitBy(mx * orbitSpeed, my * orbitSpeed);
        }

        transform.position = Vector3.SmoothDamp(transform.position, DesiredPosition(), ref smoothVelocity, smoothTime);
        AimAtTarget();
    }
}
