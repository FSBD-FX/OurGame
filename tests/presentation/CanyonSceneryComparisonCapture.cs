using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Presentation;

/// <summary>Actual generated game at fixed poses; never a claim of normal-input traversal.</summary>
public partial class CanyonSceneryComparisonCapture : SceneTree
{
    private const string MainPath = "res://scenes/prologue/PrologueLevel.tscn";
    private static readonly Vector2I WindowSize = new(1280, 720);
    private readonly record struct Sample(string Label, Vector2 Position, PrologueSectorId Sector);
    private static readonly Sample[] Samples =
    [
        new("entrance", new(160, 600), PrologueSectorId.CanyonEntrance),
        new("shaft", new(2400, 3044), PrologueSectorId.CanyonDescent),
        new("camp", new(1680, 3960), PrologueSectorId.CanyonBasin),
        new("lift", new(4700, 3424), PrologueSectorId.CanyonAscent),
        new("temple", new(5920, 1080), PrologueSectorId.LostTemple),
        new("archive", new(1520, 1604), PrologueSectorId.CanyonDescent),
        new("frozen-shelf", new(1400, 2144), PrologueSectorId.CanyonDescent),
        new("gear-platform", new(2550, 3880), PrologueSectorId.CanyonBasin),
        new("pendulum-shelf", new(4380, 2324), PrologueSectorId.CanyonAscent)
    ];

