using Godot;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Tools;
using Knight.Ui;
using Knight.Vfx;
using Knight.Audio;
using Knight.Build.Ui;
using Knight.Prologue.Seamless;
using Knight.Presentation.Minimap;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace Knight.Build.Prologue;

public partial class BuildPrologueLevel : SceneTree
{
    public override void _Initialize()
    {
        try
        {
        PrologueLevelController level = Build();
        var definitions = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors").Definitions.ToArray();
        MinimapTerrainCatalog catalog = level.GetNode<PrologueMinimapController>("MinimapController").Catalog;
        Error error = SceneBuildUtil.PackAndSaveValidated(level, "res://scenes/prologue/PrologueLevel.tscn", saved =>
        {
            ValidateSavedLevel((PrologueLevelController)saved);
            var actual = saved.GetNode<PrologueMinimapController>("MinimapController").Catalog;
            if (actual == null || actual.Revision != catalog.Revision ||
                !actual.SerializedValues.AsSpan().SequenceEqual(catalog.SerializedValues))
                throw new System.InvalidOperationException("Saved catalog geometry/order/revision differs.");
            var actualCheckpoints = saved.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints;
            var expectedCheckpoints = MinimapTerrainCatalogBuilder.Checkpoints(definitions);
            if (actualCheckpoints.Count != expectedCheckpoints.Count ||
                expectedCheckpoints.Any(pair => !actualCheckpoints.TryGetValue(pair.Key, out var pos) || pos != pair.Value))
                throw new System.InvalidOperationException("Saved authored checkpoints differ.");
        });
        level.Free();
        if (error != Error.Ok) { GD.PushError($"BuildPrologueLevel failed: {error}"); Quit(1); return; }
        GD.Print($"BuildPrologueLevel: catalog cells={catalog.CellCount} solids={catalog.SolidCount} boundaries={catalog.BoundaryCount} revision={catalog.Revision}");
        GD.Print("BuildPrologueLevel: five-sector authored canyon route packed and validated.");
        Quit(0);
        }
        finally
        {
            // Preserve existing builder finalization behavior.
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }
    }

