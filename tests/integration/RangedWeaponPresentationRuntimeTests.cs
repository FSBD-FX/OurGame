using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue.Guidance;

namespace Knight.Tests.Integration;

public static class RangedWeaponPresentationRuntimeTests
{
    private const string TelegraphPath = "res://assets/audio/prologue/sfx/enemies/laborer-telegraph.wav";
    private const string ShotPath = "res://assets/audio/prologue/sfx/enemies/rust-attack.wav";
    private const string RecoveryPath = "res://assets/audio/prologue/sfx/enemies/laborer-recovery.wav";
    private const string HurtPath = "res://assets/audio/prologue/sfx/enemies/laborer-hurt.wav";
    private const string DeathPath = "res://assets/audio/prologue/sfx/enemies/laborer-death.wav";
    private static readonly Rect2 Arena = new(80f, 120f, 1000f, 500f);

    public static async Task Run(SceneTree tree)
    {
        await ImmediateThreadCancellationRejectsRecordedConsumerInput(tree);
        foreach ((RangedWeaponRole role, int shots) in new[]
        {
            (RangedWeaponRole.Scatter, 1), (RangedWeaponRole.Burst, 3),
            (RangedWeaponRole.Missile, 1), (RangedWeaponRole.Laser, 1)
        }) await RealActorWarningAndEmissionAudio(tree, role, shots);
        await ThreadAndCheckpointReleaseRealSource(tree);
        await RealHurtDeathAndAncestryNeverUsePlayerCues(tree);
        await DeathDuringRealBurstSuppressesTailAudio(tree);
        await PausedAndDisabledConsumersDoNotEmitOrConsumeNewPhase(tree);
        await QueuedAncestorsAndDetachedSourcesFailClosed(tree);
        GD.Print("RANGED_PRESENTATION_RUNTIME_OK saved_roles=4 actual_shots=1/3/1/1 manifest=true cancellation=true human_listening=false whole_goal=false");
    }

