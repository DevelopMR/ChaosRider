# Work Log

## 2026-04-23

- Read `Chaos_Rider.md` and re-aligned the prototype plan around destruction and ragdoll as top priorities.
- Removed replay from the active plan permanently.
- Chose multi-camera coverage instead of replay, with `Mounted FPV` and `Chase Cam` as Milestone 1 targets.
- Initialized a local git repository on branch `main`.
- Added Unity-focused `.gitignore`.
- Created project tracking markdown files for planning, backlog, decisions, and session logging.
- Completed Slice 1 scaffold: added `PrototypeArena.unity`, set it in build settings, and created a runtime arena bootstrap so the scene produces a testable graybox space immediately.
- Completed Slice 2 baseline: added `GameManager`, `RunState`, `AnimalDefinition`, and a first-pass `AnimalPhysicsController` with inspector-tunable drive, steering, and bucking chaos.
- Added a temporary `SimpleFollowCamera` so the bull controller is observable while the dedicated camera slice is still ahead.
- Completed Slice 3 camera pass: replaced the temporary distant view with a `CameraModeController` that supports mounted first-person and chase camera modes with runtime switching.
- Tuned Slice 3 based on play feedback: reduced airborne behavior, added stronger ground adhesion and upright correction, and lowered the mounted camera feel so the bull reads heavy instead of floaty.

## Logging Rules

- Add one short entry after each completed slice.
- Include what was built, how it was tested, and what is next.
