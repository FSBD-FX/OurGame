using Godot;

namespace Knight.Presentation;

public readonly record struct PlayerGroundSupportObservation(bool Observed, ulong SupportId,
    Transform2D SupportTransform, Rect2 SupportBounds);

public readonly record struct PlayerGroundSample(bool Observed, Vector2 RootWorld,
    ulong SupportId, Transform2D SupportTransform, Vector2 GroundPoint, Vector2 GroundNormal)
{
    public bool HasBounds { get; init; }
    public Rect2 SupportBounds { get; init; }
    public ulong PhysicsTick { get; init; }
    public float PhysicsStepSeconds { get; init; }
    public int Facing { get; init; }
    public PlayerGroundSupportObservation PreviousSupport { get; init; }
}

public readonly record struct PlayerGroundGaitFrame(bool Observed, float Weight, float Phase,
    Vector2 NearSoleWorld, Vector2 FarSoleWorld, bool NearStance, bool FarStance)
{
    public Vector2 GroundUp { get; init; }
}

/// <summary>Presentation-only support-relative feet. Never owns native objects or physics.</summary>
public sealed class PlayerGroundGait
{
    private const float Reach=18f, Lift=8f, CycleDistance=72f;
    private bool _initialized, _seen, _swing, _settling;
    private int _swingFoot, _direction=1;
    private ulong _support;
    private ulong _lastPhysicsTick;
    private Transform2D _transform=Transform2D.Identity;
    private Rect2 _bounds;
    private Vector2 _root, _near, _far, _start, _end;
    private float _progress, _travel, _phase, _weight;
    private float _brakeAmount;
    private PlayerGroundGaitFrame _frame;
    private PlayerGroundSupportObservation _carriedSupport;

