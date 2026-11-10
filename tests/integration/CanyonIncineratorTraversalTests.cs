using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

// Saved main, detached controlled starts; not Title-to-annex or human input evidence.
// Breaks if the saved leaf is stale, either ordinary single-jump return is blocked,
// the bypass forces damage, or real pool damage fails the normal recovery chain.
public static class CanyonIncineratorTraversalTests
{
    public static async Task Run(SceneTree tree)
    {
        await RunBypass(tree);
        await RunDeath(tree);
        GD.Print("INCINERATOR_TRAVERSAL_OK saved_main=true continuous_upper_east=true west_return=true lethal_contact=true death_reset=1 fall_reset=0 controlled_start=true controlled_checkpoint=true natural_route=false human=false whole_goal=false");
    }

    public static async Task RunBypass(SceneTree tree)
    {
        AssertSavedLeaf();
        await WithFixture(tree, "upper-east-continuous", new(5020, 3563), async f =>
        {
            await f.Land("SteamRejoinTop");
            await f.Walk(5190); await f.Land("IncineratorUpperEntryA");
            await f.Walk(5310); await f.Land("IncineratorUpperEntryB");
            await f.Walk(5500); await f.Land("IncineratorSafeGallery");
            await f.Walk(6100); f.AssertSafe();
            // All three return platforms start6240; gallery ends6160. The80px
            // shaft is wide enough for the actual40px capsule, not a point oracle.
            await f.Walk(6200); await f.Land("IncineratorEastBank");
            f.AssertSafe();
            await f.SingleJump("IncineratorReturn1", 6290);
            await f.SingleJump("IncineratorReturn2", 6290);
            await f.SingleJump("IncineratorReturn3", 6290);
            await f.Walk(6110); await f.Land("IncineratorSafeGallery");
            await f.Walk(5500); f.AssertSafe();
            GD.Print("INCINERATOR_UPPER_EAST_RETURN_OK continuous=true east_bank=true single_jump_only=true engine_keyboard=true damage=0 recoveries=0 controlled_start=true natural_route=false human=false");
        });
        await WithFixture(tree, "west-service-return", new(5190, 3959), async f =>
        {
            await f.Land("IncineratorWestBank");
            await f.SingleJump("IncineratorWestReturn1", 5190);
            await f.SingleJump("IncineratorWestReturn2", 5190);
            await f.SingleJump("IncineratorWestReturn3", 5190);
            await f.SingleJump("IncineratorUpperEntryA", 5190);
            await f.SingleJump("SteamRejoinTop", 5090);
            f.AssertSafe();
            GD.Print("INCINERATOR_WEST_RETURN_OK separate_fixture=true single_jump_only=true steam_top=true engine_keyboard=true damage=0 recoveries=0 controlled_start=true natural_route=false human=false");
        });
        GD.Print("INCINERATOR_BYPASS_OK continuous_upper_east=true west_return=true single_jump_only=true controlled_start=true natural_route=false human=false whole_goal=false");
    }

