using UnityEngine;

/// <summary>
/// One propulsion stage: its fuel tank, where its engine pushes from, how hard,
/// and how fast it burns. The <see cref="RocketController"/> fires whichever stage
/// is currently active. <see cref="separator"/> (if set) is the decoupler that
/// jettisons this stage when it is spent.
/// </summary>
public class Stage : MonoBehaviour
{
    [Tooltip("Fuel tank feeding this stage's engine.")]
    public FuelTank tank;

    [Tooltip("Point the engine thrust is applied from (on the central axis).")]
    public Transform thrustPoint;

    [Tooltip("Engine thrust in Newtons at full throttle.")]
    public float maxThrust = 120f;

    [Tooltip("Fuel drawn per second at full throttle.")]
    public float fuelBurnPerSecond = 25f;

    [Tooltip("Decoupler that drops this stage. Null for the final (top) stage.")]
    public StackSeparator separator;

    /// <summary>True when this stage can still produce thrust.</summary>
    public bool HasFuel => tank != null && !tank.IsEmpty;
}
