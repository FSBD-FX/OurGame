using System;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Traversal;

namespace Knight.Tests.Integration;

public static class PrologueEnvironmentStructureTests
{
    private static readonly string[] RoomNames =
    [
        "AwakeningCourtyard", "BrokenToothCorridor", "UpperPendulum",
        "LowerBuriedMachinery", "BellChainMechanism", "FallenBellkeeperArena"
    ];

    private static readonly string[] ArtGroups =
        ["Far", "Back", "Mid", "Platforms", "Props", "Front", "Atmosphere"];

    private static readonly ExpectedRoomVisual[] ExpectedRoomVisuals =
    [
        new(PrologueRoomId.AwakeningCourtyard, "AwakeningCourtyard", "broken-clock"),
        new(PrologueRoomId.BrokenToothCorridor, "BrokenToothCorridor", "severed-gear-train"),
        new(PrologueRoomId.UpperPendulum, "UpperPendulum", "pendulum-chains"),
        new(PrologueRoomId.LowerBuriedMachinery, "LowerBuriedMachinery", "copper-pipes-fungus"),
        new(PrologueRoomId.BellChainMechanism, "BellChainMechanism", "vertical-bell-chain"),
        new(PrologueRoomId.FallenBellkeeperArena, "FallenBellkeeperArena", "backlit-broken-bell")
    ];

    private static readonly (string Path, string Role)[] ExpectedDepthRoles =
    [
        ("Far", "far"), ("Back", "far"), ("Mid", "mid"),
        ("Platforms", "play"), ("Props", "play"), ("Front", "front"),
        ("Atmosphere", "atmosphere")
    ];

    private static readonly ExpectedTraversalSegment[] ExpectedTraversalManifest =
    [
        new("AwakeningCourtyard", "spawn_to_jump_step", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, ""),
        new("AwakeningCourtyard", "jump_step_to_attack_ledge", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, ""),
        new("BrokenToothCorridor", "entry_floor_to_teaching_ledge", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, ""),
        new("BrokenToothCorridor", "entry_to_anchor_landing", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread, "Geometry/ThreadAnchor"),
        new("UpperPendulum", "entry_to_lower_swing", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread, "Geometry/AnchorLow"),
        new("UpperPendulum", "lower_to_mid_swing", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread, "Geometry/AnchorHigh"),
        new("UpperPendulum", "mid_to_high_swing", TraversalSegmentKind.Assisted,
            TraversalAbility.Jump | TraversalAbility.Thread, "Geometry/ReboundTarget"),
        new("UpperPendulum", "high_to_mechanism_landing", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, ""),
        new("LowerBuriedMachinery", "entry_to_arena", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, ""),
        new("BellChainMechanism", "arrival_to_chain_floor", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, ""),
        new("BellChainMechanism", "chain_floor_to_completion_dais", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, ""),
        new("FallenBellkeeperArena", "retry_to_arena_floor", TraversalSegmentKind.SingleJump,
            TraversalAbility.Jump, "")
    ];

    private static readonly ExpectedTraversalSurface[] ExpectedTraversalSurfaces =
    [
        new("AwakeningCourtyard", "spawn_to_jump_step", "Geometry/SpawnFloor", "Geometry/JumpStep"),
        new("AwakeningCourtyard", "jump_step_to_attack_ledge", "Geometry/JumpStep", "Geometry/AttackLedge"),
        new("BrokenToothCorridor", "entry_floor_to_teaching_ledge", "Geometry/EntryFloor", "Geometry/TeachingLedge"),
        new("BrokenToothCorridor", "entry_to_anchor_landing", "Geometry/TeachingLedge", "Geometry/AnchorLanding"),
        new("UpperPendulum", "entry_to_lower_swing", "Geometry/EntryFloor", "Geometry/LowerSwing"),
        new("UpperPendulum", "lower_to_mid_swing", "Geometry/LowerSwing", "Geometry/MidSwing"),
        new("UpperPendulum", "mid_to_high_swing", "Geometry/MidSwing", "Geometry/HighSwing"),
        new("UpperPendulum", "high_to_mechanism_landing", "Geometry/HighSwing", "Geometry/MechanismLanding"),
        new("LowerBuriedMachinery", "entry_to_arena", "Geometry/EntryFloor", "Geometry/ArenaFloor"),
        new("BellChainMechanism", "arrival_to_chain_floor", "Geometry/UpperArrival", "Geometry/LowerFloor"),
        new("BellChainMechanism", "chain_floor_to_completion_dais", "Geometry/LowerFloor", "Geometry/CompletionDais"),
        new("FallenBellkeeperArena", "retry_to_arena_floor", "Geometry/LeftDais", "Geometry/ArenaFloor")
    ];

