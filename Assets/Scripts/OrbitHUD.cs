using UnityEngine;

/// <summary>
/// IMGUI flight readout: ground clearance, vertical/horizontal speed split,
/// throttle, apoapsis/periapsis altitudes, eccentricity and a bold
/// ORBIT / SUBORBITAL / ESCAPE status. Altitude is measured from the craft's
/// lowest collider point (correct at any attitude), not the root transform.
/// Later phases extend the panel with fuel, stage and parachute info.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class OrbitHUD : MonoBehaviour
{
    [SerializeField] private PlanetBody planet;

    private Rigidbody rb;
    private RocketController controller;
    private Collider[] craftColliders;

    private GUIStyle headStyle, lineStyle, statusStyle;
    private Texture2D barTex;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<RocketController>();
        if (planet == null)
            planet = Object.FindFirstObjectByType<PlanetBody>();
        RefreshColliders();
    }

    /// <summary>Re-scans the craft's colliders (call after staging).</summary>
    public void RefreshColliders()
    {
        craftColliders = GetComponentsInChildren<Collider>();
    }

    /// <summary>Ground clearance from the craft's lowest collider point (m). Negative when below the surface.</summary>
    public float GroundClearance()
    {
        Vector3 center = planet.transform.position;
        float nearest = float.MaxValue;
        if (craftColliders != null)
            foreach (var col in craftColliders)
            {
                if (col == null || !col.enabled) continue;
                Vector3 p = col.ClosestPoint(center);
                nearest = Mathf.Min(nearest, (p - center).magnitude);
            }
        if (nearest == float.MaxValue)
            nearest = (rb.position - center).magnitude;
        return nearest - planet.Radius;
    }

    void EnsureStyles()
    {
        if (headStyle != null) return;
        headStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
        lineStyle = new GUIStyle(GUI.skin.label) { fontSize = 20 };
        statusStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
        barTex = Texture2D.whiteTexture;
    }

    void DrawBar(float frac, Color color)
    {
        Rect r = GUILayoutUtility.GetRect(300, 14);
        r.width = 300;
        Color old = GUI.color;
        GUI.color = new Color(1, 1, 1, 0.15f);
        GUI.DrawTexture(r, barTex);
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), barTex);
        GUI.color = old;
    }

    void OnGUI()
    {
        if (rb == null || planet == null) return;
        EnsureStyles();

        Vector3 center = planet.transform.position;
        Vector3 rVec = rb.position - center;
        Vector3 vVec = rb.linearVelocity;
        var el = OrbitMath.FromState(rVec, vVec, planet.Mu);
        float R = planet.Radius;

        Vector3 radial = rVec.normalized;
        float vSpeed = Vector3.Dot(vVec, radial);
        float hSpeed = (vVec - radial * vSpeed).magnitude;

        GUILayout.BeginArea(new Rect(12, 12, 340, 560), GUI.skin.box);
        GUILayout.Label("TINY ORBIT", headStyle);
        GUILayout.Label($"Altitude   {GroundClearance():F0} m", lineStyle);
        GUILayout.Label($"Vertical   {vSpeed:+0.0;-0.0} m/s", lineStyle);
        GUILayout.Label($"Horizontal {hSpeed:F1} m/s", lineStyle);
        GUILayout.Label($"Speed      {el.speed:F1} m/s", lineStyle);
        GUILayout.Space(6);

        float throttle = controller != null ? controller.Throttle : 0f;
        GUILayout.Label($"Throttle   {throttle * 100f:F0}%", lineStyle);
        DrawBar(throttle, new Color(1f, 0.55f, 0.1f));
        GUILayout.Space(6);

        string apText = el.isBound ? $"{el.apoapsisRadius - R:F0} m" : "-";
        string peText = float.IsInfinity(el.periapsisRadius) ? "-" : $"{el.periapsisRadius - R:F0} m";
        GUILayout.Label($"Apoapsis   {apText}", lineStyle);
        GUILayout.Label($"Periapsis  {peText}", lineStyle);
        GUILayout.Label($"Eccentric. {el.eccentricity:F3}", lineStyle);
        GUILayout.Space(8);

        string status;
        Color statusColor;
        if (!el.isBound) { status = "ESCAPE"; statusColor = new Color(1f, 0.3f, 0.2f); }
        else if (el.periapsisRadius - R > 0f) { status = "ORBIT"; statusColor = new Color(0.3f, 1f, 0.4f); }
        else { status = "SUBORBITAL"; statusColor = new Color(1f, 0.85f, 0.2f); }
        Color oldColor = GUI.contentColor;
        GUI.contentColor = statusColor;
        GUILayout.Label(status, statusStyle);
        GUI.contentColor = oldColor;

        GUILayout.EndArea();
    }
}
