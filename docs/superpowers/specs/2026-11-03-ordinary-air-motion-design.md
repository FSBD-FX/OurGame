# Ordinary jump/fall articulation design

KNIGHT atG:/knight/Knight only; original M04/M06/V03/V04 and U-20261103-01, not final style approval. Existing authorization is autonomous continuation and headless/internal input only. No GUI, paid art, physical tuning, saves, Git changes, deletion or runtime replacement.

## Facts and approach

v218 is a completed local increment, full11goalACTIVE. Grounded gait already has measured soles and rigid knees; ordinary JumpStart/Rise/Apex/Fall still use fixed whole-leg angles. Timeline currently treats every grounded→air departure as JumpStart, including walking off a ledge. AcceptedJumpCount resets in ResetTransientState; it cannot serve as a lifetime-monotonic event identity.

Chosen builder approach: real accepted-jump observation plus stamped ordinary-air articulation, preserving existing assets/physics. Alternative pure fixed clip interpolation does not fix false walk-off launch or velocity correspondence. New image/Spine action production needs separate asset approval/quota and is not authorized here. Character identity is unchanged; numerical pose mappings below are provisional developer choices requiring normal-speed/human review.

## Contracts

Motor adds readonly public ulong AcceptedJumpSequence, increments only alongside existing AcceptedJumpCount when result.StartedJump, never reset by transient reset. No input/force/count-budget/save behavior change.

New production PlayerOrdinaryAirMotion.cs contains:

- PlayerJumpObservation(ulong OwnerId,ulong Sequence,bool Observed).
- PlayerOrdinaryAirInput(PlayerJumpObservation Jump,PlayerGroundSample Motion,Vector2 Velocity,int Facing,bool Grounded,bool Suppressed,bool Replaced).
- PlayerOrdinaryAirFrame(bool Active,bool JumpStarted,bool JumpArc,float EntryWeight,float TorsoLeanRadians,float PelvisOffset,float NearLift,float FarLift,float FootTrail).
- PlayerOrdinaryAirMotion.Step(input,float deltaSeconds) returns frame, value-only/no Node or Resource.

Optional input.Jump/frame.OrdinaryAir default to legacy compatibility. Real Coordinator always publishes actual Motor instance/sequence/Observedtrue plus existing Ground motion. Real observed departures enter JumpStart only on a fresh accepted sequence, including coyote/buffered acceptance; a nonjump downward departure enters Fall without launch crouch. Absent legacy observation keeps old synthetic-caller behavior, not production fallback.

Regression clarification: track unsupported ground departure separately from generic post-claw air. Continued walk-off descent staysFall even in the slow-speed band; new accepted jump, upward motion, grounding/death/recovery or jet/claw takeover clears that marker. Without that departure cause, existing ±80px/s Apex band remains authoritative (including tiny positive drift after natural claw retraction). Do not relabel every nonjump nonnegative speed asFall.

Owner change/first sample primes without replay; missing/invalid/zero owner retains known sequence watermark and clears visible arc. Stale sequence/tick cannot create an event. Only positive elapsed and new valid physical tick consume jump/velocity; duplicate ticks ease cached targets only. Rollback/>8tick gap/>64px discontinuity primes, not fabricates a jump. Ground/death/recovery/jet/claw takeover clears ordinary arc and consumes current sequence without replay; subsequent generic fall is allowed, old launch cannot resume.

Ordinary air excludes Dashing/burning/coasting and claw Latched/Reel/Swing/Boost/Released/Retract; aim/windup/travel and attack/guard upper-body actions may keep ordinary legs. Real jet and measured landing always win.

Cached target from actual velocity using bounded double arithmetic: up=clamp(-vy/600,0,1), down=clamp(vy/850,0,1), horizontal=clamp(vx/250,-1,1). Accepted jump arc nearLift=4+8up-6down, farLift=5+5up-6down; unsupported nonjump near=-2down/far=-down (no invented tuck). World trail=-3horizontal; world lean=horizontal*(JumpArc?.02+.03up:.02); pelvis=JumpArc?.6up-.3down:-.3down. Exp response .07s. Near[-2,12],far[-1,10],trail±3,lean±.05,pelvis[-.3,.6]. EntryWeight=smoothstep(clamp(ordinaryAirElapsed/.08,0,1)); zero time does not advance.

## Rendering and proof

Actual-air states neutralize old body/whole-leg templates while preserving arm/mantle intent. Common bounded hip-pivot/offset after action mixing moves all10 attachments; no collision/root/image scaling. Air feet use same fixed18+18 solver. On activation capture displayed local ankle/up, interpolate ankle to freeSole+(trail,-lift)+Up*4 using EntryWeight, rotate unit up towardUp, derive sole=ankle-up*4. This preserves released leg geometry and avoids unreachable world-anchored feet while ascending. Endpoints and interpolation must not require reach clamps. Ground/landing/death/recovery/replaced clear retained free-flight targets.

Native saved-player finite ledge and real internal keys prove ordinary Space, walk-off, coyote acceptance, upward→apex→fall→landing, fixed joints and no Dash/second jump/velocity injection. Actual walk-off JumpStart failure first; pure history/rate controls and actual Draw RED→GREEN next. Preserve oldgait/landing/airjet/rig/actions/keyboard and complete ordinary pre/post; original caps/source snapshots. One fresh whole-change review, one C/I TDD fix pass; minors deferred. No final aesthetic/hardware/fullroute/resource rootcause acceptance implied.
