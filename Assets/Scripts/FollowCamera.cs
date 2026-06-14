using UnityEngine;

/// <summary>
/// Smooth third-person follow camera. Trails the target at a zoomable distance and
/// keeps it centred. Scroll wheel zooms (proportional, so it feels the same at any
/// altitude); hold right mouse to orbit. Runs in LateUpdate so it reacts to the
/// rocket's post-physics position. Camera "up" stays world-up for v1.
/// </summary>
public class FollowCamera : MonoBehaviour
{
    [Tooltip("Transform to follow (the Rocket). Auto-found by name if unset.")]
    public Transform target;

    [Header("Distance / zoom")]
    public float distance = 25f;
    public float minDistance = 8f;
    public float maxDistance = 400f;
    [Tooltip("Zoom responsiveness per scroll notch.")]
    public float zoomStrength = 3f;

    [Header("Smoothing")]
    [Tooltip("Position smoothing time for the follow (smaller = snappier).")]
    public float followSmoothTime = 0.2f;

    [Header("Orbit (hold right mouse)")]
    public float orbitSpeed = 3f;
    public float yaw = 0f;
    public float pitch = 15f;

    Vector3 velocity; // SmoothDamp state

    void Start()
    {
        if (target == null)
        {
            GameObject rk = GameObject.Find("Rocket");
            if (rk != null) target = rk.transform;
        }
        if (target != null) transform.position = DesiredPosition(); // snap, no startup glide
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Scroll zoom (proportional -> consistent feel across the altitude range)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            distance = Mathf.Clamp(distance * Mathf.Exp(-scroll * zoomStrength), minDistance, maxDistance);

        // Right-mouse orbit
        if (Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * orbitSpeed;
            pitch -= Input.GetAxis("Mouse Y") * orbitSpeed;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
        }

        Vector3 desired = DesiredPosition();
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, followSmoothTime);
        transform.LookAt(target.position, Vector3.up);
    }

    Vector3 DesiredPosition()
    {
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        return target.position + rot * Vector3.back * distance;
    }
}