    public static async Task RunDeath(SceneTree tree)
    {
        AssertSavedLeaf();
        await WithFixture(tree, "west-lip-death", new(5380, 3959), async f =>
        {
            await f.Land("IncineratorWestBank");
            TestAssert.True(f.World.TryActivateCheckpoint("canyon.v2.basin-camp"),
                "controlled checkpoint uses actual world commit, not a hand progress signal");
            TestAssert.Equal("canyon.v2.basin-camp", f.Level.Progress.RestoreCheckpoint().CheckpointId,
                "actual progress owns the controlled legal camp checkpoint");
            f.AssertCheckpointSave();
            int hp = f.Health.CurrentHealth;
            f.Keyboard.Command(InputActions.MoveRight, true);
            for (int frame = 0; frame < 240 && f.Deaths == 0; frame++) await f.Frame();
            f.Keyboard.ReleaseAll();
            TestAssert.Equal(1, f.Deaths, "walking across x5440 pool lip genuinely kills through Health.Died");
            await f.Until(() => f.Completions == 1 && f.Flow.State == PrologueGameFlowState.Playing &&
                f.Player.IsOnFloor(), 240, "normal death recovery completes on legal supported camp");
            await f.Frames(24);
            TestAssert.True(f.ObserverErrors.Count == 0 && f.Hits.Count == 1 &&
                f.Hits[0].Source == f.PoolId && f.Hits[0].Sequence != 0 &&
                f.Hits[0].Remaining == 0 && f.Hits[0].InPool &&
                f.Hits[0].Impulse == Vector2.Zero && hp == f.Health.MaxHealth,
                "one accepted full-health lethal hit names the actual saved pool source and live overlap");
            TestAssert.True(f.Events.Select(e => e.Kind).SequenceEqual(new[] { "damage", "died", "requested", "completed" }) &&
                f.Events[0].Hp == 0 && f.Events[1].Hp == 0 && f.Events[2].Hp == 0 &&
                f.Events[2].Reason == "DEATH RESET" && f.Events[3].Reason == "DEATH RESET" &&
                f.Events[3].Position.DistanceTo(new Vector2(1680, 3960)) < .1f,
                "real HP0/Died precedes exact one DEATH RESET request/completion at authored camp");
            f.AssertRecovered(); f.AssertCheckpointSave();
            float restoredX = f.Player.GlobalPosition.X;
            f.Keyboard.Command(InputActions.MoveRight, true);
            await f.Frames(10); f.Keyboard.ReleaseAll(); await f.Frames(12);
            TestAssert.True(f.Player.GlobalPosition.X > restoredX + 20 &&
                f.HasFloor("BasinCampFloor"), "fresh real D moves the same recovered actor on the safe camp floor");
            f.AssertRecovered();
            GD.Print("INCINERATOR_DEATH_RECOVERY_OK saved_main=true source_pool=true accepted_hit=1 hp0=true died=1 requested=1 completed=1 death_reset=1 fall_reset=0 same_player=true full_hp=true checkpoint=basin-camp movement_after=true engine_keyboard=true controlled_start=true controlled_checkpoint=true natural_route=false human=false whole_goal=false");
        });
    }

    private static void AssertSavedLeaf()
    {
        CanyonIncineratorLayoutTests.AssertSteamLandingRecoveryClearance();
        var source = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn");
        var basin = source.Instantiate<PrologueSector>();
        try
        {
            // Deliberately first: the old leaf must yield a valid missing-feature
            // RED before mounting a fixture, not a timeout or a guessed path error.
            TestAssert.True(basin.Bounds == new Rect2(0, 0, 5120, 720) &&
                basin.GetNodeOrNull<Area2D>("Gameplay/Mechanisms/IncineratorPool") != null,
                "INCINERATOR_SAVED_MAP_MISSING_FEATURE_RED: actual saved Basin must include expanded5120x720 bounds and pool");
            AssertGeometry(basin, new Vector2(1280, 3600), false);
        }
        finally { basin.Free(); }
    }