    public override async void _Initialize()
    {
        PrologueLevelController level = null;
        try
        {
            string project = ProjectSettings.GlobalizePath("res://");
            string userData = OS.GetUserDataDir();
            var invocation = ValidateInvocation(OS.GetCmdlineUserArgs(), project, userData,
                OS.HasEnvironment("KNIGHT_PLAYTEST_SOURCE"));
            string output = invocation.Output;
            string storage = invocation.Storage;
            CanyonSceneryCaptureContractTests.Run(output, storage);
            if (DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("scenery comparison needs an actual display");
            Root.ContentScaleSize = WindowSize;
            Root.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            Root.Size = WindowSize;
            DisplayServer.WindowSetSize(WindowSize);

            PackedScene packed = GD.Load<PackedScene>(MainPath) ??
                throw new InvalidOperationException("generated PrologueLevel scene missing");
            level = packed.Instantiate<PrologueLevelController>();
            level.Name = "Task15ActualGeneratedGame";
            PrologueGameFlowController flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = true;
            IsolatedPrologueFixture.Configure(level, "task15-scenery-comparison");
            CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            Camera2D camera = player.GetNode<Camera2D>("Camera2D");
            camera.Enabled = true;
            Root.AddChild(level);
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            await PhysicsFrames(8);
            if (flow.State != PrologueGameFlowState.Playing)
                throw new InvalidOperationException($"expected Playing after automation start, got {flow.State}");
            DisplayServer.WindowSetSize(WindowSize);
            await ToSignal(this, SignalName.ProcessFrame);
            foreach (string path in new[] { "Input", "Motor", "Combat", "Shield" })
                player.GetNode(path).SetPhysicsProcess(false);

            var manifest = new StringBuilder();
            manifest.AppendLine("kind=actual-generated-game-fixed-pose-not-played-route");
            manifest.AppendLine("held_player_nodes=Input,Motor,Combat,Shield");
            manifest.AppendLine($"project_default_renderer={ProjectSettings.GetSetting("rendering/renderer/rendering_method")}");
            manifest.AppendLine($"engine_argv={string.Join(' ', OS.GetCmdlineArgs())}");
            manifest.AppendLine($"adapter={RenderingServer.GetVideoAdapterName()}");
            manifest.AppendLine($"window={DisplayServer.WindowGetSize()} canvas={Root.GetVisibleRect().Size}");
            manifest.AppendLine($"backdrop_sha256={SourceHash("scripts/presentation/CanyonBackdropVisual.cs")}");
            manifest.AppendLine($"platform_sha256={SourceHash("scripts/presentation/CanyonPlatformVisual.cs")}");
            foreach (Sample sample in Samples)
            {
                player.GlobalPosition = sample.Position;
                player.Velocity = Vector2.Zero;
                await PhysicsFrames(12);
                if (world.CurrentSector != sample.Sector)
                    throw new InvalidOperationException($"{sample.Label} sector {world.CurrentSector}, expected {sample.Sector}");
                if (DisplayServer.WindowGetSize() != WindowSize ||
                    Root.GetVisibleRect().Size != (Vector2)WindowSize)
                    throw new InvalidOperationException($"{sample.Label} window/canvas changed");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using Image image = Root.GetTexture().GetImage();
                if (image.GetWidth() != WindowSize.X || image.GetHeight() != WindowSize.Y ||
                    image.IsEmpty())
                    throw new InvalidOperationException($"{sample.Label} invalid screenshot size");
                string png = Path.Combine(output, sample.Label + ".png");
                Error result = image.SavePng(png);
                if (result != Error.Ok || new FileInfo(png).Length < 4096)
                    throw new InvalidOperationException($"{sample.Label} PNG save failed: {result}");
                var query = PhysicsRayQueryParameters2D.Create(sample.Position,
                    sample.Position + new Vector2(0, 160));
                var hit = player.GetWorld2D().DirectSpaceState.IntersectRay(query);
                string collision = hit.Count == 0 ? "none-within-160px" :
                    $"{hit["collider"]}@{hit["position"]}";
                manifest.AppendLine($"sample={sample.Label}|player={player.GlobalPosition}|sector={world.CurrentSector}|" +
                    $"camera_global={camera.GlobalTransform}|canvas_transform={Root.CanvasTransform}|" +
                    $"collision_below={collision}|png_sha256={Sha(png)}");
                GD.Print($"TASK15_SCENERY_SAMPLE label={sample.Label} sector={world.CurrentSector} " +
                    $"player={player.GlobalPosition} camera={camera.GlobalTransform} collision_below={collision}");
            }
            File.WriteAllText(Path.Combine(output, "manifest.txt"), manifest.ToString());
            GD.Print($"TASK15_SCENERY_CAPTURE_OK samples={Samples.Length} output={output} " +
                $"adapter={RenderingServer.GetVideoAdapterName()}");
            level.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("TASK15_SCENERY_CAPTURE_FAIL: " + exception);
            if (GodotObject.IsInstanceValid(level) && level.IsInsideTree())
            {
                level.QueueFree();
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Quit(1);
        }
    }

    private async Task PhysicsFrames(int count)
    {
        for (int frame = 0; frame < count; frame++)
            await ToSignal(this, SignalName.PhysicsFrame);
    }

    private static string SingleArgument(string[] args, string prefix)
    {
        string[] matches = args.Where(value => value.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1 || matches[0].Length == prefix.Length)
            throw new ArgumentException($"exactly one {prefix}<absolute directory> is required");
        return matches[0][prefix.Length..];
    }

    internal static (string Output, string Storage) ValidateInvocation(string[] args,
        string project, string userData, bool hasGrantEnvironment)
    {
        string output = SingleArgument(args, "--output=");
        string storage = SingleArgument(args, "--case-storage-root=");
        if (args.Any(arg => arg is "--knight-playtest-grant-file" or
                "--knight-playtest-grant-nonce") || hasGrantEnvironment)
            throw new ArgumentException("hands-on evidence grants are forbidden in scenery capture");
        ValidatePaths(output, storage, project, userData);
        return (output, storage);
    }

    internal static void ValidatePaths(string output, string storage, string project, string userData)
    {
        if (Path.IsPathFullyQualified(output) && Path.IsPathFullyQualified(storage) &&
            Overlaps(output, storage))
            throw new ArgumentException("output and storage must not overlap");
        foreach (string path in new[] { output, storage })
        {
            if (!Path.IsPathFullyQualified(path))
                throw new ArgumentException("capture paths must be absolute");
            string full = Path.GetFullPath(path);
            if (!Directory.Exists(full))
                throw new ArgumentException("capture directories must already exist");
            AssertNoReparseAncestors(full);
            if (Directory.EnumerateFileSystemEntries(full).Any())
                throw new ArgumentException("capture directories must be empty");
            if (Overlaps(full, project) || Overlaps(full, userData))
                throw new ArgumentException("capture paths cannot overlap project or real userdata");
        }
    }

    private static void AssertNoReparseAncestors(string path)
    {
        for (DirectoryInfo current = new(path); current != null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException($"reparse ancestor rejected: {current.FullName}");
    }

    private static bool Overlaps(string left, string right)
    {
        string a = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string b = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return a.Equals(b, StringComparison.OrdinalIgnoreCase) ||
            a.StartsWith(b + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            b.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string SourceHash(string relative) => Sha(Path.Combine(
        ProjectSettings.GlobalizePath("res://"), relative));

    private static string Sha(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
