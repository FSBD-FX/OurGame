using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Build.Prologue;
using Knight.Enemies;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue.Residents;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

// Test-route input policy only. The saved actor/Player retain all damage,
// manifestation, direction, energy and input-sampling authority.
internal static class GhostLandingGuardRuntimeTests
{
    internal const string ScenePath = "res://scenes/enemies/residents/v001/lost-echo.tscn";

    internal static bool TrySample(RustShellSquire actor, CharacterBody2D player,
        out Vector2 point, out ulong attack)
    {
        point = default;
        attack = 0;
        if (!GodotObject.IsInstanceValid(actor) || !GodotObject.IsInstanceValid(player) ||
            !actor.IsInsideTree() || !player.IsInsideTree() || actor.IsQueuedForDeletion() || player.IsQueuedForDeletion() ||
            actor.GetTree().Paused || actor.GetWorld2D() != player.GetWorld2D() || actor.SceneFilePath != ScenePath ||
            actor.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0 ||
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0 ||
            actor.GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.IsAvailable != true) return false;
        AttackTimelineFrame timeline = actor.CurrentFrame.AttackTimeline;
        var leaf = actor.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        var hit = actor.GetNodeOrNull<Hitbox>("AttackHitbox");
        var hurt = player.GetNodeOrNull<Hurtbox>("Hurtbox");
        if (timeline.AttackId == 0 || timeline.Phase is not (AttackPhase.Telegraph or AttackPhase.Active) ||
            !float.IsFinite(timeline.PhaseRemainingSeconds) || timeline.PhaseRemainingSeconds < 0 ||
            !GodotObject.IsInstanceValid(leaf) || !leaf.IsInsideTree() || leaf.IsQueuedForDeletion() ||
            leaf.CurrentFrame.Timeline.AttackId != timeline.AttackId || leaf.CurrentFrame.Timeline.Phase != timeline.Phase ||
            leaf.CurrentFrame.Shape.Kind != AttackShapeKind.Rectangle ||
            !GodotObject.IsInstanceValid(hit) || !hit.IsInsideTree() || hit.IsQueuedForDeletion() ||
            hit.GuardClass != DamageGuardClass.Normal ||
            !GodotObject.IsInstanceValid(hurt) || !hurt.IsInsideTree() || hurt.IsQueuedForDeletion()) return false;
        Vector2 delta = hit.GlobalPosition - hurt.GlobalPosition;
        if (!delta.IsFinite() || delta.LengthSquared() <= .000001f) return false;
        point = hit.GlobalPosition;
        attack = timeline.AttackId;
        return true;
    }

