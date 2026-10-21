using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Knight.Tests.Integration;

// Actual mapped E/LMB and automatic physics only. Fixture-only initial placement
// and pre-mount zero invulnerability are explicit, not a human/full-level proof.
// No post-setup health/position/velocity/claw reset or manual jet advancement.
public static class CanyonSteamDiverterPhysicalTests
{
    public static async Task RunContracts(SceneTree tree)
    {
        GD.Print("TASK17A_PHYSICAL_GROUP_BEGIN outgoing-active");
        await WithFixture(tree, "outgoing-active", 100f, OutgoingActive);
        GD.Print("TASK17A_PHYSICAL_GROUP_OK outgoing-active");
        GD.Print("TASK17A_PHYSICAL_GROUP_BEGIN incoming-overlap");
        await WithFixture(tree, "incoming-overlap", -100f, IncomingOverlap);
        GD.Print("TASK17A_PHYSICAL_GROUP_OK incoming-overlap");
        GD.Print("TASK17A_PHYSICAL_GROUP_BEGIN central-two-directions");
        await WithFixture(tree, "central-two-directions", 0f, CentralPocket);
        GD.Print("TASK17A_PHYSICAL_GROUP_OK central-two-directions");
        GD.Print("TASK17A_PHYSICAL_GROUP_BEGIN retire-remount");
        await WithFixture(tree, "retire-remount", 0f, RetireRemount);
        GD.Print("TASK17A_PHYSICAL_GROUP_OK retire-remount");
    }

    // Break caught: an actual active outgoing contact survives the real latch,
    // or a fresh mapped cast is silently replaced by a direct receiver call.
    private static async Task OutgoingActive(Fixture f)
    {
        await f.Wait(() => f.Right.Frame.HazardActive && f.RightEntries > 0 &&
            f.Health.CurrentHealth < 100 && f.FootprintContains(f.Right) &&
            f.ActualOverlap(f.Right), 200, "right active real contact and allowed pre-latch hit");
        f.Record("allowed-pre-latch-hit");
        int preLatchHealth = f.Health.CurrentHealth;
        Latch latch = await f.Cast(CanyonSteamDiverterSide.Left);
        TestAssert.True(latch.PhysicsFrame >= latch.PreviousPhysicsFrame &&
            latch.PhysicsFrame - latch.PreviousPhysicsFrame <= 1UL &&
            latch.PreviousRightActive && latch.PreviousRightContact,
            "real outgoing latch immediately follows an active actual-Hurtbox contact sample");
        TestAssert.True(latch.Health <= preLatchHealth, "outgoing fixture discloses all actual pre-latch damage");
        await f.FinishHandoff(latch);
        await f.ObserveTwoCycles(CanyonSteamDiverterSide.Left, latch.Health, mustDamage: false);
        TestAssert.True(f.FootprintContains(f.Right), "same real outgoing Hurtbox remains in retired right footprint");
        TestAssert.Equal(latch.Health, f.Health.CurrentHealth, "post-latch real outgoing contact cannot deal another hit");
    }

    // Break caught: already-overlapping incoming jet damages before its complete
    // handoff/safe/warning, or does not produce real damage on later activation.
    private static async Task IncomingOverlap(Fixture f)
    {
        TestAssert.True(f.Left.DisabledForRun && f.FootprintContains(f.Left),
            "disabled incoming footprint physically intersects this real Hurtbox before any cast");
        TestAssert.Equal(100, f.Health.CurrentHealth, "incoming disabled contact starts healthy");
        f.Record("disabled-incoming-world-query");
        Latch latch = await f.Cast(CanyonSteamDiverterSide.Left);
        TestAssert.Equal(100, latch.Health, "real incoming cast causes no premature damage");
        await f.FinishHandoff(latch);
        ulong firstActive = await f.ObserveTwoCycles(CanyonSteamDiverterSide.Left, latch.Health, mustDamage: true);
        TestAssert.True(firstActive >= latch.PhysicsFrame && firstActive - latch.PhysicsFrame >= 120UL,
            "incoming activation preserves at least complete .60 handoff plus 1.0 safe plus .4 normal warning at 60Hz");
        TestAssert.True(f.LeftEntries >= 2 && f.Health.CurrentHealth <= 98,
            "two real incoming cycles re-enter actual Hurtbox and deal damage with fixture zero invulnerability");
    }

