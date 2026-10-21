using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

/// <summary>Actual generated leaves in the saved formal collector. Controlled poses, not traversal or pixel acceptance.</summary>
public static class FogRangedSavedControllerRuntimeTests
{
    private static readonly Vector2 PlayerPosition = new(180f, 570f);
    private static readonly Vector2 GunnerPosition = new(440f, 607f);
    private static readonly Rect2 Arena = new(0f, 0f, 1280f, 720f);

    public static async Task Run(SceneTree tree)
    {
        await ScatterRecoveryKeepsEveryActualTail(tree);
        foreach (RangedWeaponRole role in Enum.GetValues<RangedWeaponRole>())
            await GeneratedRoleWarningAndAttack(tree, role);
        await MountedNeighborAndWrongContainer(tree);
        await QueuedOwnerIsRejectedBeforeFlush(tree);
        await StreamAndTitleWithdrawActualSources(tree);
        GD.Print("FOG_RANGED_SAVED_CONTROLLER_OK roles=4 source_scenes=generated native_physics=true formal_visible=true pixels=false traversal=false");
    }

    // Break caught: admitting only the gunner's short Active warning (or excluding
    // its class entirely) drops five still-dangerous owned leaves in Recovery.
    private static async Task ScatterRecoveryKeepsEveryActualTail(SceneTree tree)
    {
        await using var f = await Fixture.Create(tree, "fog-ranged-saved-scatter-tail");
        RangedWeaponEnemy actor = f.AddActor(RangedWeaponRole.Scatter);
        await f.Until(() => actor.CurrentFrame.State == RangedWeaponState.Recovery, 120);
        List<AttackTelegraphVisual> tails = Owned(actor);
        f.AssertFormalSource(actor, tails);
        TestAssert.True(actor.CurrentFrame.State == RangedWeaponState.Recovery && tails.Count == 5 &&
            tails.All(leaf => leaf.CurrentFrame.Timeline.Phase == AttackPhase.Active &&
                leaf.CurrentFrame.Shape.Kind == AttackShapeKind.Circle &&
                leaf.CurrentFrame.Shape.Extent == new Vector2(6f, 6f) &&
                leaf.GetParent() is RangedWeaponProjectile { IsRetired: false }),
            "real saved Scatter has five physical six-pixel tails after its own Active phase, before checking the collector");
        TestAssert.Equal(5, Copies(f.Cues, EnemyAttackKind.RivetScatterShot).Count,
            "FOG_RANGED_SAVED_RECOVERY_TAILS_RED: saved formal fog must retain all five actual Scatter Recovery tail copies");
        EqualCopies(tails, Copies(f.Cues, EnemyAttackKind.RivetScatterShot), "same Recovery volley");

        var pause = f.Level.GetNode<ProloguePauseMenu>("PauseMenu");
        var positions = tails.Select(leaf => leaf.GlobalPosition).ToArray();
        pause.Open();
        await f.Frames(2);
        TestAssert.True(tree.Paused && f.Cues.PresentedCueCount == 0 && !f.Canvas.Visible,
            "real pause withdraws protected tails and formal fog together");
        for (int index = 0; index < tails.Count; index++)
            TestAssert.Equal(positions[index], tails[index].GlobalPosition, "pause freezes each actual owned tail");
        pause.Execute(ProloguePauseCommand.Resume);
        f.FixPlayer();
        await f.Frames(1);
        tails = Owned(actor);
        f.AssertFormalSource(actor, tails);
        EqualCopies(tails, Copies(f.Cues, EnemyAttackKind.RivetScatterShot), "resume rebuilds current tails");
        actor.ApplyThreadPull(Vector2.Left * 540f);
        await f.Frames(1);
        TestAssert.True(Owned(actor).Count == 0 && Copies(f.Cues, EnemyAttackKind.RivetScatterShot).Count == 0,
            "actual thread interruption immediately removes every protected Recovery tail");
        GD.Print("FOG_RANGED_SAVED_SCATTER_RECOVERY_PAUSE_THREAD_OK");
    }

