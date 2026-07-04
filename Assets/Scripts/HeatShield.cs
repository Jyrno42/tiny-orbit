using UnityEngine;

/// <summary>
/// Ablative heat shield at the base of the command pod. Cosmetic for v1 (no
/// atmosphere yet); the ablator amount is a stub a future reentry-heating
/// phase can deplete. Rides with the capsule through staging.
/// </summary>
public class HeatShield : MonoBehaviour
{
    [Tooltip("Ablator remaining; a future reentry phase burns this off.")]
    [SerializeField] private float ablator = 100f;

    /// <summary>Remaining ablator units.</summary>
    public float Ablator => ablator;
}
