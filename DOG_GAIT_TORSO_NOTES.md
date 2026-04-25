# Dog Gait Torso Notes

Reference segment:

`https://youtu.be/CR0IJsvoLic?t=564`

Observed window:

9:24 to 12:19

Important caveat:

The video changes playback speed for human understanding. Treat the visual timing as phase relationships and body-shape information, not as absolute real-time speed. In code, cadence should be driven by animal speed and gait profile values.

## Goal

Translate dog gaits into torso-first motion for Chaos Rider.

We are not animating legs.
We are using virtual contact phases to make the torso, rider, and camera feel like they are riding a mammalian quadruped.

## Shared Torso Language

Useful motion channels:

- `vertical bob`: how much the torso lifts and settles per beat
- `diagonal roll`: how much support shifts across diagonal pairs
- `fore-aft pitch`: how much shoulders and hips trade height
- `surge`: how much the torso pushes forward or compresses
- `spine flex`: visible bend/extension along the body, currently approximated with torso pitch/surge because the prototype uses one rigidbody
- `suspension`: how much the body briefly unloads between contacts

## Walk

Feel:

Stable, deliberate, four-beat weight transfer.

Torso motion:

- low vertical bob
- visible left-right sway
- small fore-aft pitch
- almost no suspension
- body stays controlled and grounded

Force interpretation:

- four separated contact phases
- long stance duration
- gentle support pulses
- low drive force
- good low-speed steering authority

Prototype use:

Best for very low speed and careful turning.

## Trot

Feel:

Brisk, symmetrical, diagonal two-beat gait.

Torso motion:

- level shoulders compared with canter or gallop
- modest vertical bob at a faster beat
- modest diagonal roll
- very little fore-aft lunge
- minimal spine flex
- small suspension or lightness between diagonal support beats

Force interpretation:

- diagonal pairs alternate:
  - front-left plus rear-right
  - front-right plus rear-left
- medium stance duration
- hindquarters provide most propulsion
- front contacts carry weight and help steer/brake
- surge must stay restrained or the gait reads as canter/gallop

Prototype use:

This is the accepted baseline gait and should remain the parent model for animal locomotion.

## Canter

Feel:

Asymmetrical rolling gait. Think of it as a low-power gallop before the dog digs in with the back and reaches full extension.

Torso motion:

- more fore-aft pitch than trot, but much less than gallop
- shoulders and hips rock through an uneven three-beat pattern
- one side feels like the leading side
- more surge/compression than trot, but it should not feel like a launch
- low, controlled suspension
- spine flex is suggested, not fully expressed yet

Force interpretation:

- rear contact starts the cycle and gives a soft push
- diagonal middle beat carries the torso forward
- leading front contact catches the body and finishes the roll
- support is less symmetrical than trot
- the torso should feel like rear -> carry -> catch, not bounce -> skip -> leap

Prototype use:

Good future bridge between controlled trot and chaotic gallop.

Current code interpretation:

- `DogCanter` uses separate canter pulse math instead of scaled trot cadence
- phase targets are rear beat, diagonal carry, and lead-fore catch
- canter should be rolling and slightly asymmetric without becoming a bucking or galloping motion
- manual gait audition gives canter a base forward speed so it is tested as a calm traveling gait, not as a canter-in-place

## Gallop

Feel:

Fast, stretched, compress-and-launch gait.

Torso motion:

- strongest fore-aft surge
- strongest pitch
- visible compress/extend body rhythm
- greatest suspension
- highest need for spine flex if we later split the visual torso

Force interpretation:

- rear contacts load and drive
- front contacts catch and redirect
- stance windows are shorter
- support pulses are stronger but less continuous

Prototype use:

Use for high-speed chaos, impacts, destruction, and rider danger.

## Spine Flex Notes

The current animal is one rigidbody torso, so true spine flex is not possible yet.

For now:

- walk and trot can work with one torso
- canter and gallop will eventually benefit from a split visual torso or spine pose layer
- physics collision should remain on the core torso so crashing into structures stays stable

Possible later model:

- keep one main rigidbody for gameplay collision
- add front and rear visual torso segments
- drive the visual segments with gait phase
- keep the rider mounted to the stable core or a filtered seat anchor

## Current Code Direction

The gait engine now supports example dog gaits:

- `DogWalk`
- `DogTrot`
- `DogCanter`
- `DogGallop`

The animal selects gait by current planar speed.

The rider and mounted camera read exported ride signals from the active gait instead of assuming every moving state is trot.

Current priority:

- preserve the accepted `DogTrot` feel
- use walk/canter/gallop as readable examples
- avoid adding visual spine complexity until the gameplay benefit is clear
- keep canter as a restrained traveling lope while giving gallop its own compress -> launch -> front-catch cycle, not a faster trot sine wave