    // Break caught: canonical-name scanning instead of the owned reference, a
    // stale frame copy, or a role-specific omission in the shared collector.
    private static async Task GeneratedRoleWarningAndAttack(SceneTree tree, RangedWeaponRole role)
    {
        await using var f = await Fixture.Create(tree, "fog-ranged-saved-role-" + role);
        RangedWeaponEnemy actor = f.AddActor(role);
        EnemyAttackKind kind = Kind(role);
        AttackTelegraphVisual warning = actor.GetNode<AttackTelegraphVisual>("VisualRoot/AttackTelegraph");
        await f.Until(() => warning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph &&
            warning.CurrentFrame.Timeline.HasLockedTarget, 110);
        TestAssert.True(warning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph &&
            warning.CurrentFrame.Timeline.HasLockedTarget && warning.CurrentFrame.Timeline.Kind == kind,
            "generated " + role + " naturally locks its actual warning against the real saved player");
        f.AssertFormalSource(actor, Owned(actor));
        EqualCopies(Owned(actor), Copies(f.Cues, kind), role + " locked warning");
        if (role == RangedWeaponRole.Scatter)
            TestAssert.True(warning.CurrentFrame.Shape.Kind == AttackShapeKind.ConvexPolygon &&
                warning.CurrentFrame.Shape.Points.Count == 3, "real Scatter warning is its world-space three-vertex fan");
        else
            TestAssert.Equal(AttackShapeKind.Capsule, warning.CurrentFrame.Shape.Kind,
                "real " + role + " warning uses its actual clipped width, not a fabricated rectangle");

        warning.Name = "RenamedButStillOwnedWarning";
        await f.Frames(1);
        EqualCopies(Owned(actor), Copies(f.Cues, kind), role + " renamed owned warning remains authorized");
        // Displace only the fixture target after the actual lock to avoid damage;
        // neither the model, emitter nor its presentation frame is called by tests.
        f.Level.Player.GlobalPosition = PlayerPosition + Vector2.Up * 50f;
        await f.Until(() => actor.CurrentFrame.State == (role == RangedWeaponRole.Laser
            ? RangedWeaponState.Active : RangedWeaponState.Recovery), 100);
        List<AttackTelegraphVisual> leaves = Owned(actor);
        f.AssertFormalSource(actor, leaves);
        int expected = role switch { RangedWeaponRole.Scatter => 5, RangedWeaponRole.Burst => 3, _ => 1 };
        TestAssert.Equal(expected, leaves.Count, "actual " + role + " independent owned Active/tail leaf count");
        TestAssert.True(leaves.All(leaf => leaf.CurrentFrame.Timeline.Phase == AttackPhase.Active),
            "all authorized " + role + " leaves describe current physical attacks");
        EqualCopies(leaves, Copies(f.Cues, kind), role + " current physical attack");
        actor.ResetForCheckpoint();
        actor.SetPhysicsProcess(false);
        await f.Frames(1);
        TestAssert.True(Owned(actor).Count == 0 && Copies(f.Cues, kind).Count == 0,
            "actual checkpoint reset withdraws " + role + " old warning/beam/tails without stale protected frames");
        actor.SetPhysicsProcess(true);
        f.FixPlayer();
        await f.Until(() => warning.CurrentFrame.Timeline.Phase == AttackPhase.Telegraph, 20);
        EqualCopies(Owned(actor), Copies(f.Cues, kind), role + " new warning after checkpoint reset");
        TestAssert.True(actor.GetNode<HealthComponent>("Health").ApplyDamage(
            new DamageRequest(2, Vector2.Left, 0f, 900UL, 5000UL)), "fixture's actual receiver kills " + role);
        await f.Frames(1);
        TestAssert.True(actor.CurrentFrame.State == RangedWeaponState.Dead && Owned(actor).Count == 0 &&
            Copies(f.Cues, kind).Count == 0, "actual owner death revokes " + role + " fog authority");
        GD.Print("FOG_RANGED_SAVED_ROLE_OK role=" + role);
    }

