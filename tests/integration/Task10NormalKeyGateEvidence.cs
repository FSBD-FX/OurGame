using System;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Integration;

// Bounded to the named LostTemple doors and the separate three-face admission hull.
// These passive samples do not query physics space or prove every wall/corridor.
internal sealed class Task10NormalKeyGateEvidence : IDisposable
{
    private const string BellShortcut = "canyon.v2.temple-bell-gate";
    private readonly PrologueLevelController _level;
    private readonly PrologueGameFlowController _flow;
    private readonly SeamlessPrologueWorld _world;
    private readonly CharacterBody2D _player;
    private readonly CollisionShape2D _capsule;
    private readonly CapsuleShape2D _capsuleShape;
    private readonly PlayerInputReader _input;
    private readonly ThrusterPostMotorObserver _before, _after;
    private readonly string _variant;
    private PrologueSector _temple;
    private BellChainMechanism _chain;
    private LegacyThreadInteractionAdapter _adapter;
    private BossArenaController _arena;
    private HealthComponent _bossHealth;
    private BoundGate[] _gates;
    private AdmissionHull _admission;
    private ulong _bellSignal, _defeatedSignal, _preTick, _preInput, _lastTick, _lastInput;
    private Rect2 _preBody;
    private int _samples, _streamGaps;
    private bool _sealed;
    private string _failure;

    internal Task10NormalKeyGateEvidence(PrologueLevelController level, PrologueGameFlowController flow,
        SeamlessPrologueWorld world, CharacterBody2D player, string variant)
    {
        _level = level; _flow = flow; _world = world; _player = player; _variant = variant;
        _capsule = player.GetNode<CollisionShape2D>("BodyCollider");
        _capsuleShape = _capsule.Shape as CapsuleShape2D ?? throw new InvalidOperationException("Missing saved Player capsule.");
        _input = player.GetNode<PlayerInputReader>("Input");
        TestAssert.True(_input.ProcessPhysicsPriority == -300 && player.GetNode<PlayerMotor>("Motor").ProcessPhysicsPriority == 0,
            "named gate ledger brackets actual sampled input and saved Motor");
        Discriminants();
        _before = new ThrusterPostMotorObserver { Name = "KeyGateBeforeMotor", ProcessMode = Node.ProcessModeEnum.Always,
            ProcessPhysicsPriority = -1, Observe = BeforeMotor };
        _after = new ThrusterPostMotorObserver { Name = "KeyGateAfterMotor", ProcessMode = Node.ProcessModeEnum.Always,
            ProcessPhysicsPriority = 2, Observe = AfterMotor };
        level.AddChild(_before); level.AddChild(_after);
    }

    private void BindTemple(PrologueSector temple)
    {
        _temple = temple;
        _chain = temple.GetNode<BellChainMechanism>("Gameplay/Mechanisms/BellChain");
        _adapter = _chain.GetNode<LegacyThreadInteractionAdapter>("ThreadTarget/LegacyThreadInteractionAdapter");
        _arena = temple.GetNode<BossArenaController>("Gameplay/BossArenaController");
        _bossHealth = _arena.CurrentBoss?.GetNodeOrNull<HealthComponent>("Health");
        TestAssert.True(_chain.ShortcutId.ToString() == BellShortcut && _adapter.ApplicationCount == 0 &&
            _arena.RequiresFragmentAdmission && Live(_bossHealth) && _bossHealth.CurrentHealth > 0,
            "binds the original unapplied BellChain and actual admission-gated living Boss");
        _gates =
        [
            new BoundGate("bell", _chain.GetNode<CollisionShape2D>(_chain.DoorCollisionPath), _player),
            new BoundGate("boss_entry", _arena.GetNode<CollisionShape2D>(_arena.EntryDoorCollisionPath), _player),
            new BoundGate("boss_exit", _arena.GetNode<CollisionShape2D>(_arena.ExitDoorCollisionPath), _player)
        ];
        try { _admission = new AdmissionHull(this, temple); }
        catch (Exception error) { throw new InvalidOperationException("admission hull binding failed", error); }
        _chain.Opened += OnBellOpened;
        _arena.EncounterDefeated += OnBossDefeated;
    }

