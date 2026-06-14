using UnityEngine;

/// <summary>
/// A decoupler between two stages. <see cref="Separate"/> detaches the stage group
/// below this joint into its own Rigidbody so it falls away, copies the parent's
/// velocity so it doesn't lurch, and pushes the two halves apart with an impulse -
/// the visible "pop" of a real stack separator.
/// </summary>
public class StackSeparator : MonoBehaviour
{
    [Tooltip("The stage group to jettison (everything below this joint).")]
    public GameObject jettisonGroup;

    [Tooltip("Mass handed to the jettisoned group; also subtracted from the main rocket.")]
    public float jettisonMass = 3f;

    [Tooltip("Impulse pushing the two halves apart along the separation axis.")]
    public float popImpulse = 6f;

    bool fired;

    /// <summary>
    /// Split off <see cref="jettisonGroup"/>. The remaining rocket keeps
    /// <paramref name="mainRb"/>; the dropped group gets a fresh Rigidbody and
    /// gravity, and both halves are pushed apart.
    /// </summary>
    public void Separate(Rigidbody mainRb)
    {
        if (fired || jettisonGroup == null || mainRb == null) return;
        fired = true;

        Vector3 axis = mainRb.transform.up;       // separation along the rocket's long axis
        Vector3 vel = mainRb.linearVelocity;
        Vector3 angVel = mainRb.angularVelocity;

        // Detach the lower group and give it independent physics.
        jettisonGroup.transform.SetParent(null, true);
        var dropRb = jettisonGroup.GetComponent<Rigidbody>();
        if (dropRb == null) dropRb = jettisonGroup.AddComponent<Rigidbody>();
        dropRb.useGravity = false;
        dropRb.mass = jettisonMass;
        dropRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        dropRb.linearVelocity = vel;
        dropRb.angularVelocity = angVel;
        if (jettisonGroup.GetComponent<GravityReceiver>() == null)
            jettisonGroup.AddComponent<GravityReceiver>();

        // The main rocket is now lighter.
        mainRb.mass = Mathf.Max(0.1f, mainRb.mass - jettisonMass);

        // Pop apart: upper half up, dropped half down.
        mainRb.AddForce(axis * popImpulse, ForceMode.Impulse);
        dropRb.AddForce(-axis * popImpulse, ForceMode.Impulse);
    }
}
