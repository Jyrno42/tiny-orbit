using UnityEngine;

/// <summary>
/// Scripted launch-to-orbit autopilot, mainly a test/demo runner so a full ascent
/// can be reproduced deterministically (the manual keys still work when it is off).
/// Toggle with T. When engaged it drives throttle, points the nose (by steering the
/// Rigidbody's rotation directly for repeatability), auto-stages spent boosters,
/// and runs a simple gravity turn then circularises near apoapsis.
/// </summary>
[RequireComponent(typeof(RocketController))]
public class AutoPilot : MonoBehaviour
{
    public enum Phase { Off, VerticalClimb, GravityTurn, Coast, Circularize, Orbit, Deorbit, Reentry, Chute, Landed }

    [Header("Targets")]
    [Tooltip("Target periapsis/apoapsis altitude to aim for (metres above surface).")]
    public float targetAltitude = 120f;

    [Header("Mission")]
    [Tooltip("After reaching orbit, deorbit and land under parachute (full demo flight).")]
    public bool landAfterOrbit = true;
    [Tooltip("Number of full orbits to complete before the deorbit burn.")]
    public int lapsBeforeDeorbit = 1;
    [Tooltip("Altitude to deploy the parachute on the way down.")]
    public float chuteAltitude = 250f;

    [Header("Auto time-warp")]
    [Tooltip("Speed up boring coasts and slow back to real time for launch/burns/landing.")]
    public bool autoWarp = true;
    [Tooltip("Maximum warp factor during the orbital coast (lower = smoother camera).")]
    public float maxWarp = 4f;

    [Header("Gravity turn")]
    [Tooltip("Altitude to begin pitching over.")]
    public float turnStartAlt = 15f;
    [Tooltip("Altitude by which the turn reaches its maximum pitch-over.")]
    public float turnEndAlt = 110f;
    [Range(0f, 1f)]
    [Tooltip("Cap on how far the powered ascent pitches toward horizontal (1 = fully horizontal). " +
             "Below 1 keeps the nose above the horizon so it always climbs; circularization finishes horizontal.")]
    public float maxTurn = 0.6f;

    [Header("Attitude")]
    [Tooltip("How fast the autopilot can slew the nose, degrees/second.")]
    public float slewRate = 120f;

    public Phase phase = Phase.Off;
    public bool engaged;

    RocketController rc;
    Rigidbody rb;
    PlanetBody planet;
    Vector3 turnTangent; // fixed downrange direction chosen at turn start
    float sweptAngle;    // degrees swept around the planet since reaching orbit (for lap counting)
    Vector3 prevRadial;  // previous radial direction
    bool warpManaged;    // true while this autopilot owns Time.timeScale

    [System.NonSerialized] public string trace = ""; // mission trajectory log for debugging
    float logT;

