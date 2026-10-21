using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Guidance;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

/// <summary>
/// Saved, mounted controller with controlled snapshots in isolated storage.
/// This is not natural acquisition, backtracking, human-input or visual acceptance.
/// </summary>
public static class BossAdmissionGuidanceTests
{
    private static readonly string[] Fragments =
    [
        "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift",
        "canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.gear-gallery",
        "canyon.v2.fragment.ascent-mid", "canyon.v2.fragment.courtyard"
    ];

    public static async Task Run(SceneTree tree, bool capture = false)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        using var warnings = new ExpectedWarningLogger();
        try
        {
            var paths = IsolatedPrologueFixture.Configure(level, "boss-admission-guidance");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await Frames(tree, 3);
            flow.StartNew();
            await Frames(tree, 3);

            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var completion = level.GetNode<PrologueCompletionController>("Completion");
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");

            async Task Restore(PrologueSaveData saved)
            {
                level.RestoreFromSave(saved);
                completion.RestoreSession(PrologueObjective.DefeatBellkeeper);
                guidance.SetGuidanceEnabled(true);
                await Frames(tree, 3);
                TestAssert.Equal(PrologueObjective.DefeatBellkeeper, completion.CurrentObjective,
                    "controlled snapshot retains the pending Boss objective, not a natural route");
                TestAssert.True(Arena(level).RequiresFragmentAdmission && !Arena(level).IsEncounterActive,
                    "actual saved current-layout arena opts into admission and remains dormant");
            }

            // Catches the original bug: the visible instruction crosses a physically locked door.
            await Restore(Snapshot(5, true, true));
            TestAssert.True(!Entry(level).Disabled,
                "five current fragments and both original mechanisms leave the real entry locked");
            AssertBothLanguages(level, guidance, visual, completion, blocked: true,
                fragments: true, courtyard: false, bell: false, missing: 1, "five-current");
            if (capture) await CaptureBothLanguages(tree, guidance, "five-current");

            // The same mounted controller must switch immediately on actual progress changes.
            ulong dormantBoss = Arena(level).CurrentBoss.GetInstanceId();
            TestAssert.True(level.Progress.TryCollectFragment(Fragments[5]),
                "controlled sixth fragment changes actual progress once");
            await Frames(tree, 2);
            TestAssert.True(Entry(level).Disabled &&
                Arena(level).CurrentBoss.GetInstanceId() == dormantBoss,
                "sixth fragment opens the physical gate without replacing the dormant Boss");
            AssertBothLanguages(level, guidance, visual, completion, blocked: false,
                fragments: false, courtyard: false, bell: false, missing: 0, "six-current-live");
            if (capture) await CaptureBothLanguages(tree, guidance, "six-current-live");

            foreach ((bool courtyard, bool bell) in new[]
                     { (false, true), (true, false), (false, false) })
            {
                await Restore(Snapshot(6, courtyard, bell));
                TestAssert.True(!Entry(level).Disabled,
                    "six fragments cannot bypass either original mechanism");
                AssertBothLanguages(level, guidance, visual, completion, blocked: true,
                    fragments: false, courtyard: !courtyard, bell: !bell, missing: 0,
                    $"six-mechanisms-{courtyard}-{bell}");
            }

            await Restore(Snapshot(0, false, false));
            AssertBothLanguages(level, guidance, visual, completion, blocked: true,
                fragments: true, courtyard: true, bell: true, missing: 6, "all-requirements-missing");

            // Exercise the real schema3 migration reader in the admitted case directory.
            string legacyPath = Path.Combine(paths.CaseRoot, "schema3-source.json");
            const string oldWire = """
                {"version":3,"layoutVersion":2,"checkpointId":"canyon.v2.temple-forecourt",
                 "hasActivatedCheckpoint":true,"objective":"DefeatBellkeeper",
                 "openShortcuts":["canyon.v2.temple-bell-gate"],
                 "activatedMechanisms":["canyon.v2.courtyard-seal-open"]}
                """;
            File.WriteAllText(legacyPath, oldWire);
            PrologueSaveData legacy = PrologueSaveStore.LoadOrNew(legacyPath, 2);
            TestAssert.True(!legacy.WasRecovered && legacy.Version == 4 &&
                legacy.LegacyCollectionQuotaExempt && legacy.CollectedFragmentIds.Count == 0,
                "lawful raw schema3 activated forecourt grants quota exemption without fake fragments");
            TestAssert.Equal(oldWire, File.ReadAllText(legacyPath),
                "migration inspection preserves the isolated schema3 source bytes");
            await Restore(legacy);
            TestAssert.True(Entry(level).Disabled,
                "lawful exemption plus both real mechanisms opens physical entry at zero fragments");
            AssertBothLanguages(level, guidance, visual, completion, blocked: false,
                fragments: false, courtyard: false, bell: false, missing: 0, "legacy-eligible");

            foreach ((bool courtyard, bool bell) in new[]
                     { (false, true), (true, false), (false, false) })
            {
                PrologueSaveData incompleteLegacy = Snapshot(0, courtyard, bell);
                incompleteLegacy.LegacyCollectionQuotaExempt = legacy.LegacyCollectionQuotaExempt;
                await Restore(incompleteLegacy);
                TestAssert.True(!Entry(level).Disabled,
                    "trusted migrated exemption does not bypass original mechanisms");
                AssertBothLanguages(level, guidance, visual, completion, blocked: true,
                    fragments: false, courtyard: !courtyard, bell: !bell, missing: 0,
                    $"legacy-mechanisms-{courtyard}-{bell}");
            }

            await Restore(Snapshot(5, true, true));
            string beforeDisabled = DurableState(level);
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                guidance.SetLanguage(language);
                guidance.SetGuidanceEnabled(false);
                TestAssert.True(!guidance.CurrentInstruction.Visible &&
                    guidance.CurrentInstruction.Targets.Count == 0 && visual.RenderedTargetCount == 0 &&
                    !visual.HasVisiblePrompt && !Entry(level).Disabled,
                    $"{language} disabled guidance clears markers and text without opening admission");
            }
            TestAssert.Equal(beforeDisabled, DurableState(level),
                "disabling/localizing guidance does not mutate durable progress");
            TestAssert.Equal(PrologueObjective.DefeatBellkeeper, completion.CurrentObjective,
                "disabled guidance retains objective ownership");