    // Break caught: current-sector-only collection or scanning every global leaf.
    // The neighbor is really mounted; only fixture pose is controlled, not route completion.
    private static async Task MountedNeighborAndWrongContainer(SceneTree tree)
    {
        await using var f = await Fixture.Create(tree, "fog-ranged-saved-mounted-neighbor");
        TestAssert.True(f.World.TryGetMountedSector(PrologueSectorId.CanyonDescent, out PrologueSector neighbor) &&
            neighbor != f.Level.CurrentSectorRoot, "real Entrance streaming has a distinct mounted Descent neighbor");
        RangedWeaponEnemy actor = f.AddActor(RangedWeaponRole.Laser, neighbor.GetNode<Node2D>("Gameplay/Actors"));
        await f.Until(() => Owned(actor).Count == 1, 20);
        f.AssertFormalSource(actor, Owned(actor));
        TestAssert.True(actor.GetParent() != f.Level.CurrentSectorRoot.GetNode("Gameplay/Actors") &&
            f.Level.CurrentSector == PrologueSectorId.CanyonEntrance,
            "actual source belongs to mounted neighbor while current sector remains Entrance");
        EqualCopies(Owned(actor), Copies(f.Cues, EnemyAttackKind.PrismLaserBeam), "mounted neighbor source");
        actor.ResetForCheckpoint(); actor.SetPhysicsProcess(false);
        RangedWeaponEnemy misplaced = f.AddActor(RangedWeaponRole.Burst,
            f.Level.CurrentSectorRoot.GetNode<Node2D>("Gameplay"));
        await f.Until(() => Owned(misplaced).Count == 1, 20);
        List<AttackTelegraphVisual> actual = Owned(misplaced);
        f.AssertFormalSource(misplaced, actual);
        TestAssert.True(actual.Count == 1 && misplaced.GetParent().Name == "Gameplay",
            "wrong-container negative control has a real timed visible warning, not an absent or default source");
        TestAssert.Equal(0, Copies(f.Cues, EnemyAttackKind.BeltBurstShot).Count,
            "real live wrong-container gunner cannot authorize protected fog copies through global discovery");
        GD.Print("FOG_RANGED_SAVED_NEIGHBOR_WRONG_CONTAINER_OK");
    }

    // Break caught: checking only the leaf's QueueFree state retains an attack
    // after its owner is queued but before Godot flushes descendants.
    private static async Task QueuedOwnerIsRejectedBeforeFlush(SceneTree tree)
    {
        await using var f = await Fixture.Create(tree, "fog-ranged-saved-queued-owner");
        RangedWeaponEnemy actor = f.AddActor(RangedWeaponRole.Scatter);
        await f.Until(() => actor.CurrentFrame.State == RangedWeaponState.Recovery, 120);
        List<AttackTelegraphVisual> tails = Owned(actor);
        f.AssertFormalSource(actor, tails);
        EqualCopies(tails, Copies(f.Cues, EnemyAttackKind.RivetScatterShot), "before queued owner boundary");
        HeadlampOcclusionFrame snapshot = f.Controller.LastOcclusionFrame;
        actor.QueueFree();
        TestAssert.True(actor.IsQueuedForDeletion() && tails.Count == 5 && tails.All(leaf =>
            GodotObject.IsInstanceValid(leaf) && leaf.IsInsideTree() && !leaf.IsQueuedForDeletion() &&
            leaf.CurrentFrame.Timeline.Phase == AttackPhase.Active),
            "preflush keeps five original active leaf references while only their ancestor is queued");
        f.Cues.PresentAttacks(tails, f.Level.Player.GlobalPosition, snapshot);
        TestAssert.Equal(0, Copies(f.Cues, EnemyAttackKind.RivetScatterShot).Count,
            "same fresh native frame rejects real queued-ancestor leaves before the deferred deletion flush");
        await f.Frames(2);
        TestAssert.Equal(0, Copies(f.Cues, EnemyAttackKind.RivetScatterShot).Count,
            "saved collector cannot renew the flushed owner's tail copies");
        GD.Print("FOG_RANGED_SAVED_QUEUED_ANCESTOR_PREFLUSH_OK");
    }

