using UnityEngine;

/// <summary>
/// Throttle-driven engine exhaust plume. The entire particle system is built
/// and configured at runtime from these serialized fields (prefab-baked
/// particle module values do not survive apply/instantiate reliably), and the
/// emission rate is driven every frame from the controller throttle. Additive
/// bright core fading orange then red; emits only while this plume's stage is
/// the active, fueled stage.
/// </summary>
public class EnginePlume : MonoBehaviour
{
    [Tooltip("Particle emission rate at full throttle.")]
    [SerializeField] private float maxRate = 300f;

    [Tooltip("Nozzle exit radius the cone emits from.")]
    [SerializeField] private float nozzleRadius = 0.22f;

    [Tooltip("Exhaust particle speed range at full throttle (m/s).")]
    [SerializeField] private Vector2 speedRange = new Vector2(6f, 10f);

    [Tooltip("Particle lifetime range (s); randomized so batches do not die together.")]
    [SerializeField] private Vector2 lifetimeRange = new Vector2(0.6f, 1.1f);

    [Tooltip("Particle size multiplier.")]
    [SerializeField] private float sizeMul = 1f;

    private RocketController controller;
    private Stage stage;
    private ParticleSystem ps;
    private ParticleSystem.EmissionModule emission;
    private ParticleSystem.MainModule main;

    /// <summary>Live particle count (for tests).</summary>
    public int ParticleCount => ps != null ? ps.particleCount : 0;

    void Start()
    {
        controller = GetComponentInParent<RocketController>();
        stage = GetComponentInParent<Stage>();
        Build();
    }

    void Build()
    {
        var go = new GameObject("PlumePS");
        go.transform.SetParent(transform, false);
        // cone emits along local +Z; aim it down the nozzle (-Y of the rocket)
        go.transform.localRotation = Quaternion.LookRotation(Vector3.down);

        ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f * sizeMul, 0.55f * sizeMul);
        main.startColor = Color.white;
        main.maxParticles = 2000;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

        emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8f;
        shape.radius = nozzleRadius;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                new GradientColorKey(new Color(1f, 0.75f, 0.2f), 0.35f),
                new GradientColorKey(new Color(1f, 0.35f, 0.1f), 0.7f),
                new GradientColorKey(new Color(0.6f, 0.12f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            });
        col.color = grad;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.5f)));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        mat.mainTexture = MakeSoftDisc();
        renderer.material = mat;

        ps.Play();
    }

    static Texture2D MakeSoftDisc()
    {
        const int s = 32;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f - 0.5f, s / 2f - 0.5f)) / (s / 2f);
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (ps == null) return;
        bool active = controller != null && stage != null
            && controller.ActiveStage == stage && stage.HasFuel;
        float throttle = active ? controller.Throttle : 0f;

        emission.rateOverTime = maxRate * throttle;
        if (throttle > 0.01f)
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                speedRange.x * Mathf.Lerp(0.4f, 1f, throttle),
                speedRange.y * Mathf.Lerp(0.4f, 1f, throttle));
    }
}
