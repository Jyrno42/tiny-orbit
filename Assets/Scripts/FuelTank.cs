using UnityEngine;

/// <summary>
/// A minimal fuel store. Engines draw from it via Consume(); when empty the
/// engine flames out. Fuel is an abstract unit; its contribution to mass is
/// handled by the owning Stage.
/// </summary>
public class FuelTank : MonoBehaviour
{
    public float maxFuel = 100f;
    public float fuel = 100f;

    public bool IsEmpty => fuel <= 0f;

    /// <summary>0..1 fill fraction, for HUD bars.</summary>
    public float Fraction => maxFuel > 0f ? Mathf.Clamp01(fuel / maxFuel) : 0f;

    /// <summary>Draw up to <paramref name="amount"/> units; returns how much was actually available.</summary>
    public float Consume(float amount)
    {
        float drawn = Mathf.Min(amount, fuel);
        fuel -= drawn;
        if (fuel < 0f) fuel = 0f;
        return drawn;
    }

    public void Refill() => fuel = maxFuel;
}
