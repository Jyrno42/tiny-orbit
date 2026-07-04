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

## Build-session learnings
- Set Main Camera farClipPlane to 8000 and READ IT BACK to confirm (it silently reverts to the default 1000, which clips the planet at any real altitude). No layer cull distances are involved; far clip is the only knob.
- Make the camera frame planet-relative, not world-space: derive camera "up" from the planet->rocket radial direction so the planet stays framed and the horizon never rolls; zoom must be a pure dolly along the view axis. A world-up LookAt lets the planet drift out of frame and rolls the view on tilt/zoom.
- Build the sideways/up basis so it does NOT degenerate at the poles. cross(radialUp, worldUp) is undefined at the pole and flips 180 degrees as the rocket crosses it, snapping the whole view (looks like the rocket reversed). Parallel-transport the previous frame's basis (or use velocity direction as a stable reference). This matters because the launch site is at a pole and the orbit recrosses it each lap.

## Run 3 learnings (v4)
- Make manual camera controls REQUIRED, not optional. Scroll wheel = dolly zoom along the view axis (clamped). Right-mouse-drag = orbit the camera around the rocket: horizontal drag rotates the heading within the tangent plane (around the planet-radial axis), vertical drag adjusts view elevation, clamped (e.g. -70 to +85 degrees) so the framing never degenerates. Run both in LateUpdate independent of who is flying, so they behave identically in manual flight and under autopilot, and do not disturb the planet-relative pole-safe framing.
