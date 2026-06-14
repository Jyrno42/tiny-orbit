using UnityEngine;

/// <summary>
/// Base heat shield for the command pod. With no atmosphere yet this is mostly
/// cosmetic plus a mass value; <see cref="ablator"/> is a stub a future
/// reentry-heating phase can deplete. It stays attached to the pod through staging.
/// </summary>
public class HeatShield : MonoBehaviour
{
    [Tooltip("Ablator remaining. Stub for future reentry heating; not consumed yet.")]
    public float ablator = 100f;

    [Tooltip("Mass contribution of the shield (informational for now).")]
    public float mass = 0.3f;
}
