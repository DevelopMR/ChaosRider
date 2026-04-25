# Chaos Rider Project Plan

## Re-Alignment

Replay is removed completely.

The prototype now stands on these primary pillars:

1. Animal torso locomotion must feel recognizable, heavy, and readable.
2. Rider ejection and ragdoll fallout must be a core source of comedy.
3. Camera options must help the player read chaos in real time instead of relying on replay.
4. We ship in small playable slices and test every slice before expanding scope.

## Non-Goals For Current Plan

- replay systems
- deterministic rewind
- complex fracture simulation
- advanced animal animation fidelity
- multiple polished arenas
- final art pipeline
- full skeletal leg rigs for the prototype

## Camera Strategy

Milestone 1 should support at least two camera modes:

1. `Mounted FPV`
   Tight first-person camera attached near rider head or chest.
   Best for panic, instability, and impact intensity.

2. `Chase Cam`
   Third-person follow camera tuned to keep the animal, rider, and destruction readable.
   Best for understanding collisions and ragdoll launches.

Optional later:

3. `Impact Cam`
   A temporary dynamic offset camera used briefly after severe collisions or ejection.

## Build Strategy

We will work in short vertical slices.
Each slice should end with:

- a playable in-editor test case
- a short note in `WORK_LOG.md`
- a local git commit

GitHub sync happens after the slice is stable locally.

## Milestone 1

Goal:
Prove a funny graybox loop with one animal torso, one arena, gait-driven locomotion, ejection, ragdoll, and camera switching.

Definition of done:

- player can press Play and immediately control a bull-like animal
- player influence should feel like directing an animal, not driving a toy vehicle
- two camera modes are switchable during play
- animal movement feels like a torso carrying a familiar gait rhythm
- several props react differently to impact severity
- rider can be ejected by instability or hard collisions
- ragdoll outcome is readable and satisfying
- run ends with a basic summary

## Slice Plan

### Slice 0: Project Spine

Deliverables:

- local git repo initialized
- Unity `.gitignore`
- markdown tracking files in place
- agreed Milestone 1 scope

Test:

- repo structure is clean and tracking docs are readable

### Slice 1: Core Scene And Game Loop Skeleton

Deliverables:

- `PrototypeArena` graybox scene
- spawn point
- ground plane and arena bounds
- simple game state flow: start, active run, ended run

Test:

- scene opens and enters play cleanly
- player spawns in consistent location

### Slice 2: Animal Controller Baseline

Deliverables:

- placeholder bull rigidbody
- forward drive
- loose steering influence
- bucking impulses
- tuning values exposed in inspector

Test:

- animal moves with unstable, funny behavior
- control feels indirect, not precise

### Slice 3: Camera Modes

Deliverables:

- mounted first-person camera
- chase camera
- runtime camera switch input
- basic camera damping and impact readability

Test:

- both views function during active riding
- collisions remain readable from at least one view

### Slice 4: Rider Mount Stability

Deliverables:

- mounted rider representation
- balance or stability model
- visual instability driven by movement and impact
- ejection trigger inputs and thresholds

Test:

- rider can lose stability during aggressive movement
- mounting state transitions cleanly to ejection
- `Status: Implemented`

### Slice 5: Ragdoll Ejection

Deliverables:

- ragdoll rider prefab or placeholder
- detach and ragdoll activation flow
- launch force based on animal velocity and impact
- end-run trigger on severe rider outcome

Test:

- ejection is reproducible and physically believable enough to be funny
- ragdoll does not explode from setup issues
- `Status: Implemented`

### Slice 6: Destructible Props

Deliverables:

- destructible fence
- destructible barrel
- destructible wall
- threshold-based intact to broken response
- property damage values

Test:

- different impacts produce different object outcomes
- bull can meaningfully alter the arena

### Slice 6A: Locomotion Pivot

Deliverables:

- `AnimalMood` stub
- `AnimalProfile` and `GaitProfile` data structures
- `GaitEngine` prototype using four virtual contact points
- first torso-only reference gait using dog timing data

Test:

- locomotion feels impulse-driven rather than oscillatory
- rider motion reads as being carried by a living torso
- `Status: Foundational baseline reached`

### Slice 6B: Trot Polish

Deliverables:

- polish the settled `DogTrot` baseline without destabilizing control
- reduce residual lean/twist and remaining odd rider energy spikes
- improve mounted readability of the trot beat
- keep zero-speed turning intact
- add speed-based transitions through example dog gaits

Test:

- trot still reads as a trot
- rider stays coupled without random launches
- zero-speed turning remains reliable
- on-screen gait label matches the active movement state
- this baseline remains suitable as the parent model for future animals

### Slice 6C: Canter/Gallop Identity Fix

Deliverables:

- preserve the accepted `DogTrot` feel
- reassess `DogCanter` and `DogGallop` from the current playtest checkpoint
- keep canter as a calm, high-speed lope rather than a broken gallop
- make gallop visibly distinct from trot, with stronger compress/launch/catch character
- use the manual gait selector for direct A/B testing independent of speed transitions

Known current issue:

- current `DogCanter` still reads more like a gallop
- current `DogGallop` still reads more like a trot
- first task after reboot should be this gait split, not destruction or broader feature work

### Slice 7: Damage And Injury Summary

Deliverables:

- track survival time
- track property damage
- track peak impact force
- track injury severity
- simple end-of-run summary UI

Test:

- run summary reflects what happened in the play session

### Slice 8: Tuning Pass

Deliverables:

- tune animal force, destruction thresholds, and ejection thresholds together
- remove obvious frustration spikes
- improve readability of chaos without making it safe

Test:

- three consecutive runs produce funny and readable outcomes

## Structural Rules

### Keep Systems Simple

- Use inspector-driven components.
- Prefer direct references over elaborate event architecture early on.
- Only generalize when a second real use case appears.

### Separate Critical States

Treat these as distinct states instead of over-unifying too early:

- mounted rider
- ejected ragdoll rider
- destructible prop intact state
- destructible prop broken state

### Torso First

For locomotion, the animal torso is the physical root object.

- the rider does not drive themselves
- the rider influences the animal
- the animal decides the body motion
- the rider experiences the result

### Optimize For Tuning Speed

If a system is hard to tune in the Inspector, it is too expensive for this phase.

## Initial Technical Modules

- `GameManager`
- `AnimalDefinition`
- `AnimalProfile`
- `AnimalMood`
- `GaitProfile`
- `GaitEngine`
- `AnimalLocomotionController`
- `AnimalPhysicsController`
- `RiderMountSystem`
- `RiderRagdollSystem`
- `DestructibleObject`
- `InjurySystem`
- `ScoreSystem`
- `RunSummaryUI`
- `CameraModeController`

## Immediate Next Step

Fix the `DogCanter` / `DogGallop` identity split first:

- preserve the current controllable, readable `DogTrot`
- use the manual gait selector to compare `CANTER` and `GALLOP` directly
- reduce canter's gallop-like launch/roll
- make gallop stop reading as trot
- update `DOG_GAIT_TORSO_NOTES.md` with whatever finally works

This keeps the project on a stable locomotion foundation instead of reopening the architecture question.
