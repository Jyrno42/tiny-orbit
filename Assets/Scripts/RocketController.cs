using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player flight controls: throttle-driven thrust along the nose (+Y) and
/// reaction-wheel torque for pitch/yaw/roll. Input is sampled in Update and
/// applied in FixedUpdate. Throttle and rotation input are public so an
/// autopilot can drive the same code path as the keys.
/// With stages configured, thrust comes from the active stage's engine, burns
/// its tank (flameout when empty), the craft mass tracks remaining fuel, and
/// Space fires the separator to jettison the spent booster.
/// Keys: Shift/Ctrl throttle up/down, Z full, X cut, W/S pitch, A/D yaw,
/// Q/E roll, Space stage, R reset to the launch pose.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RocketController : MonoBehaviour
{
    [Tooltip("Fallback thrust when no stages are configured.")]
    [SerializeField] private float maxThrust = 120f;

    [Tooltip("Reaction wheel torque strength.")]
    [SerializeField] private float torquePower = 15f;

    [Tooltip("Throttle change per second while Shift/Ctrl is held.")]
    [SerializeField] private float throttleRate = 0.5f;

    [Tooltip("Angular damping so rotation settles when keys are released.")]
    [SerializeField] private float angularDamping = 1.5f;

    [Tooltip("Stages bottom-up: element 0 is the booster, last is the final stage.")]
    [SerializeField] private List<Stage> stages = new List<Stage>();

    [Tooltip("Separator fired when jettisoning the booster.")]
    [SerializeField] private StackSeparator separator;

    /// <summary>Current throttle, clamped to [0, 1].</summary>
    public float Throttle
    {
        get => throttle;
        set => throttle = Mathf.Clamp01(value);
    }

    /// <summary>Local torque input (x = pitch, y = roll, z = yaw), each in [-1, 1].</summary>
    public Vector3 RotationInput { get; set; }

    /// <summary>
    /// When true, keyboard flight input is ignored so an autopilot (or test
    /// harness) can set Throttle/RotationInput without Update overwriting them.
    /// R (reset) and X (cut) stay active as manual overrides.
    /// </summary>
    public bool AutopilotOverride { get; set; }

    /// <summary>Index of the stage currently providing thrust (0 = booster).</summary>
    public int ActiveStageIndex { get; private set; }

    /// <summary>Number of stages configured.</summary>
    public int StageCount => stages.Count;

    /// <summary>The stage currently providing thrust, or null when none configured.</summary>
    public Stage ActiveStage =>
        stages.Count > 0 && ActiveStageIndex < stages.Count ? stages[ActiveStageIndex] : null;

    /// <summary>Thrust force currently produced (newtons); zero when flamed out.</summary>
    public float CurrentThrust
    {
        get
        {
            var s = ActiveStage;
            if (s == null) return maxThrust * throttle;
            return s.HasFuel ? s.maxThrust * throttle : 0f;
        }
    }

    /// <summary>Max thrust of the active engine (newtons).</summary>
    public float MaxThrust => ActiveStage != null ? ActiveStage.maxThrust : maxThrust;

    private Rigidbody rb;
    private float throttle;
    private Vector3 launchPosition;
    private Quaternion launchRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = angularDamping;
        launchPosition = transform.position;
        launchRotation = transform.rotation;
        UpdateMass();
    }

    void Update()
    {
        if (!AutopilotOverride)
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                Throttle = throttle + throttleRate * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                Throttle = throttle - throttleRate * Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Z))
                Throttle = 1f;

            float pitch = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            float yaw = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float roll = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            RotationInput = new Vector3(pitch, roll, yaw);

            if (Input.GetKeyDown(KeyCode.Space))
                StageNow();
        }

        if (Input.GetKeyDown(KeyCode.X))
            Throttle = 0f;
        if (Input.GetKeyDown(KeyCode.R))
            ResetToLaunch();
    }

    void FixedUpdate()
    {
        float thrust = 0f;
        if (throttle > 0f)
        {
            var s = ActiveStage;
            if (s == null)
            {
                thrust = maxThrust * throttle;
            }
            else if (s.HasFuel)
            {
                float want = s.fuelBurnPerSecond * throttle * Time.fixedDeltaTime;
                float got = s.tank != null ? s.tank.Consume(want) : want;
                thrust = s.maxThrust * throttle * (want > 1e-9f ? got / want : 1f);
            }
        }
        if (thrust > 0f)
            rb.AddForce(transform.up * thrust);
        if (RotationInput != Vector3.zero)
            rb.AddRelativeTorque(RotationInput * torquePower);
        UpdateMass();
    }

    void UpdateMass()
    {
        if (stages.Count == 0) return;
        float m = 0f;
        for (int i = ActiveStageIndex; i < stages.Count; i++)
            if (stages[i] != null)
                m += stages[i].CurrentMass;
        rb.mass = Mathf.Max(m, 0.5f);
    }

    /// <summary>
    /// Fires the separator and jettisons the spent booster. Returns true when
    /// a stage was actually dropped.
    /// </summary>
    public bool StageNow()
    {
        if (ActiveStageIndex != 0 || stages.Count < 2 || separator == null || separator.Fired)
            return false;
        separator.Separate(stages[0]);
        ActiveStageIndex = 1;
        UpdateMass();
        var hud = GetComponent<OrbitHUD>();
        if (hud != null) hud.RefreshColliders();
        return true;
    }

    /// <summary>Puts the rocket back on the pad with zero velocity and full active tanks.</summary>
    public void ResetToLaunch()
    {
        throttle = 0f;
        RotationInput = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = launchPosition;
        rb.rotation = launchRotation;
        transform.SetPositionAndRotation(launchPosition, launchRotation);
        for (int i = ActiveStageIndex; i < stages.Count; i++)
            if (stages[i] != null && stages[i].tank != null)
                stages[i].tank.Refill();
        UpdateMass();
        Physics.SyncTransforms();
    }
}
