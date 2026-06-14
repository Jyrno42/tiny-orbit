using UnityEngine;

/// <summary>
/// Capsule parachute with a small state machine: Stowed -> Deploying -> Deployed
/// (or Cut). While open it applies a speed-squared drag opposing velocity to the
/// vehicle's Rigidbody, ramped in over the deploy time so it doesn't snap the
/// descent to a halt, and inflates a canopy visual.
/// </summary>
public class Parachute : MonoBehaviour
{
    public enum State { Stowed, Deploying, Deployed, Cut }

    [Tooltip("Current parachute state (read by the HUD).")]
    public State state = State.Stowed;

    [Header("Drag")]
    [Tooltip("Drag = dragCoeff * speed^2, opposing velocity.")]
    public float dragCoeff = 0.08f;
    [Tooltip("Upper clamp on drag force so the descent doesn't stop instantly.")]
    public float maxDragForce = 400f;
    [Tooltip("Seconds to ramp from first-open to full drag.")]
    public float deployTime = 1.5f;

    [Header("Deploy gate")]
    [Tooltip("Only allow deploy at or below this altitude (metres). Large = always.")]
    public float maxDeployAltitude = 100000f;

    [Header("Visual")]
    [Tooltip("Canopy transform (a squashed sphere) inflated when open.")]
    public Transform canopy;
    [Tooltip("Canopy scale when fully deployed.")]
    public float canopyScale = 4f;

    Rigidbody rb;
    PlanetBody planet;
    float deployT;

    public bool IsOpen => state == State.Deploying || state == State.Deployed;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
        planet = FindObjectOfType<PlanetBody>();
        if (canopy != null) canopy.localScale = Vector3.zero;
    }

    /// <summary>Deploy the chute if it is stowed and within the altitude gate.</summary>
    public void Deploy()
    {
        if (state != State.Stowed) return;
        if (planet != null)
        {
            float alt = (rb.position - planet.transform.position).magnitude - planet.radius;
            if (alt > maxDeployAltitude) return;
        }
        state = State.Deploying;
        deployT = 0f;
    }

    /// <summary>Cut the chute (no more drag, canopy hidden).</summary>
    public void Cut()
    {
        state = State.Cut;
        if (canopy != null) canopy.localScale = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (!IsOpen || rb == null) return;

        float ramp = 1f;
        if (state == State.Deploying)
        {
            deployT += Time.fixedDeltaTime;
            ramp = Mathf.Clamp01(deployT / deployTime);
            if (deployT >= deployTime) state = State.Deployed;
        }

        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed > 0.01f)
        {
            float mag = Mathf.Min(dragCoeff * speed * speed, maxDragForce) * ramp;
            rb.AddForce(-v.normalized * mag, ForceMode.Force);
        }

        if (canopy != null) canopy.localScale = Vector3.one * (canopyScale * ramp);
    }
}
