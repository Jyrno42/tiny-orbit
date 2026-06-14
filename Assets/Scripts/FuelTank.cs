using UnityEngine;

/// <summary>
/// A minimal fuel store for one stage. Engines call <see cref="Consume"/> each
/// physics step; when it runs dry <see cref="IsEmpty"/> is true and the engine
/// flames out.
/// </summary>
public class FuelTank : MonoBehaviour
{
    [Tooltip("Maximum fuel this tank can hold (arbitrary units).")]
    public float maxFuel = 100f;

    [Tooltip("Current fuel remaining.")]
    public float fuel = 100f;

    /// <summary>True once the tank has no fuel left.</summary>
    public bool IsEmpty => fuel <= 0f;

    /// <summary>Fraction of capacity remaining, 0..1.</summary>
    public float Fraction => maxFuel > 0f ? Mathf.Clamp01(fuel / maxFuel) : 0f;

    /// <summary>
    /// Draw up to <paramref name="amount"/> fuel; returns how much was actually
    /// available and removed.
    /// </summary>
    public float Consume(float amount)
    {
        float drawn = Mathf.Min(amount, fuel);
        fuel -= drawn;
        if (fuel < 0f) fuel = 0f;
        return drawn;
    }
}