    // Route input admission, not new gameplay permission. Real J/Health still
    // decide whether the predicted horizontal counter actually connected.
    internal static bool CanCounter(RustShellSquire actor, CharacterBody2D player,
        ulong expectedActor, ulong expectedAttack, bool foreignDanger)
    {
        if (foreignDanger || expectedActor == 0 || expectedAttack == 0 ||
            !GodotObject.IsInstanceValid(actor) || !GodotObject.IsInstanceValid(player) ||
            !actor.IsInsideTree() || !player.IsInsideTree() || actor.IsQueuedForDeletion() || player.IsQueuedForDeletion() ||
            actor.GetTree().Paused || actor.GetWorld2D() != player.GetWorld2D() || actor.GetInstanceId() != expectedActor ||
            actor.SceneFilePath != ScenePath || !player.IsOnFloor() ||
            actor.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0 ||
            player.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth is not > 0 ||
            actor.GetNodeOrNull<ThreadTarget2D>("ThreadTarget")?.IsAvailable != true) return false;
        AttackTimelineFrame timeline = actor.CurrentFrame.AttackTimeline;
        var leaf = actor.GetNodeOrNull<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        var mode = player.GetNodeOrNull<PlayerActionModeController>("ActionModes");
        var shield = player.GetNodeOrNull<PlayerShieldController>("Shield");
        var claw = player.GetNodeOrNull<MechanicalClawController>("MechanicalClaw");
        var motor = player.GetNodeOrNull<PlayerMotor>("Motor");
        var energy = player.GetNodeOrNull<PlayerEnergyController>("Energy");
        if (timeline.Phase != AttackPhase.Recovery || timeline.AttackId != expectedAttack ||
            !GodotObject.IsInstanceValid(leaf) || !leaf.IsInsideTree() ||
            leaf.CurrentFrame.Timeline.AttackId != expectedAttack || leaf.CurrentFrame.Timeline.Phase != AttackPhase.Recovery ||
            mode?.CanBeginAttack != true || shield == null || shield.IsActive || Input.IsPhysicalKeyPressed(Key.L) ||
            claw?.CurrentFrame.State != MechanicalClawState.Idle || motor?.Tuning == null || energy?.Tuning == null)
            return false;
        float required = motor.Tuning.AttackStartupSeconds + motor.Tuning.AttackActiveSeconds +
            motor.Tuning.AttackRecoverySeconds + 8f / 60f;
        if (!float.IsFinite(required) || required <= 0 || !float.IsFinite(timeline.PhaseRemainingSeconds) ||
            timeline.PhaseRemainingSeconds < required || !float.IsFinite(energy.EnergyRatio) ||
            energy.EnergyRatio * energy.Tuning.MaxEnergy < 2f * energy.Tuning.AttackCost) return false;
        var combat = player.GetNodeOrNull<PlayerCombat>("Combat");
        var hit = player.GetNodeOrNull<Hitbox>("Combat/AttackHitbox");
        var shape = player.GetNodeOrNull<CollisionShape2D>("Combat/AttackHitbox/AttackCollider");
        var hurt = actor.GetNodeOrNull<CollisionShape2D>("Hurtbox/HurtboxCollider");
        if (combat == null || hit == null || shape?.Shape == null || hurt?.Shape == null || shape.Disabled || hurt.Disabled)
            return false;
        Transform2D candidate = hit.Transform;
        candidate.Origin = new Vector2(actor.GlobalPosition.X < player.GlobalPosition.X ? -42 : 42,0);
        Rect2 attackRect = combat.GlobalTransform * candidate * shape.Transform * shape.Shape.GetRect();
        Rect2 targetRect = hurt.GlobalTransform * hurt.Shape.GetRect();
        return attackRect.Position.IsFinite() && attackRect.Size.IsFinite() && targetRect.Position.IsFinite() &&
            targetRect.Size.IsFinite() && attackRect.Intersects(targetRect);
    }

