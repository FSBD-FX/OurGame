using System;
using Godot;

namespace Knight.Abilities;

internal enum KeyboardClawTraceStage { Begin, Ray, Candidate, Baseline, EnemyPromotion, Result, Invalid }
internal enum KeyboardClawTraceReason
{
    None, InvalidInput, InvalidSurface, IneligibleReceiver, Range, Angle, Occluded,
    BaselineRetained, BaselineReplaced, EnemyNotCloser, EnemyRejectedByRank, EnemyPromoted, ProtectedBaseline
}

internal readonly record struct KeyboardClawTraceRow(
    KeyboardClawTraceStage Stage, KeyboardClawTraceReason Reason = KeyboardClawTraceReason.None,
    int Ray = -17, ulong Surface = 0, ulong Collider = 0, Vector2 Point = default,
    Vector2 Normal = default, Vector2 RayEnd = default, float Angle = float.NaN, float Distance = float.NaN,
    bool LosEvaluated = false, bool Los = false, bool Promotable = false,
    ulong Baseline = 0, float BaselineAngle = float.NaN, float BaselineDistance = float.NaN,
    string Path = null);

// One synchronous invocation per fresh buffer. Contains no native object,
// callback or permission; saturation only loses explicitly counted evidence.
internal sealed class MechanicalClawKeyboardTrace
{
    private readonly KeyboardClawTraceRow[] _rows;
    internal ulong FocusSurfaceId { get; }
    internal int Count { get; private set; }
    internal int Total { get; private set; }
    internal int Dropped { get; private set; }
    internal int Faults { get; private set; }
    internal int ConeQueries { get; private set; }
    internal int LosQueries { get; private set; }
    internal MechanicalClawKeyboardTrace(int capacity = 128, ulong focusSurfaceId = 0)
    {
        if (capacity < 0 || capacity > 512) throw new ArgumentOutOfRangeException(nameof(capacity));
        _rows = capacity == 0 ? Array.Empty<KeyboardClawTraceRow>() : new KeyboardClawTraceRow[capacity];
        FocusSurfaceId = focusSurfaceId;
    }
    internal void Add(KeyboardClawTraceRow row)
    {
        Total++;
        if (Count == _rows.Length) { Dropped++; return; }
        _rows[Count++] = row;
    }
    internal void ConeQuery() => ConeQueries++;
    internal void LosQuery() => LosQueries++;
    internal void Fault() => Faults++;
    internal KeyboardClawTraceRow Read(int index) => index >= 0 && index < Count
        ? _rows[index] : throw new ArgumentOutOfRangeException(nameof(index));
}