    public static void Run()
    {
        var landmarks = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (ExpectedRoomVisual expected in ExpectedRoomVisuals)
        {
            RoomVisualDescriptor descriptor = RoomVisualCatalog.For(expected.Id);
            TestAssert.Equal(expected.Id, descriptor.Room,
                $"{expected.RoomName} descriptor retains its strong room ID");
            TestAssert.Equal(expected.Landmark, descriptor.Landmark,
                $"{expected.RoomName} selects its approved landmark");
            TestAssert.True(landmarks.Add(descriptor.Landmark),
                $"{expected.RoomName} landmark ID is unique across the six-room catalog");
        }
        TestAssert.Equal(6, landmarks.Count, "the descriptor catalog exposes six unique room landmarks");

        TestAssert.True(ResourceLoader.Exists("res://scenes/presentation/PrologueEnvironmentKit.tscn"),
            "formal prologue environment kit scene exists");
        TestAssert.True(FileAccess.FileExists("res://assets/environment/nameless-belfry/environment-atlas.png"),
            "formal prologue environment atlas exists");

        Node2D player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<Node2D>();
        CapsuleShape2D playerCapsule = player.GetNode<CollisionShape2D>("BodyCollider").Shape as CapsuleShape2D;
        TestAssert.True(playerCapsule != null, "formal player traversal collider is a capsule");
        TestAssert.True(float.IsFinite(playerCapsule.Radius) && playerCapsule.Radius > 0f,
            "formal player traversal capsule radius is finite and positive");
        TestAssert.True(float.IsFinite(playerCapsule.Height) && playerCapsule.Height > 0f,
            "formal player traversal capsule height is finite and positive");
        Rect2 playerBodyExtents = CapsuleBoundsInAncestor(player.GetNode<CollisionShape2D>("BodyCollider"),
            player, "Player/BodyCollider");
        AssertTransformedCollisionBoundsMath();
        AssertPackedPlayerColliderOffsetMutationFailsClosed(player);
        player.Free();

        AssertPackedAssistedTargetAncestorMutationFailsClosed();
        AssertPackedCollisionOffsetMutationFailsClosed(playerBodyExtents);
        AssertInvalidTraversalMetadataFailsClosed();

        foreach (string roomName in RoomNames)
        {
            ExpectedRoomVisual expected = Array.Find(ExpectedRoomVisuals,
                candidate => candidate.RoomName == roomName);
            string path = $"res://scenes/prologue/rooms/{roomName}.tscn";
            PrologueRoom room = GD.Load<PackedScene>(path).Instantiate<PrologueRoom>();
            RoomFormalVisual art = room.GetNodeOrNull<RoomFormalVisual>("Art");
            TestAssert.True(art != null, $"{roomName} mounts formal room art at stable Art path");
            TestAssert.Equal(new Vector2(1280, 720), art.CameraBasis,
                $"{roomName} formal art uses the 1280x720 camera basis");
            TestAssert.True(!string.IsNullOrWhiteSpace(art.RoomKey),
                $"{roomName} formal art declares a room descriptor key");
            TestAssert.Equal(expected.Id, art.Room,
                $"{roomName} formal art binds to its catalog descriptor");
            TestAssert.Equal(expected.Landmark, art.LandmarkId,
                $"{roomName} formal art serializes its unique landmark ID");
            foreach (string group in ArtGroups)
                TestAssert.True(art.GetNodeOrNull<Node2D>(group) != null,
                    $"{roomName} exposes Art/{group}");
            foreach ((string leafPath, string role) in ExpectedDepthRoles)
                TestAssert.Equal(role, art.GetNode<Node2D>(leafPath).GetMeta("depth_role").AsString(),
                    $"{roomName} maps Art/{leafPath} to the stable {role} depth role");
            TestAssert.True(art.GetNodeOrNull<Node2D>("Mid/Landmark") != null,
                $"{roomName} owns a replaceable Mid/Landmark composition");
            TestAssert.True(art.GetNode<Node2D>("Mid/Landmark").GetChildCount() >= 3,
                $"{roomName} landmark is a readable large-form layered composition");
            if (roomName == "BrokenToothCorridor")
            {
                Polygon2D driveGear = art.GetNode<Polygon2D>("Mid/Landmark/DriveGear");
                Polygon2D severedGear = art.GetNode<Polygon2D>("Mid/Landmark/SeveredGear");
                TestAssert.True(RadialSpan(driveGear.Polygon) >= 22f,
                    "Broken-Tooth drive gear has a low-frequency toothed silhouette at proof scale");
                TestAssert.True(RadialSpan(severedGear.Polygon) >= 26f,
                    "Broken-Tooth severed gear has a large toothed silhouette at proof scale");
                TestAssert.True(art.GetNodeOrNull<Line2D>("Mid/Landmark/DriveAxleStub") != null &&
                                art.GetNodeOrNull<Line2D>("Mid/Landmark/SeveredAxleStub") != null,
                    "Broken-Tooth gear train exposes two separated axle stubs instead of a bridging axle");
            }

            Node2D placeholder = room.GetNode<Node2D>("Presentation/VisualRoot/Placeholder");
            TestAssert.True(!placeholder.Visible,
                $"{roomName} gray-box presentation is not visible");
            Label debugLabel = room.GetNodeOrNull<Label>("Presentation/VisualRoot/Placeholder/RoomLabel");
            TestAssert.True(debugLabel == null || !debugLabel.Visible,
                $"{roomName} debug room label is not visible");

            Node geometry = room.GetNode("Geometry");
            foreach (Node child in geometry.GetChildren())
            {
                if (child is not StaticBody2D platform) continue;
                CollisionShape2D collision = platform.GetNodeOrNull<CollisionShape2D>("Collision");
                if (collision?.Shape is not RectangleShape2D rectangle) continue;
                Polygon2D skin = art.GetNodeOrNull<Polygon2D>($"Platforms/{platform.Name}");
                TestAssert.True(skin != null,
                    $"{roomName} formal art skins platform {platform.Name}");
                TestAssert.Equal(platform.Position, skin.Position,
                    $"{roomName} platform skin {platform.Name} aligns to collision center");
                TestAssert.Equal(rectangle.Size, PolygonSize(skin.Polygon),
                    $"{roomName} platform skin {platform.Name} aligns to collision size");
            }

            foreach (Node descendant in art.FindChildren("*", "CollisionObject2D", true, false))
                throw new InvalidOperationException(
                    $"Assertion failed: {roomName} formal art must not own gameplay collision ({descendant.GetPath()})");
            TestAssert.True(ReferenceEquals(art, room.ResolvePresentation()),
                $"{roomName} resolves its formal art without remounting gray-box visuals");
            AssertTraversalContracts(roomName, room, playerBodyExtents);
            if (roomName == "BrokenToothCorridor")
                AssertCorridorCheckpointSupport(room, playerBodyExtents);
            room.Free();

            TestAssert.True(FileAccess.FileExists(
                    $"res://assets/environment/nameless-belfry/rooms/{ToKebab(roomName)}-layers.json"),
                $"{roomName} has a replaceable layer descriptor");
        }

    }

