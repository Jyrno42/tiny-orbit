# Phase 3 - Flight controls

## Goal
Throttle, thrust along the nose, and reaction-wheel rotation so the player can
fly and steer the rocket.

## Prompt

> Add `RocketController.cs` to the `Rocket` root and wire up flight controls.
>
> Throttle:
> - Float `throttle` in [0, 1].
> - `Shift` increases, `Ctrl` decreases throttle gradually; `Z` = full, `X` = cut.
> - Serialized `maxThrust` (e.g. 120 - tune so TWR > 1 at launch; with mass ~5 and
>   surface gravity ~9.81, weight is ~49, so 120 gives TWR ~2.4).
>
> Thrust:
> - In `FixedUpdate`, apply `transform.up * maxThrust * throttle` as a force at
>   the `ThrustPoint` position using `rb.AddForceAtPosition`, or just `AddForce`
>   along `transform.up` for v1 (simpler, no gimbal needed).
>
> Rotation (reaction wheels):
> - Serialized `torquePower` (e.g. 15).
> - `W`/`S` = pitch, `A`/`D` = yaw, `Q`/`E` = roll. Apply with
>   `rb.AddRelativeTorque(...)`.
> - Add a little angular drag so the rocket settles when keys are released.
>
> Also:
> - Key `R` to reset the rocket back to the launch pose (position, rotation, zero
>   velocity) for quick retries.
> - Keep input read in `Update`, physics applied in `FixedUpdate`.

## Done when
- Full throttle lifts the rocket off the pad.
- Pitch/yaw/roll keys rotate the rocket and it settles when released.
- `R` cleanly resets to the launchpad.

## Build-session learnings
- maxThrust 120 with mass 5 gives TWR ~2.45 - good. Re-verify mass actually stuck (see Phase 2); an un-set mass=1 makes launches violent.
