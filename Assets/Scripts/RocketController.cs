using UnityEngine;

/// <summary>
/// Player flight controls: throttle, staged thrust along the nose (+Y), and
/// reaction-wheel torque for pitch/yaw/roll. The active stage's engine burns its
/// tank; when the tank is empty it flames out. Space jettisons the spent bottom
/// stage (it gets its own Rigidbody and falls away) and the next stage takes
/// over. Rigidbody mass tracks remaining dry + fuel mass so TWR climbs as fuel
/// drains. Input is read in Update, forces applied in FixedUpdate.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RocketController : MonoBehaviour
{
    [Header("Throttle")]
    [Range(0f, 1f)] public float throttle = 0f;
    public float throttleRate = 0.75f;

    [Header("Stages (ordered bottom -> top)")]
    public Stage[] stages;
    [Tooltip("Mass of the payload/command pod that always stays with the rocket.")]
    public float payloadMass = 0.6f;
    [Tooltip("Separation impulse applied to a jettisoned stage.")]
    public float separationImpulse = 8f;

    [Header("Reaction wheels")]
    public float torquePower = 15f;

    Rigidbody rb;
    Vector3 launchPos;
    Quaternion launchRot;
    Vector3 control;      // x=pitch (W/S), y=yaw (A/D), z=roll (Q/E)
    int activeIndex = 0;

    public Stage ActiveStage => (stages != null && activeIndex < stages.Length) ? stages[activeIndex] : null;
    public int ActiveStageNumber => activeIndex + 1;
    public int StageCount => stages != null ? stages.Length : 0;
    /// <summary>True while the active engine is firing and has fuel.</summary>
    public bool EngineLit { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        launchPos = rb.position;
        launchRot = rb.rotation;
        if (rb.angularDamping < 0.5f) rb.angularDamping = 1f;
        UpdateMass();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            throttle += throttleRate * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            throttle -= throttleRate * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Z)) throttle = 1f;
        if (Input.GetKeyDown(KeyCode.X)) throttle = 0f;
        throttle = Mathf.Clamp01(throttle);

        float pitch = (Input.GetKey(KeyCode.S) ? 1f : 0f) - (Input.GetKey(KeyCode.W) ? 1f : 0f);
        float yaw   = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float roll  = (Input.GetKey(KeyCode.Q) ? 1f : 0f) - (Input.GetKey(KeyCode.E) ? 1f : 0f);
        control = new Vector3(pitch, yaw, roll);

        if (Input.GetKeyDown(KeyCode.Space)) Jettison();
        if (Input.GetKeyDown(KeyCode.R)) ResetToLaunch();
    }

    void FixedUpdate()
    {
        UpdateMass();

        EngineLit = false;
        Stage s = ActiveStage;
        if (throttle > 0f && s != null && s.tank != null && !s.tank.IsEmpty)
        {
            float want = s.fuelBurnPerSecond * throttle * Time.fixedDeltaTime;
            float drawn = s.tank.Consume(want);
            if (drawn > 0f)
            {
                float frac = want > 0f ? drawn / want : 0f; // scale thrust if the tank ran dry mid-step
                Vector3 force = transform.up * (s.maxThrust * throttle * frac);
                if (s.thrustPoint != null) rb.AddForceAtPosition(force, s.thrustPoint.position);
                else rb.AddForce(force);
                EngineLit = true;
            }
        }

        if (control.sqrMagnitude > 0f)
            rb.AddRelativeTorque(control * torquePower);
    }

    void UpdateMass()
    {
        float m = payloadMass;
        if (stages != null)
            for (int i = activeIndex; i < stages.Length; i++)
                if (stages[i] != null) m += stages[i].Mass;
        rb.mass = Mathf.Max(0.1f, m);
    }

    /// <summary>Jettison the spent bottom stage; the next stage becomes active.</summary>
    public void Jettison()
    {
        if (stages == null || activeIndex >= stages.Length - 1) return; // always keep the last stage + payload
        Stage drop = stages[activeIndex];
        activeIndex++;
        if (drop == null) { UpdateMass(); return; }

        if (drop.separator != null)
        {
            // Fire the decoupler between this stage and the one above.
            drop.separator.Separate(rb, drop);
        }
        else
        {
            // Fallback: detach and hand the spent stage its own physics directly.
            drop.transform.SetParent(null, true);
            var drb = drop.gameObject.AddComponent<Rigidbody>();
            drb.useGravity = false;
            drb.mass = Mathf.Max(0.1f, drop.Mass);
            drb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            drb.linearVelocity = rb.linearVelocity;
            drb.angularVelocity = rb.angularVelocity;
            drop.gameObject.AddComponent<GravityReceiver>();
            drb.AddForce(-transform.up * separationImpulse, ForceMode.Impulse);
            rb.AddForce(transform.up * separationImpulse * 0.3f, ForceMode.Impulse);
        }

        // Mass distribution changed: recompute inertia/COM from the remaining colliders.
        rb.ResetCenterOfMass();
        rb.ResetInertiaTensor();
        UpdateMass();
    }

    /// <summary>Snap the (current) rocket back to the launch pose. Does not un-jettison stages.</summary>
    public void ResetToLaunch()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = launchPos;
        rb.rotation = launchRot;
        transform.SetPositionAndRotation(launchPos, launchRot);
        throttle = 0f;
    }
}
