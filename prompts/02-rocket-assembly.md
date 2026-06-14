# Phase 2 - Rocket assembly

## Goal
A rocket built from basic primitives, with one Rigidbody, sitting upright on the
launchpad.

## Prompt

> Build a simple rocket out of Unity primitives and make it a prefab.
>
> Hierarchy (single root GameObject named `Rocket`):
> - `Rocket` (empty root, has the Rigidbody)
>   - `CommandPod`   - a Cone or Capsule at the top (nose)
>   - `FuelTank`     - a Cylinder in the middle (the body)
>   - `Engine`       - a short Cone (wide end down) at the bottom
>   - optional `Fins` - a few thin Cubes near the base
>   - `ThrustPoint`  - an empty child at the very bottom marking where thrust
>     applies and where exhaust will go
>
> Requirements:
> - Unity has no built-in cone primitive. Use a Cylinder scaled to a taper, or
>   tell me to import a simple cone mesh; a Capsule for the nose is acceptable for
>   v1. Keep it readable.
> - Put a single `Rigidbody` on the root only (children are visual + colliders).
>   Set mass to something sensible (e.g. 5 = ~total). Set
>   `collisionDetectionMode = Continuous`, `useGravity = false`.
> - The rocket's local **up (+Y)** axis must point out the nose. Thrust will be
>   applied along this axis later.
> - Add the `GravityReceiver` from Phase 1 to the root.
> - Place the root on the planet surface (position = planet up * (radius + halfHeight))
>   so it rests on the pad, nose pointing radially outward (align local up with
>   the surface normal at the launch point).
> - Save as `Assets/Prefabs/Rocket.prefab`.

## Done when
- The rocket rests on the planet surface without sinking or jittering.
- Its nose points away from the planet centre (straight up at the launch site).
- It is a reusable prefab.

## Build-session learnings
- After creating the Rigidbody, READ its properties back and confirm mass=5, useGravity=false, collisionDetectionMode=Continuous, interpolation=Interpolate. Create-time props are unreliable on this MCP and silently revert to defaults (mass=1, useGravity=true, Discrete). At mass 5 and maxThrust 120 full-throttle TWR is ~2.45 (climb ~15 m/s^2); if it shoots up many km in seconds, mass reverted to 1 (TWR ~12).

## Run 2 learnings
- Give the rocket a FLAT BOX collider at the base for a stable stance, not a capsule collider. A capsule collider makes the rocket tip over on the pad. Keep the box wide enough that it rests upright.
