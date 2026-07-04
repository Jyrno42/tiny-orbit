using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Autonomous mission: launch, capped gravity turn, full-throttle booster to
/// depletion with auto-stage, feathered final-stage burn to the apoapsis
/// target, coast, capped horizontal circularization with a hard escape-speed
/// guard, one full 360-degree orbit, retrograde deorbit, shield-first reentry,
/// parachute deploy and landing. Engage with T or the HUD button.
/// Drives Time.timeScale KSP-style: 1x for every burn (with anticipation),
/// warp on coasts and the orbital lap, always reset on end/disable.
/// Keeps a per-second trace buffer (cleared on engage) because live polling
/// over MCP is too slow to observe a flight.
/// </summary>
[RequireComponent(typeof(RocketController))]
public class AutoPilot : MonoBehaviour
{
    public enum Phase { Idle, Launch, GravityTurn, Coast, Circularize, HoldOrbit, DeorbitBurn, Reentry, Descent, Landed }

    [Header("Ascent")]
    [SerializeField] private float apoapsisTargetAlt = 800f;
    [Tooltip("Feather band: final-stage throttle ramps down over this many metres of remaining apoapsis.")]
    [SerializeField] private float apoapsisFeather = 160f;
    [SerializeField] private float maxTurnDeg = 60f;
    [Tooltip("Altitude by which the gravity turn reaches its full pitch.")]
    [SerializeField] private float turnEndAlt = 320f;

    [Header("Orbit")]
    [SerializeField] private float periapsisTarget = 110f;
    [SerializeField] private float circThrottleCap = 0.6f;
    [SerializeField] private float escapeGuardFrac = 0.96f;

    [Header("Return")]
    [SerializeField] private float deorbitPeriapsis = -80f;
    [SerializeField] private float chuteDeployAlt = 240f;

    [Header("Time warp")]
    [SerializeField] private float maxWarp = 4f;
    [SerializeField] private float coastWarpDelay = 4f;

    [Header("Attitude PD")]
    [SerializeField] private float attitudeKp = 3f;
    [SerializeField] private float attitudeKd = 2f;

    /// <summary>Current mission phase.</summary>
    public Phase CurrentPhase { get; private set; } = Phase.Idle;

    /// <summary>True while the autopilot is flying.</summary>
    public bool Engaged { get; private set; }

    /// <summary>Per-second mission trace, cleared on each engage.</summary>
    public IReadOnlyList<string> Trace => trace;

    private RocketController rc;
    private Rigidbody rb;
    private PlanetBody planet;
    private Parachute chute;
    private OrbitHUD hud;
    private readonly List<string> trace = new List<string>();
    private Vector3 downrange = Vector3.right;
    private Vector3 prevRadial;
    private float sweptDeg;
    private float cutoffTime = -999f;
    private float nextTrace;

    void Awake()
    {
        rc = GetComponent<RocketController>();
        rb = GetComponent<Rigidbody>();
        planet = Object.FindFirstObjectByType<PlanetBody>();
        hud = GetComponent<OrbitHUD>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            Toggle();
    }

    /// <summary>Engage/disengage from the T key or HUD button.</summary>
    public void Toggle()
    {
        if (Engaged) Disengage("manual");
        else Engage();
    }

    /// <summary>Starts the autonomous mission from the pad (or mid-flight).</summary>
    public void Engage()
    {
        if (planet == null) return;
        Engaged = true;
        rc.AutopilotOverride = true;
        chute = GetComponentInChildren<Parachute>();
        trace.Clear();
        sweptDeg = 0f;
        downrange = Vector3.right;
        prevRadial = Radial();
        CurrentPhase = Alt() < 5f ? Phase.Launch : Phase.GravityTurn;
        Log($"engaged, phase={CurrentPhase}");
    }

    /// <summary>Stops flying, cuts throttle and resets time warp.</summary>
    public void Disengage(string why)
    {
        Engaged = false;
        rc.AutopilotOverride = false;
        rc.Throttle = 0f;
        rc.RotationInput = Vector3.zero;
        Time.timeScale = 1f;
        Log($"disengaged ({why})");
    }

    void OnDisable()
    {
        Time.timeScale = 1f;
    }

    Vector3 Radial() => (rb.position - planet.transform.position).normalized;

    float Alt() => hud != null
        ? hud.GroundClearance()
        : (rb.position - planet.transform.position).magnitude - planet.Radius;

