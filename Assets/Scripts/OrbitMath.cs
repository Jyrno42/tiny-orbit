using UnityEngine;

/// <summary>
/// Classic two-body orbital elements from a state vector. All inputs are
/// relative to the planet centre. Distances are in world units (metres at our
/// toy scale); mu is the planet's gravitational parameter (g0 * R^2).
/// </summary>
public static class OrbitMath
{
    public struct Elements
    {
        public float speed;    // |v|
        public float energy;   // specific orbital energy = v^2/2 - mu/|r|
        public float a;        // semi-major axis (negative on hyperbolic trajectories)
        public float e;        // eccentricity
        public float ra;       // apoapsis radius (+inf when unbound)
        public float rp;       // periapsis radius
        public bool bound;     // true when energy < 0 (closed orbit)
        public bool isOrbit;   // bound AND periapsis clears the surface
    }

    /// <param name="r">Position relative to the planet centre.</param>
    /// <param name="v">Velocity relative to the planet.</param>
    /// <param name="mu">Planet gravitational parameter.</param>
    /// <param name="planetRadius">Surface radius, used only to decide isOrbit.</param>
    public static Elements Compute(Vector3 r, Vector3 v, float mu, float planetRadius)
    {
        Elements el = default;
        float rMag = r.magnitude;
        el.speed = v.magnitude;
        el.ra = float.PositiveInfinity;
        if (rMag < 1e-3f || mu <= 0f) return el;

        float energy = el.speed * el.speed * 0.5f - mu / rMag;
        el.energy = energy;
        el.bound = energy < 0f;

        // Eccentricity vector: ((v^2 - mu/r) r - (r . v) v) / mu
        Vector3 eVec = ((el.speed * el.speed - mu / rMag) * r - Vector3.Dot(r, v) * v) / mu;
        el.e = eVec.magnitude;

        if (Mathf.Abs(energy) < 1e-6f)
            el.a = float.PositiveInfinity;   // parabolic
        else
            el.a = -mu / (2f * energy);

        // Periapsis is defined for bound and hyperbolic orbits alike (a<0, e>1 -> rp>0).
        el.rp = el.a * (1f - el.e);
        // Apoapsis only exists for bound orbits; leave +inf otherwise.
        if (el.bound) el.ra = el.a * (1f + el.e);

        el.isOrbit = el.bound && (el.rp - planetRadius > 0f);
        return el;
    }
}
