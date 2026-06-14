# Tiny Orbit - a super simple KSP-style rocket demo (Unity)

A minimal Kerbal Space Program-inspired demo: build a rocket out of basic
primitives (cylinders, cones, capsules), launch it, and put it into a stable
orbit around a single planet. No part catalog, no tech tree, no career mode -
just the core loop of **throttle up, gain horizontal velocity, circularize**.

## Goal of the demo

Stand on a launchpad, light the engine, fly up, pitch over, and reach a stable
orbit where periapsis is above the planet's surface. A small HUD shows altitude,
speed, throttle, and live apoapsis/periapsis so you know when you've made orbit.

## Scope (intentionally tiny)

In scope:
- One planet with inverse-square gravity (no atmosphere, or a trivial drag stub).
- One rocket assembled from Unity primitives in the editor.
- Thrust along the rocket's nose, reaction-wheel style rotation control.
- Throttle, staging (drop empty stage), and fuel burn.
- Follow camera and a text HUD with orbital readouts.

Out of scope (kept for later): atmosphere/aero, multiple celestial bodies,
patched-conic trajectory prediction, docking, in-game part editor, save/load.

## How to use this repo

This repo is **plans and prompts**, not (yet) Unity code. The intended workflow:

1. Read [PLAN.md](PLAN.md) for the architecture and phase breakdown.
2. Work the phases in order. Each phase has a ready-to-use prompt in
   [prompts/](prompts/) that you can hand to an AI coding assistant (or follow
   by hand) to implement that slice in Unity.
3. Track progress in [TODO.md](TODO.md).

The original human prompts that produced this repo are recorded verbatim in
[PROMPT-LOG.md](PROMPT-LOG.md) for reproducibility and for benchmarking other
(e.g. local) models on the same task later.

## Target environment

- Unity 2022.3 LTS or newer (Built-in or URP - either is fine).
- C# / MonoBehaviour scripts.
- Desktop keyboard + mouse input.

## Benchmark workspace (branch-per-run)

This repo doubles as a benchmark harness. `main` holds the recipe + a clean,
pre-configured Unity project; each build attempt lives on a `run/<model>-<date>`
branch. See [RUNS.md](RUNS.md) for how to start a run, view a run's results, and
the log of runs so far. Helper: `./scripts/new-run.sh <model>-<date>`.
