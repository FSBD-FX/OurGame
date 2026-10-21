using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Integration;

namespace Knight.Tests;

// Isolated cache/finalizer observation only; never mounted into the playable level.
public partial class RunTask14ResourceCacheDiagnostic : SceneTree
{
    private const int Iterations = 80;
    private const string Sentry = "res://scenes/enemies/BellBoltSentry.tscn";
    private const string Skitter = "res://scenes/enemies/CanyonSkitter.tscn";

    public override async void _Initialize()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            string[] modes = args.Where(arg => arg.StartsWith("--cache-mode=", StringComparison.Ordinal))
                .ToArray();
            string[] scopes = args.Where(arg => arg.StartsWith("--scope=", StringComparison.Ordinal))
                .ToArray();
            bool disposeId = scopes.Length == 1 && scopes[0] == "--scope=adapter-mounted-dispose-id";
            bool adapterMounted = scopes.Length == 1 && scopes[0] == "--scope=adapter-mounted";
            if (scopes.Length > 1 ||
                (scopes.Length == 1 && !adapterMounted && !disposeId &&
                 scopes[0] != "--scope=level-offtree") ||
                modes.Length != (adapterMounted || disposeId ? 0 : 1) ||
                args.Count(arg => arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)) != 1 ||
                args.Any(arg => !arg.StartsWith("--cache-mode=", StringComparison.Ordinal) &&
                                !arg.StartsWith("--scope=", StringComparison.Ordinal) &&
                                !arg.StartsWith("--case-storage-root=", StringComparison.Ordinal)))
                throw new InvalidOperationException("diagnostic requires exact scope, mode and case storage");
            if (adapterMounted || disposeId)
            {
#if TASK14_DISPOSE_IDENTIFICATION
                GD.Print("TASK14_DISPOSE_PROBE_COMPILED=true");
#else
                GD.Print("TASK14_DISPOSE_PROBE_COMPILED=false");
                if (disposeId)
                    throw new InvalidOperationException("disposal identification scope requires its compile symbol");
#endif
                string storage = args.Single(arg => arg.StartsWith("--case-storage-root=",
                    StringComparison.Ordinal))["--case-storage-root=".Length..];
                await RunAdapterMounted(storage);
                Quit(0);
                return;
            }
            ResourceLoader.CacheMode mode = modes[0] switch
            {
                "--cache-mode=reuse" => ResourceLoader.CacheMode.Reuse,
                "--cache-mode=ignore-deep" => ResourceLoader.CacheMode.IgnoreDeep,
                _ => throw new InvalidOperationException("cache diagnostic mode must be reuse or ignore-deep")
            };
            if (scopes.Length == 1)
            {
                RunLevelOffTree(mode);
                Quit(0);
                return;
            }
            GD.Print($"TASK14_CACHE_DIAG_BEGIN mode={mode} iterations={Iterations}");
            for (int iteration = 1; iteration <= Iterations; iteration++)
            {
                GD.Print($"TASK14_CACHE_DIAG_ROUND_BEGIN mode={mode} iteration={iteration}");
                Cycle(Sentry, "sentry", mode, iteration);
                Cycle(Skitter, "skitter", mode, iteration);
                GD.Print($"TASK14_CACHE_DIAG_ROUND_DONE mode={mode} iteration={iteration}");
                if (iteration % 5 != 0) continue;
                GD.Print($"TASK14_CACHE_DIAG_COLLECT_BEGIN mode={mode} iteration={iteration}");
                GC.Collect(); // Deliberately do not wait: next load overlaps pending finalizers.
                GD.Print($"TASK14_CACHE_DIAG_COLLECT_RETURNED mode={mode} iteration={iteration}");
            }
            GD.Print($"TASK14_CACHE_DIAG_DRAIN_BEGIN mode={mode}");
            TestSceneTeardown.DrainManagedWrappersAtProcessExit($"task14-cache-{mode}");
            GD.Print($"TASK14_CACHE_DIAG_OK mode={mode} iterations={Iterations}");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private async Task RunAdapterMounted(string storage)
    {
        if (!Path.IsPathFullyQualified(storage) || !Directory.Exists(storage))
            throw new InvalidOperationException("adapter-mounted storage must be an existing absolute directory");
        string cases = Path.Combine(Path.GetFullPath(storage), "full-regression");
        GD.Print("TASK14_ADAPTER_MOUNTED_BEGIN rounds=4 mode=fixture-default");
        for (int round = 1; round <= 4; round++)
        {
            string[] before = AdapterCasePaths(cases);
            GD.Print($"TASK14_ADAPTER_MOUNTED_ROUND_BEGIN round={round} existing_cases={before.Length}");
            await Task14TelegraphAdapterTests.Run(this);
            string[] after = AdapterCasePaths(cases);
            string[] fresh = after.Except(before, StringComparer.OrdinalIgnoreCase).ToArray();
            if (fresh.Length != 1 || after.Length != before.Length + 1)
                throw new InvalidOperationException(
                    $"adapter-mounted round {round} created {fresh.Length} fresh case paths; expected exactly one");
            GD.Print($"TASK14_ADAPTER_MOUNTED_ROUND_DONE round={round} case_storage={fresh[0]}");
            GD.Print($"TASK14_ADAPTER_MOUNTED_COLLECT_BEGIN round={round}");
            GC.Collect(); // Observe the next mounted run without waiting for pending finalizers.
            GD.Print($"TASK14_ADAPTER_MOUNTED_COLLECT_RETURNED round={round}");
        }
        GD.Print("TASK14_ADAPTER_MOUNTED_DRAIN_BEGIN");
        TestSceneTeardown.DrainManagedWrappersAtProcessExit("task14-adapter-mounted");
        GD.Print("TASK14_ADAPTER_MOUNTED_OK rounds=4");
    }

    private static string[] AdapterCasePaths(string cases) => Directory.Exists(cases)
        ? Directory.GetDirectories(cases, "task14-firefly-sentry-*")
        : Array.Empty<string>();

    private static void RunLevelOffTree(ResourceLoader.CacheMode mode)
    {
        const int levelIterations = 12;
        GD.Print($"TASK14_LEVEL_CACHE_DIAG_BEGIN mode={mode} scope=level-offtree iterations={levelIterations}");
        for (int iteration = 1; iteration <= levelIterations; iteration++)
        {
            GD.Print($"TASK14_LEVEL_CACHE_DIAG_ROUND_BEGIN mode={mode} iteration={iteration}");
            LevelCycle(mode, iteration);
            GD.Print($"TASK14_LEVEL_CACHE_DIAG_ROUND_DONE mode={mode} iteration={iteration}");
            if (iteration % 3 != 0) continue;
            GD.Print($"TASK14_LEVEL_CACHE_DIAG_COLLECT_BEGIN mode={mode} iteration={iteration}");
            GC.Collect(); // No wait: the next Load overlaps any pending finalization.
            GD.Print($"TASK14_LEVEL_CACHE_DIAG_COLLECT_RETURNED mode={mode} iteration={iteration}");
        }
        GD.Print($"TASK14_LEVEL_CACHE_DIAG_DRAIN_BEGIN mode={mode}");
        TestSceneTeardown.DrainManagedWrappersAtProcessExit($"task14-level-cache-{mode}");
        GD.Print($"TASK14_LEVEL_CACHE_DIAG_OK mode={mode} iterations={levelIterations}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LevelCycle(ResourceLoader.CacheMode mode, int iteration)
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>(
            "res://scenes/prologue/PrologueLevel.tscn", "", mode) ??
            throw new InvalidOperationException("failed to load full PrologueLevel scene");
        PrologueLevelController level = scene.Instantiate<PrologueLevelController>();
        try
        {
            IsolatedPrologueFixturePaths paths = IsolatedPrologueFixture.Configure(level,
                $"task14-level-cache-{mode}-{iteration}");
            CharacterBody2D player = level.GetNode<CharacterBody2D>("PlayerRoot/Player");
            CollisionShape2D collider = player.GetNode<CollisionShape2D>("BodyCollider");
            Shape2D bodyShape = collider.Shape ??
                throw new InvalidOperationException("packed Player has no BodyCollider shape");
            SeamlessPrologueWorld world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            PrologueSectorDefinition sector = world.Definitions[0] ??
                throw new InvalidOperationException("full level has no first sector definition");
            PackedScene sectorScene = sector.Scene ??
                throw new InvalidOperationException("first sector has no PackedScene dependency");
            GD.Print($"TASK14_LEVEL_CACHE_DIAG_OBJECT mode={mode} iteration={iteration} " +
                     $"scene_id={scene.GetInstanceId()} level_id={level.GetInstanceId()} " +
                     $"player_id={player.GetInstanceId()} collider_id={collider.GetInstanceId()} " +
                     $"shape_id={bodyShape.GetInstanceId()} sector_definition_id={sector.GetInstanceId()} " +
                     $"sector_scene_id={sectorScene.GetInstanceId()} inside_tree={level.IsInsideTree()} " +
                     $"case_storage={paths.CaseRoot}");
        }
        finally
        {
            ulong levelId = level.GetInstanceId();
            level.Free();
            GD.Print($"TASK14_LEVEL_CACHE_DIAG_FREE mode={mode} iteration={iteration} " +
                     $"level_id={levelId} valid={GodotObject.IsInstanceValid(level)}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Cycle(string scenePath, string kind, ResourceLoader.CacheMode mode,
        int iteration)
    {
        PackedScene scene = ResourceLoader.Load<PackedScene>(scenePath, "", mode) ??
            throw new InvalidOperationException($"failed to load {scenePath}");
        CharacterBody2D actor = scene.Instantiate<CharacterBody2D>();
        try
        {
            Resource settings = actor switch
            {
                BellBoltSentry sentry => sentry.Settings,
                CanyonSkitter skitter => skitter.Settings,
                _ => throw new InvalidOperationException($"unexpected packed actor {actor.GetType().Name}")
            };
            CollisionShape2D body = actor.GetNode<CollisionShape2D>("BodyCollider");
            Shape2D shape = body.Shape ?? throw new InvalidOperationException("missing body shape");
            string dimensions = shape switch
            {
                CapsuleShape2D capsule => $"capsule:{capsule.Radius}:{capsule.Height}",
                RectangleShape2D rectangle => $"rectangle:{rectangle.Size.X}:{rectangle.Size.Y}",
                _ => throw new InvalidOperationException($"unexpected body shape {shape.GetType().Name}")
            };
            GD.Print($"TASK14_CACHE_DIAG_OBJECT mode={mode} iteration={iteration} kind={kind} " +
                     $"scene_id={scene.GetInstanceId()} actor_id={actor.GetInstanceId()} " +
                     $"settings_id={settings.GetInstanceId()} shape_id={shape.GetInstanceId()} " +
                     $"body={dimensions} offset={body.Position}");
        }
        finally
        {
            ulong actorId = actor.GetInstanceId();
            actor.Free();
            GD.Print($"TASK14_CACHE_DIAG_FREE mode={mode} iteration={iteration} " +
                     $"kind={kind} actor_id={actorId} valid={GodotObject.IsInstanceValid(actor)}");
        }
    }
}