    // Break caught: real motor/capsule leaves the supposed pocket or either
    // direction of mapped cast damages it across multiple automatic cycles.
    private static async Task CentralPocket(Fixture f)
    {
        await f.ObserveTwoCycles(CanyonSteamDiverterSide.Right, 100, mustDamage: false, pocket: true);
        Latch left = await f.Cast(CanyonSteamDiverterSide.Left);
        await f.FinishHandoff(left);
        await f.ObserveTwoCycles(CanyonSteamDiverterSide.Left, 100, mustDamage: false, pocket: true);
        Latch right = await f.Cast(CanyonSteamDiverterSide.Right);
        await f.FinishHandoff(right);
        await f.ObserveTwoCycles(CanyonSteamDiverterSide.Right, 100, mustDamage: false, pocket: true);
        TestAssert.Equal(2, f.Adapter.ApplicationCount, "two actual mapped casts reached legacy adapter exactly once each");
        TestAssert.Equal(100, f.Health.CurrentHealth, "central real motor/capsule stays healthy across both directions and complete cycles");
        TestAssert.True(f.LeftEntries == 0 && f.RightEntries == 0,
            "central safe-pocket result is not hidden by invulnerability or a fake receiver");
    }

    // Break caught: removed real controller can rearm old children via old
    // Advance/reset entry points or replacement inherits old selected/active state.
    // These direct calls test retirement only; they are NOT input evidence.
    private static async Task RetireRemount(Fixture f)
    {
        await f.Wait(() => f.Right.Frame.HazardActive, 200, "old component active before removal");
        CanyonSteamDiverter old = f.Pair;
        CanyonSteamJet oldLeft = f.Left, oldRight = f.Right;
        f.DetachJetObservers(oldLeft, oldRight);
        f.Root.RemoveChild(old);
        try
        {
            old.Advance(double.MaxValue); old.ResetForCheckpoint();
            oldLeft.ResetForCheckpoint(); oldRight.ResetForCheckpoint(); old.ResetForCheckpoint();
            AssertDisarmed(oldLeft, oldRight, "removed controller remains physically/logically disarmed after old callbacks");
            TestAssert.True(!old.TryReceiveThread(new ThreadInteraction(f.Player,
                f.Player.GlobalPosition, old.Position, new ThreadAbilitySettings())),
                "retired actual controller rejects a later direct interaction");
            TestAssert.True(!old.IsInsideTree() && !oldLeft.IsInsideTree() && !oldRight.IsInsideTree(),
                "old component and children genuinely left the scene tree");
            (f.Pair, f.Left, f.Right) = MakePair();
            f.AttachJetObservers(f.Left, f.Right);
            f.Root.AddChild(f.Pair);
            TestAssert.True(f.Pair.IsNodeReady() && f.Left.IsNodeReady() && f.Right.IsNodeReady(),
                "replacement real component completes Ready");
            TestAssert.True(f.Pair.SelectedSide == CanyonSteamDiverterSide.Right && !f.Pair.IsHandingOff &&
                f.Left.DisabledForRun && !f.Right.DisabledForRun,
                "fresh instance defaults to ordinary right safe and harmless left");
            TestAssert.Equal(0f, f.Right.ElapsedSeconds, "replacement initial clock is not inherited from old active instance");
            await f.Wait(() => f.Right.Frame.HazardActive, 200, "replacement owns an automatically advancing new clock");
            old.Advance(double.MaxValue); old.ResetForCheckpoint();
            await f.Step();
            AssertDisarmed(oldLeft, oldRight, "old callbacks cannot rearm removed children after replacement advances");
            TestAssert.True(f.Right.Frame.HazardActive && !f.Left.Frame.HazardActive,
                "old callbacks cannot reset or reroute replacement's live clock");
            TestAssert.Equal(100, f.Health.CurrentHealth, "retire/remount uses same healthy actual central Player without save/recovery");
        }
        finally { old.Free(); }
    }

