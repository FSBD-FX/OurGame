using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Prologue;
using Knight.Save;

namespace Knight.Tests.Integration;

public static class QuestFragmentPersistenceRuntimeTests
{
    // Controlled metadata fixture, NOT contact collection or a normal-route proof.
    public static async Task Run(SceneTree tree)
    {
        PrologueLevelController level = null;
        string savePath = null;
        int mountSequence = 0;
        try
        {
            async Task<PrologueLevelController> Mount(string existingSave = null)
            {
                bool observe = ++mountSequence == 1;
                if (observe) QuestFragmentLoadObservation.Record("first-load-enter");
                var mounted = QuestFragmentLoadObservation.AfterLoad(
                    GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn"), observe)
                    .Instantiate<PrologueLevelController>();
                if (observe) QuestFragmentLoadObservation.Record("first-instantiate-return");
                var paths = IsolatedPrologueFixture.Configure(mounted, "fragment-persistence");
                var controller = mounted.GetNode<PrologueGameFlowController>("GameFlow");
                controller.HeadlessAutoStart = false;
                if (existingSave != null) controller.SavePath = existingSave;
                else savePath = paths.SavePath;
                tree.Root.AddChild(mounted);
                await Frames(tree, 5);
                return mounted;
            }
            level = await Mount();
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.StartNew();
            await Frames(tree, 12);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && level.Player.IsOnFloor(),
                "fragment metadata fixture starts on the supported real initial platform");
            TestAssert.True(level.Progress.TryCollectFragment("canyon.v2.fragment.entrance") &&
                level.Progress.TryCollectFragment("canyon.v2.fragment.rift"), "controlled state records two distinct known fragments");
            level.Progress.Discover("supply_cave");
            flow.PersistProgress("fragment-state-fixture");
            QuestFragmentCrossProcessTests.AssertCurrentWire(savePath, legacy: false);
            var saved = PrologueSaveStore.LoadOrNew(savePath, 2);
            TestAssert.True(!saved.WasRecovered && saved.CollectedFragmentIds.Count == 2 &&
                saved.CollectionRuleVersion == 1 && !saved.LegacyCollectionQuotaExempt &&
                saved.Discoveries.Contains("supply_cave"),
                "real flow persistence copies independent fragment state, not just the old discovery collection");
            int recoveries = 0;
            level.RecoveryCompleted += _ => recoveries++;
            var health = level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Zero, 0f,
                650001UL, level.Player.GetInstanceId(), DamageGuardClass.Unblockable)),
                "controlled lethal hit uses the actual health/recovery pipeline");
            for (int frame = 0; frame < 120 && recoveries == 0; frame++) await Frames(tree, 1);
            await Frames(tree, 4);
            TestAssert.True(recoveries == 1 && flow.State == PrologueGameFlowState.Playing &&
                level.Progress.CollectedFragmentIds.Count == 2 && health.CurrentHealth > 0 &&
                level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.start",
                "real first-checkpoint death preserves fragments and recovers to the original initial checkpoint");
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            level = null;
            level = await Mount(savePath);
            flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            string beforeContinue = Hash(savePath);
            TestAssert.True(flow.HasSave, "unloaded/remounted scene discovers its isolated collection save");
            flow.Continue();
            await Frames(tree, 5);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                level.Progress.CollectedFragmentIds.SetEquals(new[] { "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift" }) &&
                level.Progress.CollectionRuleVersion == 1 && !level.Progress.LegacyCollectionQuotaExempt,
                "scene unload and real Continue retain independent collection state");
            TestAssert.Equal(beforeContinue, Hash(savePath), "Continue reads without rewriting the collection source slot");
            QuestFragmentCrossProcessTests.PreserveFlowWrittenSlot(savePath, "collected");
            flow.ReturnToTitle();
            flow.StartNew();
            await Frames(tree, 4);
            TestAssert.True(level.Progress.CollectedFragmentIds.Count == 0 && !level.Progress.LegacyCollectionQuotaExempt &&
                PrologueSaveStore.LoadOrNew(savePath, 2).CollectedFragmentIds.Count == 0,
                "explicit new game resets live and persisted collection state");

            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            level = null;
            string legacyEligiblePath = Path.Combine(Path.GetDirectoryName(savePath)!, "schema3-eligible-save.json");
            File.WriteAllText(legacyEligiblePath, """
                {"version":3,"layoutVersion":2,"checkpointId":"canyon.v2.temple-forecourt",
                 "hasActivatedCheckpoint":true,"checkpointSector":"LostTemple",
                 "checkpointWorldX":5920,"checkpointWorldY":1080,
                 "openShortcuts":[],"activatedMechanisms":[],"discoveries":[],
                 "completedTutorials":[],"shortcutOpen":false,"bossDefeated":false,
                 "completed":false,"objective":"PullTempleMechanism"}
                """);
            string beforeLegacyContinue = Hash(legacyEligiblePath);
            level = await Mount(legacyEligiblePath);
            flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            TestAssert.True(flow.HasSave && flow.Save.Version == 4 && flow.Save.CollectionRuleVersion == 1 &&
                flow.Save.LegacyCollectionQuotaExempt && flow.Save.CollectedFragmentIds.Count == 0,
                "real flow loading establishes legitimate old schema3 entitlement without manufacturing collected fragments");
            TestAssert.Equal(beforeLegacyContinue, Hash(legacyEligiblePath),
                "loading the eligible schema3 fixture does not rewrite its original bytes");
            flow.Continue();
            await Frames(tree, 5);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && level.Progress.CollectionRuleVersion == 1 &&
                level.Progress.LegacyCollectionQuotaExempt && level.Progress.CollectedFragmentIds.Count == 0 &&
                level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.temple-forecourt",
                "real Continue restores the eligible old checkpoint with honest zero-fragment progress");
            TestAssert.Equal(beforeLegacyContinue, Hash(legacyEligiblePath),
                "Continue alone does not rewrite the eligible schema3 source slot");
            flow.PersistProgress("legacy-fragment-entitlement-fixture");
            QuestFragmentCrossProcessTests.AssertCurrentWire(legacyEligiblePath, legacy: true);
            var eligibleSaved = PrologueSaveStore.LoadOrNew(legacyEligiblePath, 2);
            TestAssert.True(!eligibleSaved.WasRecovered && eligibleSaved.Version == 4 &&
                eligibleSaved.CollectionRuleVersion == 1 && eligibleSaved.LegacyCollectionQuotaExempt &&
                eligibleSaved.CollectedFragmentIds.Count == 0 && eligibleSaved.OpenShortcuts.Count == 0 &&
                eligibleSaved.ActivatedMechanisms.Count == 0,
                "real PersistProgress retains true legacy entitlement without inventing six fragments or old controls");
            QuestFragmentCrossProcessTests.PreserveFlowWrittenSlot(legacyEligiblePath, "legacy");

            int eligibleRecoveries = 0;
            level.RecoveryCompleted += _ => eligibleRecoveries++;
            health = level.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ApplyDamage(new DamageRequest(health.CurrentHealth, Vector2.Zero, 0f,
                650002UL, level.Player.GetInstanceId(), DamageGuardClass.Unblockable)),
                "eligible legacy fixture takes a real lethal hit through the health/recovery pipeline");
            for (int frame = 0; frame < 120 && eligibleRecoveries == 0; frame++) await Frames(tree, 1);
            await Frames(tree, 4);
            TestAssert.True(eligibleRecoveries == 1 && flow.State == PrologueGameFlowState.Playing &&
                health.CurrentHealth > 0 && level.Progress.CollectionRuleVersion == 1 &&
                level.Progress.LegacyCollectionQuotaExempt && level.Progress.CollectedFragmentIds.Count == 0 &&
                level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.temple-forecourt",
                "actual death keeps the migrated quota entitlement and zero fragments at the old checkpoint");
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            level = null;
            level = await Mount(legacyEligiblePath);
            flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            string beforeEligibleRemountContinue = Hash(legacyEligiblePath);
            TestAssert.True(flow.HasSave, "unloaded eligible schema4 save remains available to real Continue");
            flow.Continue();
            await Frames(tree, 5);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && level.Progress.CollectionRuleVersion == 1 &&
                level.Progress.LegacyCollectionQuotaExempt && level.Progress.CollectedFragmentIds.Count == 0 &&
                level.Progress.RestoreCheckpoint().CheckpointId == "canyon.v2.temple-forecourt",
                "unload and Continue preserve true entitlement without fabricating collected records");
            TestAssert.Equal(beforeEligibleRemountContinue, Hash(legacyEligiblePath),
                "eligible schema4 Continue reads without rewriting its source slot");
            flow.ReturnToTitle();
            TestAssert.True(level.Progress.LegacyCollectionQuotaExempt && level.Progress.CollectedFragmentIds.Count == 0,
                "returning to the title alone does not clear the migrated quota entitlement");
            flow.StartNew();
            await Frames(tree, 4);
            var eligibleReset = PrologueSaveStore.LoadOrNew(legacyEligiblePath, 2);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing &&
                level.Progress.CollectionRuleVersion == 1 && !level.Progress.LegacyCollectionQuotaExempt &&
                level.Progress.CollectedFragmentIds.Count == 0 && !eligibleReset.WasRecovered &&
                eligibleReset.CollectionRuleVersion == 1 && !eligibleReset.LegacyCollectionQuotaExempt &&
                eligibleReset.CollectedFragmentIds.Count == 0 && eligibleReset.CheckpointId == "canyon.v2.start",
                "only explicit new game clears real legacy entitlement in both live progress and the persisted save");
            GD.Print("QUEST_FRAGMENT_PERSISTENCE_RUNTIME_OK controlled_metadata_fixture=true contact_pickup=false normal_route=false");
        }
        finally
        {
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }
    private static async Task Frames(SceneTree tree, int count)
    { for (int frame = 0; frame < count; frame++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
