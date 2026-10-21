using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;

namespace Knight.Tests.Integration;

// Controlled first-five transactions, mechanism prerequisites and actor poses.
// Sixth collection and Boss admission use original authored entities and native
// keyboard/physics contacts. This is NOT natural traversal or human input.
public static class FragmentSixAdmissionRuntimeTests
{
    private const string Sixth = "canyon.v2.fragment.temple-upper";
    private const string Forecourt = "canyon.v2.temple-forecourt";
    private const string Seal = "canyon.v2.courtyard-seal-open";
    private const string Bell = "canyon.v2.temple-bell-gate";
    private static readonly string[] FirstFive =
    [
        "canyon.v2.fragment.entrance", "canyon.v2.fragment.rift",
        "canyon.v2.fragment.abyss-floor", "canyon.v2.fragment.gear-gallery",
        "canyon.v2.fragment.ascent-mid"
    ];

    public static async Task Run(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        try
        {
            var paths = IsolatedPrologueFixture.Configure(level, "fragment-six-producer");
            var flow = ConfigureFlow(level);
            tree.Root.AddChild(level);
            await Frames(tree, 5);
            flow.StartNew();
            await Frames(tree, 8);
            var world = level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple, new Vector2(5920, 1080)),
                "controlled placement mounts the authored Temple forecourt, not a JSON seed");
            await Frames(tree, 10);
            TestAssert.True(flow.Save.CheckpointId == Forecourt && flow.Save.HasActivatedCheckpoint &&
                world.CheckpointId == Forecourt && level.Progress.RestoreCheckpoint().CheckpointId == Forecourt,
                "actual forecourt lamp contact commits the producer checkpoint before locking its slot");
            TestAssert.True(level.Progress.ActivateMechanism(Seal), "controlled setup activates original seal prerequisite");
            level.RecordShortcutOpened(Bell); // Production persistence also saves the controlled seal prerequisite.
            foreach (string id in FirstFive)
                TestAssert.Equal(FragmentCollectionResult.Collected, flow.TryCollectFragment(id),
                    "controlled first-five setup uses real durable Flow transactions");
            await Frames(tree, 3);
            AssertState(level, paths.SavePath, 5, "producer before sixth");

            var fragment = level.CurrentSectorRoot.GetNode<QuestFragment>("Gameplay/QuestFragments/temple-upper");
            var visual = fragment.GetNode<Node2D>("AvailableVisual");
            var status = fragment.GetNode<Label>("Status");
            var health = level.Player.GetNode<HealthComponent>("Health");
            int startingHealth = health.CurrentHealth, requested = 0, recovered = 0;
            level.RecoveryRequested += _ => requested++;
            level.RecoveryCompleted += _ => recovered++;
            int contacts = 0, collectedEvents = 0;
            bool committedBeforeNotification = true;
            fragment.BodyEntered += body =>
            {
                if (body != level.Player) return;
                contacts++;
                KeyEvent(Key.A, false); // Stop on the actual native contact, in the same callback frame.
            };
            fragment.Collected += id =>
            {
                collectedEvents++;
                committedBeforeNotification &= id == Sixth && StateAgrees(level, paths.SavePath, 6) &&
                    fragment.IsCollected && !visual.Visible && status.Text.Length == 0 &&
                    fragment.LastResult == FragmentCollectionResult.Collected && fragment.FeedbackCount == 1 &&
                    NoTemporarySaves(paths);
            };
            using var gate = new GateObservation(level);
            AssertAuthoredFragment(level, fragment);
            await PlaceOutsideFragment(tree, level, fragment);
            await Capture(tree, paths.CaseRoot, "01-before-sixth-cn.png");
            string fiveHash = Hash(paths.SavePath);
            PrologueSaveData oldSave = flow.Save;
            using (var replaceDenied = new FileStream(paths.SavePath, FileMode.Open,
                System.IO.FileAccess.Read, FileShare.Read))
            {
                TestAssert.True(!PrologueSaveStore.LoadOrNew(paths.SavePath, 2).WasRecovered,
                    "FileShare.Read permits real preflight reading while denying the atomic replacement");
                await WalkIntoFragment(tree, level, fragment, () => contacts, 1);
                TestAssert.True(fragment.LastResult == FragmentCollectionResult.SaveFailed &&
                    !fragment.IsCollected && visual.Visible && status.Text.Contains("保存失败", StringComparison.Ordinal) &&
                    fragment.FeedbackCount == 0 && collectedEvents == 0 && ReferenceEquals(oldSave, flow.Save) &&
                    Hash(paths.SavePath) == fiveHash && NoTemporarySaves(paths),
                    "failed actual sixth replacement keeps old bytes/Save, original pickup and feedback retryable");
                AssertState(level, paths.SavePath, 5, "failed sixth native contact");
                gate.AssertDormant(open: false);
                await Capture(tree, paths.CaseRoot, "02-sixth-replace-failed-cn.png");
            }
            TestAssert.Equal(fiveHash, Hash(paths.SavePath), "unlocking failed contact does not promote five IDs to six");
            string fiveCopy = PreserveProducer(paths.SavePath, "fragment-six-five.json");
            await RefuseAtGate(tree, level, gate, paths.SavePath, fiveHash, paths.CaseRoot);

