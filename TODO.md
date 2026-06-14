# TODO - Tiny Orbit

Generic checklist for the demo. Check items off as you go. Keep it loose -
add/remove freely.

## Phase 0 - Project setup
- [ ] Create Unity project (2022.3 LTS+), 3D template
- [ ] Add folder structure under `Assets/` (Scripts, Prefabs, Scenes)
- [ ] Create `Launch` scene
- [ ] Add a `Constants`/`PlanetBody` script with R, g0, mu
- [ ] Set Fixed Timestep to ~0.02 in Time settings

## Phase 1 - Planet & gravity
- [ ] Planet sphere at origin, radius matches R
- [ ] `PlanetBody` exposes gravity at a given position
- [ ] `GravityReceiver` applies inverse-square gravity in FixedUpdate
- [ ] Disable Unity global gravity on the rocket Rigidbody
- [ ] Verify: a dropped test object falls toward the planet centre

## Phase 2 - Rocket assembly
- [ ] Build rocket from primitives (cone nose, cylinder tanks, cone engine)
- [ ] Single Rigidbody on the root; sensible mass
- [ ] Place on launchpad on the planet surface, nose pointing up (radial)
- [ ] Save as `Rocket` prefab
- [ ] Verify: sits still on the pad, no jitter

## Phase 3 - Flight controls
- [ ] Throttle (e.g. Shift up / Ctrl down, or 0-1 keys)
- [ ] Thrust force along nose axis, scaled by throttle
- [ ] Reaction-wheel torque for pitch/yaw/roll (WASD + Q/E)
- [ ] Verify: full throttle lifts off; controls rotate the rocket

## Phase 4 - Camera
- [ ] Follow camera tracking the rocket
- [ ] Smooth follow + mouse/scroll zoom
- [ ] Verify: tracks through liftoff and rotation cleanly

## Phase 5 - Orbital HUD
- [ ] `OrbitMath` computes Ap/Pe/e/a from (r, v, mu)
- [ ] HUD shows altitude, speed, throttle
- [ ] HUD shows apoapsis & periapsis altitudes
- [ ] "ORBIT" indicator when periapsis altitude > 0
- [ ] Verify: fly an ascent, watch periapsis climb above 0

## Phase 6 - Fuel & staging
- [ ] Fuel amount per tank; thrust consumes fuel
- [ ] Engine cuts when fuel = 0
- [ ] Staging key jettisons empty stage (drops mass)
- [ ] Dry vs wet mass handled
- [ ] Verify: stage drops, remaining engine relights

## Phase 7 - Recovery parts
- [ ] Stack separator (decoupler) between stages, built from a primitive
- [ ] `Separate()` splits into two Rigidbodies and pushes them apart with impulse
- [ ] `Space` fires the lowest active separator (ties into staging)
- [ ] Heat shield at base of command pod (mass + ablator stub)
- [ ] Heat shield stays attached to capsule after separation
- [ ] Parachute on the pod: Stowed -> Deploying -> Deployed -> Cut
- [ ] `P` deploys; speed-based drag ramps in over ~1-2 s
- [ ] Canopy visual when deployed; HUD shows parachute state
- [ ] Verify: separator pops apart; chute lands the capsule under ~6 m/s

## Polish / stretch
- [ ] Launch from a clean reset (R to respawn on pad)
- [ ] Simple particle/exhaust on the engine
- [ ] Thin atmosphere + drag
- [ ] Predicted trajectory line
- [ ] Map view / maneuver nodes
