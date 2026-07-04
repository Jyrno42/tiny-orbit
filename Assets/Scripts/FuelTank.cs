using UnityEngine;

/// <summary>Simple fuel store for one stage.</summary>
public class FuelTank : MonoBehaviour
{
    [SerializeField] private float maxFuel = 100f;
    [SerializeField] private float fuel = 100f;

    /// <summary>Remaining fuel units.</summary>
    public float Fuel => fuel;

    /// <summary>Tank capacity in units.</summary>
    public float MaxFuel => maxFuel;

    /// <summary>Remaining fraction in [0, 1].</summary>
    public float Fraction => maxFuel > 0f ? fuel / maxFuel : 0f;

    /// <summary>True when no fuel remains.</summary>
    public bool IsEmpty => fuel <= 0f;

    /// <summary>Draws up to <paramref name="amount"/> and returns how much was actually drawn.</summary>
    public float Consume(float amount)
    {
        float drawn = Mathf.Min(Mathf.Max(amount, 0f), fuel);
        fuel -= drawn;
        return drawn;
    }

    /// <summary>Refills to capacity (used by launch reset).</summary>
    public void Refill() => fuel = maxFuel;
}
