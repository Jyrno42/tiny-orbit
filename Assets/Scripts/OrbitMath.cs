using UnityEngine;

/// <summary>
/// Static two-body orbital mechanics: classic elements from state vectors
/// relative to the planet centre.
/// </summary>
public static class OrbitMath
{
    /// <summary>Orbital elements computed from one (position, velocity) state.</summary>
    public struct Elements
    {
        /// <summary>Distance from the planet centre (m).</summary>
        public float radius;
        /// <summary>Speed relative to the planet (m/s).</summary>
        public float speed;
        /// <summary>Specific orbital energy E = v^2/2 - mu/r (J/kg).</summary>
        public float specificEnergy;
        /// <summary>Semi-major axis a = -mu/(2E); +infinity when parabolic.</summary>
        public float semiMajorAxis;
        /// <summary>Eccentricity (0 circular, &lt;1 ellipse, &gt;=1 escape).</summary>
        public float eccentricity;
        /// <summary>Apoapsis radius a(1+e); +infinity when unbound.</summary>
        public float apoapsisRadius;
        /// <summary>Periapsis radius a(1-e); also valid for hyperbolic paths.</summary>
        public float periapsisRadius;
        /// <summary>True when the trajectory is a closed (bound) orbit, E &lt; 0.</summary>
        public bool isBound;
    }

    /// <summary>
    /// Computes elements from position and velocity relative to the planet
    /// centre and the gravitational parameter mu.
    /// </summary>
    public static Elements FromState(Vector3 rVec, Vector3 vVec, float mu)
    {
        var el = new Elements();
        float r = Mathf.Max(rVec.magnitude, 0.001f);
        float speed = vVec.magnitude;
        el.radius = r;
        el.speed = speed;
        el.specificEnergy = 0.5f * speed * speed - mu / r;

        Vector3 eVec = ((speed * speed - mu / r) * rVec - Vector3.Dot(rVec, vVec) * vVec) / mu;
        el.eccentricity = eVec.magnitude;
        el.isBound = el.specificEnergy < 0f;

        el.semiMajorAxis = Mathf.Abs(el.specificEnergy) > 1e-6f
            ? -mu / (2f * el.specificEnergy)
            : float.PositiveInfinity;

        if (el.isBound)
        {
            el.apoapsisRadius = el.semiMajorAxis * (1f + el.eccentricity);
            el.periapsisRadius = el.semiMajorAxis * (1f - el.eccentricity);
        }
        else
        {
            el.apoapsisRadius = float.PositiveInfinity;
            // a < 0 and e >= 1 for hyperbolic, so a(1-e) is still positive
            el.periapsisRadius = float.IsInfinity(el.semiMajorAxis)
                ? r
                : el.semiMajorAxis * (1f - el.eccentricity);
        }
        return el;
    }
}
