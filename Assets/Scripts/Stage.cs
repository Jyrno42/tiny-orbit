using UnityEngine;

/// <summary>
/// One stage of the rocket: a fuel tank + engine grouped under a child of the
/// rocket root. The controller fires the active stage's engine, drains its
/// tank, and on jettison detaches this whole GameObject to fall away.
/// </summary>
public class Stage : MonoBehaviour
{
    [Tooltip("The tank this stage's engine draws from.")]
    public FuelTank tank;

    [Tooltip("On-axis point where thrust is applied (and exhaust emitted).")]
    public Transform thrustPoint;

    [Tooltip("Decoupler that releases this stage when it is jettisoned (optional).")]
    public StackSeparator separator;

    [Tooltip("Engine force at full throttle.")]
    public float maxThrust = 120f;

    [Tooltip("Fuel units burned per second at full throttle.")]
    public float fuelBurnPerSecond = 8f;

    [Tooltip("Structural (dry) mass of this stage, excluding fuel.")]
    public float dryMass = 1f;

    [Tooltip("Mass contributed per unit of fuel.")]
    public float fuelMassPerUnit = 0.014f;

    /// <summary>Current total mass: dry structure + remaining fuel.</summary>
    public float Mass => dryMass + (tank != null ? tank.fuel * fuelMassPerUnit : 0f);
}