    internal static async Task WithFixture(SceneTree tree, string name, float x, Func<Fixture, Task> contract,
        bool automatic = true)
    {
        var f = new Fixture(tree, name, x, automatic);
        try
        {
            await f.Step(); await f.Step();
            if (!f.Root.IsNodeReady() || !f.Pair.IsNodeReady() || !f.Left.IsNodeReady() || !f.Right.IsNodeReady() ||
                !f.Target.IsNodeReady() || !f.Adapter.IsNodeReady() || !f.InputReader.IsNodeReady() ||
                !f.Claw.IsNodeReady() || !f.Player.IsNodeReady() ||
                !f.Health.IsNodeReady() || f.Health.CurrentHealth != 100 || f.Pair.IsPhysicsProcessing() != automatic ||
                f.Left.IsPhysicsProcessing() || f.Right.IsPhysicsProcessing() ||
                tree.GetNodesInGroup("prologue_audio").Count != 0 ||
                f.Player.GetNode<CollisionShape2D>("BodyCollider").Shape is not CapsuleShape2D ||
                f.Hurtbox.GetNode<CollisionShape2D>("HurtboxCollider").Shape is not CapsuleShape2D ||
                Engine.PhysicsTicksPerSecond != 60)
                throw new InvalidOperationException("Physical fixture prerequisite: Ready/Health/actual capsule/60Hz/no-audio/sole clock failed.");
            f.Record("ready-initial-fixture-position-zero-invulnerability");
            await contract(f);
        }
        finally { await f.Dispose(); }
    }

    private static (CanyonSteamDiverter Pair, CanyonSteamJet Left, CanyonSteamJet Right) MakePair()
    {
        var pair = new CanyonSteamDiverter { Name = "Diverter" };
        CanyonSteamJet left = Jet("LeftJet", -100f), right = Jet("RightJet", 100f);
        pair.AddChild(left); pair.AddChild(right);
        return (pair, left, right);
    }

    private static CanyonSteamJet Jet(string name, float x)
    {
        var jet = new CanyonSteamJet { Name = name, Position = new Vector2(x, -15),
            Monitoring = false, SafeSeconds = 1f, TelegraphSeconds = .4f, ActiveSeconds = .6f };
        jet.AddChild(new CollisionShape2D { Name = "Collision", Disabled = true,
            Shape = new RectangleShape2D { Size = new Vector2(56, 150) } });
        jet.AddChild(new Node2D { Name = "Telegraph", Visible = false });
        jet.AddChild(new Node2D { Name = "ActiveVisual", Visible = false });
        return jet;
    }

    private static void AssertDisarmed(CanyonSteamJet left, CanyonSteamJet right, string reason)
    {
        TestAssert.True(left.DisabledForRun && right.DisabledForRun && !left.Frame.HazardActive &&
            !right.Frame.HazardActive && left.Collision.Disabled && right.Collision.Disabled &&
            !left.Monitoring && !right.Monitoring, reason);
    }

    internal readonly record struct Latch(ulong CastId, ulong TargetId, ulong PhysicsFrame, int Health,
        CanyonSteamDiverterSide Side, bool Handoff, bool BothDisarmed,
        ulong PreviousPhysicsFrame, bool PreviousRightActive, bool PreviousRightContact);
    private readonly record struct Release(ulong CastId, ulong TargetId, ulong PhysicsFrame);
    private readonly record struct PostPhysicsSample(ulong PhysicsFrame, bool RightActive, bool RightContact);

