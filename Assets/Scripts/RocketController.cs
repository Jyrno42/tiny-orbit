using UnityEngine;

/// <summary>
/// Player flight controls: throttle-driven thrust along the nose (+Y) and
/// reaction-wheel torque for pitch/yaw/roll. Input is sampled in Update and
/// applied in FixedUpdate. Throttle and rotation input are public so an
/// autopilot can drive the same code path as the keys.
/// Keys: Shift/Ctrl throttle up/down, Z full, X cut, W/S pitch, A/D yaw,
/// Q/E roll, R reset to the launch pose.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RocketController : MonoBehaviour
{
    [Tooltip("Thrust in newtons at throttle 1. Mass 5 and g0 9.81 give TWR ~2.45 at 120.")]
    [SerializeField] private float maxThrust = 120f;

    [Tooltip("Reaction wheel torque strength.")]
    [SerializeField] private float torquePower = 15f;

    [Tooltip("Throttle change per second while Shift/Ctrl is held.")]
    [SerializeField] private float throttleRate = 0.5f;

    [Tooltip("Angular damping so rotation settles when keys are released.")]
    [SerializeField] private float angularDamping = 1.5f;

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

    /// <summary>Thrust force currently produced (newtons).</summary>
    public float CurrentThrust => maxThrust * throttle;

    /// <summary>Max thrust of the active engine (newtons).</summary>
    public float MaxThrust => maxThrust;

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
        }

        if (Input.GetKeyDown(KeyCode.X))
            Throttle = 0f;
        if (Input.GetKeyDown(KeyCode.R))
            ResetToLaunch();
    }

    void FixedUpdate()
    {
        if (throttle > 0f)
            rb.AddForce(transform.up * CurrentThrust);
        if (RotationInput != Vector3.zero)
            rb.AddRelativeTorque(RotationInput * torquePower);
    }

    /// <summary>Puts the rocket back on the pad with zero velocity and throttle.</summary>
    public void ResetToLaunch()
    {
        throttle = 0f;
        RotationInput = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = launchPosition;
        rb.rotation = launchRotation;
        transform.SetPositionAndRotation(launchPosition, launchRotation);
        Physics.SyncTransforms();
    }
}
