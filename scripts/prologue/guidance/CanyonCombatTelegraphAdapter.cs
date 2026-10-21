using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Enemies.Ranged;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;

namespace Knight.Prologue.Guidance;

public static class CanyonCombatTelegraphAdapter
{
    public static bool IsTelegraphActive(Node source)
    {
        if (!GodotObject.IsInstanceValid(source) || !source.IsInsideTree() ||
            source.IsQueuedForDeletion()) return false;
        for (Node ancestor = source.GetParent(); ancestor != null; ancestor = ancestor.GetParent())
            if (ancestor.IsQueuedForDeletion()) return false;
        return source switch
        {
            RustShellSquire rust => rust.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph,
            BellBackLaborer laborer => laborer.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph,
            GraveLampMoth moth => moth.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph,
            BellBoltSentry sentry => sentry.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph,
            CanyonSkitter skitter => skitter.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph,
            RangedWeaponEnemy ranged => ranged.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph,
            FallenBellkeeper boss => boss.CurrentFrame.AttackTimeline.Phase == AttackPhase.Telegraph,
            _ => false
        };
    }
}
