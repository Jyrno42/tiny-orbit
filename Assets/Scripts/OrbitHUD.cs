using UnityEngine;

/// <summary>
/// On-screen flight readout (IMGUI). Shows ground-clearance altitude, the
/// velocity split (signed climb rate + tangential/orbital speed), total speed,
/// throttle, apoapsis/periapsis altitudes, eccentricity, and an
/// ORBIT / SUBORBITAL / ESCAPE status derived from OrbitMath.
/// </summary>
public class OrbitHUD : MonoBehaviour
{
    public Rigidbody rb;
    public RocketController controller;

    [Header("Debug telemetry")]
    [Tooltip("Dump the same readout to the console on an interval (handy for headless verification).")]
    public bool logTelemetry = false;
    public float logInterval = 1f;

    PlanetBody planet;
    Collider[] colliders;
    GUIStyle label, header, statusStyle;
    Texture2D panelTex, barBg, barFill;
    float nextLog;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (controller == null) controller = GetComponent<RocketController>();
        planet = FindObjectOfType<PlanetBody>();
        colliders = GetComponentsInChildren<Collider>();
    }

    static Texture2D Solid(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

    void EnsureStyles()
    {
        if (label != null) return;
        int fs = Mathf.Max(16, Mathf.RoundToInt(Screen.height * 0.020f));
        label = new GUIStyle(GUI.skin.label) { fontSize = fs };
        label.normal.textColor = Color.white;
        header = new GUIStyle(label) { fontSize = Mathf.RoundToInt(fs * 1.35f), fontStyle = FontStyle.Bold };
        statusStyle = new GUIStyle(label) { fontSize = Mathf.RoundToInt(fs * 1.5f), fontStyle = FontStyle.Bold };
        panelTex = Solid(new Color(0f, 0f, 0f, 0.55f));
        barBg = Solid(new Color(1f, 1f, 1f, 0.15f));
        barFill = Solid(new Color(0.3f, 0.8f, 1f, 0.95f));
    }

    /// <summary>Ground clearance from the collider point nearest the planet centre (root sits below the engine).</summary>
    float GroundClearance(Vector3 center)
    {
        float min = float.PositiveInfinity;
        foreach (var c in colliders)
        {
            if (c == null || !c.enabled) continue;
            float d = (c.ClosestPoint(center) - center).magnitude;
            if (d < min) min = d;
        }
        if (float.IsInfinity(min)) min = (rb.position - center).magnitude;
        return min - planet.radius;
    }

    struct Readout
    {
        public float alt, speed, vSpeed, hSpeed, throttle, apAlt, peAlt, e;
        public string status;
    }

    Readout Sample()
    {
        Vector3 center = planet.transform.position;
        Vector3 r = rb.position - center;
        Vector3 v = rb.linearVelocity;
        Vector3 up = r.normalized;
        var el = OrbitMath.Compute(r, v, planet.Mu, planet.radius);
        Readout o;
        o.alt = GroundClearance(center);
        o.speed = el.speed;
        o.vSpeed = Vector3.Dot(v, up);                  // signed climb rate
        o.hSpeed = (v - up * o.vSpeed).magnitude;        // tangential / orbital
        o.throttle = controller ? controller.throttle : 0f;
        o.apAlt = el.bound ? el.ra - planet.radius : float.PositiveInfinity;
        o.peAlt = el.rp - planet.radius;
        o.e = el.e;
        o.status = !el.bound ? "ESCAPE" : (el.isOrbit ? "ORBIT" : "SUBORBITAL");
        return o;
    }

    void Update()
    {
        if (!logTelemetry || rb == null || planet == null) return;
        if (Time.time < nextLog) return;
        nextLog = Time.time + logInterval;
        var o = Sample();
        Debug.Log($"[HUD] {o.status} alt={o.alt:F1} spd={o.speed:F1} (v={o.vSpeed:F1} h={o.hSpeed:F1}) thr={o.throttle*100f:F0}% Ap={o.apAlt:F1} Pe={o.peAlt:F1} e={o.e:F3}");
    }

    static string Alt(float a) => float.IsInfinity(a) ? "---" : a.ToString("N0") + " m";

    void OnGUI()
    {
        if (rb == null || planet == null) return;
        EnsureStyles();
        var o = Sample();

        float w = 360f, x = 14f, y = 14f, pad = 14f;
        float lh = label.fontSize * 1.5f;
        float panelH = lh * 11.5f;
        GUI.DrawTexture(new Rect(x, y, w, panelH), panelTex);

        float cx = x + pad, cy = y + pad, cw = w - pad * 2f;

        Color sc = o.status == "ORBIT" ? new Color(0.4f, 1f, 0.5f)
                 : o.status == "ESCAPE" ? new Color(1f, 0.45f, 0.35f)
                 : new Color(1f, 0.85f, 0.3f);
        statusStyle.normal.textColor = sc;
        GUI.Label(new Rect(cx, cy, cw, lh * 1.4f), o.status, statusStyle);
        cy += lh * 1.7f;

        Row(ref cy, cx, cw, lh, "Altitude", Alt(o.alt));
        Row(ref cy, cx, cw, lh, "Speed", o.speed.ToString("N1") + " m/s");
        Row(ref cy, cx, cw, lh, "  vertical", o.vSpeed.ToString("N1") + " m/s");
        Row(ref cy, cx, cw, lh, "  horizontal", o.hSpeed.ToString("N1") + " m/s");
        Row(ref cy, cx, cw, lh, "Apoapsis", Alt(o.apAlt));
        Row(ref cy, cx, cw, lh, "Periapsis", Alt(o.peAlt));
        Row(ref cy, cx, cw, lh, "Eccentricity", o.e.ToString("F3"));

        // Throttle bar
        GUI.Label(new Rect(cx, cy, cw, lh), "Throttle " + (o.throttle * 100f).ToString("F0") + "%", label);
        cy += lh;
        float bx = cx, by = cy + 2f, bw = cw, bh = lh * 0.5f;
        GUI.DrawTexture(new Rect(bx, by, bw, bh), barBg);
        GUI.DrawTexture(new Rect(bx, by, bw * Mathf.Clamp01(o.throttle), bh), barFill);
    }

    void Row(ref float cy, float cx, float cw, float lh, string name, string val)
    {
        GUI.Label(new Rect(cx, cy, cw * 0.55f, lh), name, label);
        var r = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
        GUI.Label(new Rect(cx + cw * 0.45f, cy, cw * 0.55f, lh), val, r);
        cy += lh;
    }
}
