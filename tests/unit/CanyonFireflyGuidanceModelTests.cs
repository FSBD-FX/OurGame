using Knight.Prologue.Guidance;

namespace Knight.Tests.Unit;

public static class CanyonFireflyGuidanceModelTests
{
    public static void Run()
    {
        StalledThresholdIsInclusiveAndHintDurationIsExact();
        CooldownPreventsRepeatAndRouteProgressRestartsTheStallWindow();
        PauseFreezesEveryElapsedActivePlayTimer();
        CombatTelegraphDefersAnOtherwiseEligibleHint();
        ImportantForkAndBossExitCanPromptWithoutWaitingForAStall();
        GuidanceAndReachabilityFailClosed();
    }

    private static void StalledThresholdIsInclusiveAndHintDurationIsExact()
    {
        var model = new CanyonFireflyGuidanceModel();
        CanyonFireflyFrame below = model.Advance(17.999f, Input());
        TestAssert.True(!below.Visible && below.VisibleSecondsRemaining == 0f,
            "firefly remains hidden immediately below the eighteen-second stall threshold");

        CanyonFireflyFrame at = model.Advance(.001f, Input());
        TestAssert.True(at.Visible,
            "firefly appears exactly at the eighteen-second active-play threshold");
        TestAssert.Near(3f, at.VisibleSecondsRemaining, .0001f,
            "new firefly hint starts with exactly three visible seconds");

        CanyonFireflyFrame almostDone = model.Advance(2.999f, Input());
        TestAssert.True(almostDone.Visible,
            "firefly remains visible immediately below its three-second duration");
        CanyonFireflyFrame done = model.Advance(.001f, Input());
        TestAssert.True(!done.Visible,
            "firefly clears exactly at its three-second duration");
        TestAssert.Near(15f, done.CooldownSecondsRemaining, .0001f,
            "completed hint starts the exact fifteen-second cooldown");
    }

    private static void CooldownPreventsRepeatAndRouteProgressRestartsTheStallWindow()
    {
        var model = new CanyonFireflyGuidanceModel();
        model.Advance(18f, Input());
        model.Advance(3f, Input());
        CanyonFireflyFrame below = model.Advance(14.999f, Input());
        TestAssert.True(!below.Visible && below.CooldownSecondsRemaining > 0f,
            "firefly cannot repeat immediately below cooldown expiry");

        CanyonFireflyFrame progressed = model.Advance(.001f, Input(routeProgressed: true));
        TestAssert.True(!progressed.Visible && progressed.CooldownSecondsRemaining == 0f,
            "route progress at cooldown expiry does not repeat the stale hint");
        TestAssert.True(!model.Advance(17.999f, Input()).Visible,
            "route progress restarts a full stalled-play window");
        TestAssert.True(model.Advance(.001f, Input()).Visible,
            "a fresh eighteen-second stall after route progress may prompt again");
    }

    private static void PauseFreezesEveryElapsedActivePlayTimer()
    {
        var model = new CanyonFireflyGuidanceModel();
        model.Advance(17f, Input());
        CanyonFireflyFrame frozenStall = model.Advance(20f, Input(paused: true));
        TestAssert.True(!frozenStall.Visible,
            "paused wall time cannot satisfy the stalled active-play threshold");
        CanyonFireflyFrame visible = model.Advance(1f, Input());
        CanyonFireflyFrame frozenVisible = model.Advance(20f, Input(paused: true));
        TestAssert.Near(visible.VisibleSecondsRemaining, frozenVisible.VisibleSecondsRemaining, .0001f,
            "pause freezes the visible-duration timer");
        model.Advance(visible.VisibleSecondsRemaining, Input());
        CanyonFireflyFrame cooling = model.CurrentFrame;
        CanyonFireflyFrame frozenCooldown = model.Advance(20f, Input(paused: true));
        TestAssert.Near(cooling.CooldownSecondsRemaining, frozenCooldown.CooldownSecondsRemaining, .0001f,
            "pause freezes the cooldown timer");
    }

    private static void CombatTelegraphDefersAnOtherwiseEligibleHint()
    {
        var model = new CanyonFireflyGuidanceModel();
        CanyonFireflyFrame deferred = model.Advance(18f, Input(combatTelegraphActive: true));
        TestAssert.True(!deferred.Visible,
            "an actual combat telegraph defers an otherwise eligible stalled hint");
        CanyonFireflyFrame released = model.Advance(0f, Input());
        TestAssert.True(released.Visible,
            "deferred hint appears when the telegraph ends without inventing more wall time");
    }

    private static void ImportantForkAndBossExitCanPromptWithoutWaitingForAStall()
    {
        TestAssert.True(new CanyonFireflyGuidanceModel().Advance(0f,
                Input(atImportantFork: true)).Visible,
            "an important reachable fork may prompt immediately");
        TestAssert.True(new CanyonFireflyGuidanceModel().Advance(0f,
                Input(bossExitAvailable: true)).Visible,
            "a newly available reachable Boss exit may prompt immediately");
        TestAssert.True(!new CanyonFireflyGuidanceModel().Advance(0f,
                Input(atImportantFork: true, routeProgressed: true)).Visible,
            "route progress wins over an immediate fork trigger in the same frame");
    }

    private static void GuidanceAndReachabilityFailClosed()
    {
        var model = new CanyonFireflyGuidanceModel();
        model.Advance(17f, Input());
        CanyonFireflyFrame disabled = model.Advance(30f, Input(guidanceEnabled: false));
        TestAssert.True(!disabled.Visible && disabled.VisibleSecondsRemaining == 0f &&
                        disabled.CooldownSecondsRemaining == 0f,
            "guidance-off clears every transient firefly timer");
        TestAssert.True(!model.Advance(17.999f, Input()).Visible,
            "reenabling guidance starts from a clean stall window");
        model.Reset();
        TestAssert.True(!model.Advance(60f, Input(hasReachableMarker: false)).Visible,
            "no firefly appears without a currently reachable marker");
    }

    private static CanyonFireflyInput Input(bool guidanceEnabled = true, bool paused = false,
        bool combatTelegraphActive = false, bool atImportantFork = false,
        bool bossExitAvailable = false, bool routeProgressed = false,
        bool hasReachableMarker = true) => new(guidanceEnabled, paused, combatTelegraphActive,
        atImportantFork, bossExitAvailable, routeProgressed, hasReachableMarker);
}
