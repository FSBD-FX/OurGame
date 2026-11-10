using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Collections;
using Knight.Ui;
using Knight.Tests.Presentation;
using Knight.Shared;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Prologue.Residents;
using Knight.Presentation;

namespace Knight.Tests.Integration;

/// <summary>Saved Basin outcomes. No fixture activation, clock advancing, healing, or post-mount placement.</summary>
public static class BasinCounterweightRuntimeTests
{
    // Test-route recommendation only; the production shield keeps its own
    // eligibility, damage class, energy and direction-resolution authority.
    internal static Vector2 StationaryGuardDirection(Vector2 position, Vector2 primary,
        IReadOnlyList<Vector2> sources, bool complete)
    {
        Vector2 delta = primary - position;
        if (!delta.IsFinite() || delta.LengthSquared() <= .000001f) return Vector2.Zero;
        Vector2 fallback = PlayerKeyboardAimModel.ResolveDirection(delta, 1);
        if (!complete || sources == null || sources.Count == 0) return fallback;
        var model = new PlayerShieldModel(PlayerShieldSettings.Approved);
        bool Covers(Vector2 direction, Vector2 point)
        {
            Vector2 incoming = point - position;
            return incoming.IsFinite() && incoming.LengthSquared() > .000001f &&
                model.Resolve(new PlayerShieldInput(true, direction, incoming, DamageGuardClass.Normal, true)).Guarded;
        }
        bool CoversAll(Vector2 direction)
        {
            if (!Covers(direction, primary)) return false;
            foreach (Vector2 point in sources) if (!Covers(direction, point)) return false;
            return true;
        }
        Vector2 preferred = delta.Y < 0 ? Vector2.Up : Vector2.Down;
        if (CoversAll(preferred)) return preferred;
        if (CoversAll(-preferred)) return -preferred;
        return fallback;
    }

    internal static async Task RunStationaryGuardContract(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        var released = MechanicalClawFrame.Idle() with { CastId = 17, State = MechanicalClawState.Released,
            ReleaseKind = MechanicalClawReleaseKind.Cancel };
        TestAssert.True(PrologueCanyonGeneratedMainRouteTests.ShouldTask10GuardLanding(
            "ascent-grip1", true, released, 17, 41, 41),
            "GRIP1_LANDING_GUARD_POLICY_RED: only matching original manual release admits the existing down shield");
        TestAssert.True(PrologueCanyonGeneratedMainRouteTests.ShouldTask10GuardLanding(
            "ascent-grip8", true, released, 17, 41, 41),
            "GRIP8_LANDING_GUARD_POLICY_RED: the original matching K cancel may admit current published Ghost defense");
        foreach (var item in new[]
        {
            ("lift-control", true, released, 17UL, 41UL, 41UL),
            ("ascent-grip1", false, released, 17UL, 41UL, 41UL),
            ("ascent-grip1", true, released with { State = MechanicalClawState.Swing }, 17UL, 41UL, 41UL),
            ("ascent-grip1", true, released with { ReleaseKind = MechanicalClawReleaseKind.Boost }, 17UL, 41UL, 41UL),
            ("ascent-grip1", true, released with { CastId = 16 }, 17UL, 41UL, 41UL),
            ("ascent-grip1", true, released, 17UL, 41UL, 42UL),
            ("ascent-grip1", true, released, 17UL, 0UL, 0UL)
        })
            TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.ShouldTask10GuardLanding(
                item.Item1, item.Item2, item.Item3, item.Item4, item.Item5, item.Item6),
                "unrelated phase/mode/active claw/boost/stale cast/unmatched target never starts landing shield");
        foreach (var item in new[]
        {
            (false, released, 17UL, 41UL, 41UL),
            (true, released with { State = MechanicalClawState.Swing }, 17UL, 41UL, 41UL),
            (true, released with { ReleaseKind = MechanicalClawReleaseKind.Boost }, 17UL, 41UL, 41UL),
            (true, released with { CastId = 16 }, 17UL, 41UL, 41UL),
            (true, released, 17UL, 41UL, 42UL),
            (true, released, 17UL, 0UL, 0UL)
        })
            TestAssert.True(!PrologueCanyonGeneratedMainRouteTests.ShouldTask10GuardLanding(
                "ascent-grip8", item.Item1, item.Item2, item.Item3, item.Item4, item.Item5),
                "Grip8 defense never cancels an active claw or borrows another cast/target release");
        // A direction-only/primary-only recommendation must fail these literal
        // controls before any native fixture is mounted.
        var diagonal = new Vector2(-1, -1).Normalized();
        var cases = new (Vector2 Primary, Vector2[] Sources, bool Complete, Vector2 Want)[]
        {
            (new(-120,-45), new[] { new Vector2(-120,-45) }, true, Vector2.Up),
            (new(-100,-100), new[] { new Vector2(-100,-100) }, true, Vector2.Up),
            (new(120,-45), new[] { new Vector2(120,-45) }, true, Vector2.Up),
            (new(-120,45), new[] { new Vector2(-120,45) }, true, Vector2.Down),
            (new(120,45), new[] { new Vector2(120,45) }, true, Vector2.Down),
            (new(-100,-100), new[] { new Vector2(-100,-100), new Vector2(100,-100) }, true, Vector2.Up),
            (new(-100,-100), new[] { new Vector2(-100,-100), new Vector2(-100,100) }, true, diagonal),
            (new(-100,0), new[] { new Vector2(-100,0) }, true, Vector2.Left),
            (new(100,0), new[] { new Vector2(100,0) }, true, Vector2.Right),
            (new(-100,-100), new[] { new Vector2(-100,-100) }, false, diagonal),
            (new(-100,-100), Array.Empty<Vector2>(), true, diagonal),
            (new(-100,-100), new[] { new Vector2(float.NaN, -100) }, true, diagonal),
            (new(-100,-100), new[] { Vector2.Zero }, true, diagonal),
            (new(-100,-27), new[] { new Vector2(-100,-27) }, true, Vector2.Up),
            (new(-100,-26), new[] { new Vector2(-100,-26) }, true, Vector2.Left),
            (Vector2.Zero, new[] { Vector2.Up }, true, Vector2.Zero),
            (new(float.NaN,1), new[] { Vector2.Up }, true, Vector2.Zero)
        };
        for (int index = 0; index < cases.Length; index++)
        {
            var item = cases[index];
            Vector2 actual = StationaryGuardDirection(Vector2.Zero, item.Primary, item.Sources, item.Complete);
            TestAssert.True(actual.IsEqualApprox(item.Want),
                $"BASIN_STATIONARY_GUARD_RED case={index} expected={item.Want} actual={actual}");
        }

        foreach (AttackPhase phase in new[] { AttackPhase.Telegraph, AttackPhase.Active })
        {
            int missing = 0;
            bool unknown = false;
            bool accepted = PrologueCanyonGeneratedMainRouteTests.Task10ObserveGuardSource(
                phase, new Vector2(float.NaN, 1), ref missing, ref unknown);
            TestAssert.True(!accepted && missing == 1 && unknown == (phase == AttackPhase.Active),
                $"BASIN_GUARD_SOURCE_COMPLETENESS_RED phase={phase} missing={missing} unknown={unknown}");
            accepted = PrologueCanyonGeneratedMainRouteTests.Task10ObserveGuardSource(
                phase, new Vector2(1, float.PositiveInfinity), ref missing, ref unknown);
            TestAssert.True(!accepted && missing == 2, "every rejected source is counted, not only the winner");
            // A missing shape is NOT a missing position. Existing unknown-active
            // safety remains true and is never borrowed as counter permission.
            missing = 0;
            unknown = true;
            accepted = PrologueCanyonGeneratedMainRouteTests.Task10ObserveGuardSource(
                phase, new Vector2(-120,-45), ref missing, ref unknown);
            TestAssert.True(accepted && missing == 0 && unknown,
                "known source remains observable without clearing prior unknown contact shape");
        }