    private void OnBellOpened(StringName id)
    {
        if (_bellSignal != 0 || id.ToString() != BellShortcut) { Fail("foreign or duplicate BellChain Opened"); return; }
        _bellSignal = Engine.GetPhysicsFrames();
    }

    private void OnBossDefeated()
    {
        if (_defeatedSignal != 0) { Fail("duplicate arena defeat"); return; }
        _defeatedSignal = Engine.GetPhysicsFrames();
    }

    private void BeforeMotor()
    {
        if (_sealed || _failure != null) return;
        try
        {
            if (_preTick != 0) { Fail($"lost post sample at tick={_preTick}"); return; }
            if (_flow.State != PrologueGameFlowState.Playing) return;
            bool mounted = _world.TryGetMountedSector(PrologueSectorId.LostTemple, out var temple);
            if (!mounted && _temple == null) return;
            Rect2 capsule = CapsuleBounds();
            if (_temple != null && (!mounted || temple != _temple))
            {
                if (!DropNearSideEpoch(capsule)) return;
                if (!mounted) return;
            }
            if (_temple == null) BindTemple(temple);
            if (_level.Player != _player) { Fail("the original Player was replaced"); return; }
            ulong tick = Engine.GetPhysicsFrames(), input = _input.CurrentFrameSequence;
            if (input == 0 || (_lastTick != 0 && (tick != _lastTick + 1 || input != _lastInput + 1)))
            { Fail($"mounted sample gap tick={tick} input={input} last={_lastTick}/{_lastInput}"); return; }
            _preTick = tick; _preInput = input; _preBody = capsule;
            var progress = _level.Progress;
            BossAdmissionResult admission = BossAdmissionModel.Evaluate(progress);
            _gates[0].ReadPre(_bellSignal != 0 && _bellSignal <= tick && _chain.IsOpen && _adapter.ApplicationCount == 1 &&
                progress.BellChainShortcutOpen && _flow.Save.OpenShortcuts.Contains(BellShortcut));
            _gates[1].ReadPre(!_arena.IsEncounterActive && !progress.LegacyCollectionQuotaExempt &&
                admission.Allowed);
            _gates[2].ReadPre(_defeatedSignal != 0 && _defeatedSignal <= tick && Live(_bossHealth) &&
                _bossHealth.CurrentHealth == 0 && !_arena.IsEncounterActive && progress.BossDefeated && _flow.Save.BossDefeated);
            _admission.BeforeMotor(tick, admission);
        }
        catch (Exception error) { Fail($"pre-Motor binding/observation: {error}"); }
    }

    private void AfterMotor()
    {
        if (_sealed || _failure != null || _preTick == 0) return;
        try
        {
            ulong tick = Engine.GetPhysicsFrames();
            Rect2 capsule = CapsuleBounds();
            if (_preTick != tick || _preInput != _input.CurrentFrameSequence || _level.Player != _player)
            { Fail($"mispaired samples tick={_preTick}->{tick} input={_preInput}->{_input.CurrentFrameSequence}"); return; }
            _preTick = 0;
            if (!_world.TryGetMountedSector(PrologueSectorId.LostTemple, out var temple) || temple != _temple)
            {
                if (AllNear(_preBody)) DropNearSideEpoch(capsule);
                else Fail("mount changed during a gate step from outside the near side");
                return;
            }
            _admission.AfterMotor(tick, _preBody, capsule);
            foreach (BoundGate gate in _gates)
            {
                string failure = gate.Ledger.Step(tick, gate.Bounds, _preBody, capsule, gate.NativeOpen, gate.Authorized);
                if (failure != null)
                {
                    Fail($"gate={gate.Name} body_id={gate.BodyId} shape_id={gate.ShapeId} tick={tick} " +
                        $"open={gate.NativeOpen} authority={gate.Authorized} before={_preBody} after={capsule} bounds={gate.Bounds}: {failure}");
                    return;
                }
            }
            _lastTick = tick; _lastInput = _preInput; _samples++;
        }
        catch (Exception error) { Fail($"post-Motor gate observation: {error}"); }
    }

