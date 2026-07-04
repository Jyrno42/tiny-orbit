using UnityEngine;

/// <summary>
/// Recovery parachute on the command pod. States: Stowed -> Deploying ->
/// Deployed -> Cut. P deploys (only below a gate altitude); drag ramps in over
/// the deploy time, opposes velocity, scales with speed squared and is clamped
/// so it can never reverse motion in a single step. The canopy visual scales
/// up with deploy progress and collapses on touchdown, where the chute cuts
/// automatically so the landed craft does not appear to float.
/// </summary>
public class Parachute : MonoBehaviour
{
    public enum State { Stowed, Deploying, Deployed, Cut }

    [Tooltip("Drag force per (m/s)^2 at full deployment.")]
    [SerializeField] private float dragCoeff = 3.5f;

    [Tooltip("Upper clamp on drag force (N) so deployment does not snap the craft.")]
    [SerializeField] private float maxDragForce = 140f;

    [Tooltip("Seconds to ramp from open command to full drag.")]
    [SerializeField] private float deployTime = 1.5f;

    [Tooltip("P key only deploys below this ground clearance (m).")]
    [SerializeField] private float deployMaxAltitude = 300f;

    [Tooltip("Canopy visual scaled with deploy progress.")]
    [SerializeField] private Transform canopy;

    [SerializeField] private Vector3 canopyFullScale = new Vector3(4f, 2.2f, 4f);

    [Tooltip("Ground clearance below which a slow craft counts as landed.")]
    [SerializeField] private float touchdownClearance = 0.35f;

    /// <summary>Current chute state.</summary>
    public State CurrentState { get; private set; } = State.Stowed;

    /// <summary>Deployment fraction in [0, 1].</summary>
    public float DeployProgress { get; private set; }

    private Rigidbody rb;
    private PlanetBody planet;
    private Collider[] colliders;
    private float collidersRefreshedAt = -99f;

    void Awake()
    {
        rb = GetComponentInParent<Rigidbody>();
        planet = Object.FindFirstObjectByType<PlanetBody>();
        if (canopy != null)
            canopy.localScale = Vector3.zero;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && CurrentState == State.Stowed
            && GroundClearance() <= deployMaxAltitude)
            Deploy();

        if (CurrentState == State.Deploying)
        {
            DeployProgress = Mathf.Min(1f, DeployProgress + Time.deltaTime / deployTime);
            if (DeployProgress >= 1f)
                CurrentState = State.Deployed;
        }

        if (canopy != null && CurrentState != State.Cut)
            canopy.localScale = canopyFullScale * DeployProgress;

        bool open = CurrentState == State.Deploying || CurrentState == State.Deployed;
        if (open && GroundClearance() <= touchdownClearance && rb.linearVelocity.magnitude < 2f)
            Cut();
    }

    void FixedUpdate()
    {
        if (rb == null || DeployProgress <= 0f || CurrentState == State.Cut)
            return;
        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed < 0.5f)
            return;
        float mag = dragCoeff * speed * speed * DeployProgress;
        mag = Mathf.Min(mag, maxDragForce);
        mag = Mathf.Min(mag, 0.9f * speed * rb.mass / Time.fixedDeltaTime);
        rb.AddForce(-v.normalized * mag);
    }

    /// <summary>Opens the chute (no altitude gate; the gate applies to the P key only).</summary>
    public void Deploy()
    {
        if (CurrentState == State.Stowed)
            CurrentState = State.Deploying;
    }

    /// <summary>Cuts the chute and collapses the canopy.</summary>
    public void Cut()
    {
        if (CurrentState == State.Cut)
            return;
        CurrentState = State.Cut;
        DeployProgress = 0f;
        if (canopy != null)
            canopy.localScale = Vector3.zero;
    }

    /// <summary>Re-stows the chute (used by the launch reset).</summary>
    public void Rearm()
    {
        CurrentState = State.Stowed;
        DeployProgress = 0f;
        if (canopy != null)
            canopy.localScale = Vector3.zero;
    }

    /// <summary>Ground clearance from the craft's lowest collider point (m).</summary>
    public float GroundClearance()
    {
        if (planet == null || rb == null)
            return float.MaxValue;
        if (Time.time - collidersRefreshedAt > 1f)
        {
            colliders = rb.GetComponentsInChildren<Collider>();
            collidersRefreshedAt = Time.time;
        }
        Vector3 center = planet.transform.position;
        float nearest = float.MaxValue;
        if (colliders != null)
            foreach (var col in colliders)
            {
                if (col == null || !col.enabled) continue;
                nearest = Mathf.Min(nearest, (col.ClosestPoint(center) - center).magnitude);
            }
        if (nearest == float.MaxValue)
            nearest = (rb.position - center).magnitude;
        return nearest - planet.Radius;
    }
}
