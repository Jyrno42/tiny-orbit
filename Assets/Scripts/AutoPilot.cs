using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Fully autonomous mission: launch -> gravity turn -> auto-stage -> coast to
/// apoapsis -> circularize -> hold one full orbit -> deorbit -> reentry -> chute
/// -> land. Engage with T or the HUD button. Drives throttle (via the controller)
/// and attitude (PD reaction-wheel torque) itself; KSP-style auto time-warp is
/// keyed to the mission phase. A per-second trace buffer (cleared on each engage)
/// records the flight for after-the-fact diagnosis, since a live flight cannot be
/// polled reliably across tool calls.
/// </summary>
public class AutoPilot : MonoBehaviour
{
    public RocketController controller;
    public Parachute parachute;
    Rigidbody rb;
    PlanetBody planet;

    public enum Phase { Idle, Launch, GravityTurn, CoastToApoapsis, Circularize, OrbitHold, Deorbit, Reentry, Chute, Landed }
    public Phase phase = Phase.Idle;
    public bool Engaged { get; private set; }
    public float CurrentWarp { get; private set; } = 1f;

    [Header("Ascent")]
    public float gravityTurnStartAlt = 35f;
    public float gravityTurnEndAlt = 420f;
    public float maxTurnDeg = 60f;          // cap pitch-over so it keeps climbing
    public float apoapsisTargetAlt = 320f;
    public float apoapsisFeather = 160f;    // throttle feathers over this band below target

    [Header("Circularize / orbit")]
    public float circThrottleCap = 0.6f;
    public float periapsisTargetAlt = 110f;
    public float periapsisFeather = 120f;
    public float escapeGuardFrac = 0.96f;   // cut if speed nears escape

    [Header("Deorbit / land")]
    public float deorbitThrottle = 0.5f;
    public float deorbitPeTargetAlt = -60f; // lower Pe below ground to reenter
    public float chuteDeployAlt = 160f;
    public float groundSafetyAlt = 30f;     // throttle cut ONLY this close to the ground

    [Header("Attitude PD")]
    public float kP = 7f;
    public float kD = 3.5f;
    public float maxAngAccel = 25f;

    [Header("Time warp")]
    public float warpCoast = 4f;
    public float coastWarpDelay = 4f;       // hold 1x this long after cutoff before warping
    public float anticipateVSpeed = 12f;    // drop to 1x when climb slows near apoapsis

