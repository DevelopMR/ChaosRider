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
- Reduced high-speed turning again: weaker yaw authority, smaller high-speed lead angle, and a slightly more forward steering pivot so the bull commits harder to its momentum.
- Reduced high-speed turning a further step: even less yaw authority, smaller lead angle, and the steering pivot pushed nearer the horns so the front of the bull guides the arc more decisively.
- Rebalanced turn feel toward the front shoulders: cut center-body yaw influence again and pushed the effective steering pivot farther forward so the green-line front-shoulder pivot should dominate over the red-line body-center feel.
- Reworked steering pivot model: replaced the single forward offset test with paired front and rear steering forces so the front shoulders actively define the arc instead of a center-body yaw torque dominating the feel.
- Began the gait-engine pivot in code: added `AnimalMood`, `AnimalProfile`, `GaitProfile`, `AnimalLocomotionController`, and a first torso-only `GaitEngine`.
- Wired the first `DogTrot` reference gait into the existing torso body so rider, camera, stability, and ragdoll can now be evaluated against gait-driven motion instead of springy buck oscillation.
- Softened the first `DogTrot` pass with simple body tension: reduced support/steer spikes and added upright, lateral, and vertical damping so the torso settles like a standing animal instead of oscillating like a haunted crate.
- Tuned the first gait again toward torso rhythm: narrowed the virtual contact spread, lengthened stance, softened direct corner forces, and added a light diagonal cadence layer so the body can roll and surge more like an animal torso and less like a giant shoe.
- Reworked mounted rider stability to react to bad balance, surge, and side slip instead of draining just from forward travel, and added a short low-stability grace window so ordinary gait cycles do not immediately ragdoll the rider.
- Corrected the first gait back toward `DogTrot`: reduced the fore-aft surge that was reading as a gallop, restored turn authority from standstill, and added longitudinal damping so stopping the animal sheds leftover torso momentum more cleanly.
- Tightened the trot again: made the cycle quicker and flatter, reduced cadence surge/pitch further, fixed standstill turning with real idle yaw torque, and changed rider surge stress so only meaningful braking spikes count instead of every beat-to-beat speed fluctuation.
- Isolated zero-speed turning as its own problem: added an explicit idle pivot assist for very low planar speeds so the grounded torso can rotate in place cleanly instead of only leaning under torque.
- Began trot coupling: synced the mounted rider pose and first-person camera bob/tilt to the actual `DogTrot` gait phase so the human reads as carried by the animal rhythm instead of floating on an independent sine wave.
- Tightened trot coupling stability: low stability now needs a recent real instability event before ejection, and ragdoll launch speeds are capped so failures leave the seat more naturally instead of exploding upward or far out.
- Accepted the current `DogTrot` implementation as the first settled locomotion baseline and updated the project docs so future work can focus on `Trot` polish rather than reopening the foundation.
- Studied the referenced dog gait segment for torso motion, documented walk/trot/canter/gallop translation notes, expanded the gait engine to speed-select example dog gaits, and added a large on-screen gait label for playtesting.
- Rebalanced the example gait ladder for the real prototype speed envelope: lowered the speed thresholds for gait transitions and increased faster-gait drive scaling so `TROT`, `CANTER`, and `GALLOP` can actually be reached in play.
- Fixed a `WALK` self-lock issue: gait selection now uses a smoothed selection speed with hysteresis, `WALK` has more drive to climb out of itself, and the on-screen overlay now includes current speed for faster tuning.
- Switched playtesting to manual gait audition: the top overlay is now a clickable row of `IDLE`, `WALK`, `TROT`, `CANTER`, and `GALLOP`, with the selected gait shown in white and applied regardless of current speed.
- Decoupled manual gait audition from forward input side effects: pressing forward now adds propulsion with only modest cadence change, while rhythm strength and longitudinal damping stay much closer to the selected gait's identity.
- Redesigned `DogCanter` as its own low-power gallop/lope: added rear -> diagonal -> lead-fore torso pulse math, softened the canter scaling, and documented the canter as a controlled rolling gait rather than scaled trot.
- Reassessed `DogCanter` after playtest feedback: manual gait audition now assigns base travel speeds per gait, and canter has flatter pitch/vertical ride signals so it reads more like a calm high-speed lope than a bucking bronco.
- Restored `DogGallop` separation after canter tuning: gallop now has its own compress -> launch -> front-catch cadence and stronger audition speed/rhythm so it no longer reads as a fast trot.
- Rebuilt canter/gallop contact logic around explicit gait footfall pulses: canter now uses a three-beat 1-2-1 virtual contact pattern, while gallop uses rear-rear, flight, front-front, flight pulses with reduced central speed-hold force.
- Promoted the playtest-approved canter feel into `DogGallop` and tuned `DogCanter` down into a quieter lope with reduced roll, pitch, support, drive, and rider coupling.
- Reboot handoff note: current `DogCanter` still reads too much like gallop, and current `DogGallop` still reads too much like trot; the next session should address this gait identity split before any new feature work.
- Architectural pivot approved: we are moving from rigidbody-only buck/steer tuning toward a torso-first gait engine, with the rider treated as cargo on the animal body.
- Added documentation for the new locomotion direction, including future `AnimalMood`, `AnimalProfile`, `GaitProfile`, and torso-only gait timing driven by virtual contact forces.

## Logging Rules

- Add one short entry after each completed slice.
- Include what was built, how it was tested, and what is next.