    private static void AssertGeometry(PrologueSector basin, Vector2 offset, bool mounted)
    {
        var expected = new (string Name, Rect2 Bounds, bool OneWay)[]
        {
            ("IncineratorUpperEntryA", new(5100,3676,160,24), true),
            ("IncineratorUpperEntryB", new(5240,3756,140,24), false),
            ("IncineratorSafeGallery", new(5360,3836,800,24), false),
            ("IncineratorWestBank", new(4680,3996,760,24), false),
            ("IncineratorEastBank", new(6160,3996,216,24), false),
            ("IncineratorContainmentFloor", new(5440,4296,720,8), false),
            ("IncineratorRoof", new(5360,3600,1040,40), false),
            ("IncineratorEastWall", new(6376,3600,24,720), false),
            ("IncineratorReturn1", new(6240,3916,136,16), true),
            ("IncineratorReturn2", new(6240,3836,136,16), true),
            ("IncineratorReturn3", new(6240,3756,136,16), true),
            ("IncineratorWestReturn1", new(5140,3916,100,16), true),
            ("IncineratorWestReturn2", new(5140,3836,100,16), true),
            ("IncineratorWestReturn3", new(5140,3756,100,16), true),
            ("SteamRejoinTop", new(4940,3600,180,40), false)
        };
        foreach (var row in expected)
        {
            var shape = basin.GetNode<CollisionShape2D>("Gameplay/Collision/" + row.Name + "/Collision");
            TestAssert.True(shape.Shape is RectangleShape2D && !shape.Disabled &&
                shape.OneWayCollision == row.OneWay, row.Name + " enabled saved shape/one-way contract");
            var size = ((RectangleShape2D)shape.Shape).Size;
            Vector2 center = mounted ? shape.GlobalPosition : shape.Position +
                ((Node2D)shape.GetParent()).Position + offset;
            TestAssert.True(center.DistanceTo(row.Bounds.GetCenter()) < .01f && size.DistanceTo(row.Bounds.Size) < .01f,
                row.Name + ": literal world bounds independent of generator/layout helpers");
        }
        var areaShape = basin.GetNode<CollisionShape2D>("Gameplay/Mechanisms/IncineratorPool/Collision");
        var pool = (Area2D)areaShape.GetParent();
        Vector2 areaCenter = mounted ? areaShape.GlobalPosition : pool.Position + areaShape.Position + offset;
        TestAssert.True(areaShape.Shape is RectangleShape2D rectangle && !areaShape.Disabled &&
            rectangle.Size == new Vector2(720,276) && areaCenter == new Vector2(5800,4158),
            "actual lethal area is5440,4020,720,276; large pool is not an80px FALL RESET gap");
    }