    private static void AssertCorridorCheckpointSupport(PrologueRoom room, Rect2 playerBodyExtents)
    {
        Marker2D entrance = room.GetNode<Marker2D>("Entrances/checkpoint");
        CheckpointTrigger trigger = room.GetNode<CheckpointTrigger>("Checkpoint");
        StaticBody2D completionFloor = room.GetNode<StaticBody2D>("Geometry/CompletionFloor");
        Rect2 support = RectangleBoundsInAncestor(completionFloor, room,
            "BrokenToothCorridor checkpoint CompletionFloor support");
        float supportedLeft = support.Position.X - playerBodyExtents.Position.X;
        float supportedRight = support.End.X - playerBodyExtents.End.X;
        float standingY = support.Position.Y - playerBodyExtents.End.Y;

        TestAssert.Equal(entrance.Position, trigger.Position,
            "BrokenToothCorridor checkpoint trigger and recovery entrance share one authored point");
        TestAssert.True(entrance.Position.X >= supportedLeft && entrance.Position.X <= supportedRight,
            "BrokenToothCorridor checkpoint recovery center is capsule-supported by CompletionFloor");
        float settlingOffset = standingY - entrance.Position.Y;
        TestAssert.True(Mathf.Abs(settlingOffset) <= 16f,
            "BrokenToothCorridor checkpoint recovery height settles the capsule onto CompletionFloor within 16 px");
    }