    private Rect2 CapsuleBounds()
    {
        if (!Live(_capsule) || _capsule.Disabled || _capsule.Shape != _capsuleShape || !Upright(_capsule.GlobalTransform))
            throw new InvalidOperationException("Original upright Player capsule is unavailable.");
        var half = new Vector2(_capsuleShape.Radius, _capsuleShape.Height / 2f);
        return new Rect2(_capsule.GlobalPosition - half, 2f * half);
    }

    private bool AllNear(Rect2 capsule)
    {
        foreach (BoundGate gate in _gates)
            if (!gate.Ledger.CanGap(gate.Bounds, capsule)) return false;
        return true;
    }

    private bool DropNearSideEpoch(Rect2 capsule)
    {
        if (!AllNear(capsule) || _bellSignal != 0 || _defeatedSignal != 0)
        { Fail("stream mount gap cannot transfer gate/authority history or hide a crossing"); return false; }
        if (!_admission.CanGap(capsule))
        { Fail($"admission hull: stream mount gap would lose the witnessed epoch {_admission.Ticks}"); return false; }
        UnbindTemple();
        _streamGaps++;
        _lastTick = _lastInput = 0;
        return true;
    }

    private void UnbindTemple()
    {
        _admission?.Dispose(); _admission = null;
        if (GodotObject.IsInstanceValid(_chain)) _chain.Opened -= OnBellOpened;
        if (GodotObject.IsInstanceValid(_arena)) _arena.EncounterDefeated -= OnBossDefeated;
        _temple = null; _chain = null; _arena = null; _gates = null;
    }

    internal void AssertComplete(ulong completionTick)
    {
        _sealed = true;
        if (_failure == null && (_gates == null || _preTick != 0 || _samples == 0 || _lastTick > completionTick ||
            completionTick - _lastTick > 1 || _flow.State != PrologueGameFlowState.Completed))
            Fail($"incomplete gate coverage samples={_samples} pending={_preTick} last={_lastTick} completion={completionTick}");
        if (_failure == null)
            foreach (BoundGate gate in _gates)
                if (!gate.Ledger.Complete) { Fail($"no complete native closed/authorized/open/crossing sequence for {gate.Name}"); break; }
        if (_failure == null && !_admission.Complete)
            Fail($"admission hull missing complete native sequence: {_admission.Ticks}");
        TestAssert.True(_failure == null, $"normal route named gate evidence: {_failure}");
        foreach (BoundGate gate in _gates)
            GD.Print($"NORMAL_ROUTE_KEY_GATE variant={_variant} gate={gate.Name} body={gate.BodyId} shape={gate.ShapeId} " +
                $"closed_tick={gate.Ledger.ClosedTick} authorized_open_tick={gate.Ledger.OpenTick} " +
                $"first_cross_tick={gate.Ledger.CrossTick} crossings={gate.Ledger.Crossings} bounds={gate.Bounds}");
        GD.Print($"NORMAL_ROUTE_KEY_GATES_OK variant={_variant} gates=3 bell={_gates[0].Ledger.Crossings} " +
            $"boss_entry={_gates[1].Ledger.Crossings} boss_exit={_gates[2].Ledger.Crossings} samples={_samples} " +
            $"mount_gaps={_streamGaps} player={_player.GetInstanceId()} bell_signal={_bellSignal} " +
            $"boss_defeat_signal={_defeatedSignal} completion_tick={completionTick}");
        _admission.Print(completionTick);
    }

    private void Fail(string reason) => _failure ??= reason;

    internal void ReportFailure()
    {
        if (_failure == null) return;
        GD.Print($"NORMAL_ROUTE_KEY_GATES_RED variant={_variant} {_failure}");
        if (_failure.Contains("admission hull", StringComparison.Ordinal))
            GD.Print($"NORMAL_ROUTE_ADMISSION_HULL_RED variant={_variant} {_admission?.Ticks} reason={_failure}");
    }

