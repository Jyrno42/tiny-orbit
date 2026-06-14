# Phase 4 - Camera

## Goal
A follow camera that keeps the rocket in view through liftoff and rotation.

## Prompt

> Add `FollowCamera.cs` to the Main Camera.
>
> - Serialized `target` (the Rocket transform) and `distance` (e.g. 25).
> - Follow the target with smooth damping (`Vector3.SmoothDamp` on position).
> - Keep the camera looking at the target (`transform.LookAt`).
> - Scroll wheel adjusts `distance` (clamp e.g. 8..400 so you can zoom out as you
>   climb).
> - Optional: hold right mouse to orbit the camera around the rocket (yaw/pitch);
>   keep it simple if it adds risk.
> - Use `LateUpdate` so the camera follows after physics has moved the rocket.
>
> Default the camera "up" to world up is fine for v1; don't over-engineer
> reorientation as the rocket goes around the planet.

## Done when
- Camera smoothly tracks liftoff and rotation without snapping or clipping into
  the rocket.
- Scroll zoom works across the altitude range.