    private static void AssertTraversalContracts(string roomName, PrologueRoom room, Rect2 playerBodyExtents)
    {
        Node contracts = room.GetNodeOrNull("TraversalContracts");
        TestAssert.True(contracts != null, $"{roomName} exposes stable TraversalContracts root");
        TestAssert.True(ReferenceEquals(room, contracts.Owner),
            $"{roomName} TraversalContracts root survives packed owner chain");

        ExpectedTraversalSegment[] expected = Array.FindAll(ExpectedTraversalManifest,
            segment => segment.RoomName == roomName);
        TestAssert.Equal(expected.Length, contracts.GetChildCount(),
            $"{roomName} has the exact mandatory traversal manifest count");

        var observedIds = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        foreach (Node child in contracts.GetChildren())
        {
            TestAssert.True(child is TraversalSegment2D,
                $"{roomName}/{child.Name} is typed TraversalSegment2D metadata");
            var segment = (TraversalSegment2D)child;
            string id = segment.SegmentId.ToString();
            TestAssert.True(!string.IsNullOrWhiteSpace(id),
                $"{roomName} traversal segment ID is non-empty");
            TestAssert.True(observedIds.Add(id),
                $"{roomName} traversal segment ID '{id}' is unique");
            TestAssert.Equal(id, segment.Name.ToString(),
                $"{roomName}/{id} keeps stable TraversalContracts/<segment-id> path");
            TestAssert.True(ReferenceEquals(room, segment.Owner),
                $"{roomName}/{id} survives packed owner chain");
            TestAssert.True(segment.LaunchZone.Size.X >= 48f && segment.LaunchZone.Size.Y > 0f,
                $"{roomName}/{id} launch zone is positive and at least 48 px wide");
            TestAssert.True(segment.LandingZone.Size.X >= 48f && segment.LandingZone.Size.Y > 0f,
                $"{roomName}/{id} landing zone is positive and at least 48 px wide");
            System.Collections.Generic.IReadOnlyList<string> validationErrors = segment.Validate(room);
            TestAssert.Equal(0, validationErrors.Count,
                $"{roomName}/{id} typed traversal metadata validates after packed reload: {string.Join("; ", validationErrors)}");

            ExpectedTraversalSegment contract = Array.Find(expected, item => item.Id == id);
            TestAssert.True(contract != null, $"{roomName}/{id} is declared in the binding ten-entry manifest");
            TestAssert.Equal(contract.Kind, segment.Kind, $"{roomName}/{id} declares the expected kind");
            TestAssert.Equal(contract.Required, segment.RequiredAbilities,
                $"{roomName}/{id} declares the exact legal ability mask");
            TestAssert.Equal(contract.AssistPath, segment.AssistTargetPath.ToString(),
                $"{roomName}/{id} declares the expected assist path");
            TestAssert.True(segment.Kind == TraversalSegmentKind.OptionalAdvanced ||
                            (segment.RequiredAbilities & TraversalAbility.Dash) == 0,
                $"{roomName}/{id} mandatory route never requires Dash");

            if (segment.Kind == TraversalSegmentKind.Assisted)
                AssertAssistedTarget(roomName, room, segment);
            AssertTraversalSurface(roomName, room, segment, playerBodyExtents);
        }
    }

    private static void AssertTraversalSurface(string roomName, PrologueRoom room, TraversalSegment2D segment,
        Rect2 playerBodyExtents)
    {
        string id = segment.SegmentId.ToString();
        ExpectedTraversalSurface mapping = Array.Find(ExpectedTraversalSurfaces,
            item => item.RoomName == roomName && item.Id == id);
        TestAssert.True(mapping != null, $"{roomName}/{id} maps to an intended source and target platform");
        StaticBody2D source = room.GetNodeOrNull<StaticBody2D>(mapping.SourcePath);
        StaticBody2D target = room.GetNodeOrNull<StaticBody2D>(mapping.TargetPath);
        TestAssert.True(source != null, $"{roomName}/{id} resolves real source platform {mapping.SourcePath}");
        TestAssert.True(target != null, $"{roomName}/{id} resolves real target platform {mapping.TargetPath}");
        Rect2 sourceBounds = RectangleBoundsInAncestor(source, room,
            $"{roomName}/{id} source platform {mapping.SourcePath}");
        Rect2 targetBounds = RectangleBoundsInAncestor(target, room,
            $"{roomName}/{id} target platform {mapping.TargetPath}");

        float sourceLeft = sourceBounds.Position.X - playerBodyExtents.Position.X;
        float sourceRight = sourceBounds.End.X - playerBodyExtents.End.X;
        float sourceStandingY = sourceBounds.Position.Y - playerBodyExtents.End.Y;
        TestAssert.True(segment.LaunchZone.HasPoint(new Vector2(segment.LaunchZone.GetCenter().X, sourceStandingY)),
            $"{roomName}/{id} launch band contains the real standing player center on {mapping.SourcePath}");
        foreach (float fraction in new[] { .2f, .5f, .8f })
        {
            float sampleX = Mathf.Lerp(segment.LaunchZone.Position.X, segment.LaunchZone.End.X, fraction);
            TestAssert.True(sampleX >= sourceLeft && sampleX <= sourceRight,
                $"{roomName}/{id} launch sample {fraction:P0} is capsule-supported by {mapping.SourcePath}");
            TestAssert.True(!segment.LandingZone.HasPoint(new Vector2(sampleX, sourceStandingY)),
                $"{roomName}/{id} launch sample {fraction:P0} does not begin already inside its landing zone");
        }
        TestAssert.True(!segment.LaunchZone.Intersects(segment.LandingZone),
            $"{roomName}/{id} launch and landing zones do not overlap");

        float targetLeft = targetBounds.Position.X - playerBodyExtents.Position.X;
        float targetRight = targetBounds.End.X - playerBodyExtents.End.X;
        float targetStandingY = targetBounds.Position.Y - playerBodyExtents.End.Y;
        float stableLeft = Mathf.Max(segment.LandingZone.Position.X, targetLeft);
        float stableRight = Mathf.Min(segment.LandingZone.End.X, targetRight);
        TestAssert.True(stableRight > stableLeft,
            $"{roomName}/{id} landing zone overlaps a capsule-supported target surface on {mapping.TargetPath}");
        Vector2 stableLandingCenter = new((stableLeft + stableRight) * .5f, targetStandingY);
        TestAssert.True(segment.LandingZone.HasPoint(stableLandingCenter),
            $"{roomName}/{id} landing zone contains a stable standing player center on {mapping.TargetPath}");
    }

