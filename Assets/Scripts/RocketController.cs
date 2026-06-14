using UnityEngine;

/// <summary>
/// Player flight controls: throttle, thrust along the nose (+Y), and
/// reaction-wheel torque for pitch/yaw/roll. Input is read in Update and the
/// forces/torques are applied in FixedUpdate, as Unity physics expects.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RocketController : MonoBehaviour
{
    [Header("Thrust")]
    [Tooltip("Max engine force at full throttle. With mass 5 and g0 9.81, 120 gives TWR ~2.4.")]
    public float maxThrust = 120f;
    [Range(0f, 1f)] public float throttle = 0f;
    [Tooltip("How fast Shift/Ctrl ramp the throttle, per second.")]
    public float throttleRate = 0.75f;
    [Tooltip("Optional: apply thrust at this point (on the central axis = no spurious torque). Falls back to the centre of mass.")]
    public Transform thrustPoint;

    [Header("Reaction wheels")]
    [Tooltip("Torque strength for pitch/yaw/roll.")]
    public float torquePower = 15f;

    Rigidbody rb;
    Vector3 launchPos;
    Quaternion launchRot;
    Vector3 control; // x=pitch (W/S), y=yaw (A/D), z=roll (Q/E)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        launchPos = rb.position;
        launchRot = rb.rotation;
        if (rb.angularDamping < 0.5f) rb.angularDamping = 1f; // settle when keys released
    }

    void Update()
    {
        // Throttle
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            throttle += throttleRate * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            throttle -= throttleRate * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Z)) throttle = 1f; // full
        if (Input.GetKeyDown(KeyCode.X)) throttle = 0f; // cut
        throttle = Mathf.Clamp01(throttle);

        // Rotation input (reaction wheels)
        float pitch = (Input.GetKey(KeyCode.S) ? 1f : 0f) - (Input.GetKey(KeyCode.W) ? 1f : 0f);
        float yaw   = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float roll  = (Input.GetKey(KeyCode.Q) ? 1f : 0f) - (Input.GetKey(KeyCode.E) ? 1f : 0f);
        control = new Vector3(pitch, yaw, roll);

        // Quick retry: reset to the launch pose
        if (Input.GetKeyDown(KeyCode.R)) ResetToLaunch();
    }

    void FixedUpdate()
    {
        // Thrust along the nose (+Y). On-axis application point keeps it torque-free.
        if (throttle > 0f)
        {
            Vector3 force = transform.up * (maxThrust * throttle);
            if (thrustPoint != null) rb.AddForceAtPosition(force, thrustPoint.position);
            else rb.AddForce(force);
        }

        // Reaction-wheel torque in the rocket's local frame.
        if (control.sqrMagnitude > 0f)
            rb.AddRelativeTorque(control * torquePower);
    }

    /// <summary>Snap the rocket back to the launch pose with zero velocity.</summary>
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
