using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Knight.Abilities;
using Knight.Shared;

namespace Knight.Tests.Integration;

/// <summary>
/// Full-route observer for the real player's public mechanical-thread frames.  A cast is counted
/// only when the runtime publishes Windup; successful casts must then spend multiple physics
/// frames in Travel before Latched and Apply.
/// </summary>
public static class ThreadExperienceRuntimeTests
{
    public static void Run()
    {
        string smoke = File.ReadAllText(ProjectSettings.GlobalizePath(
            "res://tests/smoke/PrologueCriticalPathSmoke.cs"));
        TestAssert.True(smoke.Contains("new ThreadExperienceRuntimeTests.ThreadRouteProbe()",
                StringComparison.Ordinal) &&
            smoke.Contains("CastFrameChanged += _threadProbe.Observe", StringComparison.Ordinal) &&
            smoke.Contains("_threadProbe.AssertIntegratedRoute()", StringComparison.Ordinal),
            "full-route smoke must bind the thread observer to the real player's public cast frames");
        TestAssert.True(!smoke.Contains("new ThreadCastFrame", StringComparison.Ordinal),
            "full-route thread proof cannot manufacture substitute cast frames");
        TestAssert.True(smoke.Contains("RetryThreadWithoutReset", StringComparison.Ordinal) &&
            smoke.Contains("PROLOGUE_THREAD_SMOKE_NATURAL_RETRY_OK", StringComparison.Ordinal),
            "full-route smoke must retry the obstructed authored anchor without ResetTransientState");
    }

    public sealed class ThreadRouteProbe
    {
        private readonly List<string> _violations = new();
        private readonly HashSet<ThreadAimMode> _successfulModes = new();
        private CastRecord _current;

        public int SuccessfulCastCount { get; private set; }
        public bool SawObstructedFailure { get; private set; }

        public void Observe(ThreadCastFrame frame, ThreadTarget2D target)
        {
            if (frame.State == ThreadCastState.Idle)
            {
                _current = null;
                return;
            }

            if (frame.State == ThreadCastState.Windup &&
                (_current == null || _current.CastId != frame.CastId || _current.SawApply))
                _current = new CastRecord(frame.CastId, frame.AimMode, frame.TargetId);

            if (frame.State == ThreadCastState.Failed)
            {
                if (frame.Failure == ThreadFailureReason.Obstructed) SawObstructedFailure = true;
                _current = null;
                return;
            }

            if (_current == null || frame.CastId != _current.CastId) return;
            if (frame.TargetId != _current.TargetId)
                _violations.Add($"cast {_current.CastId} changed target from {_current.TargetId} to {frame.TargetId}");

            switch (frame.State)
            {
                case ThreadCastState.Windup:
                    _current.SawWindup = true;
                    break;
                case ThreadCastState.Travel:
                    if (!_current.SawWindup)
                        _violations.Add($"cast {_current.CastId} entered Travel before Windup");
                    _current.SawTravel = true;
                    _current.TravelPhysicsFrames.Add(Engine.GetPhysicsFrames());
                    break;
                case ThreadCastState.Latched:
                    if (!_current.SawTravel)
                        _violations.Add($"cast {_current.CastId} entered Latched before Travel");
                    _current.SawLatched = true;
                    break;
                case ThreadCastState.Apply when !_current.SawApply:
                    _current.SawApply = true;
                    if (!_current.SawWindup || !_current.SawTravel || !_current.SawLatched)
                        _violations.Add($"cast {_current.CastId} skipped its typed Windup/Travel/Latched sequence");
                    if (_current.TravelPhysicsFrames.Count < 2)
                        _violations.Add($"cast {_current.CastId} collapsed Travel into " +
                            $"{_current.TravelPhysicsFrames.Count} distinct physics frame(s) before Apply");
                    if (target == null || target.GetInstanceId() != _current.TargetId)
                        _violations.Add($"cast {_current.CastId} Apply did not publish its real locked target");
                    SuccessfulCastCount++;
                    _successfulModes.Add(_current.AimMode);
                    break;
                case ThreadCastState.Recovery:
                    if (!_current.SawApply)
                        _violations.Add($"cast {_current.CastId} entered Recovery before Apply");
                    break;
            }
        }

        public void AssertIntegratedRoute()
        {
            TestAssert.True(SuccessfulCastCount >= 4,
                $"full route must observe at least four real successful thread casts; actual={SuccessfulCastCount}");
            TestAssert.True(_successfulModes.Contains(ThreadAimMode.Tap),
                $"full route must observe one neutral Tap cast through the real player ability; " +
                $"observed={string.Join(',', _successfulModes)} successes={SuccessfulCastCount}");
            TestAssert.True(_successfulModes.Contains(ThreadAimMode.Hold),
                "full route must observe one aimed Hold cast through the real player ability");
            TestAssert.True(SawObstructedFailure,
                "full route must observe the named Obstructed wall failure through the real player ability");
            TestAssert.True(_violations.Count == 0,
                $"thread experience contract failed: {string.Join("; ", _violations)}");
        }

        private sealed class CastRecord
        {
            public CastRecord(ulong castId, ThreadAimMode aimMode, ulong targetId)
            {
                CastId = castId;
                AimMode = aimMode;
                TargetId = targetId;
            }

            public ulong CastId { get; }
            public ThreadAimMode AimMode { get; }
            public ulong TargetId { get; }
            public bool SawWindup { get; set; }
            public bool SawTravel { get; set; }
            public bool SawLatched { get; set; }
            public bool SawApply { get; set; }
            public HashSet<ulong> TravelPhysicsFrames { get; } = new();
        }
    }
}
