using UnityEngine;

/// <summary>
/// Builds and drives an engine exhaust plume entirely at runtime, from plain
/// serialized fields - so it doesn't depend on the ParticleSystem's module values
/// surviving prefab serialization (which they don't, reliably). It owns the
/// emission rate each frame: a dense, continuous flame scaled by throttle, lit only
/// while this engine's stage is the active, fueled, firing one.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class EnginePlume : MonoBehaviour
{
    public RocketController controller;
    public Stage stage;

    [Header("Plume look")]
    [Tooltip("Particles per second at full throttle.")]
    public float maxRate = 260f;
    [Tooltip("Engine nozzle radius - scales the flame width.")]
    public float nozzleRadius = 0.5f;
    public Vector2 speedRange = new Vector2(3f, 7f);
    public Vector2 lifetimeRange = new Vector2(0.6f, 1.0f);
    [Tooltip("Particle size = nozzleRadius * (these), min/max.")]
    public Vector2 sizeMul = new Vector2(1.1f, 2.0f);

    ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (controller == null) controller = GetComponentInParent<RocketController>();
        if (stage == null) stage = GetComponentInParent<Stage>();
        Configure();
        ps.Play();
    }

    void Configure()
    {
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(nozzleRadius * sizeMul.x, nozzleRadius * sizeMul.y);
        main.startColor = new Color(1f, 0.9f, 0.6f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.gravityModifier = 0f;
        main.maxParticles = 1500;

        var em = ps.emission; em.enabled = true; em.rateOverTime = 0f; // driven in Update
        var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 9f; sh.radius = nozzleRadius * 0.45f;

        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(1f,1f,0.9f),0f), new GradientColorKey(new Color(1f,0.7f,0.25f),0.35f), new GradientColorKey(new Color(0.95f,0.3f,0.1f),0.75f), new GradientColorKey(new Color(0.4f,0.1f,0.07f),1f) },
            new[] { new GradientAlphaKey(0.85f,0f), new GradientAlphaKey(0.9f,0.3f), new GradientAlphaKey(0.5f,0.7f), new GradientAlphaKey(0f,1f) });
        col.color = g;

        var sol = ps.sizeOverLifetime; sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f,0.7f), new Keyframe(0.4f,1f), new Keyframe(1f,1.3f)));
    }

    void Update()
    {
        float thr = 0f;
        if (controller != null && stage != null && controller.ActiveStage == stage && stage.HasFuel)
            thr = controller.throttle;

        var em = ps.emission;
        em.rateOverTime = maxRate * thr; // own the rate at runtime - the dense, continuous source
    }
}