    // Same production graph, with explicit saved paths for staged authoring rather than publication.
    public static PrologueLevelController Build(IReadOnlyDictionary<PrologueSectorId, string> scenePaths = null)
    {
        if (scenePaths != null && (scenePaths.Count != Enum.GetValues<PrologueSectorId>().Length ||
            Enum.GetValues<PrologueSectorId>().Any(id => !scenePaths.TryGetValue(id, out string path) ||
                string.IsNullOrWhiteSpace(path) || !(path.StartsWith("res://", StringComparison.Ordinal) || Path.IsPathFullyQualified(path)))))
            throw new ArgumentException("Supply exactly five explicit saved sector paths.", nameof(scenePaths));
        PackedScene playerScene = GD.Load<PackedScene>("res://scenes/player/Player.tscn");
        var level = new PrologueLevelController
        {
            Name = "PrologueLevel",
            MilestoneCompletionExitId = "p0_complete",
            MilestoneCompletionObjective = "P0 CORE SLICE COMPLETE"
        };
        try
        {
        var playerRoot = new Node2D { Name = "PlayerRoot" };
        CharacterBody2D player = playerScene.Instantiate<CharacterBody2D>();
        player.Name = "Player";
        player.Position = CanyonWorldLayout.CheckpointWorldPositions["canyon.v2.start"];
        player.CollisionLayer = CombatCollisionLayers.Player;
        player.CollisionMask = CombatCollisionLayers.World;
        player.AddToGroup("player", true);
        playerRoot.AddChild(player);
        level.AddChild(playerRoot);
        var worldRoot = new Node2D { Name = "WorldRoot" };
        var sectors = new SeamlessPrologueWorld
        {
            Name = "Sectors",
            PlayerPath = new NodePath("../../PlayerRoot/Player"),
            InitialSector = PrologueSectorId.CanyonEntrance,
            LayoutVersion = CanyonWorldLayout.CurrentLayoutVersion
        };
        worldRoot.AddChild(sectors);
        var fireflyGuidance = new CanyonFireflyGuidanceController
        {
            Name = "FireflyGuidance",
            ProcessMode = Node.ProcessModeEnum.Always
        };
        fireflyGuidance.AddChild(new CanyonFireflyGuidanceView { Name = "View" });
        worldRoot.AddChild(fireflyGuidance);
        level.AddChild(worldRoot);
        level.SeamlessWorldPath = new NodePath("WorldRoot/Sectors");
        var fog = new CanvasLayer { Name = "Fog", Layer = 1 };
        var fogCanvas = new PrologueFogCanvas { Name = "Canvas", Visible = false };
        fogCanvas.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        fog.AddChild(fogCanvas);
        level.AddChild(fog);
        var protectedCues = new CanvasLayer
        {
            Name = "ProtectedCues", Layer = 2, FollowViewportEnabled = true, FollowViewportScale = 1f
        };
        protectedCues.AddChild(new FogFragmentReadabilityOverlay { Name = "Fragments" });
        protectedCues.AddChild(new FogReadabilityOverlay { Name = "Attacks" });
        protectedCues.AddChild(new FogInteractionReadabilityOverlay { Name = "Interaction" });
        protectedCues.AddChild(new FogTerrainReadabilityOverlay { Name = "Terrain" });
        protectedCues.AddChild(new FogSteamReadabilityOverlay { Name = "Steam" });
        protectedCues.AddChild(new FogShieldLaneReadabilityOverlay { Name = "ShieldLane" });
        protectedCues.AddChild(new FogPressureGateReadabilityOverlay { Name = "PressureGates" });
        protectedCues.AddChild(new FogMovingGeometryReadabilityOverlay { Name = "MovingBodies" });
        protectedCues.AddChild(new FogBarrierReadabilityOverlay { Name = "Barriers" });
        level.AddChild(protectedCues);
        level.AddChild(new PrologueFogController { Name = "FogController" });
        level.AddChild(BuildHud());
        level.AddChild(new CanvasLayer { Name = "Fade", Layer = 10 });
        level.AddChild(new PrologueCompletionController { Name = "Completion" });
        level.AddChild(new PrologueGuidanceController
        {
            Name = "Guidance",
            VisualPath = new NodePath("../Hud/Guidance/VisualRoot")
        });
        level.AddChild(BuildPauseMenu());
        level.AddChild(PrologueAudioDirector.CreateRuntimeTree());
        PackedScene vfxScene = GD.Load<PackedScene>("res://scenes/vfx/PrologueVfx.tscn");
        if (vfxScene == null)
        {
            throw new InvalidOperationException("PrologueVfx must be built before the parent level.");
        }
        PrologueVfxDirector vfx = vfxScene.Instantiate<PrologueVfxDirector>();
        vfx.Name = "Vfx";
        level.AddChild(vfx);
        PackedScene uiScene = GD.Load<PackedScene>("res://scenes/ui/PrologueUi.tscn");
        if (uiScene == null)
        {
            throw new InvalidOperationException("PrologueUi must be built before the parent level.");
        }
        PrologueUiRoot productUi = uiScene.Instantiate<PrologueUiRoot>();
        productUi.Name = "ProductUi";
        level.AddChild(productUi);
        level.AddChild(new PrologueGameFlowController { Name = "GameFlow" });
        level.AddChild(new ProloguePlaytestRecorder { Name = "PlaytestRecorder" });

        PopulateCanyonDefinitions(sectors, scenePaths);
        var definitions = sectors.Definitions.ToArray();
        MinimapTerrainCatalog catalog = MinimapTerrainCatalogBuilder.Build(definitions);
        level.AddChild(new PrologueMinimapController { Name = "MinimapController", Catalog = catalog });
        level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints =
            MinimapTerrainCatalogBuilder.Checkpoints(definitions);
        ValidateSavedLevel(level, scenePaths);
        return level;
        }
        catch { level.Free(); throw; }
    }

