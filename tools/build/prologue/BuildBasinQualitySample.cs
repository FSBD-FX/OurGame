using Godot;
using Knight.Config;
using Knight.Enemies.Laborer;
using Knight.Prologue.Sample;
using Knight.Prologue.Seamless;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildBasinQualitySample : SceneTree
{
    private const string ScenePath = "res://scenes/prologue/sample/BasinQualitySample.tscn";
    private const string LaborerPath = "res://scenes/prologue/sample/BasinLaborer.tscn";

    public override void _Initialize()
    {
        string directory = ProjectSettings.GlobalizePath("res://scenes/prologue/sample");
        Error directoryError = DirAccess.MakeDirRecursiveAbsolute(directory);
        if (directoryError is not (Error.Ok or Error.AlreadyExists))
        {
            GD.PushError($"BuildBasinQualitySample could not create output directory: {directoryError}");
            Quit(1);
            return;
        }

        Error actorError = BuildSampleLaborer();
        if (actorError != Error.Ok)
        {
            GD.PushError($"BuildBasinQualitySample laborer failed: {actorError}");
            Quit(1);
            return;
        }

        // Author the single-encounter sample before packing, so reset/remount uses the
        // same contract as the normal world, without late runtime enemy removal.
        PrologueSector basin = GD.Load<PackedScene>(BasinSampleLegacyLayout.FrozenScenePath(
            PrologueSectorId.CanyonBasin)).Instantiate<PrologueSector>();
        basin.SceneFilePath = string.Empty;
        ApplySampleEnvironment(basin);
        Node rust = basin.GetNode("Gameplay/Actors/RustShellSquire");
        rust.GetParent().RemoveChild(rust);
        rust.Free();
        var originalLaborer = basin.GetNode<BellBackLaborer>("Gameplay/Actors/BellBackLaborer");
        var laborer = GD.Load<PackedScene>(LaborerPath).Instantiate<BellBackLaborer>();
        laborer.Name = originalLaborer.Name;
        laborer.Position = originalLaborer.Position;
        Node actors = originalLaborer.GetParent();
        actors.RemoveChild(originalLaborer);
        originalLaborer.Free();
        actors.AddChild(laborer);
        laborer.Tuning = (BellBackLaborerTuning)laborer.Tuning.Duplicate();
        // Camp is 270px away: choosing to approach, not reading the controls, starts combat.
        // Leave damage, armor, movement, attack telegraph and counter windows unchanged.
        laborer.Tuning.DetectionRange = 200f;
        Error sectorError = SaveSampleAndValidate(basin,
            BasinQualitySampleEntry.SectorScenePath, out int before, out int after,
            out string failure);
        basin.Free();
        if (sectorError != Error.Ok)
        {
            GD.PushError($"BuildBasinQualitySample sector failed: {failure}");
            Quit(1);
            return;
        }
        GD.Print($"BuildBasinQualitySample: sector nodes before={before} after={after}");

        var entry = new BasinQualitySampleEntry { Name = "BasinQualitySample" };
        Error saveError = SceneBuildUtil.PackAndSave(entry, ScenePath);
        entry.Free();
        if (saveError != Error.Ok)
        {
            GD.PushError($"BuildBasinQualitySample failed: {saveError}");
            Quit(1);
            return;
        }

        BasinQualitySampleEntry copy = GD.Load<PackedScene>(ScenePath)
            ?.Instantiate<BasinQualitySampleEntry>();
        bool valid = copy != null && copy.Name == "BasinQualitySample" &&
            copy.StorageDirectory == BasinQualitySampleEntry.DefaultStorageDirectory;
        copy?.Free();
        if (!valid)
        {
            GD.PushError("BuildBasinQualitySample packed-scene verification failed.");
            Quit(1);
            return;
        }
        GD.Print("BuildBasinQualitySample: packed and validated root=1");
        Quit(0);
    }

    private static void ApplySampleEnvironment(PrologueSector basin)
    {
        // These independent art slots are sample-only. No gameplay node is moved or replaced.
        foreach (string layer in new[] { "Background", "Midground" })
        {
            Node root = basin.GetNode("VisualRoot/" + layer);
            foreach (Node child in root.GetChildren())
            {
                root.RemoveChild(child);
                child.Free();
            }
            root.AddChild(new BasinEnvironmentVisual
            {
                Name = layer == "Background" ? "AtmosphericRift" : "BuriedSanctum",
                SectorBounds = basin.Bounds,
                DistantLayer = layer == "Background"
            });
        }
        Node readable = basin.GetNode("VisualRoot/GameplayReadable");
        foreach (string name in new[] { "EntryCatch", "SalvageFloor", "BasinFloor" })
        {
            var old = readable.GetNode<CanyonPlatformVisual>(name);
            int siblingIndex = old.GetIndex();
            var replacement = new BasinPlatformVisual
            {
                Name = old.Name, Position = old.Position, SkinSize = old.SkinSize,
                SurfaceMaterial = old.SurfaceMaterial, CatchShelf = old.CatchShelf,
                DetailStartX = name == "BasinFloor" ? 800f : 0f
            };
            foreach (Node child in old.GetChildren())
            {
                child.Owner = null;
                old.RemoveChild(child);
                replacement.AddChild(child);
            }
            readable.RemoveChild(old);
            old.Free();
            readable.AddChild(replacement);
            readable.MoveChild(replacement, siblingIndex);
        }
    }

    private static Error SaveSampleAndValidate(PrologueSector basin, string path,
        out int before, out int after, out string failure)
    {
        before = Count(basin);
        after = 0;
        failure = string.Empty;
        Error error = SceneBuildUtil.PackAndSave(basin, path);
        if (error != Error.Ok) { failure = error.ToString(); return error; }
        PrologueSector copy = GD.Load<PackedScene>(path).Instantiate<PrologueSector>();
        try
        {
            after = Count(copy);
            PrologueSectorDefinition definition = BasinSampleLegacyLayout.CreateDefinitions()[2];
            var errors = copy.ValidateContract(definition);
            if (before != after || errors.Count > 0)
            {
                failure = $"sample node count {before}/{after}: " + string.Join("; ", errors);
                return Error.InvalidData;
            }
            return Error.Ok;
        }
        finally { copy.Free(); }
    }

    private static int Count(Node node)
    {
        int count = 1;
        foreach (Node child in node.GetChildren()) count += Count(child);
        return count;
    }

    private static Error BuildSampleLaborer()
    {
        var actor = GD.Load<PackedScene>("res://scenes/enemies/BellBackLaborer.tscn")
            .Instantiate<BellBackLaborer>();
        // Pack a distinct leaf: do not edit nested children of an inherited instance
        // in the sector and accidentally retain the original visual at reload.
        actor.SceneFilePath = string.Empty;
        Node root = actor.GetNode("VisualRoot");
        Node old = root.GetNode("Formal");
        root.RemoveChild(old);
        old.Free();
        root.AddChild(new BasinLaborerVisual { Name = "Formal" });
        var sweep = root.GetNode<AttackTelegraphVisual>("AttackTelegraph");
        sweep.ImpactFillOpacity = .06f;
        sweep.ImpactTint = new Color("edbb72");
        sweep.ImpactOutlineWidth = 2.5f;
        Error result = SceneBuildUtil.PackAndSave(actor, LaborerPath);
        actor.Free();
        if (result != Error.Ok) return result;
        Node copy = GD.Load<PackedScene>(LaborerPath).Instantiate();
        bool valid = copy.GetNodeOrNull<BasinLaborerVisual>("VisualRoot/Formal") != null &&
            copy.GetNodeOrNull("VisualRoot/AttackTelegraph") != null &&
            copy.GetNodeOrNull("AttackHitbox/AttackCollider") != null &&
            copy.GetNodeOrNull("ThreadTarget/LegacyThreadInteractionAdapter") != null;
        copy.Free();
        return valid ? Error.Ok : Error.InvalidData;
    }
}