    // Break caught: cached sources survive sector retirement or Title despite a
    // still-valid source reference; controller must use fresh mounted authority.
    private static async Task StreamAndTitleWithdrawActualSources(SceneTree tree)
    {
        await using var f = await Fixture.Create(tree, "fog-ranged-saved-stream-title");
        RangedWeaponEnemy actor = f.AddActor(RangedWeaponRole.Laser);
        await f.Until(() => Owned(actor).Count == 1, 20);
        EqualCopies(Owned(actor), Copies(f.Cues, EnemyAttackKind.PrismLaserBeam), "before streaming retirement");
        ulong original = actor.GetInstanceId();
        TestAssert.True(f.World.RestoreCheckpoint("canyon.v2.basin-camp"),
            "real stream adapter accepts the authored remote Basin checkpoint, not an arbitrary Ascent pose");
        f.FixPlayer(keepPosition: true);
        await f.Frames(8);
        TestAssert.True(f.World.CurrentSector == PrologueSectorId.CanyonBasin &&
            !f.World.TryGetMountedSector(PrologueSectorId.CanyonEntrance, out _) &&
            (!GodotObject.IsInstanceValid(actor) || !actor.IsInsideTree()) &&
            Copies(f.Cues, EnemyAttackKind.PrismLaserBeam).Count == 0,
            "actual distant stream retirement removes the original gunner and protected source identity " + original);
        TestAssert.True(f.World.RestoreCheckpoint("canyon.v2.start"),
            "real stream adapter remounts Entrance for a new source identity");
        f.FixPlayer(); await f.Frames(8);
        RangedWeaponEnemy replacement = f.AddActor(RangedWeaponRole.Laser);
        await f.Until(() => Owned(replacement).Count == 1, 20);
        TestAssert.True(replacement.GetInstanceId() != original, "remount fixture uses a new actual generated leaf identity");
        EqualCopies(Owned(replacement), Copies(f.Cues, EnemyAttackKind.PrismLaserBeam), "new mounted source");
        f.Flow.ReturnToTitle(); await f.Frames(2);
        TestAssert.True(f.Flow.State == PrologueGameFlowState.Title && f.Cues.PresentedCueCount == 0 &&
            f.Cues.LastOcclusionFrame == null && !f.Canvas.Visible,
            "real ReturnToTitle withdraws all current ranged copies and formal fog together");
        GD.Print("FOG_RANGED_SAVED_STREAM_TITLE_OK");
    }

    internal static List<AttackTelegraphVisual> Owned(RangedWeaponEnemy actor)
    {
        var result = new List<AttackTelegraphVisual>();
        actor.AppendAuthorizedAttackLeaves(result);
        return result;
    }

    internal static List<AttackTelegraphVisual> Copies(FogReadabilityOverlay cues, EnemyAttackKind kind) =>
        cues.GetChildren().OfType<AttackTelegraphVisual>().Where(copy =>
            copy.CurrentFrame.Timeline.Phase != AttackPhase.None && copy.CurrentFrame.Timeline.Kind == kind).ToList();