    public void Dispose()
    {
        _sealed = true; _before.Observe = null; _after.Observe = null;
        UnbindTemple();
    }

    private static bool Live(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree() && !node.IsQueuedForDeletion();
    private static bool Upright(Transform2D transform) => transform.X.IsEqualApprox(Vector2.Right) &&
        transform.Y.IsEqualApprox(Vector2.Down) && transform.Origin.IsFinite();

    private sealed class AdmissionHull : IDisposable
    {
        private const string CourtyardId = "canyon.v2.fragment.courtyard";
        private const int AllFaces = 0b111;
        private readonly Task10NormalKeyGateEvidence _owner;
        private readonly PrologueSector _temple;
        private readonly QuestFragment _courtyard;
        private readonly BoundGate[] _faces;
        private readonly GateLedger _left = new();
        private int _preMask, _observedOpenMask;
        private bool _preAuthorized;
        private ulong _five, _courtyardBodyEntered, _collected, _sixNoBell, _authorized, _allDisabled;

        internal AdmissionHull(Task10NormalKeyGateEvidence owner, PrologueSector temple)
        {
            _owner = owner; _temple = temple;
            var body = temple.GetNode<StaticBody2D>("Gameplay/Doors/AdmissionBarrier");
            var left = body.GetNode<CollisionShape2D>("Collision");
            Check(left == owner._arena.GetNode<CollisionShape2D>(owner._arena.AdmissionBarrierCollisionPath),
                "the arena did not bind the saved left admission shape");
            _faces =
            [
                new BoundGate("left", left, owner._player),
                new BoundGate("top", body.GetNode<CollisionShape2D>("TopCollision"), owner._player),
                new BoundGate("right", body.GetNode<CollisionShape2D>("RightCollision"), owner._player)
            ];
            Check(_faces[0].Bounds == new Rect2(6142, 720, 36, 436) &&
                _faces[1].Bounds == new Rect2(6142, 720, 1556, 40) &&
                _faces[2].Bounds == new Rect2(7662, 720, 36, 436), "wrong saved hull bounds");
            for (int i = 0; i < _faces.Length; i++)
            {
                BoundGate face = _faces[i];
                Check(face.BodyId == body.GetInstanceId() && face.BodyRid == body.GetRid().Id &&
                    face.BodyId != owner._gates[1].BodyId, $"{face.Name} is not on the separate admission body");
                for (int j = 0; j < i; j++)
                    Check(face.ShapeId != _faces[j].ShapeId && face.ResourceId != _faces[j].ResourceId &&
                        face.ResourceRid != _faces[j].ResourceRid && face.OwnerId != _faces[j].OwnerId &&
                        face.ShapeIndex != _faces[j].ShapeIndex, $"{face.Name} aliases another native admission shape");
            }
            _courtyard = temple.GetNode<QuestFragment>("Gameplay/QuestFragments/courtyard");
            Check(Live(_courtyard) && _courtyard.StableId == CourtyardId && !_courtyard.IsCollected &&
                _courtyard.FeedbackCount == 0, "missing the original uncollected courtyard fragment");
            _courtyard.BodyEntered += OnCourtyardBodyEntered;
            _courtyard.Collected += OnCourtyardCollected;
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException($"admission hull: {message}");
        }

        private bool Authorized(ulong tick, BossAdmissionResult admission) => admission.Allowed &&
            !_owner._level.Progress.LegacyCollectionQuotaExempt && _owner._bellSignal != 0 &&
            _owner._bellSignal <= tick && _owner._chain.IsOpen && _owner._adapter.ApplicationCount == 1 &&
            _owner._flow.Save.OpenShortcuts.Contains(BellShortcut);

        private bool DurableClosedParity(PrologueProgress live, int count, bool requireSavedSeal)
        {
            var save = _owner._flow.Save;
            return !live.LegacyCollectionQuotaExempt && !save.LegacyCollectionQuotaExempt &&
                !live.BellChainShortcutOpen && !save.OpenShortcuts.Contains(BellShortcut) &&
                live.CollectedFragmentIds.Count == count && save.CollectedFragmentIds.Count == count &&
                live.CollectedFragmentIds.SetEquals(save.CollectedFragmentIds) &&
                (!requireSavedSeal || save.ActivatedMechanisms.Contains(CanyonWorldLayout.CourtyardSealMechanismId));
        }

        private void OnCourtyardBodyEntered(Node2D body)
        {
            if (_owner._sealed || _owner._failure != null || body != _owner._player ||
                _owner._level.Player != _owner._player) return;
            ulong tick = Engine.GetPhysicsFrames();
            if (_collected == 0 || _collected == tick) _courtyardBodyEntered = tick;
        }

        private void OnCourtyardCollected(string stableId)
        {
            if (_owner._sealed || _owner._failure != null) return;
            ulong tick = Engine.GetPhysicsFrames();
            var live = _owner._level.Progress;
            if (stableId != CourtyardId || !Live(_courtyard) || _collected != 0 || _five == 0 || _five > tick ||
                _owner._flow.State != PrologueGameFlowState.Playing || _owner._level.CurrentSectorRoot != _temple ||
                _owner._level.Player != _owner._player || _owner._bellSignal != 0 || _owner._chain.IsOpen ||
                _courtyard.LastResult != FragmentCollectionResult.Collected || !_courtyard.IsCollected ||
                _courtyard.FeedbackCount != 1 || QuestFragmentProgressModel.CountCollected(live.CollectedFragmentIds) != 6 ||
                !DurableClosedParity(live, 6, requireSavedSeal: true) ||
                !live.ActivatedMechanisms.Contains(CanyonWorldLayout.CourtyardSealMechanismId))
            {
                _owner.Fail($"admission hull: uncorroborated or misordered courtyard Collected tick={tick} " +
                    $"five_tick={_five} count={live.CollectedFragmentIds.Count} bell_signal={_owner._bellSignal}");
                return;
            }
            _collected = tick;
        }

        private int ReadMask(ulong tick, bool authorized)
        {
            int mask = 0;
            for (int i = 0; i < _faces.Length; i++)
                if (_faces[i].ReadAdmissionDisabled()) mask |= 1 << i;
            if (mask != 0 && !authorized) Check(false, $"native face disabled before authorization tick={tick} mask={mask}");
            if ((mask & _observedOpenMask) != _observedOpenMask)
                Check(false, $"admission face re-enabled after grant tick={tick} mask={mask} previous={_observedOpenMask}");
            _observedOpenMask |= mask;
            return mask;
        }

        internal void BeforeMotor(ulong tick, BossAdmissionResult admission)
        {
            var progress = _owner._level.Progress;
            if (progress.LegacyCollectionQuotaExempt) Check(false, $"unexpected quota exemption tick={tick}");
            _preAuthorized = Authorized(tick, admission);
            _preMask = ReadMask(tick, _preAuthorized);
            bool bellClosed = _owner._bellSignal == 0 && !_owner._chain.IsOpen && !admission.BellChainReady;
            if (_sixNoBell == 0 && admission.CourtyardReady && bellClosed && _preMask == 0 &&
                _owner._level.CurrentSectorRoot == _temple)
            {
                int count = progress.CollectedFragmentIds.Count;
                // The seal updates live progress; the next durable collection
                // commit, not the earlier five-ID sample, must contain that seal.
                if (_five == 0 && _collected == 0 && count == 5 && admission.MissingFragments == 1 &&
                    !_courtyard.IsCollected && DurableClosedParity(progress, 5, requireSavedSeal: false)) _five = tick;
                if (_collected != 0 && _courtyardBodyEntered == _collected && _collected <= tick &&
                    count == 6 && admission.MissingFragments == 0 && DurableClosedParity(progress, 6, requireSavedSeal: true))
                    _sixNoBell = tick;
            }
            // QuestFragment may emit Collected inside the engine's BodyEntered
            // dispatch, before this independent listener gets the outer signal.
            if (_collected != 0 && _collected < tick && _courtyardBodyEntered != _collected)
                Check(false, $"courtyard Collected had no same-tick native Player BodyEntered: {Ticks}");
            if (_preAuthorized && _authorized == 0)
            {
                Check(_sixNoBell != 0 && _sixNoBell < _owner._bellSignal && _owner._bellSignal <= tick,
                    $"grant did not follow the six-fragment closed sample tick={tick} six_tick={_sixNoBell}");
                _authorized = tick;
            }
            if (_authorized != 0 && !_preAuthorized) Check(false, $"live admission authority disappeared tick={tick}");
            if (_allDisabled == 0 && _preMask == AllFaces) _allDisabled = tick;
        }

        internal void AfterMotor(ulong tick, Rect2 before, Rect2 after)
        {
            // A same-tick Bell event can schedule native disable. It cannot lend
            // authorization to this Motor step if it was missing before the step.
            bool authorizedNow = _preAuthorized || (_owner._bellSignal == tick &&
                Authorized(tick, BossAdmissionModel.Evaluate(_owner._level.Progress)));
            ReadMask(tick, authorizedNow);
            if (_left.Complete) return;
            Rect2 left = _faces[0].Bounds;
            bool usable = _preMask == AllFaces && _preAuthorized;
            if (!usable && after.End.X > left.Position.X + .1f && after.End.Y > left.Position.Y && after.Position.Y < left.End.Y)
                Check(false, $"capsule entered the left face while not authorized and fully disabled tick={tick} after={after}");
            Vector2 a = before.GetCenter(), b = after.GetCenter();
            if (a.X < left.GetCenter().X && b.X >= left.GetCenter().X)
            {
                float atPlane = Mathf.Lerp(a.Y, b.Y, (left.GetCenter().X - a.X) / (b.X - a.X));
                float halfHeight = before.Size.Y / 2;
                Check(atPlane - halfHeight >= left.Position.Y && atPlane + halfHeight <= left.End.Y,
                    $"left passage did not fit the whole capsule in the saved aperture tick={tick} center_y={atPlane}");
            }
            string failure = _left.Step(tick, left, before, after, _preMask == AllFaces, _preAuthorized);
            if (failure != null) Check(false, $"left passage tick={tick} before={before} after={after}: {failure}");
        }

        internal bool CanGap(Rect2 capsule) => _five == 0 && _courtyardBodyEntered == 0 && _collected == 0 && _sixNoBell == 0 &&
            _authorized == 0 && _observedOpenMask == 0 && _left.CanGap(_faces[0].Bounds, capsule);

        internal bool Complete => _left.Complete && _observedOpenMask == AllFaces &&
            _left.ClosedTick <= _five && _five != 0 && _five <= _collected && _courtyardBodyEntered == _collected &&
            _collected <= _sixNoBell &&
            _sixNoBell < _owner._bellSignal && _owner._bellSignal <= _authorized &&
            _authorized <= _allDisabled && _allDisabled <= _left.CrossTick;

        internal string Ticks => $"closed_tick={_left.ClosedTick} five_seal_tick={_five} courtyard_tick={_collected} " +
            $"six_no_bell_tick={_sixNoBell} bell_signal={_owner._bellSignal} authorized_tick={_authorized} " +
            $"all_disabled_tick={_allDisabled} left_clear_tick={_left.CrossTick} courtyard_body_tick={_courtyardBodyEntered}";

        internal void Print(ulong completionTick)
        {
            foreach (BoundGate face in _faces)
                GD.Print($"NORMAL_ROUTE_ADMISSION_FACE variant={_owner._variant} face={face.Name} body={face.BodyId} " +
                    $"body_rid={face.BodyRid} shape={face.ShapeId} owner={face.OwnerId} native_index={face.ShapeIndex} " +
                    $"resource={face.ResourceId} resource_rid={face.ResourceRid} bounds={face.Bounds}");
            GD.Print($"NORMAL_ROUTE_ADMISSION_HULL_OK variant={_owner._variant} temple={_temple.GetInstanceId()} " +
                $"player={_owner._player.GetInstanceId()} fragment={_courtyard.GetInstanceId()} {Ticks} " +
                $"completion_tick={completionTick} scope=three_native_states_left_passage");
        }

        public void Dispose()
        {
            if (!GodotObject.IsInstanceValid(_courtyard)) return;
            _courtyard.BodyEntered -= OnCourtyardBodyEntered;
            _courtyard.Collected -= OnCourtyardCollected;
        }
    }

    private sealed class BoundGate
    {
        internal readonly string Name;
        internal readonly Rect2 Bounds;
        internal readonly ulong BodyId, ShapeId;
        internal readonly GateLedger Ledger = new();
        internal bool NativeOpen, Authorized;
        private readonly StaticBody2D _body;
        private readonly CollisionShape2D _shape;
        private readonly RectangleShape2D _rectangle;
        private readonly uint _owner;
        private readonly CharacterBody2D _player;
        internal uint OwnerId => _owner;
        internal int ShapeIndex { get; }
        internal ulong BodyRid { get; }
        internal ulong ResourceId { get; }
        internal ulong ResourceRid { get; }

        internal BoundGate(string name, CollisionShape2D shape, CharacterBody2D player)
        {
            Name = name; _shape = shape; _player = player;
            _body = shape.GetParent() as StaticBody2D ?? throw new InvalidOperationException($"{name} is not a native static gate.");
            _rectangle = shape.Shape as RectangleShape2D ?? throw new InvalidOperationException($"{name} has no saved rectangle.");
            bool found = false;
            foreach (int owner in _body.GetShapeOwners())
            {
                if (_body.ShapeOwnerGetOwner((uint)owner) != _shape) continue;
                if (found || _body.ShapeOwnerGetShapeCount((uint)owner) != 1 || _body.ShapeOwnerGetShape((uint)owner, 0) != _rectangle)
                    throw new InvalidOperationException($"{name} has ambiguous native rectangle ownership.");
                _owner = (uint)owner; found = true;
            }
            TestAssert.True(found && _body.GetWorld2D() == _player.GetWorld2D(), $"{name} has one real Player-world shape owner");
            Bounds = ShapeBounds(); BodyId = _body.GetInstanceId(); ShapeId = _shape.GetInstanceId();
            ShapeIndex = _body.ShapeOwnerGetShapeIndex(_owner, 0);
            BodyRid = _body.GetRid().Id; ResourceId = _rectangle.GetInstanceId(); ResourceRid = _rectangle.GetRid().Id;
        }

        private Rect2 ShapeBounds() => new(_shape.GlobalPosition - _rectangle.Size / 2f, _rectangle.Size);

        internal void ReadPre(bool authorized)
        {
            if (!Live(_body) || !Live(_shape) || _shape.GetParent() != _body || _shape.Shape != _rectangle ||
                _shape.OneWayCollision || (_body.CollisionLayer & _player.CollisionMask) == 0 ||
                !Upright(_shape.GlobalTransform) || ShapeBounds() != Bounds || Bounds.Size.X <= 0 || Bounds.Size.Y <= 0)
                throw new InvalidOperationException($"{Name} no longer has its original solid native geometry.");
            NativeOpen = _body.IsShapeOwnerDisabled(_owner);
            if (NativeOpen != _shape.Disabled) throw new InvalidOperationException($"{Name} native owner/node collision state diverged.");
            Authorized = authorized;
        }

        internal bool ReadAdmissionDisabled()
        {
            try
            {
                ReadPre(false);
                var world = _player.GetWorld2D();
                var bodyRid = _body.GetRid();
                if (_body.GetWorld2D() != world || bodyRid.Id != BodyRid || PhysicsServer2D.BodyGetSpace(bodyRid) != world.Space ||
                    _rectangle.GetRid().Id != ResourceRid || _body.ShapeOwnerGetOwner(_owner) != _shape ||
                    _body.ShapeOwnerGetShapeCount(_owner) != 1 || _body.ShapeOwnerGetShape(_owner, 0) != _rectangle ||
                    _body.ShapeOwnerGetShapeIndex(_owner, 0) != ShapeIndex)
                    throw new InvalidOperationException("native owner/index/world changed");
                return NativeOpen;
            }
            catch (Exception error) { throw new InvalidOperationException($"admission hull face {Name}: {error.Message}", error); }
        }
    }

    private sealed class GateLedger
    {
        internal ulong ClosedTick, OpenTick, CrossTick;
        internal int Crossings;
        private bool _began, _transit;
        private int _side = -1;
        internal bool Complete => ClosedTick != 0 && OpenTick >= ClosedTick && CrossTick >= OpenTick && Crossings > 0 && !_transit;

        private static int Side(Rect2 gate, Rect2 body) => body.End.X <= gate.Position.X ? -1 :
            body.Position.X >= gate.End.X ? 1 : 0;

        internal bool CanGap(Rect2 gate, Rect2 body) => !_transit && _side == -1 && Crossings == 0 && Side(gate, body) == -1;

        internal string Step(ulong tick, Rect2 gate, Rect2 before, Rect2 after, bool open, bool authorized)
        {
            if (!_began)
            {
                _began = true;
                if (Side(gate, before) != -1 || open || authorized) return "missing genuine initially closed near-side baseline";
                ClosedTick = tick;
            }
            if (open && authorized && OpenTick == 0) OpenTick = tick;
            float plane = gate.GetCenter().X;
            Vector2 a = before.GetCenter(), b = after.GetCenter();
            if (!_transit && (_side < 0 ? a.X < plane && b.X >= plane : a.X > plane && b.X <= plane))
            {
                if (!open || !authorized) return "center crossed while native door was closed or independent authority was missing";
                float atPlane = Mathf.Lerp(a.Y, b.Y, (plane - a.X) / (b.X - a.X));
                float halfHeight = before.Size.Y / 2f;
                if (atPlane + halfHeight <= gate.Position.Y || atPlane - halfHeight >= gate.End.Y)
                    return "crossed around the gate outside the actual collider's vertical span";
                _transit = true;
            }
            int next = Side(gate, after);
            if (_transit && next != _side && (!open || !authorized)) return "crossing continued after native opening or authority was lost";
            if (next == -_side)
            {
                if (!_transit) return "reached far side without a continuously witnessed gate transit";
                if (_side < 0) { Crossings++; if (CrossTick == 0) CrossTick = tick; }
                _side = next; _transit = false;
            }
            else if (next == _side) _transit = false;
            return null;
        }
    }

    private static void Discriminants()
    {
        Rect2 gate = new(10, 0, 4, 10);
        Rect2 Box(float x, float y) => new(x - 1, y - 2, 2, 4);
        GateLedger Closed() { var value = new GateLedger(); TestAssert.True(value.Step(1, gate, Box(7, 4), Box(8, 4), false, false) == null,
            "synthetic gate begins at the actual near side"); return value; }
        var legal = Closed();
        TestAssert.True(legal.Step(2, gate, Box(8, 4), Box(13, 4), true, true) == null &&
            legal.Step(3, gate, Box(13, 4), Box(16, 4), true, true) == null && legal.Complete,
            "authorized real opening and whole capsule far-side transit earn one gate receipt");
        TestAssert.True(Closed().Step(2, gate, Box(8, -9), Box(16, -9), false, false) != null &&
            Closed().Step(2, gate, Box(8, -9), Box(16, -9), true, true) != null,
            "neither closed nor authorized outside-height detours masquerade as passage through the gate");
        TestAssert.True(Closed().Step(2, gate, Box(8, 4), Box(16, 4), true, false) != null &&
            Closed().Step(2, gate, Box(8, 4), Box(16, 4), false, true) != null,
            "physical opening and independent authority are both required");
        TestAssert.True(new GateLedger().Step(1, gate, Box(16, 4), Box(17, 4), true, true) != null &&
            Closed().CanGap(gate, Box(8, 4)) && !Closed().CanGap(gate, Box(16, 4)),
            "initial far side is not a crossing and a mount gap is allowed only before an actual transit");
    }
}