    internal static async Task Run(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        foreach (var item in new[]
        {
            (Guard: false, PlayerX: 280f, Cross: false, WantFacing: 1),
            (Guard: true, PlayerX: 280f, Cross: false, WantFacing: 1),
            (Guard: false, PlayerX: 230f, Cross: false, WantFacing: -1),
            (Guard: false, PlayerX: 280f, Cross: true, WantFacing: 1)
        })
        {
            bool guard = item.Guard;
            var fixture = new ResidentPatrolRuntimeTests.Fixture(tree);
            KeyboardRouteInputDriver driver = null;
            Hitbox hit = null;
            PlayerShieldController shield = null;
            Hitbox.ConnectedEventHandler onContact = null;
            Action<PlayerShieldResult, ulong> onShield = null;
            Action<AttackPresentationFrame> onAttack = null;
            Exception primary = null;
            try
            {
                fixture.Initialize(true, ScenePath);
                ResidentPopulationDefinition original = ResidentAuthoredLayout.Load()
                    .Single(x => x.Definition.StableId == "canyon.v2.resident.split-level.02").Definition;
                Vector2 offset = new Vector2(320, 96) - original.Placement.WorldSpawn;
                fixture.Actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile.ConfigureActor(
                    fixture.Actor, CanyonResidentCatalog.Subtypes.Single(x => x.Id == original.SubtypeId), original with
                {
                    Placement = new ResidentPlacementCandidate(new Vector2(320, 96),
                        new Rect2(original.Placement.Territory.Position + offset, original.Placement.Territory.Size),
                        original.Placement.PatrolPoints.Select(x => x + offset))
                });
                fixture.SavedPlayer(item.PlayerX);
                driver = KeyboardRouteInputDriver.Begin(fixture.Target);
                await fixture.Mount(false);
                var input = fixture.Target.GetNode<PlayerInputReader>("Input");
                var motor = fixture.Target.GetNode<PlayerMotor>("Motor");
                var health = fixture.Target.GetNode<HealthComponent>("Health");
                var hurt = fixture.Target.GetNode<Hurtbox>("Hurtbox");
                shield = fixture.Target.GetNode<PlayerShieldController>("Shield");
                hit = fixture.Actor.GetNode<Hitbox>("AttackHitbox");
                int hp = health.CurrentHealth, jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
                ulong firstSequence = input.CurrentFrameSequence, selectedAttack = 0, contactAttack = 0;
                int contacts = 0, guarded = 0, sampled = 0;
                ulong warningAttack = 0;
                int warningFacing = 0;
                bool orientationChanged = false, sawActive = false;
                bool activeAtContact = false, freshAtContact = false, normalClass = hit.GuardClass == DamageGuardClass.Normal;
                onShield = (result, _) => { if (result.Guarded && result.DamageAmount == 0) guarded++; };
                onContact = victim =>
                {
                    if (victim != hurt) return;
                    contacts++;
                    contactAttack = fixture.Actor.CurrentFrame.AttackTimeline.AttackId;
                    activeAtContact = shield.IsActive;
                    freshAtContact = input.CurrentFrameSequence > firstSequence &&
                        input.Current.KeyboardShieldHeld && Input.IsPhysicalKeyPressed(Key.L);
                };
                hit.Connected += onContact;
                shield.ShieldResolved += onShield;
                if (guard)
                    TestAssert.True(!fixture.Actor.GetNode<ThreadTarget2D>("ThreadTarget").IsAvailable &&
                        !TrySample(fixture.Actor, fixture.Target, out _, out _),
                        "the saved initial hidden phase cannot authorize Ghost defense from a nonexistent warning");
                int publicationRows = 0;
                string previousPublication = null;
                onAttack = frame =>
                {
                    if (frame.Timeline.Phase == AttackPhase.Telegraph && warningAttack == 0)
                    {
                        warningAttack = frame.Timeline.AttackId;
                        warningFacing = fixture.Actor.Facing;
                    }
                    if (warningAttack > 0 && frame.Timeline.AttackId == warningAttack &&
                        frame.Timeline.Phase is AttackPhase.Telegraph or AttackPhase.Active)
                    {
                        orientationChanged |= fixture.Actor.Facing != warningFacing ||
                            (frame.Shape.Origin.X - fixture.Actor.GlobalPosition.X) * warningFacing < 33.99f;
                        sawActive |= frame.Timeline.Phase == AttackPhase.Active;
                    }
                    string phase = $"{frame.Timeline.AttackId}/{frame.Timeline.Phase}";
                    if (phase == previousPublication || publicationRows >= 12) return;
                    previousPublication = phase;
                    publicationRows++;
                    GD.Print($"GHOST_LANDING_PUBLICATION guard={guard} physics={Engine.GetPhysicsFrames()} " +
                        $"timeline={phase} facing={fixture.Actor.Facing} actor={fixture.Actor.GlobalPosition} " +
                        $"player={fixture.Target.GlobalPosition} hitbox={hit.GlobalPosition} " +
                        $"shape={frame.Shape.Kind}/{frame.Shape.Origin}/{frame.Shape.Extent}");
                };
                fixture.Actor.AttackFrameChanged += onAttack;
                for (int frame = 0; frame < 360 && contacts == 0 && !(item.Cross && sawActive); frame++)
                {
                    if (item.Cross && warningAttack > 0)
                        driver.Command(Knight.Shared.InputActions.MoveLeft,
                            fixture.Target.GlobalPosition.X > fixture.Actor.GlobalPosition.X - 40);
                    Vector2 sourcePoint = default;
                    ulong attack = 0;
                    bool requested = guard && TrySample(fixture.Actor, fixture.Target, out sourcePoint, out attack);
                    if (requested)
                    {
                        selectedAttack = attack;
                        TestAssert.True(sourcePoint.IsEqualApprox(hit.GlobalPosition),
                            "landing aim consumes the actual hitbox source, not actor center or locked target");
                        Vector2 origin = hurt.GlobalPosition;
                        Vector2 direction = BasinCounterweightRuntimeTests.StationaryGuardDirection(origin,
                            sourcePoint, new[] { sourcePoint }, true);
                        driver.PointerButton(MouseButton.Right, true, fixture.Target,
                            fixture.Target.GlobalPosition + direction * 96);
                        sampled++;
                    }
                    else driver.PointerButton(MouseButton.Right, false, fixture.Target, Vector2.Zero);
                    await fixture.Frames(1);
                    driver.ObserveFrame();
                }
                TestAssert.True(warningAttack > 0 && warningFacing == item.WantFacing && sawActive && !orientationChanged,
                    $"GHOST_WARNING_FACING_RED expected={item.WantFacing} actual={warningFacing} active={sawActive} homed={orientationChanged}");
                if (item.Cross)
                {
                    TestAssert.True(fixture.Target.GlobalPosition.X < fixture.Actor.GlobalPosition.X - 20 &&
                        contacts == 0 && health.CurrentHealth == hp && motor.AcceptedJumpCount == jumps &&
                        motor.AcceptedDashCount == dashes && driver.ClawEdges == 0 && driver.AttackEdges == 0,
                        "actual A crosses behind committed warning without homing or pose writes; not a guard-contact case");
                    GD.Print("GHOST_WARNING_COMMITMENT_OK real_A=true crossed_target=true warning_and_active_fixed=true pose_writes=0");
                    continue;
                }
                GD.Print($"GHOST_LANDING_NATIVE_CONTACT guard={guard} contacts={contacts} guarded={guarded} " +
                    $"hp={hp}->{health.CurrentHealth} sampled={sampled} attack={selectedAttack}/{contactAttack} " +
                    $"active={activeAtContact} fresh_L={freshAtContact} normal={normalClass} " +
                    $"source={hit.GetInstanceId()} player={fixture.Target.GlobalPosition} actor={fixture.Actor.GlobalPosition}");
                TestAssert.True(contacts == 1 && normalClass && contactAttack > 0 &&
                    fixture.RecoveryRequests == 0 && motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes &&
                    driver.ClawEdges == 0 && driver.AttackEdges == 0,
                    "saved Ghost must make one actual native contact with unchanged action/recovery counts; no contact is not guard success");
                TestAssert.True(guard
                        ? sampled > 0 && selectedAttack == contactAttack && guarded == 1 && activeAtContact && freshAtContact && health.CurrentHealth == hp
                        : sampled == 0 && guarded == 0 && health.CurrentHealth == hp - 1,
                    $"GHOST_LANDING_GUARD_RED guard={guard} contacts={contacts} guarded={guarded} hp={hp}->{health.CurrentHealth}");
                if (guard)
                {
                    ulong initialContactAttack = contactAttack;
                    TestAssert.True(TrySample(fixture.Actor, fixture.Target, out _, out ulong beforeHidden) &&
                        beforeHidden == initialContactAttack,
                        "natural-hide control starts with the actual still-valid contacted attack, not initial hiding");
                    ulong hideStart = Engine.GetPhysicsFrames();
                    var threadTarget = fixture.Actor.GetNode<ThreadTarget2D>("ThreadTarget");
                    for (int frame = 0; frame < 300 && threadTarget.IsAvailable; frame++)
                    {
                        AttackTimelineFrame timeline = fixture.Actor.CurrentFrame.AttackTimeline;
                        bool defend = TrySample(fixture.Actor, fixture.Target, out Vector2 livePoint, out _) &&
                            (timeline.Phase == AttackPhase.Active ||
                             timeline.Phase == AttackPhase.Telegraph && timeline.PhaseRemainingSeconds <= 8f / 60f) &&
                            shield.CanBeginGuard;
                        if (defend)
                        {
                            Vector2 direction = BasinCounterweightRuntimeTests.StationaryGuardDirection(
                                hurt.GlobalPosition, livePoint, new[] { livePoint }, true);
                            driver.PointerButton(MouseButton.Right, true, fixture.Target,
                                fixture.Target.GlobalPosition + direction * 96);
                        }
                        else driver.PointerButton(MouseButton.Right, false, fixture.Target, Vector2.Zero);
                        await fixture.Frames(1);
                        driver.ObserveFrame();
                    }
                    TestAssert.True(!threadTarget.IsAvailable && fixture.Actor.IsInsideTree() &&
                        fixture.Health.CurrentHealth == 3 && health.CurrentHealth == hp && fixture.RecoveryRequests == 0 &&
                        !TrySample(fixture.Actor, fixture.Target, out _, out _) &&
                        fixture.Actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph").CurrentFrame.Timeline.Phase == AttackPhase.None &&
                        Engine.GetPhysicsFrames() - hideStart <= 300UL,
                        "valid publication must naturally lose permission and clear on the saved Ghost hidden transition without timer/reset/HP writes");
                    driver.PointerButton(MouseButton.Right, false, fixture.Target, Vector2.Zero);
                    await fixture.Frames(2);
                    driver.ObserveFrame();
                    TestAssert.True(!Input.IsPhysicalKeyPressed(Key.L) && !input.Current.KeyboardShieldHeld && !shield.IsActive,
                        "this component driver releases actual L after natural hiding; not a full landing-scope lifetime proof");
                    ulong freshAttack = 0;
                    for (int frame = 0; frame < 180 && !TrySample(fixture.Actor, fixture.Target, out _, out freshAttack); frame++)
                    {
                        await fixture.Frames(1);
                        driver.ObserveFrame();
                    }
                    TestAssert.True(TrySample(fixture.Actor, fixture.Target, out _, out freshAttack) &&
                        freshAttack > 0 && freshAttack != initialContactAttack && health.CurrentHealth == hp,
                        "next natural manifestation needs its new actual attack, not cached pre-hidden permission");
                    GD.Print("GHOST_LANDING_NATURAL_HIDE_OK sampled_before=true native_hidden=true old_permission_rejected=true next_attack_fresh=true component_driver_released=true full_scope_release_unproved=true clock_writes=0");
                    // Synchronous controlled corruptions only: no actor/Player pose,
                    // HP, native combat clock or input frame is written.
                    var leaf = fixture.Actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
                    AttackPresentationFrame publication = leaf.CurrentFrame;
                    try
                    {
                        leaf.Present(publication with { Timeline = publication.Timeline with { AttackId = publication.Timeline.AttackId + 1 } });
                        TestAssert.True(!TrySample(fixture.Actor, fixture.Target, out _, out _),
                            "another published attack cannot lend current actor attack permission");
                    }
                    finally { leaf.Present(publication); }
                    try
                    {
                        hit.GuardClass = DamageGuardClass.Heavy;
                        TestAssert.True(!TrySample(fixture.Actor, fixture.Target, out _, out _),
                            "a Heavy strike cannot borrow the zero-damage Normal landing assumption");
                    }
                    finally { hit.GuardClass = DamageGuardClass.Normal; }
                    fixture.Actor.SetResidentDormant(true);
                    TestAssert.True(!TrySample(fixture.Actor, fixture.Target, out _, out _),
                        "dormancy invalidates the current publication immediately, without lending old warning permission");
                    fixture.Actor.SetResidentDormant(false);
                    TestAssert.True(!TrySample(fixture.Actor, fixture.Target, out _, out _),
                        "waking cannot reuse the cleared prior publication before a fresh actor tick");
                    fixture.DetachActor();
                    TestAssert.True(!TrySample(fixture.Actor, fixture.Target, out _, out _),
                        "detached source cannot lend an old published attack to the landing strategy");
                    GD.Print("GHOST_LANDING_REJECTIONS_OK initial_hidden=true mismatched_publication=true nonnormal=true dormant=true wake_stale=true detached=true controlled_negatives=true");
                }
            }
            catch (Exception error) { primary = error; throw; }
            finally
            {
                Exception cleanup = null;
                void Clean(Action action) { try { action(); } catch (Exception error) { cleanup ??= error; } }
                Clean(() => { if (GodotObject.IsInstanceValid(hit) && onContact != null) hit.Connected -= onContact; });
                Clean(() => { if (GodotObject.IsInstanceValid(shield) && onShield != null) shield.ShieldResolved -= onShield; });
                Clean(() => { if (GodotObject.IsInstanceValid(fixture.Actor) && onAttack != null) fixture.Actor.AttackFrameChanged -= onAttack; });
                Clean(() => driver?.Dispose());
                try { await fixture.DisposeAsync(); } catch (Exception error) { cleanup ??= error; }
                if (cleanup != null)
                {
                    if (primary == null) throw cleanup;
                    GD.PushError("Ghost landing cleanup after primary: " + cleanup);
                }
            }
        }
        GD.Print("GHOST_LANDING_KEYBOARD_OK cases=4 contact_cases=3 commitment_cases=1 saved_lost_echo=true actual_contact=true raw_L=true translated_fixture=true airborne=false whole_route=false human=false");
    }
}
