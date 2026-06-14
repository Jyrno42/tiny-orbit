# Prompt log

Verbatim record of the human prompts that generated this repo. Kept so the whole
plan can be regenerated later - with stronger AI, or to benchmark how well local
models plan and build it from the same starting point.

## Meta
- Model that produced the initial plan: Claude Opus 4.8 (1M context).
- Tooling: Claude Code CLI (VSCode extension).
- Date: 2026-06-14.
- Working dir: `/home/jyrno42/projects/gameprojects/ai-test/opus`
- Output: `README.md`, `PLAN.md`, `TODO.md`, `.gitignore`, `prompts/00..07`.

## Prompt 1 (initial request)

> I want to make a super simple ksp style space game with unity. Lets make a
> prompt for a basic demo where one can fly a rocket (built from basic shapes,
> ciliders and conic cilinders etc) and put it on orbit.
>
> Create plan and prompts for it and make this folder a git repo so plans are
> checked in and theres a generic todolist to track stuff.

## Prompt 2 (follow-up, mid-task)

> stack separator and heat shield and parachute too

## Prompt 3 (follow-up)

> also please log initial prompt i gave for rebuild purposes with further ai
> advancements and to test out how local models can plan / build this too later

## How to reproduce / benchmark
1. Start in an empty folder with another model (local or otherwise).
2. Feed Prompt 1, then Prompt 2, then Prompt 3 in order.
3. Compare the resulting `PLAN.md` / `prompts/` against this repo's `main` for
   coverage (planet & gravity model, orbital-element math, control scheme,
   staging, recovery parts) and for whether the physics choices are sound
   (manual inverse-square gravity, torque steering, `mu = g0 * R^2`).
