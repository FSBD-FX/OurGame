using System;

namespace Knight.Prologue.Guidance;

public readonly record struct CanyonFireflyInput(
    bool GuidanceEnabled, bool Paused, bool CombatTelegraphActive,
    bool AtImportantFork, bool BossExitAvailable, bool RouteProgressed,
    bool HasReachableMarker);

public readonly record struct CanyonFireflyFrame(
    bool Visible, float VisibleSecondsRemaining, float CooldownSecondsRemaining);

public sealed class CanyonFireflyGuidanceModel
{
    public const float StalledTriggerSeconds = 18f;
    public const float VisibleDurationSeconds = 3f;
    public const float CooldownDurationSeconds = 15f;

    public CanyonFireflyFrame CurrentFrame { get; private set; }
    private float _stalledSeconds;

    public CanyonFireflyFrame Advance(float elapsedSeconds, CanyonFireflyInput input)
    {
        if (!float.IsFinite(elapsedSeconds) || elapsedSeconds < 0f)
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), elapsedSeconds,
                "Elapsed active-play seconds must be finite and non-negative.");
        if (!input.GuidanceEnabled)
        {
            Reset();
            return CurrentFrame;
        }
        if (input.Paused) return CurrentFrame;

        if (input.RouteProgressed)
        {
            _stalledSeconds = 0f;
            CurrentFrame = CurrentFrame with
            {
                Visible = false,
                VisibleSecondsRemaining = 0f
            };
        }
        if (!input.HasReachableMarker)
        {
            _stalledSeconds = 0f;
            CurrentFrame = CurrentFrame with
            {
                Visible = false,
                VisibleSecondsRemaining = 0f
            };
        }

        if (CurrentFrame.VisibleSecondsRemaining > 0f)
        {
            float remaining = ClampTimer(CurrentFrame.VisibleSecondsRemaining - elapsedSeconds);
            CurrentFrame = remaining > 0f
                ? CurrentFrame with
                {
                    Visible = !input.CombatTelegraphActive && input.HasReachableMarker,
                    VisibleSecondsRemaining = remaining
                }
                : new CanyonFireflyFrame(false, 0f, CooldownDurationSeconds);
            return CurrentFrame;
        }

        if (CurrentFrame.CooldownSecondsRemaining > 0f)
        {
            float remaining = ClampTimer(CurrentFrame.CooldownSecondsRemaining - elapsedSeconds);
            CurrentFrame = new CanyonFireflyFrame(false, 0f, remaining);
            if (!input.RouteProgressed && input.HasReachableMarker) _stalledSeconds += elapsedSeconds;
            return CurrentFrame;
        }

        if (!input.RouteProgressed && input.HasReachableMarker) _stalledSeconds += elapsedSeconds;
        bool immediate = input.AtImportantFork || input.BossExitAvailable;
        if (!input.RouteProgressed && input.HasReachableMarker && !input.CombatTelegraphActive &&
            (immediate || _stalledSeconds >= StalledTriggerSeconds - .0001f))
            CurrentFrame = new CanyonFireflyFrame(true, VisibleDurationSeconds, 0f);
        else CurrentFrame = default;
        return CurrentFrame;
    }

    public void Reset()
    {
        _stalledSeconds = 0f;
        CurrentFrame = default;
    }

    private static float ClampTimer(float value) => value <= .0001f ? 0f : value;
}
