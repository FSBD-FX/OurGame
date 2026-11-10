# Measured landing load design

Approved scope: original M04/M06/V03/V04 and U-20261103-01, under the user's instruction to choose reasonable implementation details and continue. This is a presentation increment, not approval of its final artistic proportions or a completed physics/animation overhaul.

## Facts and intent

Current Timeline sees grounded/velocity, but not Motor LandingCount/LastLandingImpact. Dash wins over a ground transition; that transition is then consumed. Existing Rise/Apex/Fall and two landing templates do exist, so this is not a claim of no animation. The separate current title-resource-entry control passed8039ms with original two mounts; prior keyboard3HandleERROR remains unexplained. No resource fix will be guessed or mixed into this change.

Connect real relative-normal landing measurements to a short, independent body/leg compression and recovery, including contact while a short jet is still burning. Keep the jet/action priority and gameplay unchanged. Actual speed determines load; do not substitute sprite/root scaling, a new jump, impact damage changes, or merely switching a state label.

## Approach (builder decision under existing autonomy)

Use a value-only landing channel fed by Motor's real owner ID/count/reliable measured speed. An independent model consumes each new count once, primes a new owner/current history without replay, and clears on leaving ground/death/recovery. This is preferable to delaying a whole landing clip until jet ends (late physical response), or ending thrust on landing (gameplay change). Legacy frames keep their existing templates when no measured observation is supplied.

Provisional presentation values, not physics tuning or final art approval: strength=clamp(speed/650,0,1)^2; maximum hip compression4px; duration .18+.10*strength seconds; continuous zero-endpoint envelope4*u*(1-u), u=elapsed/duration. Finite speed checks happen before arithmetic. Zero delta cannot consume a new impact or advance an existing envelope. Same accepted count must not retrigger after settling. Missing/unreliable input produces no claimed measured load; existing fallback landing animation stays available.

## Integration and boundaries

- New PlayerLandingLoad.cs owns pure observation/frame/model, no Node/Resource references.
- PlayerPresentationInput adds optional Landing observation; frame adds optional LandingLoad. Coordinator reads the real Motor identity/count/LastLandingImpact. State counters are never changed by presentation.
- Timeline advances load independently of Dash/action; real ground sample remains necessary for grounded leg/sole solving. During an active measured landing, the ground gait may solve planted support even while Dash otherwise owns locomotion. Do not restore ordinary land state after the fact or manufacture input.
- FormalVisual uses common parent-space compression after action mixing, so torso/arms/weapon/core/cloth/light remain coherent; hips follow and current fixed-length leg solving keeps feet on the real support. Never change root/collider transforms or scale. Only apply measured compression when support is observed; death/recovery/air suppress stale load. Measured landing replaces the fixed vertical land-template offset while active, not double-counting both.
- Keep all old slots, constructors, fallback frames, attack and claw meanings. No gameplay dynamics, HP, damage, resource ownership, map, saves, engine or input changes.

## Evidence and remaining gates

First reproduce using saved Player, real terminal fall, actual keyboard jet started near contact, real Motor landing count/speed and still-burning frame. Missing landing channel must fail as an assertion, not a compile error. Then verify pure one-shot/owner/rejection/rate behavior, formal geometry/coherent attachments/fixed soles and real Motor→Coordinator→Formal response, including no repeated standing impact. Preserve existing impact and gait tests.

Use headless only, original caps, source/helper frozen during runs. Full ordinary pre/post/actions/keyboard/models remain separate strict gates; preserve every failure. Normal-speed art/feel, hardware/human acceptance, Main899, historical native resource faults and all eleven items stay OPEN. No GUI/OS keys, payment, new crash dump, automation, external write, deletion or real-save access.
