using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Knight.Prologue;

namespace Knight.Tests.Unit;

public static class ProloguePlaytestEvidenceGrantTests
{
    private static readonly string DedicatedGrantDirectory = Path.GetFullPath(
        Path.Combine(Path.GetTempPath(), "knight-playtest-grants"));

    public static void Run()
    {
        ReviewConsumptionContractIsFailClosed();
        AtomicFileClaimMakesOriginalPathUnavailableBeforeDecision();
        FreshMatchingGrantAuthorizesExactlyOnce();
        BomPrefixedHelperPayloadStillAuthorizes();
        ForbiddenModesRejectAndConsumeEvenWithValidGrant();
        EditorAndCaptureModesCannotAuthorizeHumanTelemetry();
        InvalidOrStaleGrantFailsClosed();
    }

    private static void ReviewConsumptionContractIsFailClosed()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-27T08:00:00Z");
        var failures = new List<string>();
        foreach ((string surface, bool userArgument) in new[]
                 {
                     ("--headless", false), ("--editor", false),
                     ("--project-manager", false), ("--script", false),
                     ("--write-movie", false), ("--capture", false),
                     ("--automation", false), ("--headless", true),
                     ("--capture", true)
                 })
        {
            var store = Store(now, "nonce-forbidden");
            string[] userArguments = userArgument
                ? ValidUserArguments("nonce-forbidden", [surface])
                : null;
            bool authorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                Context(now, store, "nonce-forbidden",
                    userArgument ? null : [surface], userArguments), out _);
            if (authorized || store.ConsumeCount != 1 || store.Files.ContainsKey(GrantPath()) ||
                ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                    Context(now, store, "nonce-forbidden"), out _))
                failures.Add($"forbidden:{surface}:{(userArgument ? "user" : "process")}:" +
                    $"authorized={authorized}:consumed={store.ConsumeCount}");
        }

        foreach ((string name, string json, string nonce, string project) item in new[]
                 {
                     ("blank", "   ", "nonce-invalid", "C:/Knight"),
                     ("corrupt", "{ nope", "nonce-invalid", "C:/Knight"),
                     ("expired", Grant(now.AddMinutes(-3), "nonce-invalid", "C:/Knight"),
                         "nonce-invalid", "C:/Knight"),
                     ("nonce", Grant(now, "other", "C:/Knight"), "nonce-invalid", "C:/Knight"),
                     ("project", Grant(now, "nonce-invalid", "C:/Elsewhere"),
                         "nonce-invalid", "C:/Knight"),
                     ("source", Grant(now, "nonce-invalid", "C:/Knight", "ForgedSource"),
                         "nonce-invalid", "C:/Knight")
                 })
        {
            var store = Store(now, item.nonce, item.json);
            bool authorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                Context(now, store, item.nonce, projectPath: item.project), out _);
            if (authorized || store.ConsumeCount != 1 || store.Files.ContainsKey(GrantPath()) ||
                ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                    Context(now, store, item.nonce, projectPath: item.project), out _))
                failures.Add($"content:{item.name}:authorized={authorized}:" +
                    $"consumed={store.ConsumeCount}");
        }

        var duplicateNonceStore = Store(now, "nonce-duplicate");
        string[] duplicateNonce =
        [
            "--knight-playtest-grant-file", GrantPath(),
            "--knight-playtest-grant-nonce", "nonce-duplicate",
            "--knight-playtest-grant-nonce", "nonce-duplicate"
        ];
        bool duplicateNonceAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
            Context(now, duplicateNonceStore, "nonce-duplicate",
                userArguments: duplicateNonce), out _);
        if (duplicateNonceAuthorized || duplicateNonceStore.ConsumeCount != 1 ||
            duplicateNonceStore.Files.ContainsKey(GrantPath()))
            failures.Add($"duplicate-nonce:authorized={duplicateNonceAuthorized}:" +
                $"consumed={duplicateNonceStore.ConsumeCount}");

        foreach ((string name, string environmentSource) in new[]
                 { ("missing", (string)null), ("forged", "ForgedHandsOnSource") })
        {
            var sourceStore = Store(now, "nonce-environment");
            bool sourceAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                Context(now, sourceStore, "nonce-environment",
                    environmentSource: environmentSource), out _);
            bool replayAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                Context(now, sourceStore, "nonce-environment"), out _);
            if (sourceAuthorized || replayAuthorized || sourceStore.ConsumeCount != 1 ||
                sourceStore.Files.ContainsKey(GrantPath()))
                failures.Add($"environment-source:{name}:authorized={sourceAuthorized}:" +
                    $"replay={replayAuthorized}:consumed={sourceStore.ConsumeCount}");
        }

        string duplicateA = GrantPath("duplicate-a.json");
        string duplicateB = GrantPath("duplicate-b.json");
        var duplicatePathStore = new GrantStore(new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            [duplicateA] = Grant(now, "nonce-path", "C:/Knight"),
            [duplicateB] = Grant(now, "nonce-path", "C:/Knight")
        });
        string[] duplicatePaths =
        [
            "--knight-playtest-grant-file", duplicateA,
            "--knight-playtest-grant-file", duplicateB,
            "--knight-playtest-grant-nonce", "nonce-path"
        ];
        bool duplicatePathAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
            Context(now, duplicatePathStore, "nonce-path", userArguments: duplicatePaths), out _);
        if (duplicatePathAuthorized || duplicatePathStore.ConsumeCount != 0 ||
            !duplicatePathStore.Files.ContainsKey(duplicateA) ||
            !duplicatePathStore.Files.ContainsKey(duplicateB))
            failures.Add($"duplicate-path:authorized={duplicatePathAuthorized}:" +
                $"consumed={duplicatePathStore.ConsumeCount}");

        foreach ((string name, string[] arguments) in new[]
                 {
                     ("missing", new[] { "--knight-playtest-grant-nonce", "nonce-shape" }),
                     ("blank", new[] { "--knight-playtest-grant-file", "",
                         "--knight-playtest-grant-nonce", "nonce-shape" }),
                     ("missing-value", new[] { "--knight-playtest-grant-nonce", "nonce-shape",
                         "--knight-playtest-grant-file" }),
                     ("equals-shape", new[] { "--knight-playtest-grant-file=" + GrantPath(),
                         "--knight-playtest-grant-nonce", "nonce-shape" })
                 })
        {
            GrantStore shapeStore = Store(now, "nonce-shape");
            bool shapeAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                Context(now, shapeStore, "nonce-shape", userArguments: arguments), out _);
            if (shapeAuthorized || shapeStore.ConsumeAttempts != 0 ||
                !shapeStore.Files.ContainsKey(GrantPath()))
                failures.Add($"path-shape:{name}:authorized={shapeAuthorized}:" +
                    $"attempts={shapeStore.ConsumeAttempts}:" +
                    $"preserved={shapeStore.Files.ContainsKey(GrantPath())}");
        }

        GrantStore malformedUserStore = Store(now, "nonce-malformed-user");
        string[] malformedUserArguments = ValidUserArguments("nonce-malformed-user", [null]);
        bool malformedUserAuthorized = false;
        bool malformedUserThrew = false;
        try
        {
            malformedUserAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                Context(now, malformedUserStore, "nonce-malformed-user",
                    userArguments: malformedUserArguments), out _);
        }
        catch
        {
            malformedUserThrew = true;
        }
        if (malformedUserAuthorized || malformedUserThrew ||
            malformedUserStore.ConsumeCount != 1 ||
            malformedUserStore.Files.ContainsKey(GrantPath()))
            failures.Add($"malformed-user:authorized={malformedUserAuthorized}:" +
                $"threw={malformedUserThrew}:consumed={malformedUserStore.ConsumeCount}");

        string outside = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
            "knight-playtest-grants-prefix-collision", "outside.json"));
        var outsideStore = new GrantStore(new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
            { [outside] = Grant(now, "nonce-outside", "C:/Knight") });
        bool outsideAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
            Context(now, outsideStore, "nonce-outside",
                userArguments: ValidUserArguments("nonce-outside", grantPath: outside)), out _);
        if (outsideAuthorized || outsideStore.ConsumeCount != 0 ||
            !outsideStore.Files.ContainsKey(outside))
            failures.Add($"outside:authorized={outsideAuthorized}:" +
                $"consumed={outsideStore.ConsumeCount}:preserved={outsideStore.Files.ContainsKey(outside)}");

        string traversal = Path.Combine(DedicatedGrantDirectory, "..", "task9-arbitrary.txt");
        string canonicalTraversal = Path.GetFullPath(traversal);
        var traversalStore = new GrantStore(new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
            { [canonicalTraversal] = Grant(now, "nonce-traversal", "C:/Knight") });
        bool traversalAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
            Context(now, traversalStore, "nonce-traversal",
                userArguments: ValidUserArguments("nonce-traversal", grantPath: traversal)), out _);
        if (traversalAuthorized || traversalStore.ConsumeCount != 0 ||
            !traversalStore.Files.ContainsKey(canonicalTraversal))
            failures.Add($"traversal:authorized={traversalAuthorized}:" +
                $"consumed={traversalStore.ConsumeCount}:" +
                $"preserved={traversalStore.Files.ContainsKey(canonicalTraversal)}");

        var consumeThrowStore = Store(now, "nonce-throw");
        consumeThrowStore.ThrowOnConsume = true;
        bool consumeThrowAuthorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
            Context(now, consumeThrowStore, "nonce-throw"), out _);
        if (consumeThrowAuthorized || consumeThrowStore.ConsumeAttempts != 1 ||
            consumeThrowStore.ConsumeCount != 0 ||
            !string.Equals(consumeThrowStore.ConsumedPaths[0], GrantPath(), PathComparison()))
            failures.Add($"consume-throw:authorized={consumeThrowAuthorized}:" +
                $"attempts={consumeThrowStore.ConsumeAttempts}:" +
                $"consumed={consumeThrowStore.ConsumeCount}");

        TestAssert.Equal(0, failures.Count,
            "review grant-consumption contract failures: " + string.Join(" | ", failures));
    }

    private static void AtomicFileClaimMakesOriginalPathUnavailableBeforeDecision()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-27T08:00:00Z");
        Directory.CreateDirectory(DedicatedGrantDirectory);
        string path = GrantPath(Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, Grant(now, "nonce-atomic", "C:/Knight"));
        try
        {
            string[] userArguments = ValidUserArguments("nonce-atomic", grantPath: path);
            var context = new ProloguePlaytestLaunchContext(
                ["--headless"], userArguments, "C:/Knight", now, "HandsOnLauncher",
                ProloguePlaytestEvidenceGrantVerifier.ConsumeGrantFileAtomically);
            TestAssert.True(!ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(context, out _),
                "forbidden real-file launch is denied after the atomic grant claim");
            TestAssert.True(!File.Exists(path),
                "atomic claim removes the original capability path before terminal rejection");

            var replay = new ProloguePlaytestLaunchContext(
                [], userArguments, "C:/Knight", now, "HandsOnLauncher",
                ProloguePlaytestEvidenceGrantVerifier.ConsumeGrantFileAtomically);
            TestAssert.True(!ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(replay, out _),
                "the atomically claimed real grant cannot be replayed by a clean launch");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void BomPrefixedHelperPayloadStillAuthorizes()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-27T08:00:00Z");
        GrantStore store = Store(now, "nonce-bom",
            "\uFEFF" + Grant(now, "nonce-bom", "C:/Knight"));
        TestAssert.True(ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                Context(now, store, "nonce-bom"), out string source) &&
            source == "HandsOnLauncher",
            "runtime verifier tolerates a legacy PowerShell UTF-8 BOM at the grant boundary");
    }

    private static void FreshMatchingGrantAuthorizesExactlyOnce()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-27T08:00:00Z");
        GrantStore store = Store(now, "nonce-a");
        var context = Context(now, store, "nonce-a");
        TestAssert.True(ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                context, out string source) && source == "HandsOnLauncher",
            "fresh matching launcher grant authorizes production HandsOn telemetry");
        TestAssert.True(!store.Files.ContainsKey(GrantPath()) && store.ConsumeCount == 1 &&
            string.Equals(store.ConsumedPaths[0], GrantPath(), PathComparison()),
            "successful authorization consumes the bounded per-run grant");
        TestAssert.True(!ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                context, out _),
            "consumed launcher grant cannot be replayed");
    }

    private static void ForbiddenModesRejectAndConsumeEvenWithValidGrant()
    {
        foreach (string forbidden in new[]
                 {
                     "--headless", "--editor", "--project-manager", "--script",
                     "--write-movie", "--capture", "--automation"
                 })
        {
            DateTimeOffset now = DateTimeOffset.Parse("2026-08-27T08:00:00Z");
            GrantStore store = Store(now, "nonce-b");
            ProloguePlaytestLaunchContext context = Context(now, store, "nonce-b",
                [forbidden]);
            TestAssert.True(!ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                    context, out _),
                $"{forbidden} fails closed despite a forged environment and valid grant file");
            TestAssert.True(store.ConsumeCount == 1 && !store.Files.ContainsKey(GrantPath()),
                $"{forbidden} consumes the identified dedicated grant before rejecting");
        }
    }

    private static void EditorAndCaptureModesCannotAuthorizeHumanTelemetry()
    {
        foreach (string mode in new[] { "--headless", "--editor", "--capture", "--write-movie" })
        {
            DateTimeOffset now = DateTimeOffset.Parse("2026-08-29T00:00:00Z");
            GrantStore store = Store(now, "task5-" + mode.TrimStart('-'));
            bool authorized = ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(
                Context(now, store, "task5-" + mode.TrimStart('-'), [mode]), out string source);
            TestAssert.True(!authorized && source == null,
                $"{mode} cannot obtain a HandsOnLauncher report source");
            TestAssert.True(store.ConsumeCount == 1 && !store.Files.ContainsKey(GrantPath()),
                $"{mode} consumes the forged grant before denying automated human telemetry");
        }
    }

    private static void InvalidOrStaleGrantFailsClosed()
    {
        DateTimeOffset now = DateTimeOffset.Parse("2026-08-27T08:00:00Z");
        foreach ((string name, string json, string nonce, string project) item in new[]
                 {
                     ("nonce mismatch", Grant(now, "other", "C:/Knight"), "nonce-c", "C:/Knight"),
                     ("wrong project", Grant(now, "nonce-c", "C:/Elsewhere"), "nonce-c", "C:/Knight"),
                     ("expired", Grant(now.AddMinutes(-3), "nonce-c", "C:/Knight"), "nonce-c", "C:/Knight"),
                     ("future", Grant(now.AddSeconds(5), "nonce-c", "C:/Knight"), "nonce-c", "C:/Knight"),
                     ("corrupt", "{ nope", "nonce-c", "C:/Knight")
                 })
        {
            GrantStore store = Store(now, item.nonce, item.json);
            ProloguePlaytestLaunchContext context = Context(now, store, item.nonce,
                projectPath: item.project);
            TestAssert.True(!ProloguePlaytestEvidenceGrantVerifier.TryAuthorize(context, out _),
                $"{item.name} grant fails closed");
            TestAssert.True(store.ConsumeCount == 1 && !store.Files.ContainsKey(GrantPath()),
                $"{item.name} grant is consumed before its terminal rejection");
        }
    }

    private static ProloguePlaytestLaunchContext Context(DateTimeOffset now,
        GrantStore store, string nonce, string[] processArguments = null,
        string[] userArguments = null, string projectPath = "C:/Knight",
        string environmentSource = "HandsOnLauncher") => new(
        processArguments ?? [],
        userArguments ?? ValidUserArguments(nonce),
        projectPath,
        now,
        environmentSource,
        store.Consume);

    private static string[] ValidUserArguments(string nonce, string[] suffix = null,
        string grantPath = null)
    {
        var arguments = new List<string>
        {
            "--knight-playtest-grant-file", grantPath ?? GrantPath(),
            "--knight-playtest-grant-nonce", nonce
        };
        if (suffix != null) arguments.AddRange(suffix);
        return arguments.ToArray();
    }

    private static GrantStore Store(DateTimeOffset now, string nonce, string json = null) =>
        new(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [GrantPath()] = json ?? Grant(now, nonce, "C:/Knight")
        });

    private static string GrantPath(string fileName = "grant.json") =>
        Path.GetFullPath(Path.Combine(DedicatedGrantDirectory, fileName));

    private static StringComparison PathComparison() => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static string Grant(DateTimeOffset issued, string nonce, string projectPath,
        string source = "HandsOnLauncher") =>
        JsonSerializer.Serialize(new
        {
            version = 1,
            source,
            projectPath,
            issuedAtUtc = issued.ToString("O"),
            expiresAtUtc = issued.AddSeconds(90).ToString("O"),
            nonce
        });

    private sealed class GrantStore(Dictionary<string, string> files)
    {
        public Dictionary<string, string> Files { get; } = files;
        public int ConsumeAttempts { get; private set; }
        public int ConsumeCount { get; private set; }
        public List<string> ConsumedPaths { get; } = [];
        public bool ThrowOnConsume { get; set; }

        public string Consume(string path)
        {
            ConsumeAttempts++;
            string canonical = Path.GetFullPath(path);
            ConsumedPaths.Add(canonical);
            if (ThrowOnConsume) throw new IOException("injected consume failure");
            if (!Files.Remove(canonical, out string json))
                throw new FileNotFoundException("grant already consumed", canonical);
            ConsumeCount++;
            return json;
        }
    }
}
