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
| `run/opus-4.8-v2prompts-2026-06-14` | Claude Opus 4.8 (1M) | 2026-06-14 | Complete: orbit (Ap ~800 / Pe ~128), full autopilot mission, landing, textured planet + space visuals | 12 commits (cdbfc02..14cd113), mostly autonomous. The v1 gotchas did NOT recur (PhysX, camera, plume, autopilot tuning were pre-empted by the folded prompts). New bugs hit instead: capsule collider tipped the rocket (fixed with a flat box collider), autopilot never staged (feathered throttle hid the booster's remaining fuel; forced full-throttle depletion), capsule landed on its own spent booster and hovered (boosters now despawn ~30s after separation), and a null material-texture reference washed the planet white. User-requested polish: seamless procedural planet surface and forced mid-ascent staging. |
| `run/fable-5-2026-06-15` | Claude Fable 5 | 2026-07-04 | Complete on the FIRST attempt: orbit (Ap ~806 / Pe ~111, e 0.33), full autopilot mission (auto-stage at alt 41m, one 360 lap, deorbit to Pe -81, chute at 240m, touchdown 0.68 m/s, 67 fuel spare), starfield + textured planet + plumes | 11 commits (10 build 59170ad..962df90 + run-log). FULLY AUTONOMOUS: only the initial START-HERE prompt, zero further user input or corrections. Every phase verified in play mode before commit (angular-momentum check for gravity, injected circular-orbit state for OrbitMath, in-game trace buffers). No prior-run gotchas recurred; two-stage geometry built into the prefab from Phase 2 so Phases 6-7 needed no rework. Quality gaps vs the manual run 2: no manual camera controls (orbit/zoom), and reentry does not pitch nose-up so the capsule lands sideways under the chute. New MCP quirks logged: batch_execute lacks create_script, create_folder gives a false-positive, rename reports failure but still moves the file. |

When you finish a run, add a row here (do it on `main`, or on the run branch and
cherry-pick), noting model, result, commit count, and where it needed help.
