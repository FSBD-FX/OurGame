using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

/// <summary>Current solid faces and original advance warnings; never controls mechanism state.</summary>
public partial class FogMovingGeometryReadabilityOverlay : Node2D
{
    [Export] public float BodyCueRadius { get; set; }=620f;
    [Export] public Color BoundaryColor { get; set; }=new("cbbd93");
    public int PresentedBodyCount { get; private set; }
    public int PresentedSegmentCount { get; private set; }
    public int PresentedWarningCount { get; private set; }
    public int PresentedWarningSegmentCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    private readonly HashSet<ulong> _seen=new();

    public override void _Ready() { ProcessMode=ProcessModeEnum.Always; Clear(); }
    public override void _Process(double delta)=>Clear();
    public void PresentBodies(IReadOnlyList<AnimatableBody2D> sources,Vector2 actor,HeadlampOcclusionFrame frame)
    {
        Clear();
        if (!Live(this) || GetTree().Paused || sources==null || frame is not { IsComplete:true } ||
            !HeadlampOcclusionSampler.Finite(actor) || !float.IsFinite(BodyCueRadius) || BodyCueRadius<=0f ||
            !float.IsFinite(BoundaryColor.R) || !float.IsFinite(BoundaryColor.G) ||
            !float.IsFinite(BoundaryColor.B) || BoundaryColor.A!=1f) return;
        LastOcclusionFrame=frame;
        float radius=Mathf.Min(BodyCueRadius,620f);
        foreach (AnimatableBody2D body in sources)
        {
            if (!Live(body) || !body.IsVisibleInTree() || !_seen.Add(body.GetInstanceId()) ||
                !Canonical(body,out string skinName) || body.GetViewport()!=GetViewport() ||
                body.GetWorld2D().GetRid()!=GetWorld2D().GetRid() ||
                !Aligned(body.GetCanvasTransform(),GetCanvasTransform()) ||
                (body.CollisionLayer&CombatCollisionLayers.World)==0) continue;
            // Warnings precede collision activation: do not hide them behind the solid-body filter.
            PresentWarning(body,actor,frame,radius);
            Polygon2D skin=body.GetNodeOrNull<Polygon2D>(skinName);
            CollisionShape2D collision=body.GetNodeOrNull<CollisionShape2D>("Collision");
            if (!Live(skin) || !skin.IsVisibleInTree() || skin.Color.A<=0f || !Live(collision) || collision.Disabled ||
                collision.Shape is not RectangleShape2D rectangle) continue;
            int[] owners=body.GetShapeOwners();
            if (owners.Length!=1) continue;
            uint owner=(uint)owners[0];
            Transform2D native=body.GlobalTransform*body.ShapeOwnerGetTransform(owner);
            Rect2 rect=rectangle.GetRect();
            if (body.IsShapeOwnerDisabled(owner) || body.ShapeOwnerGetOwner(owner)!=collision ||
                body.ShapeOwnerGetShapeCount(owner)!=1 || body.ShapeOwnerGetShape(owner,0)!=rectangle ||
                !Aligned(native,collision.GlobalTransform) || !Aligned(skin.GlobalTransform,collision.GlobalTransform) ||
                rect.Size.X<2f || rect.Size.Y<2f || rect.Size.X>512f || rect.Size.Y>512f || !MatchesSkin(skin,rect)) continue;
            foreach (HeadlampOccluder surface in frame.Shapes)
            {
                if (surface.BodyInstanceId!=body.GetInstanceId() || surface.OwnerId!=owner || surface.ShapeIndex!=0 ||
                    surface.LocalBounds!=rect || !Aligned(surface.WorldToLocal,native.AffineInverse())) continue;
                Transform2D forward=surface.WorldToLocal.AffineInverse();
                int before=PresentedSegmentCount;
                PaintFace(rect.Position,new Vector2(rect.End.X,rect.Position.Y),Vector2.Down,forward,body,actor,frame,surface,radius);
                PaintFace(new Vector2(rect.Position.X,rect.End.Y),rect.End,Vector2.Up,forward,body,actor,frame,surface,radius);
                PaintFace(rect.Position,new Vector2(rect.Position.X,rect.End.Y),Vector2.Right,forward,body,actor,frame,surface,radius);
                PaintFace(new Vector2(rect.End.X,rect.Position.Y),rect.End,Vector2.Left,forward,body,actor,frame,surface,radius);
                if (PresentedSegmentCount>before) PresentedBodyCount++;
                break;
            }
        }
    }
    private void PresentWarning(AnimatableBody2D body,Vector2 actor,HeadlampOcclusionFrame frame,float radius)
    {
        CanyonMechanismFrame state;
        NodePath path;
        string name;
        if(body is CanyonGearPiston piston)
        { state=piston.Frame; path=piston.TelegraphPath; name="GearPistonTelegraph"; }
        else if(body is CanyonPendulumHazard pendulum)
        { state=pendulum.Frame; path=pendulum.TelegraphPath; name="PendulumTelegraph"; }
        else return;
        if(!body.CanProcess() || !body.IsPhysicsProcessing() || !state.Telegraphing || state.HazardActive ||
            !state.Traversable || body.GetParent()?.GetParent()?.GetParent() is not PrologueSector sector) return;
        Polygon2D warning=body.GetNodeOrNull<Polygon2D>(path);
        if(!Live(warning) || !warning.IsVisibleInTree() ||
            sector.GetNodeOrNull<Polygon2D>("VisualRoot/GameplayReadable/MechanismTelegraphs/"+name)!=warning ||
            warning.GetViewport()!=GetViewport() || warning.GetWorld2D().GetRid()!=GetWorld2D().GetRid() ||
            !Aligned(warning.GetCanvasTransform(),GetCanvasTransform()) ||
            !Aligned(warning.GlobalTransform,warning.GlobalTransform)) return;
        Vector2[] points=warning.Polygon;
        if(points.Length!=4) return;
        Rect2 rect=new(points[0],points[2]-points[0]);
        if(!HeadlampOcclusionSampler.Finite(rect.Position) || !HeadlampOcclusionSampler.Finite(rect.Size) ||
            rect.Size.X<=0 || rect.Size.Y<=0 || rect.Size.X>512 || rect.Size.Y>32 || !MatchesSkin(warning,rect)) return;
        Color color=warning.Color*warning.SelfModulate;
        for(Node node=warning;node is CanvasItem item;node=node.GetParent()) color*=item.Modulate;
        if(!float.IsFinite(color.R) || !float.IsFinite(color.G) || !float.IsFinite(color.B) ||
            !float.IsFinite(color.A) || color.A<=0 || color.A>1) return;
        int before=PresentedWarningSegmentCount;
        Transform2D transform=warning.GlobalTransform;
        for(float y=rect.Position.Y;y<rect.End.Y;y+=8)
            for(float x=rect.Position.X;x<rect.End.X;x+=8)
                PaintWarning(new Rect2(x,y,Mathf.Min(8,rect.End.X-x),Mathf.Min(8,rect.End.Y-y)),
                    transform,warning,color,actor,frame,radius);
        if(PresentedWarningSegmentCount>before) PresentedWarningCount++;
    }
    private void PaintWarning(Rect2 patch,Transform2D transform,Polygon2D source,Color color,
        Vector2 actor,HeadlampOcclusionFrame frame,float radius)
    {
        Vector2[] world=[transform*patch.Position,transform*new Vector2(patch.End.X,patch.Position.Y),
            transform*patch.End,transform*new Vector2(patch.Position.X,patch.End.Y)];
        Vector2 min=new(float.PositiveInfinity,float.PositiveInfinity),max=new(float.NegativeInfinity,float.NegativeInfinity);
        for(int i=0;i<4;i++)
        {
            Vector2 point=world[i];
            // Ordinary LOS for the whole quad; no body-face terminal-contact exemption.
            if(!HeadlampOcclusionSampler.Finite(point) || actor.DistanceSquaredTo(point)>radius*radius ||
                !frame.CanSeeSegment(actor,point,world[(i+1)%4])) return;
            Vector2 pixel=source.GetCanvasTransform()*point;
            if(!HeadlampOcclusionSampler.Finite(pixel)) return;
            min=min.Min(pixel); max=max.Max(pixel);
        }
        if(!GetViewport().GetVisibleRect().Intersects(new Rect2(min,max-min))) return;
        Vector2[] local=[ToLocal(world[0]),ToLocal(world[1]),ToLocal(world[2]),ToLocal(world[3])];
        foreach(Vector2 point in local) if(!HeadlampOcclusionSampler.Finite(point)) return;
        RenderingServer.CanvasItemAddPolygon(GetCanvasItem(),local,[color]);
        PresentedWarningSegmentCount++;
    }
    private void PaintFace(Vector2 from,Vector2 to,Vector2 inward,Transform2D transform,Node2D body,
        Vector2 actor,HeadlampOcclusionFrame frame,HeadlampOccluder surface,float radius)
    {
        Vector2 actorLocal=surface.WorldToLocal*actor;
        if (!HeadlampOcclusionSampler.Finite(actorLocal) || (actorLocal-from).Dot(inward)>=0f) return;
        Vector2 vector=to-from; float length=vector.Length();
        if (!float.IsFinite(length) || length<=0f || length>512f) return;
        Vector2 direction=vector/length;
        for (float distance=0;distance<length;distance+=8f)
        {
            Vector2 start=from+direction*distance, end=from+direction*Mathf.Min(length,distance+8f);
            Vector2[] world=[transform*start,transform*end,transform*(end+inward*2),transform*(start+inward*2)];
            bool within=true;
            Vector2 min=new(float.PositiveInfinity,float.PositiveInfinity),max=new(float.NegativeInfinity,float.NegativeInfinity);
            foreach (Vector2 point in world)
            {
                if (!HeadlampOcclusionSampler.Finite(point) || actor.DistanceSquaredTo(point)>radius*radius) { within=false; break; }
                Vector2 pixel=body.GetCanvasTransform()*point;
                if (!HeadlampOcclusionSampler.Finite(pixel)) { within=false; break; }
                min=min.Min(pixel); max=max.Max(pixel);
            }
            if (!within || !GetViewport().GetVisibleRect().Intersects(new Rect2(min,max-min)) ||
                !frame.CanSeeBodyFacePatch(actor,surface,start,end,2)) continue;
            Vector2[] local=[ToLocal(world[0]),ToLocal(world[1]),ToLocal(world[2]),ToLocal(world[3])];
            foreach (Vector2 point in local) if (!HeadlampOcclusionSampler.Finite(point)) within=false;
            if (!within) continue;
            RenderingServer.CanvasItemAddPolygon(GetCanvasItem(),local,[BoundaryColor]);
            PresentedSegmentCount++;
        }
    }
    private static bool MatchesSkin(Polygon2D skin,Rect2 rect)
    {
        Vector2[] p=skin.Polygon;
        return p.Length==4 && p[0]==rect.Position && p[1]==new Vector2(rect.End.X,rect.Position.Y) &&
            p[2]==rect.End && p[3]==new Vector2(rect.Position.X,rect.End.Y);
    }
    private static bool Canonical(AnimatableBody2D body,out string skin)
    {
        skin=body switch { CanyonCounterweightLift=>"Carriage",CanyonGearPiston=>"PistonFace",CanyonPendulumHazard=>"VisiblePendulum",_=>null };
        string name=body switch { CanyonCounterweightLift=>"CounterweightLift",CanyonGearPiston=>"GearPiston",CanyonPendulumHazard=>"PendulumHazard",_=>null };
        return skin!=null && body.GetParent()?.GetParent()?.GetParent() is PrologueSector sector && Live(sector) &&
            sector.GetNodeOrNull<AnimatableBody2D>("Gameplay/Mechanisms/"+name)==body;
    }
    private static bool Aligned(Transform2D a,Transform2D b)=>
        HeadlampOcclusionSampler.Finite(a.X) && HeadlampOcclusionSampler.Finite(a.Y) && HeadlampOcclusionSampler.Finite(a.Origin) &&
        HeadlampOcclusionSampler.Finite(b.X) && HeadlampOcclusionSampler.Finite(b.Y) && HeadlampOcclusionSampler.Finite(b.Origin) &&
        float.IsFinite(a.Determinant()) && a.Determinant()!=0f && float.IsFinite(b.Determinant()) && b.Determinant()!=0f &&
        a.X.DistanceTo(b.X)<.001f && a.Y.DistanceTo(b.Y)<.001f && a.Origin.DistanceTo(b.Origin)<.01f;
    private static bool Live(Node node)
    {
        if (!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node current=node;current!=null;current=current.GetParent())
            if (current.IsQueuedForDeletion() || current.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) return false;
        return true;
    }
    public void Clear()
    { PresentedBodyCount=0; PresentedSegmentCount=0; PresentedWarningCount=0; PresentedWarningSegmentCount=0;
        LastOcclusionFrame=null; _seen.Clear(); RenderingServer.CanvasItemClear(GetCanvasItem()); }
    public override void _ExitTree() { Clear(); RequestReady(); }
}
