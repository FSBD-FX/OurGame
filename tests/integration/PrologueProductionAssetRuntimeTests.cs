using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

public static class PrologueProductionAssetRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        string root = ProjectSettings.GlobalizePath("res://");
        string json = File.ReadAllText(Path.Combine(root, "assets", "production", "prologue-assets.json"));
        ProductionAssetManifest manifest = ProductionAssetManifestParser.Parse(json);
        TestAssert.True(manifest.Assets.Count == 8 && manifest.Assets.All(asset => asset.Final),
            "all eight production asset families are final");
        TestAssert.Equal(0, manifest.RequireRuntimeFiles(root).Count,
            "all final production manifest files exist");

        PackedScene packed = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn");
        TestAssert.True(packed != null, "production audit loads the final prologue scene");
        PrologueLevelController level = packed.Instantiate<PrologueLevelController>();
        IsolatedPrologueFixture.Configure(level, nameof(PrologueProductionAssetRuntimeTests));
        level.GetNode<PrologueAudioDirector>("Audio").PlaybackEnabled = false;
        level.GetNode<PrologueGameFlowController>("GameFlow").AutomationAutoStart = true;
        tree.Root.AddChild(level);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        IReadOnlyList<string> visibleFallbacks = PrologueProductionAudit.FindVisibleFallbacks(level);
        TestAssert.Equal(0, visibleFallbacks.Count,
            "final prologue has no visible placeholder, debug, fallback, or gray-box visuals");
        IReadOnlyList<string> invalidResources = PrologueProductionAudit.FindInvalidVisibleResources(level);
        TestAssert.Equal(0, invalidResources.Count,
            "every visible final texture, label, and font resolves at runtime");

        var sectors = new List<Node>();
        try
        {
            foreach (PrologueSectorId id in System.Enum.GetValues<PrologueSectorId>())
            {
                PrologueSector sector = GD.Load<PackedScene>(
                        $"res://scenes/prologue/sectors/{id}.tscn")
                    .Instantiate<PrologueSector>();
                TestAssert.Equal(id, sector.SectorId,
                    $"real generated sector scene {id} preserves its unique identity");
                sectors.Add(sector);
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

        level.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
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
