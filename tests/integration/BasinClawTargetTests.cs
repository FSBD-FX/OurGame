using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// Saved V2 terrain + actual keyboard/controller/physics. Pre-tree staging is
// not a title-to-exit route, human input certificate or long-fight clearance.
public static class BasinClawTargetTests
{
    private const string WallPath = "Gameplay/Collision/GearPistonOne";

    public static async Task Run(SceneTree tree)
    {
        await RunSavedWall(tree, ice: false);
        await RunSavedWall(tree, ice: true);
        GD.Print("BASIN_CLAW_TARGET_RUNTIME_OK layout=2 keyboard=true saved_terrain=true ice_counterexample=true");
    }

    private static async Task RunSavedWall(SceneTree tree, bool ice)
    {
        var root = new Node2D { Name = ice ? "BasinIceKeyboardFixture" : "BasinMetalKeyboardFixture" };
        var basin = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn")
            .Instantiate<PrologueSector>();
        basin.Position = new Vector2(1280, 3600);
        var wall = basin.GetNode<StaticBody2D>(WallPath);
        var profile = wall.GetNode<GrappleSurface2D>("GrappleSurfaceProfile");
        var wallShape = wall.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(profile.Material == GrappleSurfaceMaterial.TempleMetal && profile.UseCollisionHitPoint,
            "saved GearPistonOne is collision-hit TempleMetal, not a fabricated point anchor");
        // Instance property only; no shared Resource, collider, scene or visual mutation.
        if (ice) profile.Material = GrappleSurfaceMaterial.Ice;
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.Position = new Vector2(2180, 3960);
        root.AddChild(basin);
        root.AddChild(player);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
        var health = player.GetNode<HealthComponent>("Health");
        var encounter = basin.GetNode<CanyonEncounterGroup>("Gameplay/Encounters/GearPistonsEncounter");
        ulong playerId = player.GetInstanceId();
        var world = player.GetWorld2D();
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor() && health.CurrentHealth == 5,
                "saved player starts on actual GearEntry with default health");
            KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            Input.ParseInputEvent(new InputEventMouseMotion { Position = new Vector2(-5000, -5000) });
            Input.FlushBufferedEvents();
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(input.Current.KeyboardActions && input.KeyboardAimDirection.IsEqualApprox(Vector2.Right) &&
                Mathf.Abs(input.Current.MoveAxis) < .001f && player.GlobalPosition.X < 2200 &&
                !encounter.IsActive && encounter.LiveActorCount == 0,
                "released D retains right keyboard aim before actual encounter activation");
            TestAssert.True(wallShape.Shape is RectangleShape2D rectangle &&
                rectangle.Size.IsEqualApprox(new Vector2(212, 80)) &&
                wall.GlobalPosition.IsEqualApprox(new Vector2(2546, 3956)),
                "both cases retain the exact saved V2 collider and transform");
            NativeHit axis = Ray(player, player.GlobalPosition + Vector2.Right * 620);
            TestAssert.True(axis.Collider == wall && Mathf.Abs(axis.Point.X - 2440) < .5f &&
                axis.Normal.IsEqualApprox(Vector2.Left),
                "native axis first hits saved wall left face even with Ice material");

            MechanicalClawTargetResolution preview = claw.KeyboardPreview;
            if (!ice)
            {
                TestAssert.Equal(profile, preview.Surface, "keyboard preview selects saved wall despite remote pointer");
                AssertLegalPreview(player, preview);
                TestAssert.True(Mathf.Abs(preview.Snapshot.WorldAnchor.X - 2440) < .5f &&
                    preview.Snapshot.WorldAnchor.Y is >= 3916 and <= 3996 &&
                    preview.Snapshot.SurfaceNormal.IsEqualApprox(Vector2.Left) &&
                    !preview.Snapshot.WorldAnchor.IsEqualApprox(profile.WorldAnchor),
                    "preview is native wall-face contact, not profile center");
            }
            else AssertIceRejection(player, preview, profile, axis.Point.X);
            GD.Print($"BASIN_KEYBOARD_PREVIEW ice={ice} player={player.GlobalPosition} " +
                $"surface={preview.Surface?.GetPath()} anchor={preview.Snapshot.WorldAnchor} " +
                $"axis_hit={axis.Point} material={preview.Snapshot.Material}");