    // Small fixture-local observer/stepper. No replacement player/claw model.
    internal sealed class Fixture
    {
        public readonly SceneTree Tree;
        public readonly string Name;
        public readonly Node2D Root;
        public CanyonSteamDiverter Pair;
        public CanyonSteamJet Left, Right;
        public readonly CharacterBody2D Player;
        public readonly HealthComponent Health;
        public readonly Hurtbox Hurtbox;
        public readonly PlayerInputReader InputReader;
        public readonly PlayerActionModeController Modes;
        public readonly MechanicalClawController Claw;
        public readonly ThreadTarget2D Target;
        public readonly LegacyThreadInteractionAdapter Adapter;
        public int LeftEntries, RightEntries, FailedCasts;
        public MechanicalClawFrame LastFailedCast;
        internal Action SampleObserved;
        private readonly List<Latch> _latches = new();
        private readonly List<Release> _releases = new();
        private int _steps, _grappleEdges, _primaryEdges, _releaseEdges;
        internal int GrapplePressSamples => _grappleEdges;
        internal int PrimaryPressSamples => _primaryEdges;
        private ulong _capturedSequence = ulong.MaxValue;
        private string _lastState = "";
        private bool _heldE, _heldLeft;
        private Vector2 _pointer;
        private PostPhysicsSample _lastPostPhysics;
        private readonly ulong _controlId;

        public Fixture(SceneTree tree, string name, float x, bool automatic = true)
        {
            Tree = tree; Name = name;
            Root = new Node2D { Name = "Task17AMappedPhysicalZeroInvulnerabilityFixture", Position = new Vector2(400, 300) };
            (Pair, Left, Right) = MakePair();
            if (!automatic) Pair.Ready += () => Pair.SetPhysicsProcess(false);
            Root.AddChild(Pair);
            var floor = new StaticBody2D { Name = "Floor", Position = new Vector2(0, 60),
                CollisionLayer = CombatCollisionLayers.World, CollisionMask = CombatCollisionLayers.None };
            floor.AddChild(new CollisionShape2D { Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(1000, 20) } });
            Root.AddChild(floor);
            Target = new ThreadTarget2D { Name = "Control", Position = new Vector2(0, -45),
                Kind = ThreadTargetKind.Mechanism, Material = GrappleSurfaceMaterial.DedicatedAnchor,
                AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius, PullReceiverPath = new("../Diverter") };
            _controlId = Target.GetInstanceId();
            var area = new Area2D { Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget,
                CollisionMask = CombatCollisionLayers.None, Monitoring = true, Monitorable = true };
            area.AddChild(new CollisionShape2D { Name = "Collision", Shape = new CircleShape2D { Radius = 22f } });
            Target.AddChild(area);
            Adapter = new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter", TargetPath = new("..") };
            Target.AddChild(Adapter); Root.AddChild(Target);
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Name = "Player"; Player.Position = new Vector2(x, 14);
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Health = Player.GetNode<HealthComponent>("Health");
            Health.MaxHealth = 100; Health.InvulnerabilitySeconds = 0f; // BEFORE mount only.
            Hurtbox = Player.GetNode<Hurtbox>("Hurtbox");
            InputReader = Player.GetNode<PlayerInputReader>("Input");
            Modes = Player.GetNode<PlayerActionModeController>("ActionModes");
            Claw = Player.GetNode<MechanicalClawController>("MechanicalClaw");
            Claw.ClawFrameChanged += OnClaw;
            Health.Damaged += OnDamage;
            AttachJetObservers(Left, Right);
            Root.AddChild(Player);
            ReleaseAll();
            Tree.Root.AddChild(Root);
        }