    float Fuel() => rc.ActiveStage != null && rc.ActiveStage.tank != null ? rc.ActiveStage.tank.Fuel : -1f;

    void Log(string msg)
    {
        Debug.Log($"[AutoPilot] t={Time.time:F1} {msg}");
    }

    void Next(Phase p)
    {
        CurrentPhase = p;
        Log($"phase -> {p}");
    }

    /// <summary>PD attitude controller: torque toward a desired nose direction.</summary>
    void PointAt(Vector3 desiredDir)
    {
        Vector3 up = transform.up;
        Vector3 d = desiredDir.normalized;
        Vector3 axis = Vector3.Cross(up, d);
        float sinAng = axis.magnitude;
        float ang = Mathf.Asin(Mathf.Clamp01(sinAng));
        if (Vector3.Dot(up, d) < 0f)
            ang = Mathf.PI - ang;
        Vector3 axisN = sinAng > 1e-5f ? axis / sinAng : Vector3.zero;
        Vector3 cmdWorld = axisN * (ang * attitudeKp) - rb.angularVelocity * attitudeKd;
        Vector3 local = transform.InverseTransformDirection(cmdWorld);
        rc.RotationInput = new Vector3(
            Mathf.Clamp(local.x, -1f, 1f),
            Mathf.Clamp(local.y, -1f, 1f),
            Mathf.Clamp(local.z, -1f, 1f));
    }

