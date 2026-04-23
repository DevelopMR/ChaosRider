# Chaos Rider

## CODEX Project Brief for Unity 6.4

### Working Title
**Chaos Rider**

### High Concept
Chaos Rider is a semi-realistic first-person physics comedy game where the player rides bucking animals through destructible environments. The comedy comes from believable physical impact, exaggerated rider instability, and absurd consequences.

The player is not a superhero. The animal is mostly in control. The player can influence direction and balance, but the main experience is surviving chaotic animal movement, crashing into objects, getting thrown, and watching the aftermath in instant replay.

### Target Experience
This is a rush concept prototype. The first goal is not polish, story, or perfect art. The first goal is a playable Unity 6.4 graybox prototype that quickly proves the comedy loop:

1. Pick an animal.
2. Ride from a first-person perspective.
3. Try to steer while the animal bucks and charges.
4. Ram into objects with semi-realistic physics.
5. Get injured, thrown, or killed depending on impact severity.
6. Watch an amusing instant replay.
7. See a damage/injury summary.

The tone should be semi-realistic comedy: physics should feel grounded enough to be funny, while UI text, replay captions, and outcomes can be ridiculous.

---

## Core Design Pillars

### 1. First-Person Animal Riding
The camera is mounted at the rider's head or chest position. The view should sell the feeling of being strapped to a powerful, unstable animal.

The camera should include:
- Bucking motion
- Tilt and roll
- Impact shake
- Brief disorientation after hard hits
- Optional nausea limiter values for testing

The player should feel like they are barely controlling the situation.

### 2. Semi-Realistic Physical Comedy
Objects should react according to the animal's size, speed, and mass.

Examples:
- A sheep bounces off a house.
- A sheep's horns might dent or damage a car.
- A bull can smash fences, barrels, and weak structures.
- A rhino can tear through walls and destroy the rider in one major impact.

The humor depends on physical contrast.

### 3. Animal Personality Through Physics
Each animal should have distinct movement and impact behavior.

Initial prototype animals:

#### Sheep
- Small and light
- Fast little bursts
- Bouncy collisions
- Low destruction power
- Can dent fragile objects or car panels with horns
- Funny because it tries hard but usually rebounds

#### Bull
- Medium-heavy
- Classic rodeo bucking
- Strong charge
- Breaks fences, doors, barrels, light structures, and weak vehicle parts
- Main baseline animal for prototype tuning

#### Rhino
- Very heavy
- High destruction power
- Low finesse
- Nearly unstoppable once charging
- Can smash through a house wall
- One high-speed impact can kill or instantly eject the rider

### 4. Rider Injury and Ejection
The rider should be physically vulnerable.

Prototype rider states:
- Mounted
- Losing balance
- Ejected
- Ragdoll
- Incapacitated / run ended

The rider can have an injury meter or simpler injury severity score. Hard impacts should produce comedic but clear consequences.

Example status messages:
- “Spinal optimism reduced.”
- “Insurance denied.”
- “Medical confidence low.”
- “You are no longer professionally attached to the animal.”
- “Rhino-related decision detected.”

Avoid detailed gore. Keep it slapstick/semi-realistic rather than graphic.

### 5. Instant Replay as a Core Feature
Instant replay is not optional polish. It is part of the main comedy loop.

When the rider is ejected, knocked out, or killed, the game should replay the last few seconds of the event.

Initial target:
- Record the last 10–20 seconds of gameplay data.
- Replay the crash in slow motion.
- Allow replay camera modes later, but start simple.

Minimum replay data:
- Animal transform history
- Rider transform history
- Important prop transform history
- Impact events
- Camera position/rotation history if useful

Prototype replay can initially use transform snapshots rather than a full deterministic physics rewind.

Replay captions should be supported.

Example replay captions:
- “Impact detected.”
- “Bad decision confirmed.”
- “Property damage rising.”
- “Medical bill multiplier: x4.”
- “Replay of last known dignity.”

---

## Asset Strategy

Use free pre-built assets wherever possible. This project should not depend on custom modeling during the prototype stage.

Potential free asset sources:
- Unity Asset Store free assets
- Sketchfab free/downloadable models, license permitting
- Kenney assets
- OpenGameArt
- Poly Pizza
- Mixamo-compatible humanoids when useful

Important implementation rule:
**Do not hard-code the project around any specific final model.**

Build systems around prefab slots and scriptable definitions so assets can be swapped later.

### Placeholder Asset Requirements
The prototype can start with crude or free assets:
- Animal model prefab
- Humanoid rider prefab
- Simple ragdoll rider prefab
- Fence prefab
- Barrel prefab
- Car prefab
- Shed/wall prefab
- House wall prefab
- Arena ground

Graybox geometry is acceptable for Milestone 1.

---

## Technical Direction

### Engine
Unity 6.4

### Language
C#

### Target Platform
Desktop prototype first.

### Camera
First-person rider view.

### Physics
Use Unity Rigidbody physics.

Prioritize fast prototype behavior over physically perfect simulation.

### Destruction Approach
For the first version, avoid complex real-time fracture systems.

