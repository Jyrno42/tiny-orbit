# Phase 0 - Project setup

## Goal
A blank but well-organized Unity project ready to build the demo into.

## Prompt

> Set up a new Unity 3D project for a tiny KSP-style rocket demo called
> "Tiny Orbit". I'm using Unity 6000.4.11f1 (Unity 6.4; built-in render pipeline is fine).
>
> Do the following:
> 1. Create the folder structure under `Assets/`: `Scripts/`, `Prefabs/`,
>    `Scenes/`.
> 2. Create and save an empty scene named `Launch` in `Assets/Scenes/`.
> 3. Add a `PlanetBody.cs` MonoBehaviour that holds the world constants as
>    serialized fields and exposes them:
>    - `radius = 600` (planet radius, metres)
>    - `surfaceGravity = 9.81`
>    - a read-only `Mu` property returning `surfaceGravity * radius * radius`
>    - a method `Vector3 GravityAt(Vector3 worldPos)` returning the gravitational
>      acceleration vector at a world position (inverse-square, pointing toward
>      this object's position). Guard against divide-by-zero at the centre.
> 4. In Project Settings > Time, note that I should set Fixed Timestep to 0.02
>    (tell me where to click; you can't change it in code reliably).
>
> Keep the gravity math in `PlanetBody` so other scripts can query it. Add brief
> XML doc comments on the public members.

## Done when
- Project opens, `Launch` scene exists, folders are in place.
- `PlanetBody` compiles and `Mu` returns 3,531,600 with default values.

## Build-session learnings
- Verify the physics backend before anything else: Project Settings > Physics, confirm GameObject SDK / Physics SDK is "PhysX", not "None". It shipped as None on this project, which silently breaks all Rigidbodies (rb.position reads (0,0,0), velocity writes ignored, nothing falls - looks like a gravity-code bug but is not). Enable PhysX and commit the ProjectSettings/DynamicsManager.asset change. Sanity check: enter Play with any Rigidbody and confirm rb.position tracks transform.position.
- Enable Project Settings > Player > Run In Background (and commit). Without it Unity throttles the editor to ~2 fps when its window is unfocused, which strobes particle effects and makes play-mode observation unreliable.
