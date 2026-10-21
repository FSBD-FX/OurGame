using System;
using System.IO;
using System.Linq;
using Godot;
using Knight.Prologue.Residents;
using Knight.Tools;

namespace Knight.Build.Prologue;

// Build-only: no residence configuration, gameplay state or automatic mounting.
public partial class BuildResidentSubtypeScenes : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            if (args.Length != 2 || !args[0].StartsWith("--resident-output-root=", StringComparison.Ordinal) ||
                !args[1].StartsWith("--resident-profile-root=", StringComparison.Ordinal))
                throw new ArgumentException("Require output root and explicit source profile root, in that order.");
            BuildInto(args[0][23..], args[1][24..]);
            Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); Quit(1); }
    }

    public static void BuildInto(string outputRoot, string profileRoot)
    {
        RequireRoot(outputRoot, true);
        RequireRoot(profileRoot, false);
        foreach (ResidentSubtypeDefinition type in CanyonResidentCatalog.Subtypes)
        {
            string inputPath = Path.Combine(profileRoot, type.Id + ".tres");
            using var input = ResourceLoader.Load<ResidentSubtypeProfile>(inputPath,
                cacheMode: ResourceLoader.CacheMode.Ignore);
            if (input == null || input.SubtypeId != type.Id || input.Family != type.Family ||
                input.Archetype != type.Archetype || input.Carrier != type.Carrier)
                throw new ArgumentException("Missing or mismatched explicit profile for " + type.Id);
            // Preserve author-supplied balance bytes, never overwrite an output.
            // These are authoring inputs: changing one requires a checked rebuild.
            string profilePath = Path.Combine(outputRoot, type.Id + ".tres");
            File.Copy(inputPath, profilePath, false);
            // A build-owned pathless snapshot embeds in the leaf. Moving the
            // package then cannot retain an absolute previous-case profile path.
            using var profile = (ResidentSubtypeProfile)input.Duplicate();
            Node actor = GD.Load<PackedScene>(type.ProbeScenePath).Instantiate();
            try
            {
                // Existing distinct-leaf pattern. Nested visual scene references
                // stay intact; this does not preserve outer scene inheritance.
                actor.SceneFilePath = string.Empty;
                actor.AddChild(new ResidentSceneBinding { Name = "ResidentBinding", Profile = profile });
                string path = Path.Combine(outputRoot, type.Id + ".tscn");
                Error result = SceneBuildUtil.PackAndSave(actor, path);
                if (result != Error.Ok) throw new InvalidOperationException("Resident leaf save failed: " + type.Id + ": " + result);
                using var saved = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
                Node copy = saved.Instantiate();
                try
                {
                    ResidentSceneBinding binding = copy.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
                    if (binding?.Profile == null || binding.Profile.SubtypeId != type.Id ||
                        binding.Profile.Family != type.Family || binding.Profile.Archetype != type.Archetype ||
                        binding.Profile.Carrier != type.Carrier || binding.PresentationState != ResidentSceneState.ExplicitPlaceholder)
                        throw new InvalidOperationException("Saved leaf identity/profile round-trip failed: " + type.Id);
                }
                finally { copy.Free(); }
            }
            finally { actor.Free(); }
        }
    }

    private static void RequireRoot(string path, bool empty)
    {
        if (!Path.IsPathFullyQualified(path)) throw new ArgumentException("Staging roots must be absolute.");
        string root = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (root == Path.GetPathRoot(root)?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ||
            !Directory.Exists(root) || empty && Directory.EnumerateFileSystemEntries(root).Any())
            throw new ArgumentException("Require existing case folder; output must be empty.");
        foreach (string protectedPath in new[] { ProjectSettings.GlobalizePath("res://"), OS.GetUserDataDir() })
        {
            string protectedRoot = Path.GetFullPath(protectedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (Within(root, protectedRoot) || Within(protectedRoot, root))
                throw new ArgumentException("Staging cannot overlap protected project/userdata ancestry.");
        }
        for (DirectoryInfo parent = new(root); parent != null; parent = parent.Parent)
            if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new ArgumentException("Staging cannot traverse a reparse directory.");
    }

    private static bool Within(string path, string root) => path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}
