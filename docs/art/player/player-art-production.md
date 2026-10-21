# Thread-Spool Bearer Art Production

- Status: all independent complete layered-master part groups accepted; final dressed-master review is the next gated stage
- Project: `E:\OurGame\Knight`
- External workspace: `E:\OurGame\ArtProduction\thread-spool-bearer-v1`
- Tool workspace: `E:\OurGame\ArtTools`

## Cost and retry policy

The approved source-pixel route is Codex host-native image generation. It does
not require a separate image API key or create a separate pay-per-call API
invoice, but each attempt can consume the image allowance included with the
current Codex product plan. Agent Sprite Forge and `sprite-gen` are free local
processors; they clean, split, align, inspect, curate, and pack pixels, but do
not replace the original drawing or pose-generation step by themselves.

Generate or edit one review unit at a time. Every quota-bearing attempt is
recorded in `player-art-generation-log.json`. A rejected result is preserved
with its rejection reason and is never silently rerolled. A replacement attempt
requires an explicit, action-specific continuation. If included quota is
unavailable, stop and offer human illustration or an explicitly approved local
model route; do not switch to a separately billed API without new approval.

## Three approval gates

| Gate | Review unit | Local processing | Exit condition |
| --- | --- | --- | --- |
| A — identity | One full, right-facing identity candidate | Archive it and inspect full-size plus 96-pixel gameplay scale | User explicitly accepts one complete Thread-Spool Bearer identity |
| B — layers | One body-only edit; isolate the lantern locally first and edit it only if extraction fails | Chroma cutout, component extraction, normalization, alpha QC | User accepts the identity-locked body anchor and separate lantern |
| C — actions | One action grid at a time | Agent Sprite Forge split/alignment/QC, then `sprite-gen` curation and atlas packing | User accepts all six action rows and the final runtime atlas |

Approval at one gate authorizes only the next gate. The current
`thread-spool-bearer-direction-v2.png` is a direction reference, not the Gate A
canonical identity and not an animation source.

## Initial runtime clips

| Clip | Frames | FPS | Loop |
| --- | ---: | ---: | --- |
| `idle` | 6 | 6 | yes |
| `run` | 8 | 12 | yes |
| `jump` | 4 | 10 | no |
| `fall` | 4 | 8 | yes |
| `ground_attack` | 6 | 12 | no |
| `thread_cast` | 6 | 10 | no |

The six rows total 34 frames. Each action is generated and reviewed separately.
The untouched `sprite-gen` manifest supplies the absolute frame rectangles;
Godot must not infer equal cells or guess frame boundaries.

## Paths and publication boundary

Raw inputs, rejected candidates, layer work, action grids, previews, and curation
runs remain under:

- `E:\OurGame\ArtProduction\thread-spool-bearer-v1\base`
- `E:\OurGame\ArtProduction\thread-spool-bearer-v1\layers`
- `E:\OurGame\ArtProduction\thread-spool-bearer-v1\actions`

Only explicitly accepted assets are published to the project:

- `docs/art/player/reference/thread-spool-bearer-full.png`
- `docs/art/player/reference/thread-spool-bearer-body.png`
- `assets/player/thread-spool-bearer/player-atlas.png`
- `assets/player/thread-spool-bearer/player-atlas.json`
- `assets/player/thread-spool-bearer/lantern.png`
- `assets/player/thread-spool-bearer/player-animations.tres`

The manifest parser rejects missing clip metadata, mismatched frame counts,
non-positive frame rates or rectangle dimensions, and rectangles outside the
declared atlas. The accepted atlas is imported only from manifest-defined
regions.

## Local environment status

Verified on 2026-08-13 without invoking image generation:

- Agent Sprite Forge Python imports Pillow and Numpy.
- Agent Sprite Forge `generate2dsprite.py list-options` exits successfully.
- `sprite-gen --help` exits successfully.
- The three external production directories exist.

These checks consume no image quota. They prove the deterministic processing
environment is ready; they do not produce or approve original character art.

## Gate A review history

`identity-003` was produced on 2026-08-13 as one identity-preserving host-native
edit of the direction-approved reference. It improved the compact, low-centered
body, reduced head scale and surface noise, preserved the chest spool and
lantern relationship, and kept the five main forms readable at 96 pixels tall.
It was rejected because the weapon became a double rectangular mechanical
hammer or clamp rather than a half-bell cleaver. Border samples also showed
multiple magenta values instead of one uniform `#FF00FF` key colour. The source,
96-pixel review, prompt, hash, and rejection reason are preserved externally;
no automatic replacement attempt is authorized.

`identity-004` was produced on 2026-08-13 as one narrowly scoped host-native
edit of candidate 003. It preserved the compact body and identity markers while
replacing the rectangular hammer with a single flared half-bell cleaver. The
host output again contained small magenta variations, so `sprite-gen` removed
the background locally and a deterministic step composited the unchanged
character over exact `#FF00FF`. The resulting full-size image and 96-pixel
review passed the project preflight: the five primary forms remain readable,
the source is uncropped, and the entire border is one key colour. It remains
was explicitly accepted by the user on 2026-08-13 and published as
`docs/art/player/reference/thread-spool-bearer-full.png`, the only canonical
identity source for later derivatives.

