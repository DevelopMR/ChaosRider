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
- Fixed Slice 3 layout issues from playtesting: lifted the mounted view out of the bull body and expanded the arena floor and safety bounds to prevent wall-adjacent infinite falls.
- Slice 3 approved after playtesting: chase camera works, the moat issue is gone, and mounted rolling motion reads correctly.
- Implemented Slice 4: added a `RiderMountSystem` with stability drain, hold-on recovery, impact-based instability, and ejection thresholds.
- Implemented Slice 5: added a `RiderRagdollSystem` that builds a placeholder ragdoll at runtime, throws it using animal momentum, and moves the camera to chase the crash payoff.
- Added another first-person boost: mounted view now hides overlapping placeholder body geometry and adds speed/bob tuning for a more readable ride feel.
- Polished mounted view anchoring: first-person camera now prefers a dedicated anchor inside the rider head rather than a low seat point on the bull.
- Fixed mounted-view launch bug: the rider seat anchor and the rider head camera anchor are now separate, preventing the mounted rig from recursively chasing its own head point.
- Tuned bull handling for mass: high-speed turning now fades into a momentum arc instead of spinning on the spot, and mounted view now hides the full placeholder bull geometry for cleaner first-person readability.
- Further tuned bull turn mass: steering now pivots from a point pushed forward into the chest area and gives up even more high-speed spin so the animal carves instead of rotating like a light vehicle.

## Logging Rules

- Add one short entry after each completed slice.
- Include what was built, how it was tested, and what is next.
