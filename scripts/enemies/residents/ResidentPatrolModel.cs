using System;
using System.Linq;
using Godot;
using Knight.Prologue.Residents;

namespace Knight.Enemies.Residents;

public enum ResidentPatrolState { Patrolling, Engaged, Returning, Suppressed, Dormant, Dead }
public readonly record struct ResidentPatrolFrame(ResidentPatrolState State, float MoveDirection, bool CombatAllowed);
public readonly record struct ResidentPatrolInput(Vector2 Position, Rect2 BodyBounds,
    bool TargetPermitted, bool Supported, bool LeftSafe, bool RightSafe,
    bool Suppressed = false, bool Dormant = false, bool Dead = false);

// Supplies autonomous intent, never an actor/world position write.
public sealed class ResidentPatrolModel
{
    private readonly ResidentPlacementCandidate _placement;
    private readonly ResidentPatrolSettings _settings;
    private int _point;
    private bool _returning;

    public ResidentPatrolModel(ResidentPlacementCandidate placement, ResidentPatrolSettings settings = null)
    {
        _settings=settings??new();
        if(placement==null) throw new ArgumentNullException(nameof(placement));
        if(_settings.Validate().Count!=0 || !Finite(placement.WorldSpawn) ||
           !Finite(placement.Territory.Position) || !Finite(placement.Territory.Size) ||
           placement.Territory.Size.X<=0 || placement.Territory.Size.Y<=0 ||
           !Contains(placement.Territory,new Rect2(placement.WorldSpawn,Vector2.Zero)) ||
           placement.PatrolPoints.Count<2 || placement.PatrolPoints.Distinct().Count()<2 ||
           placement.PatrolPoints.Any(p=>!Finite(p)||MathF.Abs(p.Y-placement.WorldSpawn.Y)>.001f ||
               !Contains(placement.Territory,new Rect2(p,Vector2.Zero))) ||
           placement.PatrolPoints.Max(p=>p.X)-placement.PatrolPoints.Min(p=>p.X)<=_settings.ArrivalTolerance*2)
            throw new ArgumentException("Invalid horizontal resident patrol.",nameof(placement));
        _placement=placement;
    }

    public ResidentPatrolFrame Step(ResidentPatrolInput input)
    {
        if(!Finite(input.Position)||!Finite(input.BodyBounds.Position)||!Finite(input.BodyBounds.Size)||
           input.BodyBounds.Size.X<=0||input.BodyBounds.Size.Y<=0)
            throw new ArgumentException("Invalid resident body observation.",nameof(input));
        if(input.Dead) return new(ResidentPatrolState.Dead,0,false);
        if(input.Dormant) return new(ResidentPatrolState.Dormant,0,false);
        if(!Contains(_placement.Territory,input.BodyBounds)) _returning=true;
        if(input.Suppressed) return new(ResidentPatrolState.Suppressed,0,false);
        if(_returning)
        {
            float gap=_placement.WorldSpawn.X-input.Position.X;
            if(MathF.Abs(gap)<=_settings.ArrivalTolerance && Contains(_placement.Territory,input.BodyBounds)) _returning=false;
            else return new(ResidentPatrolState.Returning,SafeDirection(MathF.Sign(gap)),false);
        }
        if(input.TargetPermitted&&input.Supported) return new(ResidentPatrolState.Engaged,0,true);
        if(!input.Supported) return new(ResidentPatrolState.Patrolling,0,false);
        float direction=MathF.Sign(_placement.PatrolPoints[_point].X-input.Position.X);
        if(MathF.Abs(_placement.PatrolPoints[_point].X-input.Position.X)<=_settings.ArrivalTolerance || SafeDirection(direction)==0)
        {
            _point=(_point+1)%_placement.PatrolPoints.Count;
            direction=MathF.Sign(_placement.PatrolPoints[_point].X-input.Position.X);
        }
        return new(ResidentPatrolState.Patrolling,SafeDirection(direction),false);

        float SafeDirection(float value) => !input.Supported ? 0 : value<0 ? input.LeftSafe?-1:0 : value>0 ? input.RightSafe?1:0 : 0;
    }

    internal static bool Contains(Rect2 territory,Rect2 body) => body.Position.X>=territory.Position.X &&
        body.Position.Y>=territory.Position.Y && body.End.X<=territory.End.X && body.End.Y<=territory.End.Y;
    private static bool Finite(Vector2 value)=>float.IsFinite(value.X)&&float.IsFinite(value.Y);
}