        var root = new Node2D { Name = "StationaryGuardInputFixture" };
        var floorShape = new RectangleShape2D { Size = new Vector2(1600,20) };
        var floor = new StaticBody2D
        {
            Position = new Vector2(0,60), CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D { Shape = floorShape });
        root.AddChild(floor);
        PackedScene scene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        var player = scene.Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0,14);
        root.AddChild(player);
        var observer = new BasinBoardingTickObserver { ProcessPhysicsPriority = 100 };
        root.AddChild(observer);
        KeyboardRouteInputDriver driver = null;
        Exception primaryFault = null, cleanupFault = null;
        int sampled = 0;
        float coast = 0;
        async Task Frames(int count)
        {
            for (int i = 0; i < count; i++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
        try
        {
            tree.Root.AddChild(root);
            driver = KeyboardRouteInputDriver.Begin(player);
            var input = player.GetNode<PlayerInputReader>("Input");
            var motor = player.GetNode<PlayerMotor>("Motor");
            var shield = player.GetNode<PlayerShieldController>("Shield");
            var health = player.GetNode<HealthComponent>("Health");
            await Frames(8);
            TestAssert.True(player.IsOnFloor(), "stationary guard fixture starts on a real floor");
            int hp = health.CurrentHealth, jumps = motor.AcceptedJumpCount, dashes = motor.AcceptedDashCount;
            driver.Command(InputActions.MoveRight, true);
            await Frames(10);
            TestAssert.True(player.Velocity.X > 200 && input.Current.MoveAxis == 1f,
                "real D accelerates the automatic saved Player before guard handoff");
            float beforeX = player.GlobalPosition.X;
            ulong sequence = input.CurrentFrameSequence;
            ulong lastAcceptedSequence = sequence;
            Vector2 primaryPoint = player.GlobalPosition + new Vector2(-120,-45);
            Vector2 direction = StationaryGuardDirection(player.GlobalPosition, primaryPoint,
                new[] { primaryPoint }, true);
            string observerFault = null;
            int observerTicks = 0;
            observer.Sample = _ =>
            {
                try
                {
                    observerTicks++;
                    if (input.CurrentFrameSequence <= lastAcceptedSequence) return;
                    TestAssert.True(Input.IsPhysicalKeyPressed(Key.W) && Input.IsPhysicalKeyPressed(Key.L) &&
                        !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
                        input.Current.MoveAxis == 0f && input.RawFrame.MoveAxis == 0f &&
                        input.Current.KeyboardShieldHeld && shield.IsActive &&
                        shield.GuardDirection.Dot(Vector2.Up) > .99f,
                        "stationary guard uses real W+L, sampled neutral axis and active Up shield");
                    lastAcceptedSequence = input.CurrentFrameSequence;
                    sampled++;
                }
                catch (Exception error) { observerFault ??= error.Message; }
            };
            for (int frame = 0; frame < 16; frame++)
            {
                // The pilot still intends D; only legal W+L temporarily owns
                // the keyboard. No direct sample, pose or velocity write.
                driver.PointerButton(MouseButton.Right, true, player, player.GlobalPosition + direction * 80);
                await Frames(1);
                driver.ObserveFrame();
            }
            coast = player.GlobalPosition.X - beforeX;
            TestAssert.True(observerFault == null && sampled >= 12 && coast > 0 && coast < 40 &&
                Mathf.Abs(player.Velocity.X) <= .5f && player.IsOnFloor() &&
                health.CurrentHealth == hp && motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes,
                $"automatic guard handoff preserves finite inertial coast then stop: samples={sampled} coast={coast} fault={observerFault}");
            int beforeStall = sampled, ticksBeforeStall = observerTicks;
            ulong stalledSequence = input.CurrentFrameSequence;
            try
            {
                // Deliberately stall only this owned fixture reader, after a
                // valid sample. Re-reading that cached frame is not a new one.
                input.SetPhysicsProcess(false);
                await Frames(4);
                TestAssert.True(observerTicks - ticksBeforeStall >= 3 &&
                    input.CurrentFrameSequence == stalledSequence && sampled == beforeStall,
                    $"STATIONARY_GUARD_DISTINCT_SAMPLE_RED ticks={observerTicks-ticksBeforeStall} " +
                    $"distinct={sampled-beforeStall} sequence={stalledSequence}->{input.CurrentFrameSequence}");
            }
            finally { input.SetPhysicsProcess(true); }
            observer.Sample = null;
            driver.PointerButton(MouseButton.Right, false, player, Vector2.Zero);
            driver.Command(InputActions.MoveRight, false);
            await Frames(3);
            TestAssert.True(!shield.IsActive && !Input.IsPhysicalKeyPressed(Key.W) &&
                !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.L),
                "stationary guard releases its temporary direction and original movement intent");
            for (int frame = 0; frame < 6; frame++)
            {
                driver.PointerButton(MouseButton.Right, true, player, player.GlobalPosition + Vector2.Down * 96);
                await Frames(1);
            }
            TestAssert.True(Input.IsPhysicalKeyPressed(Key.S) && Input.IsPhysicalKeyPressed(Key.L) &&
                !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
                !Input.IsPhysicalKeyPressed(Key.W) && !Input.IsPhysicalKeyPressed(Key.K) &&
                !Input.IsPhysicalKeyPressed(Key.Space) && !Input.IsPhysicalKeyPressed(Key.Shift) &&
                input.CurrentFrame.MoveAxis == 0 && input.CurrentFrame.KeyboardShieldHeld &&
                shield.IsActive && shield.GuardDirection.Dot(Vector2.Down) > .99f &&
                health.CurrentHealth == hp && motor.AcceptedJumpCount == jumps && motor.AcceptedDashCount == dashes,
                "real S+L landing strategy is neutral horizontal and does not inject K/Space/Shift");
            driver.PointerButton(MouseButton.Right, false, player, Vector2.Zero);
            await Frames(2);
            TestAssert.True(!shield.IsActive && !Input.IsPhysicalKeyPressed(Key.S) && !Input.IsPhysicalKeyPressed(Key.L),
                "temporary landing strategy releases its own S+L");
            GD.Print("GRIP1_LANDING_GUARD_INPUT_OK actual_keyboard=true stale_release_refused=true no_horizontal_or_jump=true");
            await PrologueCanyonGeneratedMainRouteTests.CheckGrip1LandingGuardFaults(tree, player, root, driver);
        }
        catch (Exception error) { primaryFault = error; }
        finally
        {
            observer.Sample = null;
            try { driver?.Dispose(); } catch (Exception error) { cleanupFault ??= error; }
            try { if (GodotObject.IsInstanceValid(root)) root.QueueFree(); await Frames(3); }
            catch (Exception error) { cleanupFault ??= error; }
            try { floorShape.Dispose(); } catch (Exception error) { cleanupFault ??= error; }
            GC.KeepAlive(scene);
        }
        if (primaryFault != null || cleanupFault != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(primaryFault ?? cleanupFault).Throw();
        GD.Print($"BASIN_STATIONARY_GUARD_NATIVE samples={sampled} coast={coast} geometry_cases={cases.Length}");
        GD.Print("BASIN_STATIONARY_GUARD_OK all_sources=true conservative_fallback=true automatic_player=true real_W_L=true natural_coast=true distinct_samples=true controlled_reader_stall=true actual_enemy_contact=false human=false");
    }

    private readonly record struct RideNativeCoverage(
        int Steps, ulong Start, ulong End, int Frames, ulong First, ulong Last, int Duplicates,
        bool Contiguous, bool Stable, string Fault, int Active, bool EntryQuiet, bool ExitQuiet,
        bool OriginalWave, int Contacts, int ActivePairs, int Overlaps, int Disabled, string EnvelopeFault);

    private static bool AcceptsQuietRide(RideNativeCoverage evidence) =>
        evidence.Steps is > 0 and <= 200 && evidence.End > evidence.Start &&
        evidence.End - evidence.Start == (ulong)evidence.Steps && evidence.Frames == evidence.Steps &&
        evidence.First == evidence.Start && evidence.Last == evidence.End - 1 && evidence.Duplicates == 0 &&
        evidence.Contiguous && evidence.Stable && evidence.Fault == null && evidence.Active == 0 &&
        evidence.EntryQuiet && evidence.ExitQuiet && evidence.OriginalWave && evidence.Contacts == 0 &&
        evidence.ActivePairs == 0 && evidence.Overlaps == 0 && evidence.Disabled == 0 && evidence.EnvelopeFault == null;

    private static void VerifyQuietRideCoverageContract()
    {
        // Four original steps cover callbacks 100..103, with the final pre-node
        // signal at 104. A missing callback or a quiet but disabled actor is not proof.
        var complete = new RideNativeCoverage(4, 100, 104, 4, 100, 103, 0,
            true, true, null, 0, true, true, true, 0, 0, 0, 0, null);
        TestAssert.True(AcceptsQuietRide(complete), "complete native quiet ride is valid without forcing an attack");
        RideNativeCoverage[] rejected = {
            complete with { Steps = 0 }, complete with { Steps = 201 },
            complete with { End = 99 }, complete with { End = 105 },
            complete with { Frames = 0 }, complete with { Frames = 3 },
            complete with { First = 101 }, complete with { Last = 102 },
            complete with { Duplicates = 1 }, complete with { Contiguous = false },
            complete with { Stable = false }, complete with { Fault = "callback failed" },
            complete with { Active = 1 }, complete with { EntryQuiet = false },
            complete with { ExitQuiet = false }, complete with { OriginalWave = false },
            complete with { Contacts = 1 }, complete with { ActivePairs = 1 },
            complete with { Overlaps = 1 }, complete with { Disabled = 1 },
            complete with { EnvelopeFault = "collider unavailable" }
        };
        foreach (var evidence in rejected)
            TestAssert.True(!AcceptsQuietRide(evidence), $"incomplete/active/foreign native ride is rejected: {evidence}");
        GD.Print($"BASIN_QUIET_RIDE_COVERAGE_CONTRACT_OK accepted=1 rejected={rejected.Length} no_native_frames_added=true");
    }

    internal static async Task RunFirstReelOwnerContract(SceneTree tree)
    {
        VerifyQuietRideCoverageContract();
        var unrelated = new Node { Name = "UnrelatedReelOwnerControl" };
        tree.Root.AddChild(unrelated);
        try
        {
            for (int index = 0; index < 2; index++)
            {
                var owned = new Node { Name = "OwnedReelFailureControl" };
                tree.Root.AddChild(owned);
                Exception primary = index == 0 ? new InvalidOperationException("owned readiness control") : null;
                var cleanup = new InvalidOperationException("owned cleanup-before-release control");
                Exception observed = null;
                try { await ReleaseFirstReelOwner(tree, owned, primary, () => Task.FromException(cleanup)); }
                catch (Exception error) { observed = error; }
                try
                {
                    var errors = observed as AggregateException;
                    TestAssert.True(errors != null && errors.InnerExceptions.Count == (primary == null ? 1 : 2) &&
                        (primary == null || ReferenceEquals(errors.InnerExceptions[0], primary)) &&
                        ReferenceEquals(errors.InnerExceptions[^1], cleanup) && !GodotObject.IsInstanceValid(owned) &&
                        GodotObject.IsInstanceValid(unrelated) && unrelated.IsInsideTree(),
                        "first-Reel failure cleanup preserves exact errors, releases only its root, and leaves unrelated owner alive");
                }
                finally { if (GodotObject.IsInstanceValid(owned)) owned.Free(); }
            }
            GD.Print("BASIN_FIRST_REEL_OWNER_CONTRACT_OK cases=2 primary_preserved=true cleanup_preserved=true owned_released=true unrelated_retained=true");
        }
        finally { unrelated.Free(); }
    }

    private static async Task ReleaseFirstReelOwner(SceneTree tree, Node root, Exception primary, Func<Task> cleanup)
    {
        try { await cleanup(); }
        catch (Exception cleanupFailure)
        {
            var failures = new System.Collections.Generic.List<Exception>();
            if (primary != null) failures.Add(primary);
            failures.Add(cleanupFailure);
            void Attempt(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            if (GodotObject.IsInstanceValid(root))
            {
                if (root.IsInsideTree())
                {
                    if (root is PrologueLevelController level) Attempt(level.CancelResidentPreparation);
                    Attempt(root.QueueFree);
                    try
                    {
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    }
                    catch (Exception error) { failures.Add(error); }
                }
                else Attempt(root.Free);
                if (GodotObject.IsInstanceValid(root)) failures.Add(new InvalidOperationException("owned first-Reel root was not released"));
            }
            throw new AggregateException(failures);
        }
    }

    internal static Task RunFirstReelTrajectory(SceneTree tree) => RunFirstReelTrajectory(tree, false);

    internal static Task RunSwingReleaseTrajectory(SceneTree tree) => RunFirstReelTrajectory(tree, true);

    internal static async Task ReturnGalleryThroatToPistonTwo(CharacterBody2D player, PrologueSector sector, Func<int,Task> frames)
    {
        var driver=KeyboardRouteInputDriver.Current ?? throw new InvalidOperationException("Gallery return requires the keyboard route owner");
        var motor=player.GetNode<PlayerMotor>("Motor");
        var health=player.GetNode<HealthComponent>("Health");
        var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
        var gallery=sector.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryOne");
        var second=sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonTwo");
        var shelf=sector.GetNode<StaticBody2D>("Gameplay/Collision/GearResidentRightShelf");
        var bodyShape=player.GetNode<CollisionShape2D>("BodyCollider");
        var input=player.GetNode<PlayerInputReader>("Input");
        var fragment=sector.GetNode<QuestFragment>("Gameplay/QuestFragments/gear-gallery");
        Rect2 galleryBounds=gallery.GetNode<CollisionShape2D>("Collision").GlobalTransform*
            gallery.GetNode<CollisionShape2D>("Collision").Shape.GetRect();
        Rect2 secondBounds=second.GetNode<CollisionShape2D>("Collision").GlobalTransform*
            second.GetNode<CollisionShape2D>("Collision").Shape.GetRect();
        Rect2 shelfBounds=shelf.GetNode<CollisionShape2D>("Collision").GlobalTransform*
            shelf.GetNode<CollisionShape2D>("Collision").Shape.GetRect();
        Rect2 initialBody=bodyShape.GlobalTransform*bodyShape.Shape.GetRect();
        int hp=health.CurrentHealth,jumps=motor.AcceptedJumpCount,dashes=motor.AcceptedDashCount;
        int edges=driver.ClawEdges,feedback=fragment.FeedbackCount;
        bool collected=fragment.IsCollected;
        ulong identity=player.GetInstanceId(),cast=claw.CurrentFrame.NextCastId;
        ulong started=Engine.GetPhysicsFrames(),neutralSequence=0;
        const int cap=60+4+45;
        bool cleared=false,neutral=false,finished=false;
        TestAssert.True(Floor(player,gallery) && initialBody.Position.X>=galleryBounds.Position.X &&
            initialBody.End.X<=galleryBounds.End.X && claw.CurrentFrame.State==MechanicalClawState.Idle &&
            !input.CurrentFrame.JumpHeld && !Input.IsPhysicalKeyPressed(Key.Space) &&
            shelfBounds==new Rect2(2856,3720,136,24) && secondBounds==new Rect2(2844,3836,196,120),
            "right throat return starts from full saved Gallery support and the corrected physical corridor");
        using var supportProbe=new KinematicCollision2D();
        bool FullSecond()
        {
            Rect2 body=bodyShape.GlobalTransform*bodyShape.Shape.GetRect();
            if(!player.IsOnFloor() || body.Position.X<secondBounds.Position.X ||
                body.End.X>secondBounds.End.X || Mathf.Abs(body.End.Y-secondBounds.Position.Y)>1.1f)return false;
            return Floor(player,second) || player.TestMove(player.GlobalTransform,Vector2.Down*.001f,
                supportProbe,player.SafeMargin,true) && supportProbe.GetCollider()==second && supportProbe.GetNormal().Y<-.7f;
        }
        try
        {
            for(int tick=0;tick<cap;tick++)
            {
                Rect2 body=bodyShape.GlobalTransform*bodyShape.Shape.GetRect();
                cleared |= body.End.X<=galleryBounds.Position.X-.1f;
                if(!neutral && cleared && FullSecond())
                { neutral=true;neutralSequence=input.CurrentFrameSequence; }
                // Slow real A/D countersteering limits horizontal momentum before
                // the drop. Once clear, D brakes leftward inertia and the real
                // Gallery side wall holds the capsule while gravity lowers it.
                // No position/velocity write, extra jump or physics tuning.
                bool brake=cleared || player.Velocity.X < -60f;
                driver.Command(InputActions.MoveLeft,!neutral && !brake);
                driver.Command(InputActions.MoveRight,!neutral && brake);
                await frames(1);
                ulong elapsed=Engine.GetPhysicsFrames()-started;
                body=bodyShape.GlobalTransform*bodyShape.Shape.GetRect();
                TestAssert.True(elapsed<=cap && GodotObject.IsInstanceValid(player) &&
                    player.GetInstanceId()==identity && hp>0 && health.CurrentHealth==hp &&
                    motor.AcceptedJumpCount==jumps && motor.AcceptedDashCount==dashes &&
                    driver.ClawEdges==edges && claw.CurrentFrame.NextCastId==cast &&
                    claw.CurrentFrame.State==MechanicalClawState.Idle &&
                    fragment.IsCollected==collected && fragment.FeedbackCount==feedback &&
                    body.Position.X>=shelfBounds.End.X+.1f && player.GlobalPosition.IsFinite() && player.Velocity.IsFinite(),
                    "right throat drop retains original109, capsule clearance, identity/HP, zero jump/jet/cast and fragment state");
                if(neutral && FullSecond() && input.CurrentFrameSequence>neutralSequence &&
                    Mathf.Abs(player.Velocity.X)<=.5f && Mathf.Abs(input.CurrentFrame.MoveAxis)<=.001f &&
                    Mathf.Abs(input.RawFrame.MoveAxis)<=.001f && !Input.IsPhysicalKeyPressed(Key.A) &&
                    !Input.IsPhysicalKeyPressed(Key.D))
                { finished=true;break; }
            }
            TestAssert.True(finished && cleared && FullSecond(),
                $"right throat return reaches full PistonTwo and neutral stop in original109; player={player.GlobalPosition}");
            GD.Print($"TASK10_BASIN_SHELF_RETURN_OK elapsed={Engine.GetPhysicsFrames()-started}/109 " +
                $"right_throat=true grounded_jumps=0 extra_dashes=0 extra_claws=0 full_second=true neutral_stopped=true " +
                $"fragment_unchanged=true health={hp}->{health.CurrentHealth} player={identity} position={player.GlobalPosition} human=false");
        }
        finally
        {
            GD.Print($"TASK10_BASIN_SHELF_RETURN_END elapsed={Engine.GetPhysicsFrames()-started}/109 finished={finished} " +
                $"right_throat=true cleared={cleared} neutral={neutral} position={player.GlobalPosition} velocity={player.Velocity} jumps={motor.AcceptedJumpCount-jumps}");
            driver.Command(InputActions.MoveLeft,false);driver.Command(InputActions.MoveRight,false);
        }
    }

    internal static async Task RunGalleryThroatSingleJump(SceneTree tree)
    {
        // Geometry isolation only: saved sector and saved Player at an initial
        // supported edge spawn. This is not a normal Title-to-exit route proof.
        var fixture=new Fixture(tree,new Vector2(3018,3798));
        try
        {
            await fixture.Start();
            var player=fixture.Player;
            var source=fixture.Sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonTwo");
            var gallery=fixture.Sector.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryOne");
            var motor=player.GetNode<PlayerMotor>("Motor");
            int hp=fixture.Health.CurrentHealth,jumps=motor.AcceptedJumpCount,dashes=motor.AcceptedDashCount;
            ulong nextCast=fixture.Claw.CurrentFrame.NextCastId,identity=player.GetInstanceId();
            TestAssert.True(Floor(player,source) && player.GlobalPosition.X>=2864 && player.GlobalPosition.X<=3020,
                "Gallery throat starts on the actual full PistonTwo edge, not airborne or partial");
            using var driver=KeyboardRouteInputDriver.Begin(player);
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree,player,driver.ObserveFrame);
            try
            {
                GD.Print("BASIN_GALLERY_THROAT_BEGIN saved_geometry=true ordinary_jump=true original_transfer_cap=300 controlled_initial_spawn=true whole_route=false");
                await PrologueCanyonTraversalTests.Task4ReachSupport(tree,player,gallery,clawRequired:false);
                TestAssert.True(player.GetInstanceId()==identity && Floor(player,gallery) &&
                    player.GlobalPosition.X>=3060 && player.GlobalPosition.X<=3260 &&
                    motor.AcceptedJumpCount==jumps+1 && motor.AcceptedDashCount==dashes &&
                    fixture.Claw.CurrentFrame.NextCastId==nextCast && driver.ClawEdges==0 &&
                    fixture.Health.CurrentHealth==hp && hp>0,
                    "Gallery throat must pass one ordinary jump into full named support without cast, jet or health change");
                GD.Print("BASIN_GALLERY_THROAT_SINGLE_JUMP_OK saved_geometry=true saved_player=true full_named_support=true jump_delta=1 dash_delta=0 cast_delta=0 hp_unchanged=true controlled_initial_spawn=true whole_route=false human=false");
            }
            finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }
        }
        finally { await fixture.Dispose(); }
    }

    internal static async Task RunGalleryThroatReturn(SceneTree tree)
    {
        var fixture=new Fixture(tree,new Vector2(3079.8955f,3718));
        try
        {
            await fixture.Start();
            var motor=fixture.Player.GetNode<PlayerMotor>("Motor");
            int jumps=motor.AcceptedJumpCount,dashes=motor.AcceptedDashCount,hp=fixture.Health.CurrentHealth;
            ulong cast=fixture.Claw.CurrentFrame.NextCastId;
            using var driver=KeyboardRouteInputDriver.Begin(fixture.Player);
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree,fixture.Player,driver.ObserveFrame);
            try
            {
                GD.Print("BASIN_GALLERY_RETURN_BEGIN same_main_arrival_x=true controlled_initial_spawn=true original_cap=109");
                await ReturnGalleryThroatToPistonTwo(fixture.Player,fixture.Sector,async count=>
                {
                    for(int tick=0;tick<count;tick++)
                    {
                        await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
                        driver.ObserveFrame();
                    }
                });
                var second=fixture.Sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonTwo");
                TestAssert.True(Floor(fixture.Player,second) && fixture.Player.GlobalPosition.X>=2864 &&
                    fixture.Player.GlobalPosition.X<=3020 && motor.AcceptedJumpCount==jumps &&
                    motor.AcceptedDashCount==dashes && fixture.Claw.CurrentFrame.NextCastId==cast &&
                    driver.ClawEdges==0 && fixture.Health.CurrentHealth==hp && hp>0,
                    "new right throat return must drop into full PistonTwo without jump, jet, cast or health change");
                GD.Print("BASIN_GALLERY_THROAT_RETURN_OK saved_geometry=true full_named_support=true jump_delta=0 dash_delta=0 cast_delta=0 hp_unchanged=true original_cap=109 controlled_initial_spawn=true whole_route=false human=false");
            }
            finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }
        }
        finally { await fixture.Dispose(); }
    }

    private static async Task RunFirstReelTrajectory(SceneTree tree, bool replayFailedSwing)
    {
        var f = new Fixture(tree, new Vector2(2492, 3880), full: true);
        int recoveries = 0;
        Exception primary = null;
        PrologueLevelController.RecoveryRequestedEventHandler recovered = _ => recoveries++;
        try
        {
            await f.Start();
            f.Level.RecoveryRequested += recovered;
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, f.Level);
            using var driver = KeyboardRouteInputDriver.Begin(f.Player);
            PrologueCanyonTraversalTests.BeginTask10InputDrive(tree, f.Player, driver.ObserveFrame);
            try
            {
                await TraverseForFullRoute(f.Player, f.Sector, async count =>
                {
                    for (int index = 0; index < count; index++)
                    {
                        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                        driver.ObserveFrame();
                    }
                }, passiveFirstReel: !replayFailedSwing, replayFailedSwing: replayFailedSwing);
                TestAssert.True(f.Lift.IsActivated && recoveries == 0,
                    "passive first-Reel diagnostic retains real lift activation and zero recovery");
            }
            finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }
        }
        catch (Exception error) { primary = error; throw; }
        finally
        {
            await ReleaseFirstReelOwner(tree, f.Root, primary, async () =>
            {
                if (GodotObject.IsInstanceValid(f.Level)) f.Level.RecoveryRequested -= recovered;
                await f.Dispose();
            });
        }
    }

    public static async Task Run(SceneTree tree, BasinCounterweightPhase phase)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (phase == BasinCounterweightPhase.Contract) await Contract(tree);
        else if (phase == BasinCounterweightPhase.RouteContract)
        {
            var f=new Fixture(tree,new Vector2(2492,3880),full:true);
            try { await f.Start(); PrologueCanyonGeneratedMainRouteTests.AssertTask10GearPistonOneContract(f.Sector); }
            finally { await f.Dispose(); }
        }
        else if (phase == BasinCounterweightPhase.RoutePhysicsCadence)
        {
            var f=new Fixture(tree,new Vector2(2492,3880),full:true);
            try
            {
                await f.Start();
                await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,f.Level);
                using var driver=KeyboardRouteInputDriver.Begin(f.Player);
                PrologueCanyonTraversalTests.BeginTask10InputDrive(tree,f.Player,driver.ObserveFrame);
                try
                {
                    GD.Print("BASIN_ROUTE_PHYSICS_CADENCE diagnostic_only=true cadence=PhysicsFrame_then_Observe controlled_initial_spawn=true full_main=false human=false");
                    await TraverseForFullRoute(f.Player,f.Sector,async count =>
                    {
                        for(int i=0;i<count;i++)
                        {
                            // Match Main's PhysicsFrames -> Observe boundary.
                            // Do not add the old local fixture's ProcessFrame wait.
                            await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
                            driver.ObserveFrame();
                        }
                    });
                    TestAssert.True(f.Lift.IsActivated,"BasinFullRouteRequiresPassengerLift: route cannot skip the approved lift");
                }
                finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }
            }
            finally { await f.Dispose(); }
        }
        else if (phase is BasinCounterweightPhase.RouteTransfer or BasinCounterweightPhase.RouteEncounter)
        {
            bool encounter=phase==BasinCounterweightPhase.RouteEncounter;
            var f = new Fixture(tree,new Vector2(encounter?2460:2492,3880),full:true);
            try
            {
                await f.Start();
                await BasinAscentCameraRuntimeTests.WaitForPlaying(tree,f.Level);
                using var driver=KeyboardRouteInputDriver.Begin(f.Player);
                PrologueCanyonTraversalTests.BeginTask10InputDrive(tree,f.Player,driver.ObserveFrame);
                try
                {
                    var group=f.Sector.FindChildren("*","",true,false).OfType<CanyonEncounterGroup>()
                        .Single(g=>g.EncounterId.ToString()=="canyon.v2.encounter.gear-pistons");
                    if(encounter) await f.Until(()=>group.IsActive && !group.IsArrivalWarningActive,60,"actual location activates real gear enemies");
                    ulong[] actorIds=group.GetNode(group.ActorRootPath).GetChildren().OfType<CharacterBody2D>()
                        .Select(actor=>actor.GetInstanceId()).OrderBy(id=>id).ToArray();
                    ulong[] ownedActorIds=group.GetNode(group.ActorRootPath).GetChildren().OfType<CharacterBody2D>()
                        .Where(group.OwnsLiveActor).Select(actor=>actor.GetInstanceId()).OrderBy(id=>id).ToArray();
                    if(encounter) TestAssert.True(actorIds.Length>0,"gear retention starts with actual spawned actors, not fixture-injected actors");
                    if(encounter) TestAssert.True(ownedActorIds.Length>0 && ownedActorIds.Length==group.LiveActorCount,
                        "gear retention separately identifies every real live wave member");
                    string initialRetention=encounter?DescribeRetention():"";
                    void LogRetention(string boundary,string snapshot)
                    {
                        try { GD.Print($"BASIN_RETENTION_SNAPSHOT boundary={boundary} {snapshot}"); }
                        catch { /* Observation cannot replace the original retention failure. */ }
                    }
                    string DescribeRetention()
                    {
                        try
                        {
                            Node root=group.GetNode(group.ActorRootPath);
                            var actors=new System.Collections.Generic.List<object>();
                            for(int i=0;i<root.GetChildCount();i++)
                            {
                                if(root.GetChild(i) is not CharacterBody2D actor || !GodotObject.IsInstanceValid(actor)) continue;
                                var binding=actor.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
                                actors.Add(new {Id=actor.GetInstanceId(),Scene=actor.SceneFilePath,
                                    Health=actor.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth,
                                    Owned=group.OwnsLiveActor(actor),Resident=binding!=null,
                                    Subtype=binding?.Profile?.SubtypeId,Inside=actor.IsInsideTree(),
                                    Queued=actor.IsQueuedForDeletion()});
                            }
                            var slots=f.Level.ResidentLayout
                                .Where(site=>site.Definition.Sector==PrologueSectorId.CanyonBasin)
                                .Select(site=>f.Level.Residents.Snapshot(site.Definition.StableId)).ToArray();
                            return System.Text.Json.JsonSerializer.Serialize(new {
                                Physics=Engine.GetPhysicsFrames(),Process=Engine.GetProcessFrames(),
                                GroupId=group.GetInstanceId(),SectorId=f.Sector.GetInstanceId(),RootId=root.GetInstanceId(),
                                Active=group.IsActive,Batch=group.CurrentBatch,Live=group.LiveActorCount,
                                Player=f.Player.GlobalPosition.ToString(),Actors=actors.ToArray(),Slots=slots});
                        }
                        catch(Exception error) {return "snapshot_fault="+error.GetType().FullName;}
                    }
                    if(encounter) LogRetention("initial",initialRetention);
                    await TraverseForFullRoute(f.Player,f.Sector,async count =>
                    {
                        for(int i=0;i<count;i++)
                        {
                            await f.Frames(1); driver.ObserveFrame();
                            if(encounter)
                            {
                                var currentActors=group.GetNode(group.ActorRootPath).GetChildren().OfType<CharacterBody2D>()
                                    .Where(actor=>GodotObject.IsInstanceValid(actor) && actor.IsInsideTree() && !actor.IsQueuedForDeletion()).ToArray();
                                var currentIds=currentActors.Select(actor=>actor.GetInstanceId()).ToHashSet();
                                bool retained=group.IsActive && group.CurrentBatch==0 &&
                                    actorIds.All(currentIds.Contains) && group.LiveActorCount==ownedActorIds.Length &&
                                    ownedActorIds.SequenceEqual(currentActors.Where(group.OwnsLiveActor)
                                        .Select(actor=>actor.GetInstanceId()).OrderBy(id=>id));
                                if(!retained)
                                {
                                    LogRetention("initial_before_failure",initialRetention);
                                    LogRetention("current_before_failure",DescribeRetention());
                                }
                                TestAssert.True(retained,
                                    $"BasinGalleryEncounterRetention: real lift/gallery path must not retire/recreate active enemies; player={f.Player.GlobalPosition} active={group.IsActive} batch={group.CurrentBatch}");
                            }
                        }
                    });
                    TestAssert.True(f.Lift.IsActivated,"BasinFullRouteRequiresPassengerLift: route cannot skip the approved lift");
                    if(encounter) await LeaveAndReenterGear(f,driver,group);
                }
                finally { PrologueCanyonTraversalTests.EndTask10InputDrive(); }
            }
            finally { await f.Dispose(); }
        }
        else if (phase == BasinCounterweightPhase.Ride)
        {
            foreach (float landingX in new[] { 2724f, 2760f, 2796f })
                await Ride(tree, landingX);
        }
        else if (phase == BasinCounterweightPhase.Transfer)
        {
            foreach(float landingX in new[]{2724f,2760f,2796f}) await Transfer(tree,landingX);
        }
        else if (phase == BasinCounterweightPhase.Exits) await Exits(tree);
        else if (phase == BasinCounterweightPhase.Lifecycle) await Lifecycle(tree);
        else if (phase == BasinCounterweightPhase.Neighbors)
        {
            await CanyonMechanismRuntimeTests.RunGeneratedCounterweight(tree);
            await CanyonMechanismRuntimeTests.RunRequiredLifecycleCases(tree);
            await CanyonMechanismRuntimeTests.RunGeneratedSteamVisibility(tree);
        }
        else if (phase == BasinCounterweightPhase.NeighborLifecycle) await CanyonMechanismRuntimeTests.RunRequiredLifecycleCases(tree);
        else if (phase == BasinCounterweightPhase.Steam) await CanyonMechanismRuntimeTests.RunGeneratedSteamVisibility(tree);
        else if (phase == BasinCounterweightPhase.Fog)
        {
            await FogMovingGeometryReadabilityRuntimeTests.Run(tree);
            await FogMovingGeometrySavedControllerRuntimeTests.Run(tree);
            await FogFragmentReadabilityRuntimeTests.Run(tree);
        }
        else if (phase == BasinCounterweightPhase.AttachedExits) await AttachedExits(tree);
        else if (phase is BasinCounterweightPhase.Presentation or BasinCounterweightPhase.Stills)
            await Transfer(tree,2760,capture:true);
        else if (phase is BasinCounterweightPhase.BoardingTrace or BasinCounterweightPhase.BoardingCadence)
        {
            int originalMaxFps=Engine.MaxFps;
            if(phase==BasinCounterweightPhase.BoardingCadence) Engine.MaxFps=12;
            GD.Print($"BASIN_BOARDING_CADENCE forced_render_cap={phase==BasinCounterweightPhase.BoardingCadence} max_fps={Engine.MaxFps} physics_ticks={Engine.PhysicsTicksPerSecond} performance=false");
            var f = new Fixture(tree, new Vector2(2492,3880),full:true);
            try { await f.Start(); await Activate(f); await BoardReturn(f,2760); }
            finally { Engine.MaxFps=originalMaxFps; await f.Dispose(); }
        }
        else if (phase == BasinCounterweightPhase.Regressions)
        {
            QuestFragmentPlacementTests.Run();
            await QuestFragmentCollectionTests.Run(tree);
            await MinimapCatalogTests.Run(tree);
            await KeyboardClawTargetTests.Run(tree);
        }
        else throw new InvalidOperationException($"Phase not implemented yet: {phase}");
    }

    // Negative companion to the real airborne retention test. Drop left off
    // PistonTwo to the lower passage (the gallery wall blocks a direct right
    // walk), leave below the gallery, then return with one ordinary jump.
    private static async Task LeaveAndReenterGear(Fixture f,KeyboardRouteInputDriver driver,
        CanyonEncounterGroup group)
    {
        int completed=0,defeated=0,hp=f.Health.CurrentHealth;
        bool requireInactive=false;
        ulong playerId=f.Player.GetInstanceId();
        ulong[] oldActors=group.GetNode(group.ActorRootPath).GetChildren()
            .OfType<CharacterBody2D>().Select(a=>a.GetInstanceId()).ToArray();
        CanyonEncounterGroup.CompletedEventHandler onComplete=_=>completed++;
        CanyonEncounterGroup.GroupDefeatedEventHandler onDefeat=_=>defeated++;
        group.Completed+=onComplete; group.GroupDefeated+=onDefeat;
        var motor=f.Player.GetNode<PlayerMotor>("Motor");
        int lastDamageHealth=hp,damageRows=0,damageObservationErrors=0;
        var damageEvidence=new System.Collections.Generic.List<string>(4);
        HealthComponent.DamagedEventHandler onDamage=(remaining,impulse)=>
        {
            int before=lastDamageHealth; lastDamageHealth=remaining;
            try
            {
                if(++damageRows>4) throw new InvalidOperationException("gear-exit damage evidence capacity exceeded");
                var field=typeof(HealthComponent).GetField("_processedAttackOrder",
                    System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                if(field?.GetValue(f.Health) is not System.Collections.Generic.Queue<DamageAttackIdentity> order || order.Count==0)
                    throw new InvalidOperationException("accepted Health identity unavailable");
                DamageAttackIdentity hit=order.Last();
                Node source=GodotObject.InstanceFromId(hit.SourceRuntimeId) as Node;
                bool valid=GodotObject.IsInstanceValid(source);
                Node actor=valid?source.GetParent():null;
                bool actorValid=GodotObject.IsInstanceValid(actor);
                string path="unavailable";
                if(valid) { using NodePath sourcePath=source.GetPath(); path=sourcePath.ToString(); }
                damageEvidence.Add($"BASIN_GEAR_EXIT_DAMAGE physics={Engine.GetPhysicsFrames()} health={before}->{remaining} " +
                    $"source={hit.SourceRuntimeId} attack={hit.LocalAttackId} source_valid={valid} " +
                    $"source_type={(valid?source.GetType().Name:"unavailable")} source_path={path} " +
                    $"actor={(actorValid?actor.GetInstanceId():0)} actor_type={(actorValid?actor.GetType().Name:"unavailable")} " +
                    $"actor_scene={(actorValid?actor.SceneFilePath:"unavailable")} " +
                    $"motor={motor.GetInstanceId()} landing={motor.LandingCount} fall_height={motor.LastFallHeight:R} " +
                    $"fall_damage={motor.LastFallDamage} impulse={impulse} position={f.Player.GlobalPosition} " +
                    "accepted_identity_only=true receiver_unchanged=true");
            }
            catch { damageObservationErrors++; }
        };
        f.Health.Damaged+=onDamage;
        async Task Tick()
        {
            await f.Frames(1); driver.ObserveFrame();
            if(f.Player.GetInstanceId()!=playerId || f.Health.CurrentHealth!=hp)
                GD.Print($"BASIN_GEAR_EXIT_FAILURE physics={Engine.GetPhysicsFrames()} player={playerId}->{f.Player.GetInstanceId()} " +
                    $"health={hp}->{f.Health.CurrentHealth} position={f.Player.GlobalPosition} velocity={f.Player.Velocity} " +
                    $"active={group.IsActive} batch={group.CurrentBatch} require_inactive={requireInactive} " +
                    $"fall_applied={motor.LastFallDamageApplied} landing={motor.LandingCount} observer_only=true");
            TestAssert.True(f.Player.GetInstanceId()==playerId && f.Health.CurrentHealth==hp,
                "gear negative exit/return keeps same player and original health");
            TestAssert.True(!group.IsComplete && completed==0 && defeated==0,
                "gear departure and re-entry cannot fabricate a defeat or completion");
            if(requireInactive) TestAssert.True(!group.IsActive && group.CurrentBatch==-1 && group.LiveActorCount==0,
                "gallery continuation must not expand encounter activation along the lower return passage");
        }
        async Task Until(Func<bool> condition,int cap,string message)
        {
            for(int i=0;i<cap && !condition();i++) await Tick();
            TestAssert.True(condition(),$"{message}; position={f.Player.GlobalPosition} active={group.IsActive} batch={group.CurrentBatch}");
        }
        try
        {
            var walkway=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutBasinWalkway");
            driver.Command(InputActions.MoveLeft,true);
            // The moving lift catches a fall immediately left of PistonTwo.
            // Walk across it to the authored48px missed-lift return gap; allow
            // the unchanged ~12px braking distance before its center2676.
            await Until(()=>f.Player.GlobalPosition.X<=2690,110,"physical A reaches missed-lift return gap");
            driver.Command(InputActions.MoveLeft,false);
            await Until(()=>Floor(f.Player,walkway),140,"ordinary fall reaches original lower return walkway");
            driver.Command(InputActions.MoveRight,true);
            await Until(()=>f.Player.GlobalPosition.X>=3074,100,"physical D leaves below gallery continuation");
            driver.Command(InputActions.MoveRight,false);
            await Tick();
            TestAssert.True(f.Player.GlobalPosition.Y>=3796 && !group.IsActive &&
                group.CurrentBatch==-1 && group.LiveActorCount==0,
                "BasinGearOutsideContinuationRetires: lower Steam-side departure must still retire living enemies");
            GD.Print($"BASIN_GEAR_REAL_EXIT_OK player={f.Player.GlobalPosition} live={group.LiveActorCount} completed={completed}");
            requireInactive=true;
            driver.Command(InputActions.MoveLeft,true);
            await Until(()=>f.Player.GlobalPosition.X<=2488,180,"physical A follows original lower return passage");
            driver.Command(InputActions.MoveLeft,false);
            await Tick();
            requireInactive=false;
            driver.Command(InputActions.Jump,true); driver.Command(InputActions.MoveLeft,true);
            await Tick();
            driver.Command(InputActions.Jump,false);
            await Until(()=>group.IsActive,70,"ordinary Space+A re-enters original activation bounds");
            driver.Command(InputActions.MoveLeft,false);
            TestAssert.True(group.Definition.ActivationBounds.HasPoint(f.Player.GlobalPosition),
                "first reactivation occurs inside original authored activation bounds, not the gallery continuation");
            ulong[] newActors=group.GetNode(group.ActorRootPath).GetChildren()
                .OfType<CharacterBody2D>().Select(a=>a.GetInstanceId()).ToArray();
            TestAssert.True(group.CurrentBatch==0 && group.LiveActorCount>0 &&
                newActors.Length>0 && !newActors.Intersect(oldActors).Any(),
                "genuine re-entry restarts original batch zero with new actors, not a fake defeat or continuation teleport");
            GD.Print($"BASIN_GEAR_REAL_REENTRY_OK player={f.Player.GlobalPosition} batch={group.CurrentBatch} health={hp} completed={completed}");
        }
        finally
        {
            try { if(GodotObject.IsInstanceValid(f.Health)) f.Health.Damaged-=onDamage; }
            finally
            {
                try { driver.ReleaseAll(); }
                finally { group.Completed-=onComplete; group.GroupDefeated-=onDefeat; }
            }
            foreach(string row in damageEvidence) GD.Print(row);
            GD.Print($"BASIN_GEAR_EXIT_DAMAGE_END accepted={damageRows} observation_errors={damageObservationErrors} observer_only=true");
        }
    }

    // Shared by local route characterization and the actual Title-to-exit route.
    // Receives an already live player: never spawns, repositions, heals, resets
    // mechanism clocks, changes saves or invents an attachment.
    internal static async Task<int> TraverseForFullRoute(CharacterBody2D player,
        PrologueSector sector,Func<int,Task> frames, bool passiveFirstReel = false, bool replayFailedSwing = false,
        bool finishOnGallery = false,bool verifyRideDefense = false)
    {
        TestAssert.True(!(passiveFirstReel && replayFailedSwing), "passive and failed-input replay are distinct diagnostics");
        TestAssert.True(!finishOnGallery || !passiveFirstReel && !replayFailedSwing,
            "Gallery is a completed transfer terminal, never a passive/replay shortcut");
        var driver=KeyboardRouteInputDriver.Current ??
            throw new InvalidOperationException("Basin route requires the active keyboard driver");
        var lift=sector.GetNode<CanyonCounterweightLift>("Gameplay/Mechanisms/BasinCounterweightLift");
        var first=sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonOne");
        var second=sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonTwo");
        var gallery=sector.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryOne");
        var control=sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightControl");
        var anchor=sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightGalleryAnchor");
        var claw=player.GetNode<MechanicalClawController>("MechanicalClaw");
        var motor=player.GetNode<PlayerMotor>("Motor");
        var health=player.GetNode<HealthComponent>("Health");
        int initialHealth=health.CurrentHealth,initialJumps=motor.AcceptedJumpCount;
        ulong identity=player.GetInstanceId();
        var bell=sector.GetNode("Gameplay/Actors").GetChildren().OfType<BellBackLaborer>()
            .SingleOrDefault(actor=>actor.Name.ToString().StartsWith("canyon_v2_encounter_gear-pistons_",StringComparison.Ordinal));
        var bellHitbox=bell?.GetNode<Hitbox>(bell.AttackHitboxPath);
        var rideActors=sector.GetNode("Gameplay/Actors");
        var rideThreatLeaves=new List<AttackTelegraphVisual>(8);
        var rideGuardSources=new List<Vector2>(16);
        bool rideGuardSourcesComplete=false,rideVerticalGuardSelected=false;
        Vector2 rideChosenGuardPoint=Vector2.Zero;
        int stationaryGuardCommands=0;
        void ObserveRideGuardSource(PrologueCanyonGeneratedMainRouteTests.Task10DangerCandidate candidate)
        {
            if(rideGuardSources.Count>=16){rideGuardSourcesComplete=false;return;}
            rideGuardSources.Add(candidate.GuardSource);
        }
        PrologueCanyonGeneratedMainRouteTests.Task10EncounterDanger rideDanger=default;
        var shield=player.GetNode<PlayerShieldController>("Shield");
        var rideInput=player.GetNode<PlayerInputReader>("Input");
        var rideEnergy=player.GetNode<PlayerEnergyController>("Energy");
        var rideHurtbox=player.GetNode<Hurtbox>("Hurtbox");
        var rideHurtShape=rideHurtbox.GetNode<CollisionShape2D>("HurtboxCollider");
        var rideAttackShape=bellHitbox?.GetNode<CollisionShape2D>("AttackCollider");
        int bellContacts=0,guardedContacts=0,rideContacts=0,rideGuardedContacts=0,leftRideGuardedContacts=0;
        int rideActivePairs=0,ridePossibleOverlapPairs=0,rideDisabledActiveSamples=0;
        ulong rideEnvelopePhysics=0;
        bool ridePreviousActive=false;
        Rect2 ridePreviousAttack=default,ridePreviousHurt=default;
        string rideEnvelopeFault=null;
        int rideGuardRows=0,rideGuardOmitted=0;
        string rideGuardKey=null,rideGuardFault=null;
        bool waitShieldHeld=false,rideShieldOwner=false;
        bool boardingGuardOwnsDirections=false;
        Vector2 boardingGuardDirection=Vector2.Zero;
        ulong boardingGuardSequence=0;
        int boardingGuardSamples=0;
        int rideBellNativeFrames=0,rideBellNativeDuplicates=0,rideBellNativeActive=0,rideNativeSteps=0;
        ulong rideBellNativeFirst=0,rideBellNativeLast=0;
        bool rideBellNativeValid=true,rideBellNativeContiguous=true;
        string rideBellNativeFault=null;
        ulong rideBellIdentity=bell?.GetInstanceId()??0,rideBellParent=bell?.GetParent()?.GetInstanceId()??0;
        var rideBellHealth=bell?.GetNode<HealthComponent>("Health");
        int rideBellInitialHealth=rideBellHealth?.CurrentHealth??0;
        string stage="entry";
        void ObserveRideBellNative(BellBackLaborerFrame frame,int facing)
        {
            if(!verifyRideDefense || stage!="ride")return;
            try
            {
                ulong physics=Engine.GetPhysicsFrames();
                rideBellNativeValid &= GodotObject.IsInstanceValid(bell) && bell.IsInsideTree() &&
                    !bell.IsQueuedForDeletion() && !bell.GetTree().Paused && bell.CanProcess() && bell.IsPhysicsProcessing() &&
                    bell.GetInstanceId()==rideBellIdentity && bell.GetParent()?.GetInstanceId()==rideBellParent &&
                    GodotObject.IsInstanceValid(rideBellHealth) && rideBellInitialHealth>0 &&
                    rideBellHealth.CurrentHealth==rideBellInitialHealth && frame.State!=BellBackLaborerState.Dead;
                if(frame.AttackActive || frame.AttackTimeline.Phase==AttackPhase.Active || bellHitbox.Monitoring)
                    rideBellNativeActive++;
                if(rideBellNativeFrames==0)rideBellNativeFirst=physics;
                else
                {
                    if(physics==rideBellNativeLast)rideBellNativeDuplicates++;
                    rideBellNativeContiguous &= physics==rideBellNativeLast+1;
                }
                rideBellNativeLast=physics;
                rideBellNativeFrames++;
            }
            catch(Exception error){rideBellNativeValid=false;rideBellNativeFault??=error.ToString();}
        }
        BasinGalleryCastTrace controlTrace=null;
        BasinGalleryCastTrace controlAfterTrace=null;
        BasinGalleryCastTrace galleryTrace=null;
        Hitbox.ConnectedEventHandler contact=target=>
        {
            if(target==player.GetNode<Hurtbox>("Hurtbox"))
            {
                bellContacts++;
                if(shield.IsActive && health.CurrentHealth==initialHealth) guardedContacts++;
                if(stage=="ride")
                {
                    rideContacts++;
                    bool guarded=shield.IsActive && health.CurrentHealth==initialHealth &&
                        bellHitbox.Damage>0 && bellHitbox.GuardClass==DamageGuardClass.Normal;
                    if(guarded)
                    {
                        rideGuardedContacts++;
                        if(bellHitbox.GlobalPosition.X<target.GlobalPosition.X)leftRideGuardedContacts++;
                    }
                    GD.Print($"TASK10_BASIN_RIDE_CONTACT physics={Engine.GetPhysicsFrames()} sample={rideInput.CurrentFrameSequence} " +
                        $"actor={bell.GetInstanceId()} hitbox={bellHitbox.GetInstanceId()} attack={bell.CurrentFrame.AttackTimeline.AttackId} " +
                        $"source={bellHitbox.GlobalPosition} player_hurtbox={target.GlobalPosition} " +
                        $"guarded={guarded} class={bellHitbox.GuardClass} damage={bellHitbox.Damage} " +
                        $"health={initialHealth}->{health.CurrentHealth} held={rideInput.CurrentFrame.ShieldHeld} " +
                        $"active={shield.IsActive} direction={shield.GuardDirection}");
                }
            }
            GD.Print($"TASK10_BASIN_BELL_CONTACT source={bell.GetInstanceId()} target={target.GetPath()} physics={Engine.GetPhysicsFrames()} bell={bell.GlobalPosition} state={bell.CurrentState} player={player.GlobalPosition} shield={shield.IsActive} direction={shield.GuardDirection}");
        };
        if(bellHitbox!=null) bellHitbox.Connected+=contact;
        void ObserveRideEnvelope()
        {
            if(!verifyRideDefense || stage!="ride")return;
            ulong physics=Engine.GetPhysicsFrames();
            if(physics==rideEnvelopePhysics)return;
            try
            {
                if(!GodotObject.IsInstanceValid(bellHitbox) || !GodotObject.IsInstanceValid(rideAttackShape) ||
                    rideAttackShape.Shape==null || rideHurtShape.Shape==null || rideHurtShape.Disabled ||
                    !rideHurtbox.Monitorable)
                    throw new InvalidOperationException("Ride defense geometry has no live enabled saved collider");
                bool active=bell.CurrentFrame.AttackTimeline.Phase==AttackPhase.Active;
                if(active && (!bellHitbox.Monitoring || rideAttackShape.Disabled))rideDisabledActiveSamples++;
                Rect2 attack=rideAttackShape.GlobalTransform*rideAttackShape.Shape.GetRect();
                Rect2 hurt=rideHurtShape.GlobalTransform*rideHurtShape.Shape.GetRect();
                if(rideEnvelopePhysics!=0 && (active || ridePreviousActive))
                {
                    if(physics!=rideEnvelopePhysics+1)
                        throw new InvalidOperationException("Ride defense geometry skipped a native physics boundary");
                    rideActivePairs++;
                    // Paired native-boundary envelopes. Their union is stricter
                    // than checking the sampled endpoints separately; this is
                    // not a claim about unobserved continuous sub-step motion.
                    if(attack.Merge(ridePreviousAttack).Intersects(hurt.Merge(ridePreviousHurt),true))
                        ridePossibleOverlapPairs++;
                }
                ridePreviousAttack=attack;ridePreviousHurt=hurt;ridePreviousActive=active;
                rideEnvelopePhysics=physics;
            }
            catch(Exception error){rideEnvelopeFault??=error.ToString();}
        }
        void ObserveRideGuard(string boundary,bool requested,bool force=false)
        {
            if(!(stage=="ride" || verifyRideDefense && stage is "lower window wait" or "boarding") ||
                !GodotObject.IsInstanceValid(bell))return;
            try
            {
                var timeline=bell.CurrentFrame.AttackTimeline;
                string key=$"{stage}/{bell.CurrentState}/{timeline.AttackId}/{timeline.Phase}/{requested}/"+
                    $"{rideDanger.SourceActorId}/{rideDanger.AttackId}/{rideDanger.Phase}/"+
                    $"{rideInput.CurrentFrame.ShieldHeld}/{shield.IsActive}/{rideInput.KeyboardAimDirection}/"+
                    $"{Input.IsPhysicalKeyPressed(Key.A)}/{Input.IsPhysicalKeyPressed(Key.D)}/{Input.IsPhysicalKeyPressed(Key.L)}";
                if(!force && key==rideGuardKey)return;
                rideGuardKey=key;
                if(rideGuardRows >= (force?48:46)){rideGuardOmitted++;return;}
                rideGuardRows++;
                GD.Print("TASK10_BASIN_RIDE_GUARD "+System.Text.Json.JsonSerializer.Serialize(new
                {
                    boundary,record=rideGuardRows,cap=48,physics=Engine.GetPhysicsFrames(),
                    sequence=rideInput.CurrentFrameSequence,stage,requested,commandHeld=waitShieldHeld,
                    bell=bell.GetInstanceId(),bellX=bell.GlobalPosition.X,bellY=bell.GlobalPosition.Y,
                    sourceX=bellHitbox.GlobalPosition.X,sourceY=bellHitbox.GlobalPosition.Y,
                    state=bell.CurrentState.ToString(),timeline.AttackId,phase=timeline.Phase.ToString(),
                    remaining=timeline.PhaseRemainingSeconds,
                    dangerActor=rideDanger.SourceActorId,dangerAttack=rideDanger.AttackId,
                    dangerPhase=rideDanger.Phase.ToString(),dangerRemaining=rideDanger.RemainingSeconds,
                    dangerX=rideDanger.GuardSource.X,dangerY=rideDanger.GuardSource.Y,
                    chosenX=rideChosenGuardPoint.X,chosenY=rideChosenGuardPoint.Y,
                    verticalAlternative=rideVerticalGuardSelected,guardSources=rideGuardSources.Count,
                    sourceCoverageComplete=rideGuardSourcesComplete,missingSources=rideDanger.MissingGuardSourceCount,
                    player=identity,x=player.GlobalPosition.X,y=player.GlobalPosition.Y,
                    vx=player.Velocity.X,vy=player.Velocity.Y,floor=player.IsOnFloor(),slides=player.GetSlideCollisionCount(),
                    axis=rideInput.CurrentFrame.MoveAxis,aimX=rideInput.KeyboardAimDirection.X,aimY=rideInput.KeyboardAimDirection.Y,
                    rawA=Input.IsPhysicalKeyPressed(Key.A),rawD=Input.IsPhysicalKeyPressed(Key.D),
                    rawW=Input.IsPhysicalKeyPressed(Key.W),rawS=Input.IsPhysicalKeyPressed(Key.S),
                    rawJ=Input.IsPhysicalKeyPressed(Key.J),rawK=Input.IsPhysicalKeyPressed(Key.K),rawL=Input.IsPhysicalKeyPressed(Key.L),
                    rawFrameL=rideInput.RawFrame.ShieldHeld,sampledL=rideInput.CurrentFrame.ShieldHeld,
                    active=shield.IsActive,guardX=shield.GuardDirection.X,guardY=shield.GuardDirection.Y,
                    canGuard=shield.CanBeginGuard,energy=rideEnergy.EnergyRatio,heat=rideEnergy.HeatRatio,
                    overheated=rideEnergy.IsOverheated,claw=claw.CurrentFrame.State.ToString(),
                    cast=claw.CurrentFrame.CastId,liftTime=lift.ElapsedSeconds,liftPhase=lift.Frame.Phase01,
                    observationOnly=false,extraQueries=0,extraFrames=0,
                    policy=verifyRideDefense?"all-source-ride":"bilateral-ride"
                }));
            }
            catch(Exception error){rideGuardFault??=error.ToString();}
        }
        HealthComponent.DamagedEventHandler damage=(remaining,impulse)=>
        {
            ObserveRideGuard("damage",waitShieldHeld,true);
            string actors=string.Join(';',sector.GetNode("Gameplay/Actors").GetChildren().OfType<Node2D>()
                .Select(actor=>$"{actor.Name}@{actor.GlobalPosition}"));
            GD.Print($"TASK10_BASIN_ROUTE_DAMAGE stage={stage} physics={Engine.GetPhysicsFrames()} player={player.GlobalPosition} velocity={player.Velocity} health={initialHealth}->{remaining} impulse={impulse} floor={player.IsOnFloor()} last_fall={motor.LastFallHeight}/{motor.LastFallDamage} bell_state={bell?.CurrentState} lift_time={lift.ElapsedSeconds} actors={actors}");
        };
        health.Damaged+=damage;
        driver.ReleaseAll();
        async Task Step(int count=1)
        {
            for(int i=0;i<count;i++)
            {
                boardingGuardOwnsDirections=false;
                // Respond to the real visible sweep tell, not elapsed lift time.
                // A permanent guard overheats before the platform returns.
                bool defend=(stage is "lower window wait" or "boarding") &&
                    GodotObject.IsInstanceValid(bell) && bell.GlobalPosition.X>player.GlobalPosition.X &&
                    (bell.CurrentState==BellBackLaborerState.Attack ||
                     bell.CurrentState==BellBackLaborerState.Telegraph &&
                     bell.CurrentFrame.AttackTimeline.PhaseRemainingSeconds<=.35f);
                if(stage=="ride" || verifyRideDefense && stage is "lower window wait" or "boarding")
                {
                    defend=GodotObject.IsInstanceValid(bell) && GodotObject.IsInstanceValid(bellHitbox) &&
                        (bell.CurrentState==BellBackLaborerState.Attack ||
                         bell.CurrentState==BellBackLaborerState.Telegraph &&
                         bell.CurrentFrame.AttackTimeline.PhaseRemainingSeconds<=.35f);
                    Vector2 guardPoint=GodotObject.IsInstanceValid(bellHitbox)?bellHitbox.GlobalPosition:Vector2.Zero;
                    rideVerticalGuardSelected=false;
                    rideGuardSources.Clear();
                    rideGuardSourcesComplete=false;
                    if(verifyRideDefense)
                    {
                        // The saved residents remain live while waiting for the
                        // carriage. Use the existing whole-encounter publication
                        // scanner, not the retained Bell alone or an elapsed clock.
                        bool canChooseStationary=stage is "boarding" or "ride";
                        rideGuardSourcesComplete=canChooseStationary;
                        rideDanger=PrologueCanyonGeneratedMainRouteTests.Task10ObserveEncounterDanger(
                            rideActors,"canyon.v2.encounter.gear-pistons",bell,false,rideThreatLeaves,
                            candidateObserver:canChooseStationary?ObserveRideGuardSource:null,
                            guardCapsule:PrologueCanyonGeneratedMainRouteTests.Task10CurrentGuardCapsule(player));
                        defend=rideDanger.Threat && (rideDanger.Phase==AttackPhase.Active ||
                            rideDanger.Phase==AttackPhase.Telegraph && rideDanger.RemainingSeconds<=.35f);
                        guardPoint=rideDanger.GuardSource;
                        rideGuardSourcesComplete &= rideDanger.CandidateObserverFaults==0 &&
                            rideDanger.MissingGuardSourceCount==0;
                        if(defend && canChooseStationary)
                        {
                            Vector2 chosen=StationaryGuardDirection(player.GlobalPosition,guardPoint,
                                rideGuardSources,rideGuardSourcesComplete);
                            Vector2 original=PlayerKeyboardAimModel.ResolveDirection(guardPoint-player.GlobalPosition,1);
                            if((chosen==Vector2.Up || chosen==Vector2.Down) && !chosen.IsEqualApprox(original))
                            {
                                // This changes only the legal keyboard orientation;
                                // the original all-source winner remains in rideDanger.
                                guardPoint=player.GlobalPosition+chosen*80;
                                rideVerticalGuardSelected=true;
                                stationaryGuardCommands++;
                                if(stationaryGuardCommands<=8)
                                    GD.Print($"TASK10_BASIN_STATIONARY_GUARD physics={Engine.GetPhysicsFrames()} " +
                                        $"stage={stage} original_source={rideDanger.GuardSource} chosen={chosen} " +
                                        $"sources={rideGuardSources.Count} complete={rideGuardSourcesComplete} " +
                                        $"missing={rideDanger.MissingGuardSourceCount} unknown_shape={rideDanger.NearContactUnknownActive} " +
                                        $"player={player.GlobalPosition} velocity={player.Velocity} health={initialHealth}->{health.CurrentHealth}");
                            }
                        }
                    }
                    rideChosenGuardPoint=guardPoint;
                    ObserveRideGuard("before-command",defend);
                    ObserveRideEnvelope();
                    // Real L plus the current attack-source direction. Refresh on
                    // each original tick so a sampled horizontal turn releases A/D
                    // instead of walking across the deck for the whole guard.
                    driver.PointerButton(MouseButton.Right,defend,player,guardPoint);
                    rideShieldOwner=true;
                    waitShieldHeld=defend;
                    if(verifyRideDefense && stage=="boarding" && defend)
                    {
                        Vector2 source=guardPoint-player.GlobalPosition;
                        boardingGuardOwnsDirections=source.IsFinite() && source.LengthSquared()>1e-6f;
                        boardingGuardDirection=PlayerKeyboardAimModel.ResolveDirection(source,1);
                        boardingGuardSequence=rideInput.CurrentFrameSequence;
                    }
                }
                else if(rideShieldOwner)
                {
                    // Pair the driver's pointer intent, not just raw L: otherwise
                    // cached guard aim overrides the following gallery W+D/K.
                    driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero);
                    rideShieldOwner=false;
                    waitShieldHeld=false;
                }
                if(!rideShieldOwner && stage!="ride" && defend!=waitShieldHeld)
                {
                    driver.Command(InputActions.Shield,defend);
                    waitShieldHeld=defend;
                }
                await frames(1);
                ObserveRideGuard("after-sample",defend);
                ObserveRideEnvelope();
                TestAssert.True(GodotObject.IsInstanceValid(player) && player.IsInsideTree() &&
                    player.GetInstanceId()==identity && health.CurrentHealth==initialHealth && initialHealth>0,
                    $"Basin route keeps the same live player without damage or healing; stage={stage} position={player.GlobalPosition} health={initialHealth}->{health.CurrentHealth}");
                TestAssert.True(rideGuardFault==null,"Basin ride scalar observation remains valid: "+rideGuardFault);
                TestAssert.True(rideEnvelopeFault==null,"Basin ride defense geometry remains valid: "+rideEnvelopeFault);
            }
        }
        async Task Until(Func<bool> condition,int cap,string stage)
        {
            for(int i=0;i<cap && !condition();i++) await Step();
            TestAssert.True(condition(),$"BasinRoute {stage}: player={player.GlobalPosition} velocity={player.Velocity} lift={lift.ElapsedSeconds} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.ReleaseKind}");
        }
        async Task Walk(float target,int cap,bool stopOnFirst=false)
        {
            if(stopOnFirst)
            {
                var input=player.GetNode<PlayerInputReader>("Input");
                int used=0;
                float predictedStop=float.NaN;
                for(;used<cap;used++)
                {
                    TestAssert.True(Floor(player,first),
                        "Basin control approach retains actual PistonOne support");
                    var stop=FreeMotionForecast.Neutral(player.Velocity.X,true,motor.MotionSettings,
                        motor.ThrusterState.Module,1d/Engine.PhysicsTicksPerSecond,cap-used,.5f);
                    TestAssert.True(stop.SpeedThresholdReached,
                        "Basin control neutral forecast fits original approach budget");
                    predictedStop=player.GlobalPosition.X+stop.NetDistance;
                    bool stopped=Mathf.Abs(player.GlobalPosition.X-target)<=1.5f &&
                        Mathf.Abs(player.Velocity.X)<=.5f && Mathf.Abs(input.Current.MoveAxis)<=.001f &&
                        !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D);
                    if(stopped) break;
                    int axis=Mathf.Abs(target-predictedStop)<=1.5f ? 0 : (int)Mathf.Sign(target-predictedStop);
                    driver.Command(InputActions.MoveLeft,axis<0);
                    driver.Command(InputActions.MoveRight,axis>0);
                    await Step();
                }
                driver.Command(InputActions.MoveLeft,false);
                driver.Command(InputActions.MoveRight,false);
                TestAssert.True(used<cap && Floor(player,first) &&
                    Mathf.Abs(player.GlobalPosition.X-target)<=1.5f && Mathf.Abs(player.Velocity.X)<=.5f &&
                    Mathf.Abs(input.Current.MoveAxis)<=.001f,
                    $"Basin control station stops with original{cap} budget; player={player.GlobalPosition} velocity={player.Velocity}");
                GD.Print($"TASK10_BASIN_CONTROL_STOP_OK frames={used}/{cap} player={player.GlobalPosition} " +
                    $"velocity={player.Velocity} predicted_stop_x={predictedStop:0.000} " +
                    $"sample={input.CurrentFrameSequence} axis={input.Current.MoveAxis} " +
                    $"health={initialHealth}->{health.CurrentHealth} " +
                    "observation=physics_signal_before_node_process no_pose_or_velocity_write=true");
                await Step(4);
                return;
            }
            float direction=Mathf.Sign(target-player.GlobalPosition.X);
            if(Mathf.Abs(target-player.GlobalPosition.X)<12) return;
            StringName action=direction>0?InputActions.MoveRight:InputActions.MoveLeft;
            Key physical=direction>0?Key.D:Key.A;
            bool released=false;
            Exception failure=null;
            int boardingSamples=0;
            ulong lastBoardingSample=0;
            var walkInput=player.GetNode<PlayerInputReader>("Input");
            var walkBodyShape=player.GetNode<CollisionShape2D>("BodyCollider");
            (ulong Physics,ulong Sequence,Vector2 Position,Vector2 Velocity,Rect2 Body,
                bool Floor,bool Wall,bool RawA,bool RawD,float RawAxis,float Axis,float Scale,
                MechanicalClawState Claw) lastReturn=default;
            // Input release is a physics decision, independent of render cadence
            // or diagnostic buffers. Never re-press a withdrawn native key.
            var observer=new BasinBoardingTickObserver { ProcessPhysicsPriority=100 };
            observer.Sample=_=>
            {
                if(released || failure!=null) return;
                try
                {
                    bool guardKeys=Input.IsPhysicalKeyPressed(Key.A)==(boardingGuardDirection.X<-.5f) &&
                        Input.IsPhysicalKeyPressed(Key.D)==(boardingGuardDirection.X>.5f) &&
                        Input.IsPhysicalKeyPressed(Key.W)==(boardingGuardDirection.Y<-.5f) &&
                        Input.IsPhysicalKeyPressed(Key.S)==(boardingGuardDirection.Y>.5f);
                    bool guardTakeover=verifyRideDefense && stage=="boarding" && boardingGuardOwnsDirections &&
                        rideShieldOwner && waitShieldHeld && guardKeys && Input.IsPhysicalKeyPressed(Key.L) &&
                        walkInput.CurrentFrameSequence>boardingGuardSequence && walkInput.CurrentFrame.ShieldHeld &&
                        shield.IsActive && shield.GuardDirection.Dot(boardingGuardDirection)>.99f &&
                        walkInput.KeyboardAimDirection.Dot(boardingGuardDirection)>.99f;
                    bool held=Godot.Input.IsPhysicalKeyPressed(physical);
                    TestAssert.True(held || guardTakeover,
                        $"BasinRoute owned {physical} withdrawn without a fresh actual guard takeover before threshold release");
                    if(!held && guardTakeover)
                    {
                        boardingGuardSamples++;
                        if(boardingGuardSamples<=8)
                            GD.Print($"TASK10_BASIN_BOARDING_GUARD_TAKEOVER physics={Engine.GetPhysicsFrames()} " +
                                $"sample={walkInput.CurrentFrameSequence} command_sample={boardingGuardSequence} " +
                                $"player={player.GlobalPosition} source={bellHitbox.GlobalPosition} aim={boardingGuardDirection} " +
                                $"raw_AD={Input.IsPhysicalKeyPressed(Key.A)}/{Input.IsPhysicalKeyPressed(Key.D)} " +
                                $"raw_WS={Input.IsPhysicalKeyPressed(Key.W)}/{Input.IsPhysicalKeyPressed(Key.S)} " +
                                $"shield={walkInput.CurrentFrame.ShieldHeld}/{shield.IsActive} hp={health.CurrentHealth} " +
                                "walk_intent_retained=true extra_key_represses=0 original_deadline=true");
                    }
                    if(stage=="return to Bell")
                        lastReturn=(Engine.GetPhysicsFrames(),walkInput.CurrentFrameSequence,
                            player.GlobalPosition,player.Velocity,
                            walkBodyShape.GlobalTransform*walkBodyShape.Shape.GetRect(),
                            player.IsOnFloor(),player.IsOnWall(),Input.IsPhysicalKeyPressed(Key.A),
                            Input.IsPhysicalKeyPressed(Key.D),walkInput.RawFrame.MoveAxis,
                            walkInput.CurrentFrame.MoveAxis,motor.EffectiveMovementScale,claw.CurrentFrame.State);
                    if(stage=="boarding" && boardingSamples<12 && Engine.GetPhysicsFrames()>=lastBoardingSample+10)
                    {
                        lastBoardingSample=Engine.GetPhysicsFrames(); boardingSamples++;
                        string collisions=string.Join(';',Enumerable.Range(0,player.GetSlideCollisionCount()).Select(index=>
                        {
                            var collision=player.GetSlideCollision(index);
                            return $"{(collision.GetCollider() as Node)?.Name}:{collision.GetNormal()}";
                        }));
                        GD.Print($"TASK10_BASIN_BOARDING_SAMPLE physics={lastBoardingSample} player={player.GlobalPosition} velocity={player.Velocity} move={player.GetNode<PlayerInputReader>("Input").CurrentFrame.MoveAxis} scale={motor.EffectiveMovementScale} claw={claw.CurrentFrame.State} bell={bell?.GlobalPosition} bell_state={bell?.CurrentState} shield={shield.IsActive} collisions={collisions}");
                    }
                    if(direction*(player.GlobalPosition.X-target)>=-12)
                    { driver.Command(action,false); released=true; }
                }
                catch(Exception error) { failure=error; }
            };
            driver.Command(action,true);
            player.AddChild(observer);
            try
            {
                for(int i=0;i<cap && !released && failure==null;i++) await Step();
                if(failure!=null) throw new InvalidOperationException("Basin route walk input failed",failure);
                if(!released && stage=="return to Bell")
                    GD.Print("BASIN_RETURN_WALK_FAILURE "+System.Text.Json.JsonSerializer.Serialize(new
                    {
                        LastPhysics=lastReturn.Physics,CurrentPhysics=Engine.GetPhysicsFrames(),
                        lastReturn.Sequence,Position=lastReturn.Position.ToString(),Velocity=lastReturn.Velocity.ToString(),
                        Body=lastReturn.Body.ToString(),lastReturn.Floor,lastReturn.Wall,lastReturn.RawA,lastReturn.RawD,
                        lastReturn.RawAxis,lastReturn.Axis,lastReturn.Scale,Claw=lastReturn.Claw.ToString(),
                        Target=target,Cap=cap,BeforeKeyWithdrawal=Input.IsPhysicalKeyPressed(physical),
                        Contacts=Enumerable.Range(0,player.GetSlideCollisionCount()).Select(index=>
                        {
                            KinematicCollision2D hit=player.GetSlideCollision(index); // Borrowed.
                            Node collider=hit.GetCollider() as Node;
                            CollisionShape2D shape=hit.GetColliderShape() as CollisionShape2D;
                            return new
                            {
                                Collider=collider?.GetPath().ToString(),Id=collider?.GetInstanceId()??0,
                                Normal=hit.GetNormal().ToString(),Point=hit.GetPosition().ToString(),
                                ShapeIndex=hit.GetColliderShapeIndex(),ShapePath=shape?.GetPath().ToString(),
                                Disabled=shape?.Disabled,Bounds=shape?.Shape==null?null:
                                    (shape.GlobalTransform*shape.Shape.GetRect()).ToString()
                            };
                        }).ToArray()
                    }));
                TestAssert.True(released,$"BasinRoute walk to{target} reaches threshold; actual={player.GlobalPosition}");
                await Step(4);
            }
            finally { observer.Sample=null; driver.Command(action,false); observer.Free(); }
        }
        GraveLampMoth FindControlSightMoth()
        {
            // Pilot perception, separate from the passive trace: an enemy's
            // collider can block the control ray while its anchor is outside W's
            // cone. Do not force that enemy into the claw preview or selection.
            var excluded=new Godot.Collections.Array<Rid>();
            using var excludedOwner=(Godot.Collections.Array)excluded;
            void Exclude(Node node)
            {
                if(node is CollisionObject2D collision) excluded.Add(collision.GetRid());
                for(int i=0;i<node.GetChildCount();i++) Exclude(node.GetChild(i));
            }
            Exclude(player);
            using var query=PhysicsRayQueryParameters2D.Create(player.GlobalPosition,control.WorldAnchor);
            query.CollideWithAreas=true;query.CollideWithBodies=true;
            query.CollisionMask=CombatCollisionLayers.World|CombatCollisionLayers.Enemy|CombatCollisionLayers.ThreadTarget;
            query.Exclude=excluded;
            using var hit=player.GetWorld2D().DirectSpaceState.IntersectRay(query);
            Node collider=hit.ContainsKey("collider") ? hit["collider"].AsGodotObject() as Node : null;
            for(Node node=collider;node!=null;node=node.GetParent())
                if(node is ThreadTarget2D thread && thread.TryGetEligibleReceiver(player,out Node receiver) &&
                    receiver is GraveLampMoth moth &&
                    MechanicalClawTargetResolver.ColliderBelongsToSurface(collider,thread))
                {
                    GD.Print($"TASK10_BASIN_CONTROL_SIGHT_MOTH actor={moth.GetInstanceId()} target={thread.GetInstanceId()} " +
                        $"physics={Engine.GetPhysicsFrames()} preview={claw.KeyboardPreview.Surface?.GetInstanceId() ?? 0} " +
                        "native_first_hit=true pilot_perception=true selection_unchanged=true");
                    return moth;
                }
            return null;
        }
        async Task ClearPreviewMoth(GraveLampMoth moth)
        {
            var binding=moth.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
            var targetHealth=moth.GetNode<HealthComponent>("Health");
            var hurtbox=moth.GetNode<Hurtbox>("Hurtbox");
            var combat=player.GetNode<PlayerCombat>("Combat");
            var hitbox=combat.GetNode<Hitbox>("AttackHitbox");
            var attackShape=hitbox.GetChildren().OfType<CollisionShape2D>().Single();
            var targetShape=hurtbox.GetChildren().OfType<CollisionShape2D>().Single();
            var upPose=combat.GlobalTransform*new Transform2D(0,new Vector2(0,-42))*attackShape.Transform;
            TestAssert.True(binding?.Profile!=null && targetHealth.CurrentHealth>0 &&
                !combat.IsAttacking && player.GetNode<PlayerActionModeController>("ActionModes").CanBeginAttack &&
                attackShape.Shape.Collide(upPose,targetShape.Shape,targetShape.GlobalTransform),
                "obstructing saved resident Moth is in the real standing up-strike geometry");
            int before=targetHealth.CurrentHealth,after=-1,connections=0,starts=0;
            int bellBefore=GodotObject.IsInstanceValid(bell) ? bell.GetNode<HealthComponent>("Health").CurrentHealth : -1;
            ulong mothId=moth.GetInstanceId(),hitboxId=hitbox.GetInstanceId(),attackId=0;
            string scene=moth.SceneFilePath,subtype=binding.Profile.SubtypeId;
            bool up=false,exactReceipt=false;
            Exception receiptFailure=null;
            PlayerCombat.AttackStartedEventHandler started=(id,direction)=>
            { starts++;attackId=id;up=direction==(int)AttackDirection.Up; };
            PlayerCombat.AttackConnectedEventHandler connected=touched=>
            {
                if(!ReferenceEquals(touched,hurtbox)) return;
                try
                {
                    var accepted=typeof(HealthComponent).GetField("_processedAttackOrder",
                        System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)
                        ?.GetValue(targetHealth) as System.Collections.Generic.Queue<DamageAttackIdentity>;
                    connections++;after=targetHealth.CurrentHealth;
                    exactReceipt=ReferenceEquals(touched.ResolveDamageReceiver(),moth) && attackId!=0 &&
                        accepted is { Count:>0 } && accepted.Last()==new DamageAttackIdentity(hitboxId,attackId);
                }
                catch(Exception error) { receiptFailure=error; }
            };
            combat.AttackStarted+=started;combat.AttackConnected+=connected;
            try
            {
                driver.Command(InputActions.Attack,true);await Step(2);driver.Command(InputActions.Attack,false);
                int observed=0;
                for(;observed<30 && (connections==0 || combat.IsAttacking);observed++)
                {
                    await Step();
                    TestAssert.True(Floor(player,first),"standing Moth counter keeps the original PistonOne support");
                }
                TestAssert.True(receiptFailure==null && starts==1 && up && connections==1 && exactReceipt &&
                    after==Math.Max(0,before-1) && !combat.IsAttacking && observed<30 &&
                    (bellBefore<0 || GodotObject.IsInstanceValid(bell) && bell.GetNode<HealthComponent>("Health").CurrentHealth==bellBefore),
                    $"one real Up J must damage only its exact Moth receiver without consuming the retained Bell; " +
                    $"starts={starts} connections={connections} up={up} exact={exactReceipt} hp={before}->{after} error={receiptFailure}");
                GD.Print($"TASK10_BASIN_CONTROL_MOTH_COUNTER_OK actor={mothId} scene={scene} subtype={subtype} " +
                    $"health={before}->{after} source={hitboxId} attack={attackId} exact_receiver=true up_J=1 " +
                    $"bell_health={bellBefore} player_health={health.CurrentHealth} observation={observed}/30 support=PistonOne " +
                    "production_target_priority_unchanged=true");
            }
            finally
            {
                driver.Command(InputActions.Attack,false);
                combat.AttackStarted-=started;combat.AttackConnected-=connected;
            }
        }
        async Task ReturnAcrossResidentShelf() => await ReturnGalleryThroatToPistonTwo(player,sector,Step);
        async Task<bool> TransferViaResidentShelf()
        {
            var shelf=sector.GetNode<StaticBody2D>("Gameplay/Collision/GearResidentRightShelf");
            var side=shelf.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
            var shelfShape=shelf.GetNode<CollisionShape2D>("Collision");
            var bodyShape=player.GetNode<CollisionShape2D>("BodyCollider");
            Rect2 shelfBounds=shelfShape.GlobalTransform*shelfShape.Shape.GetRect();
            TestAssert.True(shelfBounds==new Rect2(2856,3720,136,24) &&
                claw.KeyboardPreview.Surface==side && claw.KeyboardPreview.Snapshot.HasLineOfSight &&
                claw.KeyboardPreview.Snapshot.SurfaceNormal.Dot(Vector2.Left)>.99f,
                "two-stage transfer starts only from the actual saved side-wall preview");
            var input=player.GetNode<PlayerInputReader>("Input");
            using var trajectory = passiveFirstReel || replayFailedSwing ? new BasinFirstReelTrace(player, claw, side, anchor, shelfBounds, replayFailedSwing) : null;
            int dashes=motor.AcceptedDashCount,edges=driver.ClawEdges,boosts=0,releases=0;
            ulong sideCast=0,pullStart=0,releaseSequence=0;
            bool sideReel=false,boostSample=false,replayBoostSample=false;
            Exception receiptFailure=null;
            void Observe(MechanicalClawFrame frame,GrappleSurface2D target)
            {
                try
                {
                    if(frame.LockedTargetRuntimeId==side.GetInstanceId() && frame.CastId!=0)
                    {
                        if(sideCast==0){sideCast=frame.CastId;pullStart=Engine.GetPhysicsFrames();}
                        if(frame.CastId==sideCast && frame.State==MechanicalClawState.Reel)sideReel=true;
                    }
                    if(frame.CastId==sideCast && sideCast!=0 && frame.ReleaseKind==MechanicalClawReleaseKind.Boost)
                    {
                        if(frame.State==MechanicalClawState.Boost)
                        {
                            boosts++;
                            if (replayFailedSwing)
                                replayBoostSample = frame.LockedTargetRuntimeId == side.GetInstanceId() && frame.BoostConsumed &&
                                    input.CurrentFrameSequence > releaseSequence && input.CurrentFrame.MoveAxis == -1 &&
                                    input.CurrentFrame.JumpPressed && input.CurrentFrame.JumpHeld &&
                                    !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.W);
                            boostSample=frame.LockedTargetRuntimeId==side.GetInstanceId() && frame.BoostConsumed &&
                                input.CurrentFrameSequence>releaseSequence && input.CurrentFrame.MoveAxis== -1 &&
                                input.CurrentFrame.JumpPressed && input.CurrentFrame.JumpHeld &&
                                !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.W) &&
                                frame.CasterWorldPosition.DistanceTo(frame.DockPoint)<=100;
                        }
                        if(frame.State==MechanicalClawState.Released && frame.BoostConsumed)releases++;
                    }
                }
                catch(Exception error){receiptFailure=error;}
            }
            async Task TransferStep()
            {
                await Step();
                if(receiptFailure!=null)throw new InvalidOperationException("two-stage claw receipt failed",receiptFailure);
                TestAssert.True(pullStart==0 || Engine.GetPhysicsFrames()-pullStart<=150,
                    $"shelf transfer retains one150-physics window; elapsed={Engine.GetPhysicsFrames()-pullStart} " +
                    $"stage={stage} player={player.GlobalPosition} claw={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure}");
                TestAssert.True(motor.AcceptedJumpCount==initialJumps && motor.AcceptedDashCount==dashes,
                    "side-wall Space is an attached boost, never an extra ordinary jump or dash");
            }
            claw.ClawFrameChanged+=Observe;
            try
            {
                stage="resident shelf cast";
                driver.Command(InputActions.Claw,true);
                for(int sample=0;sample<4 && claw.CurrentFrame.State==MechanicalClawState.Idle;sample++)await TransferStep();
                TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Aim or MechanicalClawState.Windup or MechanicalClawState.Travel,
                    "first native K is consumed before releasing its real diagonal direction");
                driver.Command(InputActions.Claw,false);
                driver.Command(InputActions.MoveUp,false);driver.Command(InputActions.MoveRight,false);
                for(int sample=0;sample<30 && sideCast==0;sample++)await TransferStep();
                TestAssert.True(sideCast!=0,"first K locks the real resident shelf, not a forced target");
                if (passiveFirstReel)
                {
                    stage = "passive first side Reel";
                    while (!trajectory.Terminal && trajectory.Count < 150) await TransferStep();
                    trajectory.AssertComplete();
                    return false;
                }
                if (replayFailedSwing)
                {
                    stage = "failed Swing input replay arrival";
                    while (!(claw.CurrentFrame.CastId == sideCast && claw.CurrentTarget == side &&
                        claw.CurrentFrame.State == MechanicalClawState.Swing && claw.CurrentFrame.Arrived &&
                        player.GlobalPosition.DistanceTo(claw.CurrentFrame.DockPoint) <= 8)) await TransferStep();
                    TestAssert.True(sideReel && !input.CurrentFrame.JumpHeld && !Input.IsPhysicalKeyPressed(Key.Space),
                        "failed-input replay reaches real Arrived/Swing before its one fresh A+Space");
                    releaseSequence = input.CurrentFrameSequence;
                    driver.Command(InputActions.MoveLeft, true); driver.Command(InputActions.Jump, true);
                    while (releases == 0) await TransferStep();
                    TestAssert.True(boosts == 1 && releases == 1 && replayBoostSample,
                        "failed-input replay publishes one same-cast fresh A+Space Boost and Released");
                    driver.Command(InputActions.MoveLeft, false); driver.Command(InputActions.Jump, false);
                    driver.Command(InputActions.MoveRight, true);
                    stage = "failed Swing input replay independent boundaries";
                    while (trajectory.Count < 150) await TransferStep();
                    trajectory.AssertReplayComplete();
                    return false;
                }
                stage="resident shelf arrived launch";
                while(!(claw.CurrentFrame.CastId==sideCast && claw.CurrentTarget==side &&
                    claw.CurrentFrame.LockedTargetRuntimeId==side.GetInstanceId() &&
                    claw.CurrentFrame.State==MechanicalClawState.Swing && claw.CurrentFrame.Arrived &&
                    player.GlobalPosition.DistanceTo(claw.CurrentFrame.DockPoint)<=8))await TransferStep();
                TestAssert.True(sideReel && !input.CurrentFrame.JumpHeld && !input.CurrentFrame.JumpPressed &&
                    !Input.IsPhysicalKeyPressed(Key.Space),"actual same-cast Arrived/Swing precedes a fresh sampled Space edge");
                GD.Print($"TASK10_BASIN_SIDE_SWING_LAUNCH_OK cast={sideCast} elapsed={Engine.GetPhysicsFrames()-pullStart}/150 " +
                    $"player={player.GlobalPosition} dock={claw.CurrentFrame.DockPoint} target={side.GetPath()} " +
                    $"state={claw.CurrentFrame.State} arrived={claw.CurrentFrame.Arrived} rope={claw.CurrentFrame.RopeLength} " +
                    $"distance={player.GlobalPosition.DistanceTo(claw.CurrentFrame.DockPoint)} velocity={player.Velocity}");
                releaseSequence=input.CurrentFrameSequence;
                stage="resident shelf boost";
                driver.Command(InputActions.MoveLeft,true);driver.Command(InputActions.Jump,true);
                while(releases==0)await TransferStep();
                TestAssert.True(boosts==1 && releases==1 && boostSample && player.Velocity.Y<0,
                    "same-cast real A+Space publishes Boost/Released once and actually rises");
                driver.Command(InputActions.MoveLeft,false);driver.Command(InputActions.Jump,false);
                driver.Command(InputActions.MoveRight,true);
                var galleryShape=gallery.GetNode<CollisionShape2D>("Collision");
                var fragment=sector.GetNode<QuestFragment>("Gameplay/QuestFragments/gear-gallery");
                var fragmentShape=fragment.GetNode<CollisionShape2D>("Collision");
                bool collected=fragment.IsCollected; int feedback=fragment.FeedbackCount;
                Rect2 deck=galleryShape.GlobalTransform*galleryShape.Shape.GetRect();
                Rect2 pickup=fragmentShape.GlobalTransform*fragmentShape.Shape.GetRect();
                TestAssert.True(!bodyShape.Disabled && bodyShape.Shape is CapsuleShape2D &&
                    !galleryShape.Disabled && galleryShape.Shape is RectangleShape2D &&
                    Mathf.Abs(bodyShape.GlobalTransform.X.Y)<.001f && Mathf.Abs(bodyShape.GlobalTransform.Y.X)<.001f &&
                    deck==new Rect2(3040,3756,240,40),"landing uses the actual complete saved capsule and Gallery deck");
                using var supportProbe=new KinematicCollision2D();
                bool FullGallery()
                {
                    Rect2 body=bodyShape.GlobalTransform*bodyShape.Shape.GetRect();
                    if(!player.IsOnFloor() || body.Position.X<deck.Position.X || body.End.X>deck.End.X ||
                        Mathf.Abs(body.End.Y-deck.Position.Y)>1.1f)return false;
                    if(Floor(player,gallery))return true;
                    return player.TestMove(player.GlobalTransform,Vector2.Down*.001f,supportProbe,player.SafeMargin,true) &&
                        supportProbe.GetCollider()==gallery && supportProbe.GetNormal().Y<-.7f;
                }
                stage="gallery actual landing";
                while(!FullGallery())await TransferStep();
                ulong neutralSequence=input.CurrentFrameSequence;
                driver.Command(InputActions.MoveRight,false);
                // Preserve the original45-physics floor gate after genuine landing.
                await Until(()=>Floor(player,gallery),45,"shelf release onto gallery");
                bool Settled()=>FullGallery() && claw.CurrentFrame.State==MechanicalClawState.Idle &&
                    !motor.IsDashing && !motor.HasGrappleReleaseMomentum &&
                    input.CurrentFrameSequence>neutralSequence && Mathf.Abs(player.Velocity.X)<=.5f &&
                    Mathf.Abs(input.CurrentFrame.MoveAxis)<=.001f && Mathf.Abs(input.RawFrame.MoveAxis)<=.001f &&
                    !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D);
                void CheckFragment()=>TestAssert.True(fragment.IsCollected==collected && fragment.FeedbackCount==feedback,
                    "same transfer leaves the actual Gallery fragment untouched");
                stage="gallery neutral braking";
                while(!Settled())
                {
                    TestAssert.True(FullGallery(),"neutral braking retains exact full Gallery support");
                    int remaining=150-checked((int)(Engine.GetPhysicsFrames()-pullStart));
                    TestAssert.True(remaining>17,"same original150 includes input handoff, stopping and15 hold");
                    var stop=FreeMotionForecast.Neutral(player.Velocity.X,true,motor.MotionSettings,
                        motor.ThrusterState.Module,1d/Engine.PhysicsTicksPerSecond,remaining-17,.5f);
                    Rect2 body=bodyShape.GlobalTransform*bodyShape.Shape.GetRect();
                    float handoff=2f*player.Velocity.X/Engine.PhysicsTicksPerSecond;
                    float left=Mathf.Min(body.Position.X,body.Position.X+handoff+stop.NetDistance);
                    float right=Mathf.Max(body.End.X,body.End.X+handoff+stop.NetDistance);
                    Rect2 sweep=new(new Vector2(left,body.Position.Y),new Vector2(right-left,body.Size.Y));
                    TestAssert.True(stop.SpeedThresholdReached && left>=deck.Position.X && right<=deck.End.X &&
                        !sweep.Intersects(pickup),"live neutral forecast stays on Gallery, before actual fragment, inside original150");
                    CheckFragment(); await TransferStep();
                }
                stage="gallery stable hold";
                for(int hold=0;hold<15;hold++){await TransferStep();TestAssert.True(Settled(),"original15 hold stays settled on exact Gallery");CheckFragment();}
                TestAssert.True(driver.ClawEdges==edges+1 && boosts==1 && releases==1,
                    "one actual shelf cast and attached boost land without redundant second K or ordinary jump/dash");
                GD.Print($"TASK10_BASIN_SWING_LANDING_OK side_cast={sideCast} elapsed={Engine.GetPhysicsFrames()-pullStart}/150 " +
                    $"boosts=1 ordinary_jumps=0 extra_dashes=0 hold=15 exact_gallery=true neutral_stopped=true fragment_unchanged=true " +
                    $"health={initialHealth}->{health.CurrentHealth} player={identity} position={player.GlobalPosition} native_keyboard=true human=false");
                return true;
            }
            finally
            {
                claw.ClawFrameChanged-=Observe;
                GD.Print($"TASK10_BASIN_SHELF_TRANSFER_END stage={stage} elapsed={(pullStart==0?0:Engine.GetPhysicsFrames()-pullStart)}/150 " +
                    $"side_cast={sideCast} boosts={boosts} releases={releases} " +
                    $"player={player.GlobalPosition} velocity={player.Velocity} state={claw.CurrentFrame.State}/{claw.CurrentFrame.Failure}");
                driver.Command(InputActions.Claw,false);driver.Command(InputActions.MoveUp,false);
                driver.Command(InputActions.MoveLeft,false);driver.Command(InputActions.MoveRight,false);driver.Command(InputActions.Jump,false);
            }
        }
        try
        {
            if(verifyRideDefense && GodotObject.IsInstanceValid(bell))bell.FrameChanged+=ObserveRideBellNative;
            stage="control approach";
            await Until(()=>claw.CurrentFrame.State==MechanicalClawState.Idle,60,"natural claw readiness");
            bool startsSupported=Floor(player,first);
            if(!startsSupported) ObserveRouteReadiness("original_support_failure",
                player.GetParent()?.GetParent() as PrologueLevelController,player,first);
            TestAssert.True(startsSupported,"Basin route starts on PistonOne reached by the live route");
            await Walk(2492,80,stopOnFirst:true);
            await Step(4);
            TestAssert.True(Floor(player,first) && Mathf.Abs(player.GlobalPosition.X-2492)<8,
                $"Basin route walks to visible control station; actual={player.GlobalPosition}");
            controlTrace=new BasinGalleryCastTrace(player,claw,control,"CONTROL");
            controlTrace.Start();
            driver.Command(InputActions.MoveUp,true); await Step(3);
            controlTrace.Capture("before_preview_assert");
            controlTrace.CaptureExpectedVisibility();
            controlTrace.Freeze();
            GraveLampMoth blockingMoth=claw.KeyboardPreview.Surface is ThreadTarget2D blocking &&
                blocking.TryGetEligibleReceiver(player,out Node receiver) ? receiver as GraveLampMoth : null;
            if(blockingMoth==null && claw.KeyboardPreview.Surface!=control) blockingMoth=FindControlSightMoth();
            if(blockingMoth!=null)
            {
                await ClearPreviewMoth(blockingMoth);
                controlAfterTrace=new BasinGalleryCastTrace(player,claw,control,"CONTROL_POST_COUNTER");
                controlAfterTrace.Start();
                await Step(3);
                controlAfterTrace.Capture("before_preview_assert");
                controlAfterTrace.CaptureExpectedVisibility();
                controlAfterTrace.Freeze();
            }
            TestAssert.True(claw.KeyboardPreview.Surface==control && claw.KeyboardPreview.Snapshot.HasLineOfSight,
                "Basin route W previews static lift control");
            Vector2 caster=player.GlobalPosition;
            driver.Command(InputActions.Claw,true); await Step(3); driver.Command(InputActions.Claw,false);
            await Until(()=>lift.IsActivated,40,"native K activates lift");
            driver.Command(InputActions.MoveUp,false);
            TestAssert.True(player.GlobalPosition.DistanceTo(caster)<1,"Basin mechanism cast does not pull caster");
            // Join the next complete lower dwell, even if an earlier route visit
            // already activated the lift. Never restart its production phase.
            float cycle=2*(lift.TravelSeconds+lift.EndpointDwellSeconds);
            float nextWindow=Mathf.Floor(lift.ElapsedSeconds/cycle)*cycle+2*lift.TravelSeconds+lift.EndpointDwellSeconds+.45f;
            if(nextWindow<lift.ElapsedSeconds) nextWindow+=cycle;
            stage="lower window wait";
            // W aimed at the mechanism; return to horizontal facing with an
            // actual short D tap before defending the enemy to our right.
            driver.Command(InputActions.MoveRight,true); await Step();
            driver.Command(InputActions.MoveRight,false);
            await Until(()=>lift.ElapsedSeconds>=nextWindow,510,"next lower boarding window");
            TestAssert.True(lift.Frame.Phase01<.001f && Floor(player,first),"Basin route waits on original support for returning lift");
            GD.Print($"TASK10_BASIN_WAIT_DEFENSE contacts={bellContacts} guarded={guardedContacts} health={health.CurrentHealth} player={player.GlobalPosition}");
            if(verifyRideDefense)
            {
                driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero);
                rideShieldOwner=false;
                boardingGuardOwnsDirections=false;
            }
            driver.Command(InputActions.Shield,false); waitShieldHeld=false;
            stage="boarding";
            // The real GearPiston can physically stop this walk for its entire
            // authored active interval. Include that bounded obstruction time;
            // do not alter its phase/collider, skip the lift, or widen the full
            // route deadline. Empty-arena timing alone is not a route budget.
            var piston=sector.GetNode<CanyonGearPiston>("Gameplay/Mechanisms/GearPiston");
            int obstructionFrames=Mathf.CeilToInt(piston.ActiveSeconds*Engine.PhysicsTicksPerSecond);
            TestAssert.True(obstructionFrames>0 && obstructionFrames<=60,
                "authored piston obstruction is bounded to at most one second");
            await Walk(2760,110+obstructionFrames);
            await Until(()=>Floor(player,lift),30,"walk/fall boards lift");
            GD.Print($"TASK10_BASIN_LIFT_BOARDED player={player.GlobalPosition} elapsed={lift.ElapsedSeconds} actor={identity}");
            stage="ride";
            ulong rideNativeStart=Engine.GetPhysicsFrames();
            bool rideEntryQuiet=GodotObject.IsInstanceValid(bell) && !bell.CurrentFrame.AttackActive &&
                bell.CurrentFrame.AttackTimeline.Phase!=AttackPhase.Active && !bellHitbox.Monitoring;
            for(int i=0;i<200 && lift.Frame.Phase01<=.999f;i++)
            {
                rideNativeSteps++;
                await Step();
                TestAssert.True(Floor(player,lift) && player.GlobalPosition.X>=2720 && player.GlobalPosition.X<=2800,
                    "Basin route full capsule rides actual deck without foreign support");
            }
            TestAssert.True(lift.Frame.Phase01>.999f && Floor(player,lift),"Basin route reaches upper endpoint supported");
            ulong rideNativeEnd=Engine.GetPhysicsFrames();
            bool rideExitQuiet=GodotObject.IsInstanceValid(bell) && !bell.CurrentFrame.AttackActive &&
                bell.CurrentFrame.AttackTimeline.Phase!=AttackPhase.Active && !bellHitbox.Monitoring;
            bool nativeQuietRide=verifyRideDefense && AcceptsQuietRide(new RideNativeCoverage(
                rideNativeSteps,rideNativeStart,rideNativeEnd,rideBellNativeFrames,rideBellNativeFirst,
                rideBellNativeLast,rideBellNativeDuplicates,rideBellNativeContiguous,rideBellNativeValid,
                rideBellNativeFault,rideBellNativeActive,rideEntryQuiet,rideExitQuiet,
                bell.SceneFilePath=="res://scenes/enemies/BellBackLaborer.tscn",
                rideContacts,rideActivePairs,ridePossibleOverlapPairs,rideDisabledActiveSamples,rideEnvelopeFault));
            GD.Print($"TASK10_BASIN_RIDE_NATIVE_COVERAGE steps={rideNativeSteps} callbacks={rideBellNativeFrames} " +
                $"start={rideNativeStart} end={rideNativeEnd} first={rideBellNativeFirst} last={rideBellNativeLast} " +
                $"duplicates={rideBellNativeDuplicates} contiguous={rideBellNativeContiguous} valid={rideBellNativeValid} " +
                $"active_or_monitoring={rideBellNativeActive} entry_quiet={rideEntryQuiet} exit_quiet={rideExitQuiet} " +
                $"quiet_eligible={nativeQuietRide} fault={rideBellNativeFault??"none"} " +
                "actual_FrameChanged_after_MoveAndSlide=true extra_frames=0 passive_only=true");
            bool geometricallySeparated=rideContacts==0 && rideActivePairs>0 &&
                ridePossibleOverlapPairs==0 && rideDisabledActiveSamples==0 && rideEnvelopeFault==null;
            TestAssert.True(!verifyRideDefense || health.CurrentHealth==initialHealth &&
                (rideContacts>0 && rideGuardedContacts==rideContacts || geometricallySeparated || nativeQuietRide),
                $"Basin retained Bell ride guards every actual contact, proves swept attack separation, or complete native quiet execution without damage; " +
                $"contacts={rideContacts} guarded={rideGuardedContacts} left={leftRideGuardedContacts} " +
                $"active_pairs={rideActivePairs} possible_overlap_pairs={ridePossibleOverlapPairs} " +
                $"disabled_active={rideDisabledActiveSamples}");
            GD.Print($"TASK10_BASIN_RIDE_DEFENSE_OK contacts={rideContacts} guarded={rideGuardedContacts} " +
                $"left={leftRideGuardedContacts} required={verifyRideDefense} separated={geometricallySeparated} " +
                $"native_quiet={nativeQuietRide} " +
                $"active_pairs={rideActivePairs} possible_overlap_pairs={ridePossibleOverlapPairs} " +
                $"disabled_active={rideDisabledActiveSamples} health={initialHealth}->{health.CurrentHealth} original_ride_cap=200");
            stage="gallery pull";
            galleryTrace=new BasinGalleryCastTrace(player,claw,anchor);
            galleryTrace.Start();
            driver.Command(InputActions.MoveUp,true); driver.Command(InputActions.MoveRight,true); await Step();
            galleryTrace.Capture("before_preview_assert");
            bool shelfLanded=false;
            if(claw.KeyboardPreview.Surface!=anchor)
            {
                shelfLanded=await TransferViaResidentShelf();
                if (passiveFirstReel || replayFailedSwing) return 0;
            }
            else
            {
            TestAssert.True(!passiveFirstReel && !replayFailedSwing, "first-Reel diagnostic requires the actual resident shelf selection");
            TestAssert.True(claw.KeyboardPreview.Surface==anchor && claw.KeyboardPreview.Snapshot.HasLineOfSight,
                "Basin route W+D previews gallery point, not lift or unrelated wall");
            driver.Command(InputActions.Claw,true); galleryTrace.Capture("k_issued");
            // PhysicsFrame is emitted before the reader/controller. An injected K
            // can become just-pressed on the following input tick: releasing W/D
            // after one signal would make that cast use horizontal last-facing.
            // Wait for real cast consumption, not a render-dependent frame count.
            await Until(()=>claw.CurrentFrame.State is MechanicalClawState.Aim or
                MechanicalClawState.Windup or MechanicalClawState.Travel or
                MechanicalClawState.Latched or MechanicalClawState.Reel,4,
                "native K is consumed before direction release");
            driver.Command(InputActions.Claw,false); galleryTrace.Capture("k_released");
            driver.Command(InputActions.MoveUp,false); driver.Command(InputActions.MoveRight,false);
            galleryTrace.Capture("direction_keys_released");
            await Until(()=>claw.CurrentFrame.LockedTargetRuntimeId!=0,30,"gallery anchor locked");
            galleryTrace.Capture("before_lock_assert");
            TestAssert.Equal(anchor.GetInstanceId(),claw.CurrentFrame.LockedTargetRuntimeId,"Basin route actual K locks intended gallery");
            galleryTrace.Freeze(lockVerified:true);
            bool sawReel=claw.CurrentFrame.State==MechanicalClawState.Reel;
            for(int i=0;i<150 && !claw.CurrentFrame.Arrived;i++)
            { await Step(); sawReel|=claw.CurrentFrame.State==MechanicalClawState.Reel; }
            TestAssert.True(sawReel && claw.CurrentFrame.Arrived && claw.CurrentFrame.State==MechanicalClawState.Swing,
                $"BasinRoute real Reel must arrive as Swing; actual={claw.CurrentFrame.State}/{claw.CurrentFrame.ReleaseKind}");
            }
            if(!shelfLanded)
            {
                driver.Command(InputActions.Claw,true); await Step(3); driver.Command(InputActions.Claw,false);
                await Until(()=>Floor(player,gallery),45,"K release onto gallery");
                await Step(15);
            }
            TestAssert.True(Floor(player,gallery) && player.GlobalPosition.X>=3060 && player.GlobalPosition.X<=3260,
                "Basin route lands and remains fully supported without corrective D input");
            GD.Print($"TASK10_BASIN_GALLERY_ARRIVAL player={player.GlobalPosition} actor={identity} health={health.CurrentHealth}");
            if (finishOnGallery)
            {
                TestAssert.True(motor.AcceptedJumpCount == initialJumps && player.Velocity.Length() <= 2f &&
                    player.GetNode<PlayerInputReader>("Input").CurrentFrame.MoveAxis == 0f &&
                    !Input.IsPhysicalKeyPressed(Key.A) && !Input.IsPhysicalKeyPressed(Key.D) &&
                    !Input.IsPhysicalKeyPressed(Key.Space) && claw.CurrentFrame.State == MechanicalClawState.Idle,
                    "Gallery terminal retains exact supported neutral arrival with no ordinary return hop");
                GD.Print($"TASK10_BASIN_GALLERY_TERMINAL_OK player={identity} position={player.GlobalPosition} " +
                    $"health={initialHealth}->{health.CurrentHealth} ordinary_jumps={motor.AcceptedJumpCount-initialJumps} " +
                    "return_hops=0 original_transfer=true default_roundtrip_unchanged=true");
                return 0;
            }
            // Rejoin live Bell encounter on the lower PistonTwo. Stop before the
            // gallery fragment so natural omission/recovery tests retain meaning.
            stage="return to Bell";
            if(shelfLanded)await ReturnAcrossResidentShelf();
            else
            {
                await Walk(2975,60);
                await Until(()=>Floor(player,second),45,"ordinary left drop returns to PistonTwo");
            }
            TestAssert.Equal(initialJumps,motor.AcceptedJumpCount,
                "transfer and the corrected right-throat return use no ordinary jump");
            TestAssert.True(Floor(player,second) && player.GlobalPosition.X>=2864 && player.GlobalPosition.X<=3020,
                "Basin route returns full capsule to supported Bell approach");
            GD.Print($"TASK10_BASIN_LIFT_ROUTE_OK player={player.GlobalPosition} actor={identity} health={health.CurrentHealth} keyboard_injected=true human=false");
            return 0;
        }
        finally
        {
            controlTrace?.Freeze();
            controlTrace?.Dump();
            controlAfterTrace?.Freeze();
            controlAfterTrace?.Dump();
            galleryTrace?.Freeze();
            galleryTrace?.Dump();
            health.Damaged-=damage;
            if(rideShieldOwner && GodotObject.IsInstanceValid(player) && player.IsInsideTree())
                driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero);
            driver.ReleaseAll();
            if(verifyRideDefense && GodotObject.IsInstanceValid(bell))bell.FrameChanged-=ObserveRideBellNative;
            if(GodotObject.IsInstanceValid(bellHitbox)) bellHitbox.Connected-=contact;
            GD.Print($"TASK10_BASIN_RIDE_GUARD_END rows={rideGuardRows} omitted={rideGuardOmitted} fault={rideGuardFault??"none"} observation_only=false");
            GD.Print($"TASK10_BASIN_BOARDING_GUARD_END sampled_direction_takeovers={boardingGuardSamples} " +
                "walk_intent_retained=true original_deadline=true actual_keys=true");
            GD.Print($"TASK10_BASIN_RIDE_ENVELOPE_END active_pairs={rideActivePairs} " +
                $"possible_overlap_pairs={ridePossibleOverlapPairs} disabled_active={rideDisabledActiveSamples} " +
                $"fault={rideEnvelopeFault??"none"} direct_space_queries=0 driver_changes=0");
        }
    }

    // Post-Motor scalar samples through the entire first native side pull.
    // No resolver calls, extra inputs, physics writes or per-tick formatting.
    private sealed class BasinFirstReelTrace : IDisposable
    {
        private readonly CharacterBody2D _player;
        private readonly MechanicalClawController _claw;
        private readonly PlayerInputReader _input;
        private readonly CollisionShape2D _body;
        private readonly Rect2 _bodyLocal, _shelf;
        private readonly GrappleSurface2D _gallery;
        private readonly ulong _sideId;
        private readonly bool _replay;
        private readonly PlayerMotor _motor;
        private string _firstClearVisibility;
        private ulong _firstClearPhysics;
        private readonly BasinBoardingTickObserver _observer;
        private readonly FirstReelRow[] _rows = new FirstReelRow[160];
        private MechanicalClawFrame _published;
        private ulong _cast;
        private int _count, _overflow;
        private Exception _failure;
        internal int Count => _count;
        internal bool Terminal => _count > 0 && (Ended(_rows[_count - 1].Frame) || Ended(_rows[_count - 1].Published));
        private static bool Ended(MechanicalClawFrame frame) => frame.Arrived ||
            frame.State is MechanicalClawState.Released or MechanicalClawState.Failed or MechanicalClawState.Retract;

        internal BasinFirstReelTrace(CharacterBody2D player, MechanicalClawController claw,
            GrappleSurface2D side, GrappleSurface2D gallery, Rect2 shelf, bool replay = false)
        {
            _player = player; _claw = claw; _gallery = gallery; _shelf = shelf;
            _replay = replay;
            _sideId = side.GetInstanceId();
            _input = player.GetNode<PlayerInputReader>("Input");
            _body = player.GetNode<CollisionShape2D>("BodyCollider");
            _bodyLocal = _body.Shape.GetRect();
            var motor = player.GetNode<PlayerMotor>("Motor");
            _motor = motor;
            TestAssert.True(_input.ProcessPhysicsPriority < claw.ProcessPhysicsPriority &&
                claw.ProcessPhysicsPriority < motor.ProcessPhysicsPriority,
                "first-Reel observer must follow actual input, claw and Motor owners");
            _observer = new BasinBoardingTickObserver { Name = "FirstSideReelPostMotor", ProcessPhysicsPriority = motor.ProcessPhysicsPriority + 1, Sample = Sample };
            player.AddChild(_observer);
            claw.ClawFrameChanged += Published;
        }

        private void Published(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            if (_cast == 0 && frame.CastId != 0 && frame.LockedTargetRuntimeId == _sideId) _cast = frame.CastId;
            if (_cast != 0 && frame.CastId == _cast) _published = frame;
        }

        private void Sample(double delta)
        {
            if (_cast == 0 || _failure != null) return;
            try
            {
                if (_count == _rows.Length) { _overflow++; return; }
                var preview = _claw.KeyboardPreview;
                _rows[_count++] = new(Engine.GetPhysicsFrames(), Engine.GetProcessFrames(),
                    _input.CurrentFrameSequence, delta, _player.GlobalPosition, _player.Velocity,
                    _body.GlobalTransform * _bodyLocal, _player.IsOnFloor(), _player.IsOnWall(), _player.IsOnCeiling(),
                    _input.RawFrame, _input.CurrentFrame, _claw.CurrentFrame, _published,
                    preview.Surface == null, ReferenceEquals(preview.Surface, _gallery), preview.Snapshot.HasLineOfSight,
                    preview.Snapshot.RuntimeId, _motor.HasGrappleReleaseMomentum);
                if (_replay && _firstClearPhysics == 0 && _rows[_count - 1].Body.End.Y < _shelf.Position.Y - .25f)
                {
                    _firstClearPhysics = Engine.GetPhysicsFrames();
                    var visibility = new BasinGalleryCastTrace(_player, _claw, _gallery, "SWING_FIRST_CLEAR");
                    _firstClearVisibility = visibility.CaptureExpectedVisibilityScalar();
                }
            }
            catch (Exception error) { _failure = error; }
        }

        internal void AssertComplete()
        {
            if (_failure != null) throw new InvalidOperationException("first-Reel scalar observer failed", _failure);
            TestAssert.True(_cast != 0 && _count > 0 && _count <= 150 && _overflow == 0,
                "first-Reel native samples must exist inside the original150-physics window without overflow");
            for (int index = 0; index < _count; index++)
            {
                var row = _rows[index];
                TestAssert.True(!row.Raw.JumpHeld && !row.Raw.JumpPressed && !row.Raw.DashPressed &&
                    !row.Sampled.JumpHeld && !row.Sampled.JumpPressed && !row.Sampled.DashPressed,
                    "passive trajectory never adds Space or a jet");
                if (index > 0) TestAssert.True(row.Physics == _rows[index - 1].Physics + 1 && row.Sequence > _rows[index - 1].Sequence,
                    "post-Motor trajectory has every actual physics/input tick without gaps");
            }
            GD.Print($"BASIN_FIRST_REEL_DIAGNOSTIC_COMPLETE samples={_count} cast={_cast} terminal={Terminal} no_space_or_jet=true contiguous=true controlled_spawn=true full_main=false behavior_accepted=false human=false whole_goal=false");
        }

        internal void AssertReplayComplete()
        {
            if (_failure != null) throw new InvalidOperationException("Swing replay scalar observer failed", _failure);
            TestAssert.True(_replay && _cast != 0 && _count == 150 && _overflow == 0,
                "Swing replay retains exactly the original150 post-Motor ticks without another leg budget");
            bool arrived = false, released = false;
            for (int index = 0; index < _count; index++)
            {
                var row = _rows[index];
                arrived |= row.Published.State == MechanicalClawState.Swing && row.Published.Arrived;
                released |= row.Published.State == MechanicalClawState.Released && row.Published.ReleaseKind == MechanicalClawReleaseKind.Boost;
                TestAssert.True(!row.Raw.DashPressed && !row.Sampled.DashPressed, "Swing replay never adds a jet");
                if (index > 0) TestAssert.True(row.Physics == _rows[index - 1].Physics + 1 && row.Sequence > _rows[index - 1].Sequence,
                    "Swing replay post-Motor samples are contiguous actual physics/input ticks");
            }
            TestAssert.True(arrived && released, "Swing replay contains actual arrival and same-cast release samples");
            GD.Print($"BASIN_SWING_FIRST_CLEAR physics={_firstClearPhysics} observed={_firstClearPhysics != 0} " +
                (_firstClearVisibility ?? "query_count=0 decision_unchanged=true"));
            GD.Print($"BASIN_SWING_REPLAY_COMPLETE samples={_count} cast={_cast} failed_input_replay=true no_jet=true contiguous=true controlled_spawn=true behavior_accepted=false full_main=false human=false whole_goal=false");
        }

        public void Dispose()
        {
            _observer.Sample = null;
            _claw.ClawFrameChanged -= Published;
            if (GodotObject.IsInstanceValid(_observer)) _observer.Free();
            for (int index = 0; index < _count; index++)
            {
                var r = _rows[index]; var f = r.Frame; var p = r.Published;
                GD.Print((_replay ? "BASIN_SWING_REPLAY_ROW " : "BASIN_FIRST_REEL_ROW ") + System.Text.Json.JsonSerializer.Serialize(new
                {
                    Index = index, r.Physics, r.Process, r.Sequence, r.Delta,
                    X = r.Position.X, Y = r.Position.Y, Vx = r.Velocity.X, Vy = r.Velocity.Y,
                    BodyMinX = r.Body.Position.X, BodyMaxX = r.Body.End.X, BodyMaxY = r.Body.End.Y,
                    TopClearance = _shelf.Position.Y - r.Body.End.Y, r.Floor, r.Wall, r.Ceiling,
                    RawAxis = r.Raw.MoveAxis, Axis = r.Sampled.MoveAxis, r.Sampled.ClawPressed,
                    RawJumpPressed = r.Raw.JumpPressed, RawJumpHeld = r.Raw.JumpHeld, RawDashPressed = r.Raw.DashPressed,
                    r.Sampled.JumpPressed, r.Sampled.JumpHeld, r.Sampled.DashPressed,
                    Cast = _cast, CurrentCast = f.CastId, State = f.State.ToString(), Failure = f.Failure.ToString(), f.LockedTargetRuntimeId,
                    AnchorX = f.AnchorPoint.X, AnchorY = f.AnchorPoint.Y, DockX = f.DockPoint.X, DockY = f.DockPoint.Y,
                    NormalX = f.SurfaceNormal.X, NormalY = f.SurfaceNormal.Y, f.RopeLength, f.Arrived,
                    CorrectionX = f.VelocityCorrection.X, CorrectionY = f.VelocityCorrection.Y,
                    PublishedCast = p.CastId, PublishedArrived = p.Arrived,
                    PublishedState = p.State.ToString(), PublishedX = p.CasterWorldPosition.X,
                    PublishedY = p.CasterWorldPosition.Y, PublishedVx = p.CasterVelocity.X, PublishedVy = p.CasterVelocity.Y,
                    r.PreviewNone, r.PreviewGallery, r.PreviewLos, r.PreviewId, r.ReleaseMomentum
                }));
            }
            GD.Print($"{(_replay ? "BASIN_SWING_REPLAY_TRACE_END" : "BASIN_FIRST_REEL_TRACE_END")} samples={_count} overflow={_overflow} observer_failed={_failure != null} full_main=false");
        }

        private readonly record struct FirstReelRow(ulong Physics, ulong Process, ulong Sequence, double Delta,
            Vector2 Position, Vector2 Velocity, Rect2 Body, bool Floor, bool Wall, bool Ceiling,
            PlayerInputFrame Raw, PlayerInputFrame Sampled, MechanicalClawFrame Frame, MechanicalClawFrame Published,
            bool PreviewNone, bool PreviewGallery, bool PreviewLos, ulong PreviewId, bool ReleaseMomentum);
    }

    // Passive, short-window diagnostic. No resolver calls, gameplay writes or
    // per-publication formatting/IO. Native reads are sequential, not atomic;
    // the event frame is retained separately from the then-current controller.
    private sealed class BasinGalleryCastTrace
    {
        private const int Capacity=32, PublicationCapacity=12, PreClawCapacity=12, TargetCapacity=16;
        private readonly GalleryCastRow[] _rows=new GalleryCastRow[Capacity];
        private readonly ulong[] _targetIds=new ulong[TargetCapacity];
        private readonly string[] _targetPaths=new string[TargetCapacity];
        private readonly object[] _controlVisibility=new object[3];
        private int _controlVisibilityCount;
        private readonly CharacterBody2D _player;
        private readonly MechanicalClawController _claw;
        private readonly GrappleSurface2D _expected;
        private readonly string _scope;
        private PlayerInputReader _input;
        private BasinBoardingTickObserver _preClaw;
        private int _count,_targetCount,_publications,_preClawSamples,_omittedRows,_omittedPublications,_omittedPreClaw,_unmappedTargets,_failures;
        private int _inputPriority,_preClawPriority,_clawPriority;
        private bool _subscribed,_frozen,_dumped,_firstLock,_lockVerified;
        private string _firstError;
        private string _expectedVisibility="not_observed";
        private ulong _expectedId;
        private Vector2 _expectedAnchor;

        internal BasinGalleryCastTrace(CharacterBody2D player,MechanicalClawController claw,GrappleSurface2D expected,string scope="GALLERY")
        { _player=player; _claw=claw; _expected=expected; _scope=scope; }

        internal void Start()
        {
            try
            {
                _input=_player.GetNode<PlayerInputReader>("Input");
                _expectedId=Remember(_expected);
                _expectedAnchor=_expected.WorldAnchor;
                _inputPriority=_input.ProcessPhysicsPriority;
                _clawPriority=_claw.ProcessPhysicsPriority;
                _preClawPriority=checked(_clawPriority-1);
                if(_inputPriority>=_preClawPriority)
                    throw new InvalidOperationException("Gallery trace requires input < observer < claw physics priority");
                _preClaw=new BasinBoardingTickObserver
                {
                    Name="Basin"+_scope+"PreClawTrace", ProcessPhysicsPriority=_preClawPriority,
                    Sample=OnBeforeClaw
                };
                _player.AddChild(_preClaw);
                _claw.ClawFrameChanged+=OnFrame; _subscribed=true;
                Capture("before_direction_keys");
            }
            catch(Exception error) { Fault(error); }
        }

        private void OnBeforeClaw(double delta)
        {
            if(_frozen || _firstError!=null || _firstLock) return;
            if(_preClawSamples==PreClawCapacity) { _omittedPreClaw++; return; }
            _preClawSamples++;
            // After the reader, before the controller (and ordinary motor):
            // current/preview frames are explicitly the previous publication.
            Capture("before_claw_physics");
        }

        internal void Capture(string boundary)
        {
            if(_frozen || _firstError!=null) return;
            try { Add(boundary,_claw.CurrentFrame,_claw.CurrentTarget); }
            catch(Exception error) { Fault(error); }
        }

        // One diagnostic query after the real decision, never another resolver
        // invocation or an input to cast admission. Match production LOS masks
        // and the actual caster-subtree exclusions, including Area colliders.
        internal void CaptureExpectedVisibility()
        {
            try
            {
                var excluded=new Godot.Collections.Array<Rid>();
                using var excludedOwner=(Godot.Collections.Array)excluded;
                void Exclude(Node node)
                {
                    if(node is CollisionObject2D collision) excluded.Add(collision.GetRid());
                    for(int i=0;i<node.GetChildCount();i++) Exclude(node.GetChild(i));
                }
                Exclude(_player);
                using var query=PhysicsRayQueryParameters2D.Create(_player.GlobalPosition,_expected.WorldAnchor);
                query.CollideWithAreas=true; query.CollideWithBodies=true;
                query.CollisionMask=CombatCollisionLayers.World|CombatCollisionLayers.Enemy|CombatCollisionLayers.ThreadTarget;
                query.Exclude=excluded;
                using var hit=_player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                Node collider=hit.ContainsKey("collider") ? hit["collider"].AsGodotObject() as Node : null;
                CharacterBody2D actor=null;
                for(Node node=collider;node!=null;node=node.GetParent())
                    if(node is CharacterBody2D body) { actor=body; break; }
                bool eligible=_expected is not ThreadTarget2D target || target.TryGetEligibleReceiver(_player,out _);
                bool belongs=collider!=null && MechanicalClawTargetResolver.ColliderBelongsToSurface(collider,_expected);
                _expectedVisibility=$"expected_available={_expected.IsAvailable} expected_eligible={eligible} " +
                    $"expected_validation_errors={_expected.Validate().Count} first_hit={collider?.GetPath().ToString() ?? "none"} " +
                    $"first_hit_belongs={belongs} expected_los={hit.Count==0 || belongs} " +
                    $"actor={actor?.GetInstanceId() ?? 0} actor_scene={actor?.SceneFilePath ?? "none"} " +
                    $"resident_binding={actor?.GetNodeOrNull("ResidentBinding")!=null} " +
                    $"actor_health={actor?.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth ?? -1} " +
                    "query_count=1 decision_unchanged=true";
            }
            catch(Exception error) { Fault(error); }
        }

        internal string CaptureExpectedVisibilityScalar()
        {
            CaptureExpectedVisibility();
            if (_firstError != null) throw new InvalidOperationException("first full-clear LOS observation failed: " + _firstError);
            return _expectedVisibility;
        }

        private void OnFrame(MechanicalClawFrame frame,GrappleSurface2D target)
        {
            if(_frozen || _firstError!=null || _firstLock) return;
            try
            {
                bool firstLock=frame.LockedTargetRuntimeId!=0;
                // Reserve room for the first lock and explicit input boundaries,
                // even if a long wait exhausts ordinary publication retention.
                if(firstLock || _publications<PublicationCapacity)
                {
                    Add(firstLock ? "first_lock_publication" : "claw_publication",frame,target);
                    _publications++;
                    if(_scope=="CONTROL" && _controlVisibilityCount<_controlVisibility.Length &&
                        frame.State==MechanicalClawState.Idle && _input.CurrentFrame.KeyboardActions &&
                        !_input.CurrentFrame.ClawPressed && _input.CurrentFrame.Aim.Y<0 &&
                        Mathf.IsZeroApprox(_input.CurrentFrame.Aim.X)) CaptureControlVisibility();
                }
                else _omittedPublications++;
                _firstLock=firstLock;
            }
            catch(Exception error) { Fault(error); }
        }

        // At most three W publications. These are sequential native reads AFTER
        // the real resolver, not a second resolver call or atomic rejection trace.
        // Retain scalars only; no borrowed world/body/surface/query escapes here.
        private void CaptureControlVisibility()
        {
            Vector2 origin=_player.GlobalPosition;
            Vector2 aim=_input.KeyboardAimDirection.Normalized();
            Vector2 expectedAnchor=_expected.WorldAnchor;
            var excluded=new Godot.Collections.Array<Rid>();
            using var excludedOwner=(Godot.Collections.Array)excluded;
            void Exclude(Node node)
            {
                if(node is CollisionObject2D collision) excluded.Add(collision.GetRid());
                for(int i=0;i<node.GetChildCount();i++) Exclude(node.GetChild(i));
            }
            Exclude(_player);
            using var query=PhysicsRayQueryParameters2D.Create(origin,expectedAnchor);
            query.CollideWithAreas=true; query.CollideWithBodies=true;
            query.CollisionMask=CombatCollisionLayers.World|CombatCollisionLayers.Enemy|CombatCollisionLayers.ThreadTarget;
            query.Exclude=excluded;
            using var hit=_player.GetWorld2D().DirectSpaceState.IntersectRay(query);
            Node collider=hit.ContainsKey("collider") ? hit["collider"].AsGodotObject() as Node : null;
            GrappleSurface2D blocker=null;
            CharacterBody2D actor=null;
            for(Node node=collider;node!=null;node=node.GetParent())
            {
                if(blocker==null && node is GrappleSurface2D surface) blocker=surface;
                if(actor==null && node is CharacterBody2D body) actor=body;
            }
            bool blockerKnown=GodotObject.IsInstanceValid(blocker) && blocker.IsInsideTree();
            Vector2 blockerAnchor=blockerKnown ? blocker.WorldAnchor : Vector2.Zero;
            Vector2 offset=blockerAnchor-origin;
            Node receiver=null;
            bool eligible=blockerKnown &&
                (blocker is not ThreadTarget2D thread || thread.TryGetEligibleReceiver(_player,out receiver));
            bool blockerLos=false;
            string blockerFirstHit="not_observed";
            if(blockerKnown)
            {
                query.To=blockerAnchor;
                using var selfHit=_player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                Node selfCollider=selfHit.ContainsKey("collider") ? selfHit["collider"].AsGodotObject() as Node : null;
                blockerLos=selfHit.Count==0 || selfCollider!=null &&
                    MechanicalClawTargetResolver.ColliderBelongsToSurface(selfCollider,blocker);
                blockerFirstHit=selfCollider?.GetPath().ToString() ?? "none";
            }
            bool expectedEligible=_expected is not ThreadTarget2D expectedThread ||
                expectedThread.TryGetEligibleReceiver(_player,out _);
            _controlVisibility[_controlVisibilityCount++]=new
            {
                Boundary="after_claw_publication",Physics=Engine.GetPhysicsFrames(),Process=Engine.GetProcessFrames(),
                Reader=_input.CurrentFrameSequence,Publication=_publications,
                X=origin.X,Y=origin.Y,AimX=aim.X,AimY=aim.Y,
                Preview=Remember(_claw.KeyboardPreview.Surface),PreviewLos=_claw.KeyboardPreview.Snapshot.HasLineOfSight,
                Expected=Remember(_expected),ExpectedX=expectedAnchor.X,ExpectedY=expectedAnchor.Y,
                ExpectedAvailable=_expected.IsAvailable,ExpectedEligible=expectedEligible,
                ExpectedValidationErrors=_expected.Validate().Count,
                ExpectedLos=hit.Count==0 || collider!=null && MechanicalClawTargetResolver.ColliderBelongsToSurface(collider,_expected),
                FirstHit=collider?.GetPath().ToString() ?? "none",FirstHitId=collider?.GetInstanceId() ?? 0,
                Actor=actor?.GetInstanceId() ?? 0,ActorScene=actor?.SceneFilePath ?? "none",
                ActorHp=actor?.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth ?? -1,
                Blocker=blockerKnown ? Remember(blocker) : 0,
                BlockerPath=blockerKnown ? blocker.GetPath().ToString() : "none",
                BlockerX=blockerAnchor.X,BlockerY=blockerAnchor.Y,
                BlockerAvailable=blockerKnown && blocker.IsAvailable,BlockerEligible=eligible,
                BlockerValidationErrors=blockerKnown ? blocker.Validate().Count : -1,
                BlockerSameWorld=blockerKnown && blocker.GetWorld2D()==_player.GetWorld2D(),
                BlockerMaterial=blockerKnown ? (int)blocker.Material : -1,
                BlockerKind=blocker is ThreadTarget2D kind ? (int)kind.Kind : -1,
                BlockerCollisionHitPoint=blockerKnown && blocker.UseCollisionHitPoint,
                BlockerReceiverType=receiver?.GetType().FullName ?? "none",
                BlockerDistance=blockerKnown ? offset.Length() : -1,
                BlockerAngleDegrees=blockerKnown ? Mathf.RadToDeg(Mathf.Abs(aim.AngleTo(offset))) : -1,
                BlockerFinite=blockerKnown && float.IsFinite(blockerAnchor.X) && float.IsFinite(blockerAnchor.Y),
                BlockerLosObserved=blockerKnown,BlockerLos=blockerLos,BlockerFirstHit=blockerFirstHit,
                QueryCount=blockerKnown ? 2 : 1,HitFromInside=query.HitFromInside,
                DecisionUnchanged=true,AtomicResolverTrace=false
            };
        }

        private void Add(string boundary,MechanicalClawFrame published,GrappleSurface2D publishedTarget)
        {
            if(_count==Capacity) { _omittedRows++; return; }
            MechanicalClawTargetResolution preview=_claw.KeyboardPreview;
            GrappleSurface2D currentTarget=_claw.CurrentTarget;
            ulong previewId=Remember(preview.Surface),currentId=Remember(currentTarget),publishedId=Remember(publishedTarget);
            PlayerInputFrame sampled=_input.CurrentFrame;
            var row=new GalleryCastRow(boundary,Engine.GetPhysicsFrames(),Engine.GetProcessFrames(),
                _input.CurrentFrameSequence,Godot.Input.IsPhysicalKeyPressed(Key.W),
                Godot.Input.IsPhysicalKeyPressed(Key.D),Godot.Input.IsPhysicalKeyPressed(Key.K),
                Godot.Input.IsPhysicalKeyPressed(Key.A),Godot.Input.IsPhysicalKeyPressed(Key.S),
                sampled,_input.KeyboardAimDirection,_player.GlobalPosition,_player.Velocity,
                previewId,preview.Snapshot,currentId,
                GodotObject.IsInstanceValid(currentTarget) ? currentTarget.WorldAnchor : Vector2.Zero,
                publishedId,published,_claw.CurrentFrame);
            _rows[_count++]=row;
        }

        private ulong Remember(GrappleSurface2D target)
        {
            if(!GodotObject.IsInstanceValid(target)) return 0;
            ulong id=target.GetInstanceId();
            for(int i=0;i<_targetCount;i++) if(_targetIds[i]==id) return id;
            if(_targetCount==TargetCapacity) { _unmappedTargets++; return id; }
            string path=target.GetPath().ToString();
            _targetIds[_targetCount]=id; _targetPaths[_targetCount++]=path;
            return id;
        }

        private string PathFor(ulong id)
        {
            if(id==0) return "none";
            for(int i=0;i<_targetCount;i++) if(_targetIds[i]==id) return _targetPaths[i];
            return "unmapped-runtime-id";
        }

        private void Fault(Exception error)
        { _failures++; _firstError??=error.GetType().Name+": "+error.Message; }

        internal void Freeze(bool lockVerified=false)
        {
            if(_frozen) return;
            _frozen=true; _lockVerified=lockVerified;
            try { if(_subscribed) { _claw.ClawFrameChanged-=OnFrame; _subscribed=false; } }
            catch(Exception error) { Fault(error); }
            try
            {
                if(GodotObject.IsInstanceValid(_preClaw))
                {
                    _preClaw.Sample=null;
                    _preClaw.SetPhysicsProcess(false);
                    _preClaw.QueueFree();
                }
            }
            catch(Exception error) { Fault(error); }
        }

        internal void Dump()
        {
            if(_dumped) return;
            _dumped=true;
            // Formatting reads only copied managed rows/paths. Never let ancillary
            // diagnostics replace the route exception or prevent its cleanup.
            try
            {
                GD.Print($"TASK10_BASIN_{_scope}_TRACE_BEGIN diagnostic_only=true lock_assertion_passed={_lockVerified} expected={_expectedId} expected_path={PathFor(_expectedId)} expected_anchor={_expectedAnchor} rows={_count}/{Capacity} omitted_rows={_omittedRows} omitted_publications={_omittedPublications} omitted_pre_claw={_omittedPreClaw} input_priority={_inputPriority} observer_priority={_preClawPriority} claw_priority={_clawPriority} unmapped_target_observations={_unmappedTargets} observer_failures={_failures} first_error={_firstError ?? "none"} sampled_WD=derived_from_net_aim_axes sampled_K=accepted_ClawPressed_edge_no_held_field order=pre_claw_or_publication_not_OS_or_atomic_snapshot pre_claw_frames=previous_publication");
                if(_scope.StartsWith("CONTROL",StringComparison.Ordinal) || _scope=="GALLERY_SECOND")
                    GD.Print($"TASK10_BASIN_{_scope}_VISIBILITY {_expectedVisibility}");
                for(int i=0;i<_controlVisibilityCount;i++)
                    GD.Print("TASK10_BASIN_CONTROL_DECISION_OBSERVATION "+System.Text.Json.JsonSerializer.Serialize(_controlVisibility[i]));
                for(int i=0;i<_count;i++)
                {
                    GalleryCastRow row=_rows[i];
                    GD.Print($"TASK10_BASIN_{_scope}_TRACE row={i} boundary={row.Boundary} physics={row.Physics} process={row.Process} reader={row.Reader} raw_WDK={row.RawW}/{row.RawD}/{row.RawK} raw_AS={row.RawA}/{row.RawS} sampled_WD={row.Sampled.Aim.Y<0}/{row.Sampled.Aim.X>0} sampled_K_pressed={row.Sampled.ClawPressed} keyboard={row.Sampled.KeyboardActions} sampled_aim={row.Sampled.Aim} move={row.Sampled.MoveAxis} resolved_aim={row.Aim} position={row.Position} velocity={row.Velocity} preview={row.PreviewId}:{PathFor(row.PreviewId)} preview_snapshot_id={row.Preview.RuntimeId} preview_anchor={row.Preview.WorldAnchor} preview_normal={row.Preview.SurfaceNormal} preview_los={row.Preview.HasLineOfSight} current={row.CurrentId}:{PathFor(row.CurrentId)} current_world_anchor={row.CurrentAnchor} published_target={row.PublishedId}:{PathFor(row.PublishedId)} published_state={row.Published.State} published_cast={row.Published.CastId} published_lock={row.Published.LockedTargetRuntimeId}:{PathFor(row.Published.LockedTargetRuntimeId)} published_anchor={row.Published.AnchorPoint} published_normal={row.Published.SurfaceNormal} published_caster={row.Published.CasterWorldPosition} controller_state={row.Current.State} controller_cast={row.Current.CastId} controller_lock={row.Current.LockedTargetRuntimeId}:{PathFor(row.Current.LockedTargetRuntimeId)} controller_anchor={row.Current.AnchorPoint} controller_normal={row.Current.SurfaceNormal}");
                }
                GD.Print($"TASK10_BASIN_{_scope}_TRACE_END");
            }
            catch(Exception) { /* Route result and original cleanup stay authoritative. */ }
        }

        private readonly record struct GalleryCastRow(string Boundary,ulong Physics,ulong Process,ulong Reader,
            bool RawW,bool RawD,bool RawK,bool RawA,bool RawS,PlayerInputFrame Sampled,Vector2 Aim,Vector2 Position,Vector2 Velocity,
            ulong PreviewId,MechanicalClawTargetSnapshot Preview,ulong CurrentId,Vector2 CurrentAnchor,
            ulong PublishedId,MechanicalClawFrame Published,MechanicalClawFrame Current);
    }

    // Catches replacement by a decorative one-shot, missing receiver, caster pull,
    // and phase-restarting repeated K casts in the actual serialized level leaf.
    private static async Task Contract(SceneTree tree)
    {
        var f = new Fixture(tree, new Vector2(2492, 3880));
        try
        {
            await f.Start();
            await Activate(f);
            float elapsed = f.Lift.ElapsedSeconds;
            await f.Frames(45);
            TestAssert.True(f.Lift.ElapsedSeconds > elapsed + .6f && f.Lift.GlobalPosition.Y < 3968,
                "native K starts immediate real ascent, not an endpoint dwell or one-shot decoration");
            await f.Until(() => f.Claw.CurrentFrame.State == MechanicalClawState.Idle, 90, "mechanism cast retracts");
            float before = f.Lift.ElapsedSeconds;
            f.Key(Key.W, true);
            await f.Frames(2);
            TestAssert.True(f.Claw.KeyboardPreview.Surface == f.Sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightControl"),
                "repeat activation must again aim at static control, not unrelated terrain");
            await f.Press(Key.K, 3);
            await f.Frames(25);
            f.Key(Key.W, false);
            TestAssert.True(f.Lift.ElapsedSeconds > before + .3f && f.Lift.ElapsedSeconds < before + .6f,
                "repeated K neither restarts nor accelerates lift phase");
            TestAssert.True(f.Player.GlobalPosition.DistanceTo(new Vector2(2492,3880)) < 1f,
                $"static mechanism activation never drags its caster; actual={f.Player.GlobalPosition} target={f.Claw.CurrentTarget?.GetPath()}");
            GD.Print($"BASIN_NATIVE_K_OK receipt={f.KeyReceipts} samples={f.Input.CurrentFrameSequence} elapsed={f.Lift.ElapsedSeconds}");
            AssertSweep(f);
        }
        finally { await f.Dispose(); }
    }

    private static async Task Activate(Fixture f)
    {
        TestAssert.True(f.Player.IsOnFloor(), "mechanism casting station stands on authored PistonOne");
        f.Key(Key.W, true);
        await f.Frames(3);
        var control = f.Sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightControl");
        TestAssert.True(f.Claw.KeyboardPreview.Surface == control &&
            f.Claw.KeyboardPreview.Snapshot.IsLegacyInteraction &&
            f.Claw.KeyboardPreview.Snapshot.HasLineOfSight,
            $"W preview selects visible static mechanism; actual={f.Claw.KeyboardPreview.Surface?.GetPath()}");
        Vector2 caster = f.Player.GlobalPosition;
        await f.Press(Key.K, 3);
        await f.Until(() => f.Lift.IsActivated, 40, "real K reaches static receiver");
        f.Key(Key.W, false);
        TestAssert.True(f.SampledClawEdges>0,"real PlayerInputReader sampled physical K edge before receiver activation");
        TestAssert.True(f.Player.GlobalPosition.DistanceTo(caster) < 1f, "activation does not move caster toward control");
        TestAssert.True(f.Lift.ElapsedSeconds < .3f, "first departure has no hidden startup wait");
        TestAssert.Equal(GrappleSurfaceMaterial.Forbidden,
            f.Lift.GetNode<GrappleSurface2D>("GrappleSurfaceProfile").Material,
            "moving passenger deck remains claw Forbidden");
    }

    private static async Task Ride(SceneTree tree, float landingX)
    {
        var f = new Fixture(tree, new Vector2(2492, 3880));
        try
        {
            await f.Start(); await Activate(f); AssertSweep(f);
            await BoardReturn(f, landingX);
            int health = f.Health.CurrentHealth;
            float start = f.Lift.ElapsedSeconds;
            bool up=false, down=false, upperDwell=false, lowerDwell=false;
            bool safe=false, telegraph=false, active=false, recovery=false;
            while (f.Lift.ElapsedSeconds < start + 8.1f)
            {
                float before = f.Lift.GlobalPosition.Y;
                await f.Frames(1);
                AssertRider(f, health);
                float moved = f.Lift.GlobalPosition.Y - before;
                up |= moved < -.1f; down |= moved > .1f;
                upperDwell |= !f.Lift.Frame.HazardActive && f.Lift.Frame.Phase01 > .999f;
                lowerDwell |= !f.Lift.Frame.HazardActive && f.Lift.Frame.Phase01 < .001f;
                var p = f.Piston.Frame;
                safe |= p.Phase01 == 0 && !p.Telegraphing;
                telegraph |= p.Telegraphing; active |= p.HazardActive;
                recovery |= !p.HazardActive && !p.Telegraphing && p.Phase01 > 0;
            }
            TestAssert.True(up && down && upperDwell && lowerDwell,
                "passenger remains supported through both travel directions and endpoint dwells");
            TestAssert.True(safe && telegraph && active && recovery, "ride observed all actual piston phases");
            GD.Print($"BASIN_RIDE_OK landing_x={landingX} passenger={f.Player.GlobalPosition} health={health} up={up} down={down} dwell={upperDwell && lowerDwell} piston_all=true frames={f.Input.CurrentFrameSequence}");
        }
        finally { await f.Dispose(); }
    }

    private static async Task BoardReturn(Fixture f, float landingX)
    {
        GD.Print($"BASIN_PREWAIT physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} player={f.Player.GlobalPosition} velocity={f.Player.Velocity} lift_time={f.Lift.ElapsedSeconds} raw_d={Godot.Input.IsPhysicalKeyPressed(Key.D)} axis={f.Input.Current.MoveAxis}");
        await f.Until(() => f.Lift.ElapsedSeconds >= 6.45f, 430, "missed departure genuinely returns");
        TestAssert.True(f.Lift.Frame.Phase01 < .001f && f.Player.IsOnFloor(),
            "first missed cycle returns to lower boarding window while caster waits on original support");
        GD.Print($"BASIN_BOARDING_BEGIN physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} player={f.Player.GlobalPosition} velocity={f.Player.Velocity} lift_time={f.Lift.ElapsedSeconds} raw_d={Godot.Input.IsPhysicalKeyPressed(Key.D)} axis={f.Input.Current.MoveAxis}");
        TestAssert.True(f.Player.GlobalPosition.DistanceTo(new Vector2(2492,3880))<1f &&
            f.Player.Velocity.Length()<1f && !Godot.Input.IsPhysicalKeyPressed(Key.D),
            "BasinBoardingInitialState: untouched caster waits at original station before owned D walk");
        int jumps = f.Motor.AcceptedJumpCount;
        f.BeginBoardingTrace(landingX - 12f);
        f.Key(Key.D, true);
        // Release12px before desired center to allow the unchanged motor's
        // ordinary braking distance. Waiting for floor is part of falling80px,
        // not a second boarding attempt or a new timing window.
        await f.Until(() => f.BoardingReleaseCompleted, 110,
            "walk/fall boarding from original PistonOne reaches deck");
        f.AssertBoardingReleaseTimely();
        await f.Until(() => Floor(f.Player, f.Lift), 30, "ordinary fall completes on returned carriage");
        await f.Frames(4);
        TestAssert.True(Floor(f.Player, f.Lift), $"walk/fall boards returned lift, position={f.Player.GlobalPosition} lift={f.Lift.GlobalPosition}");
        TestAssert.Equal(jumps, f.Motor.AcceptedJumpCount, "boarding uses real walking/falling, not hidden jump/teleport");
        f.CompleteBoardingTrace();
    }

    // Missing point anchor, wrong native ranking, non-radial docking, or an
    // edge-only landing must fail this real saved-scene transfer contract.
    private static async Task Transfer(SceneTree tree,float landingX,bool capture=false)
    {
        var f = new Fixture(tree, new Vector2(2492,3880),full:capture);
        try
        {
            if(capture) f.EnableCapture();
            await f.Start(); await Activate(f); await BoardReturn(f,landingX);
            f.CaptureStage("02-boarded");
            int health=f.Health.CurrentHealth;
            await f.Until(() => f.Lift.Frame.Phase01 > .999f, 200, "upper passenger endpoint reached");
            f.CaptureStage("03-upper-deck");
            AssertRider(f,health);
            var gallery=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/UpperGearGalleryOne");
            var surface=f.Sector.GetNodeOrNull<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightGalleryAnchor");
            TestAssert.True(surface!=null,"SavedBasinGalleryPointAnchorMissing");
            f.Key(Key.W,true); f.Key(Key.D,true); await f.Frames(1);
            MechanicalClawTargetSnapshot preview=f.Claw.KeyboardPreview.Snapshot;
            Vector2 radial=(f.Player.GlobalPosition-new Vector2(3112,3624)).Normalized();
            GD.Print($"BASIN_UPPER_PREVIEW station={landingX} player={f.Player.GlobalPosition} surface={f.Claw.KeyboardPreview.Surface?.GetPath()} anchor={preview.WorldAnchor} normal={preview.SurfaceNormal} los={preview.HasLineOfSight}");
            TestAssert.True(f.Claw.KeyboardPreview.Surface == surface && preview.HasLineOfSight &&
                !preview.IsLegacyInteraction && preview.Material==GrappleSurfaceMaterial.DedicatedAnchor &&
                preview.WorldAnchor.DistanceTo(new Vector2(3112,3624))<.1f && preview.SurfaceNormal.DistanceTo(radial)<.01f,
                "native W+D preview selects visible dedicated point anchor with independent radial normal");
            AssertRider(f,health);
            f.Key(Key.K,true); await f.Frames(1); f.Key(Key.K,false); f.Key(Key.W,false); f.Key(Key.D,false); await f.Frames(2);
            await f.Until(() => f.Claw.CurrentFrame.LockedTargetRuntimeId != 0, 30, "upper K locks previewed gallery");
            TestAssert.Equal(surface.GetInstanceId(),f.Claw.CurrentFrame.LockedTargetRuntimeId,"actual K locks exact previewed gallery, not a competing side");
            TestAssert.True(f.Claw.CurrentFrame.SurfaceNormal.DistanceTo(radial)<.02f && f.Claw.CurrentFrame.EffectiveVisibility &&
                f.Claw.CurrentFrame.DockPoint.X>=3068 && f.Claw.CurrentFrame.DockPoint.X<=3072 &&
                f.Claw.CurrentFrame.DockPoint.Y>=3643 && f.Claw.CurrentFrame.DockPoint.Y<=3650,
                "locked point target retains radial dock fully above and inside GalleryOne");
            MechanicalClawState previous=f.Claw.CurrentFrame.State;
            for(int i=0;i<150 && !f.Claw.CurrentFrame.Arrived;i++)
            {
                await f.Frames(1);
                var frame=f.Claw.CurrentFrame;
                if(frame.State==MechanicalClawState.Reel) f.CaptureStage("04-automatic-pull");
                if(frame.State!=previous || i%10==0)
                    GD.Print($"BASIN_PULL_TRACE tick={i} state={frame.State} failure={frame.Failure} release={frame.ReleaseKind} arrived={frame.Arrived} player={f.Player.GlobalPosition} velocity={f.Player.Velocity} dock={frame.DockPoint} collision={string.Join(',',Enumerable.Range(0,f.Player.GetSlideCollisionCount()).Select(n=>f.Player.GetSlideCollision(n).GetCollider() is Node node?node.Name.ToString():"none"))}");
                previous=frame.State;
                if(frame.State==MechanicalClawState.Released && frame.ReleaseKind==MechanicalClawReleaseKind.Obstructed) break;
            }
            // Physical supported landing is the outcome. Preserve early release
            // reason; never relabel Obstructed as an Arrived event.
            bool arrived=f.Claw.CurrentFrame.Arrived;
            string release=f.Claw.CurrentFrame.ReleaseKind.ToString();
            Vector2 dock=f.Player.GlobalPosition;
            TestAssert.True(arrived && f.Claw.CurrentFrame.State==MechanicalClawState.Swing,
                "BasinTransferArrival: automatic pull must genuinely arrive before a supported gallery release");
            f.CaptureStage("05-arrived-swing");
            if(arrived) await f.Press(Key.K,3);
            await f.Until(() => Floor(f.Player,gallery),45,"normal release settles onto actual gallery top");
            TestAssert.True(f.Player.GlobalPosition.X>=3060 && f.Player.GlobalPosition.X<=3260 &&
                MathF.Abs(f.Player.GlobalPosition.Y-3720f)<1.5f,
                "release lands full capsule on supported gallery, not side cling");
            TestAssert.Equal(health,f.Health.CurrentHealth,"claw transfer landing neither damages nor heals");
            f.CaptureStage("06-supported-gallery");
            await f.Frames(15);
            TestAssert.True(Floor(f.Player,gallery) && f.Player.GlobalPosition.X>=3060 && f.Player.GlobalPosition.X<=3260,
                "natural landing stays fully supported without D walk-on repair");
            GD.Print($"BASIN_TRANSFER_OK station={landingX} preview={preview.WorldAnchor} dock={dock} supported={f.Player.GlobalPosition} health={health} receipt={f.KeyReceipts} arrived={arrived} release={release}");
            if(capture) f.AssertCaptureComplete();
        }
        finally { await f.Dispose(); }
    }

    private static void AssertRider(Fixture f, int health)
    {
        TestAssert.True(Floor(f.Player, f.Lift), $"every-frame floor remains deck; player={f.Player.GlobalPosition} lift={f.Lift.GlobalPosition} elapsed={f.Lift.ElapsedSeconds}");
        TestAssert.Near(f.Lift.GlobalPosition.Y - 48f, f.Player.GlobalPosition.Y, 1.5f,
            "real 72px capsule bottom tracks real 24px deck top");
        TestAssert.True(f.Player.GlobalPosition.X >= 2720f - .1f && f.Player.GlobalPosition.X <= 2800f + .1f,
            "complete unchanged40px capsule remains fully on120px deck");
        TestAssert.Equal(health, f.Health.CurrentHealth, "ride never damages or heals passenger");
        for (int i=0;i<f.Player.GetSlideCollisionCount();i++)
            TestAssert.True(f.Player.GetSlideCollision(i).GetCollider() == f.Lift, "rider has no foreign static/piston contacts");
    }

    private static async Task AttachedExits(SceneTree tree)
    {
        foreach(Key exit in new[]{Key.K,Key.Space,Key.Shift})
        {
            var f=new Fixture(tree,new Vector2(2492,3880));
            try
            {
                await f.Start();await Activate(f);await BoardReturn(f,2760);
                await f.Until(()=>f.Lift.Frame.Phase01>.999f,200,"attached exit begins at actual upperdeck");
                var target=f.Sector.GetNode<GrappleSurface2D>("Gameplay/GrappleSurfaces/BasinCounterweightGalleryAnchor");
                f.Key(Key.W,true);f.Key(Key.D,true);await f.Frames(1);
                TestAssert.True(f.Claw.KeyboardPreview.Surface==target && f.Claw.KeyboardPreview.Snapshot.HasLineOfSight,
                    "attached exit W+D previews the same saved gallery anchor as transfer");
                await f.Press(Key.K,3);f.Key(Key.W,false);f.Key(Key.D,false);
                await f.Until(()=>f.Claw.CurrentFrame.State==MechanicalClawState.Reel && f.Player.GlobalPosition.X>2810,90,"actual saved gallery pull is underway before exit");
                TestAssert.Equal(target.GetInstanceId(),f.Claw.CurrentFrame.LockedTargetRuntimeId,
                    "attached exit cancels the intended gallery anchor, not another surface");
                int jumps=f.Motor.AcceptedJumpCount,dashes=f.Motor.AcceptedDashCount,health=f.Health.CurrentHealth;
                int boosts=0,cancels=0;
                void Released(MechanicalClawFrame frame,GrappleSurface2D _) {
                    if(frame.State==MechanicalClawState.Boost) boosts++;
                    if(frame.State==MechanicalClawState.Released && frame.ReleaseKind==MechanicalClawReleaseKind.Cancel) cancels++;
                }
                f.Claw.ClawFrameChanged+=Released;
                try {await f.Press(exit,3);await f.Frames(3);}
                finally {f.Claw.ClawFrameChanged-=Released;}
                TestAssert.Equal(exit==Key.Space?1:0,boosts,"attached Space is exactly one original boost; K/Shift cannot boost");
                TestAssert.Equal(exit==Key.Space?0:1,cancels,"fresh K and Shift each release actual attachment once");
                TestAssert.Equal(jumps,f.Motor.AcceptedJumpCount,"attached Space is not an extra normaljump");
                TestAssert.Equal(dashes+(exit==Key.Shift?1:0),f.Motor.AcceptedDashCount,"attached Shift retains original dash");
                await f.Until(()=>f.Player.IsOnFloor(),180,"attached release reaches real original support");
                TestAssert.Equal(health,f.Health.CurrentHealth,"bounded attachment exit has no hidden healing or damage suppression");
                GD.Print($"BASIN_ATTACHED_EXIT_OK key={exit} boost={boosts} cancel={cancels} player={f.Player.GlobalPosition} height={f.Motor.LastFallHeight} damage={f.Motor.LastFallDamage} Ksamples={f.SampledClawEdges} Spacesamples={f.SampledJumpEdges} Shiftsamples={f.SampledDashEdges}");
            }
            finally {await f.Dispose();}
        }
    }

    private static async Task Exits(SceneTree tree)
    {
        foreach(Key exit in new[]{Key.Space,Key.Shift})
        {
            var f=new Fixture(tree,new Vector2(2492,3880));
            try
            {
                await f.Start();await Activate(f);await BoardReturn(f,2760);
                int jumps=f.Motor.AcceptedJumpCount,dashes=f.Motor.AcceptedDashCount,health=f.Health.CurrentHealth;
                // The unchanged variable-height jump needs a held Space to clear
                // the80px boarding ledge. A three-frame tap intentionally cannot.
                GD.Print($"BASIN_NORMAL_EXIT_BEGIN key={exit} player={f.Player.GlobalPosition} lift={f.Lift.GlobalPosition} elapsed={f.Lift.ElapsedSeconds}");
                f.Key(Key.A,true);await f.Press(exit,exit==Key.Space?20:3);
                await f.Until(()=>!Floor(f.Player,f.Lift),15,"normal exit leaves carriage");
                for(int i=0;i<90 && f.Player.GlobalPosition.X>=2648;i++)
                {
                    await f.Frames(1);
                    if(i%10==0) GD.Print($"BASIN_NORMAL_EXIT_TRACE key={exit} tick={i} player={f.Player.GlobalPosition} velocity={f.Player.Velocity} onfloor={f.Player.IsOnFloor()} lift={f.Lift.GlobalPosition} elapsed={f.Lift.ElapsedSeconds} piston={f.Piston.Frame} piston_collision={!f.Piston.Collision.Disabled} contacts={string.Join(',',Enumerable.Range(0,f.Player.GetSlideCollisionCount()).Select(n=>f.Player.GetSlideCollision(n).GetCollider() is Node node?node.Name.ToString():"none"))}");
                }
                TestAssert.True(f.Player.GlobalPosition.X<2648,"normal exit moves clear of shaft");
                f.Key(Key.A,false);
                await f.Until(()=>f.Player.IsOnFloor(),150,"normal carriage exit reaches existing support");
                TestAssert.Equal(jumps+(exit==Key.Space?1:0),f.Motor.AcceptedJumpCount,"Space exit is original jump, Shift is not hidden jump");
                TestAssert.Equal(dashes+(exit==Key.Shift?1:0),f.Motor.AcceptedDashCount,"Shift exit is original dash");
                TestAssert.Equal(health,f.Health.CurrentHealth,"normal lower exit preserves original health");
                GD.Print($"BASIN_NORMAL_EXIT_OK key={exit} player={f.Player.GlobalPosition} fall={f.Motor.LastFallHeight} damage={f.Motor.LastFallDamage}");
            }
            finally {await f.Dispose();}
        }
        var missed=new Fixture(tree,new Vector2(2492,3880));
        try
        {
            await missed.Start();await Activate(missed);
            int health=missed.Health.CurrentHealth;
            // Miss the immediate first departure by walking into the empty shaft
            // after activation. Do not wait for the returning carriage this time.
            await missed.Until(()=>missed.Lift.Frame.Phase01>.999f,150,"first departure clears boarding station before deliberately late approach");
            missed.Key(Key.D,true);
            for(int i=0;i<110 && missed.Player.GlobalPosition.X<2748;i++)
            {
                await missed.Frames(1);
                if(i%10==0) GD.Print($"BASIN_MISSED_TRACE tick={i} player={missed.Player.GlobalPosition} velocity={missed.Player.Velocity} onfloor={missed.Player.IsOnFloor()} elapsed={missed.Lift.ElapsedSeconds} piston={missed.Piston.Frame} piston_collision={!missed.Piston.Collision.Disabled} contacts={string.Join(',',Enumerable.Range(0,missed.Player.GetSlideCollisionCount()).Select(n=>missed.Player.GetSlideCollision(n).GetCollider() is Node node?node.Name.ToString():"none"))}");
            }
            TestAssert.True(missed.Player.GlobalPosition.X>=2748,"real missed boarding enters empty shaft");
            missed.Key(Key.D,false);
            var walkway=missed.Sector.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutBasinWalkway");
            await missed.Until(()=>Floor(missed.Player,walkway),180,"missed departure falls to original lower return walkway");
            TestAssert.Equal(0,missed.Motor.LastFallDamage,"bounded failed jump retains original below600px safe fall rule");
            TestAssert.Equal(health,missed.Health.CurrentHealth,"failed jump needs no fixture healing/invulnerability");
            GD.Print($"BASIN_MISSED_BOARDING_LANDING player={missed.Player.GlobalPosition} height={missed.Motor.LastFallHeight} damage={missed.Motor.LastFallDamage} health={missed.Health.CurrentHealth}");
            await WalkBack(missed);
        }
        finally {await missed.Dispose();}
    }

    private static async Task WalkBack(Fixture f)
    {
        f.Key(Key.A,true);
        await f.Until(()=>f.Player.GlobalPosition.X<=2488,180,"actual A walks under shaft/piston body to original return step");
        await f.Press(Key.Space,3);
        var step=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/ShortcutCampReturnStep");
        await f.Until(()=>Floor(f.Player,step),100,"original Space climbs40px camp return step");
        f.Key(Key.A,false);await f.Frames(4);
        f.Key(Key.A,true);
        await f.Until(()=>f.Player.GlobalPosition.X<=2412 && Floor(f.Player,step),30,"supported step approach clears original low ceiling");
        // Original variable-height jump requires holding Space for its full rise.
        await f.Press(Key.Space,20);
        var entry=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/GearEntry");
        await f.Until(()=>Floor(f.Player,entry),120,"original Space climbs80px back to GearEntry");
        f.Key(Key.A,false);await f.Frames(4);
        TestAssert.True(f.Player.GlobalPosition.X<2380 && Floor(f.Player,entry),"missed jump returns by real supported walking/jumps, without reposition/recovery");
        GD.Print($"BASIN_WALK_BACK_OK player={f.Player.GlobalPosition} jumpcount={f.Motor.AcceptedJumpCount} fall={f.Motor.LastFallHeight} damage={f.Motor.LastFallDamage}");
    }

    private static async Task Lifecycle(SceneTree tree)
    {
        var f=new Fixture(tree,new Vector2(2492,3880),full:true);
        string save=null;
        try
        {
            await f.Start();await Activate(f);await BoardReturn(f,2760);
            var level=f.Level;
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu");
            await f.Until(()=>f.Lift.Frame.Phase01>.2f && f.Lift.Frame.Phase01<.8f,120,"riding pause occurs during motion");
            pause.Open();float time=f.Lift.ElapsedSeconds;Vector2 carriage=f.Lift.GlobalPosition,passenger=f.Player.GlobalPosition;
            await f.Frames(12);
            TestAssert.True(tree.Paused && f.Lift.ElapsedSeconds==time && f.Lift.GlobalPosition==carriage && f.Player.GlobalPosition==passenger,
                "real pause freezes actual riding passenger and lift phase");
            pause.Execute(ProloguePauseCommand.Resume);await f.Frames(3);
            TestAssert.True(f.Lift.ElapsedSeconds>time,"resume continues original phase, no restart");
            TestAssert.True(world.TryActivateCheckpoint("canyon.v2.basin-camp"),"legal Basin checkpoint activated");
            TestAssert.Equal(FragmentCollectionResult.Collected,flow.TryCollectFragment("canyon.v2.fragment.entrance"),"isolated metadata fixture commits first distinct known fragment");
            TestAssert.Equal(FragmentCollectionResult.Collected,flow.TryCollectFragment("canyon.v2.fragment.rift"),"isolated metadata fixture commits second distinct known fragment");
            level.Progress.OpenShortcut(CanyonWorldLayout.UpperBasinShortcutId);
            flow.PersistProgress("basin-lifecycle-fixture");
            int recoveries=0;level.RecoveryCompleted+=_=>recoveries++;
            level.RequestRespawn("basin-counterweight-checkpoint-reset");
            await f.Until(()=>recoveries==1 && flow.State==PrologueGameFlowState.Playing,180,"production checkpoint recovery completes once");
            GD.Print($"BASIN_RECOVERY_BOUNDARY activated={f.Lift.IsActivated} elapsed={f.Lift.ElapsedSeconds} position={f.Lift.GlobalPosition}");
            await f.Until(()=>!f.Lift.IsActivated && f.Lift.ElapsedSeconds==0 &&
                f.Lift.GlobalPosition==new Vector2(2760,4008),6,"existing deferred checkpoint mechanism reset and SyncToPhysics transform complete");
            TestAssert.True(!f.Lift.IsActivated && f.Lift.ElapsedSeconds==0 && f.Lift.GlobalPosition==new Vector2(2760,4008),
                "checkpoint recovery resets lift to inactive lower endpoint");
            AssertProgress(level);
            ulong old=f.Lift.GetInstanceId();
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,new Vector2(5800,1040)),"legal actual Temple stream request");
            await f.Frames(12);
            TestAssert.True(!world.TryGetMountedSector(PrologueSectorId.CanyonBasin,out _),"actual remote stream unloads Basin, not visibility-only");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(2492,3880)),"legal Basin remount request");
            await f.Frames(12);
            TestAssert.True(world.TryGetMountedSector(PrologueSectorId.CanyonBasin,out var sector),"actual Basin remounted");
            var fresh=sector.GetNode<CanyonCounterweightLift>("Gameplay/Mechanisms/BasinCounterweightLift");
            TestAssert.True(fresh.GetInstanceId()!=old && !fresh.IsActivated && fresh.ElapsedSeconds==0,"new streamed identity cannot retain lift transient phase");
            AssertProgress(level);
            TestAssert.Equal(1,recoveries,"streaming remount is not extra checkpoint recovery");
            TestAssert.True(flow.TrySaveBeforeQuit(out _),"isolated durable save succeeds before title");
            save=f.Paths.SavePath;
            flow.ReturnToTitle();await f.Frames(3);
            TestAssert.Equal(PrologueGameFlowState.Title,flow.State,"real ReturnToTitle completed");
            GD.Print($"BASIN_LIFECYCLE_OK pause=true recovery={recoveries} unloaded=true fresh_identity=true isolated=true");
        }
        finally {await f.Dispose();}
        var continued=new Fixture(tree,new Vector2(2492,3880),full:true,existingSave:save);
        try
        {
            await continued.Start();
            var flow=continued.Level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(flow.HasSave,"ConfigureExisting admitted same isolated saved slot");
            flow.Continue();await continued.Frames(12);
            var sector=continued.Level.CurrentSectorRoot;
            var lift=sector.GetNode<CanyonCounterweightLift>("Gameplay/Mechanisms/BasinCounterweightLift");
            AssertProgress(continued.Level);
            TestAssert.True(!lift.IsActivated && lift.ElapsedSeconds==0,"Continue preserves progress but never transient lift phase");
            GD.Print("BASIN_CONTINUE_OK fragments=2 shortcut=true checkpoint=basin-camp transient_lift=false real_save_untouched=true");
        }
        finally {await continued.Dispose();}
    }

    private static void AssertProgress(PrologueLevelController level)
    {
        TestAssert.True(level.Progress.CollectedFragmentIds.SetEquals(new[]{"canyon.v2.fragment.entrance","canyon.v2.fragment.rift"}) &&
            level.Progress.IsShortcutOpen(CanyonWorldLayout.UpperBasinShortcutId) &&
            level.Progress.RestoreCheckpoint().CheckpointId=="canyon.v2.basin-camp",
            "Basin ride lifecycle retains exact fragment IDs, shortcut and original checkpoint");
    }

    private static void AssertSweep(Fixture f)
    {
        // Independent hand-derived conservative envelope: deck center x2760 +/-60
        // plus full capsule radius20 even for edge-centered riders. Upper deck3844
        // minus full capsule72 =3772; lower deck bottom4020. Not production helpers.
        Rect2 fullSweep = new(2680,3772,160,248);
        var ledge=f.Sector.GetNode<StaticBody2D>("Gameplay/Collision/GearPistonOne");
        var ledgeCollision=ledge.GetNode<CollisionShape2D>("Collision");
        float ledgeRight=ledgeCollision.GlobalPosition.X+((RectangleShape2D)ledgeCollision.Shape).Size.X/2;
        float missedBoardingGap=2700f-ledgeRight; // Actual120px deck's left edge; x is fixed.
        TestAssert.True(missedBoardingGap>=44f,
            $"SavedBasinMissedBoardingClearance:40px capsule plus positive margin must fit below upper carriage; actual={missedBoardingGap}");
        foreach (StaticBody2D body in f.Sector.GetNode("Gameplay/Collision").GetChildren().OfType<StaticBody2D>())
        {
            var shape = body.GetNodeOrNull<CollisionShape2D>("Collision");
            if (shape?.Shape is not RectangleShape2D rect) continue;
            Rect2 bounds = new(shape.GlobalPosition - rect.Size / 2, rect.Size);
            TestAssert.True(!fullSweep.Intersects(bounds), $"conservative deck/edge-rider sweep clears actual saved static {body.Name}, bounds={bounds}");
        }
        var pistonShape = (RectangleShape2D)f.Piston.Collision.Shape;
        Rect2 pistonSweep = new(2534,3844,132,132);
        TestAssert.True(pistonShape.Size == new Vector2(132,32) && !fullSweep.Intersects(pistonSweep),
            "actual live piston extends/retracts outside passenger sweep");
        GD.Print($"BASIN_SWEEP_OK rider={fullSweep} piston={pistonSweep} minimum_static_margin=4 clearance_piston=14 missed_boarding_gap={missedBoardingGap}");
    }

    // Synchronous value snapshots only: mounted geometry is not a Playing or
    // MoveAndSlide receipt. Observation failure must not replace the route failure.
    private static void ObserveRouteReadiness(string stage, PrologueLevelController level,
        CharacterBody2D player, StaticBody2D first)
    {
        try
        {
            var flow=level?.GetNodeOrNull<PrologueGameFlowController>("GameFlow");
            var motor=player.GetNode<PlayerMotor>("Motor");
            var input=player.GetNode<PlayerInputReader>("Input");
            var body=player.GetNode<CollisionShape2D>("BodyCollider");
            var deck=first.GetNode<CollisionShape2D>("Collision");
            var contacts=new System.Collections.Generic.List<string>();
            for(int i=0;i<player.GetSlideCollisionCount();i++)
            {
                KinematicCollision2D hit=player.GetSlideCollision(i);
                Node collider=hit.GetCollider() as Node;
                contacts.Add($"{collider?.GetPath()}#{collider?.GetInstanceId()} " +
                    $"normal={hit.GetNormal()} first={ReferenceEquals(collider,first)}");
            }
            GD.Print("BASIN_ROUTE_READINESS "+System.Text.Json.JsonSerializer.Serialize(new
            {
                Stage=stage,Physics=Engine.GetPhysicsFrames(),Process=Engine.GetProcessFrames(),
                State=flow?.State.ToString(),Pending=level?.ResidentPreparationPending,
                Failure=level?.ResidentPreparationFailure,Rejected=flow?.LastRejectedTransition,
                NeedsPreparation=level?.NeedsResidentPreparation,Paused=player.GetTree().Paused,
                PlayerPhysics=player.IsPhysicsProcessing(),PlayerCanProcess=player.CanProcess(),
                MotorPhysics=motor.IsPhysicsProcessing(),MotorCanProcess=motor.CanProcess(),
                InputPhysics=input.IsPhysicsProcessing(),InputCanProcess=input.CanProcess(),
                Sample=input.CurrentFrameSequence,Position=player.GlobalPosition.ToString(),
                Velocity=player.Velocity.ToString(),OnFloor=player.IsOnFloor(),
                SlideCount=player.GetSlideCollisionCount(),FirstId=first.GetInstanceId(),
                BodyBounds=(body.GlobalTransform*body.Shape.GetRect()).ToString(),
                FirstBounds=(deck.GlobalTransform*deck.Shape.GetRect()).ToString(),
                FirstDisabled=deck.Disabled,Contacts=contacts.ToArray()
            }));
        }
        catch(Exception error)
        {
            try { GD.Print($"BASIN_ROUTE_READINESS_FAULT stage={stage} type={error.GetType().FullName}"); }
            catch { /* Keep the original support assertion authoritative. */ }
        }
    }

    private static bool Floor(CharacterBody2D player, Node floor)
    {
        if (!player.IsOnFloor()) return false;
        for (int i=0;i<player.GetSlideCollisionCount();i++)
            if (player.GetSlideCollision(i).GetCollider() == floor && player.GetSlideCollision(i).GetNormal().Y < -.7f) return true;
        return false;
    }

    private sealed class Fixture
    {
        internal readonly SceneTree Tree;
        internal readonly Node2D Root;
        internal PrologueSector Sector;
        internal readonly CharacterBody2D Player;
        internal readonly PrologueLevelController Level;
        internal readonly IsolatedPrologueFixturePaths Paths;
        internal PlayerInputReader Input => Player.GetNode<PlayerInputReader>("Input");
        internal PlayerMotor Motor => Player.GetNode<PlayerMotor>("Motor");
        internal MechanicalClawController Claw => Player.GetNode<MechanicalClawController>("MechanicalClaw");
        internal HealthComponent Health => Player.GetNode<HealthComponent>("Health");
        internal CanyonCounterweightLift Lift;
        internal CanyonGearPiston Piston => Sector.GetNode<CanyonGearPiston>("Gameplay/Mechanisms/GearPiston");
        internal int KeyReceipts;
        internal int SampledClawEdges,SampledJumpEdges,SampledDashEdges;
        private ulong _sampleSequence;
        private readonly FormalInputSampleObserver _inputObserver=new() { ProcessPhysicsPriority=-250 };
        private readonly System.Collections.Generic.HashSet<Key> _held = new();
        private readonly System.Collections.Generic.HashSet<string> _captured = new();
        private readonly System.Collections.Generic.HashSet<string> _captureRequests = new();
        private string _captureFolder;
        private Exception _captureFailure;
        private readonly BasinBoardingTickObserver _boardingObserver=new() { ProcessPhysicsPriority=100 };
        private readonly BoardingSample[] _boardingSamples=new BoardingSample[240];
        private int _boardingCount;
        private bool _boardingTrace;
        private float _boardingThreshold;
        private ulong _boardingReleaseTick;
        private Vector2 _boardingReleasePosition;
        private bool _boardingReleaseArmed,_boardingReleased;
        private Exception _boardingFailure;
        internal bool BoardingReleaseCompleted => _boardingFailure==null ? _boardingReleased :
            throw new InvalidOperationException("boarding physics input release failed",_boardingFailure);
        private readonly record struct BoardingSample(ulong Physics,ulong Process,ulong Sequence,
            Vector2 Position,Vector2 Velocity,float Axis,bool RawD,bool OwnedD,bool Floor,float LiftTime,double Delta);
        internal void BeginBoardingTrace(float threshold)
        {
            _boardingThreshold=threshold; _boardingCount=0; _boardingTrace=true;
            _boardingReleaseArmed=true; _boardingReleased=false; _boardingFailure=null;
        }
        private void RecordBoardingRelease()
        {
            _boardingReleaseTick=Engine.GetPhysicsFrames(); _boardingReleasePosition=Player.GlobalPosition;
        }
        internal void AssertBoardingReleaseTimely()
        {
            BoardingSample crossed=default;
            for(int i=0;i<_boardingCount;i++)
                if(_boardingSamples[i].Position.X>=_boardingThreshold) { crossed=_boardingSamples[i];break; }
            TestAssert.True(crossed.Physics!=0 && _boardingReleaseTick>=crossed.Physics &&
                _boardingReleaseTick-crossed.Physics<=1,
                $"BasinBoardingDriverLateRelease: intended threshold must release by next physics tick; crossed={crossed.Physics} release={_boardingReleaseTick} delta={_boardingReleaseTick-crossed.Physics} position={_boardingReleasePosition}");
        }
        private void SampleBoarding(double delta)
        {
            if(!_boardingTrace || _boardingCount>=_boardingSamples.Length) return;
            _boardingSamples[_boardingCount++]=new(Engine.GetPhysicsFrames(),Engine.GetProcessFrames(),
                Input.CurrentFrameSequence,Player.GlobalPosition,Player.Velocity,Input.Current.MoveAxis,
                Godot.Input.IsPhysicalKeyPressed(Godot.Key.D),_held.Contains(Godot.Key.D),
                Floor(Player,Lift),Lift.ElapsedSeconds,delta);
            // The driver must see every physics tick, even when a drawn frame
            // batches several ticks. Release only its own D at the same authored
            // threshold; ordinary production braking/collision remains in charge.
            if(_boardingReleaseArmed && Player.GlobalPosition.X>=_boardingThreshold)
            {
                _boardingReleaseArmed=false;
                try { Key(Godot.Key.D,false); RecordBoardingRelease(); _boardingReleased=true; }
                catch(Exception error) { _boardingFailure=error; }
            }
        }
        internal void CompleteBoardingTrace() => WriteBoardingTrace();
        private void WriteBoardingTrace()
        {
            if(!_boardingTrace) return;
            _boardingTrace=false;
            _boardingReleaseArmed=false;
            GD.Print($"BASIN_BOARDING_TRACE threshold={_boardingThreshold} release_tick={_boardingReleaseTick} release_position={_boardingReleasePosition} count={_boardingCount} overflow={_boardingCount==_boardingSamples.Length}");
            for(int i=0;i<_boardingCount;i++)
            {
                var s=_boardingSamples[i];
                GD.Print($"BASIN_BOARDING_TICK i={i} physics={s.Physics} process={s.Process} sequence={s.Sequence} position={s.Position} velocity={s.Velocity} axis={s.Axis} raw_d={s.RawD} owned_d={s.OwnedD} lift_floor={s.Floor} lift_time={s.LiftTime} delta={s.Delta}");
            }
        }
        internal void EnableCapture()
        {
            _captureFolder=System.IO.Path.Combine(IsolatedPrologueFixture.ValidateStorageRoot(),"proof");
            TestAssert.True(!System.IO.Directory.Exists(_captureFolder),"capture output must be a fresh external directory");
            System.IO.Directory.CreateDirectory(_captureFolder);
            RenderingServer.FramePostDraw+=OnCaptureFrame;
            CaptureStage("01-control-station");
        }
        internal void CaptureStage(string stage)
        {
            if(_captureFolder!=null && !_captured.Contains(stage)) _captureRequests.Add(stage);
        }
        private void OnCaptureFrame()
        {
            if(_captureFailure!=null || !Root.IsInsideTree()) return;
            try
            {
                foreach(string stage in _captureRequests.ToArray())
                {
                    using Image pixels=Tree.Root.GetTexture().GetImage();
                    TestAssert.True(pixels.GetWidth()==1280 && pixels.GetHeight()==720,"proof is actual1280x720 viewport");
                    TestAssert.Equal(Error.Ok,pixels.SavePng(System.IO.Path.Combine(_captureFolder,stage+".png")),"save rendered proof PNG");
                    GD.Print($"BASIN_CAPTURE stage={stage} physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} actor={Player.GlobalPosition} claw={Claw.CurrentFrame.State} health={Health.CurrentHealth} full_scene={Level!=null} human=false performance=false");
                    _captured.Add(stage);_captureRequests.Remove(stage);
                }
            }
            catch(Exception error) { _captureFailure=error; }
        }
        internal void AssertCaptureComplete()
        {
            if(_captureFailure!=null) throw new InvalidOperationException("rendered proof failed",_captureFailure);
            TestAssert.True(_captured.Count==6 && _captureRequests.Count==0,"all six physical transfer stages were actually rendered");
        }
        internal Fixture(SceneTree tree, Vector2 spawn,bool full=false,string existingSave=null)
        {
            Tree=tree;
            if(full)
            {
                Level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
                Paths=existingSave==null?IsolatedPrologueFixture.Configure(Level,"basin-counterweight-lifecycle"):
                    IsolatedPrologueFixture.ConfigureExisting(Level,existingSave);
                var flow=Level.GetNode<PrologueGameFlowController>("GameFlow");
                flow.HeadlessAutoStart=false;flow.AutomationAutoStart=existingSave==null;
                Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector=PrologueSectorId.CanyonBasin;
                Player=Level.GetNode<CharacterBody2D>("PlayerRoot/Player");Player.Position=spawn;
                Root=Level;return;
            }
            Root=new Node2D {Name="BasinCounterweightFixture"};
            Sector=GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn").Instantiate<PrologueSector>();
            Sector.Position=new Vector2(1280,3600);
            Player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position=spawn;
            Root.AddChild(Sector); Root.AddChild(Player);
        }
        internal async Task Start()
        {
            Root.AddChild(_inputObserver);
            _boardingObserver.Sample=SampleBoarding;
            Root.AddChild(_boardingObserver);
            Tree.Root.AddChild(Root);
            await Frames(8);
            if(Level!=null) Sector=Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").TryGetMountedSector(PrologueSectorId.CanyonBasin,out var mounted)?mounted:null;
            TestAssert.True(Sector!=null,"saved Basin fixture actually mounted");
            Lift=Sector.GetNodeOrNull<CanyonCounterweightLift>("Gameplay/Mechanisms/BasinCounterweightLift");
            TestAssert.True(Lift != null, "SavedBasinPassengerLiftMissing: saved Basin must provide a real looping passenger-bearing lift");
            GD.Print($"BASIN_FIXTURE_READY physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} player={Player.GlobalPosition} velocity={Player.Velocity} lift_time={Lift.ElapsedSeconds} full={Level!=null}");
            ObserveRouteReadiness("fixture_after_original8",Level,Player,
                Sector.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/GearPistonOne"));
        }
        internal void Key(Key key, bool down)
        {
            if (_held.Contains(key)==down) return;
            if (down) _held.Add(key); else _held.Remove(key);
            if(down && key is Godot.Key.K or Godot.Key.Space or Godot.Key.Shift) _inputObserver.Arm(Input);
            var inputEvent=new InputEventKey { PhysicalKeycode=key, Keycode=key, Pressed=down };
            Godot.Input.ParseInputEvent(inputEvent); Godot.Input.FlushBufferedEvents();
            TestAssert.True(Godot.Input.IsPhysicalKeyPressed(key)==down, $"native physical key receipt {key}={down}");
            KeyReceipts++;
        }
        internal async Task Press(Key key, int frames) { Key(key,true); await Frames(frames); Key(key,false); }
        internal async Task Frames(int count)
        {
            for (int i=0;i<count;i++)
            {
                await Tree.ToSignal(Tree, SceneTree.SignalName.PhysicsFrame);
                await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
                // Observe the actual input tick, before action owners consume
                // edges. Slow rendered frames can contain several physics ticks.
                if(_inputObserver.Captured && _inputObserver.FirstSequence!=_sampleSequence)
                {
                    _sampleSequence=_inputObserver.FirstSequence;
                    var sampled=_inputObserver.FirstFrame;
                    if(sampled.ClawPressed) SampledClawEdges++;
                    if(sampled.JumpPressed) SampledJumpEdges++;
                    if(sampled.DashPressed) SampledDashEdges++;
                }
            }
        }
        internal async Task Until(Func<bool> condition,int cap,string description)
        {
            for(int i=0;i<cap && !condition();i++) await Frames(1);
            TestAssert.True(condition(), $"{description}; player={Player.GlobalPosition} lift={Lift?.GlobalPosition} elapsed={Lift?.ElapsedSeconds} claw={Claw.CurrentFrame.State}");
        }
        internal async Task Dispose()
        {
            WriteBoardingTrace();
            _boardingObserver.Sample=null;
            if(_captureFolder!=null) RenderingServer.FramePostDraw-=OnCaptureFrame;
            foreach (Key key in _held.ToArray()) Key(key,false);
            Tree.Paused=false;
            if(Root.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(Tree,Root); else Root.Free();
        }
    }
}

// Test-only post-Motor callback; the fixture may release its owned D here.
internal partial class BasinBoardingTickObserver : Node
{
    internal Action<double> Sample;
    public override void _PhysicsProcess(double delta) => Sample?.Invoke(delta);
}
