# START HERE - kickoff prompt for a build session

Paste the block below as the first message in a fresh Claude Code session that
has the Unity MCP connected. It points the session at this repo and the plan, and
starts the build. (Setup notes for the MCP itself are at the bottom.)

```
Work in this repo: /home/jyrno42/projects/gameprojects/ai-test/opus/tiny-orbit
(a git repo and a Unity 6000.4.11f1 project that's open in the editor). The Unity
MCP (CoplayDev UnityMCP, HTTP on 127.0.0.1:8080) is connected - use it to build
directly in the editor.

Read PLAN.md and the prompts/ folder first - they define a tiny KSP-style demo
called Tiny Orbit (fly a primitive-built rocket into orbit around one planet).
Assets/Scripts/ already has FrustumMesh.cs and LatheMesh.cs for procedural rocket
parts.

Then start building, working the phases in order:
1. Phase 0/1 first: create the Launch scene, add the PlanetBody (R=600, g0=9.81,
   mu=g0*R^2) and GravityReceiver scripts, the planet sphere, and verify
   inverse-square gravity by dropping a test Rigidbody and checking it falls
   toward the planet centre.
2. After each phase: check the Unity console for errors via the MCP, confirm the
   scene looks right, then git add -A && git commit (include the .meta files Unity
   generates). Single-line commit messages, no Co-Authored-By trailer, no
   em-dashes anywhere.
3. Pause after Phase 1 and show me the result before continuing to the rocket.
4. When the full demo works, add a HUD hide toggle (e.g. H) and capture 3-4 clean
   gallery screenshots with the HUD hidden: the rocket on the pad against the
   starfield, the craft in orbit over the planet, and the landed capsule. Capture
   during play with the HUD hidden, or via a positioned camera; never with the
   IMGUI HUD on screen or at timeScale=0 (both garble MCP captures on this editor).
   Save them under docs/shots/ and commit, so they can go into RUNS.md.

Use the MCP to create GameObjects/components and write scripts; don't just hand me
code to paste.
```

## Notes
- The prompt stops after Phase 1 so gravity can be sanity-checked before the whole
  rocket is built. Remove that line to let it run straight through.
- It only references committed files, so a fresh session has full context from the
  repo without any prior chat history.

## MCP setup recap (if the connection is ever lost)
- Use CoplayDev's MCP for Unity, not the official Unity AI Assistant relay - the
  official one (com.unity.ai.assistant 2.7-2.12) has a sticky "Connection revoked"
  bug that downgrading to 2.6.0 and re-approving did not fix on this machine.
- Unity package: `com.coplaydev.unity-mcp` (git URL in `Packages/manifest.json`).
- In Unity: Window > MCP for Unity, then Auto-Setup / Configure for Claude Code.
  It runs a Python server over HTTP on 127.0.0.1:8080.
- Registered with Claude Code as `UnityMCP` (user scope):
  `{ "type": "http", "url": "http://127.0.0.1:8080/mcp" }`.
- MCP servers attach at session start, so restart the Claude Code session after
  registering. Its tools are `manage_gameobject`, `manage_script`, `manage_scene`,
  `read_console`, etc.

## MCP workflow rules (learned the hard way)
- Never trust create-time component_properties on this MCP. They silently fall back to defaults (Rigidbody mass/useGravity/collision, camera farClip, collider center/size, particle modules). After creating any component, read the critical properties back and set them explicitly via manage_components set_property or execute_code (set field + EditorUtility.SetDirty + PrefabUtility.ApplyPrefabInstance for prefab parts).
- Apply scene-level changes (material wiring, camera clear, lighting, skybox, component refs) in EDIT mode, then SaveScene. SaveScene silently fails in play mode, so play-mode changes revert on stop. Asset files (a .mat) persist; scene wiring does not.
- MCP screenshots are garbled (noise) whenever an OnGUI/IMGUI HUD is on screen or when timeScale=0 (a capture quirk on this headless Linux GL editor, not a code bug). Capture in edit mode with the HUD off, or with a positioned manage_camera view. For live-HUD/plume looks, ask the user to confirm - you cannot screenshot it reliably.
- ~10-13s of game-time elapses between consecutive MCP calls (the editor runs real time while you think). Do not observe a flight by polling state across calls. Drive timed sequences from an in-game script (the AutoPilot) and add an in-game ring/trace buffer (timestamped alt/speed/Ap/Pe/phase/throttle) read back AFTER the run.
- Edit-mode retuning does NOT update an already-running play instance - Stop and Play again to pick up new values. Verify math by setting state directly (assign a circular orbit position+velocity, read back Ap/Pe) rather than flying and polling.
- Material/texture references must point at SAVED assets, not transient runtime textures. A material assigned a transient texture serializes to a null/white reference (e.g. a planet that renders washed-out white). Save the texture as an asset, assign the asset, and verify the reference survives a save. Same family as the create-time-props rule.
- Fable-run MCP quirks: batch_execute does not include create_script (create scripts directly with create_script / manage_script, not inside a batch); create_folder can report a false-positive success (verify the folder exists after); rename can report failure while still moving the file (verify the result, do not trust the return status).
- Scroll-wheel and mouse-drag hardware events cannot be injected through the MCP. To verify input-driven features (camera zoom/orbit), call the underlying methods the inputs map to and confirm the input-reading code runs clean each frame, then ask the user for one manual mouse wiggle in Play mode.