    internal static void EqualCopies(IReadOnlyList<AttackTelegraphVisual> sources,
        IReadOnlyList<AttackTelegraphVisual> copies, string label)
    {
        TestAssert.True(sources.Count > 0, label + ": must have genuine live source leaves before comparing");
        TestAssert.Equal(sources.Count, copies.Count, label + ": exactly one current protected copy per actual source");
        var unmatched = copies.Select(copy => copy.CurrentFrame).ToList();
        foreach (AttackTelegraphVisual source in sources)
        {
            AttackPresentationFrame frame = source.CurrentFrame;
            int index = unmatched.FindIndex(copy => SameFrame(frame, copy));
            TestAssert.True(index >= 0, label + ": full timeline/shape/world-polygon values match the real leaf (not record reference equality)");
            unmatched.RemoveAt(index);
        }
    }

    internal static bool SameFrame(AttackPresentationFrame a, AttackPresentationFrame b)
    {
        if (a.Timeline != b.Timeline || a.Shape.Kind != b.Shape.Kind ||
            a.Shape.Origin != b.Shape.Origin || a.Shape.Extent != b.Shape.Extent ||
            a.Shape.RotationRadians != b.Shape.RotationRadians || a.Shape.Points.Count != b.Shape.Points.Count) return false;
        for (int index = 0; index < a.Shape.Points.Count; index++)
            if (a.Shape.Points[index] != b.Shape.Points[index]) return false;
        return true;
    }

