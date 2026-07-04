# Tiny Orbit - a tiny KSP-style rocket demo, built by AI via Unity MCP

A minimal Kerbal Space Program-inspired demo: a rocket built from basic primitives
(cylinders, cones, capsules) that launches from a pad, reaches a stable orbit
around a single planet, then deorbits and lands - by hand or via a one-press
autopilot. No part catalog, no tech tree, just the core loop: **throttle up, gain
horizontal velocity, circularize**, then come home.

It also doubles as a small **benchmark**: the same prompt spec is handed to
different AI models, which build the whole thing inside the Unity editor through a
Model Context Protocol (MCP) connection. See [RUNS.md](RUNS.md) for the runs and
how they compare.

## Two things in one repo
- **The recipe** (on `main`): a phase-by-phase prompt spec ([prompts/](prompts/),
  [PLAN.md](PLAN.md)) plus a clean, pre-configured Unity project to build it into.
- **The builds** (on `run/<model>-<date>` branches): each branch is one AI model's
  full implementation of the recipe.

## Goal of the demo
Stand on a launchpad, light the engine, fly up, pitch over, and reach a stable
orbit (periapsis above the surface), then deorbit and land under a parachute. A
HUD shows altitude, a vertical/horizontal velocity split, throttle/fuel, and live
apoapsis/periapsis so you know when you have made orbit.

## Scope (intentionally tiny)
In scope: one planet with inverse-square gravity; a 2-stage rocket from Unity
primitives; nose thrust plus reaction-wheel rotation; throttle, staging, and fuel;
a follow camera; an orbital-readout HUD; a parachute landing; a full
launch-to-land autopilot; and a starfield plus textured planet.

Out of scope (kept for later): atmosphere/aero, multiple bodies, patched-conic
trajectory prediction, docking, in-game part editor, save/load.

## See a built demo
```
git checkout run/fable-5-2026-06-15      # or any other run/ branch
```
Open the project in Unity, open `Assets/Scenes/Launch.unity`, press Play, then
**T** for the autonomous flight. Manual controls: Shift/Ctrl throttle, Z/X
full/cut, WASD + Q/E rotate, Space stage, P parachute, R reset.

## Start your own run
`main` is a clean, pre-configured baseline. To have a model build it from scratch:
```
./scripts/new-run.sh <model>-<date>      # branches run/<model>-<date> off main
```
Then open the project in Unity and paste [prompts/START-HERE.md](prompts/START-HERE.md)
as the first message to the build session. [RUNS.md](RUNS.md) has the full workflow
and the run log.

The original human prompts that seeded the repo are recorded verbatim in
[PROMPT-LOG.md](PROMPT-LOG.md) for reproduction and benchmarking.

## Target environment
- Unity 6000.4.11f1 (Unity 6.4); C# / MonoBehaviour scripts.
- Unity MCP: CoplayDev `com.coplaydev.unity-mcp` (connection recap at the bottom of
  [prompts/START-HERE.md](prompts/START-HERE.md)).
- Desktop keyboard + mouse.
