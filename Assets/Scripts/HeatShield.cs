using UnityEngine;

/// <summary>
/// Base heat shield for the command pod. With no atmosphere yet it is mostly
/// cosmetic plus a mass value; the serialized ablator amount is a stub a future
/// reentry-heating phase can deplete. It stays attached to the capsule (it is a
/// child of the pod/root, not of any jettisoned stage).
/// </summary>
public class HeatShield : MonoBehaviour
{
    [Tooltip("Remaining ablative material. Stub for a future reentry-heating phase.")]
    public float ablator = 100f;
    public float maxAblator = 100f;

    [Tooltip("Structural mass of the shield (folded into the payload mass).")]
    public float mass = 0.3f;

    public float Fraction => maxAblator > 0f ? Mathf.Clamp01(ablator / maxAblator) : 0f;
}