Use simple destruction methods:
- Rigidbody impacts
- Break thresholds
- Health/damage values
- Prefab swaps
- Enable broken pieces on impact
- Disable intact object
- Apply explosion/impact force to debris pieces

This will be faster, more controllable, and funnier than trying to simulate true structural fracture immediately.

---

## Recommended Core Data Structures

### AnimalDefinition
Create a ScriptableObject or serializable definition for animal tuning.

Suggested fields:

```csharp
public string displayName;
public GameObject animalPrefab;
public float mass;
public float buckForce;
public float chargeForce;
public float turnResponsiveness;
public float chaosIntensity;
public float destructionPower;
public float riderStabilityModifier;
public float injuryMultiplier;
public Vector3 riderMountLocalPosition;
public Vector3 riderMountLocalRotation;
```

Purpose:
- Allows sheep, bull, and rhino to share one controller system.
- Makes tuning fast.
- Allows free assets to be swapped easily.

### RiderDefinition
Suggested fields:

```csharp
public string displayName;
public GameObject riderPrefab;
public float mass;
public float stability;
public float injuryResistance;
public float ragdollFlailMultiplier;
```

Initial rider body types:
- Small/light rider: unstable, dramatic ragdoll, low injury resistance
- Average rider: default baseline
- Large/heavy rider: more stable, heavier ragdoll, higher injury resistance

### DestructibleObject
Suggested fields:

```csharp
public float durability;
public float minimumBreakForce;
public float dentThreshold;
public float breakThreshold;
public float obliterateThreshold;
public GameObject intactPrefab;
public GameObject dentedPrefab;
public GameObject brokenPrefab;
public float propertyDamageValue;
```

### ImpactEvent
Suggested data:

```csharp
public Vector3 position;
public Vector3 normal;
public float impactForce;
public string impactedObjectName;
public string result;
public float timestamp;
```

---

## Core Systems

### 1. AnimalPhysicsController
Responsibilities:
- Apply forward charge force.
- Apply player steering influence.
- Add chaotic bucking impulses.
- Handle animal-specific movement tuning.
- Report impact force to destruction and injury systems.

Player control should feel indirect. The animal should have its own unstable movement.

Possible player inputs:
- A/D or left/right stick: influence turn direction
- W: encourage charge
- S: pull back / reduce charge slightly
- Space or trigger: balance/hold on attempt

### 2. RiderMountSystem
Responsibilities:
- Attach rider to animal mount point while mounted.
- Apply camera movement based on animal motion and bucking.
- Track rider stability.
- Trigger ejection when stability fails or impact is severe.

### 3. RiderRagdollSystem
Responsibilities:
- Keep rider non-ragdoll while mounted.
- On ejection, detach rider.
- Enable ragdoll physics.
- Apply throw force based on animal speed and impact direction.
- Notify replay and scoring systems.

### 4. DestructionSystem
Responsibilities:
- Receive impact data.
- Compare animal force/destruction power against object thresholds.
- Trigger dent, break, or obliterate response.
- Spawn debris or swap prefabs.
- Add property damage score.

### 5. InjurySystem
Responsibilities:
- Track rider injury severity.
- Determine fall severity.
- End run if injury exceeds threshold.
- Generate comedic injury summary text.

### 6. ReplayRecorder
Responsibilities:
- Continuously record recent transform snapshots.
- Maintain rolling buffer of last 10–20 seconds.
- Record important impact events.
- Provide data to replay playback system.

### 7. ReplayPlaybackSystem
Responsibilities:
- Freeze gameplay after run-ending event.
- Play back recent snapshots in slow motion.
- Show replay captions at key impacts.
- Return to summary screen.

### 8. ScoreSystem
Responsibilities:
- Track destruction value.
- Track survival time.
- Track distance traveled.
- Track number and severity of impacts.
- Track rider injury cost.
- Produce end-of-run summary.

Example end screen fields:
- Survival Time
- Property Damage
- Medical Bill
- Animal Used
- Peak Impact Force
- Dignity Remaining

---

## Prototype Milestones

## Milestone 1: Playable Graybox Chaos
Goal: prove the core comedy loop as fast as possible.

Must include:
- Unity 6.4 project setup
- One small test arena
- One animal controller, using placeholder animal body if needed
- First-person mounted camera
- Basic bucking movement
- Basic player steering influence
- Destructible graybox props
- Rider ejection
- Simple ragdoll or thrown body placeholder
- Instant replay of last few seconds
- Basic end summary

Recommended animal for first test:
**Bull**, because it is the best middle-ground for tuning.

## Milestone 2: Animal Differences
Goal: make the comedy come from animal contrast.

Add:
- Sheep definition
- Bull definition
- Rhino definition
- Animal select screen or debug selector
- Different mass, force, turning, bucking, and destruction values
- Animal-specific run summary text

Expected results:
- Sheep rebounds from large objects.
- Bull breaks normal obstacles.
- Rhino destroys major structures and endangers rider severely.

## Milestone 3: Rider Body Types
Goal: add variety to falls and replays.

