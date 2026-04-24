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
  - torso-first animal locomotion
  - destructible environment reactions
  - rider ejection and ragdoll consequences
  - rider-as-cargo comedy

## Current Prototype Status

- `Slice 3` approved: mounted FPV and chase camera are working.
- `Slices 4-5` now add rider stability, ejection triggers, and runtime placeholder ragdoll ejection.
- Mounted FPV now hides overlapping placeholder body geometry in first-person to keep the ride readable.
- Steering tuning exposed the current limit of the rigidbody-only approach.
- The project is now pivoting toward a `Gait Engine` with virtual leg/contact forces driving a torso body.
- Animal `Mood` is planned as a future behavior modifier; for now animals remain obedient.

## New Locomotion Direction

The animal is the true gameplay body.
The rider is cargo with opinions.

That means:

- the animal torso is the root locomotion object
- the player influences the animal instead of directly rotating themselves
- gait timing should create recognizable body motion even with no visible legs
- rider instability, camera feel, and ragdoll should emerge from that torso motion

## Planned Locomotion Architecture

- `AnimalProfile`
  - mass, body proportions, shoulder bias, hind-drive bias, obedience baseline
- `AnimalMood`
  - stubbed for later states like agreeable, angry, petulant, protective
- `GaitProfile`
  - gait timing, stance windows, per-contact force patterns, body pitch/roll biases
- `GaitEngine`
  - applies torso-driving forces through four virtual contact points
- `AnimalLocomotionController`
  - handles desired speed, heading influence, and active gait selection

## First Gait Target

We will start with a torso-only dog reference gait, likely `Trot`, because:

- it has a clear and recognizable rhythm
- diagonal pairing is easier to prototype than a full gallop
- it should immediately feel less like spring oscillation and more like animal locomotion

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
