using UnityEngine;

/// <summary>
/// One-shot in-game test driver (attach at runtime): flies a full-throttle
/// hop, deploys the parachute on the way down below a set altitude, and logs
/// one line per second so the descent can be verified from the console after
/// the run. MCP calls are too slow to drive this from outside the game.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ChuteDropTest : MonoBehaviour
{
    [SerializeField] private float deployBelowAlt = 80f;

    private RocketController rc;
    private Parachute chute;
    private Rigidbody rb;
    private PlanetBody planet;
    private bool deployed;
    private float nextLog;
    private float landedAt = -1f;

    void Start()
    {
        rc = GetComponent<RocketController>();
        chute = GetComponentInChildren<Parachute>();
        rb = GetComponent<Rigidbody>();
        planet = Object.FindFirstObjectByType<PlanetBody>();
        rc.AutopilotOverride = true;
        rc.Throttle = 1f;
        Debug.Log("[ChuteDropTest] hop started");
    }

    void FixedUpdate()
    {
        if (chute == null || planet == null) return;
        float alt = chute.GroundClearance();
        float vUp = Vector3.Dot(rb.linearVelocity, (rb.position - planet.transform.position).normalized);

        if (!deployed && vUp < -1f && alt <= deployBelowAlt)
        {
            chute.Deploy();
            deployed = true;
            Debug.Log($"[ChuteDropTest] DEPLOY t={Time.time:F1} alt={alt:F1} vUp={vUp:F1}");
        }
        if (Time.time >= nextLog)
        {
            nextLog = Time.time + 1f;
            Debug.Log($"[ChuteDropTest] t={Time.time:F1} alt={alt:F1} vUp={vUp:F1} chute={chute.CurrentState} speed={rb.linearVelocity.magnitude:F2}");
        }
        if (deployed && landedAt < 0f && alt < 0.4f && rb.linearVelocity.magnitude < 0.5f)
        {
            landedAt = Time.time;
            Debug.Log($"[ChuteDropTest] LANDED t={Time.time:F1} chute={chute.CurrentState} restAlt={alt:F2}");
        }
    }
}
