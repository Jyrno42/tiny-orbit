using UnityEngine;

/// <summary>
/// Drives an engine's exhaust particle system: emits (scaled by throttle) only while
/// this engine's stage is the active, fueled one that is actually firing. A jettisoned
/// booster's plume goes dark because its stage is no longer the controller's active stage.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class EnginePlume : MonoBehaviour
{
    public RocketController controller;
    public Stage stage;

    ParticleSystem ps;
    ParticleSystem.MinMaxCurve baseSpeed;
    float baseSize;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (controller == null) controller = GetComponentInParent<RocketController>();
        if (stage == null) stage = GetComponentInParent<Stage>();
        var main = ps.main;
        baseSpeed = main.startSpeed;
        baseSize = main.startSizeMultiplier;
        ps.Play();
    }

    void Update()
    {
        float thr = 0f;
        if (controller != null && stage != null && controller.ActiveStage == stage && stage.HasFuel)
            thr = controller.throttle;

        var em = ps.emission;
        em.rateOverTimeMultiplier = thr;                         // density tracks throttle

        var main = ps.main;
        main.startSpeedMultiplier = Mathf.Lerp(0.4f, 1f, thr);   // longer plume at higher throttle
        main.startSizeMultiplier = baseSize * Mathf.Lerp(0.7f, 1f, thr);
    }
}
