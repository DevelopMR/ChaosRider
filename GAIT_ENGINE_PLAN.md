# Gait Engine Plan

## Goal

Build a functional torso-only `Gait Engine` for a dog that produces recognizable animal motion from force timing alone.

No visible legs.
No IK.
No skeleton dependency.

The rider is cargo with opinions riding the torso.

## Core Principle

The animal torso is the locomotion root body.

- the player influences the animal
- the animal decides the body motion
- the rider experiences the consequences

We are not trying to recreate veterinary-grade locomotion.
We are trying to create torso motion that humans instantly recognize as animal motion.

## Prototype Scope

We are building:

- four virtual contact points
  - front-left
  - front-right
  - rear-left
  - rear-right
- gait timing data
- stance and swing phase logic
- force pulses into a rigidbody torso
- body roll, pitch, surge, and heading response
- rider and camera response to that motion

We are explicitly not building:

- visible leg animation
- terrain-following foot placement
- per-animal skeleton rigs
- deep leg-joint simulation

## Planned Architecture

### `AnimalProfile`

Defines the animal body and high-level behavior.

Expected fields:

- species id
- display name
- mass
- torso length
- torso height
- shoulder bias
- hind-drive bias
- obedience baseline
- future `AnimalMood`

### `AnimalMood`

Stub now, behavior axis later.

Planned future values:

- `Obedient`
- `Agreeable`
- `Angry`
- `Petulant`
- `Protective`

For the first pass, the animal remains obedient.

### `GaitProfile`

Defines one gait as timing and force data.

Expected fields:

- gait type
- cycle duration
- phase offsets per virtual leg
- stance duration per leg
- vertical support force
- forward drive force
- pitch bias
- roll bias
- surge bias
- steering responsiveness bias

### `GaitEngine`

Owns the gait cycle and applies timed forces.

Responsibilities:

- advance normalized gait phase
- determine stance vs swing for each virtual leg
- apply forces at virtual contact points
- expose debug data for active leg phases
- output body-state signals for rider/camera systems

### `AnimalLocomotionController`

Wraps gait with gameplay intent.

Responsibilities:

- desired speed
- desired heading
- gait selection
- obedient response to input
- later integration with mood

## First Implementation Target

### `DogTrot`

Why start here:

- highly recognizable rhythm
- diagonal pair timing is simpler than canter or gallop
- should immediately feel less like a spring mattress
- good proof that torso-only motion can work before we adapt it to heavier animals

## Force Model

Each virtual leg contributes only when in stance.

During stance, a leg may contribute:

- vertical support/load force
- forward propulsion force
- slight lateral loading effect
- pitch and roll contribution through where the force is applied

During swing, support force is removed.

The result should be:

- front/rear load transfer
- left/right body sway
- forward surge and compression
- recognizable gait rhythm

Important rule:

No sine-wave bob as the primary motion driver.
Any bob should be a consequence of timed support and propulsion, not the source of the gait itself.

## Implementation Sequence

### 1. Data Pass

Create a minimal, usable representation of gait timing.

Deliverables:

- `GaitType` enum
- normalized gait cycle representation
- phase offsets for all four virtual legs
- first `DogTrot` timing values

### 2. Profile Pass

Create profile data structures.

Deliverables:

- `AnimalProfile`
- `AnimalMood` stub
- `GaitProfile`

### 3. Engine Pass

Implement the first `GaitEngine`.

Deliverables:

- phase progression
- stance/swing evaluation
- per-contact force application
- debug visualization hooks

### 4. Torso Motion Pass

Make the rigidbody torso feel alive.

Deliverables:

- recognizable weight transfer
- diagonal trot rhythm
- front/rear pitch changes
- left/right sway without springiness

### 5. Rider/Cargo Pass

Reconnect rider experience to gait-driven motion.

Deliverables:

- rider stability tested against gait rhythm
- mounted camera retested against gait motion
- ejection and ragdoll retested against gait impulses

## Reference Material

### Primary Working References

#### Dog gait timing video

`https://www.youtube.com/watch?v=grGYAnFae7c`

Why it matters:

- shows gait timing visually
- useful for phase relationships
- useful for identifying recognizable rhythm

#### AKC gait article

`https://www.akc.org/expert-advice/sports/looking-over-the-gait/`

Why it matters:

- practical and readable
- translates gait into body behavior
- good source for thinking about propulsion, carriage, and balance without needing full leg math

### Secondary Reference

#### NAHF canine gait article

`https://www.nahf.org/article/canine-gait`

Why it matters:

- rich real-world knowledge
- useful validation source
- useful as a later comparison point if we need more confidence in the rhythms

Why it is secondary:

- high-level and sophisticated
- potentially hard to translate directly into torso-force data
- we should avoid deep analysis of it right now and return later only if needed

## Explicit Non-Reference

We are intentionally avoiding deep leg-skeleton mathematical references for the prototype.

Reason:

- they pull us toward leg reconstruction
- that is exactly what we want to avoid in this rush phase
- our immediate goal is torso recognition, not anatomical completeness

## Success Criteria

The first pass is successful if:

- the torso no longer feels like a loose spring mattress
- motion rhythm feels recognizably animal even without legs
- the rider feels carried by a living body
- `DogTrot` feels distinct and readable
- the architecture clearly supports later animal profiles and gait profiles

## Current Status

This first pass is now considered successful enough to serve as the parent locomotion baseline.

Accepted baseline qualities:

- the torso reads as a trotting dog often enough to be useful
- zero-speed turning is solved well enough for continued iteration
- rider and mounted camera now couple to gait phase
- rider stability and ejection are stable enough for repeated testing

Immediate focus from here:

- polish `DogTrot`
- preserve the stable baseline
- avoid destabilizing control while improving feel

## Immediate Next Step

Implement a minimal torso-only `DogTrot` using:

- one `AnimalProfile`
- one `GaitProfile`
- one `GaitEngine`
- four virtual contact points
- rigidbody torso force application

Only after that works should we expand toward:

- additional gaits
- bull adaptation
- mood-driven locomotion variation
