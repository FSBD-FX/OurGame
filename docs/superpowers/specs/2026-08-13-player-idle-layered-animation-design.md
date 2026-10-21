# Thread-Spool Bearer Layered Idle Animation Design

**Date:** 2026-08-13  
**Status:** Rejected at the mandatory separation preflight on 2026-08-13
**Scope:** Replace only the rejected `action-idle-001` production method

> This approach is superseded by
> `2026-08-13-player-complete-layered-master-design.md`. A single composited
> identity image could not yield complete, unambiguous hidden parts: the local
> masks clipped contours, mixed rigid hardware into cloth, and could not prove
> that occluded limbs or lining were complete. No animation was rendered and no
> runtime asset was published from this approach.

## 1. Problem

`action-idle-001` passed geometric frame checks but failed visual review. Its
six poses were independently redrawn, so the head, torso, armor, weapon, and
mantle changed shape between frames. Playback looked like a still image being
stretched rather than one solid character moving. The mantle also lacked a
readable cloth chain: attachment, delayed drag, transmitted wave, and rebound.

The replacement must animate accepted pixels locally. It must not ask an image
model to redraw the whole character for every frame.

## 2. Chosen approach

Create a free, deterministic layered rig from the accepted body and lantern:

1. Fixed boot plates hold the ground contact while rigid chassis plates hold
   the head, torso, chest spool, arms, and leg armor. Overlap at the ankle and
   hip joints permits a 1-2 pixel settle without scaling any plate.
2. A rigid weapon plate provides a small counterweight rotation.
3. One continuous mantle texture uses three smoothly blended control regions:
   root, middle, and tip. These are deformation influences, not three visible
   cardboard pieces.
4. A small back-core light overlay supplies the energy pulse.
5. The accepted lantern remains a separate visual layer and follows the body
   with delayed spring motion.

The authoring rig renders twelve transparent review frames. Accepted frames can
then enter the existing sprite-atlas pipeline; the authoring rig does not
replace gameplay nodes or collision.

### Alternatives considered

- **Another AI contact sheet:** fastest, but repeats the identity drift and
  rubber-stretch failure. Rejected.
- **Three rigid mantle fragments:** deterministic, but seams and hinge-like
  motion would make cloth look mechanical. Rejected.
- **Continuous weighted mantle mesh:** more setup, but preserves the painted
  material while producing controllable drag and rebound. Selected.

## 3. Timing

The first replacement attempt is exactly **12 frames at 8 FPS**, for a
**1.5-second seamless loop**.

- Frames 1-4: quiet breathing and continuous ambient breeze.
- Frames 5-8: a medium back-core pulse travels from mantle root to its tip.
- Frames 9-12: mantle rebound and lantern overshoot settle into frame 1.

If the approved motion cannot breathe or settle cleanly at this density, the
same rig and key poses expand to **16 frames at 8 FPS**. That fallback adds
in-betweens; it does not redraw the character or consume image quota.

This specification supersedes the earlier six-frame idle count only. The other
five action contracts remain unchanged until reviewed separately.

## 4. Motion contract

### 4.1 Rigid body

- Keep both boot plates planted on one baseline.
- Move the rigid chassis plates vertically by no more than 2 output pixels,
  with overlap at the joints hiding the motion against the fixed boots.
- Permit a very small whole-body settling rotation only if the feet remain
  visually locked.
- Never scale, squash, stretch, mesh-warp, or independently resize the head,
  torso, chest spool, arms, or legs.
- Preserve the right-facing silhouette and all accepted identity markers.

### 4.2 Weapon

- Treat the half-bell cleaver as one rigid plate.
- Use a subtle counterweight rotation opposite the body's vertical momentum.
- Keep the grip visually attached and the blade clear of the frame boundary.
- Do not deform the blade or change its width.

### 4.3 Mantle

- Keep the shoulder/root attachment visually pinned.
- Apply a low-amplitude ambient breeze throughout the loop.
- Let the middle region follow the root one frame late and the tip follow the
  middle one frame late.
