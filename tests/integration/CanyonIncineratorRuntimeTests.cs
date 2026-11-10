using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Shared;

namespace Knight.Tests.Integration;

// Component/native-contact proof only. The saved-map Title/route/DEATH RESET proof is separate.
public static class CanyonIncineratorRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        Type type = typeof(HealthComponent).Assembly.GetType("Knight.Mechanisms.CanyonIncinerator");
        TestAssert.True(type != null && typeof(Area2D).IsAssignableFrom(type),
            "INCINERATOR_MISSING_FEATURE_RED: Knight.Mechanisms.CanyonIncinerator must be an actual native Area2D");
        Fixture f = null;
        try
        {
            f = await Fixture.Create(tree, type);
            await LethalNativeContactAndReceiverDedup(tree, f);
            await ExistingIframeRequiresLaterDistinctPulse(tree, f);
            await EscapeDuringIframeHasNoDamageDebt(tree, f);
            await SynchronousResetAndRelocationCannotKillOldOverlap(tree, f);
            await ActiveShieldDoesNotBlockExplicitUnblockableTerrain(tree, f);
            await RevokedLifecycleAndGeometryCannotDeliver(tree, f);
            await OtherWorldAndNonPlayerCannotDeliver(tree, f);
            await QueuedAncestorRevokesBeforeFlush(tree, f);
            GD.Print("INCINERATOR_NATIVE_OK lethal=true iframe=true escaped=true reentry=true shield=true lifecycle=true geometry=true same_player=true full_map=false human=false whole_goal=false");
        }
        finally
        {
            Input.ActionRelease(InputActions.Shield);
            tree.Paused = false;
            if (f != null) await f.Dispose(tree);
        }
    }

    // Break: missing damage, bypassed GuardedDamage, duplicate shape attempts, or repeated corpse hits.
    private static async Task LethalNativeContactAndReceiverDedup(SceneTree tree, Fixture f)
    {
        TestAssert.Equal(new Rect2(-360, -138, 720, 276), f.Bounds, "full-sized local danger rectangle");
        TestAssert.Near(.1f, f.Pulse, .00001f, "literal persistent contact cadence");
        TestAssert.True(f.Trap is ICheckpointResettable && f.Trap is ICanyonTelegraphSource,
            "real checkpoint and warning consumers can discover the mechanism");
        await f.PrepareInside(tree);
        ulong before = f.Sequence;
        int deaths = f.Deaths, hits = f.Accepted.Count;
        f.Trap.SetPhysicsProcess(true);
        await Frames(tree, 3);
        TestAssert.True(f.Health.CurrentHealth == 0 && f.Deaths == deaths + 1 && f.Accepted.Count == hits + 1,
            "one real contact gives one accepted lethal hit and one actual Died; " + f.FailureSnapshot());
        TestAssert.Equal(before + 1UL, f.Sequence, "two real body colliders do not allocate two attempts");
        DamageAttackIdentity id = f.Accepted.Last();
        TestAssert.True(id.SourceRuntimeId == f.Trap.GetInstanceId() && id.LocalAttackId == f.Sequence,
            "accepted Health identity belongs to the live terrain source and exact attempt");
        TestAssert.True(f.Player.GetNode<Hurtbox>("Hurtbox").ResolveDamageReceiver() == f.Receiver,
            "saved Player keeps its authoritative GuardedDamage path");
        await Frames(tree, 18);
        TestAssert.True(f.Sequence == before + 1UL && f.Deaths == deaths + 1,
            "a corpse cannot consume more attempts or emit repeated death");
    }

    // Break: one-entry identity reused forever, or a terrain-specific iframe bypass.
    private static async Task ExistingIframeRequiresLaterDistinctPulse(SceneTree tree, Fixture f)
    {
        await f.PrepareInside(tree);
        f.InjureForIframe();
        ulong before = f.Sequence;
        int deaths = f.Deaths, accepted = f.Accepted.Count;
        f.Trap.SetPhysicsProcess(true);
        await Frames(tree, 4);
        TestAssert.True(f.Health.CurrentHealth == 4 && f.Deaths == deaths && f.Sequence == before + 1UL &&
            f.Accepted.Count == accepted, "existing actual Health iframe rejects first native terrain attempt");
        ulong rejected = f.Sequence;
        await Frames(tree, 32);
        TestAssert.True(f.Health.CurrentHealth == 0 && f.Deaths == deaths + 1 &&
            f.Accepted.Count == accepted + 1 && f.Sequence > rejected,
            "remaining in volume eventually accepts a new identity after ordinary iframe expiration; " + f.FailureSnapshot());
        TestAssert.True(f.Accepted.Last().SourceRuntimeId == f.Trap.GetInstanceId() &&
            f.Accepted.Last().LocalAttackId > rejected, "lethal acceptance is not replay of rejected identity");
    }

    // Break: delayed damage delivered from a cached entry after leaving the volume.
    private static async Task EscapeDuringIframeHasNoDamageDebt(SceneTree tree, Fixture f)
    {
        await f.PrepareInside(tree);
        f.InjureForIframe();
        int deaths = f.Deaths;
        f.Trap.SetPhysicsProcess(true);
        await Frames(tree, 4);
        TestAssert.Equal(4, f.Health.CurrentHealth, "escape starts after a real rejected terrain contact");
        ulong rejected = f.Sequence;
        f.Player.Position = new Vector2(1000, 0);
        // Deliberately before the physics server refreshes its overlap list.
        f.Trap._PhysicsProcess(.11d);
        await Frames(tree, 36);
        TestAssert.True(f.Health.CurrentHealth == 4 && f.Deaths == deaths && f.Sequence == rejected,
            "current collider geometry, not stale overlaps, authorizes retries");
    }

    // Break: reset reuses identities or a synchronous callback continues its old contact transaction.
    private static async Task SynchronousResetAndRelocationCannotKillOldOverlap(SceneTree tree, Fixture f)
    {
        await f.PrepareInside(tree);
        ulong before = f.Sequence, playerId = f.Player.GetInstanceId();
        int deaths = f.Deaths;
        void Relocate()
        {
            ((ICheckpointResettable)f.Trap).ResetForCheckpoint();
            f.Health.ResetFull();
            f.Player.Position = new Vector2(1000, 0);
        }
        f.Health.Died += Relocate;
        try
        {
            f.Trap.SetPhysicsProcess(true);
            await Frames(tree, 20);
            TestAssert.True(f.Health.CurrentHealth == 5 && f.Deaths == deaths + 1 && f.Sequence == before + 1UL,
                "synchronous reset and safety relocation revoke the old overlap transaction");
        }
        finally { f.Health.Died -= Relocate; }
        f.Player.Position = Vector2.Zero;
        await Frames(tree, 4);
        TestAssert.True(f.Health.CurrentHealth == 0 && f.Deaths == deaths + 2 && f.Sequence > before + 1UL &&
            f.Player.GetInstanceId() == playerId, "same living Player re-entry uses fresh monotonic source identity");
    }

    // Break: Normal/Heavy replaces the explicitly classified Unblockable terrain request.
    private static async Task ActiveShieldDoesNotBlockExplicitUnblockableTerrain(SceneTree tree, Fixture f)
    {
        await f.PrepareInside(tree);
        f.Energy.ResetFull(); f.Shield.ResetFull();
        Input.ActionPress(InputActions.Shield);
        await Frames(tree, 3);
        f.Player.Position = -f.Shield.GuardDirection * 80f;
        await Frames(tree, 3);
        TestAssert.True(f.Shield.IsActive, "real saved shield controller is sustained by real input/energy");
        TestAssert.True(f.Shield.GuardDirection.Dot(f.Player.Position.DirectionTo(Vector2.Zero)) > .99f,
            "shield actually faces the terrain source; a Normal directional request would be blocked");
        int deaths = f.Deaths;
        f.Trap.SetPhysicsProcess(true);
        await Frames(tree, 3);
        Input.ActionRelease(InputActions.Shield);
        TestAssert.True(f.Health.CurrentHealth == 0 && f.Deaths == deaths + 1 && !f.Shield.IsActive,
            "explicit Unblockable terrain reaches actual Health despite sustained shield and death closes shield");
    }

    // Break: hidden/staged/paused/disabled/changed physical geometry still authorizes damage.
    private static async Task RevokedLifecycleAndGeometryCannotDeliver(SceneTree tree, Fixture f)
    {
        await Rejected(tree, f, "staged ancestor", () => f.Owner.SetMeta("_seamless_staged", true),
            () => f.Owner.RemoveMeta("_seamless_staged"));
        await Rejected(tree, f, "hidden owner", () => f.Owner.Hide(), () => f.Owner.Show());
        await Rejected(tree, f, "disabled owner processing", () => f.Owner.ProcessMode = Node.ProcessModeEnum.Disabled,
            () => f.Owner.ProcessMode = Node.ProcessModeEnum.Inherit);
        await Rejected(tree, f, "player gameplay processing disabled", () => f.Player.SetPhysicsProcess(false),
            () => f.Player.SetPhysicsProcess(true));
        await Rejected(tree, f, "hidden player", () => f.Player.Hide(), () => f.Player.Show());
        await Rejected(tree, f, "disabled collision", () => f.Collision.Disabled = true, () => f.Collision.Disabled = false);
        await Rejected(tree, f, "disabled monitoring", () => f.Trap.Monitoring = false, () => f.Trap.Monitoring = true);
        await Rejected(tree, f, "wrong rectangle dimensions", () => f.Shape.Size = new Vector2(700, 276),
            () => f.Shape.Size = new Vector2(720, 276));
        await Rejected(tree, f, "offset collider", () => f.Collision.Position = new Vector2(8, 0),
            () => f.Collision.Position = Vector2.Zero);
        await Rejected(tree, f, "paused tree", () => tree.Paused = true, () => tree.Paused = false);
        var ownerHealth = new HealthComponent { Name = "Health" };
        f.Owner.AddChild(ownerHealth);
        try
        {
            await Rejected(tree, f, "dead source owner", () =>
                TestAssert.True(ownerHealth.ReceiveDamage(new DamageRequest(5, Vector2.Zero, 0, 1UL,
                    f.Owner.GetInstanceId())), "actual source-owner Health accepts lethal fixture injury"),
                () => ownerHealth.ResetFull());
        }
        finally { ownerHealth.Free(); }
        // AABB-only testing would accept this corner outside the actual round capsule.
        await f.PrepareInside(tree);
        f.Player.Position = new Vector2(379, 173);
        await Frames(tree, 3);
        ulong seq = f.Sequence;
        f.Trap.SetPhysicsProcess(true);
        await Frames(tree, 10);
        TestAssert.True(f.Health.CurrentHealth == 5 && f.Sequence == seq,
            "capsule AABB corner outside rectangle has no physical contact");
    }

    private static async Task Rejected(SceneTree tree, Fixture f, string label, Action revoke, Action restore)
    {
        await f.PrepareInside(tree);
        ulong seq = f.Sequence;
        int deaths = f.Deaths;
        f.Trap.SetPhysicsProcess(true);
        revoke();
        try
        {
            // This direct runtime callback probes the revocation before deferred native contact flush.
            f.Trap._PhysicsProcess(.2d);
            f.Trap.SetPhysicsProcess(true);
            await Frames(tree, 10);
            TestAssert.True(f.Health.CurrentHealth == 5 && f.Deaths == deaths && f.Sequence == seq,
                "revoked permission cannot allocate or deliver: " + label);
        }
        finally { f.Trap.SetPhysicsProcess(false); restore(); }
        await Frames(tree, 2);
    }

    // Break: player group alone authorizes stale contacts from another world or unrelated bodies.
    private static async Task OtherWorldAndNonPlayerCannotDeliver(SceneTree tree, Fixture f)
    {
        await f.PrepareInside(tree);
        f.Player.RemoveFromGroup("player");
        ulong seq = f.Sequence;
        f.Trap.SetPhysicsProcess(true);
        await Frames(tree, 12);
        TestAssert.True(f.Health.CurrentHealth == 5 && f.Sequence == seq, "nonplayer body is not a terrain target");
        f.Trap.SetPhysicsProcess(false);
        f.Player.AddToGroup("player");
        f.Player.Reparent(f.OtherViewport);
        f.Trap.SetPhysicsProcess(true);
        f.Trap._PhysicsProcess(.2d);
        TestAssert.True(f.Health.CurrentHealth == 5 && f.Sequence == seq,
            "stale native overlap from a different World2D cannot deliver");
        f.Player.Reparent(f.Viewport);
        f.Player.Position = new Vector2(1000, 0);
        await Frames(tree, 3);
    }

    // Break: queued-for-deletion ancestor remains authoritative until the deferred free.
    private static async Task QueuedAncestorRevokesBeforeFlush(SceneTree tree, Fixture f)
    {
        await f.PrepareInside(tree);
        ulong seq = f.Sequence;
        f.Trap.SetPhysicsProcess(true);
        f.Owner.QueueFree();
        f.Trap._PhysicsProcess(.2d);
        TestAssert.True(f.Health.CurrentHealth == 5 && f.Sequence == seq,
            "queued terrain ancestor revokes before deferred deletion");
        await Frames(tree, 2);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int i = 0; i < count; i++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
    }

    private sealed class Fixture
    {
        public SubViewport Viewport, OtherViewport;
        public Node2D Owner;
        public Area2D Trap;
        public CharacterBody2D Player;
        public CollisionShape2D Collision;
        public RectangleShape2D Shape;
        public CapsuleShape2D ExtraShape;
        public HealthComponent Health;
        public PlayerGuardedDamageReceiver Receiver;
        public PlayerShieldController Shield;
        public PlayerEnergyController Energy;
        public World2D World, OtherWorld;
        public int Deaths;
        public readonly List<DamageAttackIdentity> Accepted = new();
        private ulong _injury;
        public ulong Sequence => (ulong)Trap.GetType().GetProperty("AttemptSequence").GetValue(Trap);
        public Rect2 Bounds => (Rect2)Trap.GetType().GetProperty("DangerBounds").GetValue(Trap);
        public float Pulse => (float)Trap.GetType().GetProperty("PulseSeconds").GetValue(Trap);

        public static async Task<Fixture> Create(SceneTree tree, Type type)
        {
            var f = new Fixture();
            try
            {
                f.World = new World2D(); f.OtherWorld = new World2D();
                f.Viewport = new SubViewport { Name = "IncineratorNativeFixture", World2D = f.World };
                f.OtherViewport = new SubViewport { Name = "IncineratorOtherWorld", World2D = f.OtherWorld };
                f.Owner = new Node2D { Name = "TerrainLifecycle" };
                f.Trap = (Area2D)Activator.CreateInstance(type);
                f.Trap.Name = "ActualIncinerator";
                f.Trap.GetType().GetProperty("Size").SetValue(f.Trap, new Vector2(720, 276));
                f.Shape = new RectangleShape2D { Size = new Vector2(720, 276) };
                f.Collision = new CollisionShape2D { Name = "Collision", Shape = f.Shape };
                f.Trap.AddChild(f.Collision); f.Owner.AddChild(f.Trap); f.Viewport.AddChild(f.Owner);
                using PackedScene packed = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
                f.Player = packed.Instantiate<CharacterBody2D>();
                f.Player.Position = new Vector2(1000, 0);
                f.Player.AddToGroup("player");
                f.Player.GetNode<Camera2D>("Camera2D").Enabled = false;
                f.ExtraShape = new CapsuleShape2D { Radius = 20, Height = 72 };
                f.Player.AddChild(new CollisionShape2D { Name = "SecondBodyCollider", Shape = f.ExtraShape });
                f.Health = f.Player.GetNode<HealthComponent>("Health");
                f.Receiver = f.Player.GetNode<PlayerGuardedDamageReceiver>("GuardedDamage");
                f.Shield = f.Player.GetNode<PlayerShieldController>("Shield");
                f.Energy = f.Player.GetNode<PlayerEnergyController>("Energy");
                f.Health.Damaged += f.OnDamaged; f.Health.Died += f.OnDied;
                f.Viewport.AddChild(f.Player);
                tree.Root.AddChild(f.Viewport); tree.Root.AddChild(f.OtherViewport);
                // The real Level explicitly enables this saved bare CharacterBody root at gameplay
                // admission (PrologueLevelController.SetPlayerGameplayProcessing). This isolated
                // component fixture has no Level; preserve that same admission, not a weaker hazard gate.
                GD.Print($"INCINERATOR_FIXTURE_ADMISSION player_physics_before={f.Player.IsPhysicsProcessing()} " +
                    $"player_can={f.Player.CanProcess()} health_physics={f.Health.IsPhysicsProcessing()} " +
                    $"player_ready={f.Player.IsNodeReady()} health_ready={f.Health.IsNodeReady()} player_inside={f.Player.IsInsideTree()}");
                f.Player.SetPhysicsProcess(true);
                f.Trap.SetPhysicsProcess(false);
                await Frames(tree, 3);
                TestAssert.True(f.Player.IsNodeReady() && f.Health.IsNodeReady(),
                    "component fixture admits the real saved Player before locking its controlled position");
                // Godot auto-enables overridden callbacks before Ready. A
                // pre-Ready disable was overwritten, so gravity moved this
                // controlled component fixture outside the volume during iframe.
                f.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
                f.Trap.SetPhysicsProcess(false);
                GD.Print($"INCINERATOR_FIXTURE_POST_READY player_ready={f.Player.IsNodeReady()} health_ready={f.Health.IsNodeReady()} " +
                    $"player_physics={f.Player.IsPhysicsProcessing()} health_physics={f.Health.IsPhysicsProcessing()} " +
                    $"motor_physics={f.Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing()} player_pos={f.Player.Position}");
                return f;
            }
            catch { await f.Dispose(tree); throw; }
        }

        public async Task PrepareInside(SceneTree tree)
        {
            Trap.SetPhysicsProcess(false);
            Input.ActionRelease(InputActions.Shield);
            Health.ResetFull(); Receiver.ResetFull(); Shield.ResetFull(); Energy.ResetFull();
            ((ICheckpointResettable)Trap).ResetForCheckpoint();
            Player.Position = Vector2.Zero; Player.Velocity = Vector2.Zero;
            await Frames(tree, 3);
            Godot.Collections.Array<Node2D> bodies = Trap.GetOverlappingBodies();
            using Godot.Collections.Array backing = (Godot.Collections.Array)bodies;
            TestAssert.True(bodies.Contains(Player), "fixture has actual native player/rectangle overlap");
        }

        public void InjureForIframe()
        {
            TestAssert.True(Receiver.ReceiveDamage(new DamageRequest(1, Vector2.Zero, 0, ++_injury,
                Player.GetInstanceId(), DamageGuardClass.Unblockable)) && Health.CurrentHealth == 4,
                "real guarded receiver creates existing ordinary Health iframe");
        }

        private void OnDied() => Deaths++;

        public string FailureSnapshot()
        {
            object Field(object instance, string name) => instance.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);
            string Cooldowns()
            {
                var remaining = Field(Trap, "_remaining") as Dictionary<ulong, double>;
                return remaining == null ? "missing" : string.Join(",", remaining.Take(4).Select(p => $"{p.Key}:{p.Value:0.000000}"));
            }
            string Probe(string name, params object[] args)
            {
                try
                {
                    return Trap.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.Invoke(Trap, args)?.ToString() ?? "missing";
                }
                catch (Exception error) { return error.GetBaseException().GetType().Name; }
            }
            return $"INCINERATOR_STATE seq={Sequence} hp={Health.CurrentHealth} deaths={Deaths} accepted={Accepted.Count} " +
                $"iframe={Field(Health, "_invulnerabilityRemaining")} health_dead={Field(Health, "_isDead")} " +
                $"present_count={(Field(Trap, "_present") as HashSet<ulong>)?.Count} remaining=[{Cooldowns()}] " +
                $"owns={Probe("OwnsDanger")} geometry={Probe("GeometryMatches", false)} contact={Probe("CurrentBodyContact", Player)} " +
                $"player_can={Player.CanProcess()} player_physics={Player.IsPhysicsProcessing()} player_visible={Player.IsVisibleInTree()} " +
                $"owner_can={Owner.CanProcess()} owner_physics={Owner.IsPhysicsProcessing()} " +
                $"viewport_can={Viewport.CanProcess()} viewport_physics={Viewport.IsPhysicsProcessing()} " +
                $"receiver_can={Receiver.CanProcess()} receiver_physics={Receiver.IsPhysicsProcessing()} " +
                $"health_can={Health.CanProcess()} health_physics={Health.IsPhysicsProcessing()} " +
                $"player_ready={Player.IsNodeReady()} health_ready={Health.IsNodeReady()} " +
                $"motor_physics={Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing()} velocity={Player.Velocity} " +
                $"trap_can={Trap.CanProcess()} trap_physics={Trap.IsPhysicsProcessing()} " +
                $"monitoring={Trap.Monitoring} disabled={Collision.Disabled} player_pos={Player.Position}";
        }

        private void OnDamaged(int remaining, Vector2 impulse)
        {
            var ids = (Queue<DamageAttackIdentity>)typeof(HealthComponent).GetField("_processedAttackOrder",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Health);
            Accepted.Add(ids.Last());
        }

        public async Task Dispose(SceneTree tree)
        {
            if (GodotObject.IsInstanceValid(Health)) { Health.Damaged -= OnDamaged; Health.Died -= OnDied; }
            foreach (Node node in new Node[] { Viewport, OtherViewport })
                if (GodotObject.IsInstanceValid(node)) { if (node.IsInsideTree()) node.QueueFree(); else node.Free(); }
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            Shape?.Dispose(); ExtraShape?.Dispose(); World?.Dispose(); OtherWorld?.Dispose();
        }
    }
}
