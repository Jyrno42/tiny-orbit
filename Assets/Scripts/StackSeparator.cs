using UnityEngine;

/// <summary>
/// Decoupler between two stages. Separate() splits the craft: the stack above
/// keeps the main Rigidbody and controller, the stage below gets its own
/// Rigidbody, collider and gravity, plus an equal-and-opposite impulse "pop".
/// The separator ring rides away with the discarded stage, which despawns on a
/// timer so the returning capsule cannot land on its own booster.
/// </summary>
public class StackSeparator : MonoBehaviour
{
    [Tooltip("Impulse applied to each half along the stack axis (N*s).")]
    [SerializeField] private float popImpulse = 8f;

    [Tooltip("Seconds after separation before the discarded stage despawns.")]
    [SerializeField] private float despawnAfter = 30f;

    [Tooltip("Collider given to the jettisoned booster.")]
    [SerializeField] private Vector3 boosterColliderSize = new Vector3(1.1f, 2.24f, 1.1f);
    [SerializeField] private Vector3 boosterColliderCenter = new Vector3(0f, 1.12f, 0f);

    [Tooltip("Root collider shrinks to just the remaining upper stack.")]
    [SerializeField] private Vector3 upperColliderSize = new Vector3(1.1f, 2.44f, 1.1f);
    [SerializeField] private Vector3 upperColliderCenter = new Vector3(0f, 3.48f, 0f);

    /// <summary>True once this separator has fired.</summary>
    public bool Fired { get; private set; }

    /// <summary>
    /// Detaches <paramref name="lower"/> from the stack and pops the halves
    /// apart. Returns the jettisoned GameObject (or null if already fired).
    /// </summary>
    public GameObject Separate(Stage lower)
    {
        if (Fired || lower == null) return null;
        Fired = true;

        var rootRb = GetComponentInParent<Rigidbody>();
        var rootGo = rootRb.gameObject;

        // ring caps the discarded booster
        transform.SetParent(lower.transform, true);
        lower.transform.SetParent(null, true);

        var lowerRb = lower.gameObject.AddComponent<Rigidbody>();
        lowerRb.mass = Mathf.Max(lower.CurrentMass, 0.1f);
        lowerRb.useGravity = false;
        lowerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        lowerRb.interpolation = RigidbodyInterpolation.Interpolate;
        lowerRb.linearVelocity = rootRb.GetPointVelocity(lower.transform.TransformPoint(boosterColliderCenter));
        lowerRb.angularVelocity = rootRb.angularVelocity;

        var lowerCol = lower.gameObject.AddComponent<BoxCollider>();
        lowerCol.size = boosterColliderSize;
        lowerCol.center = boosterColliderCenter;
        lower.gameObject.AddComponent<GravityReceiver>();

        var rootCol = rootGo.GetComponent<BoxCollider>();
        if (rootCol != null)
        {
            rootCol.size = upperColliderSize;
            rootCol.center = upperColliderCenter;
        }

        Vector3 axis = rootGo.transform.up;
        rootRb.AddForce(axis * popImpulse, ForceMode.Impulse);
        lowerRb.AddForce(-axis * popImpulse, ForceMode.Impulse);

        Destroy(lower.gameObject, despawnAfter);
        return lower.gameObject;
    }
}
