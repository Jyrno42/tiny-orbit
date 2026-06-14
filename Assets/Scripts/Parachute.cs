using UnityEngine;

/// <summary>
/// Command-pod parachute. State machine: Stowed -> Deploying -> Deployed (or
/// Cut). Press P to deploy. While open it applies an upward drag opposing
/// velocity that scales with speed^2, clamped so it never snaps the descent to a
/// halt, and ramped in over the Deploying window. The canopy (a squashed sphere)
/// scales up as it inflates and collapses on cut/touchdown.
/// </summary>
public class Parachute : MonoBehaviour
{
    public enum ChuteState { Stowed, Deploying, Deployed, Cut }
    public ChuteState state = ChuteState.Stowed;

    public Rigidbody rb;
    [Tooltip("Canopy visual; scaled from ~0 up to its design size as it inflates.")]
    public Transform canopy;

    [Header("Drag")]
    public float dragCoeff = 2.5f;       // sets terminal speed: v_t = sqrt(m*g/dragCoeff)
    public float maxDragForce = 45f;     // clamp so a fast fall doesn't stop instantly
    public float deployTime = 1.5f;      // ramp-in duration

    [Header("Deploy gating")]
    public float deployMaxAltitude = 100000f; // allow deploy below this altitude
    public float touchdownClearance = 0.6f;   // cut the chute when this close to the ground

    PlanetBody planet;
    float deployT;
    Vector3 canopyScale;

    void Awake()
    {
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
        planet = FindObjectOfType<PlanetBody>();
        if (canopy != null)
        {
            canopyScale = canopy.localScale;
            canopy.localScale = Vector3.zero;
            canopy.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) Deploy();
    }

    public void Deploy()
    {
        if (state != ChuteState.Stowed) return;
        if (GroundClearance() > deployMaxAltitude) return;
        state = ChuteState.Deploying;
        deployT = 0f;
        if (canopy != null) canopy.gameObject.SetActive(true);
    }

    public void Cut()
    {
        state = ChuteState.Cut;
        if (canopy != null) { canopy.localScale = Vector3.zero; canopy.gameObject.SetActive(false); }
    }

    /// <summary>0..1 inflation amount.</summary>
    public float DeployFraction =>
        state == ChuteState.Deployed ? 1f :
        state == ChuteState.Deploying ? Mathf.Clamp01(deployT / deployTime) : 0f;

    public string StateName => state.ToString();

    void FixedUpdate()
    {
        if (rb == null || planet == null) return;
        if (state == ChuteState.Deploying)
        {
            deployT += Time.fixedDeltaTime;
            if (deployT >= deployTime) state = ChuteState.Deployed;
        }
        if (state != ChuteState.Deploying && state != ChuteState.Deployed) return;

        float ramp = DeployFraction;
        Vector3 v = rb.linearVelocity;
        float spd = v.magnitude;
        if (spd > 0.01f)
        {
            float mag = Mathf.Min(dragCoeff * spd * spd, maxDragForce) * ramp;
            rb.AddForce(-v.normalized * mag);
        }
        if (canopy != null) canopy.localScale = canopyScale * Mathf.Lerp(0.15f, 1f, ramp);

        // Collapse on touchdown so the craft doesn't read as floating under an open chute.
        if (GroundClearance() < touchdownClearance && spd < 8f) Cut();
    }

    // Ground clearance from the lowest collider point (root sits below the engine after staging).
    float GroundClearance()
    {
        Vector3 c = planet.transform.position;
        float min = float.PositiveInfinity;
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            if (col == null || !col.enabled) continue;
            float d = (col.ClosestPoint(c) - c).magnitude;
            if (d < min) min = d;
        }
        if (float.IsInfinity(min)) min = (rb.position - c).magnitude;
        return min - planet.radius;
    }
}
