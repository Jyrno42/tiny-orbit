using UnityEngine;

/// <summary>
/// Groups one stage's engine data: tank, thrust, burn rate and mass model.
/// Lives on the stage's group GameObject (Stage1, Stage2) so jettisoning the
/// group carries everything with it.
/// </summary>
public class Stage : MonoBehaviour
{
    [Tooltip("Fuel tank feeding this stage's engine.")]
    public FuelTank tank;

    [Tooltip("Thrust in newtons at throttle 1.")]
    public float maxThrust = 120f;

    [Tooltip("Fuel units burned per second at throttle 1.")]
    public float fuelBurnPerSecond = 8f;

    [Tooltip("Structural mass without fuel.")]
    public float dryMass = 0.8f;

    [Tooltip("Mass of one fuel unit.")]
    public float fuelMassPerUnit = 0.05f;

    [Tooltip("Where thrust applies / exhaust appears.")]
    public Transform thrustPoint;

    /// <summary>True while the tank still holds fuel (or no tank is wired).</summary>
    public bool HasFuel => tank == null || !tank.IsEmpty;

    /// <summary>Current mass contribution: dry structure plus remaining fuel.</summary>
    public float CurrentMass => dryMass + (tank != null ? tank.Fuel * fuelMassPerUnit : 0f);
}