    private static async Task WithFixture(SceneTree tree, string name, Vector2 spawn, Func<Fixture, Task> run)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        string protectedBefore = ProtectedDefaults();
        Fixture f = null;
        try
        {
            f = new Fixture(tree, spawn);
            await f.Start();
            GD.Print($"INCINERATOR_CONTROLLED_FIXTURE case={name} spawn={spawn} controlled_start=true natural_route=false human=false");
            await run(f);
        }
        finally
        {
            try { if (f != null) await f.Dispose(); }
            finally { TestAssert.Equal(protectedBefore, ProtectedDefaults(), "default knight/playtests/logs inventories remain byte-identical"); }
        }
    }

    private sealed class Fixture
    {
        private readonly SceneTree _tree;
        internal readonly PrologueLevelController Level;
        internal readonly CharacterBody2D Player;
        internal readonly HealthComponent Health;
        internal readonly PlayerMotor Motor;
        internal readonly SeamlessPrologueWorld World;
        internal readonly PrologueGameFlowController Flow;
        internal readonly KeyboardRouteInputDriver Keyboard;
        internal readonly ulong PlayerId;
        internal Area2D Pool;
        internal ulong PoolId;
        internal int Deaths, Requests, Completions;
        internal readonly List<(string Kind, int Hp, Vector2 Position, string Reason)> Events = new();
        internal readonly List<(ulong Source, ulong Sequence, int Remaining, bool InPool, Vector2 Impulse, string SourcePath)> Hits = new();
        internal readonly List<string> ObserverErrors = new();
        private readonly Queue<string> _recentFrames = new();
        private readonly int _maxHp;
        private PrologueSector _basin;

        internal Fixture(SceneTree tree, Vector2 spawn)
        {
            _tree = tree;
            Level = BasinAscentCameraRuntimeTests.CreateLevelFixture(PrologueSectorId.CanyonBasin, spawn);
            Player = Level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            // Factory disables only input for stationary camera holds. Restore
            // actual PlayerInputReader BEFORE mount; never write Motor frames.
            Player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Inherit;
            PlayerId = Player.GetInstanceId();
            Health = Player.GetNode<HealthComponent>("Health");
            _maxHp = Health.MaxHealth;
            Motor = Player.GetNode<PlayerMotor>("Motor");
            World = Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            Flow = Level.GetNode<PrologueGameFlowController>("GameFlow");
            Keyboard = KeyboardRouteInputDriver.Begin(Player);
            Health.Damaged += Damaged; Health.Died += Died;
            Level.RecoveryRequested += Requested; Level.RecoveryCompleted += Completed;
        }

        internal async Task Start()
        {
            _tree.Root.AddChild(Level);
            await Frames(8);
            await BasinAscentCameraRuntimeTests.WaitForPlaying(_tree, Level);
            TestAssert.True(World.TryGetMountedSector(PrologueSectorId.CanyonBasin, out _basin), "real saved main mounts actual Basin");
            TestAssert.Equal(new Vector2(1280,3600), _basin.GlobalPosition, "old Basin offset remains unchanged");
            AssertGeometry(_basin, Vector2.Zero, true);
            Pool = _basin.GetNode<Area2D>("Gameplay/Mechanisms/IncineratorPool"); PoolId = Pool.GetInstanceId();
            var capsule = Player.GetNode<CollisionShape2D>("BodyCollider").Shape as CapsuleShape2D;
            TestAssert.True(capsule != null && capsule.Radius == 20 && capsule.Height == 72,
                "route uses unchanged full40x72 actual player capsule");
            TestAssert.True(Mathf.IsEqualApprox(Motor.MotionSettings.Gravity,1850) &&
                Mathf.IsEqualApprox(Motor.MotionSettings.JumpSpeed,590), "route retains original single-jump physics");
            AssertSafe();
        }

        internal async Task Frame()
        {
            await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
            await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            Keyboard.ObserveFrame();
            // Bounded values only: no retained native contact/actor wrappers and
            // no extra physics query or gameplay state change for diagnostics.
            _recentFrames.Enqueue($"physics={Engine.GetPhysicsFrames()} process={Engine.GetProcessFrames()} " +
                $"position={Player.GlobalPosition} velocity={Player.Velocity} floor={Player.IsOnFloor()} " +
                $"sector={World.CurrentSector} hp={Health.CurrentHealth}");
            if (_recentFrames.Count > 16) _recentFrames.Dequeue();
            TestAssert.True(Player.GlobalPosition.IsFinite() && Player.GetInstanceId() == PlayerId &&
                Health.MaxHealth == _maxHp && Motor.AcceptedDashCount == 0 && Keyboard.ClawEdges == 0,
                "bounded real-input route keeps finite same actor, original HP, no Shift/K assistance");
        }
        internal async Task Frames(int count) { for (int i = 0; i < count; i++) await Frame(); }
        internal async Task Until(Func<bool> done, int cap, string message)
        {
            for (int i = 0; i < cap && !done(); i++) await Frame();
            TestAssert.True(done(), message + $" position={Player.GlobalPosition} velocity={Player.Velocity} deaths={Deaths} requests={Requests} completed={Completions}");
        }
        private void Steer(float target)
        {
            // Brake with real key release. Do not modify body velocity/position.
            float projected = Player.GlobalPosition.X + Player.Velocity.X * Mathf.Abs(Player.Velocity.X) / (2 * 2800f);
            Keyboard.Command(InputActions.MoveRight, projected < target - 2);
            Keyboard.Command(InputActions.MoveLeft, projected > target + 2);
        }
        internal async Task Walk(float target)
        {
            for (int frame = 0; frame < 300; frame++)
            {
                Steer(target); await Frame(); AssertSafe();
                if (Mathf.Abs(Player.GlobalPosition.X-target) < 5 && Mathf.Abs(Player.Velocity.X) < 15)
                { Keyboard.ReleaseAll(); return; }
            }
            Keyboard.ReleaseAll(); TestAssert.True(false, $"bounded keyboard walk failed target={target} actual={Player.GlobalPosition}");
        }
        internal bool HasFloor(string name)
        {
            if (!_basin.IsInsideTree() || !Player.IsOnFloor()) return false;
            var support = _basin.GetNode<StaticBody2D>("Gameplay/Collision/"+name);
            for (int i=0;i<Player.GetSlideCollisionCount();i++)
            {
                using KinematicCollision2D contact = Player.GetSlideCollision(i);
                if (contact.GetCollider() == support && contact.GetNormal().Y < -.7f) return true;
            }
            return false;
        }
        internal async Task Land(string name) => await Until(() => HasFloor(name), 180, "native upward contact on "+name);
        internal async Task SingleJump(string support, float target)
        {
            TestAssert.True(Player.IsOnFloor(), "single Space is pressed only from real ground");
            ulong before = Motor.AcceptedJumpSequence;
            Steer(target); Keyboard.Command(InputActions.Jump,true); await Frames(1);
            bool airborne = !Player.IsOnFloor();
            bool released = false;
            for (int frame=0;frame<180;frame++)
            {
                Steer(target); await Frame(); AssertSafe(); airborne |= !Player.IsOnFloor();
                // Releasing Space after one tick invokes the real jump-cut and
                // cannot climb80px. Hold this ONE edge until the natural apex;
                // no second press or ordinary air-jump is requested.
                if (!released && airborne && Player.Velocity.Y >= 0)
                { Keyboard.Command(InputActions.Jump,false); released = true; }
                if (airborne && HasFloor(support))
                {
                    Keyboard.ReleaseAll(); await Frames(3);
                    TestAssert.True(Motor.AcceptedJumpSequence == before+1 && HasFloor(support),
                        "one accepted ground jump alone reaches "+support);
                    GD.Print($"INCINERATOR_SINGLE_JUMP support={support} accepted=1 position={Player.GlobalPosition} dash=0 claw=0");
                    return;
                }
            }
            Keyboard.ReleaseAll(); TestAssert.True(false,$"single-jump return blocked support={support} position={Player.GlobalPosition}");
        }
        internal void AssertSafe()
        {
            bool safe = Health.CurrentHealth == _maxHp &&
                Deaths==0 && Requests==0 && Completions==0 && Hits.Count==0;
            TestAssert.True(safe, "safe bypass/return must not use damage, death or recovery as travel" +
                (safe ? "" : SafetyDiagnostics()));
        }
        private string SafetyDiagnostics() =>
            $" hp={Health.CurrentHealth}/{_maxHp} deaths={Deaths} requests={Requests} completions={Completions} " +
            $"hits={Hits.Count} events={Events.Count} observer_errors={ObserverErrors.Count} " +
            $"hit_values=[{string.Join(" | ", Hits.TakeLast(16).Select(hit =>
                $"source={hit.Source}/{hit.Sequence} source_path_at_damage={hit.SourcePath} remaining={hit.Remaining} in_pool={hit.InPool} impulse={hit.Impulse}"))}] " +
            $"event_values=[{string.Join(" | ", Events.TakeLast(16).Select(e =>
                $"kind={e.Kind} hp={e.Hp} position={e.Position} reason={e.Reason}"))}] " +
            $"observer_values=[{string.Join(" | ", ObserverErrors.TakeLast(16))}] " +
            $"recent_frames=[{string.Join(" | ", _recentFrames)}]";
        internal void AssertRecovered() => TestAssert.True(Deaths==1 && Requests==1 && Completions==1 &&
            Health.CurrentHealth==_maxHp && Player.GetInstanceId()==PlayerId && Level.Player==Player &&
            Level.GetNode("PlayerRoot").GetChildCount()==1 && Flow.State==PrologueGameFlowState.Playing &&
            !_tree.Paused && Player.IsPhysicsProcessing() &&
            World.CheckpointId=="canyon.v2.basin-camp" &&
            Level.Progress.RestoreCheckpoint().CheckpointId=="canyon.v2.basin-camp",
            "exact one real death restores same full-health playable actor, no FALL RESET or duplicate death");
        internal void AssertCheckpointSave()
        {
            using JsonDocument disk = JsonDocument.Parse(File.ReadAllText(Flow.SavePath));
            TestAssert.True(disk.RootElement.GetProperty("checkpointId").GetString()=="canyon.v2.basin-camp" &&
                disk.RootElement.GetProperty("hasActivatedCheckpoint").GetBoolean(),
                "isolated real disk save records activated camp checkpoint, not only transient progress");
        }
        private void Record(string kind,string reason="") => Events.Add((kind,Health.CurrentHealth,Player.GlobalPosition,reason));
        private void Damaged(int remaining,Vector2 impulse)
        {
            try
            {
                var field = typeof(HealthComponent).GetField("_processedAttackOrder",BindingFlags.NonPublic|BindingFlags.Instance);
                TestAssert.True(field?.GetValue(Health) is Queue<DamageAttackIdentity>, "real Health accepted identity is available read-only");
                var identity = ((Queue<DamageAttackIdentity>)field.GetValue(Health)).Last();
                string sourcePath = SourcePathAtDamage(identity.SourceRuntimeId);
                bool inPool = GodotObject.IsInstanceValid(Pool) && Pool.IsInsideTree() && Pool.GetOverlappingBodies().Contains(Player);
                Hits.Add((identity.SourceRuntimeId,identity.LocalAttackId,remaining,inPool,impulse,sourcePath)); Record("damage");
            }
            catch(Exception error) { ObserverErrors.Add(error.ToString()); }
        }
        private static string SourcePathAtDamage(ulong sourceId)
        {
            // Resolve only at the accepted event, never later from a retired ID.
            // Keep only the string; diagnostics must not own/dispose the source.
            if (sourceId == 0) return "<source-id-zero>";
            try
            {
                var source = GodotObject.InstanceFromId(sourceId) as Node;
                return GodotObject.IsInstanceValid(source)
                    ? source.IsInsideTree() ? source.GetPath().ToString() : "<detached:" + source.Name + ">"
                    : "<not-a-live-node>";
            }
            catch (Exception error) { return "<source-path-error:" + error.Message + ">"; }
        }
        private void Died() { Deaths++; Record("died"); Keyboard.ReleaseAll(); }
        private void Requested(string reason) { Requests++; Record("requested",reason); }
        private void Completed(string reason) { Completions++; Record("completed",reason); }
        internal async Task Dispose()
        {
            try
            {
                Keyboard.Dispose();
                Health.Damaged -= Damaged; Health.Died -= Died;
                Level.RecoveryRequested -= Requested; Level.RecoveryCompleted -= Completed;
            }
            finally
            {
                if(GodotObject.IsInstanceValid(Level))
                {
                    if(Level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree,Level);
                    else Level.Free();
                }
            }
        }
    }

    private static string ProtectedDefaults()
    {
        var rows = new List<string>();
        void Scan(string path,string label)
        {
            FileAttributes flags=File.GetAttributes(path);
            if((flags&FileAttributes.ReparsePoint)!=0) { rows.Add(label+"|reparse"); return; }
            if((flags&FileAttributes.Directory)!=0)
            {
                rows.Add(label+"|directory");
                foreach(string child in Directory.EnumerateFileSystemEntries(path).OrderBy(x=>x,StringComparer.Ordinal))
                    Scan(child,label+"/"+Path.GetFileName(child));
                return;
            }
            using FileStream stream=File.Open(path,FileMode.Open,System.IO.FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
            rows.Add(label+"|"+stream.Length+"|"+Convert.ToHexString(SHA256.HashData(stream)));
        }
        foreach(string name in new[]{"knight","playtests","logs"})
        {
            string path=Path.Combine(OS.GetUserDataDir(),name);
            if(Directory.Exists(path)||File.Exists(path)) Scan(path,name); else rows.Add(name+"|missing");
        }
        return string.Join("\n",rows);
    }
}
