using System;
using Godot;
using Knight.Abilities;
using Knight.Mechanisms;
using Knight.Prologue.Residents;

namespace Knight.Enemies;

// Development carrier only: this does not make the saved Rust scene a formal
// Ghost identity. Phase permission is independent of render opacity and sleep.
public partial class RustShellSquire : IThreadInteractionReceiver
{
    private ResidentCombatProfile _residentCombatProfile;
    private ResidentFamily _residentCombatFamily;
    private bool _boneThreadOpening;
    private bool _ghostManifest;
    private float _ghostRemaining;
    private Node2D _ghostVisual;
    private Color _ghostBaseColor;
    private bool HasGhostProfile=>_residentCombatFamily==ResidentFamily.Ghost&&_residentCombatProfile!=null;
    private bool HasBoneProfile=>_residentCombatFamily==ResidentFamily.Bone&&
        _residentCombatProfile?.BoneWeakness==ResidentBoneWeakness.RecoveryOrThreadStunned;
    private bool HasFamilyCombatProfile=>HasGhostProfile||HasBoneProfile;
    private bool GhostPhaseAllowsInteraction=>!HasGhostProfile||_ghostManifest;
    private bool BonePoseAllowsDamage=>!HasBoneProfile||CurrentState==RustShellState.Recover||
        CurrentState==RustShellState.Stunned&&_boneThreadOpening;

    private static void ValidateWandererCombatProfile(ResidentPopulationDefinition definition)
    {
        ResidentCombatProfile profile=definition.CombatProfile;
        if(definition.Family==ResidentFamily.Ghost)
        {
            if(profile==null||profile.BoneWeakness!=ResidentBoneWeakness.None||
                !ValidDuration(profile.GhostHiddenSeconds)||!ValidDuration(profile.GhostManifestSeconds))
                throw new ArgumentException("Ghost wanderer needs a finite, exclusive manifestation profile.",nameof(definition));
        }
        else if(definition.Family==ResidentFamily.Bone)
        {
            if(profile==null||profile.BoneWeakness!=ResidentBoneWeakness.RecoveryOrThreadStunned||
                profile.GhostHiddenSeconds!=0||profile.GhostManifestSeconds!=0)
                throw new ArgumentException("Bone wanderer needs an exclusive recovery/thread-stun weakness profile.",nameof(definition));
        }
        else if(profile!=null)throw new ArgumentException("This wanderer cannot silently ignore another family's combat profile.",nameof(definition));
        static bool ValidDuration(float value)=>float.IsFinite(value)&&value>=1f/60&&value<=60;
    }
    private void InitializeGhostResidence()
    {
        if(!HasGhostProfile)return;
        _ghostVisual=GetNodeOrNull<Node2D>("VisualRoot");
        if(IsInstanceValid(_ghostVisual))_ghostBaseColor=_ghostVisual.Modulate;
        ResetGhostPhase();
    }
    private void ResetGhostPhase()
    {
        if(!HasGhostProfile)return;
        _ghostManifest=false;_ghostRemaining=_residentCombatProfile.GhostHiddenSeconds;
        ApplyResidentDormancy();PresentGhostPhase();
    }
    private void StepGhostPhase(double delta)
    {
        if(!HasGhostProfile||_residentDormant||_residentVoidPending||_health.CurrentHealth<=0||!double.IsFinite(delta)||delta<=0)return;
        // The ordinary engine tick is small. Modulo bounds even a long hitch to
        // at most two phase crossings, without a potentially unbounded loop.
        float cycle=_residentCombatProfile.GhostHiddenSeconds+_residentCombatProfile.GhostManifestSeconds;
        _ghostRemaining-=(float)(delta%cycle);bool changed=false;
        while(_ghostRemaining<=0)
        {
            _ghostManifest=!_ghostManifest;
            _ghostRemaining+=_ghostManifest?_residentCombatProfile.GhostManifestSeconds:_residentCombatProfile.GhostHiddenSeconds;
            changed=true;
            if(!_ghostManifest){_stunRequested=true;_residentPermitted=false;_attackHitbox.End();PublishAttack(default);}
        }
        if(changed){ApplyResidentDormancy();PresentGhostPhase();}
    }
    private void PresentGhostPhase()
    {
        if(IsInstanceValid(_ghostVisual))_ghostVisual.Modulate=new Color(_ghostBaseColor.R,_ghostBaseColor.G,_ghostBaseColor.B,
            _ghostBaseColor.A*(_ghostManifest?1:.3f));
    }
    private void CommitResidentHurtMonitorable()
    {
        if(!IsInstanceValid(_hurtbox)||!IsInsideTree()||IsQueuedForDeletion()||!IsInstanceValid(_health))return;
        _hurtbox.Monitorable=!_residentDormant&&!_residentVoidPending&&_health.CurrentHealth>0&&_originalHurtMonitorable;
    }
    public bool TryReceiveThread(ThreadInteraction interaction)
    {
        if(!IsInstanceValid(_health)||_health.CurrentHealth<=0||!GhostPhaseAllowsInteraction||
            _residentModel!=null&&(_residentDormant||_residentVoidPending)||
            !IsInstanceValid(interaction.Player)||!interaction.Player.IsInsideTree()||!IsInsideTree()||IsQueuedForDeletion()||
            interaction.Player.GetWorld2D()!=GetWorld2D()||interaction.Settings==null||!float.IsFinite(interaction.Settings.EnemyPullSpeed)||interaction.Settings.EnemyPullSpeed<=0)return false;
        Vector2 direction=GlobalPosition.DirectionTo(interaction.Player.GlobalPosition);
        if(!float.IsFinite(direction.X)||!float.IsFinite(direction.Y))return false;
        ApplyThreadPull(direction*interaction.Settings.EnemyPullSpeed);
        return true;
    }
}