## Gate B layer review

The accepted full identity was first cut out locally. Automated connected-
component extraction correctly stopped because the lantern and luminous tether
formed one continuous foreground component; no pixels were erased to force a
pass. That failure evidence is retained in the external layer workspace.

One host-native edit produced `body-001` by removing only the lantern and
tether while preserving the back core and all identity markers. Because local
lantern extraction was blocked, one conditional host-native edit produced
`lantern-001`. Both were chroma-cut locally. The body passed full-size and
96-pixel inspection. The lantern was normalized to transparent 96×96 with an
alpha bounding box of `(21,6)-(74,90)`, so every edge retains the required
6-pixel safe margin. Both layers were explicitly approved by the user on
2026-08-13. The body was published as
`docs/art/player/reference/thread-spool-bearer-body.png`; the normalized lantern
was published as `assets/player/thread-spool-bearer/lantern.png`.

## Gate C idle review

`action-idle-001` was the first action-specific review unit. One host-native
generation produced exactly six right-facing, body-only idle poses in a 2x3
grid. Agent Sprite Forge then removed the key background, split the grid,
preserved scale, aligned the feet, and created six transparent 256x256 frames
plus a six-frame preview GIF.

Strict QC passed with 6/6 valid frames, zero empty frames, zero edge-touch
frames, and zero clamped paste positions. Body-scale CV is `0.03947` against a
maximum of `0.08`; foot-anchor Y standard deviation is `0.04862` against a
maximum of `0.05`. User review nevertheless rejected it: the independently
redrawn poses produce abrupt silhouette changes and a visible stretching
effect, while the mantle lacks convincing cloth inertia, delayed drag, and
rebound. This proves the geometric checks do not certify animation quality.
The candidate remains external as failure evidence and must not be published,
atlas-packed, or used as the source for later actions. No later action has been
generated.

## Complete layered-master route

The rejected action grid and rejected mask-separation experiment are retained
only as failure evidence. The replacement route is a free, local, deterministic
SVG and PNG authoring workflow under
`E:\OurGame\ArtProduction\thread-spool-bearer-v1\masters\complete-layered-001`.
It does not call image-generation services, use a paid API, or consume image
allowance.

The user accepted the low-detail structure gate on 2026-08-13 with the exact
reply `批准结构稿”`. The accepted evidence consists of the uncovered-body,
dressed-silhouette, back-interface, and 35-part-map reviews. Their hashes and
the approval timestamp are stored in the external `evidence/review-state.json`.
This acceptance approves only the structure and authorizes the body-part stage;
it does not approve final rendering, materials, animation, or runtime assets.

The user accepted the body-layer gate on 2026-08-13 with the exact reply
`批准身体`. The accepted evidence consists of the 16-part isolation sheet,
the assembled uncovered body, and its 96-pixel gameplay-scale review. Their
SHA-256 hashes are stored with the approval timestamp in the external
`evidence/review-state.json`. This acceptance locks the articulated ceramic
body and authorizes the front-spool, back-core, and hidden-axle stage; it does
not approve the mantle, weapon, lantern, final rendering, animation, or runtime
assets.

The first accepted body review was superseded after the user identified an
inconsistent lighting problem. The user selected revision A: one soft
canvas-space key light from the upper left, restrained lower-right shadow,
joint contact shadows, and a subtle cyan bounce local to the chest core. The
body silhouette, proportions, part boundaries, and mechanical interfaces were
not changed. The user accepted the revised body and the mounted spool/core
system together on 2026-08-13 with the exact reply
`批准新版身体和胸背系统`. The six accepted review-image hashes and the
superseded body approval remain recorded externally. This approval authorizes
only the mantle-system stage.

The user accepted the mantle-system review on 2026-08-13 with the exact reply
`批准`. The accepted evidence covers the four isolated layers, the outer/inner
cloth comparison, and the mounted character. The mantle is one continuous
weighted cloth subject with two transparent holes and its own trim; the rigid
shoulder collar, fastener, and front collar flap remain independent layers.
Their three review hashes are recorded externally. This approval authorizes
only the half-bell cleaver and grip-overlay stage.

The user accepted the weapon/grip review on 2026-08-13 with the exact reply
`批准`. The half-bell cleaver remains a complete independent weapon with an
uncropped handle and no baked hands. The two isolated finger-occlusion overlays
cover 25.06% and 28.18% of their corresponding complete hand layers, below the
35% limit. The isolated, unheld, and held review hashes are recorded
externally. This approval authorizes only the floating-lantern and three-part
tether stage.

The user accepted the floating-lantern and tether review on 2026-08-13 with
the exact reply `批准悬浮灯`. The five independent layers comprise a complete
lantern housing, isolated glow, solid back terminal, S-curve energy middle, and
solid lantern terminal. Both segment joins are exact, the housing has no alpha
overlap with the head, and the solid terminals remain visible with energy off.
The isolated, connected, and energy-off review hashes are recorded externally.
This approval authorizes final static-master assembly, beginning with the
dressed-master restoration gate only.
