using System;
using System.IO;
using System.Linq;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests.Diagnostics;

/// <summary>
/// One isolated pre-Ready load boundary, not a gameplay regression or a fix.
/// No mount, explicit collection, cache override, resource disposal or user IO.
/// </summary>
public partial class RunQuestFragmentLoadBoundaryDiagnostic : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            string[] branches = args.Where(value => value.StartsWith("--branch=", StringComparison.Ordinal)).ToArray();
            string[] storage = args.Where(value => value.StartsWith("--case-storage-root=", StringComparison.Ordinal)).ToArray();
            if (args.Length != 2 || branches.Length != 1 || storage.Length != 1)
                throw new InvalidOperationException("boundary diagnostic requires one branch and one isolated storage root");
            string branch = branches[0]["--branch=".Length..];
            if (branch is not ("load-only" or "placement-load"))
                throw new InvalidOperationException("unknown boundary diagnostic branch");
            string root = storage[0]["--case-storage-root=".Length..];
            if (!Path.IsPathFullyQualified(root) || !Directory.Exists(root))
                throw new InvalidOperationException("boundary diagnostic requires an existing absolute storage root");

            GD.Print($"QUEST_FRAGMENT_LOAD_BOUNDARY_BEGIN branch={branch}");
            if (branch == "placement-load")
            {
                GD.Print("QUEST_FRAGMENT_LOAD_BOUNDARY_BEFORE_PLACEMENT");
                QuestFragmentPlacementTests.Run();
            }
            GD.Print($"QUEST_FRAGMENT_LOAD_BOUNDARY_BEFORE_LOAD branch={branch}");
            PackedScene scene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                ?? throw new InvalidOperationException("formal PrologueLevel failed to load");
            GD.Print($"QUEST_FRAGMENT_LOAD_BOUNDARY_RETURNED branch={branch} scene_id={scene.GetInstanceId()}");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