            await Restore(Snapshot(6, true, true));
            BossArenaController arena = Arena(level);
            arena.BeginEncounter();
            FallenBellkeeper boss = arena.CurrentBoss;
            boss.SetPhysicsProcess(false);
            boss._PhysicsProcess(0);
            TestAssert.True(arena.IsEncounterActive, "eligible actual arena enters its active lifecycle");
            // Admission is no longer the owner after combat starts, even if progress is replaced.
            level.Progress.Restore(Snapshot(0, false, false));
            AssertOwnedTarget(guidance, visual, PrologueGuidanceTargetKey.BossBody, "active");

            boss._PhysicsProcess(boss.Tuning.IntroSeconds + .01);
            var health = boss.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.MaxHealth / 2,
                Vector2.Zero, 0, 712001UL)), "controlled actual Boss health enters phase-two range");
            boss._PhysicsProcess(0);
            boss._PhysicsProcess(boss.Tuning.TransitionSeconds + .01);
            boss._PhysicsProcess(boss.Tuning.PhaseTwoArmoredSeconds + .01);
            boss._PhysicsProcess(boss.Tuning.HammerThrowTelegraphSeconds + .01);
            TestAssert.True(boss.TryReceiveThread(default(ThreadInteraction)),
                "actual thrown-hammer chain accepts the controlled thread request");
            boss._PhysicsProcess(0);
            TestAssert.Equal(FallenBellkeeperState.Exposed, boss.CurrentFrame.State,
                "real Boss frame owns exposed-chain guidance");
            AssertOwnedTarget(guidance, visual, PrologueGuidanceTargetKey.BossChain, "chain-exposed");

            TestAssert.True(!warnings.Snapshot().Any(warning =>
                warning.Message.Contains("Guidance target", StringComparison.Ordinal)),
                "all mounted admission/active instructions resolve without unknown-target warnings");
            GD.Print("BOSS_ADMISSION_GUIDANCE_OK mounted=true controlledSnapshots=true naturalRoute=false languages=2");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
                else level.Free();
            }
        }
    }

    private static void AssertBothLanguages(PrologueLevelController level,
        PrologueGuidanceController guidance, WorldTargetMarkerVisual visual,
        PrologueCompletionController completion, bool blocked, bool fragments,
        bool courtyard, bool bell, int missing, string scenario)
    {
        string before = DurableState(level);
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            guidance.SetLanguage(language);
            guidance.RefreshNow();
            PrologueGuidanceInstruction instruction = guidance.CurrentInstruction;
            string label = $"{scenario}/{language}";
            TestAssert.True(instruction.Visible, label + " retains visible guidance");
            TestAssert.True(blocked
                ? !instruction.Targets.Contains(PrologueGuidanceTargetKey.BossActivation) &&
                  instruction.Targets.Count == 0 && visual.RenderedTargetCount == 0
                : instruction.Targets.Count == 1 &&
                  instruction.Targets[0] == PrologueGuidanceTargetKey.BossActivation &&
                  visual.RenderedTargetCount == 1,
                label + " blocked admission must not direct the player across the locked Boss door");
            string text = instruction.StepText;
            string fragmentWord = language == GameLanguage.English ? "fragment" : "残片";
            string courtyardWord = language == GameLanguage.English ? "courtyard" : "庭院";
            string bellWord = language == GameLanguage.English ? "bell" : "钟链";
            TestAssert.Equal(fragments, text.Contains(fragmentWord, StringComparison.OrdinalIgnoreCase),
                label + " names fragment shortage only when quota is unmet");
            TestAssert.Equal(courtyard, text.Contains(courtyardWord, StringComparison.OrdinalIgnoreCase),
                label + " names courtyard requirement only when its mechanism is unmet");
            // Allowed entry text names the Bellkeeper, so only blocked copy tests the bell keyword.
            if (blocked) TestAssert.Equal(bell, text.Contains(bellWord, StringComparison.OrdinalIgnoreCase),
                label + " names bell-chain requirement only when that mechanism is unmet");
            if (fragments) TestAssert.True(text.Contains(missing.ToString(), StringComparison.Ordinal),
                label + " tells the effective number of still-needed fragments");
            TestAssert.True(!text.Contains('{') && !text.Contains('}') &&
                visual.RenderedStepText == text &&
                visual.GetNode<Label>("Text").Text.Contains(text, StringComparison.Ordinal),
                label + " actual mounted Label receives the complete localized instruction");
            TestAssert.Equal(PrologueObjective.DefeatBellkeeper, completion.CurrentObjective,
                label + " guidance does not replace the real objective");
        }
        TestAssert.Equal(before, DurableState(level), scenario + " guidance is read-only over progress");
    }

    private static void AssertOwnedTarget(PrologueGuidanceController guidance,
        WorldTargetMarkerVisual visual, PrologueGuidanceTargetKey target, string scenario)
    {
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            guidance.SetLanguage(language);
            guidance.RefreshNow();
            TestAssert.True(guidance.CurrentInstruction.Visible &&
                guidance.CurrentInstruction.Targets.Count == 1 &&
                guidance.CurrentInstruction.Targets[0] == target && visual.RenderedTargetCount == 1,
                $"{scenario}/{language} combat owns its body/chain marker despite unmet admission metadata");
        }
    }

    private static string DurableState(PrologueLevelController level) =>
        JsonSerializer.Serialize(level.Progress.ToSave(PrologueSaveData.NewLayoutV2()));

    private static async Task CaptureBothLanguages(SceneTree tree,
        PrologueGuidanceController guidance, string scenario)
    {
        if (DisplayServer.GetName() == "headless") return;
        string storage = IsolatedPrologueFixture.ValidateStorageRoot();
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        {
            guidance.SetLanguage(language);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using Image frame = tree.Root.GetTexture().GetImage();
            string path = Path.Combine(storage, $"guidance-{scenario}-{language}.png");
            TestAssert.True(!File.Exists(path), "comparison capture must preserve prior originals");
            TestAssert.Equal(Error.Ok, frame.SavePng(path), "save actual rendered admission comparison");
        }
    }

    private static BossArenaController Arena(PrologueLevelController level) =>
        level.CurrentSectorRoot.GetNode<BossArenaController>("Gameplay/BossArenaController");

    private static CollisionShape2D Entry(PrologueLevelController level) =>
        level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Doors/Entry/Collision");

    private static PrologueSaveData Snapshot(int count, bool courtyard, bool bell)
    {
        var saved = PrologueSaveData.NewLayoutV2();
        saved.CheckpointId = "canyon.v2.temple-forecourt";
        saved.HasActivatedCheckpoint = true;
        saved.Objective = nameof(PrologueObjective.DefeatBellkeeper);
        for (int index = 0; index < count; index++) saved.CollectedFragmentIds.Add(Fragments[index]);
        if (courtyard) saved.ActivatedMechanisms.Add("canyon.v2.courtyard-seal-open");
        if (bell) saved.OpenShortcuts.Add("canyon.v2.temple-bell-gate");
        return saved.Normalize();
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