        public async Task Step(Action events = null)
        {
            if (++_steps > 1600) throw new InvalidOperationException("Physical fixture frame cap exceeded.");
            ulong previousSequence = InputReader.CurrentFrameSequence;
            await Tree.ToSignal(Tree, SceneTree.SignalName.PhysicsFrame);
            events?.Invoke();
            await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
            if (InputReader.CurrentFrameSequence <= previousSequence || !Player.IsInsideTree() ||
                !float.IsFinite(Player.Position.X) || !float.IsFinite(Player.Position.Y))
                throw new InvalidOperationException("Physical fixture sampling/live-player prerequisite failed.");
            CaptureInput();
            _lastPostPhysics = new(Engine.GetPhysicsFrames(), Right.Frame.HazardActive,
                FootprintContains(Right) && ActualOverlap(Right));
            string state = $"{Pair.IsHandingOff}/{Pair.SelectedSide}/{Phase(Left)}/{Phase(Right)}/{Health.CurrentHealth}/{Claw.CurrentFrame.State}/{InputReader.CurrentFrame.PrimaryHeld}";
            if (state != _lastState) { Record("sample-boundary"); _lastState = state; }
            SampleObserved?.Invoke();
        }

        public async Task Wait(Func<bool> predicate, int cap, string reason)
        {
            for (int index = 0; index < cap; index++)
            {
                if (predicate()) return;
                await Step();
            }
            TestAssert.True(predicate(), reason + $" (bounded {cap} automatic steps)");
        }

        public async Task<Latch> Cast(CanyonSteamDiverterSide wanted)
        {
            await Wait(() => Claw.CurrentFrame.State is MechanicalClawState.Idle or MechanicalClawState.Released,
                30, "fresh cast is not a busy/latched button assumption");
            int grappleBefore = _grappleEdges, pressBefore = _primaryEdges, releaseBefore = _releaseEdges;
            int latchBefore = _latches.Count, adapterBefore = Adapter.ApplicationCount;
            await Step(() => { Aim(); SendE(true); });
            await Wait(() => _grappleEdges > grappleBefore && Modes.CurrentMode == PlayerActionMode.Grapple,
                8, "actual E event sampled through InputMap into grapple mode");
            await Step(() => SendE(false));
            await Step();
            await Step(() => { Aim(); SendLeft(true); });
            await Wait(() => _latches.Count > latchBefore, 90, "fresh actual LMB cast reaches legacy mechanism latch");
            await Step(() => SendLeft(false));
            await Wait(() => _releaseEdges > releaseBefore && !Input.IsActionPressed("primary_action"),
                8, "actual LMB release crosses a new input sample");
            TestAssert.True(_primaryEdges > pressBefore, "real primary press was observed by PlayerInputReader");
            TestAssert.Equal(latchBefore + 1, _latches.Count, "one press publishes exactly one accepted real latch");
            TestAssert.Equal(adapterBefore + 1, Adapter.ApplicationCount, "one real adapter application per fresh press");
            Latch latch = _latches[latchBefore];
            TestAssert.True(latch.TargetId == Target.GetInstanceId() && latch.Side == wanted &&
                latch.Handoff && latch.BothDisarmed, "passive real latch records target and immediate selected-side dual disarm");
            TestAssert.True(_releases.Exists(release => release.CastId == latch.CastId &&
                release.PhysicsFrame == latch.PhysicsFrame),
                "passive observer catches Latched and Released in the same actual claw callback frame");
            return latch;
        }

        public async Task FinishHandoff(Latch latch)
        {
            for (int index = 0; index < 90 && Pair.IsHandingOff; index++)
            {
                AssertDisarmed(Left, Right, "every actual handoff observation keeps both jets disarmed");
                TestAssert.Equal(latch.Health, Health.CurrentHealth, "actual handoff contact is harmless after real latch baseline");
                await Step();
            }
            TestAssert.True(!Pair.IsHandingOff && Pair.SelectedSide == latch.Side, "bounded full handoff completes into requested side");
            TestAssert.True(Engine.GetPhysicsFrames() - latch.PhysicsFrame >= 36UL,
                "same latch frame cannot shorten the full .60 second automatic handoff");
            TestAssert.Equal(latch.Health, Health.CurrentHealth, "full automatic handoff completes without a late hit");
            Record("handoff-complete");
        }

