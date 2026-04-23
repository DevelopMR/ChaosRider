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
