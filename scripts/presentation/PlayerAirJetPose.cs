using System;
using Godot;

namespace Knight.Presentation;

public readonly record struct PlayerAirJetInput(ulong OwnerId, Vector2 Velocity, PlayerGroundSample Motion,
    int Facing, bool Grounded, bool Suppressed, ThrusterPresentationFrame Mobility);

public readonly record struct PlayerAirJetFrame(bool Active, bool ForceObserved,
    Vector2 NormalizedAccelerationWorld, float TorsoLeanRadians, float PelvisLag,
    float NearFold, float FarFold, float FootTrail);

// Observes accepted thrust and stamped motion; never owns or changes physical state.
public sealed class PlayerAirJetPoseModel
{
    private ulong _owner, _life, _epoch, _burn, _tick;
    private bool _retired, _hasMotion, _hasForce, _active;
    private Vector2 _velocity, _root, _acceleration, _direction;
    private ThrusterPresentationFrame _sampledSource;
    private float _leanWorld, _lag, _near, _far, _trailWorld;

    public PlayerAirJetFrame Step(PlayerAirJetInput input, float deltaSeconds)
    {
        int facing=input.Facing<0?-1:1;
        if(input.OwnerId==0)
        {
            _retired|=_burn!=0;
            return Clear();
        }
        if(_owner!=input.OwnerId)
        {
            _owner=input.OwnerId; _life=_epoch=_burn=0; _retired=false;
            Clear();
        }
        if(input.Suppressed)
        {
            _retired|=_burn!=0;
            return Clear();
        }
        if(input.Grounded) Clear();

        var source=input.Mobility;
        var id=source.Identity;
        double directionLength=(double)source.Direction.X*source.Direction.X+
            (double)source.Direction.Y*source.Direction.Y;
        bool valid=id.OwnerId==_owner && id.Lifecycle!=0 && id.ConfigurationEpoch!=0 && id.BurnId!=0 &&
            source.Direction.IsFinite() && Math.Abs(directionLength-1d)<.001d && source.Burning!=source.Coasting &&
            float.IsFinite(source.CoastRemaining) && (source.Coasting
                ? source.CoastRemaining>0 && source.CoastRemaining<=.4501f : source.CoastRemaining==0);
        if(!valid)
        {
            _retired|=_burn!=0;
            return Clear();
        }
        // Out-of-order observations cannot retire or resample the newer lease.
        if(id.Lifecycle<_life || id.ConfigurationEpoch<_epoch ||
            id.Lifecycle==_life && id.ConfigurationEpoch==_epoch && id.BurnId<_burn)
            return Publish(facing);
        if(id.Lifecycle!=_life || id.ConfigurationEpoch!=_epoch)
        {
            _life=id.Lifecycle; _epoch=id.ConfigurationEpoch; _burn=0; _retired=false;
            Clear();
        }
        if(id.BurnId==_burn && _retired) return Clear();
        if(id.BurnId>_burn) { _burn=id.BurnId; _retired=false; _direction=source.Direction; _sampledSource=source; }
        else if(source.Direction!=_direction)
        {
            _retired=true;
            return Clear();
        }
        if(input.Grounded) return Clear();

        var motion=input.Motion;
        if(!motion.Observed || motion.PhysicsTick==0 || !motion.RootWorld.IsFinite() ||
            !input.Velocity.IsFinite() || !float.IsFinite(motion.PhysicsStepSeconds) || motion.PhysicsStepSeconds<=0)
            return Clear(); // Missing measurement is not proof that the live pulse retired.
        bool primed=!_hasMotion;
        if(primed) Prime(input);
        if(!float.IsFinite(deltaSeconds) || deltaSeconds<=0) return Publish(facing);
        if(primed || motion.PhysicsTick>_tick) ObservePhase(source);
        if(motion.PhysicsTick!=_tick)
        {
            if(motion.PhysicsTick<_tick || motion.PhysicsTick-_tick>8 ||
                motion.RootWorld.DistanceSquaredTo(_root)>4096)
                Prime(input);
            else
            {
                double elapsed=(motion.PhysicsTick-_tick)*(double)motion.PhysicsStepSeconds;
                _acceleration=new((float)Math.Clamp(((double)input.Velocity.X-_velocity.X)/elapsed/4500d,-1d,1d),
                    (float)Math.Clamp(((double)input.Velocity.Y-_velocity.Y)/elapsed/4500d,-1d,1d));
                _hasForce=true; _tick=motion.PhysicsTick; _velocity=input.Velocity; _root=motion.RootWorld;
            }
        }

        var measuredSource=_sampledSource;
        double energy=measuredSource.Burning?1d:Math.Pow(Math.Clamp(measuredSource.CoastRemaining/.45d,0d,1d),2);
        double horizontal=measuredSource.Braking?-.10d*Math.Sign(_velocity.X):
            (measuredSource.Burning?.10d:.06d)*_direction.X;
        float lean=(float)Math.Clamp((horizontal+.035d*_acceleration.X)*energy,-.16d,.16d);
        float lag=(float)Math.Clamp(-.8d*_acceleration.Y*energy,-.8d,.8d);
        double up=Math.Max(0d,-_direction.Y), side=Math.Abs((double)_direction.X);
        float near=(float)Math.Min(11d,(2d+8d*up+3d*side)*energy);
        float far=(float)Math.Min(10d,(1d+7d*up+5d*side)*energy);
        float trail=(float)(-4d*_direction.X*energy);
        double response=measuredSource.Braking?.045d:measuredSource.Burning?.06d:.035d;
        float weight=(float)(1d-Math.Exp(-(double)deltaSeconds/response));
        _leanWorld=Mathf.Lerp(_leanWorld,lean,weight); _lag=Mathf.Lerp(_lag,lag,weight);
        _near=Mathf.Lerp(_near,near,weight); _far=Mathf.Lerp(_far,far,weight);
        _trailWorld=Mathf.Lerp(_trailWorld,trail,weight); _active=true;
        return Publish(facing);
    }

    private void ObservePhase(ThrusterPresentationFrame source)
    {
        // A duplicate physical sample can only ease its cached target. Once a
        // lease coasts, stale burn/longer-tail frames cannot re-energize it.
        if(_sampledSource.Coasting && (!source.Coasting || source.CoastRemaining>_sampledSource.CoastRemaining)) return;
        _sampledSource=source;
    }

    private void Prime(PlayerAirJetInput input)
    {
        _hasMotion=true; _tick=input.Motion.PhysicsTick; _velocity=input.Velocity; _root=input.Motion.RootWorld;
        _hasForce=false; _acceleration=Vector2.Zero;
    }
    private PlayerAirJetFrame Publish(int facing)=>new(_active,_hasForce,_acceleration,
        _leanWorld*facing,_lag,_near,_far,_trailWorld*facing);
    private PlayerAirJetFrame Clear()
    {
        _hasMotion=_hasForce=_active=false; _tick=0; _acceleration=Vector2.Zero;
        _leanWorld=_lag=_near=_far=_trailWorld=0;
        return default;
    }
}