        public async Task<ulong> ObserveTwoCycles(CanyonSteamDiverterSide side, int baseline, bool mustDamage, bool pocket = false)
        {
            CanyonSteamJet selected = side == CanyonSteamDiverterSide.Left ? Left : Right;
            CanyonSteamJet dormant = side == CanyonSteamDiverterSide.Left ? Right : Left;
            int entriesBefore = side == CanyonSteamDiverterSide.Left ? LeftEntries : RightEntries;
            int cycles = 0;
            int cycleStartHealth = baseline;
            int previousHealth = baseline;
            bool sawEarly = false, sawWarning = false, sawActive = false, sawActualContact = false;
            ulong firstActive = 0, warningStart = 0;
            for (int index = 0; index < 420 && cycles < 2; index++)
            {
                await Step();
                TestAssert.True(Pair.SelectedSide == side && !Pair.IsHandingOff &&
                    dormant.DisabledForRun && !dormant.Frame.HazardActive && dormant.Collision.Disabled &&
                    !dormant.Monitoring && !Left.IsPhysicsProcessing() && !Right.IsPhysicsProcessing(),
                    "every real cycle retains sole controller clock and harmless nonselected physical state");
                TestAssert.True(selected.Collision.Disabled == !selected.Frame.HazardActive &&
                    selected.Monitoring == selected.Frame.HazardActive, "real selected shape/monitor matches phase");
                if (pocket)
                    TestAssert.True(!FootprintContains(Left) && !FootprintContains(Right),
                        "actual upright capsule/Hurtbox remains outside both world-query footprints");
                if (!mustDamage)
                    TestAssert.Equal(baseline, Health.CurrentHealth, "real harmless intervals/pocket cannot hide damage");
                else if (!selected.Frame.HazardActive)
                    TestAssert.Equal(previousHealth, Health.CurrentHealth,
                        "every incoming cycle's safe/early/normal warning interval preserves its latest real Health baseline");
                previousHealth = Health.CurrentHealth;
                if (selected.Frame.Telegraphing && selected.ElapsedSeconds < 1f) sawEarly = true;
                if (selected.Frame.Telegraphing && selected.ElapsedSeconds >= 1f)
                {
                    if (!sawWarning) warningStart = Engine.GetPhysicsFrames();
                    sawWarning = true;
                }
                if (selected.Frame.HazardActive)
                {
                    if (!sawActive)
                        TestAssert.True(sawEarly && sawWarning && Engine.GetPhysicsFrames() - warningStart >= 24UL,
                            "automatic real cycle presents early warning and full .4 ordinary harmless warning");
                    sawActive = true;
                    if (firstActive == 0) firstActive = Engine.GetPhysicsFrames();
                    if (mustDamage && !sawActualContact && ActualOverlap(selected) && FootprintContains(selected))
                    {
                        sawActualContact = true;
                        Record("real-active-overlap-and-world-query");
                    }
                }
                if (sawActive && !selected.Frame.HazardActive && !selected.Frame.Telegraphing)
                {
                    cycles++; Record("complete-cycle-" + cycles);
                    if (mustDamage)
                        TestAssert.True(sawActualContact && FootprintContains(selected) &&
                            (side == CanyonSteamDiverterSide.Left ? LeftEntries : RightEntries) >= entriesBefore + cycles &&
                            Health.CurrentHealth < cycleStartHealth,
                            "each completed incoming cycle has real Hurtbox contact/AreaEntered and new Health damage");
                    cycleStartHealth = Health.CurrentHealth;
                    sawEarly = false; sawWarning = false; sawActive = false; sawActualContact = false; warningStart = 0;
                }
            }
            TestAssert.Equal(2, cycles, "two complete automatic cycles observed within fixed cap");
            return firstActive;
        }

        // Independent actual-world query uses the disabled jet's borrowed shape
        // without enabling it. Query/Array/Dictionary wrappers are disposed;
        // the scene-owned shape is never disposed here.
        public bool FootprintContains(CanyonSteamJet jet)
        {
            using var query = new PhysicsShapeQueryParameters2D
            {
                Shape = jet.Collision.Shape, Transform = jet.Collision.GlobalTransform,
                CollisionMask = CombatCollisionLayers.Player, CollideWithAreas = true, CollideWithBodies = false
            };
            var hits = Root.GetWorld2D().DirectSpaceState.IntersectShape(query, 32);
            using var owner = (Godot.Collections.Array)hits;
            foreach (Godot.Collections.Dictionary hit in hits)
                using (hit)
                    if (hit.ContainsKey("collider") && hit["collider"].AsGodotObject() == Hurtbox) return true;
            return false;
        }