    // Candidate-only snapshot package. Authoring leaves remain separate and require checked rebuilds after edits.
    public static Error SaveCandidate(PrologueLevelController level, string resourcePath)
    {
        if (!GodotObject.IsInstanceValid(level) || level.IsInsideTree() || !Path.IsPathFullyQualified(resourcePath))
            throw new ArgumentException("Candidate parent must be off-tree and have an absolute new output path.");
        string output = Path.GetFullPath(resourcePath);
        string project = Path.GetFullPath(ProjectSettings.GlobalizePath("res://")).TrimEnd(Path.DirectorySeparatorChar);
        string userdata = Path.GetFullPath(OS.GetUserDataDir()).TrimEnd(Path.DirectorySeparatorChar);
        if (File.Exists(output) || !Directory.Exists(Path.GetDirectoryName(output)) ||
            output.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            output.StartsWith(userdata + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Candidate output cannot replace a file or enter project/userdata.");
        var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        ValidateSavedLevel(level, world.Definitions.ToDictionary(d => d.Id, d => d.Scene.ResourcePath));
        foreach (PrologueSectorDefinition definition in world.Definitions)
        {
            // Clear only the owned duplicate's path; never take over a cached source resource path.
            var snapshot = (PackedScene)definition.Scene.Duplicate();
            snapshot.ResourcePath = string.Empty;
            definition.Scene = snapshot;
        }
        using MinimapTerrainCatalog rebuilt = MinimapTerrainCatalogBuilder.Build(world.Definitions.ToArray());
        if (!rebuilt.SerializedValues.AsSpan().SequenceEqual(
            level.GetNode<PrologueMinimapController>("MinimapController").Catalog.SerializedValues))
            throw new InvalidOperationException("Candidate sector snapshots differ from the embedded minimap.");
        return SceneBuildUtil.PackAndSave(level, output);
    }

    private static void ValidateSavedLevel(PrologueLevelController copy, IReadOnlyDictionary<PrologueSectorId, string> scenePaths = null)
    {
        SeamlessPrologueWorld copyWorld =
            copy.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors");
        bool streamValid = ValidateCanyonDefinitions(copyWorld, scenePaths);
        bool valid = copy.GetNodeOrNull<CharacterBody2D>("PlayerRoot/Player") != null &&
            copy.GetNodeOrNull<ThreadShuttleVisual>("PlayerRoot/Player/VisualRoot/MechanicalClaw") != null &&
            copy.GetNodeOrNull<Node2D>("RoomRoot") == null &&
            copy.SeamlessWorldPath.ToString() == "WorldRoot/Sectors" && streamValid &&
            copy.GetNodeOrNull<CanvasLayer>("Fog")?.Layer == 1 &&
            copy.GetNodeOrNull<PrologueFogCanvas>("Fog/Canvas") is { Visible: false } &&
            copy.GetNodeOrNull<PrologueFogController>("FogController") != null &&
            copy.GetNodeOrNull<CanvasLayer>("ProtectedCues") is { Layer: 2, FollowViewportEnabled: true, FollowViewportScale: 1f } &&
            copy.GetNodeOrNull<FogFragmentReadabilityOverlay>("ProtectedCues/Fragments") is { FragmentCueRadius: 240f } &&
            copy.GetNodeOrNull<FogReadabilityOverlay>("ProtectedCues/Attacks") != null &&
            copy.GetNodeOrNull<FogInteractionReadabilityOverlay>("ProtectedCues/Interaction") != null &&
            copy.GetNodeOrNull<FogTerrainReadabilityOverlay>("ProtectedCues/Terrain") is { TerrainCueRadius: 620f } &&
            copy.GetNodeOrNull<FogSteamReadabilityOverlay>("ProtectedCues/Steam") is { SteamCueRadius: 620f } &&
            copy.GetNodeOrNull<FogShieldLaneReadabilityOverlay>("ProtectedCues/ShieldLane") is { LaneCueRadius: 620f } &&
            copy.GetNodeOrNull<FogPressureGateReadabilityOverlay>("ProtectedCues/PressureGates") is { GateCueRadius: 620f } &&
            copy.GetNodeOrNull<FogMovingGeometryReadabilityOverlay>("ProtectedCues/MovingBodies") is { BodyCueRadius: 620f } &&
            copy.GetNodeOrNull<FogBarrierReadabilityOverlay>("ProtectedCues/Barriers") is { BarrierCueRadius: 620f } &&
            copy.GetNodeOrNull<PrologueHud>("Hud") != null &&
            copy.GetNodeOrNull<PrologueFormalHud>("Hud/FormalHud") != null &&
            copy.GetNodeOrNull<Knight.Presentation.Minimap.PrologueMinimapView>("Hud/FormalHud/Minimap") is
                { Visible: false, MouseFilter: Control.MouseFilterEnum.Ignore, FocusMode: Control.FocusModeEnum.None } &&
            copy.GetNodeOrNull<PrologueThreadSpoolVisual>("Hud/FormalHud/ThreadSpool") != null &&
            copy.GetNodeOrNull<Label>("Hud/FormalHud/ThreadSpool/State") != null &&
            copy.GetNodeOrNull<Control>("Hud/FormalHud/ModeSelector/Attack") != null &&
            copy.GetNodeOrNull<Control>("Hud/FormalHud/ModeSelector/Grapple") != null &&
            copy.GetNodeOrNull<Control>("Hud/FormalHud/Loadout/Slot1") != null &&
            copy.GetNodeOrNull<Control>("Hud/FormalHud/Loadout/Slot2") != null &&
            copy.GetNodeOrNull<Control>("Hud/FormalHud/Loadout/Slot3") != null &&
            copy.GetNodeOrNull<Control>("Hud/FormalHud/Loadout/Slot4") != null &&
            copy.GetNodeOrNull<ProgressBar>("Hud/FormalHud/ResourceCluster/Energy") != null &&
            copy.GetNodeOrNull<ProgressBar>("Hud/FormalHud/ResourceCluster/Heat") != null &&
            copy.GetNodeOrNull<PrologueAimReticle>("Hud/FormalHud/AimReticle") != null &&
            copy.GetNodeOrNull<ProloguePlaytestRecorder>("PlaytestRecorder") != null &&
            copy.GetNodeOrNull<PrologueVfxDirector>("Vfx") != null &&
            copy.GetNodeOrNull<PrologueAudioDirector>("Audio") != null &&
            copy.GetNodeOrNull<PrologueUiRoot>("ProductUi") != null &&
            copy.GetNodeOrNull<PrologueGameFlowController>("GameFlow") != null &&
            copy.GetNodeOrNull<CameraFeedbackController>("Vfx/CameraFeedback") != null &&
            copy.GetNodeOrNull<PrologueGuidanceController>("Guidance") != null &&
            copy.GetNodeOrNull<CanyonFireflyGuidanceController>("WorldRoot/FireflyGuidance") != null &&
            copy.GetNodeOrNull<CanyonFireflyGuidanceView>("WorldRoot/FireflyGuidance/View") != null &&
            copy.GetNodeOrNull<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot") != null &&
            copy.RoomDefinitions.Count == 0 && copy.RequiredRooms.Count == 0;
        if (!valid) throw new System.InvalidOperationException("BuildPrologueLevel contract validation failed before replacement.");
    }

    private static void PopulateCanyonDefinitions(SeamlessPrologueWorld world, IReadOnlyDictionary<PrologueSectorId, string> scenePaths = null)
    {
        PrologueSectorId[] route = System.Enum.GetValues<PrologueSectorId>();
        for (int index = 0; index < route.Length; index++)
        {
            PrologueSectorId id = route[index];
            string scenePath = (scenePaths == null ? PrologueCanyonBuildParts.ScenePath(id) : scenePaths[id]).Replace((char)92, (char)47);
            PackedScene scene = GD.Load<PackedScene>(scenePath) ??
                throw new System.InvalidOperationException(
                    $"Authored canyon sector is missing or invalid: {scenePath}");
            var definition = new PrologueSectorDefinition(
                id,
                scene,
                PrologueCanyonBuildParts.WorldOffsets[id],
                PrologueCanyonBuildParts.Bounds(id),
                index > 0 ? route[index - 1] : null,
                index + 1 < route.Length ? route[index + 1] : null)
            {
                NeighborValues = StreamingNeighborValues(id)
            };
            foreach (Rect2 interaction in PrologueCanyonBuildParts.CriticalInteractionBounds(id))
                definition.KeyInteractionBounds.Add(interaction);
            world.Definitions.Add(definition);
        }
    }

    private static bool ValidateCanyonDefinitions(SeamlessPrologueWorld world, IReadOnlyDictionary<PrologueSectorId, string> scenePaths = null)
    {
        if (world == null || world.Definitions.Count != 5) return false;
        var definitions = new System.Collections.Generic.List<PrologueSectorDefinition>();
        foreach (PrologueSectorDefinition definition in world.Definitions)
            definitions.Add(definition);
        try
        {
            _ = new PrologueSectorStreamModel(definitions);
            foreach (PrologueSectorDefinition definition in definitions)
            {
                if (definition.Scene?.ResourcePath != (scenePaths == null ? PrologueCanyonBuildParts.ScenePath(definition.Id) : scenePaths[definition.Id]).Replace((char)92, (char)47) ||
                    definition.WorldOffset != PrologueCanyonBuildParts.WorldOffsets[definition.Id] ||
                    definition.Bounds != PrologueCanyonBuildParts.Bounds(definition.Id) ||
                    !NeighborValuesMatch(definition.NeighborValues,
                        StreamingNeighborValues(definition.Id)) ||
                    definition.KeyInteractionBounds.Count == 0)
                    return false;
                PrologueSector sector = definition.Scene?.Instantiate<PrologueSector>();
                if (sector == null) return false;
                bool valid = sector.ValidateContract(definition).Count == 0;
                sector.Free();
                if (!valid) return false;
            }
            return true;
        }
        catch (System.Exception exception)
        {
            GD.PushError($"BuildPrologueLevel authored canyon validation failed: {exception.Message}");
            return false;
        }
    }

    private static Godot.Collections.Array<int> StreamingNeighborValues(PrologueSectorId id)
    {
        var values = new Godot.Collections.Array<int>();
        foreach (PrologueSectorId neighbor in id switch
        {
            PrologueSectorId.CanyonEntrance => new[] { PrologueSectorId.CanyonDescent },
            PrologueSectorId.CanyonDescent => new[]
                { PrologueSectorId.CanyonEntrance, PrologueSectorId.CanyonBasin },
            PrologueSectorId.CanyonBasin => new[]
                { PrologueSectorId.CanyonDescent, PrologueSectorId.CanyonAscent },
            PrologueSectorId.CanyonAscent => new[]
                { PrologueSectorId.CanyonBasin, PrologueSectorId.LostTemple },
            PrologueSectorId.LostTemple => new[] { PrologueSectorId.CanyonAscent },
            _ => System.Array.Empty<PrologueSectorId>()
        })
            values.Add((int)neighbor);
        return values;
    }

    private static bool NeighborValuesMatch(Godot.Collections.Array<int> actual,
        Godot.Collections.Array<int> expected)
    {
        if (actual == null || actual.Count != expected.Count) return false;
        for (int index = 0; index < expected.Count; index++)
            if (actual[index] != expected[index]) return false;
        return true;
    }

    private static PrologueHud BuildHud()
    {
        var hud = new PrologueHud { Name = "Hud", Layer = 5 };
        Control legacyPanel = PrologueBuildParts.HudPanel("Panel", new Vector2(18, 16), new Vector2(360, 104));
        legacyPanel.Modulate = new Color(1, 1, 1, 0);
        hud.AddChild(legacyPanel);
        Label legacyHealth = Label("Health", new Vector2(32, 26), new Vector2(200, 28), "HEALTH 5/5");
        Label legacyThread = Label("Thread", new Vector2(230, 26), new Vector2(190, 28), string.Empty);
        Label legacyObjective = Label("Objective", new Vector2(32, 57), new Vector2(640, 28), "WAKE AND FOLLOW THE BELLS");
        legacyHealth.Modulate = legacyThread.Modulate = legacyObjective.Modulate = new Color(1, 1, 1, 0);
        hud.AddChild(legacyHealth);
        hud.AddChild(legacyThread);
        hud.AddChild(legacyObjective);
        var guidance = new Control { Name = "Guidance" };
        guidance.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var visual = new WorldTargetMarkerVisual
        {
            Name = "VisualRoot",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        visual.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        visual.AddChild(new ColorRect
        {
            Name = "Backdrop", Position = new Vector2(48, 156), Size = new Vector2(480, 88),
            Color = new Color(0.04f, 0.065f, 0.085f, .88f), MouseFilter = Control.MouseFilterEnum.Ignore
        });
        visual.AddChild(new ColorRect
        {
            Name = "TopRule", Position = new Vector2(48, 156), Size = new Vector2(480, 2),
            Color = new Color("d4a83f"), MouseFilter = Control.MouseFilterEnum.Ignore
        });
        var guidanceText = Label("Text", new Vector2(62, 166), new Vector2(452, 68), string.Empty);
        guidanceText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        guidanceText.MouseFilter = Control.MouseFilterEnum.Ignore;
        guidanceText.AddThemeFontSizeOverride("font_size", 17);
        visual.AddChild(guidanceText);
        guidance.AddChild(visual);
        hud.AddChild(guidance);
        var prompt = Label("Prompt", new Vector2(390, 610), new Vector2(500, 50), string.Empty);
        prompt.Visible = false;
        prompt.Modulate = new Color(1, 1, 1, 0);
        hud.AddChild(prompt);
        var boss = new Control { Name = "Boss", Position = new Vector2(390, 196), Size = new Vector2(500, 90), Visible = false };
        boss.AddChild(Label("Name", Vector2.Zero, new Vector2(300, 28), string.Empty));
        boss.AddChild(new ProgressBar { Name = "Health", Position = new Vector2(0, 32), Size = new Vector2(500, 18) });
        boss.AddChild(Label("State", new Vector2(0, 55), new Vector2(300, 28), string.Empty));
        boss.Modulate = new Color(1, 1, 1, 0);
        hud.AddChild(boss);
        hud.AddChild(BuildFormalHud());
        return hud;
    }

    private static PrologueFormalHud BuildFormalHud()
    {
        var root = new PrologueFormalHud { Name = "FormalHud", MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var health = new Control { Name = "Health" };
        var shards = new HBoxContainer { Name = "Shards", Position = new Vector2(0, 8), Size = new Vector2(190, 42) };
        shards.AddThemeConstantOverride("separation", 5);
        for (int index = 0; index < 5; index++)
        {
            shards.AddChild(new PrologueHealthShardVisual
            {
                Name = $"Shard{index + 1}", CustomMinimumSize = new Vector2(34, 42),
                MouseFilter = Control.MouseFilterEnum.Ignore
            });
        }
        health.AddChild(shards);
        root.AddChild(health);

        var spool = new PrologueThreadSpoolVisual
        {
            Name = "ThreadSpool", MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false
        };
        Label spoolState = Label("State", Vector2.Zero, Vector2.Zero, string.Empty);
        spoolState.HorizontalAlignment = HorizontalAlignment.Center;
        spoolState.VerticalAlignment = VerticalAlignment.Center;
        spoolState.AutowrapMode = TextServer.AutowrapMode.Off;
        spoolState.AddThemeFontSizeOverride("font_size", 10);
        spool.AddChild(spoolState);
        root.AddChild(spool);

        root.AddChild(BuildModeSelector());
        root.AddChild(BuildLoadout());
        root.AddChild(BuildResourceCluster());
        root.AddChild(new PrologueAimReticle
        {
            Name = "AimReticle", MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = Vector2.One * PrologueAimReticle.Diameter, ZIndex = 100
        });

        ColorRect objective = Panel("ObjectiveRibbon", new Color("101a22"), new Color("9a7b35"));
        objective.GetNode<ColorRect>("TopRule").Size = new Vector2(352, 2);
        Label objectiveText = Label("Text", new Vector2(14, 2), new Vector2(324, 30), "跟随钟声醒来");
        objectiveText.VerticalAlignment = VerticalAlignment.Center;
        objectiveText.AddThemeFontSizeOverride("font_size", 15);
        objective.AddChild(objectiveText);
        root.AddChild(objective);

        ColorRect prompt = Panel("Prompt", new Color("101a22"), new Color("9a7b35"));
        prompt.GetNode<ColorRect>("TopRule").Size = new Vector2(390, 2);
        var glyph = Label("Glyph", new Vector2(12, 8), new Vector2(58, 40), string.Empty);
        glyph.HorizontalAlignment = HorizontalAlignment.Center;
        glyph.VerticalAlignment = VerticalAlignment.Center;
        glyph.AddThemeFontSizeOverride("font_size", 19);
        glyph.AddThemeColorOverride("font_color", new Color("9edbd2"));
        prompt.AddChild(glyph);
        var promptText = Label("Text", new Vector2(78, 4), new Vector2(292, 50), string.Empty);
        promptText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        promptText.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        promptText.ClipText = false;
        promptText.VerticalAlignment = VerticalAlignment.Center;
        promptText.AddThemeFontSizeOverride("font_size", 16);
        prompt.AddChild(promptText);
        prompt.Visible = false;
        root.AddChild(prompt);

        ColorRect boss = Panel("Boss", new Color("0d141b"), new Color("8e4939"));
        boss.GetNode<ColorRect>("TopRule").Size = new Vector2(640, 2);
        var bossName = Label("Name", new Vector2(18, 3), new Vector2(320, 22), string.Empty);
        bossName.AddThemeFontSizeOverride("font_size", 17);
        bossName.AddThemeColorOverride("font_color", new Color("e8dfc6"));
        boss.AddChild(bossName);
        boss.AddChild(new ProgressBar
        {
            Name = "Health", Position = new Vector2(18, 31), Size = new Vector2(604, 15),
            ShowPercentage = false, MaxValue = 18, Value = 18
        });
        var bossState = Label("State", new Vector2(340, 3), new Vector2(282, 22), string.Empty);
        bossState.HorizontalAlignment = HorizontalAlignment.Right;
        bossState.AddThemeFontSizeOverride("font_size", 14);
        bossState.AddThemeColorOverride("font_color", new Color("d4a83f"));
        boss.AddChild(bossState);
        boss.Visible = false;
        root.AddChild(boss);

        root.AddChild(new Knight.Presentation.Minimap.PrologueMinimapView
        {
            Name = "Minimap", MouseFilter = Control.MouseFilterEnum.Ignore,
            FocusMode = Control.FocusModeEnum.None, Visible = false
        });
        root.AddChild(BuildRecovery());
        root.AddChild(BuildCompletion());
        return root;
    }

    private static PrologueActionModeVisual BuildModeSelector()
    {
        var selector = new PrologueActionModeVisual
            { Name = "ModeSelector", MouseFilter = Control.MouseFilterEnum.Ignore };
        selector.AddChild(BuildActionOption("Attack", string.Empty, 0f, true));
        selector.AddChild(BuildActionOption("Grapple", string.Empty, 102f, false));
        return selector;
    }

    private static ColorRect BuildActionOption(string name, string text, float x, bool selected)
    {
        ColorRect option = Panel(name, new Color("101a22"), new Color("5a7480"));
        option.Position = new Vector2(x, 0);
        option.Size = new Vector2(94, 32);
        option.GetNode<ColorRect>("TopRule").Size = new Vector2(94, 2);
        Label label = Label("Label", new Vector2(8, 5), new Vector2(58, 22), text);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.AddThemeFontSizeOverride("font_size", 13);
        option.AddChild(label);
        Label marker = Label("SelectedMarker", new Vector2(70, 4), new Vector2(18, 24), "◆");
        marker.Visible = selected;
        marker.HorizontalAlignment = HorizontalAlignment.Center;
        marker.TooltipText = "selected mode";
        option.AddChild(marker);
        return option;
    }

    private static PrologueLoadoutVisual BuildLoadout()
    {
        var loadout = new PrologueLoadoutVisual
            { Name = "Loadout", MouseFilter = Control.MouseFilterEnum.Ignore };
        for (int slot = 1; slot <= 4; slot++) loadout.AddChild(BuildLoadoutSlot(slot));
        Label rejection = Label("RejectionMarker", new Vector2(178, 7), new Vector2(18, 18), "!");
        rejection.Visible = false;
        rejection.TooltipText = "primary rejection";
        rejection.HorizontalAlignment = HorizontalAlignment.Center;
        rejection.AddThemeFontSizeOverride("font_size", 16);
        loadout.AddChild(rejection);
        return loadout;
    }

    private static ColorRect BuildLoadoutSlot(int slot)
    {
        ColorRect option = Panel($"Slot{slot}", new Color("0d141b"), new Color("4b5c62"));
        option.Position = new Vector2((slot - 1) * 50f, 0);
        option.Size = new Vector2(46, 32);
        option.GetNode<ColorRect>("TopRule").Size = new Vector2(46, 2);
        Label label = Label("Label", new Vector2(5, 5), new Vector2(16, 22), slot.ToString());
        label.HorizontalAlignment = HorizontalAlignment.Center;
        option.AddChild(label);
        Label selected = Label("SelectedMarker", new Vector2(25, 5), new Vector2(16, 22), "◆");
        selected.Visible = slot == 1;
        selected.TooltipText = "selected slot";
        selected.HorizontalAlignment = HorizontalAlignment.Center;
        selected.AddThemeFontSizeOverride("font_size", 11);
        option.AddChild(selected);
        if (slot > 1)
        {
            Label empty = Label("EmptyMarker", new Vector2(15, 4), new Vector2(16, 22), "□");
            empty.TooltipText = "empty slot";
            empty.HorizontalAlignment = HorizontalAlignment.Center;
            option.AddChild(empty);
        }
        return option;
    }

    private static PrologueEnergyHeatVisual BuildResourceCluster()
    {
        var resources = new PrologueEnergyHeatVisual
            { Name = "ResourceCluster", MouseFilter = Control.MouseFilterEnum.Ignore };
        resources.AddChild(Label("EnergyLabel", Vector2.Zero, new Vector2(48, 26), string.Empty));
        resources.AddChild(new ProgressBar
        {
            Name = "Energy", Position = new Vector2(50, 6), Size = new Vector2(136, 14),
            MinValue = 0, MaxValue = 1, Value = 1, ShowPercentage = false
        });
        resources.AddChild(Label("HeatLabel", new Vector2(0, 36), new Vector2(48, 26), string.Empty));
        var heat = new ProgressBar
        {
            Name = "Heat", Position = new Vector2(50, 42), Size = new Vector2(136, 14),
            MinValue = 0, MaxValue = 1, Value = 0, ShowPercentage = false
        };
        Label overheat = Label("OverheatMarker", new Vector2(112, -6), new Vector2(20, 22), "!");
        overheat.Visible = false;
        overheat.TooltipText = "overheated";
        overheat.HorizontalAlignment = HorizontalAlignment.Center;
        overheat.AddThemeFontSizeOverride("font_size", 16);
        heat.AddChild(overheat);
        resources.AddChild(heat);
        return resources;
    }

    private static PrologueRecoveryView BuildRecovery()
    {
        var recovery = new PrologueRecoveryView { Name = "Recovery", Visible = false };
        recovery.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        recovery.AddChild(new ColorRect
        {
            Name = "Veil", AnchorRight = 1, AnchorBottom = 1,
            Color = new Color(0.025f, 0.04f, 0.055f, .82f), MouseFilter = Control.MouseFilterEnum.Stop
        });
        var title = Label("Title", Vector2.Zero, new Vector2(500, 46), "墓灯重燃");
        Center(title, new Vector2(500, 46), new Vector2(0, -54));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 30);
        title.AddThemeColorOverride("font_color", new Color("d4a83f"));
        recovery.AddChild(title);
        var reason = Label("Reason", Vector2.Zero, new Vector2(500, 34), string.Empty);
        Center(reason, new Vector2(500, 34), new Vector2(0, 8));
        reason.HorizontalAlignment = HorizontalAlignment.Center;
        recovery.AddChild(reason);
        return recovery;
    }

    private static PrologueCompletionView BuildCompletion()
    {
        var completion = new PrologueCompletionView { Name = "Completion", Visible = false };
        completion.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        completion.AddChild(new ColorRect
        {
            Name = "Veil", AnchorRight = 1, AnchorBottom = 1,
            Color = new Color(0.02f, 0.035f, 0.05f, .94f), MouseFilter = Control.MouseFilterEnum.Stop
        });
        ColorRect panel = Panel("Panel", new Color("101a22"), new Color("d4a83f"));
        Center(panel, new Vector2(500, 520));
        panel.GetNode<ColorRect>("TopRule").Size = new Vector2(500, 2);
        var title = Label("Title", new Vector2(40, 34), new Vector2(420, 48), "无名钟楼 · 序章完成");
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", new Color("d4a83f"));
        panel.AddChild(title);
        var subtitle = Label("Subtitle", new Vector2(40, 88), new Vector2(420, 34), "最后的守墓人仍在钟声深处");
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeColorOverride("font_color", new Color("b7aa88"));
        panel.AddChild(subtitle);
        var stats = Label("Stats", new Vector2(70, 146), new Vector2(360, 190), string.Empty);
        stats.AddThemeFontSizeOverride("font_size", 18);
        panel.AddChild(stats);
        panel.AddChild(Button("ReturnToTitle", new Vector2(70, 376), "返回标题", 170));
        panel.AddChild(Button("Replay", new Vector2(260, 376), "再次游玩", 170));
        completion.AddChild(panel);
        return completion;
    }

    private static ColorRect Panel(string name, Color fill, Color border)
    {
        var panel = new ColorRect { Name = name, Color = fill };
        panel.AddChild(new ColorRect
        {
            Name = "TopRule", Position = Vector2.Zero, Size = new Vector2(760, 2), Color = border,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
        return panel;
    }

    private static Label Label(string name, Vector2 position, Vector2 size, string text)
    {
        var label = new Label { Name = name, Position = position, Size = size, Text = text };
        label.AddThemeColorOverride("font_color", new Color("e8dfc6"));
        return label;
    }

    private static ProloguePauseMenu BuildPauseMenu()
    {
        var menu = new ProloguePauseMenu { Name = "PauseMenu", Layer = 20 };
        Theme theme = PrologueUiPalette.CreateTheme();
        var panel = new ColorRect
        {
            Name = "Panel", Size = new Vector2(720, 480),
            Color = new Color("101820"), Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop, Theme = theme
        };
        Center(panel, new Vector2(720, 480));
        panel.AddChild(new ColorRect
        {
            Name = "TopRule", Position = Vector2.Zero, Size = new Vector2(720, 3),
            Color = new Color("d4a83f"), MouseFilter = Control.MouseFilterEnum.Ignore
        });
        Label title = Label("Title", new Vector2(36, 24), new Vector2(648, 44), "无名钟楼 · 暂停");
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 27);
        title.AddThemeColorOverride("font_color", new Color("d4a83f"));
        panel.AddChild(title);
        Label subtitle = Label("Subtitle", new Vector2(36, 66), new Vector2(648, 28), "钟声仍在等待");
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.AddThemeColorOverride("font_color", new Color("b7aa88"));
        panel.AddChild(subtitle);
        panel.AddChild(Button("Resume", new Vector2(36, 112), "继续"));
        panel.AddChild(Button("ShowControls", new Vector2(196, 112), "操作说明"));
        panel.AddChild(Button("ReturnToCheckpoint", new Vector2(356, 112), "返回墓灯", 170));
        panel.AddChild(Button("QuitToDesktop", new Vector2(536, 112), "退出", 148));
        panel.AddChild(Button("GuidanceToggle", new Vector2(36, 168), "第一关指导：开", 190));
        ColorRect controlsWell = new()
        {
            Name = "ControlsWell", Position = new Vector2(36, 224), Size = new Vector2(648, 206),
            Color = new Color("0b1118"), MouseFilter = Control.MouseFilterEnum.Ignore
        };
        panel.AddChild(controlsWell);
        Label controls = Label("Controls", new Vector2(56, 238), new Vector2(608, 178), string.Empty);
        controls.AddThemeFontSizeOverride("font_size", 16);
        panel.AddChild(controls);
        var quitFailure = new ColorRect
        {
            Name = "QuitFailure", Position = new Vector2(24, 96), Size = new Vector2(672, 350),
            Color = new Color("101820"), Visible = false, MouseFilter = Control.MouseFilterEnum.Stop
        };
        Label failureMessage = Label("Message", new Vector2(24, 32), new Vector2(624, 150), string.Empty);
        failureMessage.HorizontalAlignment = HorizontalAlignment.Center;
        failureMessage.AddThemeFontSizeOverride("font_size", 20);
        quitFailure.AddChild(failureMessage);
        quitFailure.AddChild(Button("Retry", new Vector2(24, 220), "重试保存", 170));
        quitFailure.AddChild(Button("ExitWithoutSaving", new Vector2(204, 220), "不保存退出", 250));
        quitFailure.AddChild(Button("Cancel", new Vector2(464, 220), "取消", 184));
        panel.AddChild(quitFailure);
        menu.AddChild(panel);
        return menu;
    }

    private static Button Button(string name, Vector2 position, string text, float width = 150f) => new()
    {
        Name = name,
        Position = position,
        Size = new Vector2(width, 44),
        Text = text
    };

    private static void Center(Control control, Vector2 size, Vector2 offset = default)
    {
        control.AnchorLeft = control.AnchorRight = control.AnchorTop = control.AnchorBottom = .5f;
        control.OffsetLeft = -size.X / 2f + offset.X;
        control.OffsetRight = size.X / 2f + offset.X;
        control.OffsetTop = -size.Y / 2f + offset.Y;
        control.OffsetBottom = size.Y / 2f + offset.Y;
    }
}
