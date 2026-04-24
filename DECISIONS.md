# Decisions

## 2026-04-23

### Removed Replay

Replay is cut from the project.

Reason:
It adds high implementation cost without helping the core prototype pillars as much as destruction and ragdoll do.

### Added Multiple Camera Modes

Milestone 1 will support at least:

- mounted first-person
- chase camera

Reason:
We still need both immersion and readability, and camera switching is much cheaper than replay.

### Prototype Priority Order

Priority order for the prototype is:

1. animal controller
2. camera readability
3. rider ejection and ragdoll
4. destructible props
5. scoring and summary

Reason:
This sequence gets us to a playable and tunable loop fastest.

### Workflow Rule

We will build in small vertical slices, test each slice, then commit locally before syncing to GitHub.

Reason:
This keeps risk low and progress visible.

### Mounted First-Person Rendering

Mounted first-person view hides overlapping placeholder rider and bull body geometry.

Reason:
For the prototype, first-person readability is more important than always rendering the full placeholder mesh.

### Ejection Camera Behavior

When the rider ejects, the camera should move to chase-style follow on the ragdoll instead of staying at the seat.

Reason:
The crash is the payoff, so the camera needs to preserve readability at the moment of failure.

### Torso-First Locomotion Pivot

The animal torso is now treated as the true locomotion root body, and locomotion will be driven by gait timing rather than generic oscillation.

Reason:
The rigidbody-only tuning path produces springy motion that does not read as mammalian.

### Rider-As-Cargo Rule

The rider is cargo with opinions.

Reason:
This captures the actual gameplay fantasy: the player influences the animal, but the animal body owns the motion.

### Mood Stub

`AnimalMood` will be part of the architecture now, but animals remain obedient in the first implementation.

Reason:
Mood is a strong future design axis, but should not block the first gait engine pass.
