using UnityEngine;

/// <summary>
/// A decoupler between two stages. Separate() splits the stage below off into
/// its own Rigidbody and pops both halves apart along the separation axis - the
/// visible "pop" of a real decoupler. Single-use.
/// </summary>
public class StackSeparator : MonoBehaviour
{
    [Tooltip("Impulse pushing the two halves apart on separation.")]
    public float separationImpulse = 8f;

    public bool Fired { get; private set; }

    /// <summary>
    /// Detach <paramref name="stageBelow"/> (and its children) from the rocket,
    /// give it independent physics, and push the halves apart. Returns the new
    /// Rigidbody of the jettisoned half (or null if already fired).
    /// </summary>
    public Rigidbody Separate(Rigidbody upperRb, Stage stageBelow)
    {
        if (Fired || stageBelow == null || upperRb == null) return null;
        Fired = true;

        Vector3 axis = upperRb.transform.up; // separation along the rocket's long axis
        var go = stageBelow.gameObject;
        go.transform.SetParent(null, true);

        var drb = go.AddComponent<Rigidbody>();
        drb.useGravity = false;
        drb.mass = Mathf.Max(0.1f, stageBelow.Mass);
        drb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        drb.linearVelocity = upperRb.linearVelocity;
        drb.angularVelocity = upperRb.angularVelocity;
        go.AddComponent<GravityReceiver>();

        // Pop apart: spent stage shoved back, upper given a smaller forward nudge.
        drb.AddForce(-axis * separationImpulse, ForceMode.Impulse);
        upperRb.AddForce(axis * separationImpulse * 0.3f, ForceMode.Impulse);
        return drb;
    }
}