    public PlayerGroundGaitFrame Step(PlayerGroundSample sample, bool eligible, float delta, bool plantedImpact=false,
        float brakeStrength=0f)
    {
        if (!sample.Observed)
        { _initialized=_seen=false; _weight=0; _lastPhysicsTick=0; _brakeAmount=0; _carriedSupport=default; return _frame=default; }
        if (!float.IsFinite(delta) || delta<=0) return _frame;
        if(sample.PhysicsTick!=0)
        {
            if(sample.PhysicsTick==_lastPhysicsTick) return _frame;
            if(!float.IsFinite(sample.PhysicsStepSeconds) || sample.PhysicsStepSeconds<=0)
            { _initialized=false; _weight=0; return _frame=new(true,0,0,Vector2.Zero,Vector2.Zero,false,false); }
            ulong steps=_lastPhysicsTick!=0 && sample.PhysicsTick>_lastPhysicsTick ? sample.PhysicsTick-_lastPhysicsTick : 1;
            if(_lastPhysicsTick>sample.PhysicsTick) _initialized=false;
            delta=steps*sample.PhysicsStepSeconds;
            _lastPhysicsTick=sample.PhysicsTick;
        }
        if (!sample.RootWorld.IsFinite())
        {
            _initialized=false; _weight=0; _seen=true;
            return _frame=new(true,0,0,Vector2.Zero,Vector2.Zero,false,false) { GroundUp=Vector2.Up };
        }
        bool valid=eligible && sample.SupportId!=0 && Valid(sample.SupportTransform) &&
            sample.GroundPoint.IsFinite() && sample.GroundNormal.IsFinite() &&
            sample.GroundNormal.LengthSquared()>.25f && sample.GroundNormal.Normalized().Dot(Vector2.Up)>.5f &&
            sample.HasBounds && sample.SupportBounds.Position.IsFinite() && sample.SupportBounds.Size.IsFinite() &&
            sample.SupportBounds.Size.X>0 && sample.SupportBounds.Size.Y>0;
        if (!valid)
        {
            _carriedSupport=default;
            _brakeAmount=0;
            Vector2 carry=_seen ? sample.RootWorld-_root : Vector2.Zero;
            _root=sample.RootWorld; _initialized=false; _seen=true;
            _weight=Mathf.Max(0,_weight-delta/.08f);
            return _frame=new(true,_weight,_phase,_frame.NearSoleWorld+carry,
                _frame.FarSoleWorld+carry,false,false) { GroundUp=_frame.GroundUp };
        }
        Vector2 up=sample.GroundNormal.Normalized(), tangent=new(-up.Y,up.X);
        Transform2D inverse=sample.SupportTransform.AffineInverse();
        bool changed=_support!=sample.SupportId || _bounds!=sample.SupportBounds;
        bool handoff=_initialized && changed && CanContinueAcrossSeam(sample);
        if(_carriedSupport.Observed && !changed && AllFeetWithin(sample.SupportTransform,sample.SupportBounds))
            _carriedSupport=default;
        bool lostCarry=_carriedSupport.Observed && !handoff &&
            (sample.PreviousSupport!=_carriedSupport || !sample.SupportTransform.IsEqualApprox(_transform) ||
             !CoplanarConnected(_carriedSupport.SupportTransform,_carriedSupport.SupportBounds,
                 sample.SupportTransform,sample.SupportBounds));
        Vector2 previousRoot=handoff ? _root :
            _initialized ? sample.SupportTransform*(_transform.AffineInverse()*_root) : _root;
        bool reset=!_initialized || (changed && !handoff) || lostCarry ||
            previousRoot.DistanceTo(sample.RootWorld)>64f;
        if(handoff && !reset)
        {
            // Transfer the coordinate basis, not the displayed feet or swing clock.
            _near=inverse*World(_near); _far=inverse*World(_far);
            _start=inverse*World(_start); _end=inverse*World(_end);
            _carriedSupport=sample.PreviousSupport;
        }
        if (reset)
        {
            _carriedSupport=default;
            bool preserve=_seen && _frame.Weight>0 && _root.DistanceTo(sample.RootWorld)<=64f;
            float facing=sample.Facing<0?-1:1;
            Vector2 near=preserve ? _frame.NearSoleWorld : sample.RootWorld+tangent*(8*facing);
            Vector2 far=preserve ? _frame.FarSoleWorld : sample.RootWorld-tangent*(8*facing);
            _near=inverse*OnGround(near,sample,up,tangent);
            _far=inverse*OnGround(far,sample,up,tangent);
            if (!_seen) _weight=1;
            _swing=_settling=false; _phase=0; _initialized=true;
            previousRoot=sample.RootWorld;
        }
        _support=sample.SupportId; _transform=sample.SupportTransform; _bounds=sample.SupportBounds;
        float brakeTarget=float.IsFinite(brakeStrength)?Mathf.Clamp(brakeStrength,0,1):0;
        _brakeAmount=Mathf.MoveToward(_brakeAmount,brakeTarget,delta/.08f);
        float signed=(sample.RootWorld-previousRoot).Dot(tangent);
        float distance=Mathf.Abs(signed);
        if (distance>.0001f)
        {
            int direction=signed<0 ? -1 : 1;
            if (_swing && (_settling || direction!=_direction))
                BeginSwing(_swingFoot,previousRoot,direction,sample,up,tangent);
            _direction=direction;
            Vector2 cursor=previousRoot;
            float remaining=distance;
            // A bounded sample is <=64px. Split actual travel at lift/plant
            // boundaries; a low physics rate must not skip a stance event.
            for(int transitions=0;remaining>.0001f && transitions<16;transitions++)
            {
                if (!_swing)
                {
                    float near=(World(_near)-cursor).Dot(tangent)*direction;
                    float far=(World(_far)-cursor).Dot(tangent)*direction;
                    float until=Mathf.Max(0,Mathf.Min(near,far)+Reach);
                    if (remaining<=until) break;
                    cursor+=tangent*direction*until; remaining-=until;
                    BeginSwing(near<=far?0:1,cursor,direction,sample,up,tangent);
                }
                float advance=Mathf.Min(remaining,_travel*(1-_progress));
                cursor+=tangent*direction*advance; remaining-=advance;
                _progress=Mathf.Min(1,_progress+advance/_travel);
                MoveSwing(up,true);
                if (_progress>=1) { SetFoot(_swingFoot,_end); _swing=false; }
            }
            _phase=(_phase+distance/CycleDistance)%1f;
        }
        else if (_swing)
        {
            if (!_settling)
            {
                _start=_swingFoot==0?_near:_far;
                _end=inverse*OnGround(sample.RootWorld+tangent*_direction*Reach,sample,up,tangent);
                _settling=true; _progress=0;
            }
            _progress=Mathf.Min(1,_progress+delta/.12f);
            MoveSwing(up,false);
            if (_progress>=1) { SetFoot(_swingFoot,_end); _swing=false; }
        }
        _root=sample.RootWorld; _seen=true;
        // A measured contact has already proved an actual support plane. Plant
        // within this owner before body absorption; ordinary reentry still fades.
        _weight=plantedImpact ? 1f : Mathf.Min(1,_weight+delta/.08f);
        return _frame=new(true,_weight,_phase,World(_near),World(_far),
            _weight>=.999f && (!_swing||_swingFoot!=0),
            _weight>=.999f && (!_swing||_swingFoot!=1)) { GroundUp=up };
    }

    private bool CanContinueAcrossSeam(PlayerGroundSample sample)
    {
        var old=sample.PreviousSupport;
        return old.Observed && old.SupportId==_support && old.SupportId!=sample.SupportId &&
            old.SupportTransform==_transform && old.SupportBounds==_bounds &&
            _frame.GroundUp.IsEqualApprox(Vector2.Up) &&
            sample.GroundNormal.Normalized().IsEqualApprox(Vector2.Up) &&
            _root.DistanceTo(sample.RootWorld)<=64f &&
            CoplanarConnected(_transform,_bounds,sample.SupportTransform,sample.SupportBounds) &&
            AllFeetWithinUnion(_transform,_bounds,sample.SupportTransform,sample.SupportBounds);
    }

