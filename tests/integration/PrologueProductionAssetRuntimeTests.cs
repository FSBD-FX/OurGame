using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class PrologueProductionAssetRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        IsolatedPrologueFixture.ValidateStorageRoot();
        string root = ProjectSettings.GlobalizePath("res://");
        string json = File.ReadAllText(Path.Combine(root, "assets", "production", "prologue-assets.json"));
        ProductionAssetManifest manifest = ProductionAssetManifestParser.Parse(json);
        TestAssert.True(manifest.Assets.Count == 8 && manifest.Assets.All(asset => asset.Final),
            "all eight production asset families have final manifest declarations");
        TestAssert.Equal(0, manifest.RequireRuntimeFiles(root).Count,
            "all final production manifest files exist");

        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        TestAssert.True(packed != null, "production audit loads the final prologue scene");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        try
        {
            IsolatedPrologueFixture.Configure(level, nameof(PrologueProductionAssetRuntimeTests));
            level.GetNode<PrologueAudioDirector>("Audio").PlaybackEnabled = false;
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await AuditPlayableEntrance(tree, level, flow);
            AuditReplacementBoundaries(manifest, level);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree())
                {
                    level.QueueFree();
                    await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                }
                else level.Free();
            }
        }
    }

    private static async Task AuditPlayableEntrance(SceneTree tree, PrologueLevelController level,
        PrologueGameFlowController flow)
    {
        var playerVisual = level.Player.GetNode<Node2D>("VisualRoot");
        var input = level.Player.GetNode<PlayerInputReader>("Input");
        var menu = level.GetNode<CanvasLayer>("ProductUi");
        var hud = level.GetNode<CanvasLayer>("Hud");
        var start = menu.GetNode<Button>("MainMenu/Panel/Buttons/Start");
        var fallbackProbe = new PlayerPlaceholderVisual { Name = "ProductionAuditVisualProbe" };
        var textureProbe = new Sprite2D { Name = "ProductionAuditTextureProbe" };
        int starts = 0;
        ulong frameAtStart = 0;
        bool committedAtStart = false;
        void OnStarted()
        {
            starts++;
            frameAtStart = input.CurrentFrameSequence;
            committedAtStart = flow.State == PrologueGameFlowState.Playing &&
                !level.ResidentPreparationPending && level.Player.IsVisibleInTree();
        }

        flow.SessionStarted += OnStarted;
        try
        {
            // Add after the real presentation profile has selected its formal visual;
            // leave the probes locally visible so only production hierarchy can hide them.
            playerVisual.AddChild(fallbackProbe);
            playerVisual.AddChild(textureProbe);
            string fallback = $"{fallbackProbe.GetPath()} [PlayerPlaceholderVisual]";
            string missingTexture = $"{textureProbe.GetPath()}: visible Sprite2D has no texture";
            void AssertProbesHidden(string phase) => TestAssert.True(
                fallbackProbe.Visible && textureProbe.Visible && !fallbackProbe.IsVisibleInTree() &&
                !textureProbe.IsVisibleInTree() &&
                !PrologueProductionAudit.FindVisibleFallbacks(level).Contains(fallback) &&
                !PrologueProductionAudit.FindInvalidVisibleResources(level).Contains(missingTexture),
                $"the actual {phase} hierarchy excludes both locally visible test probes");

            TestAssert.True(flow.State == PrologueGameFlowState.Title && menu.Visible &&
                start.IsVisibleInTree() && !start.Disabled && !hud.Visible && starts == 0,
                "production visibility audit starts at the real usable title");
            AssertProbesHidden("Title");
            start.EmitSignal(BaseButton.SignalName.Pressed);
            TestAssert.True(flow.State == PrologueGameFlowState.Intro && level.ResidentPreparationPending &&
                !level.Player.IsVisibleInTree() && !hud.Visible && starts == 0,
                "real Start enters hidden Intro and prepares the authored population");
            AssertProbesHidden("Intro");

            for (int frame = 0; frame < 180 &&
                 (flow.State == PrologueGameFlowState.Intro ||
                  flow.State == PrologueGameFlowState.Playing && starts == 1 &&
                  input.CurrentFrameSequence <= frameAtStart); frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);

            var sector = level.CurrentSectorRoot;
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && starts == 1 && committedAtStart &&
                !level.ResidentPreparationPending && level.ResidentPreparationFailure.Length == 0 &&
                flow.LastRejectedTransition.Length == 0 && !tree.Paused && !menu.Visible && hud.Visible &&
                level.Player.IsVisibleInTree() && input.IsPhysicsProcessing() && input.CanProcess() &&
                input.CurrentFrameSequence > frameAtStart &&
                playerVisual.GetNodeOrNull<PlayerFormalVisual>("Formal") is { } formal &&
                PrologueProductionAudit.IsEffectivelyVisible(formal) &&
                hud.GetNodeOrNull<Control>("FormalHud/ResourceCluster") is { } resources &&
                PrologueProductionAudit.IsEffectivelyVisible(resources) &&
                level.CurrentSector == PrologueSectorId.CanyonEntrance && sector != null &&
                sector.SectorId == PrologueSectorId.CanyonEntrance && sector.GetParent() == world &&
                sector.IsInsideTree() && !sector.IsQueuedForDeletion() &&
                sector.GetNodeOrNull<Polygon2D>("VisualRoot/GameplayReadable/StartFloor/ReadableEdge")
                    is { } edge && PrologueProductionAudit.IsEffectivelyVisible(edge),
                $"PRODUCTION_ASSET_AUDIT_BEFORE_GAMEPLAY state={flow.State} starts={starts} " +
                $"pending={level.ResidentPreparationPending} failure={level.ResidentPreparationFailure} " +
                $"rejection={flow.LastRejectedTransition} input={frameAtStart}->{input.CurrentFrameSequence} " +
                $"sector={level.CurrentSector} hud_visible={hud.Visible} menu_visible={menu.Visible}");

            TestAssert.True(fallbackProbe.IsVisibleInTree() && textureProbe.IsVisibleInTree() &&
                PrologueProductionAudit.FindVisibleFallbacks(level).Count(path => path == fallback) == 1 &&
                PrologueProductionAudit.FindInvalidVisibleResources(level).Count(path => path == missingTexture) == 1,
                "Playing reports both exact visible probes: drawn fallback and missing texture");

            textureProbe.SelfModulate = new Color(1f, 1f, 1f, 0f);
            TestAssert.True(textureProbe.IsVisibleInTree() && !PrologueProductionAudit.IsEffectivelyVisible(textureProbe) &&
                !PrologueProductionAudit.FindInvalidVisibleResources(level).Contains(missingTexture),
                "a fully transparent probe is excluded even when the engine reports it visible in tree");
        }
        finally
        {
            flow.SessionStarted -= OnStarted;
            fallbackProbe.Free();
            textureProbe.Free();
        }

        IReadOnlyList<string> visibleFallbacks = PrologueProductionAudit.FindVisibleFallbacks(level);
        IReadOnlyList<string> invalidResources = PrologueProductionAudit.FindInvalidVisibleResources(level);
        TestAssert.True(visibleFallbacks.Count == 0 && invalidResources.Count == 0,
            "playable entrance contains unexpected visible resource nodes: " +
            string.Join("; ", visibleFallbacks.Concat(invalidResources)));
        GD.Print($"PRODUCTION_ASSET_PLAYABLE_ENTRANCE_OK starts={starts} " +
            $"input_advanced={input.CurrentFrameSequence > frameAtStart} sector=CanyonEntrance " +
            "visible_world_player_hud=true hidden_probes=2 visible_probes=2 transparent_probe_excluded=true baseline_clean=true");
    }

    private static void AuditReplacementBoundaries(ProductionAssetManifest manifest, PrologueLevelController level)
    {
        var sectors = new List<Node>();
        try
        {
            foreach (PrologueSectorId id in System.Enum.GetValues<PrologueSectorId>())
            {
                PrologueSector sector = GD.Load<PackedScene>(
                        $"res://scenes/prologue/sectors/{id}.tscn")
                    .Instantiate<PrologueSector>();
                sectors.Add(sector);
                TestAssert.Equal(id, sector.SectorId,
                    $"real generated sector scene {id} preserves its unique identity");
            }
            IReadOnlyList<string> replacementErrors =
                manifest.AuditReplacementBoundaries(level, sectors);
            TestAssert.Equal(0, replacementErrors.Count,
                "all five real generated sector scenes pass stable VisualRoot and state boundaries");
        }
        finally
        {
            foreach (Node sector in sectors) sector.Free();
        }
    }

    public static void Task10ReplacementBoundaryMutationsFail()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "production",
            "prologue-assets.json");
        ProductionAssetManifest manifest = ProductionAssetManifestParser.Parse(File.ReadAllText(path));
        Node fixture = BuildReplacementFixture(out List<Node> sectors);
        try
        {
            TestAssert.Equal(0, manifest.AuditReplacementBoundaries(fixture, sectors).Count,
                "Task10 stable HUD/player/sector replacement fixture passes before mutation");

            Node reticle = fixture.GetNode("Hud/FormalHud/AimReticle");
            reticle.GetParent().RemoveChild(reticle);
            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, sectors).Any(
                    error => error.Contains("slot.hud.aim-reticle")),
                "Task10 removing a real stable HUD node fails the structural gate");
            fixture.GetNode("Hud/FormalHud").AddChild(reticle);

            Node energy = fixture.GetNode("Hud/FormalHud/ResourceCluster/Energy");
            energy.GetParent().RemoveChild(energy);
            fixture.GetNode("Hud/FormalHud").AddChild(energy);
            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, sectors).Any(
                    error => error.Contains("slot.hud.energy")),
                "Task10 moving a real stable HUD node fails the structural gate");
            energy.GetParent().RemoveChild(energy);
            fixture.GetNode("Hud/FormalHud/ResourceCluster").AddChild(energy);

            Node visualRoot = sectors[0].GetNode("VisualRoot");
            var collision = new CollisionShape2D { Name = "IllegalGameplayCollision" };
            visualRoot.AddChild(collision);
            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, sectors).Any(
                    error => error.Contains("gameplay collision")),
                "Task10 gameplay collision under VisualRoot fails the structural gate");
            visualRoot.RemoveChild(collision);
            collision.Free();

            var owner = new Task10StateOwningVisualFixture { Name = "IllegalStateOwner" };
            visualRoot.AddChild(owner);
            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, sectors).Any(
                    error => error.Contains("owns combat state")),
                "Task10 placeholder script owning combat state fails the structural gate");
            visualRoot.RemoveChild(owner);
            owner.Free();

            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, []).Any(
                    error => error.Contains("exactly five")),
                "Task10 empty sector set fails closed");
            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, sectors.Take(4)).Any(
                    error => error.Contains("exactly five")),
                "Task10 incomplete four-sector set fails closed");

            var duplicate = BuildSector(PrologueSectorId.CanyonEntrance);
            fixture.AddChild(duplicate);
            var duplicateSet = sectors.Take(4).Append<Node>(duplicate).ToList();
            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, duplicateSet).Any(
                    error => error.Contains("duplicate replacement sector id")),
                "Task10 duplicate sector identity fails closed");
            fixture.RemoveChild(duplicate);
            duplicate.Free();

            PrologueSector wrongIdentity = (PrologueSector)sectors[0];
            wrongIdentity.Name = "WrongEntranceIdentity";
            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, sectors).Any(
                    error => error.Contains("does not match id")),
                "Task10 wrong sector root identity fails closed");
            wrongIdentity.Name = PrologueSectorId.CanyonEntrance.ToString();

            Node omittedEffects = sectors[4].GetNode("VisualRoot/Effects");
            omittedEffects.GetParent().RemoveChild(omittedEffects);
            TestAssert.True(manifest.AuditReplacementBoundaries(fixture, sectors).Any(
                    error => error.Contains("slot.sector.effects")),
                "Task10 one omitted sector VisualRoot subtree fails closed");
            sectors[4].GetNode("VisualRoot").AddChild(omittedEffects);
        }
        finally
        {
            fixture.Free();
        }
    }

    public static void Task10EmptySectorSetFailsClosed()
    {
        ProductionAssetManifest manifest = ProductionAssetManifestParser.Parse(File.ReadAllText(
            Path.Combine(Directory.GetCurrentDirectory(), "assets", "production",
                "prologue-assets.json")));
        Node fixture = BuildReplacementFixture(out List<Node> sectors);
        try
        {
            IReadOnlyList<string> errors = manifest.AuditReplacementBoundaries(fixture, []);
            TestAssert.True(errors.Any(error => error.Contains("five", System.StringComparison.OrdinalIgnoreCase)),
                "an empty replacement-sector set fails closed instead of passing vacuously");
        }
        finally
        {
            fixture.Free();
        }
    }

    private static Node BuildReplacementFixture(out List<Node> sectors)
    {
        var level = new Node { Name = "Level" };
        AddPath(level, "Hud/FormalHud/ResourceCluster/Energy");
        AddPath(level, "Hud/FormalHud/ResourceCluster/Heat");
        AddPath(level, "Hud/FormalHud/ModeSelector/Attack");
        AddPath(level, "Hud/FormalHud/ModeSelector/Grapple");
        for (int slot = 1; slot <= 4; slot++) AddPath(level, $"Hud/FormalHud/Loadout/Slot{slot}");
        AddPath(level, "Hud/FormalHud/AimReticle");
        AddPath(level, "PlayerRoot/Player/VisualRoot/Shield");
        AddPath(level, "PlayerRoot/Player/VisualRoot/MechanicalClaw");
        sectors = new List<Node>();
        foreach (PrologueSectorId id in System.Enum.GetValues<PrologueSectorId>())
        {
            PrologueSector sector = BuildSector(id);
            level.AddChild(sector);
            sectors.Add(sector);
        }
        return level;
    }

    private static PrologueSector BuildSector(PrologueSectorId id)
    {
        var sector = new PrologueSector { Name = id.ToString(), SectorId = id };
        foreach (string stablePath in RoomVisualCatalog.StableSectorVisualRoots)
            AddPath(sector, stablePath);
        return sector;
    }

    private static void AddPath(Node root, string path)
    {
        Node cursor = root;
        foreach (string segment in path.Split('/'))
        {
            Node next = cursor.GetNodeOrNull(segment);
            if (next == null)
            {
                next = new Node2D { Name = segment };
                cursor.AddChild(next);
            }
            cursor = next;
        }
    }
}

public partial class Task10StateOwningVisualFixture : Node2D
{
    public int CurrentHealth { get; set; } = 3;
}