    // trace
    struct Sample { public float t, alt, spd, vsp, ap, pe; public int stg; public float thr; public string ph; }
    readonly List<Sample> trace = new List<Sample>();
    float nextSample, engageTime, cutoffTime = -999f;
    Vector3 lastTangent = Vector3.forward;
    Vector3 prevR;
    float sweptDeg;
    GUIStyle warpStyle, btnStyle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (controller == null) controller = GetComponent<RocketController>();
        if (parachute == null) parachute = GetComponent<Parachute>();
        planet = FindObjectOfType<PlanetBody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T)) Toggle();
        if (!Engaged) return;
        if (Time.time >= nextSample) { Record(); nextSample = Time.time + 1f; }
    }

    public void Toggle() { if (Engaged) Disengage(); else Engage(); }

    public void Engage()
    {
        Engaged = true;
        phase = Phase.Launch;
        trace.Clear();
        engageTime = Time.time;
        nextSample = Time.time;
        cutoffTime = -999f;
        sweptDeg = 0f;
        prevR = R;
        lastTangent = Vector3.ProjectOnPlane(transform.forward, RadialUp).normalized;
        if (lastTangent.sqrMagnitude < 1e-4f) lastTangent = Vector3.ProjectOnPlane(Vector3.forward, RadialUp).normalized;
    }

    public void Disengage()
    {
        Engaged = false;
        phase = Phase.Idle;
        if (controller != null) controller.throttle = 0f;
        Time.timeScale = 1f; // never leak warp
    }

    void OnDisable() { Time.timeScale = 1f; }   // reset on play-stop / disable
    void OnApplicationQuit() { Time.timeScale = 1f; }

    // --- state helpers ---
    Vector3 R => rb.position - planet.transform.position;
    Vector3 V => rb.linearVelocity;
    Vector3 RadialUp => R.normalized;
    float Alt => R.magnitude - planet.radius;
    float VSpeed => Vector3.Dot(V, RadialUp);
    float Mu => planet.Mu;

    Vector3 Tangent()
    {
        Vector3 t = Vector3.ProjectOnPlane(V, RadialUp);
        if (t.sqrMagnitude > 0.5f) lastTangent = t.normalized;
        else lastTangent = Vector3.ProjectOnPlane(lastTangent, RadialUp).normalized;
        return lastTangent;
    }

    void FixedUpdate()
    {
        if (!Engaged) return;
        var el = OrbitMath.Compute(R, V, Mu, planet.radius);
        float apAlt = el.bound ? el.ra - planet.radius : float.PositiveInfinity;
        float peAlt = el.rp - planet.radius;
        float speed = V.magnitude;
        float vEsc = Mathf.Sqrt(2f * Mu / R.magnitude);
        bool escapeGuard = speed >= escapeGuardFrac * vEsc;

        Vector3 steer = RadialUp; // default: point up
        float thr = 0f;

        switch (phase)
        {
            case Phase.Launch:
                steer = RadialUp; thr = 1f;
                if (Alt > gravityTurnStartAlt) phase = Phase.GravityTurn;
                break;

            case Phase.GravityTurn:
            {
                float f = Mathf.Clamp01((Alt - gravityTurnStartAlt) / (gravityTurnEndAlt - gravityTurnStartAlt));
                float turn = f * maxTurnDeg;
                steer = Quaternion.AngleAxis(turn, Vector3.Cross(RadialUp, Tangent())) * RadialUp;
                // feather throttle so apoapsis creeps to target, with hard escape guard
                thr = escapeGuard ? 0f : Mathf.Clamp01((apoapsisTargetAlt - apAlt) / apoapsisFeather);
                if (controller.ActiveStage != null && controller.ActiveStage.tank.IsEmpty)
                    controller.Jettison();
                if (apAlt >= apoapsisTargetAlt || escapeGuard) { phase = Phase.CoastToApoapsis; cutoffTime = Time.time; }
                break;
            }

            case Phase.CoastToApoapsis:
                steer = Tangent(); thr = 0f;
                if (controller.ActiveStage != null && controller.ActiveStage.tank.IsEmpty) controller.Jettison();
                if (VSpeed <= 2f) phase = Phase.Circularize; // at/just before apoapsis
                break;

            case Phase.Circularize:
                steer = Tangent(); // burn horizontal / downrange, NEVER raw velocity
                thr = escapeGuard ? 0f : Mathf.Min(circThrottleCap, Mathf.Clamp01((periapsisTargetAlt - peAlt) / periapsisFeather));
                if (controller.ActiveStage != null && controller.ActiveStage.tank.IsEmpty) controller.Jettison();
                if (peAlt >= periapsisTargetAlt || escapeGuard)
                { phase = Phase.OrbitHold; thr = 0f; sweptDeg = 0f; prevR = R; }
                break;

            case Phase.OrbitHold:
            {
                steer = Tangent(); thr = 0f;
                float d = Vector3.Angle(prevR, R); sweptDeg += d; prevR = R;
                if (sweptDeg >= 360f) phase = Phase.Deorbit;
                break;
            }

            case Phase.Deorbit:
                steer = -Tangent(); // retrograde
                thr = deorbitThrottle;
                if (peAlt <= deorbitPeTargetAlt) { phase = Phase.Reentry; thr = 0f; }
                break;

            case Phase.Reentry:
                steer = -V.normalized; thr = 0f; // retrograde-stable
                if (Alt <= chuteDeployAlt && VSpeed < 0f) { if (parachute != null) parachute.Deploy(); phase = Phase.Chute; }
                break;

            case Phase.Chute:
                steer = RadialUp; thr = 0f;
                if (Alt < 2f && speed < 4f) phase = Phase.Landed;
                break;

            case Phase.Landed:
                thr = 0f; Disengage(); return;
        }

        // Ground-safety cut: only very near the ground while descending (never near apoapsis).
        if (Alt < groundSafetyAlt && VSpeed < 0f && phase != Phase.Launch) thr = 0f;

        controller.throttle = Mathf.Clamp01(thr);
        SteerTo(steer);
    }

    void SteerTo(Vector3 targetDir)
    {
        Vector3 cur = rb.transform.up;
        Vector3 tgt = targetDir.normalized;
        Vector3 cross = Vector3.Cross(cur, tgt);
        float sin = cross.magnitude, cos = Vector3.Dot(cur, tgt);
        float ang = Mathf.Atan2(sin, cos);
        Vector3 axis = sin > 1e-5f ? cross / sin : Vector3.zero;
        Vector3 torque = axis * (ang * kP) - rb.angularVelocity * kD;
        torque = Vector3.ClampMagnitude(torque, maxAngAccel);
        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    void LateUpdate()
    {
        if (!Engaged) { return; }
        // Auto time-warp keyed to phase. Burns/launch/chute/landing at 1x.
        float w = 1f;
        switch (phase)
        {
            case Phase.CoastToApoapsis:
                if (Time.time - cutoffTime > coastWarpDelay && VSpeed > anticipateVSpeed) w = warpCoast;
                break;
            case Phase.OrbitHold:
                w = (sweptDeg < 330f) ? warpCoast : 1f; // anticipate deorbit burn
                break;
            case Phase.Reentry:
                w = (Alt > 260f && VSpeed < 0f) ? warpCoast : 1f;
                break;
        }
        CurrentWarp = Mathf.Min(w, warpCoast);
        Time.timeScale = CurrentWarp;
    }

    void Record()
    {
        var el = OrbitMath.Compute(R, V, Mu, planet.radius);
        trace.Add(new Sample {
            t = Time.time - engageTime, alt = Alt, spd = V.magnitude, vsp = VSpeed,
            ap = el.bound ? el.ra - planet.radius : float.PositiveInfinity,
            pe = el.rp - planet.radius, stg = controller.ActiveStageNumber, thr = controller.throttle,
            ph = phase.ToString()
        });
    }

    public string DumpTrace()
    {
        var sb = new StringBuilder();
        sb.AppendLine("t alt spd vsp Ap Pe stg thr phase");
        foreach (var s in trace)
            sb.AppendLine($"{s.t:F0} {s.alt:F0} {s.spd:F0} {s.vsp:F0} {s.ap:F0} {s.pe:F0} {s.stg} {s.thr:F2} {s.ph}");
        return sb.ToString();
    }

    void EnsureStyles()
    {
        if (warpStyle != null) return;
        int fs = Mathf.Max(16, Mathf.RoundToInt(Screen.height * 0.022f));
        warpStyle = new GUIStyle(GUI.skin.label) { fontSize = fs, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        warpStyle.normal.textColor = new Color(0.6f, 0.9f, 1f);
        btnStyle = new GUIStyle(GUI.skin.button) { fontSize = fs };
    }

    void OnGUI()
    {
        EnsureStyles();
        // Engage button (bottom-left of HUD area)
        string label = Engaged ? ("AUTOPILOT: " + phase) : "ENGAGE AUTOPILOT (T)";
        if (GUI.Button(new Rect(14, Screen.height - 60, 360, 44), label, btnStyle)) Toggle();

        // Time-warp indicator, centered at top, hidden at 1x
        if (Engaged && CurrentWarp > 1.01f)
        {
            string arrows = CurrentWarp >= 3.5f ? ">>>>" : CurrentWarp >= 2.5f ? ">>>" : ">>";
            GUI.Label(new Rect(Screen.width / 2f - 200f, 10f, 400f, 40f),
                $"{arrows}  {CurrentWarp:0}x TIME WARP  {arrows}", warpStyle);
        }
    }
}