            await PlaceOutsideFragment(tree, level, fragment);
            TestAssert.True(contacts == 1 && fragment.LastResult == FragmentCollectionResult.SaveFailed &&
                fragment.FeedbackCount == 0 && Hash(paths.SavePath) == fiveHash,
                "retry genuinely leaves overlap without a new claim or implicit unlock write");
            await WalkIntoFragment(tree, level, fragment, () => contacts, 2);
            AssertState(level, paths.SavePath, 6, "successful sixth native reentry");
            TestAssert.True(collectedEvents == 1 && fragment.FeedbackCount == 1 && committedBeforeNotification &&
                fragment.IsCollected && !visual.Visible && status.Text.Length == 0 &&
                !ReferenceEquals(oldSave, flow.Save) && Hash(paths.SavePath) != fiveHash && NoTemporarySaves(paths),
                "one reentry commits six exact IDs and spent visual before the sole Collected notification");
            gate.AssertDormant(open: true);
            string sixHash = Hash(paths.SavePath);
            string sixCopy = PreserveProducer(paths.SavePath, "fragment-six-six.json");

            // A third real contact under the same replacement lock must not even
            // turn a spent pickup into a failed transaction or repeat its feedback.
            await PlaceOutsideFragment(tree, level, fragment);
            DateTime lastWrite = File.GetLastWriteTimeUtc(paths.SavePath);
            PrologueSaveData committedSave = flow.Save;
            using (var duplicateReplaceDenied = new FileStream(paths.SavePath, FileMode.Open,
                System.IO.FileAccess.Read, FileShare.Read))
            {
                await WalkIntoFragment(tree, level, fragment, () => contacts, 3);
                TestAssert.True(collectedEvents == 1 && fragment.FeedbackCount == 1 &&
                    fragment.LastResult == FragmentCollectionResult.Collected && ReferenceEquals(committedSave, flow.Save) &&
                    Hash(paths.SavePath) == sixHash && File.GetLastWriteTimeUtc(paths.SavePath) == lastWrite &&
                    NoTemporarySaves(paths), "duplicate native spent contact under lock performs no durable rewrite or repeated feedback");
            }
            AssertAuthoredFragment(level, fragment);
            await EnterAtGate(tree, level, gate, paths.SavePath, sixHash, paths.CaseRoot);
            AssertState(level, paths.SavePath, 6, "admitted Boss keeps all six IDs");
            TestAssert.True(requested == 0 && recovered == 0 && health.CurrentHealth == startingHealth &&
                contacts == 3 && collectedEvents == 1 && fragment.FeedbackCount == 1 &&
                Hash(fiveCopy) == fiveHash && Hash(sixCopy) == sixHash,
                "producer uses no death/recovery/damage, consumes no fragments and retains both prebattle producer originals");
            GD.Print($"FRAGMENT_SIX_ADMISSION_OK contacts={contacts} collected_events={collectedEvents} feedback={fragment.FeedbackCount} " +
                $"intro_entries={gate.IntroEntries} native_trigger_entries={gate.TriggerEntries} five_sha={fiveHash} six_sha={sixHash} " +
                $"slot={paths.SavePath} controlled_prerequisites=true controlled_positions=true natural_route=false human=false");
        }
        finally
        {
            ReleaseKeys();
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
        }
    }

    public static async Task RunCold(SceneTree tree)
    {
        string storage = IsolatedPrologueFixture.ValidateStorageRoot();
        string inputs = Path.Combine(Directory.GetParent(storage)!.FullName, "fragment-six-input");
        foreach (var (tag, count) in new[] { ("five", 5), ("six", 6) })
        {
            var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
                .Instantiate<PrologueLevelController>();
            try
            {
                // Runner pins provenance/hash of the actual terminated producer.
                // C# never manufactures a JSON seed or embeds an expected source hash.
                string source = Path.Combine(inputs, tag + ".json");
                string sourceHash = Hash(source);
                var paths = IsolatedPrologueFixture.Configure(level, "fragment-six-cold-" + tag);
                File.Copy(source, paths.SavePath, overwrite: false);
                TestAssert.True(DiskAgrees(paths.SavePath, count) && Hash(paths.SavePath) == sourceHash,
                    "cold fixture copies actual producer bytes into a fresh process-owned slot");
                var flow = ConfigureFlow(level);
                int sessions = 0, requested = 0, recovered = 0;
                bool restoredAtSession = false;
                Vector2 initial = default;
                level.RecoveryRequested += _ => requested++;
                level.RecoveryCompleted += _ => recovered++;
                flow.SessionStarted += () =>
                {
                    sessions++;
                    initial = level.Player.GlobalPosition;
                    restoredAtSession = StateAgrees(level, paths.SavePath, count) && initial == new Vector2(5920, 1080);
                };
                tree.Root.AddChild(level);
                await Frames(tree, 5);
                TestAssert.True(flow.State == PrologueGameFlowState.Title && flow.HasSave && sessions == 0 &&
                    flow.LoadResult.Status == PrologueSaveLoadStatus.Current && Hash(paths.SavePath) == sourceHash,
                    "cold current-schema producer offers Continue without rewriting its bytes");
                var start = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start");
                var resume = level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Continue");
                TestAssert.True(start.HasFocus() && !resume.Disabled, "cold title initially focuses Start and enables Continue");
                KeyEvent(Key.Down, true);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                KeyEvent(Key.Down, false);
                TestAssert.True(resume.HasFocus(), "native title Down selects Continue");
                KeyEvent(Key.Enter, true);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                KeyEvent(Key.Enter, false);
                await Frames(tree, 12);
                TestAssert.True(sessions == 1 && restoredAtSession && flow.State == PrologueGameFlowState.Playing &&
                    level.Player.GlobalPosition.DistanceTo(initial) < 2f && Supported(level, shelf: false) &&
                    Hash(paths.SavePath) == sourceHash, "only native title keys restore exact producer state before gameplay");
                AssertState(level, paths.SavePath, count, "cold " + tag + " Continue");
                var fragment = level.CurrentSectorRoot.GetNode<QuestFragment>("Gameplay/QuestFragments/temple-upper");
                AssertAuthoredFragment(level, fragment);
                TestAssert.True(fragment.IsCollected == (count == 6) && fragment.FeedbackCount == 0 &&
                    fragment.GetNode<Node2D>("AvailableVisual").Visible == (count == 5),
                    "cold producer state restores original sixth pickup availability without reward replay");
                int health = level.Player.GetNode<HealthComponent>("Health").CurrentHealth;
                using var gate = new GateObservation(level);
                if (count == 5) await RefuseAtGate(tree, level, gate, paths.SavePath, sourceHash, paths.CaseRoot);
                else await EnterAtGate(tree, level, gate, paths.SavePath, sourceHash, paths.CaseRoot);
                AssertState(level, paths.SavePath, count, "cold " + tag + " physical admission result");
                TestAssert.True(requested == 0 && recovered == 0 && sessions == 1 &&
                    level.Player.GetNode<HealthComponent>("Health").CurrentHealth == health && Hash(source) == sourceHash,
                    "cold admission uses no recovery/damage/new session and never mutates its producer input");
                if (count == 5) TestAssert.Equal(sourceHash, Hash(paths.SavePath), "refused cold five slot stays byte-identical");
                GD.Print($"FRAGMENT_SIX_COLD_OK kind={tag} source_sha={sourceHash} fragments={count} " +
                    $"intro_entries={gate.IntroEntries} native_trigger_entries={gate.TriggerEntries} title_keys=true " +
                    "controlled_positions=true producer_was_controlled=true seeded_json=false human=false");
            }
            finally
            {
                ReleaseKeys();
                tree.Paused = false;
                if (GodotObject.IsInstanceValid(level)) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, level);
            }
        }
    }

    private static PrologueGameFlowController ConfigureFlow(PrologueLevelController level)
    {
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        return flow;
    }

    private static void AssertAuthoredFragment(PrologueLevelController level, QuestFragment fragment)
    {
        TestAssert.True(fragment.StableId == Sixth && fragment.GlobalPosition == new Vector2(5896, 924) &&
            fragment.GetNode<CollisionShape2D>("Collision").Shape is CircleShape2D { Radius: 18 } &&
            fragment.Monitoring && !fragment.GetNode<CollisionShape2D>("Collision").Disabled &&
            level.Player.GetNode<CollisionShape2D>("BodyCollider").Shape is CapsuleShape2D { Radius: 20, Height: 72 },
            "sixth uses original saved temple-upper and original radius20 height72 player, never a substitute collectible");
        var shelf = level.CurrentSectorRoot.GetNode<CollisionShape2D>("Gameplay/Collision/TempleSupplyShelf/Collision");
        TestAssert.True(!shelf.Disabled && shelf.GlobalPosition == new Vector2(5972.5f, 980) &&
            shelf.Shape is RectangleShape2D { Size: var size } && size == new Vector2(215, 40),
            "original TempleSupplyShelf remains at world rectangle 5865,960,215,40");
    }

    private static async Task PlaceOutsideFragment(SceneTree tree, PrologueLevelController level, QuestFragment fragment)
    {
        Place(level, new Vector2(5948, 924));
        await Frames(tree, 6);
        TestAssert.True(!fragment.OverlapsBody(level.Player) && Supported(level, shelf: true) &&
            level.Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing(),
            "controlled shelf stance fully supports original capsule outside sixth overlap with motor enabled");
    }

    private static async Task WalkIntoFragment(SceneTree tree, PrologueLevelController level,
        QuestFragment fragment, Func<int> contacts, int expected)
    {
        KeyEvent(Key.A, true);
        try
        {
            for (int tick = 0; tick < 30 && contacts() < expected; tick++) await Frames(tree, 1);
        }
        finally { KeyEvent(Key.A, false); }
        await Frames(tree, 4);
        TestAssert.True(contacts() == expected && fragment.OverlapsBody(level.Player) && Supported(level, shelf: true),
            "native A produces exactly one real authored BodyEntered while remaining supported on the shelf");
    }

    private static async Task RefuseAtGate(SceneTree tree, PrologueLevelController level,
        GateObservation gate, string savePath, string expectedHash, string captureRoot)
    {
        Place(level, new Vector2(6120, 1080));
        await Frames(tree, 5);
        gate.AssertDormant(open: false);
        int health = level.Player.GetNode<HealthComponent>("Health").CurrentHealth;
        bool nativeGateContact = false;
        KeyEvent(Key.D, true);
        try
        {
            for (int tick = 0; tick < 36; tick++)
            {
                await Frames(tree, 1);
                for (int index = 0; index < level.Player.GetSlideCollisionCount(); index++)
                {
                    GodotObject collider = level.Player.GetSlideCollision(index).GetCollider();
                    nativeGateContact |= collider == gate.Entry.GetParent() || collider == gate.Barrier.GetParent();
                }
                TestAssert.True(level.Player.GlobalPosition.X + 20 <= 6142.5f && !gate.Arena.IsEncounterActive &&
                    !gate.Exit.Disabled && Hash(savePath) == expectedHash,
                    "native D cannot cross five-fragment gate or rewrite its rejected slot");
            }
        }
        finally { KeyEvent(Key.D, false); }
        TestAssert.True(nativeGateContact && Supported(level, shelf: false) && gate.TriggerEntries == 0 &&
            gate.IntroEntries == 0 && level.Player.GetNode<HealthComponent>("Health").CurrentHealth == health,
            "five-fragment refusal actually contacts native gate with full floor support and no damage/activation");
        await Capture(tree, captureRoot, "03-five-native-gate-refusal-cn.png");
        KeyEvent(Key.A, true);
        try { await Frames(tree, 12); }
        finally { KeyEvent(Key.A, false); }
        await Frames(tree, 5);
        TestAssert.True(level.Player.GlobalPosition.X < 6095 && Supported(level, shelf: false) &&
            Hash(savePath) == expectedHash, "actual A retreats safely from closed gate without writing five-ID slot");
        gate.AssertDormant(open: false);
        GD.Print("FRAGMENT_SIX_FIVE_REFUSAL_OK native_d=true native_a_retreat=true supported=true unchanged_bytes=true q_tested=false");
    }

    private static async Task EnterAtGate(SceneTree tree, PrologueLevelController level,
        GateObservation gate, string savePath, string expectedHash, string captureRoot)
    {
        Place(level, new Vector2(6120, 1080));
        await Frames(tree, 5);
        gate.AssertDormant(open: true);
        TestAssert.True(Hash(savePath) == expectedHash && !gate.Trigger.OverlapsBody(level.Player),
            "six-ID gate opens before entry, without remount/reset or an implicit prebattle save");
        await Capture(tree, captureRoot, "04-six-native-gate-open-cn.png");
        bool unchangedAtNativeEntry = false;
        void OnNativeEntry(Node2D body)
        {
            if (body == level.Player) unchangedAtNativeEntry = Hash(savePath) == expectedHash && StateAgrees(level, savePath, 6);
        }
        gate.Trigger.BodyEntered += OnNativeEntry;
        KeyEvent(Key.D, true);
        try
        {
            for (int tick = 0; tick < 50 && (!gate.Arena.IsEncounterActive || level.Player.GlobalPosition.X - 20 < 6180); tick++)
            {
                if (!gate.Arena.IsEncounterActive)
                    TestAssert.Equal(expectedHash, Hash(savePath), "pre-encounter six-ID bytes remain untouched");
                await Frames(tree, 1);
            }
        }
        finally { KeyEvent(Key.D, false); gate.Trigger.BodyEntered -= OnNativeEntry; }
        await Frames(tree, 4);
        TestAssert.True(unchangedAtNativeEntry && gate.TriggerEntries == 1 && gate.IntroEntries == 1 && gate.BossReplacements == 0 &&
            gate.Arena.IsEncounterActive && gate.Arena.CurrentBoss.GetInstanceId() == gate.BossId &&
            !gate.Entry.Disabled && !gate.Exit.Disabled && gate.HullDisabled(true) &&
            level.Player.GlobalPosition.X - 20 >= 6180 && Supported(level, shelf: false),
            "actual D crosses original ActivationTrigger, begins original Boss once, and closes combat doors only");
        await Frames(tree, 6);
        TestAssert.True(gate.TriggerEntries == 1 && gate.IntroEntries == 1 && gate.BossReplacements == 0 &&
            gate.Arena.IsEncounterActive && gate.Arena.CurrentBoss.GetInstanceId() == gate.BossId,
            "continued actual trigger overlap does not replay encounter or replace the Boss");
        await Capture(tree, captureRoot, "05-six-native-encounter-cn.png");
    }

    private sealed class GateObservation : IDisposable
    {
        public readonly BossArenaController Arena;
        public readonly CollisionShape2D Entry, Exit, Barrier;
        public readonly Area2D Trigger;
        public readonly ulong BossId;
        private readonly PrologueLevelController _level;
        private readonly FallenBellkeeper _boss;
        private readonly CollisionShape2D[] _hull;
        public int TriggerEntries { get; private set; }
        public int IntroEntries { get; private set; }
        public int BossReplacements { get; private set; }

        public GateObservation(PrologueLevelController level)
        {
            _level = level;
            var sector = level.CurrentSectorRoot;
            Arena = sector.GetNode<BossArenaController>("Gameplay/BossArenaController");
            Entry = sector.GetNode<CollisionShape2D>("Gameplay/Doors/Entry/Collision");
            Exit = sector.GetNode<CollisionShape2D>("Gameplay/Doors/Exit/Collision");
            Barrier = sector.GetNode<CollisionShape2D>("Gameplay/Doors/AdmissionBarrier/Collision");
            _hull = [Barrier, Barrier.GetParent().GetNode<CollisionShape2D>("TopCollision"),
                Barrier.GetParent().GetNode<CollisionShape2D>("RightCollision")];
            Trigger = sector.GetNode<Area2D>("Gameplay/ActivationTrigger");
            _boss = Arena.CurrentBoss;
            BossId = _boss.GetInstanceId();
            Trigger.BodyEntered += OnBodyEntered;
            _boss.FrameChanged += OnFrame;
            Arena.BossReady += OnBossReady;
        }

        public bool HullDisabled(bool disabled) => Array.TrueForAll(_hull, shape => shape.Disabled == disabled);
        public void AssertDormant(bool open) => TestAssert.True(Arena.RequiresFragmentAdmission &&
            !Arena.IsEncounterActive && Arena.CurrentBoss.GetInstanceId() == BossId &&
            _boss.CurrentFrame.State == FallenBellkeeperState.Dormant && IntroEntries == 0 &&
            BossReplacements == 0 && Entry.Disabled == open && HullDisabled(open) && !Exit.Disabled &&
            Entry.GetParent().GetNode<CanvasItem>("ReadableDoor").Visible != open,
            "original dormant Boss, native admission hull and visible entry agree with durable quota");
        private void OnBodyEntered(Node2D body) { if (body == _level.Player) TriggerEntries++; }
        private void OnFrame(FallenBellkeeperFrame frame, int facing)
        { if (frame.EnteredState && frame.State == FallenBellkeeperState.Intro) IntroEntries++; }
        private void OnBossReady() => BossReplacements++;
        public void Dispose()
        {
            if (GodotObject.IsInstanceValid(Trigger)) Trigger.BodyEntered -= OnBodyEntered;
            if (GodotObject.IsInstanceValid(_boss)) _boss.FrameChanged -= OnFrame;
            if (GodotObject.IsInstanceValid(Arena)) Arena.BossReady -= OnBossReady;
        }
    }

    private static bool DiskAgrees(string path, int count)
    {
        var disk = PrologueSaveStore.LoadOrNew(path, 2);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement raw = document.RootElement;
        string[] ids = raw.GetProperty("collectedFragmentIds").EnumerateArray().Select(id => id.GetString()).ToArray();
        string[] expected = count == 5 ? FirstFive : FirstFive.Append(Sixth).ToArray();
        return !disk.WasRecovered && raw.GetProperty("version").GetInt32() == 4 &&
            raw.GetProperty("layoutVersion").GetInt32() == 2 && raw.GetProperty("collectionRuleVersion").GetInt32() == 1 &&
            !raw.GetProperty("legacyCollectionQuotaExempt").GetBoolean() &&
            ids.Length == count && ids.Distinct(StringComparer.Ordinal).Count() == count &&
            ids.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(expected.OrderBy(id => id, StringComparer.Ordinal)) &&
            disk.CollectedFragmentIds.Count == count && !disk.LegacyCollectionQuotaExempt &&
            disk.CheckpointId == Forecourt && disk.HasActivatedCheckpoint &&
            disk.CheckpointWorldPosition == new Vector2(5920, 1080) &&
            disk.ActivatedMechanisms.Contains(Seal) && disk.OpenShortcuts.Contains(Bell) && !disk.BossDefeated && !disk.Completed;
    }

    private static bool StateAgrees(PrologueLevelController level, string path, int count)
    {
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
        var formal = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
        var counter = formal.GetNode<Label>("Health/FragmentCounter");
        string[] expected = count == 5 ? FirstFive : FirstFive.Append(Sixth).ToArray();
        return DiskAgrees(path, count) && level.Progress.CollectedFragmentIds.Count == count &&
            level.Progress.CollectedFragmentIds.SetEquals(expected) && flow.Save.CollectedFragmentIds.Count == count &&
            flow.Save.CollectedFragmentIds.OrderBy(id => id, StringComparer.Ordinal)
                .SequenceEqual(expected.OrderBy(id => id, StringComparer.Ordinal)) &&
            !level.Progress.LegacyCollectionQuotaExempt && !flow.Save.LegacyCollectionQuotaExempt &&
            level.Progress.CollectionRuleVersion == 1 && flow.Save.CollectionRuleVersion == 1 &&
            level.Progress.ActivatedMechanisms.Contains(Seal) && level.Progress.BellChainShortcutOpen &&
            flow.Save.ActivatedMechanisms.Contains(Seal) && flow.Save.OpenShortcuts.Contains(Bell) &&
            level.Progress.RestoreCheckpoint().CheckpointId == Forecourt && flow.Save.CheckpointId == Forecourt &&
            formal.CurrentModel.FragmentCount == count && formal.CurrentModel.FragmentRequired == 6 &&
            !formal.CurrentModel.CollectionQuotaExempt && counter.IsVisibleInTree() &&
            counter.Text.Contains($"{count}/6", StringComparison.Ordinal) && flow.Settings.Language == GameLanguage.ChineseSimplified;
    }

    private static void AssertState(PrologueLevelController level, string path, int count, string context) =>
        TestAssert.True(StateAgrees(level, path, count), context + ": raw durable IDs, live, Save, CN HUD and original prerequisites agree");

    private static bool Supported(PrologueLevelController level, bool shelf)
    {
        float floorY = shelf ? 960 : 1116;
        if (!level.Player.IsOnFloor() || MathF.Abs(level.Player.GlobalPosition.Y + 36 - floorY) > 2f) return false;
        foreach (float dx in new[] { -19f, 19f })
        {
            Vector2 point = level.Player.GlobalPosition + new Vector2(dx, 34);
            using var ray = PhysicsRayQueryParameters2D.Create(point, point + Vector2.Down * 4, CombatCollisionLayers.World);
            using var hit = level.Player.GetWorld2D().DirectSpaceState.IntersectRay(ray);
            if (hit.Count == 0 || hit["collider"].AsGodotObject() is not StaticBody2D body ||
                hit["normal"].AsVector2().Dot(Vector2.Up) < .7f) return false;
            bool authored = shelf ? body == level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/TempleSupplyShelf") :
                body == level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/CheckpointFloor") ||
                body == level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Collision/ArenaFloor");
            if (!authored || MathF.Abs(hit["position"].AsVector2().Y - floorY) > .1f) return false;
            var shape = body.GetNode<CollisionShape2D>("Collision");
            if (shape.Shape is not RectangleShape2D rectangle ||
                point.X < shape.GlobalPosition.X - rectangle.Size.X / 2 || point.X > shape.GlobalPosition.X + rectangle.Size.X / 2) return false;
        }
        return true;
    }

    private static void Place(PrologueLevelController level, Vector2 position)
    {
        ReleaseKeys();
        level.Player.GetNode<MechanicalClawController>("MechanicalClaw").ResetTransientState();
        level.Player.GetNode<PlayerMotor>("Motor").ResetTransientState();
        level.Player.GlobalPosition = position;
        level.Player.Velocity = Vector2.Zero;
        TestAssert.True(level.Player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing(), "controlled relocation never disables native player motor");
    }

    private static string PreserveProducer(string source, string filename)
    {
        string target = Path.Combine(IsolatedPrologueFixture.ValidateStorageRoot(), filename);
        File.Copy(source, target, overwrite: false);
        TestAssert.Equal(Hash(source), Hash(target), "retained prebattle producer is an exact non-overwritten production-save copy");
        return target;
    }

    private static bool NoTemporarySaves(IsolatedPrologueFixturePaths paths) =>
        Directory.GetFiles(paths.CaseRoot, ".save.json.*.tmp").Length == 0;
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static async Task Frames(SceneTree tree, int count)
    { for (int frame = 0; frame < count; frame++) await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); }
    private static void KeyEvent(Key key, bool pressed)
    {
        using var input = new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed };
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
    }
    private static void ReleaseKeys()
    { foreach (Key key in new[] { Key.A, Key.D, Key.Down, Key.Enter }) KeyEvent(key, false); }
    private static async Task Capture(SceneTree tree, string directory, string filename)
    {
        if (DisplayServer.GetName() == "headless") return;
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using Image image = tree.Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok, image.SavePng(Path.Combine(directory, filename)), "retain original GPU fragment-six frame in owned case");
    }
}