Add:
- Small/light rider
- Average rider
- Large/heavy rider
- Different stability and ragdoll behavior
- Rider select screen or debug selector

## Milestone 4: Comedy Replay Polish
Goal: make crashes shareable and funny.

Add:
- Slow-motion replay camera
- Replay captions
- Impact freeze-frame moment
- Optional replay restart
- Better summary screen

## Milestone 5: Free Asset Replacement Pass
Goal: replace graybox visuals without changing systems.

Add:
- Free animal models
- Free rider models
- Better props
- Better arena environment
- Better object debris prefabs

Do not block core gameplay on final assets.

---

## Initial Scene Proposal

Create a small test arena called `PrototypeArena`.

Arena contents:
- Flat ground
- Fence line
- Barrels
- Hay bales
- Parked car placeholder
- Shed wall
- Simple house wall section
- A few traffic cones or signs
- Clear spawn point
- Run-end boundary

This arena should be small enough for fast testing and dense enough to cause crashes quickly.

---

## Physics Tuning Notes

Start with exaggerated but understandable physics.

Use force thresholds rather than exact real-world values.

Example relative tuning:

### Sheep
- Mass: low
- Charge force: low-medium
- Buck force: high relative to size
- Destruction power: low
- Bounce tendency: high

### Bull
- Mass: medium-high
- Charge force: high
- Buck force: high
- Destruction power: medium-high
- Bounce tendency: medium

### Rhino
- Mass: very high
- Charge force: very high
- Buck force: medium
- Destruction power: extreme
- Bounce tendency: very low

---

## CODEX Implementation Guidance

CODEX should work in small, testable increments.

Do not attempt to build the whole game in one pass.

Preferred workflow:
1. Create project structure and scripts.
2. Build the first animal controller.
3. Add mounted camera.
4. Add basic destructible props.
5. Add rider ejection.
6. Add replay recorder.
7. Add replay playback.
8. Add scoring and summary.
9. Add animal definitions.
10. Add rider definitions.

CODEX should explain each major change and keep systems modular.

Avoid fragile dependencies on exact model bones or specific imported assets during early prototype.

Use clean inspector-exposed fields so the user can tune behavior in Unity.

---

## Suggested Folder Structure

```text
Assets/
  ChaosRider/
    Scripts/
      Animals/
        AnimalDefinition.cs
        AnimalPhysicsController.cs
      Rider/
        RiderDefinition.cs
        RiderMountSystem.cs
        RiderRagdollSystem.cs
        InjurySystem.cs
      Destruction/
        DestructibleObject.cs
        DestructionSystem.cs
        ImpactEvent.cs
      Replay/
        ReplayRecorder.cs
        ReplayPlaybackSystem.cs
        TransformSnapshot.cs
      Scoring/
        ScoreSystem.cs
        RunSummary.cs
      UI/
        AnimalSelectUI.cs
        RunSummaryUI.cs
        ReplayCaptionUI.cs
      Game/
        GameManager.cs
        RunState.cs
    ScriptableObjects/
      Animals/
      Riders/
    Prefabs/
      Animals/
      Riders/
      Props/
      Arena/
    Scenes/
      PrototypeArena.unity
```

---

## First CODEX Task

Use this as the opening prompt to CODEX:

```text
We are building a rush Unity 6.4 prototype called Chaos Rider.

It is a semi-realistic first-person physics comedy game where the player rides bucking animals that crash into destructible objects. The first prototype should prove the core loop quickly: mounted first-person camera, animal bucking physics, destructible props, rider ejection, ragdoll/fall behavior, instant replay, and a simple run summary.

Please start with Milestone 1 only.

Build a graybox Unity prototype with:
1. A small test arena scene.
2. A bull-like placeholder animal controlled by Rigidbody physics.
3. First-person rider camera mounted to the animal.
4. Loose player steering influence, not full direct control.
5. Chaotic bucking impulses.
6. A few destructible props using simple break thresholds and prefab swaps or disabled/intact states.
7. Rider ejection on hard impact or stability failure.
8. A simple replay recorder that stores the last 10 seconds of transforms.
9. A slow-motion replay after ejection.
10. A basic summary showing survival time, property damage, peak impact force, and injury severity.

Keep the architecture modular. Use inspector-exposed fields. Do not depend on final art assets. Use placeholder geometry where needed. Do not implement sheep, rhino, advanced UI, or final assets until Milestone 1 works.

After inspecting or creating the initial project files, propose the exact scripts and scene objects needed before implementing.
```

---

## Non-Goals for Prototype 1

Do not prioritize:
- Full story
- Multiplayer
- Realistic gore
- Complex building fracture
- Advanced animation blending
- Perfect animal anatomy
- Final art direction
- Large open world
- Complicated mission system

Prototype 1 should be funny and playable before it is beautiful.

---

## Success Criteria

The prototype is successful if the user can:
1. Press Play.
2. Ride a bucking animal in first person.
3. Crash into objects.
4. See objects react differently based on impact strength.
5. Get thrown from the animal.
6. Watch the crash replay.
7. Laugh at the summary screen.