            Vector2 beforeCast = player.GlobalPosition;
            KeyboardActionRuntimeTests.KeyEvent(Key.K, true);
            for (int tick = 0; tick < 30 && claw.CurrentFrame.LockedTargetRuntimeId == 0; tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            if (preview.Surface != null)
            {
                TestAssert.Equal(preview.Snapshot.RuntimeId, claw.CurrentFrame.LockedTargetRuntimeId,
                    "real K locks the exact shown keyboard target");
                TestAssert.True(claw.CurrentFrame.AnchorPoint.DistanceTo(preview.Snapshot.WorldAnchor) < .5f,
                    "K retains shown native contact through windup");
            }
            else TestAssert.Equal(0UL, claw.CurrentFrame.LockedTargetRuntimeId,
                "empty Ice preview does not invent a K target");

            if (ice)
            {
                TestAssert.True(claw.CurrentTarget != profile &&
                    claw.CurrentFrame.LockedTargetRuntimeId != profile.GetInstanceId(),
                    "real K cannot lock the Ice wall");
                if (claw.CurrentFrame.LockedTargetRuntimeId != 0)
                    TestAssert.True(claw.CurrentFrame.AnchorPoint.X <= axis.Point.X + .5f,
                        "Ice cannot be bypassed to lock rear terrain");
                GD.Print($"BASIN_ICE_REJECTED collider_retained=true locked={claw.CurrentFrame.LockedTargetRuntimeId} " +
                    $"alternative={claw.CurrentTarget?.GetPath()}");
            }
            else
            {
                bool attached = false, commandedPull = false;
                float nearestDock = float.PositiveInfinity;
                for (int tick = 0; tick < 60; tick++)
                {
                    await KeyboardActionRuntimeTests.Frames(tree, input, 1);
                    MechanicalClawFrame frame = claw.CurrentFrame;
                    attached |= frame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing;
                    commandedPull |= frame.VelocityCorrection.X > 0 && frame.LockedTargetRuntimeId == profile.GetInstanceId();
                    if (frame.LockedTargetRuntimeId == profile.GetInstanceId())
                        nearestDock = Mathf.Min(nearestDock, player.GlobalPosition.DistanceTo(frame.DockPoint));
                    if (attached && commandedPull && player.GlobalPosition.X - beforeCast.X >= 8) break;
                    if (frame.State is MechanicalClawState.Released or MechanicalClawState.Retract or MechanicalClawState.Idle) break;
                }
                GD.Print($"BASIN_KEYBOARD_PULL observed_dx={player.GlobalPosition.X - beforeCast.X} attached={attached} " +
                    $"commanded={commandedPull} nearest_dock={nearestDock} state={claw.CurrentFrame.State} " +
                    $"failure={claw.CurrentFrame.Failure} encounter_active={encounter.IsActive} actors={encounter.LiveActorCount}");
                TestAssert.True(attached && commandedPull && player.GlobalPosition.X - beforeCast.X >= 8 &&
                    nearestDock < beforeCast.DistanceTo(preview.Snapshot.WorldAnchor + Vector2.Left * 48) - 8 &&
                    Mathf.Abs(input.Current.MoveAxis) < .001f &&
                    !Input.IsPhysicalKeyPressed(Key.D) && !Input.IsPhysicalKeyPressed(Key.W),
                    "actual saved-wall auto-pull advances toward dock with no held movement key");
                // Stop after a bounded witnessed pull. Live enemies may subsequently
                // obstruct LOS; arrival through an enemy is not this contract.
                GD.Print("BASIN_KEYBOARD_PULL_OK arrival_not_certified=true");
            }
            TestAssert.True(player.GetInstanceId() == playerId && player.GetWorld2D() == world &&
                player.IsInsideTree() && health.CurrentHealth == 5,
                "same living player/world persists without reset through bounded trial");
        }
        finally
        {
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.K, false);
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
        }
    }

    private static void AssertIceRejection(CharacterBody2D player, MechanicalClawTargetResolution preview,
        GrappleSurface2D ice, float blockedX)
    {
        TestAssert.True(preview.Surface != ice && preview.Snapshot.RuntimeId != ice.GetInstanceId(),
            "keyboard rejects actual Ice profile, not its collider");
        if (preview.Surface == null) return;
        AssertLegalPreview(player, preview);
        TestAssert.True(preview.Snapshot.WorldAnchor.X <= blockedX + .5f,
            "legal Ice alternative stays in front of first solid wall, never behind it");
    }

    private static void AssertLegalPreview(CharacterBody2D player, MechanicalClawTargetResolution preview)
    {
        TestAssert.True(preview.Surface != null && preview.Surface.IsAvailable &&
            preview.Surface.GetWorld2D() == player.GetWorld2D() && preview.Snapshot.Exists &&
            preview.Snapshot.HasLineOfSight &&
            preview.Snapshot.Material is not (GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden) &&
            preview.Surface.UseCollisionHitPoint && !preview.Snapshot.IsLegacyInteraction,
            "this terrain preview is live, legal and collision-derived in player's world");
        Vector2 direction = player.GlobalPosition.DirectionTo(preview.Snapshot.WorldAnchor);
        NativeHit hit = Ray(player, preview.Snapshot.WorldAnchor + direction);
        TestAssert.True(hit.Collider == preview.Surface.GetParent() &&
            hit.Point.DistanceTo(preview.Snapshot.WorldAnchor) < .5f &&
            hit.Normal.IsEqualApprox(preview.Snapshot.SurfaceNormal),
            "independent native first-hit ray agrees with preview contact and normal");
    }

    private static NativeHit Ray(CharacterBody2D player, Vector2 end)
    {
        using var query = PhysicsRayQueryParameters2D.Create(player.GlobalPosition, end);
        query.CollisionMask = CombatCollisionLayers.World;
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        using var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
        TestAssert.True(hit.ContainsKey("collider") && hit.ContainsKey("position") && hit.ContainsKey("normal"),
            "saved terrain native ray has a real first collision");
        return new NativeHit(hit["collider"].AsGodotObject() as Node,
            hit["position"].AsVector2(), hit["normal"].AsVector2());
    }

    private readonly record struct NativeHit(Node Collider, Vector2 Point, Vector2 Normal);
}
