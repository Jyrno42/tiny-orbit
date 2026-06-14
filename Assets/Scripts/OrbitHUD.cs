using UnityEngine;

/// <summary>
/// On-screen readout of the rocket's flight and orbit state: altitude, speed,
/// throttle, live apoapsis/periapsis altitudes, and a bold ORBIT / SUBORBITAL /
/// ESCAPE status. Uses OnGUI so it needs no Canvas setup. References are auto-found.
/// </summary>
public class OrbitHUD : MonoBehaviour
{
    [Tooltip("The planet (for centre, mu, and radius). Auto-found if unset.")]
    public PlanetBody planet;

    [Tooltip("The rocket's Rigidbody (for position + velocity). Auto-found if unset.")]
    public Rigidbody rocket;

    [Tooltip("Optional throttle source for the throttle readout.")]
    public RocketController controller;

    GUIStyle label, status;

    void Awake()
    {
        if (planet == null) planet = FindObjectOfType<PlanetBody>();
        if (rocket == null)
        {
            var rc = FindObjectOfType<RocketController>();
            if (rc != null) { rocket = rc.GetComponent<Rigidbody>(); controller = rc; }
        }
        if (controller == null && rocket != null) controller = rocket.GetComponent<RocketController>();
    }

    void OnGUI()
    {
        if (planet == null || rocket == null) return;

        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true };
            label.normal.textColor = Color.white;
            status = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
        }

        Vector3 r = rocket.position - planet.transform.position;
        Vector3 v = rocket.linearVelocity;
        var o = OrbitMath.Compute(r, v, planet.Mu, planet.radius);

        float R = planet.radius;
        float throttle = controller != null ? controller.throttle : 0f;

        GUILayout.BeginArea(new Rect(16, 16, 340, 260), GUI.skin.box);
        GUILayout.Label($"<b>Altitude</b>   {(o.radius - R):N0} m", label);
        GUILayout.Label($"<b>Speed</b>      {o.speed:N1} m/s", label);
        GUILayout.Label($"<b>Throttle</b>   {(throttle * 100f):N0} %", label);
        GUILayout.Space(6);
        GUILayout.Label($"<b>Apoapsis</b>   {FmtAlt(o.ra, R, o.bound)}", label);
        GUILayout.Label($"<b>Periapsis</b>  {FmtAlt(o.rp, R, true)}", label);
        GUILayout.Label($"<b>Eccentricity</b> {o.e:N3}", label);
        GUILayout.Space(8);

        string text; Color c;
        if (!o.bound) { text = "ESCAPE"; c = new Color(1f, 0.55f, 0.1f); }
        else if (o.isOrbit) { text = "ORBIT"; c = new Color(0.2f, 1f, 0.3f); }
        else { text = "SUBORBITAL"; c = new Color(1f, 0.85f, 0.2f); }
        status.normal.textColor = c;
        GUILayout.Label(text, status);
        GUILayout.EndArea();
    }

    // Show an altitude (radius - R). "--" when the value is not meaningful (e.g. apoapsis on escape).
    static string FmtAlt(float radiusValue, float R, bool meaningful)
    {
        if (!meaningful || float.IsInfinity(radiusValue) || float.IsNaN(radiusValue))
            return "--";
        return $"{(radiusValue - R):N0} m";
    }
}
