# Phase 9 - Autopilot (full autonomous mission)

## Goal
An autopilot that flies the whole mission with no human input: launch, gravity
turn, auto-staging, coast to apoapsis, circularize, hold one full orbit, deorbit,
reenter, deploy the chute and land - with KSP-style auto time-warp and a HUD
indicator.

## Prompt

> Add `AutoPilot.cs` to the Rocket. Engage it with key `T` and a HUD button.
> When engaged it flies the entire mission autonomously:
>
> 1. Launch straight up at full throttle.
> 2. Gravity turn: pitch over gradually, but CAP the pitch-over so the rocket
>    keeps climbing - never go full-horizontal at low altitude.
> 3. Auto-stage when the active tank empties (fire the separator, switch to the
>    upper engine/tank).
> 4. Coast to apoapsis.
> 5. Circularize: burn HORIZONTAL / downrange (the tangential direction), NEVER
>    along raw velocity (which points downward while descending). Cap the burn at
>    ~0.6 throttle and add a hard escape-speed guard so it cannot fly off.
> 6. Hold orbit for ONE full revolution: accumulate 360 degrees of swept angle
>    around the planet (do not use a fixed timer).
> 7. Retrograde deorbit burn, then reentry.
> 8. Deploy the parachute and land.
>
> Safety / control details:
> - Ground-safety throttle cut that triggers ONLY very near the ground
>   (alt < ~30). Do NOT key it to apoapsis height - an apoapsis-height guard would
>   kill the circularization burn.
> - Per-second trace buffer, CLEARED on each engage, sampling
>   time/alt/speed/Ap/Pe/phase/throttle for after-the-fact diagnosis (you cannot
>   reliably poll a live flight across MCP calls).
>
> Auto time-warp (KSP-style):
> - Drive `Time.timeScale` from the mission phase, no manual control.
> - HUD indicator like "ARROWS Nx TIME WARP" centered at the top, hidden at 1x.
> - Cap warp at 4x (8x jerks the camera).
> - Run launch and ALL burns at 1x. Warp the coasts and the orbital lap at 3-4x.
> - ANTICIPATE each burn by dropping back to 1x shortly before it starts.
> - Hold 1x for ~4s after engine cutoff (`coastWarpDelay`) before warping the
>   ascent coast.
> - Reset to 1x on mission end AND on play-stop, so a leaked timeScale does not
>   persist. Note: `Time.timeScale` does not change the trajectory, it only steps
>   fixed game-time faster.

## Done when
- Pressing `T` (or the HUD button) flies the full launch -> gravity turn ->
  auto-stage -> coast -> circularize -> one full orbit -> deorbit -> reentry ->
  chute -> land sequence with no further input.
- Time-warp ramps up on coasts and drops to 1x for every burn, with the HUD
  indicator showing the current multiplier (hidden at 1x), and resets to 1x at
  mission end and on Stop.
- A verified good run reached roughly Pe 120 / Ap 791 with fuel to spare.

## Run 2 learnings
- The autopilot must burn each lower stage to depletion at FULL throttle before it stages. A feathered/throttle-limited ascent leaves fuel in the booster, so the "tank empty" stage trigger never fires and the rocket never decouples. Size the booster fuel and the apoapsis target so the booster runs dry mid-ascent and decouples cleanly (Ap ~800 / Pe ~128 worked).
- Self-inflicted landing artifact: with boosters left in the world, the returning capsule can touch down on a spent booster and read as hovering. Despawn boosters after separation (see Phase 6).
