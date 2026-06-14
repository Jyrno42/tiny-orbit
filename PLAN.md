# Plan - Tiny Orbit

A super simple KSP-style demo: fly a primitive-built rocket into a stable orbit
around one planet. This document describes the design and the build order. Each
phase maps to a prompt in [prompts/](prompts/).

## Design overview

### Coordinate & scale choices
Real planet scale (millions of metres) causes floating-point jitter and is
overkill for a demo. Use a **toy scale**:

- Planet radius: `R = 600` units (metres).
- Surface gravity: `g0 = 9.81` m/s^2.
- Derive the gravitational parameter so surface gravity matches:
  `mu = g0 * R^2`. With the numbers above, `mu = 9.81 * 600^2 = 3,531,600`.
- Gravity at distance `r` from planet centre: `a = mu / r^2`, directed inward.
- Circular orbit speed at `r`: `v = sqrt(mu / r)`. At ~`r = 700` that's about
  `sqrt(3,531,600 / 700) ~= 71` m/s, so orbit is reachable in well under a minute
  of burn. Good for a demo.

These constants live in one `PlanetBody` component so they're easy to tweak.

### Physics model
- Single planet, single rocket. No N-body.
- Rocket is one `Rigidbody`. We apply **gravity manually** each `FixedUpdate`
  (do not use Unity's global gravity - it's a constant flat vector and won't
  curve an orbit). Set `rigidbody.useGravity = false`.
- Thrust is a force along the rocket's local up (nose) axis, scaled by throttle
  and remaining fuel.
- Rotation control is torque (reaction-wheel style) - simple and stable, avoids
  needing gimballed thrust for a first demo.
- Fixed timestep tightened (e.g. 0.01-0.02 s) for stable integration.

### Why torque-based control and manual gravity
KSP-feel without aerodynamics: the player pitches the nose with reaction wheels,
points "horizontal" near apoapsis, and burns to raise periapsis. Manual
inverse-square gravity is what makes a closed orbit possible at all.

### Orbital readout (the "did I make orbit?" feedback)
From the rocket's position `r` and velocity `v` relative to the planet centre,
compute the classic two-body orbital elements each frame:

- Specific orbital energy: `E = v^2/2 - mu/|r|`.
- Semi-major axis: `a = -mu / (2E)`.
- Eccentricity vector → eccentricity `e`.
- Apoapsis radius: `a*(1+e)`, periapsis radius: `a*(1-e)`.
- Show altitudes as `radius - R`. **Orbit achieved** when periapsis altitude > 0
  (and ideally above any atmosphere line, which we don't have here).

No trajectory line is required for v1; the numbers are enough.

## Phase breakdown

Each phase is small, testable on its own, and has a prompt file.

| Phase | Name | Prompt | Outcome |
|-------|------|--------|---------|
| 0 | Project setup | [prompts/00-project-setup.md](prompts/00-project-setup.md) | Empty Unity project, scene, folders, constants. |
| 1 | Planet & gravity | [prompts/01-planet-and-gravity.md](prompts/01-planet-and-gravity.md) | Planet sphere + inverse-square gravity pulling a test Rigidbody. |
| 2 | Rocket assembly | [prompts/02-rocket-assembly.md](prompts/02-rocket-assembly.md) | Rocket prefab from primitives with mass and a launch pose. |
| 3 | Flight controls | [prompts/03-flight-controls.md](prompts/03-flight-controls.md) | Throttle + thrust + reaction-wheel rotation; rocket flies. |
| 4 | Camera | [prompts/04-camera.md](prompts/04-camera.md) | Smooth follow camera with zoom. |
| 5 | Orbital HUD | [prompts/05-orbital-readout-ui.md](prompts/05-orbital-readout-ui.md) | HUD: altitude, speed, throttle, Ap/Pe; "ORBIT" indicator. |
| 6 | Fuel & staging | [prompts/06-fuel-and-staging.md](prompts/06-fuel-and-staging.md) | Fuel burn, empty-stage jettison, dry/wet mass. |
| 7 | Recovery parts | [prompts/07-recovery-parts.md](prompts/07-recovery-parts.md) | Stack separator (decoupler), heat shield, parachute. |
| 8 | Space visuals | [prompts/08-space-visuals.md](prompts/08-space-visuals.md) | Black sky, Sun + low ambient, procedural starfield, engine exhaust plume. |
| 9 | Autopilot | [prompts/09-autopilot.md](prompts/09-autopilot.md) | Full autonomous launch-to-land mission with auto time-warp + HUD. |

### Recommended verification per phase
- P1: drop a cube near the planet, confirm it falls and accelerates toward centre.
- P2: rocket sits on the pad without sinking or jittering.
- P3: full throttle lifts off; rotation keys pitch the nose.
- P4: camera tracks through liftoff and rotation without clipping.
- P5: numbers update live; do a real ascent and watch periapsis rise above 0.
- P6: engine cuts when fuel hits 0; staging drops mass and relights.
- P7: separator pops the stages apart; parachute deploys and slows descent to a
  survivable landing; heat shield stays on the capsule.

## Build-session learnings (tuning + tight margins)
- Tight margins: escape speed is only sqrt(2)x circular speed (~90 vs ~71 m/s) and the rocket is over-powered, so full-throttle guidance escapes and the apoapsis dwell is only ~4-5s. Use proportional throttle that feathers as Ap/Pe approach target, cap the circularization burn (~0.6 throttle), target apoapsis ~200m (not ~110) for enough dwell, and add a hard escape-speed guard.
- Key numeric values: R=600, g0=9.81, mu=3,531,600; mass=5, maxThrust=120 -> TWR 2.45; escape ~ sqrt(2)x circular; apoapsis target ~200m; circularize ~0.6 throttle; upper-stage fuel ~80; camera farClip 8000; time-warp cap 4x (8x jerks the camera), coastWarpDelay ~4s, followSmoothTime 0.1; plume rate ~300, lifetime 0.6-1.1s randomized; chute touchdown ~3 m/s; HUD panel height ~540.

## Stretch ideas (after the demo works)
- Thin atmosphere with exponential-density drag and a "safe orbit" altitude line.
- Predicted-trajectory line via patched conics or simple forward integration.
- Map view, maneuver nodes, multiple parts/engines, save/load.

## Project structure (target, once Unity code exists)
```
Assets/
  Scripts/
    PlanetBody.cs        # constants: R, mu, gravity query
    GravityReceiver.cs   # applies mu/r^2 to a Rigidbody
    RocketController.cs  # throttle, thrust, reaction-wheel torque
    OrbitMath.cs         # static: elements from (r, v, mu)
    OrbitHUD.cs          # on-screen readouts
    FollowCamera.cs
    FuelTank.cs / Stage.cs
    StackSeparator.cs    # decoupler: split into two Rigidbodies + push apart
    HeatShield.cs        # base shield (ablator stub for future reentry heat)
    Parachute.cs         # deploy state machine + speed-based drag
  Prefabs/
    Rocket.prefab
  Scenes/
    Launch.unity
```
