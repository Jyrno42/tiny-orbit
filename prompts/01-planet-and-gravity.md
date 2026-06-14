# Phase 1 - Planet & gravity

## Goal
A planet in the scene and a Rigidbody that falls toward it with realistic
inverse-square gravity (so orbits are possible).

## Prompt

> In the `Launch` scene, set up the planet and custom gravity.
>
> 1. Add a Sphere named `Planet` at the world origin. Scale it so its visual
>    radius equals the `PlanetBody.radius` (a default Unity sphere is 0.5 units
>    radius, so scale = `2 * radius`). Put the `PlanetBody` component on it.
>    Give it a sphere collider matching the surface.
> 2. Create `GravityReceiver.cs`: a MonoBehaviour that, in `FixedUpdate`, queries
>    the scene's `PlanetBody.GravityAt(transform.position)` and applies it to its
>    `Rigidbody` with `rb.AddForce(gravity * rb.mass)` (or `AddForce(accel,
>    ForceMode.Acceleration)`). It must find the `PlanetBody` (serialized
>    reference or `FindObjectOfType` in Awake).
> 3. Important: set `rb.useGravity = false` on anything with a `GravityReceiver`
>    so Unity's flat global gravity doesn't fight ours.
> 4. For testing, drop a Cube with a Rigidbody + `GravityReceiver` a few hundred
>    units above the planet surface.
>
> Use a tightened fixed timestep (0.02) and continuous collision detection on
> fast bodies so they don't tunnel through the planet.

## Done when
- The test cube accelerates toward the planet centre and lands on the surface.
- Given a sideways nudge at altitude, it visibly curves (a partial orbit/arc),
  proving the force is radial, not flat.
