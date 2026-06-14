using UnityEngine;

/// <summary>
/// Player flight controls: throttle + main-engine thrust along the nose (+Y) and
/// reaction-wheel torque for pitch/yaw/roll. Input is read in Update; all forces
/// are applied in FixedUpdate. Press R to snap back to the launch pose.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RocketController : MonoBehaviour
{
    [Header("Thrust")]
    [Tooltip("Engine thrust in Newtons at full throttle. With mass 5 and g0 9.81 " +
             "(weight ~49), 120 gives TWR ~2.4.")]
    public float maxThrust = 120f;

    [Tooltip("Point where thrust is applied (rocket base). On the central axis, so " +
             "it produces no steering torque.")]
    public Transform thrustPoint;

    [Header("Throttle")]
    [Range(0f, 1f)] public float throttle = 0f;

    [Tooltip("How fast Shift/Ctrl ramp the throttle, units per second.")]
    public float throttleRate = 1f;

    [Header("Reaction wheels")]
    [Tooltip("Torque strength for pitch/yaw/roll.")]
    public float torquePower = 15f;

    [Tooltip("Angular drag so the rocket settles when steering keys are released.")]
    public float angularDrag = 1.5f;

    Rigidbody rb;
    Vector3 launchPos;
    Quaternion launchRot;
    Vector3 steerTorque; // local-space torque axis: x=pitch, y=roll, z=yaw

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.angularDamping = angularDrag;
        launchPos = rb.position;
        launchRot = rb.rotation;
        if (thrustPoint == null)
        {
            Transform tp = transform.Find("ThrustPoint");
            thrustPoint = tp != null ? tp : transform;
        }
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

        // --- Reset ---
        if (Input.GetKeyDown(KeyCode.R)) ResetToLaunch();
    }

    void FixedUpdate()
    {
        if (throttle > 0f)
            rb.AddForceAtPosition(transform.up * (maxThrust * throttle),
                                  thrustPoint.position, ForceMode.Force);

        if (steerTorque != Vector3.zero)
            rb.AddRelativeTorque(steerTorque * torquePower, ForceMode.Force);
    }

    /// <summary>Snap back to the captured launch pose with zero velocity.</summary>
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