- During the medium energy pulse, send one visible wave from root to middle to
  tip. The tip overshoots once, then returns without snapping.
- Use smooth skinning weights across one connected mantle image so no internal
  seams or rigid hinges appear.
- Preserve folds, holes, edge wear, and color placement; only their positions
  may deform.

### 4.4 Back core and chest spool

- The back core brightens briefly to initiate the pulse.
- The chest spool may make one restrained luminance pulse but cannot change
  diameter or shape.
- Glow remains a separate overlay and cannot bleach the body silhouette.

### 4.5 Floating lantern

- Keep the lantern separate from every body frame.
- Follow the body along a slow, small arc with spring-like delay.
- On the energy pulse, rise briefly, overshoot, and settle after the mantle
  wave rather than in perfect synchrony with it.
- Do not orbit the head continuously or collide with the body silhouette.
- The tether is a runtime visual connection from back core to lantern; it is
  not baked into the body atlas.

## 5. Local production flow

1. Copy the accepted transparent body reference into an external idle-rig
   workspace; never edit the canonical reference in place.
2. Create explicit alpha masks for rigid body, weapon, mantle, and core glow.
3. Build an occlusion-safe body plate beneath the mantle. If exposed areas
   cannot be reconstructed cleanly with local painting and nearby accepted
   pixels, stop and show the defect before requesting any generated edit.
4. Author the continuous mantle mesh and the twelve deterministic transforms.
5. Render body frames and a separate composite review preview containing the
   lantern and tether.
6. Run local alpha, bounds, anchor, identity, and loop checks.
7. Show the animated composite and representative frames to the user. Publish
   nothing until explicit approval.

No paid API or included image-generation quota is authorized for this flow.
Any proposed generated repair requires a new explanation and explicit approval.

## 6. Outputs and publication boundary

Work files remain outside the repository under:

`E:\OurGame\ArtProduction\thread-spool-bearer-v1\actions\idle-layered`

The workspace contains masks, source plates, rig metadata, rendered body
frames, composite review frames, and a preview GIF. Rejected material remains
there as evidence.

Only an explicitly approved result may replace the idle row in the future
player atlas. The lantern continues to use:

`assets/player/thread-spool-bearer/lantern.png`

No runtime asset, atlas, manifest, scene, or gameplay file changes during the
local review stage.

## 7. Acceptance criteria

Automated checks are necessary but not sufficient.

### Technical checks

- Exactly 12 transparent body frames and 12 composite review frames.
- Exactly 8 FPS metadata and a 1.5-second preview loop.
- Identical canvas size and output origin for every frame.
- Feet vary by no more than 1 pixel vertically.
- No empty frame, clipping, edge contact, alpha island, or magenta fringe.
- Rigid body and weapon layers use translation/rotation only; scale is exactly
  `(1, 1)` in all frames.
- Frame 12 to frame 1 has no position, rotation, brightness, or velocity snap.

### Visual checks

- Head, torso, chest spool, limbs, and weapon retain constant volume.
- Playback does not resemble image stretching or morphing.
- Mantle root is pinned; middle and tip visibly lag.
- The energy wave travels root-to-tip once and rebounds naturally.
- Ambient breeze remains visible before and after the pulse.
- Lantern follows a delayed arc and settles later than the mantle.
- The motion remains readable at approximately 96-pixel body height.
- The user explicitly accepts the animated preview.

Failure of any visual check rejects the candidate even when every geometric
metric passes.

## 8. Failure handling

- If local layer separation damages the accepted identity, stop before rigging.
- If the continuous mesh exposes unreconstructable holes, show a still-frame
  comparison and request direction; do not hide the defect with blur or glow.
- If 12 frames feel rushed but the motion path is correct, expand the same rig
  to 16 frames at 8 FPS.
- If the motion path itself is wrong, revise the rig and timing before adding
  frames.
- Never regenerate automatically and never reinterpret an automated QC pass as
  visual approval.