    private static void AssertAssistedTarget(string roomName, PrologueRoom room, TraversalSegment2D segment)
    {
        ThreadTarget2D target = room.GetNodeOrNull<ThreadTarget2D>(segment.AssistTargetPath);
        TestAssert.True(target != null,
            $"{roomName}/{segment.SegmentId} assist path resolves to ThreadTarget2D");
        TestAssert.Equal(ThreadTargetKind.Anchor, target.Kind,
            $"{roomName}/{segment.SegmentId} assist target is an Anchor");
        Area2D collision = target.GetNodeOrNull<Area2D>("TargetCollision");
        TestAssert.True(collision != null &&
                        (collision.CollisionLayer & CombatCollisionLayers.ThreadTarget) != 0,
            $"{roomName}/{segment.SegmentId} assist target uses ThreadTarget collision layer");

        Vector2 launch = segment.LaunchZone.GetCenter();
        Vector2 anchor = PositionInAncestor(target, room,
            $"{roomName}/{segment.SegmentId} assist target {segment.AssistTargetPath}");
        Vector2 landing = segment.LandingZone.GetCenter();
        TestAssert.True(launch.DistanceTo(anchor) <= 400f,
            $"{roomName}/{segment.SegmentId} anchor is within formal 400 px hold range");
        TestAssert.True((anchor.X - launch.X) * (landing.X - launch.X) > 0f,
            $"{roomName}/{segment.SegmentId} anchor lies in the declared landing-facing half-plane");
    }

    private static void AssertInvalidTraversalMetadataFailsClosed()
    {
        var room = new Node2D { Name = "InvalidRoom" };
        var contracts = new Node2D { Name = "TraversalContracts" };
        room.AddChild(contracts);
        var duplicate = new TraversalSegment2D
        {
            Name = "Duplicate", SegmentId = "duplicate", Kind = TraversalSegmentKind.SingleJump,
            RequiredAbilities = TraversalAbility.Jump,
            LaunchZone = new Rect2(0, 0, 0, 24), LandingZone = new Rect2(80, 0, 48, 24)
        };
        contracts.AddChild(duplicate);
        contracts.AddChild(new TraversalSegment2D
        {
            Name = "DuplicatePeer", SegmentId = "duplicate", Kind = TraversalSegmentKind.SingleJump,
            RequiredAbilities = TraversalAbility.Jump,
            LaunchZone = new Rect2(0, 0, 48, 24), LandingZone = new Rect2(80, 0, 48, 24)
        });

        System.Collections.Generic.IReadOnlyList<string> errors = duplicate.Validate(room);
        TestAssert.True(Contains(errors, nameof(TraversalSegment2D.LaunchZone)),
            "zero-width traversal launch zone fails closed with named LaunchZone error");
        TestAssert.True(Contains(errors, "duplicate"),
            "duplicate traversal SegmentId fails closed with named duplicate error");
        room.Free();

        var nonFiniteRoom = new Node2D { Name = "NonFiniteRoom" };
        var geometry = new Node2D { Name = "Geometry", Position = new Vector2(float.NaN, float.PositiveInfinity) };
        var anchor = new ThreadTarget2D { Name = "Anchor", Kind = ThreadTargetKind.Anchor };
        var targetCollision = new Area2D
        {
            Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget
        };
        anchor.AddChild(targetCollision);
        geometry.AddChild(anchor);
        nonFiniteRoom.AddChild(geometry);
        var nonFiniteContracts = new Node2D { Name = "TraversalContracts" };
        nonFiniteRoom.AddChild(nonFiniteContracts);
        var nonFinite = new TraversalSegment2D
        {
            Name = "non_finite_anchor", SegmentId = "non_finite_anchor",
            Kind = TraversalSegmentKind.Assisted,
            RequiredAbilities = TraversalAbility.Jump | TraversalAbility.Thread,
            LaunchZone = new Rect2(0, 0, 96, 48), LandingZone = new Rect2(200, 0, 96, 48),
            AssistTargetPath = new NodePath("Geometry/Anchor")
        };
        nonFiniteContracts.AddChild(nonFinite);
        System.Collections.Generic.IReadOnlyList<string> nonFiniteErrors = nonFinite.Validate(nonFiniteRoom);
        TestAssert.True(Contains(nonFiniteErrors, "finite"),
            "non-finite resolved anchor position fails closed with a named finite-position error");
        nonFiniteRoom.Free();
    }

