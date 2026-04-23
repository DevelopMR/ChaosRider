# Chaos Rider

Chaos Rider is a fast-built 3D Unity prototype focused on two things being genuinely funny:

- powerful animal-driven destruction
- violent, readable rider ragdoll outcomes

This repo is being developed in small vertical slices. Each slice should be playable, testable, and commit-worthy before we move on.

## Current Direction

- Engine: Unity 6
- Platform: Desktop prototype
- Core camera plan: two or more runtime camera modes instead of replay
- Core pillars:
  - unstable animal control
  - destructible environment reactions
  - rider ejection and ragdoll consequences

## Current Prototype Status

- `Slice 3` approved: mounted FPV and chase camera are working.
- `Slices 4-5` now add rider stability, ejection triggers, and runtime placeholder ragdoll ejection.
- Mounted FPV now hides overlapping placeholder body geometry in first-person to keep the ride readable.

## Working Rules

1. Build the smallest playable version of each system first.
2. Test each slice immediately in-editor.
3. Commit locally when a slice works.
4. Sync to GitHub after the local slice is stable.
5. Do not block gameplay on final art, UI polish, or advanced infrastructure.

## Project Tracking Files

- `Chaos_Rider.md`: original concept brief
- `PROJECT_PLAN.md`: current milestone and slice plan
- `BACKLOG.md`: ordered tasks not yet in active work
- `WORK_LOG.md`: session-by-session progress notes
- `DECISIONS.md`: architecture and scope decisions
