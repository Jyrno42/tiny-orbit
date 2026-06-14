# Phase 0 - Project setup

## Goal
A blank but well-organized Unity project ready to build the demo into.

## Prompt

> Set up a new Unity 3D project for a tiny KSP-style rocket demo called
> "Tiny Orbit". I'm using Unity 2022.3 LTS (built-in render pipeline is fine).
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
