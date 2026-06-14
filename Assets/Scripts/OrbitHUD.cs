using UnityEngine;

/// <summary>
/// Flight + orbit telemetry HUD: altitude, total/horizontal/vertical speed,
/// throttle and fuel bars, stage, parachute state, live apoapsis/periapsis and
/// eccentricity, a colour-coded ORBIT/SUBORBITAL/ESCAPE status, and clickable
/// Autopilot and Stage buttons. Uses OnGUI so it needs no Canvas. Refs auto-found.
/// </summary>
public class OrbitHUD : MonoBehaviour
{
    [Tooltip("The planet (centre, mu, radius). Auto-found if unset.")]
    public PlanetBody planet;
    [Tooltip("The rocket's Rigidbody. Auto-found if unset.")]
    public Rigidbody rocket;
    [Tooltip("Flight controller (throttle, stages). Auto-found if unset.")]
    public RocketController controller;
    [Tooltip("Parachute (chute state). Auto-found if unset.")]
    public Parachute parachute;
    [Tooltip("Autopilot (play button). Auto-found if unset.")]
    public AutoPilot autopilot;

    GUIStyle title, label, value, status, btn;
    Texture2D px;
    float groundClearance; // altitude of the lowest part above the surface (cached each frame)

    void Awake()
    {
        if (planet == null) planet = FindObjectOfType<PlanetBody>();
        if (controller == null) controller = FindObjectOfType<RocketController>();
        if (rocket == null && controller != null) rocket = controller.GetComponent<Rigidbody>();
        if (parachute == null) parachute = FindObjectOfType<Parachute>();
        if (autopilot == null) autopilot = FindObjectOfType<AutoPilot>();

        px = new Texture2D(1, 1);
        px.SetPixel(0, 0, Color.white);
        px.Apply();
    }

    // Recompute the true ground clearance once per frame (cheap enough; OnGUI runs twice a frame).
    void Update()
    {
        if (rocket == null || planet == null) return;
        Vector3 centre = planet.transform.position;
        float min = (rocket.position - centre).magnitude; // fallback if no colliders
        foreach (var c in rocket.GetComponentsInChildren<Collider>())
        {
            if (!c.enabled) continue;
            float d = (c.ClosestPoint(centre) - centre).magnitude; // nearest point on this part to the planet centre
            if (d < min) min = d;
        }
        groundClearance = min - planet.radius;
    }

    void EnsureStyles()
    {
        if (label != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, richText = true };
        title.normal.textColor = new Color(0.7f, 0.85f, 1f);
        label = new GUIStyle(GUI.skin.label) { fontSize = 17 };
        label.normal.textColor = new Color(0.78f, 0.81f, 0.85f);
        value = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
        value.normal.textColor = Color.white;
        status = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        btn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
    }

