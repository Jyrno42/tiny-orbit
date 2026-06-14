# Phase 8 - Space visuals (sky, starfield, exhaust plume)

## Goal
Make Tiny Orbit look like space: a black sky with a procedural starfield, a warm
Sun with low ambient fill so a day/night terminator reads on the planet, and a
throttle-driven engine exhaust plume on the firing stage.

## Prompt

> Give the demo a proper space look and an engine plume. Apply all the
> scene-level look changes IN EDIT MODE so they persist, then SaveScene.
>
> ### Space look (edit mode, then SaveScene)
> - Set the Main Camera clear to a Skybox using a `Skybox/Panoramic` material
>   (so it draws the starfield, below).
> - Rename the Directional Light to "Sun", give it a warm color, enable soft
>   shadows.
> - Set a LOW ambient fill (not pure black) so the night side of the planet stays
>   dimly visible and a day/night terminator reads across the surface.
> - Give the planet a teal material.
> - Note: `DynamicGI` lives in the `UnityEngine` namespace (not in a GI/Rendering
>   sub-namespace) if you call `DynamicGI.UpdateEnvironment()`.
>
> ### Procedural starfield
> - Generate a star texture with ~1500-2200 stars distributed UNIFORMLY on the
>   sphere (sample directions so they do not cluster at the poles - e.g. cosine /
>   area-correct latitude sampling, not uniform-in-angle).
> - Most stars faint, with subtle blue and warm tints; scatter a few brighter
>   colored "feature" stars. An optional very faint nebula tint is a nice touch.
> - Wrap the texture on a `Skybox/Panoramic` material so the stars sit at infinity
>   with no parallax as the camera moves.
>
> ### Engine plume
> - `EnginePlume.cs`, one per engine. Additive particles: a bright yellow-white
>   core fading orange then red.
> - Throttle-driven: emission rate and plume length scale with throttle, and
>   emission is zero at 0% throttle. Fire ONLY on the active fueled stage.
> - Set the particle system to always-simulate (do not pause when off-screen).
>
> ### CRITICAL build note (do not skip)
> - Do NOT bake ParticleSystem module values into the prefab. Emission rate does
>   not survive prefab apply/instantiate on this MCP (it reverts to ~1, i.e. a
>   single bloom instead of a flame). Instead make `EnginePlume` BUILD and
>   CONFIGURE the entire particle system at runtime from plain serialized fields,
>   and drive `emission.rateOverTime` every frame (`maxRate * throttle`).
> - For a continuous (non-pulsing) flame use low exhaust speed + high particle
>   density, and randomize lifetime / speed / size so particle batches do not all
>   die at the same instant.
> - Expose knobs: `maxRate` (~300), `nozzleRadius`, `speedRange`, `lifetimeRange`
>   (0.6-1.1s), `sizeMul`.

## Done when
- Sky is black with a believable, non-clustered starfield that does not parallax.
- The Sun warms the lit side and a terminator reads across the teal planet; the
  night side is dimly lit, not pure black.
- The active engine shows a continuous throttle-driven flame (bright core fading
  to red), zero at no throttle, only on the fueled stage, and it survives a
  Stop/Play cycle (because it is built at runtime, not baked into the prefab).
