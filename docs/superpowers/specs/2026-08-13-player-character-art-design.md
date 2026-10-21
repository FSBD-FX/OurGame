# Player Character Art Design

- Status: approved written visual specification
- Date: 2026-08-13
- Project: `E:\OurGame\Knight`
- Engine target: Godot 4.7.1 Mono, C#/.NET 9
- Scope: player base design and first formal animation-asset pipeline

## 1. Purpose

Replace the technical prototype's procedural player visual with an original,
production-oriented character design that supports the current movement,
four-direction combat, mechanical thread, and future Ember and Echo abilities.

The character enters the story as a clockwork knight. Gravekeeper and nameless
child identities appear gradually through equipment, light behavior, material
changes, and memory-state details. The first asset pass must establish one
canonical visual identity before any animation rows are generated.

This design takes genre-level inspiration from polished 2D action exploration
games but must not reproduce Hollow Knight's horned or insect-mask silhouette,
small black-cloak body proportions, or long nail-like primary weapon.

## 2. Approved Character Direction

### 2.1 Primary silhouette

The player is the **Thread-Spool Bearer**, a broad, low-centered clockwork
knight. The body is mechanically substantial without becoming a slow giant.
The torso is wider than the head and legs, and a large circular spool assembly
dominates the chest.

At gameplay distance, the character must be recognized by five primary forms:

1. A sealed bone-white ceramic head with a narrow horizontal observation slit.
2. A broad ceramic torso with a large circular mechanical thread spool.
3. A short-handled half-bell cleaver.
4. A floating crown lantern above the head.
5. An asymmetric, damaged gravekeeper mantle.

No secondary detail may obscure or replace these forms.

### 2.2 Head and identity language

The head is a closed ceramic instrument housing, not a face or mask. It has no
horns, antennae, insect mandibles, or protruding crown shape. A narrow horizontal
slit contains a restrained cyan-green glow.

During normal play, the slit reads as a single quiet instrument window. During
moon-silver memory scenes, two small light points may briefly resolve inside the
slit, suggesting the consciousness of the nameless child without changing the
outer shell.

### 2.3 Chest spool and thread system

The chest spool is the primary identity marker. It consists of a dark recessed
core, aged-brass rim, visible winding structure, and a limited number of readable
guides or calibration marks. Mechanical thread exits through controlled guide
points rather than appearing as magic emitted from empty space.

The spool must remain visually readable in idle, run, jump, attack, and thread
cast poses. It must not be covered by the mantle, arms, or effects for most of an
animation.

### 2.4 Weapon

The primary weapon is the **Half-Bell Cleaver**: a short-handled, thick-backed
blade shaped like the skirt of an old bell split lengthwise.

- Horizontal attacks read as broad, weighty sweeps.
- Up attacks present a clear rising edge.
- Down attacks lead with a compact, heavy bell-shaped mass.
- Impact language evokes striking an old bell.

The weapon may use a tapered needle-like tip or thin engraved edge as secondary
language, but it must not become a long nail, rapier, needle sword, or visually
light primary weapon.

### 2.5 Floating crown lantern

A compact energy core is embedded in the upper back. It drives a lantern that
floats above the head without touching the shell. A luminous mechanical thread
or energy tether makes the relationship between back core and lantern explicit.

The default composition is centered above the head like a detached mechanical
crown. The lantern is not rigidly locked to the player:

- Idle: slight delayed sway and a restrained breathing-like glow.
- Run: small trailing lag opposite movement.
- Jump: short upward float, then settle.
- Dash: lower and trail backward without hiding the player silhouette.
- Hurt: brief destabilized offset, followed by recovery.
- Ember cast: rise, unfold or open, and intensify its light.

The lantern is presentation-only unless gameplay explicitly activates Ember.
Its visual motion cannot change collision, damage, cooldown, or targeting rules.

### 2.6 Mantle and secondary silhouette