    private static void AssertPackedCollisionOffsetMutationFailsClosed(Rect2 playerBodyExtents)
    {
        PrologueRoom room = GD.Load<PackedScene>(
            "res://scenes/prologue/rooms/AwakeningCourtyard.tscn").Instantiate<PrologueRoom>();
        TraversalSegment2D segment = room.GetNode<TraversalSegment2D>(
            "TraversalContracts/spawn_to_jump_step");
        CollisionShape2D sourceCollision = room.GetNode<CollisionShape2D>(
            "Geometry/SpawnFloor/Collision");
        sourceCollision.Position += new Vector2(600f, 0f);

        bool failedByNamedSurfaceGate = false;
        try
        {
            AssertTraversalSurface("AwakeningCourtyard", room, segment, playerBodyExtents);
        }
        catch (InvalidOperationException error)
        {
            failedByNamedSurfaceGate = error.Message.Contains(
                "AwakeningCourtyard/spawn_to_jump_step launch sample", StringComparison.Ordinal) &&
                error.Message.Contains("Geometry/SpawnFloor", StringComparison.Ordinal);
        }
        finally
        {
            room.Free();
        }

        TestAssert.True(failedByNamedSurfaceGate,
            "AwakeningCourtyard/spawn_to_jump_step packed source CollisionShape2D offset fails the named surface-support gate");
    }

    private static void AssertPackedPlayerColliderOffsetMutationFailsClosed(Node2D player)
    {
        CollisionShape2D bodyCollider = player.GetNode<CollisionShape2D>("BodyCollider");
        Vector2 originalPosition = bodyCollider.Position;
        PrologueRoom room = GD.Load<PackedScene>(
            "res://scenes/prologue/rooms/AwakeningCourtyard.tscn").Instantiate<PrologueRoom>();
        TraversalSegment2D segment = room.GetNode<TraversalSegment2D>(
            "TraversalContracts/spawn_to_jump_step");
        bool failedByNamedSurfaceGate = false;
        try
        {
            bodyCollider.Position += new Vector2(300f, 0f);
            Rect2 offsetBodyExtents = CapsuleBoundsInAncestor(bodyCollider, player, "Player/BodyCollider");
            AssertTraversalSurface("AwakeningCourtyard", room, segment, offsetBodyExtents);
        }
        catch (InvalidOperationException error)
        {
            failedByNamedSurfaceGate = error.Message.Contains(
                "AwakeningCourtyard/spawn_to_jump_step launch sample", StringComparison.Ordinal) &&
                error.Message.Contains("Geometry/SpawnFloor", StringComparison.Ordinal);
        }
        finally
        {
            bodyCollider.Position = originalPosition;
            room.Free();
        }

        TestAssert.True(failedByNamedSurfaceGate,
            "AwakeningCourtyard/spawn_to_jump_step transformed Player/BodyCollider offset fails the named surface-support gate");
    }