    // Break caught: ApplyThreadPull synchronously revokes attacks and publishes
    // None, releasing audio dedup, while CurrentFrame still says Active. The
    // public consumer must reject a recorded real event in that same stack,
    // not merely rely on the actor suppressing future natural emissions.
    private static async Task ImmediateThreadCancellationRejectsRecordedConsumerInput(SceneTree tree)
    {
        Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedImmediateThreadConsumer");
        try
        {
            await Until(tree, () => f.Shots.Count == 1, 110, "actual first saved Burst event before synchronous thread cancellation");
            TestAssert.True(f.Actor.CurrentFrame.AttackTimeline.Phase is AttackPhase.Active or AttackPhase.Recovery,
                "real Burst is in a shot-owning phase immediately before thread cancellation");
            TestAssert.Equal(1, f.Shots.Count, "exactly the first actual Burst event is recorded before cancellation");
            TestAssert.True(f.Actor.GetChildren().OfType<RangedWeaponProjectile>().Any(projectile => !projectile.IsRetired),
                "actual first Burst event has a live owned projectile to revoke");
            ShotObservation shot = f.Shots[0];
            AttackPresentationFrame oldWarning = f.Frames.First(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
            int before = f.Audio.FramePlaybackCount;

            // No await/manual step between cancellation and either replay.
            f.Actor.ApplyThreadPull(Vector2.Left * 400f);
            var leaves = new List<AttackTelegraphVisual>();
            f.Actor.AppendAuthorizedAttackLeaves(leaves);
            TestAssert.Equal(0, leaves.Count, "real thread request synchronously revokes every authorized attack leaf");
            TestAssert.True(!f.Actor.GetChildren().OfType<RangedWeaponProjectile>().Any(projectile => !projectile.IsRetired),
                "real thread request synchronously retires its actual owned Burst projectile");
            TestAssert.Equal(AttackPhase.None, f.Frames.Last().Timeline.Phase,
                "actual cancellation publishes None to the serialized presentation before physics advances");
            TestAssert.True(!TryShot(ShotApi(), f, shot),
                "RANGED_AUDIO_IMMEDIATE_THREAD_CANCEL_RED: same-stack cancelled owner must reject the exact recorded real ShotEmitted ordinal");
            TestAssert.Equal(before, f.Audio.FramePlaybackCount,
                "same-stack cancelled real event cannot increment playback after dedup release");
            f.Audio.PlayAttackFrame(f.Actor, oldWarning, oldWarning.Shape.Origin);
            TestAssert.Equal(before, f.Audio.FramePlaybackCount,
                "RANGED_AUDIO_IMMEDIATE_THREAD_PHASE_RED: same-stack cancelled owner must reject its old actual typed warning frame");
            TestAssert.Equal(1, f.Shots.Count,
                "consumer replay probes do not fabricate another natural Burst emission");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: saved actors publish a real warning, but the shared guidance
    // adapter ignores their base class; presentation drops audio or sounds once
    // per Active frame/pellet instead of once per real ShotEmitted event.
    private static async Task RealActorWarningAndEmissionAudio(SceneTree tree, RangedWeaponRole role, int expectedShots)
    {
        Fixture f = Create(tree, role, "RangedConsumer" + role);
        try
        {
            await Until(tree, () => f.Actor.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph, 12,
                "actual saved " + role + " actor naturally enters Telegraph");
            AssertRealAttackPreconditions(f);
            TestAssert.True(CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor),
                "RANGED_GUIDANCE_REAL_TELEGRAPH_RED: actual saved " + role + " warning must be recognized by the shared guidance consumer");
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor.GetNode("Presentation")),
                "an arbitrary descendant is not itself a telegraph source");
            TestAssert.Equal(1, f.Audio.FramePlaybackCount,
                "RANGED_AUDIO_REAL_TELEGRAPH_RED: saved presentation must route the actor's first typed warning exactly once");
            AssertOutput(f, PrologueAudioCue.LaborerTelegraph, TelegraphPath, "real typed warning");

            AttackPresentationFrame warning = f.Frames.Last(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
            int warningCount = f.Audio.FramePlaybackCount;
            f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
            f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
            TestAssert.Equal(warningCount, f.Audio.FramePlaybackCount,
                "re-presenting the real typed warning cannot replay its sound");
            await Frames(tree, 5);
            TestAssert.Equal(warningCount, f.Audio.FramePlaybackCount,
                "natural warning geometry refreshes do not produce polling chatter");

            await Until(tree, () => f.Actor.CurrentFrame.AttackTimeline.Phase == AttackPhase.Recovery, 150,
                "actual saved " + role + " actor finishes its first native attack");
            f.Actor.SetPhysicsProcess(false);
            TestAssert.Equal(expectedShots, f.Shots.Count,
                role + " naturally publishes the independently specified emission count");
            TestAssert.Equal(expectedShots, f.Shots.Select(shot => shot.Ordinal).Distinct().Count(),
                "real emissions keep distinct owner-local damage ordinals");
            for (int index = 0; index < f.Shots.Count; index++)
            {
                ShotObservation shot = f.Shots[index];
                TestAssert.Equal(warningCount + index + 1, shot.PlaybackCount,
                    "RANGED_AUDIO_REAL_SHOT_RED: " + role + " emission " + (index + 1) + " produces exactly one actual shot onset, with no Active-phase extra");
                TestAssert.Equal(PrologueAudioCue.RustShellAttack, shot.Cue,
                    "hostile real shot uses an existing enemy onset, never PlayerAttack");
                TestAssert.Equal(ShotPath, shot.Path, "real emission selects the exact existing manifest shot path");
                TestAssert.Near(0f, shot.Origin.DistanceTo(shot.AudioOrigin), .001f,
                    "actual shot audio retains the emission world origin");
            }
            TestAssert.Equal(expectedShots + 2, f.Audio.FramePlaybackCount,
                "one warning, actual emission count and one recovery are the entire first attack audio");
            AssertOutput(f, PrologueAudioCue.LaborerRecovery, RecoveryPath, "real typed recovery");
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor),
                "guidance stops advertising a warning during Recovery");

            int count = f.Audio.FramePlaybackCount;
            AttackPresentationFrame active = f.Frames.First(frame => frame.Timeline.Phase == AttackPhase.Active);
            f.Audio.PlayAttackFrame(f.Actor, active, active.Shape.Origin);
            f.Audio.PlayAttackFrame(f.Actor, active, active.Shape.Origin);
            TestAssert.Equal(count, f.Audio.FramePlaybackCount,
                "replayed actual Active frames cannot add a fourth burst or a second volley/beam onset");
            MethodInfo shotApi = ShotApi();
            foreach (ShotObservation shot in f.Shots)
                TestAssert.True(!TryShot(shotApi, f, shot), "an already audible actual event ordinal cannot sound twice");
            TestAssert.Equal(count, f.Audio.FramePlaybackCount, "actual-shot ordinal dedup preserves the total count");
            await Frames(tree, 180);
            TestAssert.Equal(count, f.Audio.FramePlaybackCount,
                "projectile retirement and beam recovery never create late shot onsets");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: thread/reset clears visible geometry but leaves consumer
    // identities alive, or resets damage ordinals and suppresses the next shot.
    private static async Task ThreadAndCheckpointReleaseRealSource(SceneTree tree)
    {
        Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedThreadResetConsumer");
        try
        {
            await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning before thread cancellation");
            int beforeThread = f.Audio.FramePlaybackCount;
            f.Actor.ApplyThreadPull(Vector2.Left * 400f);
            await Frames(tree, 25);
            TestAssert.Equal(RangedWeaponState.Stunned, f.Actor.CurrentFrame.State, "real thread receiver enters its native stun");
            TestAssert.Equal(0, f.Shots.Count, "thread cancellation before firing produces no shot event");
            TestAssert.Equal(beforeThread, f.Audio.FramePlaybackCount, "cancelled warning produces no late recovery or shot sound");
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor), "stunned actor is not a guidance warning");

            f.Actor.ResetForCheckpoint();
            await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning after checkpoint reset");
            TestAssert.Equal(beforeThread + 1, f.Audio.FramePlaybackCount,
                "reset releases the cancelled source so restarted local AttackId=1 warns audibly again");
            await Until(tree, () => f.Shots.Count == 1, 100, "first real burst event after reset");
            ShotObservation first = f.Shots[0];
            TestAssert.Equal(PrologueAudioCue.RustShellAttack, first.Cue, "reset's fresh event reaches the actual shot consumer");
            f.Actor.ResetForCheckpoint();
            int afterReset = f.Audio.FramePlaybackCount;
            await Until(tree, () => f.Shots.Count == 2, 110, "first real burst event after a second reset");
            f.Actor.SetPhysicsProcess(false);
            ShotObservation second = f.Shots[1];
            TestAssert.True(second.Ordinal > first.Ordinal, "checkpoint restart preserves real damage-ordinal monotonicity");
            TestAssert.Equal(afterReset + 2, f.Audio.FramePlaybackCount,
                "post-reset repeated local timeline emits one new warning and one fresh monotonic shot");
            TestAssert.Equal(PrologueAudioCue.RustShellAttack, second.Cue, "new monotonic ordinal is not suppressed by stale source dedup");
            f.Actor.ApplyThreadPull(Vector2.Left * 400f);
            f.Actor.SetPhysicsProcess(true);
            int cancelled = f.Audio.FramePlaybackCount;
            await Frames(tree, 25);
            TestAssert.Equal(2, f.Shots.Count, "thread cancellation removes both un-emitted burst tails");
            TestAssert.Equal(cancelled, f.Audio.FramePlaybackCount, "thread-retired rounds produce no audio after cancellation");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: after an actual first shot, death lets either remaining
    // burst event through, or the public consumer accepts an abandoned event.
    private static async Task DeathDuringRealBurstSuppressesTailAudio(SceneTree tree)
    {
        Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedDeathMidBurstConsumer");
        try
        {
            await Until(tree, () => f.Shots.Count == 1, 110, "real first event before burst-owner death");
            ShotObservation shot = f.Shots[0];
            HealthComponent health = f.Actor.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(2, Vector2.Left, 0f, 601UL, 9601UL)),
                "actual burst owner accepts lethal damage after the first shot");
            int count = f.Audio.FramePlaybackCount;
            f.Audio.ReleaseAttackSource(f.Actor);
            TestAssert.True(!TryShot(ShotApi(), f, shot), "dead real owner rejects even a released actual-shot ordinal");
            await Frames(tree, 40);
            TestAssert.Equal(1, f.Shots.Count, "death after first burst event suppresses both un-emitted real tails");
            TestAssert.Equal(count, f.Audio.FramePlaybackCount, "dead burst and retired rounds cannot emit late shot audio");
        }
        finally { await Dispose(tree, f); }
    }

    // Break caught: Hurt/Death from a new actor or its descendant takes the
    // generic player fallback; a dead owner continues a pending burst.
    private static async Task RealHurtDeathAndAncestryNeverUsePlayerCues(SceneTree tree)
    {
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
        {
            Fixture f = Create(tree, role, "RangedLifeConsumer" + role);
            try
            {
                await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning before damage");
                HealthComponent health = f.Actor.GetNode<HealthComponent>("Health");
                TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 501UL, 9501UL)),
                    "real saved ranged Health accepts the nonlethal request");
                AssertOutput(f, PrologueAudioCue.LaborerHurt, HurtPath, "actual ranged Hurt publication");
                PrologueAudioDirector.PlayPresentationCueFromTree(f.Actor.GetNode("Presentation"), PresentationCue.Hurt, f.Actor.GlobalPosition);
                AssertOutput(f, PrologueAudioCue.LaborerHurt, HurtPath, "ranged descendant ancestry mapping");
                TestAssert.True(health.ApplyDamage(new DamageRequest(1, Vector2.Left, 0f, 502UL, 9501UL)),
                    "real saved ranged Health accepts the lethal request");
                AssertOutput(f, PrologueAudioCue.LaborerDeath, DeathPath, "actual ranged Death publication");
                PrologueAudioDirector.PlayPresentationCueFromTree(f.Actor.GetNode("Presentation"), PresentationCue.Died, f.Actor.GlobalPosition);
                AssertOutput(f, PrologueAudioCue.LaborerDeath, DeathPath, "ranged descendant death ancestry mapping");
                int deadCount = f.Audio.FramePlaybackCount;
                await Frames(tree, 110);
                TestAssert.Equal(RangedWeaponState.Dead, f.Actor.CurrentFrame.State, "actual actor remains dead");
                TestAssert.Equal(0, f.Shots.Count, "death during warning cancels every real pending emission");
                TestAssert.Equal(deadCount, f.Audio.FramePlaybackCount, "dead actor cannot sound its abandoned shot or recovery");
                TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor), "dead owner releases its guidance warning");
            }
            finally { await Dispose(tree, f); }
        }
    }

    // Break caught: new phase/shot audio bypasses pause or disabled playback,
    // or a rejected attempt consumes dedup and suppresses a later accepted one.
    private static async Task PausedAndDisabledConsumersDoNotEmitOrConsumeNewPhase(SceneTree tree)
    {
        foreach (string gate in new[] { "audio-pause", "tree-pause", "disabled" })
        {
            Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedGate" + gate);
            try
            {
                await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning before gate");
                AttackPresentationFrame warning = f.Frames.Last(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
                f.Actor.SetPhysicsProcess(false);
                f.Audio.ReleaseAttackSource(f.Actor);
                if (gate == "audio-pause") f.Audio.SetPaused(true);
                else if (gate == "tree-pause") tree.Paused = true;
                else f.Audio.PlaybackEnabled = false;
                int before = f.Audio.FramePlaybackCount;
                string beforePath = f.Audio.LastPlayedPath;
                f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
                TestAssert.Equal(before, f.Audio.FramePlaybackCount, gate + " rejects actual typed warning without counting nonexistent playback");
                TestAssert.Equal(beforePath, f.Audio.LastPlayedPath, gate + " cannot select a new sound path");
                tree.Paused = false;
                f.Audio.SetPaused(false);
                f.Audio.PlaybackEnabled = true;
                f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
                TestAssert.Equal(before + 1, f.Audio.FramePlaybackCount, "rejected " + gate + " phase does not consume the warning identity");
                f.Actor.SetPhysicsProcess(true);

                if (gate == "audio-pause") f.Audio.SetPaused(true);
                else f.Audio.PlaybackEnabled = false;
                int muted = f.Audio.FramePlaybackCount;
                await Until(tree, () => f.Shots.Count == 1, 100, "real emission while shot audio is gated");
                f.Actor.SetPhysicsProcess(false);
                ShotObservation shot = f.Shots[0];
                TestAssert.Equal(muted, shot.PlaybackCount, gate + " cannot count a muted actual shot as playback");
                MethodInfo api = ShotApi();
                TestAssert.True(!TryShot(api, f, shot), "gated actual-shot boundary rejects the real emitted ordinal");
                tree.Paused = true;
                f.Audio.SetPaused(false);
                f.Audio.PlaybackEnabled = true;
                TestAssert.True(!TryShot(api, f, shot), "raw tree pause gates the real shot boundary even though the director always processes");
                tree.Paused = false;
                TestAssert.True(!TryShot(api, f, shot with { Origin = new Vector2(float.NaN, 0f) }),
                    "actual ordinal with malformed origin fails closed without consuming its sound identity");
                TestAssert.True(TryShot(api, f, shot), "rejected real ordinal is accepted once after playback resumes");
                TestAssert.Equal(muted + 1, f.Audio.FramePlaybackCount, "accepted actual shot increments the established playback counter exactly once");
                AssertOutput(f, PrologueAudioCue.RustShellAttack, ShotPath, "unconsumed real emission after gate");
                TestAssert.True(!TryShot(api, f, shot), "accepted actual event is deduplicated after resume");
            }
            finally { tree.Paused = false; await Dispose(tree, f); }
        }
    }

    // Break caught: only source.IsQueuedForDeletion is checked; a queued parent
    // or grandparent still leaks guidance/audio before deferred deletion runs.
    private static async Task QueuedAncestorsAndDetachedSourcesFailClosed(SceneTree tree)
    {
        foreach (int queuedDepth in new[] { 0, 1, 2 })
        {
            Fixture f = Create(tree, RangedWeaponRole.Burst, "RangedQueuedConsumer" + queuedDepth);
            try
            {
                await Until(tree, () => f.Actor.CurrentFrame.State == RangedWeaponState.Telegraph, 12, "real warning before queue gate");
                AttackPresentationFrame warning = f.Frames.Last(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
                f.Actor.SetPhysicsProcess(false);
                Node queued = queuedDepth == 0 ? f.Actor : queuedDepth == 1 ? f.Branch : f.Envelope;
                queued.QueueFree();
                TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(f.Actor),
                    "RANGED_GUIDANCE_QUEUED_ANCESTOR_RED: real warning rejects queued ancestor depth " + queuedDepth);
                f.Audio.ReleaseAttackSource(f.Actor);
                int count = f.Audio.FramePlaybackCount;
                f.Audio.PlayAttackFrame(f.Actor, warning, warning.Shape.Origin);
                TestAssert.Equal(count, f.Audio.FramePlaybackCount, "queued ancestry cannot replay a released real warning");
            }
            finally { await Dispose(tree, f); }

            Fixture emitted = Create(tree, RangedWeaponRole.Burst, "RangedQueuedShotConsumer" + queuedDepth);
            try
            {
                await Until(tree, () => emitted.Shots.Count == 1, 110, "real emitted ordinal before queued-source gate");
                emitted.Actor.SetPhysicsProcess(false);
                ShotObservation shot = emitted.Shots[0];
                Node queued = queuedDepth == 0 ? emitted.Actor : queuedDepth == 1 ? emitted.Branch : emitted.Envelope;
                emitted.Audio.ReleaseAttackSource(emitted.Actor);
                queued.QueueFree();
                int count = emitted.Audio.FramePlaybackCount;
                TestAssert.True(!TryShot(ShotApi(), emitted, shot), "actual-shot boundary rejects queued ancestry depth " + queuedDepth);
                TestAssert.Equal(count, emitted.Audio.FramePlaybackCount, "queued actual ordinal cannot count or replay a late shot");
                await Frames(tree, 3);
                TestAssert.Equal(1, emitted.Shots.Count, "queued actual owner cannot emit either remaining burst tail");
            }
            finally { await Dispose(tree, emitted); }
        }

        Fixture detached = Create(tree, RangedWeaponRole.Burst, "RangedDetachedConsumer");
        try
        {
            await Until(tree, () => detached.Shots.Count == 1, 110, "real emission before source detachment");
            detached.Actor.SetPhysicsProcess(false);
            ShotObservation shot = detached.Shots[0];
            AttackPresentationFrame warning = detached.Frames.First(frame => frame.Timeline.Phase == AttackPhase.Telegraph);
            detached.Branch.RemoveChild(detached.Actor);
            int count = detached.Audio.FramePlaybackCount;
            TestAssert.True(!CanyonCombatTelegraphAdapter.IsTelegraphActive(detached.Actor), "detached actual actor has no guidance warning");
            detached.Audio.PlayAttackFrame(detached.Actor, warning, warning.Shape.Origin);
            TestAssert.True(!TryShot(ShotApi(), detached, shot), "detached actual shot source fails closed");
            TestAssert.Equal(count, detached.Audio.FramePlaybackCount, "source exit releases audio state and blocks late typed/shot playback");
            detached.Actor.Free();
        }
        finally
        {
            if (GodotObject.IsInstanceValid(detached.Actor) && !detached.Actor.IsInsideTree()) detached.Actor.Free();
            await Dispose(tree, detached);
        }
    }

    private static Fixture Create(SceneTree tree, RangedWeaponRole role, string name)
    {
        string actorName = role switch
        {
            RangedWeaponRole.Scatter => nameof(RivetScatterGunner),
            RangedWeaponRole.Burst => nameof(BeltBurstGunner),
            RangedWeaponRole.Missile => nameof(FurnaceMissileGunner),
            _ => nameof(PrismLaserGunner)
        };
        string path = "res://scenes/enemies/" + actorName + ".tscn";
        PackedScene scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
        TestAssert.True(scene != null, "consumer fixture loads the actual saved leaf: " + path);
        RangedWeaponEnemy actor = scene.Instantiate<RangedWeaponEnemy>();
        TestAssert.Equal(path, actor.SceneFilePath, "consumer fixture retains the saved leaf identity");
        var resources = new HashSet<Resource> { actor.Settings };
        CollectShapes(actor, resources);
        var root = new Node2D { Name = name };
        var envelope = new Node2D { Name = "EncounterEnvelope" };
        var branch = new Node2D { Name = "ActorBranch" };
        var floorShape = new RectangleShape2D { Size = new Vector2(1100f, 20f) };
        resources.Add(floorShape);
        var floor = new StaticBody2D { Name = "NativeFloor", Position = new Vector2(580f, 339f),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
        floor.AddChild(new CollisionShape2D { Shape = floorShape });
        // The cached Player scene and its imported resources are borrowed, never disposed here.
        CharacterBody2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "ActualPlayer";
        player.Position = new Vector2(440f, 300f);
        player.AddToGroup("player");
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        PrologueAudioDirector audio = PrologueAudioDirector.CreateRuntimeTree();
        audio.PlaybackEnabled = false;
        root.AddChild(audio);
        root.AddChild(floor);
        root.AddChild(player);
        root.AddChild(envelope);
        envelope.AddChild(branch);
        actor.Position = new Vector2(200f, 300f);
        actor.ConfigureEncounterEnvelope(Arena);
        branch.AddChild(actor);
        tree.Root.AddChild(root);
        audio.PlaybackEnabled = true;
        HealthComponent playerHealth = player.GetNode<HealthComponent>("Health");
        playerHealth.MaxHealth = 100;
        playerHealth.ResetFull();
        var f = new Fixture(root, envelope, branch, actor, player, audio, scene, resources);
        // Mount first: the serialized Presentation subscribes in _Ready before
        // observations, so snapshots measure its real consumer side effects.
        actor.AttackFrameChanged += frame => f.Frames.Add(frame);
        actor.ShotEmitted += (ordinal, origin) => f.Shots.Add(new ShotObservation(ordinal, origin,
            audio.FramePlaybackCount, audio.LastCue, audio.LastPlayedPath, audio.LastWorldPosition));
        return f;
    }

    private static void AssertRealAttackPreconditions(Fixture f)
    {
        Vector2 muzzle = f.Actor.GetNode<Node2D>("Muzzle").GlobalPosition;
        TestAssert.True(f.Player is CharacterBody2D && f.Player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
            "warning precondition uses the actual living saved Player/Health");
        TestAssert.True(Arena.HasPoint(muzzle) && muzzle.DistanceTo(f.Player.GlobalPosition) < 360f,
            "real muzzle is admitted and player is within the shortest weapon detection range");
        TestAssert.True(f.Actor.GetViewport().GetVisibleRect().HasPoint(f.Actor.GetNode<Node2D>("Muzzle").GetGlobalTransformWithCanvas().Origin),
            "native viewport contains the actual muzzle");
        TestAssert.True(RangedAttackGeometry.HasClearWorldPath(f.Actor.GetWorld2D().DirectSpaceState, muzzle, f.Player.GlobalPosition),
            "native world physics confirms actual player LOS before consumer RED");
    }

    private static void AssertOutput(Fixture f, PrologueAudioCue cue, string path, string context)
    {
        TestAssert.Equal(cue, f.Audio.LastCue, context + " selects the agreed existing enemy cue");
        TestAssert.Equal(path, f.Audio.LastPlayedPath, context + " selects its real manifest path");
        PrologueAudioManifest manifest = PrologueAudioManifest.Load(PrologueAudioDirector.ManifestPath);
        TestAssert.True(manifest.Cues[cue.ToString()].Contains(path), context + " path is declared in the real sound bank");
        TestAssert.True(f.Audio.GetChildren().OfType<AudioStreamPlayer>().Any(player =>
                player.Bus == "SFX" && player.Playing && player.Stream?.ResourcePath == path),
            context + " reaches an actual playing SFX stream, not only LastCue bookkeeping");
        TestAssert.True(cue is not PrologueAudioCue.PlayerAttack and not PrologueAudioCue.PlayerHurt,
            context + " is never a hostile-to-player fallback");
    }

    private static MethodInfo ShotApi()
    {
        MethodInfo method = typeof(PrologueAudioDirector).GetMethod("TryPlayRangedShot",
            BindingFlags.Public | BindingFlags.Instance, null,
            new[] { typeof(RangedWeaponEnemy), typeof(ulong), typeof(Vector2) }, null);
        TestAssert.True(method != null && method.ReturnType == typeof(bool),
            "RANGED_AUDIO_ACTUAL_SHOT_API_RED: guarded bool TryPlayRangedShot(RangedWeaponEnemy, ulong, Vector2) boundary is required");
        return method;
    }

    private static bool TryShot(MethodInfo api, Fixture f, ShotObservation shot) =>
        (bool)api.Invoke(f.Audio, new object[] { f.Actor, shot.Ordinal, shot.Origin });

    private static async Task Until(SceneTree tree, Func<bool> condition, int budget, string context)
    {
        for (int index = 0; index < budget && !condition(); index++) await Frames(tree, 1);
        TestAssert.True(condition(), "fixture/native timing precondition: " + context);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private static void CollectShapes(Node node, HashSet<Resource> resources)
    {
        if (node is CollisionShape2D collider && collider.Shape != null) resources.Add(collider.Shape);
        foreach (Node child in node.GetChildren()) CollectShapes(child, resources);
    }

    private static async Task Dispose(SceneTree tree, Fixture f)
    {
        tree.Paused = false;
        if (GodotObject.IsInstanceValid(f.Root) && !f.Root.IsQueuedForDeletion()) f.Root.QueueFree();
        await Frames(tree, 3);
        foreach (Resource resource in f.Resources) if (GodotObject.IsInstanceValid(resource)) resource.Dispose();
        f.Scene.Dispose();
    }

    private sealed record ShotObservation(ulong Ordinal, Vector2 Origin, int PlaybackCount,
        PrologueAudioCue Cue, string Path, Vector2 AudioOrigin);
    private sealed record Fixture(Node2D Root, Node2D Envelope, Node2D Branch,
        RangedWeaponEnemy Actor, CharacterBody2D Player, PrologueAudioDirector Audio,
        PackedScene Scene, HashSet<Resource> Resources)
    {
        public List<AttackPresentationFrame> Frames { get; } = new();
        public List<ShotObservation> Shots { get; } = new();
    }
}
