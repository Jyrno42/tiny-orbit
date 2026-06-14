# Phase 6 - Fuel & staging

## Goal
Engines burn fuel, cut out when empty, and the player can jettison spent stages
to drop dead mass - the last bit of KSP flavour.

## Prompt

> Add fuel and simple staging.
>
> 1. `FuelTank.cs` - serialized `fuel` and `maxFuel` (units). Provides
>    `Consume(float amount)` returning how much was actually drawn, and a
>    `IsEmpty` flag.
> 2. Update `RocketController`:
>    - Serialized `fuelBurnPerSecond` at full throttle.
>    - Each `FixedUpdate`, while thrusting, draw `fuelBurnPerSecond * throttle *
>      fixedDeltaTime` from the active tank. If the tank is empty, produce no
>      thrust (engine flames out).
>    - Optionally reduce Rigidbody mass as fuel drains (interpolate between dry and
>      wet mass) for a more realistic TWR climb - nice but optional.
> 3. Staging:
>    - Define a `Stage` (root child grouping a tank + engine). Pressing `Space`
>      jettisons the bottommost stage: detach it from the rocket root, give it its
>      own Rigidbody so it falls away, and switch the controller to the next
>      stage's engine/tank.
>    - Keep it to 2 stages for the demo.
>
> Keep the data model minimal; the point is to feel the mass drop and the engine
> relight, not to simulate a full fuel-flow graph.

## Done when
- Holding throttle drains fuel; at 0 the engine stops producing thrust.
- `Space` drops the spent stage (you see mass fall away) and the upper engine
  takes over.
- With staging you can reach orbit you couldn't on one stage.

## Build-session learnings
- Size upper-stage fuel with margin for a deorbit burn, not just circularization (~80 units worked). A budget that just reaches orbit leaves nothing for the retrograde deorbit, so the full autopilot mission cannot complete.
