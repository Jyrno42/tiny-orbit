using UnityEngine;

/// <summary>
/// Player flight controls: throttle, staged main-engine thrust that burns fuel,
/// reaction-wheel torque for pitch/yaw/roll, stage jettison (Space) and parachute
/// deploy (P). Input is read in Update; all forces are applied in FixedUpdate.
/// Press R to snap back to the launch pose.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RocketController : MonoBehaviour
{
    [Header("Throttle")]
    [Range(0f, 1f)] public float throttle = 0f;
    [Tooltip("How fast Shift/Ctrl ramp the throttle, units per second.")]
    public float throttleRate = 1f;

    [Header("Stages (bottom-first: element 0 is the booster)")]
    public Stage[] stages;
    [Tooltip("Index of the stage currently firing.")]
    public int activeStage = 0;

    [Header("Reaction wheels")]
    public float torquePower = 15f;
    [Tooltip("Angular drag so the rocket settles when steering keys are released.")]
    public float angularDrag = 1.5f;

    [Header("Recovery")]
    public Parachute parachute;

    Rigidbody rb;
    Vector3 launchPos;
    Quaternion launchRot;
    Vector3 steerTorque; // local-space torque axis: x=pitch, y=roll, z=yaw

    /// <summary>The stage currently providing thrust, or null if all are spent/gone.</summary>
    public Stage ActiveStage =>
        (stages != null && activeStage >= 0 && activeStage < stages.Length) ? stages[activeStage] : null;

    /// <summary>Remaining fuel fraction (0..1) of the active stage, for the HUD.</summary>
    public float ActiveFuelFraction
    {
        get { var s = ActiveStage; return (s != null && s.tank != null) ? s.tank.Fraction : 0f; }
    }

    /// <summary>1-based number of the active stage, for the HUD.</summary>
    public int ActiveStageNumber => Mathf.Clamp(activeStage + 1, 0, stages != null ? stages.Length : 0);
    public int StageCount => stages != null ? stages.Length : 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = angularDrag;
        launchPos = rb.position;
        launchRot = rb.rotation;
    }

    void Update()
    {
        // --- Throttle ---
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            throttle += throttleRate * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            throttle -= throttleRate * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Z)) throttle = 1f; // full
        if (Input.GetKeyDown(KeyCode.X)) throttle = 0f; // cut
        throttle = Mathf.Clamp01(throttle);

        // --- Reaction-wheel steering (nose is local +Y) ---
        float pitch = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        float yaw   = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float roll  = (Input.GetKey(KeyCode.Q) ? 1f : 0f) - (Input.GetKey(KeyCode.E) ? 1f : 0f);
        steerTorque = new Vector3(pitch, roll, yaw); // X=pitch, Y=roll(nose axis), Z=yaw

        // --- Staging / recovery / reset ---
        if (Input.GetKeyDown(KeyCode.Space)) Jettison();
        if (Input.GetKeyDown(KeyCode.P) && parachute != null) parachute.Deploy();
        if (Input.GetKeyDown(KeyCode.R)) ResetToLaunch();
    }

    void FixedUpdate()
    {
        Stage st = ActiveStage;
        if (st != null && throttle > 0f && st.HasFuel && st.thrustPoint != null)
        {
            float request = st.fuelBurnPerSecond * throttle * Time.fixedDeltaTime;
            float drawn = st.tank.Consume(request);
            if (drawn > 0f)
                rb.AddForceAtPosition(transform.up * (st.maxThrust * throttle),
                                      st.thrustPoint.position, ForceMode.Force);
        }

        if (steerTorque != Vector3.zero)
            rb.AddRelativeTorque(steerTorque * torquePower, ForceMode.Force);
    }

    /// <summary>Fire the active stage's decoupler (if any) and advance to the next stage.</summary>
    public void Jettison()
    {
        Stage st = ActiveStage;
        if (st != null && st.separator != null)
        {
            st.separator.Separate(rb);
            activeStage = Mathf.Min(activeStage + 1, stages.Length - 1);
        }
    }

    /// <summary>Snap back to the captured launch pose with zero velocity. (Dropped stages are not restored.)</summary>
    void ResetToLaunch()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = launchPos;
        rb.rotation = launchRot;
        transform.SetPositionAndRotation(launchPos, launchRot);
        throttle = 0f;
    }
}
