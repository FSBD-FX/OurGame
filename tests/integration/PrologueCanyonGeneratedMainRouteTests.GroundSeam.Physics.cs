using System;
using Godot;
using Knight.Player;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    private readonly record struct SeamPhysicsStep(ulong Tick, ulong Motor, ulong Revision,
        ulong Shape, Vector2 Root, bool Valid);
    private readonly record struct SeamPhysicsProof(bool Complete, int Samples,
        ulong Before, ulong After, string Reason);

    // A value-only diagnostic reader, not a gameplay or visual interpolation model.
    private static SeamPhysicsProof ValidateGroundSeamPhysics(ReadOnlySpan<SeamPhysicsStep> steps,
        ulong first, ulong last, ulong motor, ulong revision, ulong left, ulong right)
    {
        if (first == 0 || last <= first || last - first >= 256 || motor == 0 ||
            left == 0 || right == 0 || left == right)
            return new(false, steps.Length, 0, 0, "invalid-window");
        if ((ulong)steps.Length != last - first + 1)
            return new(false, steps.Length, 0, 0, "missing-steps");
        ulong before = 0, after = 0;
        int changes = 0;
        for (int i = 0; i < steps.Length; i++)
        {
            var step = steps[i];
            if (step.Tick != first + (ulong)i)
                return new(false, steps.Length, 0, 0, "tick-order");
            if (!step.Valid || step.Motor != motor || step.Revision != revision ||
                step.Shape != left && step.Shape != right || !step.Root.IsFinite())
                return new(false, steps.Length, 0, 0, "invalid-authority-or-contact");
            if (i == 0) continue;
            var prior = steps[i - 1];
            if (step.Root.X <= prior.Root.X || Mathf.Abs(step.Root.Y - prior.Root.Y) >= .05f)
                return new(false, steps.Length, 0, 0, "not-ordinary-horizontal-motion");
            if (step.Shape == prior.Shape) continue;
            if (prior.Shape != left || step.Shape != right || ++changes != 1)
                return new(false, steps.Length, 0, 0, "non-monotone-contact");
            before = prior.Tick;
            after = step.Tick;
        }
        return steps[0].Shape == left && steps[^1].Shape == right && changes == 1
            ? new(true, steps.Length, before, after, "complete-native-not-visual")
            : new(false, steps.Length, 0, 0, "missing-handoff");
    }

    public static void RunGroundSeamPhysicsControls()
    {
        SeamPhysicsStep[] literal = {
            new(450, 3, 0, 11, new(1268, 600), true),
            new(451, 3, 0, 11, new(1272, 600), true),
            new(452, 3, 0, 12, new(1276, 600), true),
            new(453, 3, 0, 12, new(1280, 600), true),
            new(454, 3, 0, 12, new(1284, 600), true)
        };
        var proof = ValidateGroundSeamPhysics(literal, 450, 454, 3, 0, 11, 12);
        TestAssert.True(proof.Complete && proof.Samples == 5 && proof.Before == 451 && proof.After == 452,
            "WORLD_GROUND_SEAM_NATIVE_CONTROL_RED: literal five physics steps must expose actual 451->452 contact handoff despite two visual endpoints");
        int rejected = 0;
        void Reject(SeamPhysicsStep[] input, string name, ulong first = 450, ulong last = 454,
            ulong motor = 3, ulong revision = 0, ulong left = 11, ulong right = 12)
        {
            TestAssert.True(!ValidateGroundSeamPhysics(input, first, last, motor, revision, left, right).Complete,
                $"WORLD_GROUND_SEAM_NATIVE_CONTROL: {name} must not produce native coverage");
            rejected++;
        }
        SeamPhysicsStep[] Change(int index, SeamPhysicsStep value)
        {
            var result = (SeamPhysicsStep[])literal.Clone();
            result[index] = value;
            return result;
        }
        Reject(new[] { literal[0], literal[1], literal[3], literal[4] }, "missing internal step");
        Reject(Change(2, literal[2] with { Tick = 451 }), "duplicate tick");
        Reject(Change(2, literal[2] with { Tick = 455 }), "out-of-order tick");
        Reject(Change(2, literal[2] with { Valid = false }), "revoked or ambiguous sample");
        Reject(Change(2, literal[2] with { Motor = 4 }), "different motor");
        Reject(Change(2, literal[2] with { Revision = 1 }), "stale revision");
        Reject(Change(2, literal[2] with { Shape = 99 }), "unrelated contact");
        Reject(Change(2, literal[2] with { Root = new(float.NaN, 600) }), "nonfinite root");
        Reject(Change(2, literal[2] with { Root = new(1260, 600) }), "backward displacement");
        Reject(Change(2, literal[2] with { Root = new(1276, 601) }), "vertical displacement");
        var noHandoff = (SeamPhysicsStep[])literal.Clone();
        for (int i = 0; i < noHandoff.Length; i++) noHandoff[i] = noHandoff[i] with { Shape = 11 };
        Reject(noHandoff, "no handoff");
        Reject(Change(3, literal[3] with { Shape = 11 }), "right-left-right contacts");
        Reject(literal, "zero motor", motor: 0);
        Reject(literal, "identical floor identities", right: 11);
        Reject(literal, "zero first tick", first: 0);
        Reject(literal, "window exceeds fixed ledger", last: 706);
        GD.Print($"WORLD_GROUND_SEAM_NATIVE_CONTROLS_OK positive=1 negative={rejected} revision_zero_valid=true native=false visual_interpolation=false");
    }

    private sealed partial class Task10GroundSeamEvidence
    {
        private readonly record struct NativeEntry(SeamPhysicsStep Step, ulong Process,
            ulong Publication, bool InPhysics, float Delta, int Slides, int Upward,
            int Distinct, int SlideIndex, int ShapeIndex, ulong Body, Vector2 Point,
            Vector2 Normal, string InvalidReason);
        private readonly NativeEntry[] _nativeTrail = new NativeEntry[256];
        private PlayerMotor _nativeMotor;
        private BasinBoardingTickObserver _nativeObserver;
        private ulong _nativeMotorId;
        private int _nativeWrite, _nativeCount, _nativeAttempts, _nativeComplete;
        private bool _nativeClosed, _nativeAuthorityLost;
        private string _nativeExitReason = "";
        private Exception _nativeFailure;

        private void InitializeNativeTrace()
        {
            _nativeMotor = _player.GetNode<PlayerMotor>("Motor");
            _nativeMotorId = _nativeMotor.GetInstanceId();
            TestAssert.True(_nativeMotor.ProcessPhysicsPriority == 0 &&
                _player.GetNode<Node>("Input").ProcessPhysicsPriority == -300 &&
                _player.GetNode<Node>("MechanicalClaw").ProcessPhysicsPriority == -100 &&
                _player.GetNode<Node>("EnvironmentInteraction").ProcessPhysicsPriority == -50,
                "WORLD_GROUND_SEAM_NATIVE: probe must follow the original saved movement owners");
            _nativeObserver = new BasinBoardingTickObserver
            {
                Name = "GroundSeamNativePostMotor", ProcessMode = Node.ProcessModeEnum.Always,
                ProcessPhysicsPriority = _nativeMotor.ProcessPhysicsPriority + 1,
                Sample = ObserveNativeStep
            };
            _level.AddChild(_nativeObserver);
            _level.TreeExiting += NativeOwnerExiting;
            _player.TreeExiting += NativeOwnerExiting;
            _nativeMotor.TreeExiting += NativeOwnerExiting;
            _nativeObserver.TreeExiting += NativeOwnerExiting;
        }

        private void NativeOwnerExiting()
        {
            if (_nativeClosed) return;
            _nativeAuthorityLost = true;
            _nativeExitReason = "original-owner-tree-exiting";
        }

        private void ObserveNativeStep(double delta)
        {
            if (_nativeClosed || _disposed || _nativeFailure != null) return;
            try
            {
                if (!Live(_player)) { NativeOwnerExiting(); return; }
                Vector2 root = _player.GlobalPosition;
                if (root.X < 1100 || root.X > 1420 || Mathf.Abs(root.Y - 600) > 2) return;
                ulong tick = Engine.GetPhysicsFrames();
                var before = GodotObject.IsInstanceValid(_nativeMotor)
                    ? _nativeMotor.GroundMotionObservation : default;
                ulong revision = GodotObject.IsInstanceValid(_nativeMotor)
                    ? _nativeMotor.MotionAuthorityRevision : 0;
                string invalid = "";
                if (_nativeAuthorityLost) invalid = _nativeExitReason;
                else if (_tree.Paused) invalid = "paused";
                else if (!Live(_level) || !Live(_world) || !Live(_nativeMotor) ||
                    !Live(_nativeObserver) || _nativeObserver.GetParent() != _level ||
                    _nativeObserver.ProcessPhysicsPriority != _nativeMotor.ProcessPhysicsPriority + 1 ||
                    !_nativeMotor.IsPhysicsProcessing() || !_nativeMotor.CanProcess()) invalid = "owner-unavailable";
                else if (before.OwnerId != _nativeMotorId || before.Revision != revision ||
                    before.PhysicsTick != tick) invalid = "missing-or-revoked-completed-step";
                else if (!float.IsFinite(before.StepSeconds) || before.StepSeconds <= 0 ||
                    Math.Abs(before.StepSeconds - delta) > .00001 || !before.Grounded || !before.Eligible ||
                    !_player.IsOnFloor() || !float.IsFinite(before.ActualX) ||
                    Mathf.Abs(before.ActualX - _player.Velocity.X) > .001f || _player.Velocity.X <= 80)
                    invalid = "not-completed-ordinary-walk";

                ulong shapeId = 0, bodyId = 0;
                int slides = 0, upward = 0, distinct = 0, slideIndex = -1, shapeIndex = -1;
                Vector2 point = Vector2.Zero, normal = Vector2.Zero;
                if (invalid.Length == 0)
                {
                    slides = _player.GetSlideCollisionCount();
                    for (int i = 0; i < slides; i++)
                    {
                        // All native objects below are callback-local BORROWED values.
                        var contact = _player.GetSlideCollision(i);
                        Vector2 candidateNormal = contact.GetNormal();
                        if (!candidateNormal.IsFinite() || candidateNormal.Dot(Vector2.Up) < .7f) continue;
                        upward++;
                        if (contact.GetCollider() is not CollisionObject2D body || !Live(body) ||
                            contact.GetColliderShape() is not CollisionShape2D shape || !Live(shape) ||
                            shape.GetParent() != body || shape.Disabled ||
                            (body.CollisionLayer & _player.CollisionMask) == 0 ||
                            body.GetWorld2D().GetInstanceId() != _physicsWorldId)
                        { invalid = "unknown-upward-contact"; continue; }
                        var owners = body.GetShapeOwners();
                        int nativeIndex = contact.GetColliderShapeIndex();
                        if (owners.Length != 1 || nativeIndex < 0 ||
                            body.IsShapeOwnerDisabled((uint)owners[0]) ||
                            body.ShapeOwnerGetShapeCount((uint)owners[0]) != 1 ||
                            body.ShapeOwnerGetShapeIndex((uint)owners[0], 0) != nativeIndex ||
                            body.ShapeFindOwner(nativeIndex) != (uint)owners[0] ||
                            body.ShapeOwnerGetOwner((uint)owners[0]) != shape ||
                            shape.Shape is not RectangleShape2D ||
                            body.ShapeOwnerGetShape((uint)owners[0], 0) != shape.Shape ||
                            !contact.GetPosition().IsFinite())
                        { invalid = "unresolved-native-shape-owner"; continue; }
                        ulong candidate = shape.GetInstanceId();
                        if (shapeId == 0)
                        {
                            distinct = 1; shapeId = candidate; bodyId = body.GetInstanceId();
                            point = contact.GetPosition(); normal = candidateNormal;
                            slideIndex = i; shapeIndex = nativeIndex;
                        }
                        else if (candidate != shapeId) distinct = 2;
                    }
                    if (upward == 0 || shapeId == 0) invalid = "no-native-upward-contact";
                    else if (distinct > 1) invalid = "multiple-upward-shape-owners";
                    if (_nativeMotor.GroundMotionObservation != before ||
                        _nativeMotor.MotionAuthorityRevision != revision)
                        invalid = "step-revoked-during-read";
                }
                var step = new SeamPhysicsStep(tick, before.OwnerId, before.Revision, shapeId, root,
                    invalid.Length == 0);
                _nativeTrail[_nativeWrite] = new(step, Engine.GetProcessFrames(), _publicationSequence,
                    Engine.IsInPhysicsFrame(), (float)delta, slides, upward, distinct, slideIndex,
                    shapeIndex, bodyId, point, normal, invalid);
                _nativeWrite = (_nativeWrite + 1) % _nativeTrail.Length;
                _nativeCount = Math.Min(_nativeCount + 1, _nativeTrail.Length);
            }
            catch (Exception error) { _nativeFailure ??= error; }
        }

        private SeamPhysicsStep[] PrintNativeHandoff(SeamSample old, SeamSample now)
        {
            // This exact value-only batch also feeds the joint publication gate.
            try
            {
                _nativeAttempts++;
                var entries = new System.Collections.Generic.List<NativeEntry>();
                int first = (_nativeWrite - _nativeCount + _nativeTrail.Length) % _nativeTrail.Length;
                for (int i = 0; i < _nativeCount; i++)
                {
                    var entry = _nativeTrail[(first + i) % _nativeTrail.Length];
                    if (entry.Step.Tick >= old.Tick && entry.Step.Tick <= now.Tick) entries.Add(entry);
                }
                var steps = new SeamPhysicsStep[entries.Count];
                for (int i = 0; i < steps.Length; i++) steps[i] = entries[i].Step;
                var proof = ValidateGroundSeamPhysics(steps, old.Tick, now.Tick,
                    _nativeMotorId, _nativeMotor.MotionAuthorityRevision, _leftId, _rightId);
                if (proof.Complete && _nativeFailure == null && !_nativeAuthorityLost) _nativeComplete++;
                GD.Print($"WORLD_GROUND_SEAM_NATIVE_PAIR variant={_variant} old_tick={old.Tick} now_tick={now.Tick} " +
                    $"old_publication={old.Publication} now_publication={now.Publication} samples={steps.Length} " +
                    $"capacity={_nativeTrail.Length} complete={proof.Complete} reason={proof.Reason} " +
                    $"actual_before={proof.Before} actual_after={proof.After} motor={_nativeMotorId} " +
                    $"revision={_nativeMotor.MotionAuthorityRevision} authority_lost={_nativeAuthorityLost} " +
                    $"callback_fault={_nativeFailure != null} native_only=true missing_visuals_fabricated=false " +
                    "publication_gate=adjacent-publications-complete-native-v1");
                foreach (var entry in entries)
                {
                    var step = entry.Step;
                    GD.Print($"WORLD_GROUND_SEAM_NATIVE_STEP variant={_variant} tick={step.Tick} motor={step.Motor} " +
                        $"revision={step.Revision} shape={step.Shape} root={step.Root} valid={step.Valid} " +
                        $"reason={entry.InvalidReason} process={entry.Process} last_publication={entry.Publication} " +
                        $"in_physics={entry.InPhysics} delta={entry.Delta:R} slides={entry.Slides} " +
                        $"upward={entry.Upward} distinct_shapes={entry.Distinct} first_upward_slide={entry.SlideIndex} " +
                        $"native_shape_index={entry.ShapeIndex} body={entry.Body} point={entry.Point} normal={entry.Normal} " +
                        "contains_visual_pose=false");
                }
                return steps;
            }
            catch (Exception error) { _nativeFailure ??= error; }
            return Array.Empty<SeamPhysicsStep>();
        }

        private void AssertNativeTraceComplete()
        {
            if (_nativeFailure != null) throw new InvalidOperationException(
                "WORLD_GROUND_SEAM_NATIVE: cached diagnostic callback failure", _nativeFailure);
            TestAssert.True(!_nativeClosed && !_nativeAuthorityLost && _nativeAttempts > 0 &&
                _nativeComplete == _nativeAttempts && Live(_nativeObserver) &&
                _nativeObserver.GetParent() == _level && _nativeObserver.IsPhysicsProcessing(),
                $"WORLD_GROUND_SEAM_NATIVE_INSUFFICIENT: attempts={_nativeAttempts} complete={_nativeComplete} exit={_nativeExitReason}");
            GD.Print($"WORLD_GROUND_SEAM_NATIVE_OK variant={_variant} attempts={_nativeAttempts} complete={_nativeComplete} " +
                "post_motor=true publication_gate=adjacent-publications-complete-native-v1 pixels=false human=false");
        }

        private void CloseNativeTrace()
        {
            _nativeClosed = true;
            var errors = new System.Collections.Generic.List<Exception>();
            void Clean(Action action)
            {
                try { action(); }
                catch (Exception error) { errors.Add(error); }
            }
            Clean(() => { if (GodotObject.IsInstanceValid(_nativeObserver)) _nativeObserver.Sample = null; });
            Clean(() => { if (GodotObject.IsInstanceValid(_nativeObserver)) _nativeObserver.SetPhysicsProcess(false); });
            Clean(() => { if (GodotObject.IsInstanceValid(_nativeObserver)) _nativeObserver.TreeExiting -= NativeOwnerExiting; });
            Clean(() => { if (GodotObject.IsInstanceValid(_nativeMotor)) _nativeMotor.TreeExiting -= NativeOwnerExiting; });
            Clean(() => { if (GodotObject.IsInstanceValid(_player)) _player.TreeExiting -= NativeOwnerExiting; });
            Clean(() => { if (GodotObject.IsInstanceValid(_level)) _level.TreeExiting -= NativeOwnerExiting; });
            // The existing level teardown owns this Node. Never Free it inside its callback.
            if (errors.Count > 0) throw new AggregateException("Native seam probe cleanup failed", errors);
        }
    }
}
