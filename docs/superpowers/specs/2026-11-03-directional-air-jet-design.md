# Directional airborne jet pose design

Scope: approved M03/M06/V03/V04 and U-20261103-01 under the user's explicit autonomous continuation. Existing Godot/.NET atG:/knight/Knight only. This adds a real direction/force-driven articulated pose, not new gameplay or final artistic approval.

## Facts and approach

Motor already publishes accepted identity, locked eight-direction thrust, burn/coast/brake. Current body uses one Dash template plus horizontal velocity-based root lean; up/down/diagonal thrust lacks its own articulated body/leg response. Existing timed transitions/secondary cloth/light and rigid18+18 leg solving remain, not replaced.

Use a pure value model observing the accepted Mobility frame and actual physics-stamped velocity changes. Compose a small common torso/attachment load after action mixing; separately fold/trail free airborne feet through the existing rigid solver. Keep actual weapons/grips coherent. Do not rotate the entire character toward an arrow or stretch bitmaps. Grounded/landing pose keeps authority; no new keys, pulse/force, damage, collision, map/save/engine/resource changes.

Builder choice: direction plus real acceleration rather than eight static clip labels; the latter would not explain inherited momentum/reverse braking. Existing real velocity root lean stays compatible. No generated/purchased art, final Spine choice or normal-speed acceptance is implied.

## Value contracts

New PlayerAirJetPose.cs owns PlayerAirJetInput(ulong OwnerId,Vector2 Velocity,PlayerGroundSample Motion,int Facing,bool Grounded,bool Suppressed,ThrusterPresentationFrame Mobility), PlayerAirJetFrame(bool Active,bool ForceObserved,Vector2 NormalizedAccelerationWorld,float TorsoLeanRadians,float PelvisLag,float NearFold,float FarFold,float FootTrail), and PlayerAirJetPoseModel.Step(input,float delta)→frame. No Node/Resource refs, queue, callback or physical writes.

Only accepted nonzero identity matching owner, unit finite direction and exclusive burn/coast can activate. Owner/lifecycle/epoch/retired burn history rejects stale replays; a new burn blends rather than snapping. Grounding clears pose without retiring a still-valid burn; death/recovery or missing/invalid source clears/retire history. A first valid zero-time sample may prime kinematics, but must not advance pose or measure force; later zero-time calls cannot consume a new physics sample. Same physical tick may ease the cached target at positive render time, never create another acceleration measurement. Rollback, >8-tick gap or >64px root discontinuity re-primes rather than inventing force. Double arithmetic/clamps handle finite extremes.

Provisional presentation mapping (not SI units/final taste): acceleration normalized by4500 and clamped each axis±1. Burn energy1, coast energy=clamp(CoastRemaining/.45,0,1)^2. World torso target=.10*dirX during burn/.06*dirX during coast plus .035*normalizedAx, all energy-weighted; braking uses opposing current horizontal velocity instead of a forward burn lean. Clamp torso±.16rad. Pelvis target=-.8*normalizedAy*energy, clamp±.8px. Near fold=(2+8*max(0,-dirY)+3*abs(dirX))*energy, max11px; far=(1+7*up+5*abs(dirX))*energy, max10px. World trail=-4*dirX*energy. Ease in seconds with exp response .06burn/.035coast/.045brake. Convert horizontal angular/trail values to facing-local only when publishing, preserving world-side continuity across facing flips.

Authoritative source expiry/cancel revokes pose; no extra fabricated thrust/recovery lease after it. Coast target approaches neutral before expiry. Grounding/death/recovery cannot carry free-flight feet into supported or terminal poses. Action hand/weapon ownership remains intact: common load follows mixing, free-foot solving does not rewrite attack/claw state.

## Renderer and proof

Optional frame.AirJet defaults inactive for all old callers. Active measured airborne Dash replaces its old fixed horizontal body/mantle template rather than double-summing. Common hip-pivot rotation/lag applies all ten slots coherently; no root/collider/scale change beyond existing facing/root lean. Existing ground feet still own supported frames. For active airborne feet, use original FreeLegSoleLocal+(trail,-fold) through SetLegSoleLocal, with unit up/fixed18+18 links and existing nearest-knee continuity. No hidden reach clamp accepted for normal inputs.

Real saved Player/native keyboard tests: separate air fixture perW/S/A/D/diagonals, Shift only/noSpace, same-frame snapshot, acceptedDash1/jump0/noairrefund, locked direction/sameidentity across fresh samples, actual pose/rigid folded feet/nozzle. Test native channel RED before implementation, pure physics/rate/history controls RED→GREEN, formal draw RED→GREEN, then actual eight-direction consumers. Preserve old five rig delegates and v217 landing/ground contracts. One fresh review, one C/I TDD fix pass if needed; minors deferred; no re-review.

Headless/internal Godot input only, original build180s/focused55s/pre180s/post300s; freezeCS/helpers during owned work. Full ordinarypre/post/actions/keyboard/models still required, every error retained. Main899/oldfatal/Handle/fullroutes/R-I/R-C/R-P/normal pixels/hardware/human/all11 remainOPEN. No GUI/OS input, paid assets, crash capture, automation, Git mutation, deletion or true-save access.
