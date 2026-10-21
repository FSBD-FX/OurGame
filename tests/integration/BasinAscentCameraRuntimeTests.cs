using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class BasinAscentCameraRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        Vector2I oldSize = tree.Root.Size, oldContentSize = tree.Root.ContentScaleSize;
        try
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            tree.Root.Size = new Vector2I(1280, 720);
            tree.Root.ContentScaleSize = new Vector2I(1280, 720);
            await CheckPosition(tree, PrologueSectorId.CanyonBasin, new Vector2(3915, 3759),
                new Vector2(3915, 3760), "ShortcutStep3796", new Rect2(3840, 3796, 150, 32), false);
            await CheckPosition(tree, PrologueSectorId.CanyonAscent, new Vector2(3915, 3547),
                new Vector2(3915, 3548), "LiftEntryFloor", new Rect2(3840, 3584, 160, 16), false);
            await CheckPosition(tree, PrologueSectorId.CanyonAscent, new Vector2(4780, 1283),
                new Vector2(4780, 1284), "AscentFinalCatch", new Rect2(4660, 1320, 240, 40), true);
            GD.Print("BASIN_ASCENT_CAMERA_RUNTIME_OK");
        }
        finally
        {
            tree.Root.Size = oldSize;
            tree.Root.ContentScaleSize = oldContentSize;
        }
    }

    // Break caught: V1 sector limits, lost native floor contact or a world-space focus assigned
    // as player-local camera Position can clip a grounded capsule in V2's y=3600 seam holds.
    private static async Task CheckPosition(SceneTree tree, PrologueSectorId sector, Vector2 spawn,
        Vector2 settled, string supportName, Rect2 supportBounds, bool aboveSharedBand)
    {
        PrologueLevelController level = null;
        int recoveries = 0;
        void Recovered(string reason) => recoveries++;
        try
        {
            level = CreateLevelFixture(sector, spawn);
            CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            level.RecoveryCompleted += Recovered;
            ulong playerId = player.GetInstanceId();
            ulong worldId = world.GetInstanceId();
            tree.Root.AddChild(level);
            await Frames(tree, 8);
            var camera = player.GetNode<Camera2D>("Camera2D");
            var collider = player.GetNode<CollisionShape2D>("BodyCollider");
            TestAssert.True(collider.Shape is CapsuleShape2D, "fixture projects the actual player capsule, not a center point");
            StaticBody2D support = ResolveAuthoredSupport(world, sector, supportName, supportBounds);
            Vector2 initialOffset = camera.Offset;
            float minMargin = float.PositiveInfinity;
            for (int sample = 0; sample < 12; sample++)
            {
                await Frames(tree, 1);
                TestAssert.Equal(sector, world.CurrentSector, "camera protection must not advance or reverse the world sector authority");
                TestAssert.Equal(sector, level.CurrentSector!.Value, "level location agrees with the unchanged world sector");
                TestAssert.True(world.MountedSectorIds.Contains(PrologueSectorId.CanyonBasin) &&
                    world.MountedSectorIds.Contains(PrologueSectorId.CanyonAscent) && world.MountedSectorIds.Count <= 3,
                    "both real adjacent sectors remain mounted within the existing stream budget");
                TestAssert.Equal(playerId, level.Player.GetInstanceId(), "camera adjustment preserves the persistent player instance");
                TestAssert.Equal(worldId, level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").GetInstanceId(),
                    "camera adjustment preserves the persistent seamless world instance");
                TestAssert.Equal(0, recoveries, "camera visibility cannot be obtained through recovery relocation");
                float margin = AssertV2GroundedSample(player, world, support, sector, settled,
                    aboveSharedBand, initialOffset);
                minMargin = Math.Min(minMargin, margin);
                GD.Print($"BASIN_ASCENT_CAMERA_SAMPLE sector={sector} high={aboveSharedBand} sample={sample} " +
                    $"player={player.GlobalPosition} support={support.GetPath()} native_upward_contact=True " +
                    $"margin={margin} focus={camera.Position} limits={camera.LimitLeft},{camera.LimitTop}.." +
                    $"{camera.LimitRight},{camera.LimitBottom} recovery={recoveries}");
            }
            GD.Print($"BASIN_ASCENT_CAMERA_CASE_OK sector={sector} high={aboveSharedBand} minMargin={minMargin}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level))
            {
                level.RecoveryCompleted -= Recovered;
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
        }
    }

    internal static StaticBody2D ResolveAuthoredSupport(SeamlessPrologueWorld world,
        PrologueSectorId sector, string supportName, Rect2 expectedBounds)
    {
        TestAssert.True(world.TryGetMountedSector(sector, out PrologueSector mounted),
            "ground hold resolves its actual mounted sector");
        StaticBody2D support = mounted.GetNode<StaticBody2D>("Gameplay/Collision/" + supportName);
        CollisionShape2D collider = support.GetNode<CollisionShape2D>("Collision");
        TestAssert.True(collider.Shape is RectangleShape2D && !collider.Disabled,
            supportName + ": native support uses the enabled saved rectangular collision");
        var rectangle = (RectangleShape2D)collider.Shape;
        TestAssert.True((collider.GlobalPosition - rectangle.Size * .5f)
                .DistanceTo(expectedBounds.Position) < .01f &&
            rectangle.Size.DistanceTo(expectedBounds.Size) < .01f,
            supportName + ": saved support matches the independently audited world rectangle");
        return support;
    }

    internal static float AssertV2GroundedSample(CharacterBody2D player, SeamlessPrologueWorld world,
        StaticBody2D support, PrologueSectorId sector, Vector2 settled, bool aboveSharedBand,
        Vector2 initialOffset)
    {
        TestAssert.Equal(2, world.LayoutVersion, "grounded holds exercise the saved V2 world");
        TestAssert.True(world.ProcessMode != Node.ProcessModeEnum.Disabled && world.IsPhysicsProcessing(),
            "the real seamless world continues automatic camera updates throughout each hold");
        TestAssert.Equal(Node.ProcessModeEnum.Disabled, player.GetNode<PlayerInputReader>("Input").ProcessMode,
            "grounded holds disable input rather than driving or relocating the player");
        PlayerMotor motor = player.GetNode<PlayerMotor>("Motor");
        TestAssert.True(motor.ProcessMode != Node.ProcessModeEnum.Disabled && motor.IsPhysicsProcessing(),
            "the actual PlayerMotor remains automatic during every ground sample");
        TestAssert.True(player.IsOnFloor(), "all three camera holds remain on authored ground");
        bool upwardContact = false;
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            using KinematicCollision2D contact = player.GetSlideCollision(index);
            if (contact.GetCollider() == support && contact.GetNormal().Y < -.7f)
                upwardContact = true;
        }
        TestAssert.True(upwardContact,
            $"{sector}: native upward slide contact must name {support.GetPath()}; player={player.GlobalPosition}");
        TestAssert.True(player.GlobalPosition.DistanceTo(settled) <= .25f,
            $"{sector}: automatic motor settles on the audited support without relocation; " +
            $"expected={settled} actual={player.GlobalPosition}");
        TestAssert.True(player.GlobalPosition.X > 3840f && player.GlobalPosition.X < 5120f &&
            (sector == PrologueSectorId.CanyonBasin ? player.GlobalPosition.Y > 3600f : player.GlobalPosition.Y < 3600f),
            "the capsule center retains its authoritative y-side of the shared x3840..5120 seam");

        Camera2D camera = player.GetNode<Camera2D>("Camera2D");
        TestAssert.True(camera.Enabled && ReferenceEquals(camera.GetParent(), player),
            "the automatic live camera remains owned by the same persistent player");
        TestAssert.Equal(0, camera.LimitLeft, "V2 holds retain the global design left bound");
        TestAssert.Equal(0, camera.LimitTop, "V2 holds retain the global design top bound");
        TestAssert.Equal(7680, camera.LimitRight, "V2 holds retain the global design right bound");
        TestAssert.Equal(4320, camera.LimitBottom, "V2 holds retain the global design bottom bound");
        Vector2 expectedFocus = sector == PrologueSectorId.CanyonBasin
            ? new Vector2(100, -40) : new Vector2(80, -140);
        TestAssert.True(camera.Position.DistanceTo(expectedFocus) < .25f,
            $"{sector}: stationary V2 camera retains its authored player-local focus; actual={camera.Position}");
        TestAssert.Equal(initialOffset, camera.Offset, "route focus preserves the independent shake Offset channel");
        Vector2 viewport = player.GetViewportRect().Size;
        TestAssert.Equal(new Vector2(1280, 720), viewport,
            "full-capsule projection uses the actual 1280x720 logical viewport");
        Vector2 center = camera.GetScreenCenterPosition();
        Vector2 worldHalf = viewport * .5f / camera.Zoom;
        TestAssert.True(center.X - worldHalf.X >= -.01f && center.Y - worldHalf.Y >= -.01f &&
            center.X + worldHalf.X <= 7680.01f && center.Y + worldHalf.Y <= 4320.01f,
            "the complete horizontal and vertical viewport stays inside V2 global design coverage");
        if (aboveSharedBand)
            TestAssert.True(center.Y - worldHalf.Y < 3600f,
                "high grounded Ascent genuinely looks above Basin's y3600 authored coverage");

        CollisionShape2D collider = player.GetNode<CollisionShape2D>("BodyCollider");
        TestAssert.True(collider.Shape is CapsuleShape2D, "each hold projects the actual full capsule");
        var capsule = (CapsuleShape2D)collider.Shape;
        Transform2D canvas = collider.GetGlobalTransformWithCanvas();
        Vector2 half = new(capsule.Radius, capsule.Height * .5f);
        Vector2[] corners = { canvas * -half, canvas * new Vector2(half.X, -half.Y),
            canvas * half, canvas * new Vector2(-half.X, half.Y) };
        float horizontalMargin = Math.Min(corners.Min(point => point.X), viewport.X - corners.Max(point => point.X));
        float verticalMargin = Math.Min(corners.Min(point => point.Y), viewport.Y - corners.Max(point => point.Y));
        float margin = Math.Min(horizontalMargin, verticalMargin);
        TestAssert.True(margin >= 16f,
            $"{sector}: complete real capsule requires 16px clearance on all four viewport edges; " +
            $"horizontal={horizontalMargin} vertical={verticalMargin}");
        return margin;
    }

    internal static PrologueLevelController CreateLevelFixture(PrologueSectorId sector, Vector2 spawn)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        // Controlled pre-tree fixture position. Motor/camera/AI remain automatic;
        // these holds do not claim to exercise the real InputMap seam crossing.
        var player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        player.Position = spawn;
        player.GetNode<PlayerInputReader>("Input").ProcessMode = Node.ProcessModeEnum.Disabled;
        level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").InitialSector = sector;
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = true;
        IsolatedPrologueFixture.Configure(level, "basin-camera-" + sector);
        return level;
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int tick = 0; tick < count; tick++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