    internal static EnemyAttackKind Kind(RangedWeaponRole role) => role switch
    {
        RangedWeaponRole.Scatter => EnemyAttackKind.RivetScatterShot,
        RangedWeaponRole.Burst => EnemyAttackKind.BeltBurstShot,
        RangedWeaponRole.Missile => EnemyAttackKind.FurnaceMissileShot,
        RangedWeaponRole.Laser => EnemyAttackKind.PrismLaserBeam,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SceneTree _tree;
        private readonly List<RangedWeaponEnemy> _actors = new();
        private readonly List<PackedScene> _scenes = new();
        private readonly HashSet<Resource> _resources = new();
        public PrologueLevelController Level { get; }
        public PrologueGameFlowController Flow => Level.GetNode<PrologueGameFlowController>("GameFlow");
        public SeamlessPrologueWorld World => Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        public PrologueFogController Controller => Level.GetNode<PrologueFogController>("FogController");
        public PrologueFogCanvas Canvas => Level.GetNode<PrologueFogCanvas>("Fog/Canvas");
        public FogReadabilityOverlay Cues => Level.GetNode<FogReadabilityOverlay>("ProtectedCues/Attacks");

        private Fixture(SceneTree tree, PrologueLevelController level) { _tree = tree; Level = level; }
        public static async Task<Fixture> Create(SceneTree tree, string name)
        {
            var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            var f = new Fixture(tree, level);
            try
            {
                IsolatedPrologueFixture.Configure(level, name);
                f.Flow.HeadlessAutoStart = false; f.Flow.AutomationAutoStart = false;
                tree.Root.AddChild(level);
                await f.Frames(3);
                TestAssert.Equal(PrologueGameFlowState.Title, f.Flow.State, "actual isolated saved fixture begins at Title");
                f.Flow.StartNew(); await f.Frames(3);
                f.FixPlayer(); await f.Frames(8);
                TestAssert.True(f.Flow.State == PrologueGameFlowState.Playing && f.Canvas.Visible &&
                    f.Controller.LastOcclusionFrame?.IsComplete == true && f.Cues.GetParent() is CanvasLayer
                    { Layer: 2, FollowViewportEnabled: true, FollowViewportScale: 1f },
                    "saved formal fog and original layer2 consumer are actually live before mounting generated ranged sources");
                return f;
            }
            catch { await f.DisposeAsync(); throw; }
        }

        public RangedWeaponEnemy AddActor(RangedWeaponRole role, Node2D container = null)
        {
            container ??= Level.CurrentSectorRoot.GetNode<Node2D>("Gameplay/Actors");
            string sceneName = role switch { RangedWeaponRole.Scatter => nameof(RivetScatterGunner),
                RangedWeaponRole.Burst => nameof(BeltBurstGunner), RangedWeaponRole.Missile => nameof(FurnaceMissileGunner),
                _ => nameof(PrismLaserGunner) };
            string path = $"res://scenes/enemies/{sceneName}.tscn";
            PackedScene scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            TestAssert.True(scene != null, "generated ranged scene must actually exist: " + path);
            _scenes.Add(scene);
            RangedWeaponEnemy actor = scene.Instantiate<RangedWeaponEnemy>();
            _actors.Add(actor);
            _resources.Add(actor.Settings);
            CollectShapes(actor, _resources);
            TestAssert.True(actor.SceneFilePath == path && actor.Role == role && actor.Settings.Role == role,
                "actual cache-ignored generated packed role/settings identity: " + role);
            actor.Name = "SavedFog" + sceneName;
            actor.Position = container.ToLocal(GunnerPosition);
            actor.ConfigureEncounterEnvelope(Arena);
            container.AddChild(actor);
            return actor;
        }

        public void FixPlayer(bool keepPosition = false)
        {
            Level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            if (!keepPosition) Level.Player.GlobalPosition = PlayerPosition;
            Level.Player.Velocity = Vector2.Zero;
            Level.Player.GetNode<Camera2D>("Camera2D").ResetSmoothing();
        }

        public void AssertFormalSource(RangedWeaponEnemy actor, IReadOnlyList<AttackTelegraphVisual> leaves)
        {
            TestAssert.True(GodotObject.IsInstanceValid(actor) && actor.IsInsideTree() && leaves.Count > 0 &&
                Canvas.Visible && Controller.LastOcclusionFrame?.IsComplete == true &&
                ReferenceEquals(Cues.LastOcclusionFrame, Controller.LastOcclusionFrame) &&
                Level.Player.GetNode<HealthComponent>("Health").CurrentHealth > 0,
                "actual mounted ranged source, living saved player, visible formal fog and same complete native display snapshot are prerequisites");
            foreach (AttackTelegraphVisual leaf in leaves)
                TestAssert.True(leaf.IsInsideTree() && actor.IsAncestorOf(leaf) && leaf.IsVisibleInTree() &&
                    leaf.GetWorld2D().GetRid() == Cues.GetWorld2D().GetRid() && leaf.GetViewport() == Cues.GetViewport() &&
                    Cues.GetViewport().GetVisibleRect().HasPoint(leaf.GetGlobalTransformWithCanvas().Origin) &&
                    Level.Player.GlobalPosition.DistanceTo(leaf.GlobalPosition) <= 620f &&
                    Controller.LastOcclusionFrame.CanSee(Level.Player.GlobalPosition, leaf.GlobalPosition),
                    "every actual owned leaf independently passes real viewport/world/distance/native-LOS eligibility before asserting protected collection");
        }

        public async Task Until(Func<bool> predicate, int limit)
        { for (int tick = 0; tick < limit && !predicate(); tick++) await Frames(1); }
        public async Task Frames(int count)
        {
            for (int tick = 0; tick < count; tick++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _tree.Paused = false;
            if (GodotObject.IsInstanceValid(Level))
            {
                if (Level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(_tree, Level);
                else Level.Free();
            }
            foreach (RangedWeaponEnemy actor in _actors)
                if (GodotObject.IsInstanceValid(actor)) { actor.GetParent()?.RemoveChild(actor); actor.Free(); }
            foreach (Resource resource in _resources) if (GodotObject.IsInstanceValid(resource)) resource.Dispose();
            foreach (PackedScene scene in _scenes) scene.Dispose();
        }
    }

    internal static void CollectShapes(Node node, HashSet<Resource> resources)
    {
        if (node is CollisionShape2D { Shape: not null } shape) resources.Add(shape.Shape);
        foreach (Node child in node.GetChildren()) CollectShapes(child, resources);
    }
}