The mantle is asymmetric and damaged. Its exterior begins in muted oxidized
copper-green; the underside and torn lower regions transition toward near-black.
Thin, irregular points are allowed along damaged edges. The mantle supplies
secondary motion and a gravekeeper identity without turning the whole character
into a tiny black-cloaked silhouette.

## 3. Visual Hierarchy

Previously avoided motifs are permitted through controlled hierarchy rather
than prohibited outright.

### Level 1: gameplay-distance identity

- Horizontal-slit head
- Large chest spool
- Half-bell cleaver
- Floating crown lantern
- Broad, low-centered body

These must remain legible at the final in-game display size.

### Level 2: mid-distance language

- Near-black mantle lining and torn tapered edges
- Needle-like thread guides, blade tip, back rails, and joint pins
- Asymmetric mantle and lantern motion

Thin or dark elements support the design but cannot replace the primary forms.

### Level 3: close-view craftsmanship

- Ceramic glaze wear and restrained cracks
- Oxidized brass, rivets, calibration marks, fine tubes, and gears
- Back-core construction and thread-routing details

Fine detail is concentrated around the chest spool, joints, weapon mechanism,
and back core. Ceramic limbs and large body planes retain quiet areas. The
character must not be uniformly covered with micro-components.

## 4. Material and Rendering Language

Use a **storybook painterly treatment with clear action-game contours**.

- Strong, hand-drawn outer contour and readable internal separations.
- Soft volume lighting rather than flat cel shading.
- Bone-white aged ceramic, not clean plastic or exposed bone.
- Aged brass with selective oxidation, not uniform shiny gold.
- Damp, worn cloth with a copper-green exterior and dark inner layer.
- Local realistic material detail, but not photographic rendering.
- Limited texture frequency so movement remains readable at gameplay scale.

The first base should be rendered at high resolution and downsample cleanly for
the game. It must preserve a complete silhouette on a flat chroma-ready
background. It must not request or fake transparency during generation.

## 5. Colour-State Progression

The same geometry and material rules persist across the three emotional states.

### Bronze-green dreamscape — normal state

Bone-white ceramic, oxidized copper-green mantle, aged brass, restrained
cyan-green slit and thread light. This is the canonical base.

### Rust-red funeral bell — high-pressure state

The environment supplies most of the red shift. Character changes are limited
to warmer reflected light, sparks, stronger shadow, and stressed core glow. Do
not recolour the whole character red.

### Moon-silver elegy — memory state

Lower saturation, silver-blue thread light, pale dust, and two subtle light
points within the slit. The outer silhouette remains unchanged.

## 6. Canonical Base Deliverable

The first approved image is a strict identity anchor, not an animation sheet.
It contains one character only and provides:

- Full-body side-view neutral stance facing right.
- Entire lantern, tether, weapon, mantle, hands, and feet inside frame.
- Clear separation among head, torso, chest spool, arms, weapon, and legs.
- Grounded feet on a consistent baseline.
- Weapon held close enough to preserve a compact neutral silhouette.
- Flat chroma background with safe padding on all sides.
- No detached effects, attack trails, scenery, text, UI, frame boxes, or labels.

The base must be reviewed at full resolution and at intended gameplay size. It
does not become the canonical identity anchor until the user explicitly accepts
it. Rejected candidates remain outside runtime-loaded `assets/`.

## 7. Initial Animation Scope

After the base is locked, create separate action runs rather than one mixed raw
sheet:

1. `idle`: body settle, restrained mantle motion, lantern sway.
2. `run`: readable acceleration cycle with lantern lag and stable chest spool.
3. `jump_fall`: takeoff, ascent, apex, and fall poses, kept separate if needed.
4. `ground_attack`: Half-Bell Cleaver wind-up, strike, and recovery; detached
   impact or slash effects remain separate.
5. `thread_cast`: chest spool engagement, controlled tether launch, recovery.

Later formal-art work will add air attack, up attack, down attack, dash, hurt,
death, Ember, and Echo states. They are not part of the first visual-pipeline
proof unless the initial five actions pass.

## 8. Asset Pipeline

The external toolchain remains outside the Godot project at
`E:\OurGame\ArtTools`.

