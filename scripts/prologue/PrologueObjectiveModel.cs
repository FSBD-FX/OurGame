using System.Collections.Generic;

namespace Knight.Prologue;

public sealed class PrologueObjectiveModel
{
    private static readonly IReadOnlyDictionary<PrologueObjective, string> Text =
        new Dictionary<PrologueObjective, string>
        {
            [PrologueObjective.Wake] = "WAKE IN THE CANYON",
            [PrologueObjective.LearnAttackMode] = "STRIKE WITH YOUR WEAPON",
            [PrologueObjective.LearnGrappleMode] = "GRAB A VALID SURFACE",
            [PrologueObjective.ReachBasin] = "REACH THE CANYON BASIN",
            [PrologueObjective.LearnShield] = "LEARN THE ENERGY SHIELD",
            [PrologueObjective.ClimbCanyon] = "CLIMB THE CANYON",
            [PrologueObjective.EnterTemple] = "ENTER THE LOST TEMPLE",
            [PrologueObjective.PullTempleMechanism] = "PULL THE TEMPLE MECHANISM",
            [PrologueObjective.DefeatBellkeeper] = "DEFEAT THE FALLEN BELLKEEPER",
            [PrologueObjective.Complete] = "PROLOGUE COMPLETE — THE LAST GRAVEKEEPER REMEMBERS"
        };

    public PrologueObjective Current { get; private set; } = PrologueObjective.Wake;

    public void Reset() => Current = PrologueObjective.Wake;

    public bool Observe(PrologueProgressEvent progressEvent)
    {
        PrologueProgressEvent expected = Current switch
        {
            PrologueObjective.Wake => PrologueProgressEvent.Woke,
            PrologueObjective.LearnAttackMode => PrologueProgressEvent.AttackModeLearned,
            PrologueObjective.LearnGrappleMode => PrologueProgressEvent.GrappleModeLearned,
            PrologueObjective.ReachBasin => PrologueProgressEvent.BasinReached,
            PrologueObjective.LearnShield => PrologueProgressEvent.ShieldLearned,
            PrologueObjective.ClimbCanyon => PrologueProgressEvent.CanyonClimbed,
            PrologueObjective.EnterTemple => PrologueProgressEvent.TempleEntered,
            PrologueObjective.PullTempleMechanism => PrologueProgressEvent.TempleMechanismPulled,
            PrologueObjective.DefeatBellkeeper => PrologueProgressEvent.BossDefeated,
            _ => (PrologueProgressEvent)(-1)
        };
        if (progressEvent != expected) return false;
        Current = Current switch
        {
            PrologueObjective.Wake => PrologueObjective.LearnAttackMode,
            PrologueObjective.LearnAttackMode => PrologueObjective.LearnGrappleMode,
            PrologueObjective.LearnGrappleMode => PrologueObjective.ReachBasin,
            PrologueObjective.ReachBasin => PrologueObjective.LearnShield,
            PrologueObjective.LearnShield => PrologueObjective.ClimbCanyon,
            PrologueObjective.ClimbCanyon => PrologueObjective.EnterTemple,
            PrologueObjective.EnterTemple => PrologueObjective.PullTempleMechanism,
            PrologueObjective.PullTempleMechanism => PrologueObjective.DefeatBellkeeper,
            PrologueObjective.DefeatBellkeeper => PrologueObjective.Complete,
            _ => Current
        };
        return true;
    }

    public static string TextFor(PrologueObjective objective) => Text[objective];
}
