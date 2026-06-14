# Runs - Tiny Orbit benchmark workspace

`main` is the baseline: the recipe (prompts + plan) plus a clean, already-configured
Unity 6000.4.11f1 project (PhysX enabled, Run In Background on, CoplayDev MCP in
`Packages/manifest.json`, and the `FrustumMesh`/`LatheMesh` procedural-mesh tools).
It contains **no game build**.

Every build attempt lives on its own `run/<model>-<date>` branch, so runs can be
compared against each other and against the prompts on `main`.

## Start a new run
1. `git checkout main`
2. `git checkout -b run/<model>-<date>`  (or: `./scripts/new-run.sh <model>-<date>`)
3. Open this folder in Unity 6000.4.11f1. The project is already configured, so no
   physics/MCP setup is needed. (If the Unity MCP is not connected, see the setup
   recap at the bottom of `prompts/START-HERE.md`.)
4. Paste `prompts/START-HERE.md` as the first message to the build session and let
   the model build. Commits accumulate on the run branch.

## See a run's results
1. `git checkout run/<model>-<date>`
2. Open the project in Unity, open `Assets/Scenes/Launch.unity`, press Play.
3. Press **T** for the autonomous launch -> orbit -> deorbit -> landing flight.
   (Manual: throttle Shift/Ctrl, rotate WASD + Q/E, Space to stage, P for chute,
   R to reset.)

## Runs log
| Branch | Model | Date | Result | Notes |
|--------|-------|------|--------|-------|
| `run/opus-4.8-2026-06-14` | Claude Opus 4.8 (1M) | 2026-06-14 | Complete: reaches orbit, full autopilot mission, landing, space visuals | ~20 commits. Needed live feedback for PhysX-off, camera framing, exhaust plume, and autopilot tuning. Those learnings are now folded into the prompts on `main`, so the next run should need far less. |

When you finish a run, add a row here (do it on `main`, or on the run branch and
cherry-pick), noting model, result, commit count, and where it needed help.