    private static void AssertPackedAssistedTargetAncestorMutationFailsClosed()
    {
        PrologueRoom room = GD.Load<PackedScene>(
            "res://scenes/prologue/rooms/BrokenToothCorridor.tscn").Instantiate<PrologueRoom>();
        TraversalSegment2D segment = room.GetNode<TraversalSegment2D>(
            "TraversalContracts/entry_to_anchor_landing");
        Node2D geometry = room.GetNode<Node2D>("Geometry");
        geometry.Position += new Vector2(-600f, 0f);
        bool failedByNamedAnchorGate = false;
        try
        {
            AssertAssistedTarget("BrokenToothCorridor", room, segment);
        }
        catch (InvalidOperationException error)
        {
            failedByNamedAnchorGate = error.Message.Contains(
                "BrokenToothCorridor/entry_to_anchor_landing anchor", StringComparison.Ordinal) &&
                (error.Message.Contains("400 px hold range", StringComparison.Ordinal) ||
                 error.Message.Contains("landing-facing half-plane", StringComparison.Ordinal));
        }
        finally
        {
            room.Free();
        }

        TestAssert.True(failedByNamedAnchorGate,
            "BrokenToothCorridor/entry_to_anchor_landing transformed Geometry ancestor affects the room-local anchor gate");
    }

    private static void AssertTransformedCollisionBoundsMath()
    {
        var room = new Node2D { Name = "BoundsRoom" };
        var geometry = new Node2D
        {
            Name = "Geometry", Position = new Vector2(100f, 50f), Scale = new Vector2(2f, 3f)
        };
        var body = new StaticBody2D { Name = "Platform", Position = new Vector2(10f, -5f) };
        var collision = new CollisionShape2D
        {
            Name = "Collision", Position = new Vector2(4f, 2f), Scale = new Vector2(.5f, 2f),
            Shape = new RectangleShape2D { Size = new Vector2(20f, 10f) }
        };
        body.AddChild(collision);
        geometry.AddChild(body);
        room.AddChild(geometry);
        TestAssert.Equal(new Rect2(118f, 11f, 20f, 60f),
            RectangleBoundsInAncestor(body, room, "focused transformed rectangle"),
            "rectangle bounds include collider offset/scale plus body and ancestor transforms");

        collision.Rotation = .1f;
        bool rotationFailedNamed = false;
        try
        {
            RectangleBoundsInAncestor(body, room, "focused rotated rectangle");
        }
        catch (InvalidOperationException error)
        {
            rotationFailedNamed = error.Message.Contains("focused rotated rectangle", StringComparison.Ordinal) &&
                                  error.Message.Contains("axis-aligned", StringComparison.Ordinal);
        }
        collision.Rotation = 0f;
        TestAssert.True(rotationFailedNamed,
            "rotated/skewed collision transforms fail with a named axis-aligned diagnostic");

        geometry.Position = new Vector2(float.NaN, 50f);
        bool nonFiniteFailedNamed = false;
        try
        {
            RectangleBoundsInAncestor(body, room, "focused non-finite rectangle");
        }
        catch (InvalidOperationException error)
        {
            nonFiniteFailedNamed = error.Message.Contains("focused non-finite rectangle", StringComparison.Ordinal) &&
                                   error.Message.Contains("finite", StringComparison.Ordinal);
        }
        TestAssert.True(nonFiniteFailedNamed,
            "non-finite collision transforms fail with a named finite-transform diagnostic");
        room.Free();

        var player = new Node2D { Name = "Player" };
        var holder = new Node2D
        {
            Name = "ColliderHolder", Position = new Vector2(3f, -2f), Scale = new Vector2(2f, .5f)
        };
        var bodyCollider = new CollisionShape2D
        {
            Name = "BodyCollider", Position = new Vector2(4f, 6f), Scale = new Vector2(.5f, 2f),
            Shape = new CapsuleShape2D { Radius = 10f, Height = 40f }
        };
        holder.AddChild(bodyCollider);
        player.AddChild(holder);
        TestAssert.Equal(new Rect2(1f, -19f, 20f, 40f),
            CapsuleBoundsInAncestor(bodyCollider, player, "focused transformed player capsule"),
            "player body-origin extents include BodyCollider offset/scale and ancestor transforms");
        player.Free();
    }

    private static Rect2 RectangleBoundsInAncestor(StaticBody2D body, Node2D ancestor, string label)
    {
        CollisionShape2D collision = body.GetNodeOrNull<CollisionShape2D>("Collision");
        TestAssert.True(collision != null, $"{label} resolves a CollisionShape2D at Collision");
        TestAssert.True(collision.Shape is RectangleShape2D,
            $"{label} collision shape is a supported RectangleShape2D");
        var rectangle = (RectangleShape2D)collision.Shape;
        TestAssert.True(float.IsFinite(rectangle.Size.X) && rectangle.Size.X > 0f &&
                        float.IsFinite(rectangle.Size.Y) && rectangle.Size.Y > 0f,
            $"{label} rectangle dimensions are finite and positive");
        return AxisAlignedBoundsInAncestor(collision, ancestor, rectangle.Size * .5f, label);
    }

