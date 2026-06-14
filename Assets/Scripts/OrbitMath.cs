using UnityEngine;

/// <summary>
/// Two-body orbital elements from a position and velocity relative to the planet
/// centre. Pure math, no scene dependencies - call <see cref="Compute"/> each frame.
/// </summary>
public static class OrbitMath
{
    /// <summary>Snapshot of the current orbit's classic elements.</summary>
    public struct OrbitState
    {
        public float radius;   // |r|, distance from planet centre
        public float speed;    // |v|, orbital speed relative to planet
        public float energy;   // specific orbital energy
        public float a;        // semi-major axis (negative on escape trajectories)
        public float e;        // eccentricity
        public float ra;       // apoapsis radius (PositiveInfinity if unbound)
        public float rp;       // periapsis radius (closest approach)
        public bool bound;     // true when energy < 0 (a closed/elliptical path)
        public bool isOrbit;   // bound AND periapsis is above the surface
    }

    /// <summary>
    /// Compute orbital elements. <paramref name="r"/> and <paramref name="v"/> are
    /// position/velocity relative to the planet centre; <paramref name="mu"/> is the
    /// gravitational parameter; <paramref name="planetRadius"/> sets the surface for
    /// the orbit-achieved test.
    /// </summary>
    public static OrbitState Compute(Vector3 r, Vector3 v, float mu, float planetRadius)
    {
        var s = new OrbitState();
        float rMag = r.magnitude;
        s.radius = rMag;
        s.speed = v.magnitude;
        if (rMag < 1e-4f || mu <= 0f) return s;

        s.energy = s.speed * s.speed * 0.5f - mu / rMag;
        s.bound = s.energy < 0f;

        // a = -mu/(2E): positive for ellipses, negative for hyperbolas.
        s.a = -mu / (2f * s.energy);

        // Eccentricity vector -> eccentricity.
        Vector3 eVec = ((s.speed * s.speed - mu / rMag) * r - Vector3.Dot(r, v) * v) / mu;
        s.e = eVec.magnitude;

        // Periapsis is meaningful for both ellipse and hyperbola; apoapsis only when bound.
        s.rp = s.a * (1f - s.e);
        s.ra = s.bound ? s.a * (1f + s.e) : float.PositiveInfinity;

        s.isOrbit = s.bound && s.rp > planetRadius;
        return s;
    }
}
