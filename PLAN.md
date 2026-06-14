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

### Recommended verification per phase
- P1: drop a cube near the planet, confirm it falls and accelerates toward centre.
- P2: rocket sits on the pad without sinking or jittering.
- P3: full throttle lifts off; rotation keys pitch the nose.
- P4: camera tracks through liftoff and rotation without clipping.
- P5: numbers update live; do a real ascent and watch periapsis rise above 0.
- P6: engine cuts when fuel hits 0; staging drops mass and relights.
- P7: separator pops the stages apart; parachute deploys and slows descent to a
  survivable landing; heat shield stays on the capsule.

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
