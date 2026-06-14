using UnityEngine;

/// <summary>
/// Throttle-driven engine exhaust plume. The entire ParticleSystem is BUILT and
/// CONFIGURED at runtime from the serialized knobs below - particle module values
/// baked into a prefab do not survive apply/instantiate on this setup (they
/// revert to a single bloom), so nothing here is serialized on the prefab except
/// the plain fields. Emission rate and plume length scale with throttle and are
/// zero unless this plume's stage is the active, fuelled, firing stage.
/// Additive particles: a bright yellow-white core fading orange then red.
/// </summary>
public class EnginePlume : MonoBehaviour
{
    [Tooltip("Controller that tells us throttle + which stage is active.")]
    public RocketController controller;
    [Tooltip("The stage this plume belongs to; it only fires when this stage is active.")]
    public Stage stage;

    [Header("Plume knobs")]
    public float maxRate = 300f;
    public float nozzleRadius = 0.25f;
    public Vector2 speedRange = new Vector2(6f, 13f);
    public Vector2 lifetimeRange = new Vector2(0.6f, 1.1f);
    public float sizeMul = 1f;
    [Tooltip("Cone emission direction as local euler; (90,0,0) points it out the nozzle (-Y).")]
    public Vector3 emitEuler = new Vector3(90f, 0f, 0f);

    ParticleSystem ps;
    ParticleSystem.EmissionModule emission;
    ParticleSystem.MainModule main;

    void Awake()
    {
        Build();
        if (controller == null) controller = GetComponentInParent<RocketController>();
        if (stage == null) stage = GetComponentInParent<Stage>();
    }

    void Build()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f * sizeMul, 1.05f * sizeMul);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // leave a trail as the rocket moves
        main.maxParticles = 2000;
        main.gravityModifier = 0f;
        main.playOnAwake = false;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate; // do not pause off-screen

        emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 7f;
        shape.radius = nozzleRadius;
        shape.rotation = emitEuler; // aim it out the nozzle

        // Bright core fading orange then red, alpha in then out.
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] {
                new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.5f),
                new GradientColorKey(new Color(0.85f, 0.12f, 0.08f), 1f),
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.18f),
                new GradientAlphaKey(0.75f, 0.6f),
                new GradientAlphaKey(0f, 1f),
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Taper the plume along its life.
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.2f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Additive renderer with a soft round particle texture (built at runtime).
        var r = GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        mat.mainTexture = SoftDot(64);
        r.material = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.alignment = ParticleSystemRenderSpace.View;

        ps.Play();
    }

    // Soft radial alpha sprite for a flame puff.
    static Texture2D SoftDot(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float c = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a; // softer falloff
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }

    void LateUpdate()
    {
        if (ps == null) return;
        bool firing = controller != null && stage != null
                      && controller.ActiveStage == stage
                      && stage.tank != null && !stage.tank.IsEmpty
                      && controller.throttle > 0.01f;
        float t = firing ? controller.throttle : 0f;
        emission.rateOverTime = maxRate * t;
        // Longer, faster plume at higher throttle.
        main.startSpeedMultiplier = Mathf.Lerp(0.5f, 1f, t);
        main.startLifetimeMultiplier = Mathf.Lerp(0.6f, 1f, t);
    }
}
