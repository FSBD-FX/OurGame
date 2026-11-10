using System;
using Godot;

namespace Knight.Presentation;

public readonly record struct PlayerJumpObservation(ulong OwnerId,ulong Sequence,bool Observed);
public readonly record struct PlayerOrdinaryAirInput(PlayerJumpObservation Jump,PlayerGroundSample Motion,
    Vector2 Velocity,int Facing,bool Grounded,bool Suppressed,bool Replaced)
{
    public bool SupportedJetCoast { get; init; }
}
public readonly record struct PlayerOrdinaryAirFrame(bool Active,bool JumpStarted,bool JumpArc,float EntryWeight,
    float TorsoLeanRadians,float PelvisOffset,float NearLift,float FarLift,float FootTrail);

// Observes accepted causes and completed physical samples, never moves the body.
public sealed class PlayerOrdinaryAirMotion
{
    private ulong _owner,_sequence,_tick;
    private bool _hasMotion,_active,_arc,_hasAirTarget;
    private Vector2 _root;
    private double _airElapsed;
    private float _leanWorld,_pelvis,_near,_far,_trailWorld;
    private float _targetLean,_targetPelvis,_targetNear,_targetFar,_targetTrail;

    public PlayerOrdinaryAirFrame Step(PlayerOrdinaryAirInput input,float deltaSeconds)
    {
        int facing=input.Facing<0?-1:1;
        var cause=input.Jump;
        if(!cause.Observed || cause.OwnerId==0)return Clear();
        bool newOwner=_owner!=cause.OwnerId;
        if(newOwner)
        {
            Clear();_owner=cause.OwnerId;_sequence=cause.Sequence;
        }
        var motion=input.Motion;
        bool validMotion=motion.Observed && motion.PhysicsTick!=0 && motion.RootWorld.IsFinite() &&
            input.Velocity.IsFinite() && float.IsFinite(motion.PhysicsStepSeconds) && motion.PhysicsStepSeconds>0;
        bool positive=float.IsFinite(deltaSeconds) && deltaSeconds>0;
        if(input.Suppressed || input.Replaced)
        {
            bool supportedCoast=!input.Suppressed && input.Grounded && input.SupportedJetCoast &&
                validMotion && cause.Sequence>=_sequence;
            // A zero-time presentation can repeat the cached physical sample, not replace it.
            bool keepSupportedBaseline=supportedCoast && (positive ||
                (_hasMotion && motion.PhysicsTick==_tick && motion.RootWorld==_root));
            _sequence=Math.Max(_sequence,cause.Sequence);
            if(supportedCoast && positive)Prime(input);
            return Clear(forgetMotion:!keepSupportedBaseline);
        }
        if(cause.Sequence<_sequence)return input.Grounded?Clear():Publish(facing);
        if(!validMotion)
        {
            _sequence=Math.Max(_sequence,cause.Sequence);
            return Clear();
        }
        if(input.Grounded)
        {
            // Grounding revokes pending launch authority even when render time
            // or the kinematic sample cannot advance. Airborne zero-time still defers.
            _sequence=Math.Max(_sequence,cause.Sequence);
            if(!_hasMotion || positive && motion.PhysicsTick!=_tick)Prime(input);
            return Clear(forgetMotion:false);
        }
        if(!positive)
        {
            if(!_hasMotion)Prime(input);
            return Publish(facing);
        }
        bool started=false;
        if(!_hasMotion || motion.PhysicsTick<_tick || motion.PhysicsTick-_tick>8 ||
            motion.RootWorld.DistanceSquaredTo(_root)>4096)
        {
            Prime(input);_arc=false;ObserveTargets(input.Velocity);
        }
        else if(motion.PhysicsTick!=_tick)
        {
            started=cause.Sequence>_sequence;
            _sequence=cause.Sequence;_tick=motion.PhysicsTick;_root=motion.RootWorld;
            if(started)_arc=true;
            ObserveTargets(input.Velocity);
        }
        if(!_hasAirTarget)return Publish(facing);
        _airElapsed+=deltaSeconds;
        float weight=(float)(1d-Math.Exp(-(double)deltaSeconds/.07d));
        _leanWorld=Mathf.Lerp(_leanWorld,_targetLean,weight);_pelvis=Mathf.Lerp(_pelvis,_targetPelvis,weight);
        _near=Mathf.Lerp(_near,_targetNear,weight);_far=Mathf.Lerp(_far,_targetFar,weight);
        _trailWorld=Mathf.Lerp(_trailWorld,_targetTrail,weight);_active=true;
        return Publish(facing,started);
    }

    private void ObserveTargets(Vector2 velocity)
    {
        double up=Math.Clamp(-(double)velocity.Y/600d,0d,1d);
        double down=Math.Clamp((double)velocity.Y/850d,0d,1d);
        double horizontal=Math.Clamp((double)velocity.X/250d,-1d,1d);
        _targetNear=(float)(_arc?4d+8d*up-6d*down:-2d*down);
        _targetFar=(float)(_arc?5d+5d*up-6d*down:-down);
        _targetLean=(float)(horizontal*(_arc?.02d+.03d*up:.02d));
        _targetPelvis=(float)(_arc?.6d*up-.3d*down:-.3d*down);
        _targetTrail=(float)(-3d*horizontal);_hasAirTarget=true;
    }
    private void Prime(PlayerOrdinaryAirInput input)
    {
        _sequence=Math.Max(_sequence,input.Jump.Sequence);_tick=input.Motion.PhysicsTick;
        _root=input.Motion.RootWorld;_hasMotion=true;
    }
    private PlayerOrdinaryAirFrame Publish(int facing,bool started=false)
    {
        double t=Math.Clamp(_airElapsed/.08d,0d,1d);
        return new(_active,started,_arc,(float)(t*t*(3d-2d*t)),_leanWorld*facing,
            _pelvis,_near,_far,_trailWorld*facing);
    }
    private PlayerOrdinaryAirFrame Clear(bool forgetMotion=true)
    {
        if(forgetMotion)_hasMotion=false;
        _active=_arc=_hasAirTarget=false;_airElapsed=0;
        _leanWorld=_pelvis=_near=_far=_trailWorld=0;
        return default;
    }
}
