using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;

namespace Knight.Mechanisms;

// A non-solid hazard. The native capsule query is the exact union of this
// circle along the visited straight interval, not its rectangular AABB.
public partial class CanyonMovingGear : CanyonFixedGear
{
    [Export] public Vector2 TravelOffset { get; set; }=new(0,80);
    [Export] public float PeriodSeconds { get; set; }=3.2f;
    public MovingGearMotionFrame CurrentMotion => _motion?.Current ?? default;
    public bool HasValidMotionGeometry => _motionReady && _motion!=null && _queryComplete &&
        GeometryMatches(true) && TranslationOnly(GlobalTransform) &&
        GlobalPosition.DistanceSquaredTo(_originWorld+CurrentMotion.Offset)<.000001f &&
        Position.DistanceSquaredTo(_origin+CurrentMotion.Offset)<.000001f &&
        PhysicsServer2D.AreaGetTransform(GetRid()).IsEqualApprox(GlobalTransform);
    public override bool IsDangerTelegraphActive => _motionReady && _query!=null &&
        IsPhysicsProcessing() && HasValidMotionGeometry && base.IsDangerTelegraphActive;
    public Rect2 MotionWorldBounds
    {
        get
        {
            if(!HasValidMotionGeometry) return default;
            Vector2 travel=_motion.TravelOffset,extent=Vector2.One*DangerRadius;
            return new(_originWorld.Min(_originWorld+travel)-extent,
                new Vector2(Mathf.Abs(travel.X),Mathf.Abs(travel.Y))+extent*2);
        }
    }

    private MovingGearMotionModel _motion;
    private PhysicsShapeQueryParameters2D _query;
    private CircleShape2D _queryCircle;
    private CapsuleShape2D _queryCapsule;
    private Vector2 _origin,_originWorld;
    private bool _motionReady,_queryComplete;

    public override void _Ready()
    {
        _motionReady=false;
        var motion=new MovingGearMotionModel(TravelOffset,PeriodSeconds);
        if(!TranslationOnly(GlobalTransform))
            throw new InvalidOperationException("Moving gear requires a finite unit translation-only world basis.");
        base._Ready();
        try
        {
            _origin=Position; _originWorld=GlobalPosition; _motion=motion; _queryComplete=true;
            _queryCircle=new CircleShape2D { Radius=DangerRadius };
            _queryCapsule=new CapsuleShape2D { Radius=DangerRadius,Height=DangerRadius*2 };
            _query=new PhysicsShapeQueryParameters2D
            {
                Shape=_queryCircle,CollisionMask=CombatCollisionLayers.Player,
                CollideWithBodies=true,CollideWithAreas=false,Margin=0,Motion=Vector2.Zero
            };
            _motionReady=true;
            ForceUpdateTransform();
            if(!HasValidMotionGeometry)
                throw new InvalidOperationException("Moving gear authored circle and native Area transform must agree.");
            QueueRedraw();
        }
        catch
        {
            _motionReady=false; _motion=null; DisposeOwnedQueries();
            throw;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if(IsProcessingGearContacts || !double.IsFinite(delta) || delta<=0 ||
            !IsDangerTelegraphActive || GetTree().Paused || !CanProcess()) return;
        MovingGearMotionFrame frame=_motion.Step(delta);
        Position=_origin+frame.Offset;
        ForceUpdateTransform(); // Commit this Area's pose before querying/painting the matching native circle.
        if(!IsDangerTelegraphActive) return;
        Vector2 from=_originWorld+frame.SweepFromOffset,to=_originWorld+frame.SweepToOffset;
        Vector2 vector=to-from;
        float length=vector.Length();
        if(length<=.0001f)
        {
            _query.Shape=_queryCircle;
            _query.Transform=new Transform2D(0,from);
        }
        else
        {
            _queryCapsule.Height=length+DangerRadius*2;
            _query.Shape=_queryCapsule;
            Vector2 center=new((float)(((double)from.X+to.X)*.5d),(float)(((double)from.Y+to.Y)*.5d));
            _query.Transform=new Transform2D(vector.Angle()-Mathf.Pi*.5f,center);
        }
        using Godot.Collections.Array hits=(Godot.Collections.Array)GetWorld2D().DirectSpaceState.IntersectShape(_query,256);
        if(hits.Count>=256)
        {
            _queryComplete=false;
            throw new InvalidOperationException("Moving gear native contact query was truncated; damage authorization withdrawn.");
        }
        var bodies=new List<Node2D>();
        var seen=new HashSet<ulong>();
        for(int index=0;index<hits.Count;index++)
        {
            using Godot.Collections.Dictionary hit=hits[index].AsGodotDictionary();
            if(hit["collider"].AsGodotObject() is Node2D body && IsInstanceValid(body) &&
                seen.Add(body.GetInstanceId())) bodies.Add(body);
        }
        ProcessGearContacts(bodies,delta);
    }

    public override void ResetForCheckpoint()
    {
        base.ResetForCheckpoint(); // Revoke the old delivery before the reset teleport.
        if(!_motionReady || _motion==null) return;
        _motion.Reset(); Position=_origin; ForceUpdateTransform(); QueueRedraw();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _motionReady=false;
        if(_motion!=null) Position=_origin;
        _motion=null; DisposeOwnedQueries();
        RequestReady();
    }

    private void DisposeOwnedQueries()
    {
        _query?.Dispose(); _query=null;
        _queryCapsule?.Dispose(); _queryCapsule=null;
        _queryCircle?.Dispose(); _queryCircle=null;
    }

    public override void _Draw()
    {
        if(!HasValidMotionGeometry) return;
        Vector2 from=-CurrentMotion.Offset,to=_motion.TravelOffset-CurrentMotion.Offset;
        DrawLine(from,to,new Color("152832"),7,true);
        DrawLine(from,to,new Color("6d827c"),2,true);
        DrawCircle(from,5,new Color("d4aa58")); DrawCircle(to,5,new Color("d4aa58"));
        base._Draw();
    }

    private static bool TranslationOnly(Transform2D transform)=>
        transform.Origin.IsFinite() && transform.X.IsFinite() && transform.Y.IsFinite() &&
        transform.X.DistanceSquaredTo(Vector2.Right)<.00000001f &&
        transform.Y.DistanceSquaredTo(Vector2.Down)<.00000001f;
}