        public bool ActualOverlap(CanyonSteamJet jet)
        {
            // Disabled contact is established by the independent world query,
            // never by asking a nonmonitoring Area for an overlap collection.
            if (!jet.Monitoring || !jet.Frame.HazardActive) return false;
            var areas = jet.GetOverlappingAreas();
            using var owner = (Godot.Collections.Array)areas;
            foreach (Area2D area in areas) if (area == Hurtbox) return true;
            return false;
        }

        private void OnClaw(MechanicalClawFrame frame, GrappleSurface2D target)
        {
            CaptureInput();
            if (frame.State == MechanicalClawState.Failed) { FailedCasts++; LastFailedCast = frame; }
            if (frame.State == MechanicalClawState.Latched)
            {
                bool disarmed = Left.DisabledForRun && Right.DisabledForRun && !Left.Frame.HazardActive &&
                    !Right.Frame.HazardActive && Left.Collision.Disabled && Right.Collision.Disabled &&
                    !Left.Monitoring && !Right.Monitoring;
                _latches.Add(new(frame.CastId, frame.LockedTargetRuntimeId, Engine.GetPhysicsFrames(),
                    Health.CurrentHealth, Pair.SelectedSide, Pair.IsHandingOff, disarmed,
                    _lastPostPhysics.PhysicsFrame, _lastPostPhysics.RightActive, _lastPostPhysics.RightContact));
            }
            if (frame.State == MechanicalClawState.Released)
                _releases.Add(new(frame.CastId, frame.LockedTargetRuntimeId, Engine.GetPhysicsFrames()));
            if (frame.State is MechanicalClawState.Latched or MechanicalClawState.Released or MechanicalClawState.Failed)
                Record("claw-" + frame.State, frame.CastId, frame.LockedTargetRuntimeId);
        }

        private void CaptureInput()
        {
            if (_capturedSequence == InputReader.CurrentFrameSequence) return;
            _capturedSequence = InputReader.CurrentFrameSequence;
            PlayerInputFrame input = InputReader.CurrentFrame;
            if (input.GrappleModePressed) _grappleEdges++;
            if (input.PrimaryPressed) _primaryEdges++;
            if (input.PrimaryReleased) _releaseEdges++;
            if (input.GrappleModePressed || input.PrimaryPressed || input.PrimaryReleased) Record("actual-input-sample");
        }

        private void OnDamage(int remaining, Vector2 impulse) => Record("real-health-damaged");
        private void OnLeftEntered(Area2D area) { if (area == Hurtbox) { LeftEntries++; Record("real-left-area-entered"); } }
        private void OnRightEntered(Area2D area) { if (area == Hurtbox) { RightEntries++; Record("real-right-area-entered"); } }
        public void AttachJetObservers(CanyonSteamJet left, CanyonSteamJet right)
        { left.AreaEntered += OnLeftEntered; right.AreaEntered += OnRightEntered; }
        public void DetachJetObservers(CanyonSteamJet left, CanyonSteamJet right)
        {
            if (GodotObject.IsInstanceValid(left)) left.AreaEntered -= OnLeftEntered;
            if (GodotObject.IsInstanceValid(right)) right.AreaEntered -= OnRightEntered;
        }

