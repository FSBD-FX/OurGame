using System;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Mechanisms;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.World;
using Knight.Interactions;

namespace Knight.Build.Prologue;

/// <summary>
/// Owns the active, replaceable graybox content for the five canyon sectors. Common sector roots,
/// streaming seams, recovery volumes, packing, and validation remain in
/// <see cref="PrologueCanyonBuildParts"/>.
/// </summary>
public static class CanyonContentBuilder
{
    public static void Build(PrologueSector root)
    {
        ArgumentNullException.ThrowIfNull(root);
        root.SetMeta("canyon_layout_version", CanyonWorldLayout.CurrentLayoutVersion);
        ConfigureReplaceableArtLayers(root);
        AddMicroSceneMarkers(root);
        EnsureContainer(root.GetNode("Gameplay"), "Mechanisms");
        switch (root.SectorId)
        {
            case PrologueSectorId.CanyonEntrance:
                BuildEntrance(root);
                break;
            case PrologueSectorId.CanyonDescent:
                BuildDescent(root);
                break;
            case PrologueSectorId.CanyonBasin:
                BuildBasin(root);
                break;
            case PrologueSectorId.CanyonAscent:
                BuildAscent(root);
                break;
            case PrologueSectorId.LostTemple:
                BuildTemple(root);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(root.SectorId), root.SectorId, null);
        }
        AddQuestFragments(root);
        AddEncounterGroups(root);
    }

    private static void AddQuestFragments(PrologueSector root)
    {
        (string Suffix, Vector2 WorldCenter)[] placements = root.SectorId switch
        {
            PrologueSectorId.CanyonEntrance => [("entrance", new(280f, 600f))],
            PrologueSectorId.CanyonDescent =>
            [
                ("rift", new(1460f, 1040f)),
                ("abyss-floor", new(2240f, 3524f)),
                ("archive", new(1520f, 1604f))
            ],
            PrologueSectorId.CanyonBasin =>
            [
                ("gear-gallery", new(3160f, 3720f)),
                ("steam-loop", new(3380f, 4200f))
            ],
            PrologueSectorId.CanyonAscent => [("ascent-mid", new(4730f, 3044f))],
            PrologueSectorId.LostTemple =>
            [
                ("courtyard", new(5320f, 1204f)),
                ("temple-upper", new(5896f, 924f))
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(root.SectorId), root.SectorId, null)
        };
        Node2D container = EnsureContainer(root.GetNode("Gameplay"), "QuestFragments");
        Vector2 offset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        foreach ((string suffix, Vector2 worldCenter) in placements)
        {
            var fragment = new QuestFragment
            {
                Name = suffix,
                StableId = "canyon.v2.fragment." + suffix,
                Position = worldCenter - offset,
                CollisionLayer = CombatCollisionLayers.None,
                CollisionMask = CombatCollisionLayers.Player,
                Monitoring = true,
                Monitorable = false
            };
            fragment.AddChild(new CollisionShape2D
            {
                Name = "Collision", Shape = new CircleShape2D { Radius = 18f }
            });
            var visual = new QuestFragmentVisual { Name = "AvailableVisual" };
            visual.SetMeta("replaceable_art_layer", true);
            visual.SetMeta("gameplay_authority", false);
            fragment.AddChild(visual);
            fragment.AddChild(new Label
            {
                Name = "Status", Position = new Vector2(-90f, -48f),
                CustomMinimumSize = new Vector2(180f, 30f),
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
            container.AddChild(fragment);
        }
    }

    private static void ConfigureReplaceableArtLayers(PrologueSector root)
    {
        Node2D visualRoot = root.GetNode<Node2D>("VisualRoot");
        foreach (string path in RoomVisualCatalog.StableSectorVisualRoots)
        {
            Node layer = root.GetNode(path);
            layer.SetMeta("replaceable_art_layer", true);
            layer.SetMeta("gameplay_authority", false);
        }

        Node2D background = visualRoot.GetNode<Node2D>("Background");
        Polygon2D canyonVoid = background.GetNode<Polygon2D>("CanyonVoid");
        canyonVoid.Color = RoomVisualCatalog.CanyonSkirtBase;
        Vector2 sectorWorldOffset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        Rect2 coverageWorld = RoomVisualCatalog.ContinuousCanyonCoverageBounds();
        background.AddChild(new CanyonBackdropVisual
        {
            Name = "CanyonCoverageSkirt",
            SectorId = root.SectorId,
            SectorBounds = new Rect2(coverageWorld.Position - sectorWorldOffset,
                coverageWorld.Size),
            CoverageLayer = true,
            VisualRole = "coverage",
            ZAsRelative = false,
            ZIndex = -100
        });

        Node2D compositions = EnsureContainer(visualRoot.GetNode("Midground"),
            "ChallengeCompositions");
        foreach (CanyonRouteNodeDefinition definition in CanyonWorldLayout.Nodes
                     .Where(node => node.SectorId == root.SectorId && node.ChallengeKind.HasValue))
        {
            CanyonChallengeKind challenge = definition.ChallengeKind.Value;
            string compositionId = RoomVisualCatalog.CanyonCompositionId(challenge);
            var composition = new CanyonBackdropVisual
            {
                Name = challenge.ToString(),
                SectorId = root.SectorId,
                VisualRole = "challenge",
                CompositionId = compositionId,
                UseCanyonDepthStaging =
                    (root.SectorId == PrologueSectorId.CanyonEntrance && compositionId == "broken-entrance-bell") ||
                    (root.SectorId == PrologueSectorId.CanyonDescent && compositionId == "staggered-belfry-ribs"),
                CompositionBounds = new Rect2(
                    definition.WorldBounds.Position - sectorWorldOffset,
                    definition.WorldBounds.Size),
                AccentColor = ChallengeAccent(challenge)
            };
            composition.SetMeta("challenge_kind", (int)challenge);
            composition.SetMeta("height_layer", HeightLayer(definition.WorldBounds));
            composition.SetMeta("silhouette_id", compositionId);
            composition.SetMeta("collision_source", false);
            compositions.AddChild(composition);
        }

        AddForegroundDepth(root);
    }

    private static int HeightLayer(Rect2 worldBounds)
    {
        float centerY = worldBounds.GetCenter().Y;
        if (centerY <= 1600f) return 2;
        return centerY < 3500f ? 1 : 0;
    }

    private static Color ChallengeAccent(CanyonChallengeKind kind) => kind switch
    {
        CanyonChallengeKind.FrozenDetour => new Color("628a9a"),
        CanyonChallengeKind.SteamValves => new Color("6f8f8b"),
        CanyonChallengeKind.SealedCourtyard or CanyonChallengeKind.BossHall => new Color("80624b"),
        _ => new Color("796541")
    };

    private static void AddForegroundDepth(PrologueSector root)
    {
        Rect2 bounds = new(Vector2.Zero,
            CanyonWorldLayout.SectorBounds[root.SectorId].Size);
        Node2D occluders = EnsureContainer(root.GetNode("VisualRoot/Foreground"), "DepthOccluders");
        float depth = Mathf.Min(96f, bounds.Size.Y * .13f);
        float shoulder = Mathf.Min(150f, bounds.Size.X * .12f);
        Color near = new("17252a", .66f);
        var left = new Polygon2D
        {
            Name = "NearLeft",
            Polygon =
            [
                bounds.Position, new Vector2(bounds.Position.X + shoulder, bounds.Position.Y),
                new Vector2(bounds.Position.X + shoulder * .52f, bounds.Position.Y + depth),
                new Vector2(bounds.Position.X, bounds.Position.Y + depth * .72f)
            ],
            Color = near
        };
        left.SetMeta("occlusion_class", "foreground-safe-edge");
        var right = new Polygon2D
        {
            Name = "NearRight",
            Polygon =
            [
                new Vector2(bounds.End.X - shoulder, bounds.Position.Y),
                new Vector2(bounds.End.X, bounds.Position.Y),
                new Vector2(bounds.End.X, bounds.Position.Y + depth * .76f),
                new Vector2(bounds.End.X - shoulder * .46f, bounds.Position.Y + depth)
            ],
            Color = near
        };
        right.SetMeta("occlusion_class", "foreground-safe-edge");
        // The bridge-to-rift traversal uses the Entrance upper-right and Descent upper-left
        // corners. Keep those actor envelopes open while retaining the opposite near-depth
        // shoulder in each sector; gameplay seam geometry remains authoritative and unchanged.
        if (root.SectorId != PrologueSectorId.CanyonDescent) occluders.AddChild(left);
        else left.Free();
        if (root.SectorId != PrologueSectorId.CanyonEntrance) occluders.AddChild(right);
        else right.Free();
    }

    private static void AddMicroSceneMarkers(PrologueSector root)
    {
        Node markers = EnsureContainer(root.GetNode("Gameplay"), "MicroScenes");
        Node2D readability = EnsureContainer(root.GetNode("VisualRoot/GameplayReadable"),
            "MicroSceneReadability");
        foreach (CanyonMicroSceneDefinition definition in CanyonContentCatalog.ForSector(root.SectorId))
        {
            var marker = new Marker2D
            {
                Name = definition.Id,
                Position = definition.LocalBounds.GetCenter()
            };
            marker.SetMeta("local_bounds", definition.LocalBounds);
            marker.SetMeta("optional_subarea", definition.OptionalSubarea);
            marker.SetMeta("route_node_id", (int)definition.RouteNodeId);
            marker.SetMeta("challenge_kind", definition.ChallengeKind.HasValue ? (int)definition.ChallengeKind.Value : -1);
            markers.AddChild(marker);

            // Keep guidance authority, but let the continuous entry/rift art carry its
            // own color: whole-region inset washes create an artificial sector gutter.
            if (root.SectorId is PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent)
                continue;

            Color color = definition.OptionalSubarea
                ? new Color(0.52f, 0.69f, 0.60f, .055f)
                : new Color(0.72f, 0.58f, 0.30f, .04f);
            readability.AddChild(RectPolygon(definition.Id, definition.LocalBounds.GetCenter(),
                definition.LocalBounds.Size - new Vector2(8f, 8f), color));
        }
    }

    // All dimensions below are WORLD coordinates inside the immutable Task1 rectangles.
    // Background pixels remain independent; Solid owns the actual body and matching skin.
    private static StaticBody2D Solid(PrologueSector root, string name, Rect2 world,
        GrappleSurfaceMaterial material = GrappleSurfaceMaterial.GripStone, bool catchShelf = false,
        bool oneWay = false)
    {
        Vector2 offset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        StaticBody2D body = Platform(root, name, world.GetCenter() - offset, world.Size, material, catchShelf, oneWay);
        body.SetMeta(MinimapTerrainCatalogBuilder.StructuralMarker, MinimapTerrainCatalogBuilder.Provenance);
        return body;
    }

    private static void BuildEntrance(PrologueSector root)
    {
        // A visible permanent canyon edge, not an off-screen recovery substitute.
        Solid(root, "EntranceOuterWall", new(0,0,48,720));
        Solid(root, "StartFloor", new(0,636,320,84));
        Solid(root, "BridgeFarBank", new(400,636,880,84));
        Solid(root, "LowBridgeCeiling", new(250,484,150,24));
        // Exterior ledge under an overhang, then a236px-clear rock mouth. The
        // former536px upward staircase contradicted entering a descending canyon.
        Solid(root, "EntranceSkyOverhang", new(48,0,552,80));
        Solid(root, "RiftEntryCeiling", new(600,0,680,400));
        Marker(root, "Spawn", CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.start"]);
        RouteMarker(root, "NextSeam", new(1278,600));
        GroundEdge(root, "bridge-to-rift", new(1190,564,70,72), new(1300,564,120,72));
    }

    private static void BuildDescent(PrologueSector root)
    {
        // Connected upper rift:196px net between shelves instead of76px. Close
        // the east dead-end all the way to the roof, never a painted/no-collision wall.
        Solid(root, "RiftRoof", new(1280,0,1280,400));
        Solid(root, "RiftOuterRightWall", new(2524,400,36,896));
        Solid(root, "RiftArchiveOuterWall", new(1280,720,48,960));
        Solid(root, "RiftUpperFloor", new(1280,636,620,24), catchShelf:true);
        Solid(root, "RiftMiddleCatch", new(1500,856,700,24), catchShelf:true);
        // Keep72px between the wall's1328 face and shelf1400, admitting the
        // unchanged40px capsule into the authored Archive branch.
        Solid(root, "RiftLowerCatch", new(1400,1076,700,24), catchShelf:true);
        Solid(root, "RiftAlternationWall", new(2200,400,36,700));
        Solid(root, "AbyssCommitment", new(2080,1296,120,40), catchShelf:true);
        var updraft = new CanyonUpdraft
        {
            Name = "AbyssUpdraft", Position = new Vector2(1956,1360) -
                CanyonWorldLayout.SectorBounds[root.SectorId].Position,
            Bounds = new Rect2(0,0,534,2120), DescentSpeed = 500, BrakingAcceleration = 3000,
            ZIndex = 2
        };
        updraft.AddChild(new Label
        {
            Name = "EntryCue", Position = new Vector2(70,-205),
            Text = "↑ 上升气流 · 下落减缓", MouseFilter = Control.MouseFilterEnum.Ignore,
            Modulate = new Color("a0c5b8")
        });
        updraft.GetNode<Label>("EntryCue").AddThemeFontSizeOverride("font_size",16);
        root.GetNode("Gameplay/Mechanisms").AddChild(updraft);
        for (int i = 0; i < CanyonWorldLayout.AbyssBrakeWallSegments.Count; i++)
        {
            Rect2 right = CanyonWorldLayout.AbyssBrakeWallSegments[i];
            Solid(root, $"AbyssBrakeRight{i + 1}", right);
            // The left brake face is continuous until the final safe-bottom egress.
            // Its last 160px is open so a 72px capsule can leave the bottom for Basin.
            Solid(root, $"AbyssBrakeLeft{i + 1}", new(1920, right.Position.Y,36,
                right.Size.Y - (i == CanyonWorldLayout.AbyssBrakeWallSegments.Count - 1 ? 160 : 0)));
        }
        for (int i = 0; i < CanyonWorldLayout.AbyssSideExits.Count; i++)
            Solid(root, $"AbyssExit{i + 1}", CanyonWorldLayout.AbyssSideExits[i], catchShelf:true);
        StaticBody2D abyssLanding = Solid(root, "AbyssSafeBottom", CanyonWorldLayout.AbyssSafeBottom, catchShelf:true);
        // Only the unavoidable long-descent landing has a visible padded surface. Ordinary
        // catch shelves, checkpoints and grapple stone retain normal height-scaled injury.
        abyssLanding.SetMeta("soft_landing", true);
        Node2D landingSkin = root.GetNode<Node2D>("VisualRoot/GameplayReadable/AbyssSafeBottom");
        var softPad = new Node2D { Name = "SoftLandingPad" };
        softPad.AddChild(RectPolygon("PaddedRim", new Vector2(0f, -17f),
            new Vector2(640f, 6f), new Color("94d4a5")));
        for (int index = 0; index < 8; index++)
        {
            softPad.AddChild(new Polygon2D
            {
                Name = $"Cushion{index + 1}", Position = new Vector2(-280f + index * 80f, -7f),
                Polygon = [new(-36f,-10f), new(36f,-10f), new(39f,-5f),
                    new(34f,9f), new(-34f,9f), new(-39f,-5f)],
                Color = new Color(index % 2 == 0 ? "477e68" : "588b6c")
            });
        }
        landingSkin.AddChild(softPad);

        // Archive and frozen branch occupy the left column, never the passive shaft.
        Solid(root, "ArchiveEntry", new(1280,1260,300,40), catchShelf:true);
        // Full192px charger motion needs220px of supported body width. Extend
        // left only, keeping the supply-roof edge and152px left descent open.
        Solid(root, "ArchiveShelf", new(1480,1420,240,40), catchShelf:true);
        // Deliberate72px branch exit beside the wall, never a one-way floor hack.
        Solid(root, "ArchiveFloor", new(1400,1640,480,40));
        Solid(root, "ArchiveSupplyRoof", new(1720,1400,160,24));
        Solid(root, "ArchiveSupplyRightWall", new(1880,1400,40,280));
        AddArchivePressureGateAndSupply(root);
        // Match the descending opening. The old1320 edge required a zero-margin
        // ice-top detour outside the sector for the40px body; retain the right end.
        Solid(root, "FrozenCeiling", new(1400,1840,440,36), GrappleSurfaceMaterial.Ice);
        // Enclosed resident pockets, not a new open shaft or shifted old spawn.
        // The140px local recess keeps the old Sentry alternate body's1290 edge
        // clear; a36px full-height wall would cut through that existing actor.
        Solid(root, "FrozenOuterUpperWall", new(1280,1800,36,280));
        Solid(root, "FrozenSentryRecessWall", new(1280,2080,8,140));
        Solid(root, "FrozenOuterLowerWall", new(1280,2220,36,1140));
        Solid(root, "FrozenLeftCatch", new(1280,2180,230,40), catchShelf:true);
        Solid(root, "FrozenUpperResidentShelf", new(1316,2400,368,40), catchShelf:true);
        Solid(root, "FrozenRightCatch", new(1580,2640,340,40), catchShelf:true);
        //80px admits both the40px capsule and the actual wall-claw dock48px
        // from the wall, leaving12px beyond dock+radius instead of8px overlap.
        Solid(root, "FrozenLowerResidentShelf", new(1540,2880,300,40), catchShelf:true);
        AddSteamDiverter(root, "SteamDiverterFrozen", "canyon.v2.steam-diverter.frozen",
            new(1740, 2500), new(1660, 2540), new(1820, 2540), new(56, 150), "FrozenRightCatch");
        Solid(root, "FrozenExit", new(1280,3280,560,40), catchShelf:true);
        AddMechanism(root, "HiddenPanel", CanyonClawMechanismKind.HiddenPanel, new(160,1440));
        RouteMarker(root, "PreviousSeam", new(2,600));
        RouteMarker(root, "NextSeam", new(620,3598));
        // The original forward egress only opened x1804..1996 in world space.
        // A legitimate camp W+K return crosses the same bottom edge farther west
        // (measured body center x1676, real capsule radius20). Keep a bounded,
        // overlapping return corridor, not a global KillZone or motion exemption.
        RouteMarker(root, "BasinReturnSeam", new(432,3598));
        GroundEdge(root, "rift-to-abyss", new(1420,1004,60,72), new(2110,1224,70,72));
        GroundEdge(root, "abyss-to-basin-camp", new(1960,3488,120,72), new(1820,3924,180,72));
        GroundEdge(root, "rift-to-archive", new(1420,1004,60,72), new(1348,1188,120,72));
        Edge(root, "archive-to-frozen-detour", new(1440,1568,120,72), new(1320,2108,140,72));
        Edge(root, "frozen-detour-to-basin-camp", new(1640,3208,160,72), new(1560,3924,240,72));
        EnsureContainer(root.GetNode("Gameplay"), "Actors");
    }

    private static void BuildBasin(PrologueSector root)
    {
        Solid(root, "BasinCampFloor", new(1280,3996,800,40));
        Solid(root, "GearEntry", new(2080,3996,300,40));
        // Preserve top/bottom and the camp approach. A missed upper carriage
        // needs48px between this right edge and the deck's left edge so the
        // unchanged40px capsule can fall into the original return channel.
        Solid(root, "GearPistonOne", new(2440,3916,212,80), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "GearPistonTwo", new(2844,3836,196,120), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "UpperGearGalleryOne", new(3040,3756,240,40));
        Solid(root, "UpperGearGalleryTwo", new(3280,3676,240,40));
        Solid(root, "UpperGearGalleryThree", new(3520,3600,320,40));
        AddGearPiston(root);
        Solid(root, "SteamTrenchFloor", new(3040,4236,1160,40), GrappleSurfaceMaterial.TempleMetal);
        AddSteamValveAndJet(root);
        AddSteamDiverter(root, "SteamDiverterTrench", "canyon.v2.steam-diverter.trench",
            new(3990, 4070), new(3890, 4110), new(4110, 4110), new(64, 150), "SteamTrenchFloor");
        for (int i = 0; i < CanyonWorldLayout.SteamRejoinSteps.Count; i++)
            Solid(root, $"SteamRejoinStep{i + 1}", CanyonWorldLayout.SteamRejoinSteps[i], GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "SteamRejoinTop", CanyonWorldLayout.SteamRejoinTop, GrappleSurfaceMaterial.TempleMetal);
        // Lower return bypass is below the piston tops and above the recovery envelope.
        // Preserve its authored walking top at4116. The lower SteamValves lane
        // needs92px native clearance for the unchanged84px Laborer capsule.
        Solid(root, "ShortcutBasinWalkway", new(2080,4116,1720,28));
        Solid(root, "ShortcutCampReturnStep", new(2380,4076,80,40), catchShelf:true);
        BuildShortcutStairs(root, 4116, 3636);
        var checkpoint = PrologueBuildParts.Checkpoint(root.GetNode("Gameplay"),
            CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.basin-camp"] -
                CanyonWorldLayout.SectorBounds[root.SectorId].Position, "canyon.v2.basin-camp");
        checkpoint.Name = "BasinCheckpoint";
        AddBasinCounterweightLift(root);
        RouteMarker(root, "PreviousSeam", new(620,2));
        RouteMarker(root, "NextSeam", new(2460,2));
        Edge(root, "basin-camp-to-gears", new(1920,3924,120,72), new(2120,3924,180,72));
        Edge(root, "gears-to-lift-high", new(2840,3764,160,72), new(3880,3512,220,72));
        Edge(root, "gears-to-steam-valves", new(2840,3764,160,72), new(3100,4164,240,72));
        Edge(root, "steam-valves-to-lift", new(3560,4164,160,72), new(4940,3528,140,72));
        Edge(root, "basin-to-upper-shortcut", new(1920,3924,120,72), new(4100,1408,120,72),
            "GearEntry");
        ClawDropEdge(root, "basin-camp-to-abyss", new(1660,3924,120,72), new(2160,3488,160,72),
            "../CanyonDescent/Gameplay/Collision/AbyssBrakeLeft4/GrappleSurfaceProfile",
            new(-18,290.7278f), Vector2.Left,
            "../CanyonDescent/Gameplay/Collision/AbyssSafeBottom");
        var shieldLane = Trigger("ShieldLane", new(980,330), new(260,120));
        root.GetNode("Gameplay").AddChild(shieldLane);
        shieldLane.SetScript(GD.Load<Script>("res://scripts/prologue/traversal/CanyonShieldLaneHazard.cs"));
    }

    private static void BuildAscent(PrologueSector root)
    {
        // Public junction: main-route access must remain open before shortcut unlock.
        // The branch gate belongs above this catch, never across these access steps.
        Solid(root, "LiftEntryFloor", new(3840,3584,160,16), catchShelf:true);
        Solid(root, "LiftAccessStepOne", new(4320,3556,160,40), catchShelf:true);
        Solid(root, "LiftAccessStepTwo", new(4500,3496,160,40), catchShelf:true);
        Solid(root, "LiftBackLanding", new(4600,3460,200,40), catchShelf:true);
        Solid(root, "LiftBackLandingGrip", new(4680,3290,36,60), GrappleSurfaceMaterial.TempleMetal);
        // Separate from the shortcut's x3840..4250 lane. Near-side inset grips dock
        // the full capsule 40px inside these 240px catch shelves, not at their edge.
        for (int i = 0; i < 9; i++)
        {
            float x = i % 2 == 0 ? 4260 : 4660;
            float y = 3320-i*240;
            Solid(root, $"AscentGrip{i + 1}", new(x,3320-i*240,i == 1 ? 140 : 240,40),
                GrappleSurfaceMaterial.TempleMetal, catchShelf:true);
            // Raise Grip2's post for upper-stop walk-off headroom while keeping lateral docking.
            Solid(root, $"AscentGrip{i + 1}Grip",
                new(i % 2 == 0 ? x + 116 : x + 88,y - (i == 1 ? 240 : 160),36,60),
                GrappleSurfaceMaterial.TempleMetal);
        }
        Solid(root, "PendulumCeiling", new(4900,2070,220,40), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "SplitUpperGallery", new(4080,1480,280,40), catchShelf:true);
        Solid(root, "AscentFinalCatch", new(4660,1320,240,40), GrappleSurfaceMaterial.TempleMetal, catchShelf:true);
        Solid(root, "AscentFinalCatchGrip", new(4748,1160,36,60), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "CourtyardApproach", new(4980,1240,140,40), GrappleSurfaceMaterial.TempleMetal);
        BuildShortcutStairs(root, 3476, 1556);
        AddCounterweightLift(root);
        AddPendulum(root);
        AddUpperBasinShortcutWinch(root);
        AddSealPrerequisites(root);
        RouteMarker(root, "PreviousSeam", new(70,2878));
        RouteMarker(root, "PublicJunctionWestSeam", new(220,2878));
        RouteMarker(root, "PublicJunctionEastSeam", new(400,2878));
        RouteMarker(root, "SteamRejoinSeam", new(1152,2878));
        RouteMarker(root, "SteamRejoinJumpSeam", new(1056,2878));
        RouteMarker(root, "NextSeam", new(1278,484));
        Edge(root, "lift-to-pendulums", new(4660,3008,160,72), new(4400,2288,80,72), "AscentGrip3");
        Edge(root, "pendulums-to-split-level", new(4660,2048,160,72), new(4400,1328,80,72), "AscentGrip7");
        Edge(root, "split-level-to-courtyard", new(4400,1328,80,72), new(5180,1168,220,72), "AscentFinalCatchGrip");
        Edge(root, "upper-to-basin-shortcut", new(4100,1408,120,72), new(1840,3924,200,72));
    }

    private static void BuildShortcutStairs(PrologueSector root, int bottom, int top)
    {
        for (int y = bottom, i = 0; y >= top; y -= 160, i++)
        {
            float x = i % 2 == 0 ? 3840 : 4100;
            Solid(root, $"ShortcutStep{y}", y==3636 ? new(4100,y,200,32) : new(x,y,150,y==3476 ? 16 : 32), catchShelf:true)
                .SetMeta("public_main_junction", y==3636);
            if(y != 4116)
                // The low grip docks above its own shelf without pulling the capsule
                // through the next shelf. Its underside retains an 80px passage.
                Solid(root, $"ShortcutStep{y}Grip", new(i%2==0 ? x : x+114,y-140,36,60));
        }
    }

    private static void BuildTemple(PrologueSector root)
    {
        Solid(root, "CourtyardFloor", new(5120,1240,520,40), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "CheckpointFloor", new(5680,1116,400,40), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "CourtyardStep", new(5540,1196,200,40), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "ArenaFloor", new(6080,1116,1600,40), GrappleSurfaceMaterial.TempleMetal);
        AddCourtyardSeal(root);
        Solid(root, "BossCeilingGrip", new(6300,840,600,40), GrappleSurfaceMaterial.TempleMetal);
        var checkpoint = PrologueBuildParts.Checkpoint(root.GetNode("Gameplay"),
            CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.temple-forecourt"] -
                CanyonWorldLayout.SectorBounds[root.SectorId].Position, "canyon.v2.temple-forecourt");
        checkpoint.Name = "TempleCheckpoint";
        var mechanisms = root.GetNode<Node2D>("Gameplay/Mechanisms");
        var chain = GD.Load<PackedScene>("res://scenes/mechanisms/BellChainMechanism.tscn").Instantiate<BellChainMechanism>();
        chain.Name = "BellChain";
        chain.ShortcutId = "canyon.v2.temple-bell-gate";
        chain.Position = new(560,250);
        chain.DoorHeight = 240;
        mechanisms.AddChild(chain);
        AddMechanism(root, "BellBody", CanyonClawMechanismKind.BellBody, new(940,240));
        Node gameplay = root.GetNode("Gameplay");
        var doors = new Node2D { Name = "Doors" };
        doors.AddChild(Door("Entry", new(1040,306)));
        doors.AddChild(Door("Exit", new(2180,306)));
        // The short combat door is not an admission boundary: a claw/jump could
        // otherwise pass over its roof. The finite pre-fight hull overlaps the
        // original ArenaFloor at both lower corners and its own top at both upper
        // corners. All three shapes open together; the original combat geometry
        // and ceiling-grapple beam remain unchanged after admission.
        Vector2 offset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        var admission = new StaticBody2D
        {
            Name = "AdmissionBarrier", Position = new Vector2(6160, 938) - offset,
            CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None
        };
        admission.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile", Material = GrappleSurfaceMaterial.Forbidden,
            UseCollisionHitPoint = true
        });
        admission.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(36, 436) }
        });
        admission.AddChild(new CollisionShape2D
        {
            Name = "TopCollision", Position = new Vector2(760, -198),
            Shape = new RectangleShape2D { Size = new Vector2(1556, 40) }
        });
        admission.AddChild(new CollisionShape2D
        {
            Name = "RightCollision", Position = new Vector2(1520, 0),
            Shape = new RectangleShape2D { Size = new Vector2(36, 436) }
        });
        doors.AddChild(admission);
        gameplay.AddChild(doors);
        gameplay.AddChild(new Node2D { Name = "BossRoot" });
        gameplay.AddChild(Trigger("ActivationTrigger", new(1140,306), new(90,180)));
        gameplay.AddChild(new BossArenaController
        {
            Name = "BossArenaController",
            BossScene = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn"),
            BossRootPath = new("../BossRoot"), EntryDoorCollisionPath = new("../Doors/Entry/Collision"),
            ExitDoorCollisionPath = new("../Doors/Exit/Collision"), ActivationTriggerPath = new("../ActivationTrigger"),
            BossSpawnPosition = new(1600,360), RequiresFragmentAdmission = true
        });
        RouteMarker(root, "PreviousSeam", new(2,484));
        Marker(gameplay, "BossApproachStart", new(940,360));
        Edge(root, "courtyard-to-boss-hall", new(5880,1044,180,72), new(6320,1044,180,72));
        Edge(root, "boss-hall-to-safe-exit", new(7140,1044,120,72), new(7360,1044,240,72));
        EnsureContainer(gameplay, "Actors");
        AddTemplePressureGateAndSupply(root);
    }

    private static void AddGearPiston(PrologueSector root)
    {
        Node2D telegraphs = EnsureContainer(root.GetNode("VisualRoot/GameplayReadable"), "MechanismTelegraphs");
        telegraphs.AddChild(RectPolygon("GearPistonTelegraph", new Vector2(1320, 250),
            new Vector2(132, 18), new Color("d6a341")));
        var piston = new CanyonGearPiston
        {
            Name = "GearPiston", RetractedPosition = new Vector2(1320, 360),
            ExtendedPosition = new Vector2(1320, 260), RouteBounds = new Rect2(1240, 220, 160, 180),
            SafeSeconds = 1.4f, TelegraphSeconds = .55f, ActiveSeconds = .65f,
            RecoverySeconds = .6f,
            TelegraphPath = new NodePath("../../../VisualRoot/GameplayReadable/MechanismTelegraphs/GearPistonTelegraph")
        };
        AddMovingCollision(piston, new Vector2(132, 32), "PistonFace", new Color("a87834"));
        root.GetNode("Gameplay/Mechanisms").AddChild(piston);
    }

    private static void AddSteamValveAndJet(PrologueSector root)
    {
        Node2D telegraphs = EnsureContainer(root.GetNode("VisualRoot/GameplayReadable"), "MechanismTelegraphs");
        Polygon2D telegraph = RectPolygon("SteamJetTelegraph", new Vector2(2180, 510),
            new Vector2(72, 150), new Color(0.54f, .76f, .75f, .54f));
        Polygon2D active = RectPolygon("SteamJetActive", new Vector2(2180, 510),
            new Vector2(72, 150), new Color(.70f, .94f, .96f, .82f));
        telegraph.ZIndex = 1;
        active.ZIndex = 1;
        AddSteamShape(telegraph, new Color(.77f, .92f, .89f, .45f), 10f);
        AddSteamShape(active, new Color(.91f, 1f, 1f, .78f), 17f);
        telegraphs.AddChild(telegraph);
        telegraphs.AddChild(active);
        var jet = new CanyonSteamJet
        {
            Name = "SteamJet", Position = new Vector2(2180, 510), SafeSeconds = 1.25f,
            TelegraphSeconds = .45f, ActiveSeconds = .7f,
            TelegraphPath = new NodePath("../../../VisualRoot/GameplayReadable/MechanismTelegraphs/SteamJetTelegraph"),
            ActiveVisualPath = new NodePath("../../../VisualRoot/GameplayReadable/MechanismTelegraphs/SteamJetActive")
        };
        jet.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(72, 150) } });
        root.GetNode("Gameplay/Mechanisms").AddChild(jet);

        var valve = new CanyonSteamValve
        {
            Name = "SteamValve", Position = new Vector2(1880, 520),
            ActivationId = "canyon.v2.steam-valve-lower", PairedJetPath = new NodePath("../SteamJet")
        };
        valve.AddChild(MechanicalWheel("ValveWheel", Vector2.Zero, 27f, new Color("8f7344")));
        root.GetNode("Gameplay/Mechanisms").AddChild(valve);
        AddStaticControl(root, "SteamValveControl", new Vector2(1880, 440),
            new NodePath("../../Mechanisms/SteamValve"), manualSteamValve: true);
    }

    private static void AddSteamDiverter(PrologueSector root, string name, string mechanismId,
        Vector2 controlWorld, Vector2 leftWorld, Vector2 rightWorld, Vector2 jetSize,
        string supportBodyName)
    {
        Vector2 offset = PrologueCanyonBuildParts.WorldOffsets[root.SectorId];
        Vector2 controlLocal = controlWorld - offset;
        Vector2 leftLocal = leftWorld - offset;
        Vector2 rightLocal = rightWorld - offset;
        var controller = new CanyonSteamDiverter
        {
            Name = name, Position = controlLocal, MechanismId = mechanismId
        };
        foreach ((string childName, Vector2 localCenter) in new[]
                 { ("LeftJet", leftLocal), ("RightJet", rightLocal) })
        {
            var jet = new CanyonSteamJet
            {
                Name = childName, Position = localCenter - controlLocal, Monitoring = false
            };
            jet.AddChild(new CollisionShape2D
            {
                Name = "Collision", Shape = new RectangleShape2D { Size = jetSize }, Disabled = true
            });
            jet.AddChild(new Node2D { Name = "Telegraph", Visible = false });
            jet.AddChild(new Node2D { Name = "ActiveVisual", Visible = false });
            controller.AddChild(jet);
        }
        root.GetNode("Gameplay/Mechanisms").AddChild(controller);
        AddStaticControl(root, name + "Control", controlLocal,
            new NodePath("../../Mechanisms/" + name));
        Node2D telegraphs = EnsureContainer(root.GetNode("VisualRoot/GameplayReadable"), "MechanismTelegraphs");
        telegraphs.AddChild(new CanyonSteamDiverterVisual
        {
            Name = name + "Visual", ZIndex = 1,
            ControllerPath = new NodePath("../../../../Gameplay/Mechanisms/" + name),
            ControlPath = new NodePath("../../../../Gameplay/GrappleSurfaces/" + name + "Control"),
            SupportShapePath = new NodePath("../../../../Gameplay/Collision/" + supportBodyName + "/Collision")
        });
        // The optional visual support borrows the actual floor; gameplay stays authoritative.
    }

    private static void AddSteamShape(Polygon2D root, Color color, float sway)
    {
        root.AddChild(CirclePolygon("LowerVapor", new Vector2(-sway * .28f, 45f), 18f, color));
        root.AddChild(CirclePolygon("MiddleVapor", new Vector2(sway * .36f, 3f), 22f,
            new Color(color, color.A * .82f)));
        root.AddChild(CirclePolygon("UpperVapor", new Vector2(-sway * .24f, -48f), 15f,
            new Color(color, color.A * .60f)));
    }

    private static void AddCounterweightLift(PrologueSector root)
    {
        var lift = new CanyonCounterweightLift
        {
            Name = "CounterweightLift", LowerPosition = new Vector2(1000, 2820),
            UpperPosition = new Vector2(1000, 2300), TravelSeconds = 1.8f,
            EndpointDwellSeconds = 1f
        };
        AddMovingCollision(lift, new Vector2(80, 24), "Carriage", new Color("90713d"));
        root.GetNode("Gameplay/Mechanisms").AddChild(lift);
        AddStaticControl(root, "CounterweightLiftControl", new Vector2(1260, 2700),
            new NodePath("../../Mechanisms/CounterweightLift"));
    }

    private static void AddBasinCounterweightLift(PrologueSector root)
    {
        var lift = new CanyonCounterweightLift
        {
            Name = "BasinCounterweightLift", LowerPosition = new Vector2(1480, 408),
            UpperPosition = new Vector2(1480, 256), TravelSeconds = 2f,
            EndpointDwellSeconds = 2f
        };
        AddMovingCollision(lift, new Vector2(120, 24), "Carriage", new Color("90713d"));
        root.GetNode("Gameplay/Mechanisms").AddChild(lift);
        AddStaticControl(root, "BasinCounterweightControl", new Vector2(1212, 216),
            new NodePath("../../Mechanisms/BasinCounterweightLift"));
        PointTarget(root, "BasinCounterweightGalleryAnchor", new Vector2(1832, 24),
            GrappleSurfaceMaterial.DedicatedAnchor, "basin-gallery-transfer", 24f);
    }

    private static void AddPendulum(PrologueSector root)
    {
        Node2D telegraphs = EnsureContainer(root.GetNode("VisualRoot/GameplayReadable"), "MechanismTelegraphs");
        telegraphs.AddChild(RectPolygon("PendulumTelegraph", new Vector2(1080, 1510),
            new Vector2(300, 24), new Color(0.85f, .54f, .24f, .68f)));
        var pendulum = new CanyonPendulumHazard
        {
            Name = "PendulumHazard", Position = new Vector2(1080, 1370),
            PeriodSeconds = 2.4f, MaxAngleDegrees = 58f,
            TelegraphPath = new NodePath("../../../VisualRoot/GameplayReadable/MechanismTelegraphs/PendulumTelegraph")
        };
        var collision = new CollisionShape2D { Name = "Collision", Position = new Vector2(0, 110),
            Shape = new RectangleShape2D { Size = new Vector2(28, 220) } };
        pendulum.AddChild(collision);
        pendulum.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.Forbidden, UseCollisionHitPoint = true });
        pendulum.AddChild(RectPolygon("VisiblePendulum", new Vector2(0, 110),
            new Vector2(28, 220), new Color("a56f3d")));
        root.GetNode("Gameplay/Mechanisms").AddChild(pendulum);
    }

    private static void AddUpperBasinShortcutWinch(PrologueSector root)
    {
        Node2D readable = EnsureContainer(root.GetNode("VisualRoot/GameplayReadable"), "MechanismTelegraphs");
        // Fill the complete non-public shortcut shaft while locked. A narrow divider still
        // permits same-side drops along the alternating shelves; this shutter ends above the
        // public junction and stays left of the independent main-ascent supports.
        Polygon2D closed = RectPolygon("UpperBasinShortcutClosed", new Vector2(205, 1765),
            new Vector2(410, 1930), new Color("202a2f"));
        closed.ZIndex = 1;
        var panels = new Node2D { Name = "RecessedPanels" };
        for (int index = 0; index < 4; index++)
        {
            panels.AddChild(RectPolygon($"Panel{index + 1}",
                new Vector2(-120 + index * 80, 0), new Vector2(62, 1790),
                index % 2 == 0 ? new Color("2b373c") : new Color("263238")));
        }
        closed.AddChild(panels);
        closed.AddChild(RectPolygon("LeftRail", new Vector2(-176, 0), new Vector2(26, 1880),
            new Color("59666a")));
        closed.AddChild(RectPolygon("RightRail", new Vector2(176, 0), new Vector2(26, 1880),
            new Color("59666a")));
        var braces = new Node2D { Name = "CrossBraces" };
        for (int index = -4; index <= 4; index++)
        {
            float y = index * 190f;
            Polygon2D brace = RectPolygon($"Brace{index + 5}", new Vector2(0, y),
                new Vector2(340, 18), new Color("485257"));
            brace.Rotation = index % 2 == 0 ? .16f : -.16f;
            braces.AddChild(brace);
        }
        closed.AddChild(braces);
        readable.AddChild(closed);
        readable.AddChild(RectPolygon("UpperBasinShortcutOpen", new Vector2(170, 2540),
            new Vector2(80, 12), new Color("79aa83")));
        var winch = new CanyonShortcutWinch
        {
            Name = "UpperBasinShortcutWinch", Position = Vector2.Zero,
            ShortcutId = "canyon.v2.upper-basin-shortcut",
            CorridorBounds = new Rect2(0, 780, 410, 2060),
            ClosedVisualPath = new NodePath("../../../VisualRoot/GameplayReadable/MechanismTelegraphs/UpperBasinShortcutClosed"),
            OpenVisualPath = new NodePath("../../../VisualRoot/GameplayReadable/MechanismTelegraphs/UpperBasinShortcutOpen")
        };
        var barrier = new StaticBody2D { Name = "Barrier", Position = new Vector2(205, 1765),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None };
        barrier.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(410, 1930) } });
        barrier.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.Forbidden, UseCollisionHitPoint = true });
        winch.AddChild(barrier);
        winch.AddChild(MechanicalWheel("WinchWheel", new Vector2(500, 700), 32f,
            new Color("8e7240")));
        root.GetNode("Gameplay/Mechanisms").AddChild(winch);
        AddStaticControl(root, "UpperBasinShortcutControl", new Vector2(500, 700),
            new NodePath("../../Mechanisms/UpperBasinShortcutWinch"));
    }

    private static void AddSealPrerequisites(PrologueSector root)
    {
        var linkage = new CanyonSteamValve
        {
            Name = "CourtyardSealLinkage", Position = new Vector2(1080, 620),
            ActivationId = "canyon.v2.courtyard-crank", PairedJetPath = new NodePath()
        };
        linkage.AddChild(MechanicalWheel("LinkageWheel", Vector2.Zero, 29f,
            new Color("a47b42")));
        root.GetNode("Gameplay/Mechanisms").AddChild(linkage);
        AddStaticControl(root, "CourtyardSealLinkageControl", new Vector2(1080, 540),
            new NodePath("../../Mechanisms/CourtyardSealLinkage"));
    }

    private static void AddEncounterGroups(PrologueSector root)
    {
        Node2D gameplay = root.GetNode<Node2D>("Gameplay");
        Node2D actors = EnsureContainer(gameplay, "Actors");
        if (root.SectorId == PrologueSectorId.CanyonEntrance)
        {
            CanyonEncounterDefinition first = CanyonContentCatalog.EncountersForSector(root.SectorId)
                .Single(encounter => encounter.RouteNode == CanyonRouteNodeId.LowCeilingBridge);
            var guidance = new Marker2D
            {
                Name = "RustShellSquire",
                Position = first.Enemies[0].WorldPosition -
                    CanyonWorldLayout.SectorBounds[root.SectorId].Position
            };
            guidance.SetMeta("guidance_only", true);
            actors.AddChild(guidance);
        }
        Node2D groups = EnsureContainer(gameplay, "Encounters");
        foreach (CanyonEncounterDefinition definition in
                 CanyonContentCatalog.EncountersForSector(root.SectorId))
        {
            groups.AddChild(new CanyonEncounterGroup
            {
                Name = EncounterNodeName(definition.StableId),
                EncounterId = definition.StableId,
                Definition = definition,
                ActorRootPath = new NodePath("../../Actors")
            });
        }
    }

    private static string EncounterNodeName(string stableId) => stableId switch
    {
        "canyon.v2.encounter.low-ceiling-bridge" => "LowCeilingBridgeEncounter",
        "canyon.v2.encounter.staggered-rift" => "StaggeredRiftEncounter",
        "canyon.v2.encounter.controlled-abyss" => "ControlledAbyssEncounter",
        "canyon.v2.encounter.gear-pistons" => "GearPistonsEncounter",
        "canyon.v2.encounter.counterweight-lift" => "CounterweightLiftEncounter",
        "canyon.v2.encounter.pendulum-gallery" => "PendulumGalleryEncounter",
        "canyon.v2.encounter.split-level" => "SplitLevelEncounter",
        "canyon.v2.encounter.sealed-courtyard" => "SealedCourtyardEncounter",
        "canyon.v2.encounter.archive-side-cave" => "ArchiveSideCaveEncounter",
        "canyon.v2.encounter.frozen-upper" => "FrozenUpperEncounter",
        "canyon.v2.encounter.steam-valves" => "SteamValvesEncounter",
        "canyon.v2.encounter.frozen-lower" => "FrozenLowerEncounter",
        _ => throw new ArgumentOutOfRangeException(nameof(stableId), stableId,
            "Unknown Task6 encounter stable ID")
    };

    private static void AddCourtyardSeal(PrologueSector root)
    {
        Node2D readable = EnsureContainer(root.GetNode("VisualRoot/GameplayReadable"), "MechanismTelegraphs");
        readable.AddChild(RectPolygon("CourtyardSealClosed", new Vector2(398, 250),
            new Vector2(36, 500), new Color("9e623e")));
        readable.AddChild(RectPolygon("CourtyardSealOpen", new Vector2(398, 70),
            new Vector2(90, 12), new Color("72a77f")));
        var seal = new CanyonSealGate
        {
            Name = "CourtyardSeal", Position = new Vector2(398, 250),
            RequiredMechanismId = "canyon.v2.courtyard-crank",
            RequiredEncounterId = "canyon.v2.encounter.split-level",
            ActivationId = "canyon.v2.courtyard-seal-open",
            ClosedVisualPath = new NodePath("../../../VisualRoot/GameplayReadable/MechanismTelegraphs/CourtyardSealClosed"),
            OpenVisualPath = new NodePath("../../../VisualRoot/GameplayReadable/MechanismTelegraphs/CourtyardSealOpen")
        };
        seal.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(36, 500) } });
        seal.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.Forbidden, UseCollisionHitPoint = true });
        root.GetNode("Gameplay/Mechanisms").AddChild(seal);
    }

    private static void AddArchivePressureGateAndSupply(PrologueSector root)
    {
        Vector2 offset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        var gate = new CanyonPressurePlateGate { Name = "ArchivePressureGate" };
        var door = new StaticBody2D
        {
            Name = "Door", Position = new Vector2(1740f, 1532f) - offset,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        // The streamer snapshots authored collision flags before _Ready and restores them
        // at activation. Provisional open is therefore the only safe authored baseline;
        // the gate commits a closed shape after its first real occupancy check.
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Disabled = true,
            Shape = new RectangleShape2D { Size = new Vector2(40f, 216f) }
        });
        door.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile", Material = GrappleSurfaceMaterial.Forbidden,
            UseCollisionHitPoint = true
        });
        gate.AddChild(door);
        gate.AddChild(new CanyonPressureGateVisual { Name = "ReadableGate" });
        root.GetNode("Gameplay/Mechanisms").AddChild(gate);

        var reward = new PrologueSupplyCache
        {
            Name = "ArchiveSupply", Position = new Vector2(1820f, 1604f) - offset,
            DiscoveryId = "canyon.v2.reward.archive-supply", HealthAmount = 2,
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true, Monitorable = false
        };
        reward.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 28f }
        });
        var available = new Node2D { Name = "AvailableVisual" };
        available.AddChild(MechanicalPanel("SupplyMark", Vector2.Zero,
            new Vector2(44f, 36f), new Color("d7bc72")));
        available.AddChild(RectPolygon("HealingCross", Vector2.Zero,
            new Vector2(8f, 25f), new Color("f0ead5")));
        available.AddChild(RectPolygon("HealingCrossBar", Vector2.Zero,
            new Vector2(25f, 8f), new Color("f0ead5")));
        reward.AddChild(available);
        var spent = new Node2D { Name = "SpentVisual", Visible = false };
        spent.AddChild(MechanicalPanel("EmptySupply", Vector2.Zero,
            new Vector2(44f, 36f), new Color("655c4c")));
        reward.AddChild(spent);
        reward.AddChild(new Label
        {
            Name = "Status", Position = new Vector2(-90f, -56f),
            CustomMinimumSize = new Vector2(180f, 30f),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
        EnsureContainer(root.GetNode("Gameplay"), "Rewards").AddChild(reward);
    }

    private static void AddTemplePressureGateAndSupply(PrologueSector root)
    {
        // Connected upper candidate only; old corridor, seal, checkpoint and Boss remain intact.
        Solid(root, "TempleSupplyShelf", new(5865, 960, 215, 40), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "TempleSupplyGripBeam", new(5805, 820, 50, 40), GrappleSurfaceMaterial.TempleMetal);
        Solid(root, "TempleSupplyBeamSupport", new(5835, 760, 20, 70), GrappleSurfaceMaterial.Forbidden);
        Solid(root, "TempleSupplyRightWall", new(6060, 760, 20, 200), GrappleSurfaceMaterial.Forbidden);
        Solid(root, "TempleSupplyCeiling", new(5536, 720, 784, 40), GrappleSurfaceMaterial.Forbidden);
        Solid(root, "TempleSupplyLeftPier", new(5536, 760, 20, 240), GrappleSurfaceMaterial.Forbidden);
        Solid(root, "TempleSupplyEasternSoffit", new(6215, 760, 105, 120), GrappleSurfaceMaterial.Forbidden);
        Vector2 offset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        var gate = new CanyonPressurePlateGate
        {
            Name = "TemplePressureGate", MechanismId = "canyon.v2.pressure-gate.temple-reward",
            PlateWorldRect = new Rect2(5868, 900, 60, 60),
            InteriorWorldRect = new Rect2(5960, 760, 100, 200),
            AntiCrushWorldRect = new Rect2(5910, 760, 70, 200)
        };
        var door = new StaticBody2D
        {
            Name = "Door", Position = new Vector2(5945, 860) - offset,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        // Preserve the reused safe mount baseline; only the real gate commits closure.
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Disabled = true,
            Shape = new RectangleShape2D { Size = new Vector2(30, 200) }
        });
        door.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile", Material = GrappleSurfaceMaterial.Forbidden,
            UseCollisionHitPoint = true
        });
        gate.AddChild(door);
        gate.AddChild(new CanyonPressureGateVisual { Name = "ReadableGate" });
        root.GetNode("Gameplay/Mechanisms").AddChild(gate);

        var reward = new PrologueSupplyCache
        {
            Name = "TempleSupply", Position = new Vector2(6010, 924) - offset,
            DiscoveryId = "canyon.v2.reward.temple-supply", HealthAmount = 2,
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true, Monitorable = false
        };
        reward.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 28f }
        });
        var available = new Node2D { Name = "AvailableVisual" };
        available.AddChild(MechanicalPanel("SupplyMark", Vector2.Zero,
            new Vector2(44f, 36f), new Color("d7bc72")));
        available.AddChild(RectPolygon("HealingCross", Vector2.Zero,
            new Vector2(8f, 25f), new Color("f0ead5")));
        available.AddChild(RectPolygon("HealingCrossBar", Vector2.Zero,
            new Vector2(25f, 8f), new Color("f0ead5")));
        reward.AddChild(available);
        var spent = new Node2D { Name = "SpentVisual", Visible = false };
        spent.AddChild(MechanicalPanel("EmptySupply", Vector2.Zero,
            new Vector2(44f, 36f), new Color("655c4c")));
        reward.AddChild(spent);
        reward.AddChild(new Label
        {
            Name = "Status", Position = new Vector2(-90f, -56f),
            CustomMinimumSize = new Vector2(180f, 30f),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
        EnsureContainer(root.GetNode("Gameplay"), "Rewards").AddChild(reward);
    }

    private static void AddMovingCollision(CollisionObject2D body, Vector2 size, string visualName, Color color)
    {
        body.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new RectangleShape2D { Size = size } });
        body.AddChild(new GrappleSurface2D { Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.Forbidden, UseCollisionHitPoint = true });
        body.AddChild(MechanicalPanel(visualName, Vector2.Zero, size, color));
    }

    private static void AddStaticControl(PrologueSector root, string name, Vector2 position,
        NodePath receiverPath, bool manualSteamValve = false)
    {
        var target = new ThreadTarget2D
        {
            Name = name, Position = position, Kind = ThreadTargetKind.Mechanism,
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius,
            PullReceiverPath = receiverPath
        };
        target.SetMeta("target_role", "mechanism-control");
        var area = new Area2D { Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None, Monitoring = true, Monitorable = true };
        area.AddChild(new CollisionShape2D { Name = "Collision",
            Shape = new CircleShape2D { Radius = 22f } });
        target.AddChild(area);
        target.AddChild(new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter",
            TargetPath = new NodePath("..") });
        if (manualSteamValve)
            target.AddChild(new ManualSteamValveControl { Name = "ManualSteamValveControl" });
        root.GetNode("Gameplay/GrappleSurfaces").AddChild(target);
        var visual = new CanyonAnchorVisual
        {
            Name = name, Position = position, SurfaceMaterial = GrappleSurfaceMaterial.DedicatedAnchor,
            Radius = 22f
        };
        visual.SetMeta("material_family",
            RoomVisualCatalog.CanyonMaterialFamily(GrappleSurfaceMaterial.DedicatedAnchor));
        root.GetNode("VisualRoot/GameplayReadable").AddChild(visual);
    }

    private static void GroundEdge(PrologueSector root, string shortId, Rect2 launch, Rect2 landing)
    {
        Vector2 offset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        Segment(root, "canyon.v2.route." + shortId, TraversalSegmentKind.GroundTransfer,
            TraversalAbility.None, new Rect2(launch.Position - offset, launch.Size),
            new Rect2(landing.Position - offset, landing.Size));
    }

    private static void ClawDropEdge(PrologueSector root, string shortId, Rect2 launch, Rect2 landing,
        string targetPath, Vector2 collisionLocalPoint, Vector2 collisionLocalNormal, string supportPath)
    {
        Vector2 offset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        TraversalSegment2D segment = Segment(root, "canyon.v2.route." + shortId,
            TraversalSegmentKind.ClawDrop, TraversalAbility.Thread,
            new Rect2(launch.Position - offset, launch.Size), new Rect2(landing.Position - offset, landing.Size),
            new NodePath(targetPath));
        segment.AssistHitPoint = collisionLocalPoint;
        segment.AssistHitNormal = collisionLocalNormal;
        segment.LandingSupportPath = new NodePath(supportPath);
    }

    private static void Edge(PrologueSector root, string shortId, Rect2 launch, Rect2 landing,
        string assistBody = null)
    {
        Vector2 offset = CanyonWorldLayout.SectorBounds[root.SectorId].Position;
        Segment(root, "canyon.v2.route." + shortId,
            assistBody == null ? TraversalSegmentKind.SingleJump : TraversalSegmentKind.Assisted,
            assistBody == null ? TraversalAbility.Jump : TraversalAbility.Jump | TraversalAbility.Thread,
            new Rect2(launch.Position - offset, launch.Size), new Rect2(landing.Position - offset, landing.Size),
            assistBody == null ? new NodePath() : new NodePath("Gameplay/Collision/" + assistBody + "/GrappleSurfaceProfile"));
    }

    private static CanyonClawMechanism2D AddMechanism(PrologueSector root, string name,
        CanyonClawMechanismKind kind, Vector2 position)
    {
        var mechanism = new CanyonClawMechanism2D
        {
            Name = name,
            Kind = kind,
            Position = position,
            LinkedNodePath = new NodePath("LinkedNode")
        };
        var linked = new Node2D { Name = "LinkedNode" };
        linked.AddChild(MechanicalPanel("ReadableMechanism", Vector2.Zero, new Vector2(72, 72),
            kind == CanyonClawMechanismKind.HiddenPanel
                ? new Color("6c725d")
                : new Color("9a7640")));
        mechanism.AddChild(linked);

        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget",
            Position = new Vector2(0, -64),
            Kind = ThreadTargetKind.Mechanism,
            Material = GrappleSurfaceMaterial.DedicatedAnchor,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius,
            PullReceiverPath = new NodePath("..")
        };
        target.SetMeta("target_role", "mechanism");
        var collision = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        collision.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 28f }
        });
        target.AddChild(collision);
        target.AddChild(new LegacyThreadInteractionAdapter
        {
            Name = "LegacyThreadInteractionAdapter",
            TargetPath = new NodePath("..")
        });
        mechanism.AddChild(target);
        root.GetNode("Gameplay/Mechanisms").AddChild(mechanism);
        return mechanism;
    }

    private static StaticBody2D Platform(PrologueSector root, string name, Vector2 center,
        Vector2 size, GrappleSurfaceMaterial material, bool catchShelf = false,
        bool oneWay = false)
    {
        if (material == GrappleSurfaceMaterial.DedicatedAnchor)
            throw new ArgumentOutOfRangeException(nameof(material),
                "collision bodies require an explicit terrain material");
        var body = new StaticBody2D
        {
            Name = name,
            Position = center,
            CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None
        };
        body.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile",
            Material = material,
            UseCollisionHitPoint = true
        });
        body.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = size },
            OneWayCollision = oneWay,
            OneWayCollisionMargin = oneWay ? 16f : 1f
        });
        if (catchShelf) body.SetMeta("catch_shelf", true);
        root.GetNode("Gameplay/Collision").AddChild(body);

        var visual = new CanyonPlatformVisual
        {
            Name = name, Position = center, SkinSize = size,
            SurfaceMaterial = material, CatchShelf = catchShelf, SectorId = root.SectorId,
            UseCanyonDepthStaging = material == GrappleSurfaceMaterial.GripStone &&
                (root.SectorId is PrologueSectorId.CanyonEntrance or PrologueSectorId.CanyonDescent)
        };
        visual.SetMeta("material_family", RoomVisualCatalog.CanyonMaterialFamily(material));
        visual.AddChild(RectPolygon("ReadableEdge", new Vector2(0, -size.Y / 2f + 3),
            new Vector2(size.X, 6), RoomVisualCatalog.CanyonReadableEdge(root.SectorId, material)));
        root.GetNode("VisualRoot/GameplayReadable").AddChild(visual);
        return body;
    }

    private static GrappleSurface2D PointTarget(PrologueSector root, string name,
        Vector2 position, GrappleSurfaceMaterial material, string role, float radius = 24f)
    {
        var surface = new GrappleSurface2D
        {
            Name = name,
            Position = position,
            Material = material,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius,
            UseCollisionHitPoint = false
        };
        surface.SetMeta("target_role", role);
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true,
            Monitorable = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = radius }
        });
        surface.AddChild(area);
        root.GetNode("Gameplay/GrappleSurfaces").AddChild(surface);
        var visual = new CanyonAnchorVisual
        {
            Name = name, Position = position, SurfaceMaterial = material, Radius = radius
        };
        visual.SetMeta("material_family", RoomVisualCatalog.CanyonMaterialFamily(material));
        root.GetNode("VisualRoot/GameplayReadable").AddChild(visual);
        return surface;
    }

    private static TraversalSegment2D Segment(PrologueSector root, string id,
        TraversalSegmentKind kind, TraversalAbility abilities, Rect2 launch, Rect2 landing,
        NodePath assist = default)
    {
        TraversalSegment2D segment = PrologueBuildParts.TraversalSegment(
            root.GetNode("Gameplay/TraversalContracts"), id, kind, abilities, launch, landing,
            assist ?? new NodePath());
        segment.Name = id.ValidateNodeName();
        segment.AddChild(ZonePolygon("LaunchZoneVisual", launch,
            new Color(0.36f, .72f, .62f, .32f)));
        segment.AddChild(ZonePolygon("LandingZoneVisual", landing,
            new Color(.82f, .66f, .32f, .34f)));
        return segment;
    }

    private static Polygon2D ZonePolygon(string name, Rect2 zone, Color color) => new()
    {
        Name = name,
        // Keep the authored route bounds inspectable without presenting test volumes as
        // floating platforms. First-level guidance supplies the actual player-facing cues.
        Visible = false,
        Polygon =
        [
            zone.Position, new Vector2(zone.End.X, zone.Position.Y), zone.End,
            new Vector2(zone.Position.X, zone.End.Y)
        ],
        Color = color
    };

    private static void RouteMarker(PrologueSector root, string name, Vector2 position) =>
        Marker(root.GetNode("Gameplay/RouteMarkers"), name, position);

    private static Marker2D Marker(Node parent, string name, Vector2 position)
    {
        var marker = new Marker2D { Name = name, Position = position };
        parent.AddChild(marker);
        return marker;
    }

    private static Node2D OptionalPath(PrologueSector root, string name)
    {
        var path = new Node2D { Name = name };
        root.GetNode("Gameplay/OptionalPaths").AddChild(path);
        return path;
    }

    private static Area2D Trigger(string name, Vector2 position, Vector2 size)
    {
        var trigger = new Area2D
        {
            Name = name,
            Position = position,
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        trigger.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = size }
        });
        return trigger;
    }

    private static StaticBody2D Door(string name, Vector2 position)
    {
        var door = new StaticBody2D
        {
            Name = name,
            Position = position,
            CollisionLayer = CombatCollisionLayers.World | CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None
        };
        door.AddChild(new GrappleSurface2D
        {
            Name = "GrappleSurfaceProfile",
            Material = GrappleSurfaceMaterial.Forbidden,
            UseCollisionHitPoint = true
        });
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(36, 180) }
        });
        door.AddChild(MechanicalPanel("ReadableDoor", Vector2.Zero, new Vector2(36, 180),
            new Color("8e6a46")));
        return door;
    }

    private static Node2D EnsureContainer(Node parent, string name)
    {
        Node2D existing = parent.GetNodeOrNull<Node2D>(name);
        if (existing != null) return existing;
        var created = new Node2D { Name = name };
        parent.AddChild(created);
        return created;
    }

    private static Polygon2D RectPolygon(string name, Vector2 center, Vector2 size,
        Color color) => new()
    {
        Name = name,
        Position = center,
        Polygon =
        [
            new Vector2(-size.X / 2f, -size.Y / 2f),
            new Vector2(size.X / 2f, -size.Y / 2f),
            new Vector2(size.X / 2f, size.Y / 2f),
            new Vector2(-size.X / 2f, size.Y / 2f)
        ],
        Color = color
    };

    private static Polygon2D MechanicalPanel(string name, Vector2 center, Vector2 size,
        Color color)
    {
        Polygon2D panel = RectPolygon(name, center, size, color.Darkened(.30f));
        Vector2 faceSize = new(Mathf.Max(4f, size.X - 8f), Mathf.Max(4f, size.Y - 8f));
        panel.AddChild(RectPolygon("FacingPlane", new Vector2(0f, -2f), faceSize, color));
        if (size.Y >= 18f)
            panel.AddChild(RectPolygon("ContactShadow", new Vector2(0f, size.Y * .5f - 5f),
                new Vector2(Mathf.Max(4f, size.X - 8f), 6f), color.Darkened(.48f)));
        if (size.X >= 34f && size.Y >= 24f)
        {
            Color bolt = new("c2af76", .72f);
            panel.AddChild(CirclePolygon("BoltLeft", new Vector2(-size.X * .5f + 8f,
                -size.Y * .5f + 8f), 2.5f, bolt));
            panel.AddChild(CirclePolygon("BoltRight", new Vector2(size.X * .5f - 8f,
                -size.Y * .5f + 8f), 2.5f, bolt));
        }
        return panel;
    }

    private static Polygon2D MechanicalWheel(string name, Vector2 center, float radius,
        Color color)
    {
        Polygon2D wheel = CirclePolygon(name, center, radius, color.Darkened(.38f));
        wheel.AddChild(CirclePolygon("Face", Vector2.Zero, radius * .78f, color));
        wheel.AddChild(CirclePolygon("Hub", Vector2.Zero, radius * .19f,
            new Color("223137")));
        for (int index = 0; index < 4; index++)
        {
            Polygon2D spoke = RectPolygon($"Spoke{index + 1}", Vector2.Zero,
                new Vector2(radius * 1.42f, 4f), color.Lightened(.13f));
            spoke.Rotation = index * Mathf.Pi / 4f;
            wheel.AddChild(spoke);
        }
        return wheel;
    }

    private static Polygon2D CirclePolygon(string name, Vector2 center, float radius,
        Color color)
    {
        var points = new Vector2[20];
        for (int index = 0; index < points.Length; index++)
        {
            float angle = Mathf.Tau * index / points.Length;
            points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return new Polygon2D { Name = name, Position = center, Polygon = points, Color = color };
    }
}