    void OnGUI()
    {
        if (planet == null || rocket == null) return;
        EnsureStyles();

        Vector3 r = rocket.position - planet.transform.position;
        Vector3 v = rocket.linearVelocity;
        Vector3 up = r.normalized;
        float vVert = Vector3.Dot(v, up);                  // radial (vertical), + = climbing
        float vHorz = (v - up * vVert).magnitude;          // tangential (horizontal)
        var o = OrbitMath.Compute(r, v, planet.Mu, planet.radius);
        float R = planet.radius;
        float throttle = controller != null ? controller.throttle : 0f;

        // Surface altitude from whatever part is actually lowest (works at any attitude - upside
        // down, sideways, tumbling). Reads ~0 on the ground and goes negative below the surface.
        float surfaceAlt = groundClearance;

        const float W = 320f;
        GUILayout.BeginArea(new Rect(16, 16, W, 620), GUI.skin.box);
        GUILayout.Label("TINY ORBIT", title);
        Sep();

        Row("Altitude", $"{surfaceAlt:N0} m");
        Row("Speed", $"{o.speed:N1} m/s");
        Row("  Vertical", $"{vVert:+0.0;-0.0;0.0} m/s");
        Row("  Horizontal", $"{vHorz:N1} m/s");

        GUILayout.Space(4);
        Row("Throttle", $"{throttle * 100f:N0} %");
        Bar(throttle, new Color(0.95f, 0.75f, 0.2f));

        if (controller != null && controller.StageCount > 0)
        {
            GUILayout.Space(4);
            Row("Stage", $"{controller.ActiveStageNumber} / {controller.StageCount}");
            Row("Fuel", $"{controller.ActiveFuelFraction * 100f:N0} %");
            Bar(controller.ActiveFuelFraction, new Color(0.3f, 0.8f, 1f));
        }
        if (parachute != null) Row("Chute", parachute.state.ToString());

        Sep();
        Row("Apoapsis", FmtAlt(o.ra, R, o.bound));
        Row("Periapsis", FmtAlt(o.rp, R, true));
        Row("Eccentricity", $"{o.e:N3}");
        Sep();

        string st; Color c;
        if (!o.bound) { st = "ESCAPE"; c = new Color(1f, 0.55f, 0.1f); }
        else if (o.isOrbit) { st = "ORBIT"; c = new Color(0.25f, 1f, 0.4f); }
        else { st = "SUBORBITAL"; c = new Color(1f, 0.85f, 0.2f); }
        status.normal.textColor = c;
        GUILayout.Label(st, status);

        GUILayout.Space(4);
        if (autopilot != null)
        {
            bool on = autopilot.engaged;
            GUI.backgroundColor = on ? new Color(0.8f, 0.3f, 0.3f) : new Color(0.3f, 0.7f, 0.4f);
            string apLabel = on ? $"■  AUTOPILOT  ({autopilot.phase})" : "▶  AUTOPILOT";
            if (GUILayout.Button(apLabel, btn, GUILayout.Height(30)))
            {
                if (on) autopilot.Disengage(); else autopilot.Engage();
            }
            GUI.backgroundColor = Color.white;
        }
        if (controller != null && controller.ActiveStage != null && controller.ActiveStage.separator != null)
        {
            if (GUILayout.Button("STAGE  ⤓", btn, GUILayout.Height(26))) controller.Jettison();
        }

        GUILayout.EndArea();

        // Time-warp indicator, centred near the top (KSP-style) - only when warping.
        float ts = Time.timeScale;
        if (ts > 1.05f)
        {
            string w = $"▶▶  {ts:0}×  TIME WARP";
            var ws = new GUIStyle(status) { fontSize = 22 };
            ws.normal.textColor = new Color(0.45f, 0.9f, 1f);
            Vector2 sz = ws.CalcSize(new GUIContent(w));
            GUI.Label(new Rect((Screen.width - sz.x) * 0.5f, 18f, sz.x + 6f, sz.y), w, ws);
        }
    }

    void Row(string name, string val)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(name, label);
        GUILayout.FlexibleSpace();
        GUILayout.Label(val, value);
        GUILayout.EndHorizontal();
    }

    void Sep()
    {
        GUILayout.Space(3);
        Rect rt = GUILayoutUtility.GetRect(10, 1);
        Color old = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.18f);
        GUI.DrawTexture(rt, px); GUI.color = old;
        GUILayout.Space(3);
    }

    void Bar(float frac, Color fill)
    {
        Rect rt = GUILayoutUtility.GetRect(10, 12);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.4f); GUI.DrawTexture(rt, px);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(rt.x, rt.y, rt.width * Mathf.Clamp01(frac), rt.height), px);
        GUI.color = old;
    }

    static string FmtAlt(float radiusValue, float R, bool meaningful)
    {
        if (!meaningful || float.IsInfinity(radiusValue) || float.IsNaN(radiusValue)) return "--";
        return $"{(radiusValue - R):N0} m";
    }
}