    void FixedUpdate()
    {
        if (!Engaged || planet == null)
            return;

        var el = OrbitMath.FromState(rb.position - planet.transform.position, rb.linearVelocity, planet.Mu);
        float R = planet.Radius;
        float alt = Alt();
        float apAlt = el.isBound ? el.apoapsisRadius - R : float.PositiveInfinity;
        float peAlt = float.IsInfinity(el.periapsisRadius) ? -R : el.periapsisRadius - R;
        Vector3 radial = Radial();
        Vector3 vel = rb.linearVelocity;
        float vUp = Vector3.Dot(vel, radial);
        Vector3 vHoriz = vel - radial * vUp;

        // parallel-transported downrange heading; follow real motion once moving
        Vector3 dr = vHoriz.sqrMagnitude > 4f
            ? vHoriz
            : downrange - radial * Vector3.Dot(downrange, radial);
        if (dr.sqrMagnitude > 1e-6f)
            downrange = dr.normalized;

        float vesc = Mathf.Sqrt(2f * planet.Mu / el.radius);
        bool escapeDanger = el.speed >= escapeGuardFrac * vesc;
        // ground-safety cut: only very near the ground, never during ascent
        bool groundDanger = CurrentPhase >= Phase.Coast && alt < 30f;

        switch (CurrentPhase)
        {
            case Phase.Launch:
                rc.Throttle = 1f;
                PointAt(radial);
                if (alt > 40f)
                    Next(Phase.GravityTurn);
                break;

            case Phase.GravityTurn:
            {
                bool finalStage = rc.ActiveStageIndex >= rc.StageCount - 1;
                if (!finalStage)
                {
                    // lower stages burn flat out to depletion so staging actually triggers
                    rc.Throttle = 1f;
                    if (rc.ActiveStage != null && !rc.ActiveStage.HasFuel)
                    {
                        rc.StageNow();
                        Log($"auto-stage at alt={alt:F0}");
                    }
                }
                else
                {
                    float remaining = apoapsisTargetAlt - apAlt;
                    rc.Throttle = float.IsInfinity(apAlt) ? 0f : Mathf.Clamp01(remaining / apoapsisFeather);
                    if (remaining <= 0f)
                    {
                        rc.Throttle = 0f;
                        cutoffTime = Time.time;
                        Next(Phase.Coast);
                    }
                }
                if (escapeDanger)
                {
                    rc.Throttle = 0f;
                    cutoffTime = Time.time;
                    Log("escape guard cut");
                    Next(Phase.Coast);
                }
                float pitch = maxTurnDeg * Mathf.Pow(Mathf.Clamp01(alt / turnEndAlt), 0.6f);
                PointAt(Vector3.RotateTowards(radial, downrange, pitch * Mathf.Deg2Rad, 0f));
                break;
            }

            case Phase.Coast:
                rc.Throttle = 0f;
                PointAt(downrange);
                if (vUp < 2f)
                    Next(Phase.Circularize);
                break;

            case Phase.Circularize:
            {
                Vector3 burnDir = vHoriz.sqrMagnitude > 1f ? vHoriz.normalized : downrange;
                PointAt(burnDir);
                float align = Vector3.Dot(transform.up, burnDir);
                float feather = Mathf.Clamp((periapsisTarget + 60f - peAlt) / 120f, 0.08f, 1f);
                rc.Throttle = align > 0.9f && !escapeDanger && !groundDanger
                    ? circThrottleCap * feather
                    : 0f;
                bool fuelOut = rc.ActiveStage != null && !rc.ActiveStage.HasFuel;
                if (peAlt >= periapsisTarget || escapeDanger || fuelOut)
                {
                    rc.Throttle = 0f;
                    sweptDeg = 0f;
                    prevRadial = radial;
                    Log($"circularized: Ap={apAlt:F0} Pe={peAlt:F0} e={el.eccentricity:F3} fuel={Fuel():F0}");
                    Next(Phase.HoldOrbit);
                }
                break;
            }

            case Phase.HoldOrbit:
                rc.Throttle = 0f;
                if (vel.sqrMagnitude > 1f)
                    PointAt(vel.normalized);
                sweptDeg += Vector3.Angle(prevRadial, radial);
                prevRadial = radial;
                if (sweptDeg >= 360f)
                {
                    Log("full orbit complete, deorbiting");
                    Next(Phase.DeorbitBurn);
                }
                break;

            case Phase.DeorbitBurn:
            {
                Vector3 retro = -vel.normalized;
                PointAt(retro);
                float align = Vector3.Dot(transform.up, retro);
                rc.Throttle = align > 0.95f && !groundDanger ? 0.6f : 0f;
                bool fuelOut = rc.ActiveStage != null && !rc.ActiveStage.HasFuel;
                if (peAlt <= deorbitPeriapsis || fuelOut)
                {
                    rc.Throttle = 0f;
                    Log($"deorbit burn done: Pe={peAlt:F0} fuel={Fuel():F0}");
                    Next(Phase.Reentry);
                }
                break;
            }

            case Phase.Reentry:
                rc.Throttle = 0f;
                if (vel.sqrMagnitude > 1f)
                    PointAt(-vel.normalized); // heat shield leads
                if (alt < chuteDeployAlt && vUp < 0f)
                {
                    if (chute != null)
                        chute.Deploy();
                    Log($"chute deploy: alt={alt:F0} speed={el.speed:F1}");
                    Next(Phase.Descent);
                }
                break;

            case Phase.Descent:
                rc.Throttle = 0f;
                rc.RotationInput = Vector3.zero;
                if (alt < 0.6f && vel.magnitude < 1f)
                {
                    Time.timeScale = 1f;
                    Log($"LANDED, touchdown speed={vel.magnitude:F2}");
                    Next(Phase.Landed);
                }
                break;

            case Phase.Landed:
                rc.Throttle = 0f;
                rc.RotationInput = Vector3.zero;
                break;
        }

        if (groundDanger)
            rc.Throttle = 0f;

        UpdateWarp(alt, vUp);

        if (Time.time >= nextTrace)
        {
            nextTrace = Time.time + 1f;
            trace.Add($"t={Time.time:F0} ph={CurrentPhase} alt={alt:F0} v={el.speed:F1} vUp={vUp:F1} Ap={(float.IsInfinity(apAlt) ? -1f : apAlt):F0} Pe={peAlt:F0} thr={rc.Throttle:F2} fuel={Fuel():F0} warp={Time.timeScale:F0}");
        }
    }

    void UpdateWarp(float alt, float vUp)
    {
        float target = 1f;
        switch (CurrentPhase)
        {
            case Phase.Coast:
                // wait after cutoff, and anticipate the circularization burn
                if (Time.time - cutoffTime > coastWarpDelay && vUp > 6f)
                    target = 3f;
                break;
            case Phase.HoldOrbit:
                target = sweptDeg < 345f ? maxWarp : 1f;
                break;
            case Phase.Reentry:
                target = alt > 500f ? 3f : 1f;
                break;
            case Phase.Descent:
                // chute drift is slow; gentle warp until close to the ground
                target = alt > 80f ? 2f : 1f;
                break;
        }
        Time.timeScale = target;
    }
}