    void Awake()
    {
        rc = GetComponent<RocketController>();
        rb = GetComponent<Rigidbody>();
        planet = FindObjectOfType<PlanetBody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) { if (engaged) Disengage(); else Engage(); }
        HandleWarp();
    }

    /// <summary>Engage from script (used by tests/demos).</summary>
    public void Engage() { engaged = true; phase = Phase.VerticalClimb; trace = ""; logT = 0f; }
    public void Disengage() { engaged = false; phase = Phase.Off; }

    // Auto time-warp: ramp Time.timeScale toward a per-phase target, and hand control back at 1x.
    void HandleWarp()
    {
        if (engaged && autoWarp)
        {
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, TargetWarp(), 10f * Time.unscaledDeltaTime);
            warpManaged = true;
        }
        else if (warpManaged)
        {
            Time.timeScale = 1f;
            warpManaged = false;
        }
    }

    float TargetWarp()
    {
        if (planet == null) return 1f;
        Vector3 rvec = rb.position - planet.transform.position;
        float alt = rvec.magnitude - planet.radius;
        float radialVel = Vector3.Dot(rb.linearVelocity, rvec.normalized);
        float lapTarget = 360f * Mathf.Max(1, lapsBeforeDeorbit);
        switch (phase)
        {
            // Warp only the pure coasts; drop back to 1x BEFORE each burn so it is clearly visible.
            case Phase.Coast:   return radialVel > 4f ? 3f : 1f;             // slow as we near apoapsis (circularize next)
            case Phase.Orbit:   return sweptAngle > lapTarget - 25f ? 1f : maxWarp; // slow just before the deorbit burn
            case Phase.Reentry: return alt > chuteAltitude + 40f ? maxWarp : 1f;    // warp the fall, slow before the chute
            case Phase.Chute:   return alt > chuteAltitude - 40f ? 1f : (alt > 60f ? 2f : 1f); // deploy + touchdown at 1x
            default:            return 1f;  // launch, gravity turn, Circularize burn, Deorbit burn, landed
        }
    }

    void OnDisable() { if (warpManaged) { Time.timeScale = 1f; warpManaged = false; } }

    void FixedUpdate()
    {
        if (!engaged || planet == null) return;

        Vector3 r = rb.position - planet.transform.position;
        Vector3 radialOut = r.normalized;
        float alt = r.magnitude - planet.radius;
        var o = OrbitMath.Compute(r, rb.linearVelocity, planet.Mu, planet.radius);
        float apAlt = o.bound ? o.ra - planet.radius : float.PositiveInfinity;
        float radialVel = Vector3.Dot(rb.linearVelocity, radialOut); // + = climbing

        // Auto-stage the moment the active stage runs dry and a decoupler is available.
        if (rc.ActiveStage != null && rc.ActiveStage.separator != null && !rc.ActiveStage.HasFuel)
            rc.Jettison();

        Vector3 targetUp = radialOut; // default: straight up
        float throttle = 0f;
        float peAlt = o.rp - planet.radius;
        const float band = 25f; // feather throttle to zero over the last `band` metres to target

        switch (phase)
        {
            case Phase.VerticalClimb:
                targetUp = radialOut;
                // raise apoapsis toward target, easing off as it approaches (prevents overshoot/escape)
                throttle = Mathf.Clamp01((targetAltitude - apAlt) / band);
                if (alt >= turnStartAlt) { phase = Phase.GravityTurn; turnTangent = Downrange(radialOut); }
                break;

            case Phase.GravityTurn:
            {
                // pitch over toward downrange, capped by maxTurn so the nose stays above the horizon
                float f = Mathf.Clamp01((alt - turnStartAlt) / Mathf.Max(1f, turnEndAlt - turnStartAlt)) * maxTurn;
                targetUp = Vector3.Slerp(radialOut, turnTangent, f).normalized;
                throttle = Mathf.Clamp01((targetAltitude - apAlt) / band);
                if (apAlt >= targetAltitude - 3f) phase = Phase.Coast;
                break;
            }

            case Phase.Coast:
                throttle = 0f;
                targetUp = Downrange(radialOut); // horizontal; never points into the ground
                if (radialVel <= 0.5f) phase = Phase.Circularize;
                break;

            case Phase.Circularize:
                // burn HORIZONTAL (downrange), not raw prograde - prograde points down while
                // descending and would drive the rocket into the terrain after a missed apoapsis.
                targetUp = Downrange(radialOut);
                throttle = Mathf.Clamp01((targetAltitude - peAlt) / 50f) * 0.6f;
                if (o.isOrbit && peAlt >= targetAltitude * 0.6f) { throttle = 0f; phase = Phase.Orbit; sweptAngle = 0f; prevRadial = radialOut; }
                if (rc.ActiveStage == null || !rc.ActiveStage.HasFuel) { phase = Phase.Orbit; sweptAngle = 0f; prevRadial = radialOut; }
                break;

            case Phase.Orbit:
                // coast a full lap (or more) around the planet, then begin the descent
                throttle = 0f;
                targetUp = Downrange(radialOut);
                sweptAngle += Vector3.Angle(prevRadial, radialOut);
                prevRadial = radialOut;
                if (landAfterOrbit && sweptAngle >= 360f * Mathf.Max(1, lapsBeforeDeorbit)) phase = Phase.Deorbit;
                break;

            case Phase.Deorbit:
                // brake retrograde to drop periapsis below the surface so we reenter
                targetUp = -rb.linearVelocity.normalized;
                throttle = 0.7f;
                if (peAlt < -20f) phase = Phase.Reentry;
                if (rc.ActiveStage == null || !rc.ActiveStage.HasFuel) phase = Phase.Reentry; // burned what we had
                break;

            case Phase.Reentry:
                // fall, nose up, so the canopy ends up on top for the landing
                throttle = 0f;
                targetUp = radialOut;
                if (alt < chuteAltitude && radialVel < 0f) phase = Phase.Chute;
                break;

            case Phase.Chute:
                throttle = 0f;
                targetUp = radialOut;
                if (rc.parachute != null) rc.parachute.Deploy(); // idempotent: only fires from Stowed
                if (alt < 3f && o.speed < 2f) phase = Phase.Landed;
                break;

            case Phase.Landed:
            default:
                throttle = 0f;
                targetUp = radialOut;
                break;
        }

        // Safety guards.
        float vEsc = Mathf.Sqrt(2f * planet.Mu / r.magnitude);
        if (!o.bound || o.speed > 0.95f * vEsc) throttle = 0f;       // never burn toward escape
        if (radialVel < -3f && alt < 30f) throttle = 0f;  // only stop a doomed burn very near the ground (no ground-boost)

        rc.throttle = throttle;
        SteerTo(targetUp);

        // trajectory trace (every ~0.5 s of mission time) for offline diagnosis
        if (Time.time - logT >= 0.5f)
        {
            logT = Time.time;
            float pitchAboveHoriz = 90f - Vector3.Angle(rb.rotation * Vector3.up, radialOut);
            trace += $"{Time.time:F1} {phase} alt={alt:F0} ap={(o.bound ? apAlt : 9999):F0} pe={peAlt:F0} thr={throttle:F2} rv={radialVel:F0} spd={o.speed:F0} pitch={pitchAboveHoriz:F0} stg={rc.ActiveStageNumber}\n";
        }
    }

    // Horizontal downrange direction (always perpendicular to radial, so thrust never points
    // into the ground): the horizontal part of velocity, else the stored turn tangent, else a seed.
    Vector3 Downrange(Vector3 radialOut)
    {
        Vector3 horiz = Vector3.ProjectOnPlane(rb.linearVelocity, radialOut);
        if (horiz.sqrMagnitude > 0.01f) return horiz.normalized;
        Vector3 t = Vector3.ProjectOnPlane(turnTangent, radialOut);
        if (t.sqrMagnitude > 0.01f) return t.normalized;
        Vector3 seed = Mathf.Abs(radialOut.x) < 0.9f ? Vector3.right : Vector3.forward;
        return Vector3.ProjectOnPlane(seed, radialOut).normalized;
    }

    // Rotate the body so its nose (+Y) slews toward targetUp at slewRate deg/s.
    void SteerTo(Vector3 targetUp)
    {
        Vector3 curUp = rb.rotation * Vector3.up;
        Quaternion desired = Quaternion.FromToRotation(curUp, targetUp) * rb.rotation;
        Quaternion next = Quaternion.RotateTowards(rb.rotation, desired, slewRate * Time.fixedDeltaTime);
        rb.MoveRotation(next);
        rb.angularVelocity = Vector3.zero; // autopilot owns attitude; no residual tumble
    }
}
