# Phase 5 - Orbital readout HUD

## Goal
On-screen numbers that tell the player altitude, speed, throttle, and - crucially
- live apoapsis/periapsis, plus an "ORBIT" indicator. This is what turns flying
into "making orbit".

## Prompt

> Add orbital-element math and a HUD.
>
> 1. `OrbitMath.cs` - a static helper. Given the rocket position and velocity
>    **relative to the planet centre** and the planet's `mu`, compute:
>    - `r = relative position`, `v = relative velocity`, `speed = |v|`.
>    - specific energy `E = speed^2/2 - mu/|r|`.
>    - semi-major axis `a = -mu / (2E)` (handle E >= 0 = escape/parabolic).
>    - eccentricity vector `eVec = ((speed^2 - mu/|r|) * r - dot(r,v) * v) / mu`;
>      `e = |eVec|`.
>    - apoapsis radius `ra = a*(1+e)`, periapsis radius `rp = a*(1-e)`.
>    Return these in a small struct (e is fine, a, ra, rp, and a `bool isOrbit`).
>
> 2. `OrbitHUD.cs` (OnGUI or uGUI/TextMeshPro - your call, keep it simple).
>    Use the planet centre and `PlanetBody.radius` to convert radii to altitudes
>    (`altitude = radius_value - R`). Display:
>    - Altitude (surface): `|r| - R`
>    - Speed (orbital, relative to planet)
>    - Throttle %
>    - Apoapsis altitude: `ra - R`
>    - Periapsis altitude: `rp - R`  (can be negative = you'll hit the ground)
>    - A bold "ORBIT" / "SUBORBITAL" / "ESCAPE" status: ORBIT when
>      `rp - R > 0` and bound (E < 0).
>
> Pull velocity from the rocket Rigidbody (`rb.velocity`) and position relative to
> the planet's transform.

## Done when
- Numbers update live during flight.
- Doing a real gravity turn (ascend, pitch toward horizontal near apoapsis, burn)
  raises periapsis above 0 and flips the status to ORBIT.
