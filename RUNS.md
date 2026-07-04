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
| `run/fable-5-2026-06-15` | Claude Fable 5 (1M) | 2026-07-04 | Complete: orbit (Ap ~806 / Pe ~111, e 0.33), full autopilot mission on the FIRST attempt (auto-stage at alt 41, one full 360 lap, deorbit to Pe -81, chute at 240m, touchdown 0.68 m/s, 67 fuel spare), starfield + textured planet + plumes | 10 commits (59170ad..962df90), fully autonomous after the phase-1 checkpoint - zero mid-build corrections needed. Every phase verified in play mode before commit (angular-momentum check for gravity, injected circular orbit for OrbitMath, in-game trace buffers for chute and mission). No prior-run gotchas recurred; two-stage geometry was built into the prefab from phase 2 so later phases needed no rework. New MCP quirks logged: batch_execute lacks create_script, create_folder false positive, rename reports failure but moves the file. |

When you finish a run, add a row here (do it on `main`, or on the run branch and
cherry-pick), noting model, result, commit count, and where it needed help.