    private static bool FlatBounds(Transform2D transform,Rect2 bounds,out Rect2 world)
    {
        world=default;
        if(!Valid(transform) || !transform.X.IsEqualApprox(Vector2.Right) ||
            !transform.Y.IsEqualApprox(Vector2.Down) || !bounds.Position.IsFinite() ||
            !bounds.Size.IsFinite() || bounds.Size.X<=0 || bounds.Size.Y<=0) return false;
        world=new(transform*bounds.Position,bounds.Size);
        return world.Position.IsFinite() && world.End.IsFinite();
    }

    private static bool CoplanarConnected(Transform2D a,Rect2 aBounds,Transform2D b,Rect2 bBounds) =>
        FlatBounds(a,aBounds,out var left) && FlatBounds(b,bBounds,out var right) &&
        Mathf.Abs(left.Position.Y-right.Position.Y)<=.001f &&
        left.Position.X<=right.End.X+.001f && right.Position.X<=left.End.X+.001f;

    private bool AllFeetWithin(Transform2D transform,Rect2 bounds) =>
        FlatBounds(transform,bounds,out var world) && FeetWithin(world,world);

    private bool AllFeetWithinUnion(Transform2D a,Rect2 aBounds,Transform2D b,Rect2 bBounds) =>
        FlatBounds(a,aBounds,out var left) && FlatBounds(b,bBounds,out var right) && FeetWithin(left,right);

    private bool FeetWithin(Rect2 a,Rect2 b)
    {
        bool Contains(Vector2 local)
        {
            float x=World(local).X;
            return float.IsFinite(x) && ((x>=a.Position.X-.001f && x<=a.End.X+.001f) ||
                (x>=b.Position.X-.001f && x<=b.End.X+.001f));
        }
        return Contains(_near) && Contains(_far) && (!_swing || (Contains(_start) && Contains(_end)));
    }

    private Vector2 World(Vector2 local) => _transform*local;
    private void SetFoot(int foot,Vector2 local) { if(foot==0) _near=local; else _far=local; }
    private void BeginSwing(int foot,Vector2 root,int direction,PlayerGroundSample sample,Vector2 up,Vector2 tangent)
    {
        _swing=true; _settling=false; _swingFoot=foot; _progress=0;
        _start=foot==0?_near:_far;
        Vector2 other=World(foot==0?_far:_near);
        _travel=Mathf.Clamp(Reach+(other-root).Dot(tangent)*direction,4,Reach*2);
        _end=_transform.AffineInverse()*OnGround(root+tangent*direction*(_travel+Reach),sample,up,tangent);
    }
    private void MoveSwing(Vector2 up,bool lift)
    {
        float smooth=_progress*_progress*(3-2*_progress);
        Vector2 start=World(_start), end=World(_end);
        Vector2 world=start.Lerp(end,smooth);
        if(lift)
        {
            // A reversal or resumed settle can start above the plane. Keep
            // that height in the interpolation and spend only the unused lift.
            float carried=Mathf.Clamp((start-end).Dot(up),0,Lift);
            world+=up*(Mathf.Sin(_progress*Mathf.Pi)*(Lift-carried)*(1-.75f*_brakeAmount));
        }
        SetFoot(_swingFoot,_transform.AffineInverse()*world);
    }
    private static bool Valid(Transform2D transform)
    {
        float determinant=transform.X.Cross(transform.Y);
        return transform.X.IsFinite() && transform.Y.IsFinite() && transform.Origin.IsFinite() &&
            float.IsFinite(determinant) && Mathf.Abs(determinant)>.000001f;
    }
    private static Vector2 OnGround(Vector2 wanted,PlayerGroundSample sample,Vector2 up,Vector2 tangent)
    {
        Vector2 projected=wanted-up*(wanted-sample.GroundPoint).Dot(up);
        Rect2 bounds=sample.SupportBounds;
        Vector2 a=sample.SupportTransform*bounds.Position;
        Vector2 b=sample.SupportTransform*(bounds.Position+new Vector2(bounds.Size.X,0));
        Vector2 c=sample.SupportTransform*bounds.End;
        Vector2 d=sample.SupportTransform*(bounds.Position+new Vector2(0,bounds.Size.Y));
        float minimum=Mathf.Min(Mathf.Min(a.Dot(tangent),b.Dot(tangent)),Mathf.Min(c.Dot(tangent),d.Dot(tangent)));
        float maximum=Mathf.Max(Mathf.Max(a.Dot(tangent),b.Dot(tangent)),Mathf.Max(c.Dot(tangent),d.Dot(tangent)));
        float margin=Mathf.Min(2,(maximum-minimum)*.25f);
        return projected+tangent*(Mathf.Clamp(projected.Dot(tangent),minimum+margin,maximum-margin)-projected.Dot(tangent));
    }
}