    private static Rect2 CapsuleBoundsInAncestor(CollisionShape2D collision, Node2D ancestor, string label)
    {
        TestAssert.True(collision.Shape is CapsuleShape2D,
            $"{label} collision shape is a supported CapsuleShape2D");
        var capsule = (CapsuleShape2D)collision.Shape;
        TestAssert.True(float.IsFinite(capsule.Radius) && capsule.Radius > 0f &&
                        float.IsFinite(capsule.Height) && capsule.Height > 0f,
            $"{label} capsule dimensions are finite and positive");
        return AxisAlignedBoundsInAncestor(collision, ancestor,
            new Vector2(capsule.Radius, capsule.Height * .5f), label);
    }

    private static Rect2 AxisAlignedBoundsInAncestor(Node2D node, Node2D ancestor, Vector2 halfExtents,
        string label)
    {
        Transform2D transform = TransformToAncestor(node, ancestor, label);
        TestAssert.True(IsFinite(transform), $"{label} transform into {ancestor.Name} is finite");
        const float epsilon = .0001f;
        TestAssert.True(Mathf.Abs(transform.X.Y) <= epsilon && Mathf.Abs(transform.Y.X) <= epsilon,
            $"{label} transform into {ancestor.Name} is axis-aligned; rotation/skew is unsupported");
        TestAssert.True(Mathf.Abs(transform.X.X) > epsilon && Mathf.Abs(transform.Y.Y) > epsilon,
            $"{label} transform into {ancestor.Name} has non-zero axis-aligned scale");
        Vector2[] corners =
        [
            transform * new Vector2(-halfExtents.X, -halfExtents.Y),
            transform * new Vector2(halfExtents.X, -halfExtents.Y),
            transform * new Vector2(halfExtents.X, halfExtents.Y),
            transform * new Vector2(-halfExtents.X, halfExtents.Y)
        ];
        return Bounds(corners);
    }

    private static Vector2 PositionInAncestor(Node2D node, Node2D ancestor, string label)
    {
        Vector2 position = TransformToAncestor(node, ancestor, label).Origin;
        TestAssert.True(float.IsFinite(position.X) && float.IsFinite(position.Y),
            $"{label} position in {ancestor.Name} is finite");
        return position;
    }

    private static Transform2D TransformToAncestor(Node2D node, Node2D ancestor, string label)
    {
        Transform2D result = Transform2D.Identity;
        Node current = node;
        while (current != null && !ReferenceEquals(current, ancestor))
        {
            TestAssert.True(current is Node2D,
                $"{label} transform chain to {ancestor.Name} contains only Node2D ancestors");
            result = ((Node2D)current).Transform * result;
            current = current.GetParent();
        }
        TestAssert.True(ReferenceEquals(current, ancestor),
            $"{label} transform chain reaches ancestor {ancestor.Name}");
        return result;
    }

    private static bool IsFinite(Transform2D transform) =>
        float.IsFinite(transform.X.X) && float.IsFinite(transform.X.Y) &&
        float.IsFinite(transform.Y.X) && float.IsFinite(transform.Y.Y) &&
        float.IsFinite(transform.Origin.X) && float.IsFinite(transform.Origin.Y);

    private static bool Contains(System.Collections.Generic.IReadOnlyList<string> errors, string text)
    {
        foreach (string error in errors)
            if (error.Contains(text, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private static Vector2 PolygonSize(Vector2[] points)
        => Bounds(points).Size;

    private static float RadialSpan(Vector2[] points)
    {
        float minimum = float.PositiveInfinity;
        float maximum = 0f;
        foreach (Vector2 point in points)
        {
            minimum = Mathf.Min(minimum, point.Length());
            maximum = Mathf.Max(maximum, point.Length());
        }
        return maximum - minimum;
    }

    private static Rect2 Bounds(Vector2[] points)
    {
        float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;
        foreach (Vector2 point in points)
        {
            minX = Mathf.Min(minX, point.X);
            minY = Mathf.Min(minY, point.Y);
            maxX = Mathf.Max(maxX, point.X);
            maxY = Mathf.Max(maxY, point.Y);
        }
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }

    private static string ToKebab(string value)
    {
        var result = new System.Text.StringBuilder();
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (index > 0 && char.IsUpper(character)) result.Append('-');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }

    private sealed record ExpectedTraversalSegment(string RoomName, string Id, TraversalSegmentKind Kind,
        TraversalAbility Required, string AssistPath);

    private sealed record ExpectedTraversalSurface(string RoomName, string Id, string SourcePath,
        string TargetPath);

    private sealed record ExpectedRoomVisual(PrologueRoomId Id, string RoomName, string Landmark);
}