        public void Record(string kind, ulong castId = 0, ulong targetId = 0) =>
            GD.Print("TASK17A_PHYSICAL " + JsonSerializer.Serialize(new {
                scenario = Name, kind, step = _steps, physics = Engine.GetPhysicsFrames(),
                inputSequence = InputReader.CurrentFrameSequence,
                heldE = _heldE, heldLMB = _heldLeft, hurtboxId = Hurtbox.GetInstanceId(), playerId = Player.GetInstanceId(),
                controlId = _controlId, controlValid = GodotObject.IsInstanceValid(Target),
                priorPostPhysics = _lastPostPhysics.PhysicsFrame,
                priorRightActive = _lastPostPhysics.RightActive, priorRightContact = _lastPostPhysics.RightContact,
                hp = Health.CurrentHealth, position = Player.GlobalPosition.ToString(), velocity = Player.Velocity.ToString(),
                mode = Modes.CurrentMode.ToString(), sampled = InputReader.CurrentFrame.ToString(),
                handoff = Pair.IsHandingOff, selected = Pair.SelectedSide.ToString(),
                left = Phase(Left), leftElapsed = Left.ElapsedSeconds, leftMonitor = Left.Monitoring,
                leftShapeDisabled = Left.Collision.Disabled, right = Phase(Right), rightElapsed = Right.ElapsedSeconds,
                rightMonitor = Right.Monitoring, rightShapeDisabled = Right.Collision.Disabled,
                leftEntries = LeftEntries, rightEntries = RightEntries, castId, targetId,
                currentCast = Claw.CurrentFrame.CastId, claw = Claw.CurrentFrame.State.ToString(), failure = Claw.CurrentFrame.Failure.ToString()
            }));

        private static string Phase(CanyonSteamJet jet) => jet.DisabledForRun ? "disabled" :
            jet.Frame.HazardActive ? "active" : !jet.Frame.Telegraphing ? "safe" :
            jet.ElapsedSeconds < 1f ? "early-warning" : "ordinary-warning";

        internal void Aim()
        {
            _pointer = Player.GetViewport().GetFinalTransform() *
                (Player.GetGlobalTransformWithCanvas() * Player.ToLocal(Target.WorldAnchor));
            using var input = new InputEventMouseMotion { Position = _pointer, GlobalPosition = _pointer };
            Player.GetViewport().PushInput(input, true);
            Input.ParseInputEvent(input); Input.FlushBufferedEvents();
            Record("actual-mouse-motion");
        }

        internal void SendE(bool pressed)
        {
            _heldE = pressed;
            using var input = new InputEventKey { Keycode = Key.E, PhysicalKeycode = Key.E, Pressed = pressed, Echo = false };
            Input.ParseInputEvent(input); Input.FlushBufferedEvents();
            Record("actual-key-E-" + pressed);
        }

        internal void SendLeft(bool pressed)
        {
            _heldLeft = pressed;
            using var input = new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = pressed,
                Position = _pointer, GlobalPosition = _pointer };
            Input.ParseInputEvent(input); Input.FlushBufferedEvents();
            Record("actual-LMB-" + pressed);
        }

        private void ReleaseAll()
        {
            foreach (Key key in new[] { Key.E, Key.Q, Key.A, Key.D, Key.Space })
            {
                using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = false, Echo = false };
                Input.ParseInputEvent(input);
            }
            foreach (MouseButton button in new[] { MouseButton.Left, MouseButton.Right })
            {
                using var input = new InputEventMouseButton { ButtonIndex = button, Pressed = false };
                Input.ParseInputEvent(input);
            }
            foreach (string action in new[] { "move_left", "move_right", "move_up", "move_down", "jump", "dash",
                "select_attack_mode", "select_grapple_mode", "primary_action", "shield" })
                if (Input.IsActionPressed(action)) Input.ActionRelease(action);
            Input.FlushBufferedEvents();
            _heldE = false; _heldLeft = false;
        }

        public async Task Dispose()
        {
            Tree.Paused = false;
            // Always release, including an exception after a physical event press.
            ReleaseAll();
            Claw.ClawFrameChanged -= OnClaw; Health.Damaged -= OnDamage;
            DetachJetObservers(Left, Right);
            Root.QueueFree();
            for (int index = 0; index < 3; index++)
                await Tree.ToSignal(Tree, SceneTree.SignalName.PhysicsFrame);
        }
    }
}
