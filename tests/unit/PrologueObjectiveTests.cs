using System;
using Knight.Prologue;

namespace Knight.Tests.Unit;

public static class PrologueObjectiveTests
{
    private static readonly PrologueObjective[] ApprovedSequence =
    [
        PrologueObjective.Wake,
        PrologueObjective.LearnAttackMode,
        PrologueObjective.LearnGrappleMode,
        PrologueObjective.ReachBasin,
        PrologueObjective.LearnShield,
        PrologueObjective.ClimbCanyon,
        PrologueObjective.EnterTemple,
        PrologueObjective.PullTempleMechanism,
        PrologueObjective.DefeatBellkeeper,
        PrologueObjective.Complete
    ];

    private static readonly PrologueProgressEvent[] ApprovedEvents =
    [
        PrologueProgressEvent.Woke,
        PrologueProgressEvent.AttackModeLearned,
        PrologueProgressEvent.GrappleModeLearned,
        PrologueProgressEvent.BasinReached,
        PrologueProgressEvent.ShieldLearned,
        PrologueProgressEvent.CanyonClimbed,
        PrologueProgressEvent.TempleEntered,
        PrologueProgressEvent.TempleMechanismPulled,
        PrologueProgressEvent.BossDefeated
    ];

    public static void Run()
    {
        ExactContinuousSequenceAdvancesOneObservationAtATime();
        SameFrameEventBurstsCannotSkipObjectives();
        CompleteIsTerminalAndTextIsCentralized();
    }

    private static void ExactContinuousSequenceAdvancesOneObservationAtATime()
    {
        var model = new PrologueObjectiveModel();
        TestAssert.Equal(ApprovedSequence[0], model.Current,
            "continuous objective sequence starts at Wake");

        for (int index = 0; index < ApprovedEvents.Length; index++)
        {
            TestAssert.True(model.Observe(ApprovedEvents[index]),
                $"event {ApprovedEvents[index]} advances objective {ApprovedSequence[index]}");
            TestAssert.Equal(ApprovedSequence[index + 1], model.Current,
                $"objective edge {index} lands on the exact approved next objective");
        }
    }

    private static void SameFrameEventBurstsCannotSkipObjectives()
    {
        var model = new PrologueObjectiveModel();
        TestAssert.True(!model.Observe(PrologueProgressEvent.BasinReached),
            "a future sector observation cannot skip Wake");
        TestAssert.Equal(PrologueObjective.Wake, model.Current,
            "rejected future event leaves the current objective unchanged");

        TestAssert.True(model.Observe(PrologueProgressEvent.Woke),
            "the expected observation advances exactly once");
        TestAssert.True(!model.Observe(PrologueProgressEvent.GrappleModeLearned),
            "a second event from the same frame cannot skip LearnAttackMode");
        TestAssert.Equal(PrologueObjective.LearnAttackMode, model.Current,
            "one observation edge advances at most one objective");
    }

    private static void CompleteIsTerminalAndTextIsCentralized()
    {
        var model = new PrologueObjectiveModel();
        foreach (PrologueProgressEvent progressEvent in ApprovedEvents)
            TestAssert.True(model.Observe(progressEvent),
                $"approved sequence accepts {progressEvent}");

        TestAssert.Equal(PrologueObjective.Complete, model.Current,
            "approved continuous sequence reaches Complete");
        foreach (PrologueProgressEvent progressEvent in Enum.GetValues<PrologueProgressEvent>())
            TestAssert.True(!model.Observe(progressEvent),
                $"Complete ignores later {progressEvent}");
        foreach (PrologueObjective objective in Enum.GetValues<PrologueObjective>())
            TestAssert.True(!string.IsNullOrWhiteSpace(PrologueObjectiveModel.TextFor(objective)),
                $"{objective} has centralized visible objective text");
    }
}