1. Generate and approve the canonical base with Codex's host-native image
   generation tool. This path requires no external image API key and creates no
   separate pay-per-call API invoice, but it may consume the user's included
   Codex image quota.
2. Use Agent Sprite Forge as the primary action-sheet processor: each action is
   generated as its own multi-row grid, then chroma-cleaned, split, aligned,
   checked, and exported locally.
3. Use `sprite-gen` for human curation, atlas composition, and the final runtime
   manifest after every Agent Sprite Forge action passes QC.
4. Use Gorest to review animation timing, anchors, metadata, lantern motion,
   scene scale, and side-scroller readability.
5. Perform human curation and manual cleanup before accepting an atlas.
6. Convert the curated atlas manifest into Godot `SpriteFrames` or equivalent
   generated resources through a project-owned importer.
7. Verify inside the actual prototype room with current movement, combat,
   thread, reset, and deterministic capture flows.

Runtime-loaded final assets belong under `E:\OurGame\Knight\assets`. Generation
inputs, rejected candidates, raw sheets, and review previews stay outside that
directory until accepted.

## 9. Ownership, Quota, and Cost Boundary

- Use original project-owned characters and references.
- Record the generator mode, reported model/provider when exposed, prompt
  source, quota-bearing attempt, licence/provenance, intended display size, and
  accepted derivative chain for each production asset.
- Tool source-code licences do not automatically license generated output.
- Gorest demonstration media cannot be reused as production art.
- The approved default is host-native Codex image generation plus local
  processing. Do not configure Gemini, xAI, or a separately metered OpenAI API
  key for this milestone.
- Host-native generation is not described as unlimited or universally free: it
  uses the image allowance attached to the current Codex product plan. Failed
  and rejected generations still count as attempts and must be logged.
- If the included host quota is unavailable or exhausted, stop and offer a
  human-drawn or fully local-model route. Any separately billed API route still
  requires a new explicit quote and approval.

## 10. Godot Integration Boundary

Animation presentation reads player state; it does not own gameplay rules.

- `PlayerAnimationCoordinator` maps motor, combat, ability, health, and facing
  state to named animation clips.
- Sprite animation never decides hit timing, damage, invulnerability, cooldown,
  dash distance, thread targeting, or reset state.
- Current collision shapes remain gameplay-owned and are not inferred from
  painted pixels.
- The lantern uses a separate visual node or layer so its delayed motion does
  not distort the body atlas or collision.
- Detached weapon impact, thread, and Ember effects use separate visual layers.
- Horizontal facing may be mirrored at runtime after visual orientation is
  verified; do not spend image quota generating redundant mirrored art by
  default.

## 11. Acceptance Criteria

### Base-image approval

- Full silhouette and all attachments are uncropped.
- Five primary forms remain readable at intended display size.
- Head contains no horned or insect-mask silhouette.
- Chest spool remains the strongest torso marker.
- Weapon reads as a short, broad half-bell cleaver.
- Lantern visibly relates to the back core and floats above the head.
- Thin, black, realistic, and micro-mechanical motifs remain secondary.
- Material treatment matches storybook painterly contours.
- Identity, source, host-generation mode, attempt count, quota status, and
  licence information are logged.

### Animation-pipeline proof

- Each of the five initial actions has its declared frame count and timing.
- Feet/root alignment does not visibly drift in grounded actions.
- Body scale, head size, chest spool, weapon, and lantern identity remain stable.
- Run and idle loops have acceptable seams and motion continuity.
- One-shot attacks and thread casts have clear anticipation, action, and
  recovery without duplicated held ending frames.
- Gorest previews pass human visual review.
- Godot imports the curated atlas and uses only manifest-defined frame regions.
- The formal player remains playable through the prototype smoke route without
  gameplay, collision, reset, or performance regression.

## 12. Explicit Exclusions

This phase does not produce final enemy art, boss art, environments, UI, audio,
all player animations, equipment variants, or real-time identity transformation.
It establishes the player identity and proves the production pipeline with five
actions before formal-art scope expands.
