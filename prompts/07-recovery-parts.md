# Phase 7 - Stack separator, heat shield & parachute

## Goal
The parts that make a rocket separate cleanly and come back down safely: a stack
separator (decoupler) between stages, a heat shield at the base of the capsule,
and a parachute for a soft landing.

## Prompt

> Add three classic KSP parts to the rocket: a stack separator, a heat shield, and
> a parachute. Build them from primitives like the rest of the rocket.
>
> ### Stack separator (decoupler)
> - A short, wide Cylinder placed between two stages in the hierarchy.
> - `StackSeparator.cs`: a `Separate()` method that splits the rocket into two
>   bodies at this joint - the part above keeps the main `Rigidbody`/controller,
>   the part below gets its own `Rigidbody`.
> - On separation, apply a small impulse along the separation axis to both halves
>   (`rb.AddForce(... ForceMode.Impulse)`) so they physically push apart - the
>   visible "pop" of a real decoupler.
> - Hook this into the staging from Phase 6: pressing `Space` fires the lowest
>   active separator (which then jettisons the stage below it).
>
> ### Heat shield
> - A shallow cone / dished Cylinder at the very bottom of the command pod,
>   slightly wider than the pod.
> - `HeatShield.cs`: for v1 it's mostly cosmetic + a mass value, since we have no
>   atmosphere yet. Expose a serialized `ablator` amount as a stub so a future
>   reentry-heating phase can deplete it. Keep it simple.
> - It should stay attached to the capsule after the lower stages separate, so it
>   protects the pod on the way down.
>
> ### Parachute
> - A `Parachute.cs` on the command pod. States: `Stowed -> Deploying -> Deployed`
>   (and `Cut`).
> - Press `P` to deploy. When deployed, apply an upward drag force opposing
>   velocity that scales with speed (`-velocity.normalized * dragCoeff *
>   speed^2`), clamped so it doesn't snap the descent to a halt instantly. Ramp
>   the drag in over `Deploying` (~1-2 s) instead of full force on frame one.
> - Show a simple visual: scale up a hemisphere/canopy (a squashed Sphere) above
>   the pod when deployed. Optionally only allow deploy below some altitude.
> - Goal: a rocket falling at high speed deploys the chute and touches down slow
>   enough to survive (define "survive" as vertical speed < ~6 m/s at contact).
>
> Wire the new keys into the existing input handling and update the HUD to show
> parachute state and (optionally) separator-armed status.

## Done when
- `Space` fires the stack separator and the two halves visibly push apart.
- The heat shield stays on the capsule through separation.
- `P` deploys the parachute, drag ramps in, and the capsule lands slowly instead
  of cratering.

## Build-session learnings
- On touchdown, cut the parachute and collapse the canopy (scale -> 0); a chute left inflated reads as the craft floating. Detect landing from the craft's lowest part, not the Rigidbody root (root sits ~3m below the engine after staging, so root-based detection stops early or never triggers). Tune chute drag so descent is brisk enough to record (~3 m/s touchdown), not an ~80s drift.
