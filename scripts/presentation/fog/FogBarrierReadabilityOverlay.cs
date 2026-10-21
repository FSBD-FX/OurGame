using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

/// <summary>Current authored solid door faces, not admission logic or a promise that a passage is open.</summary>
public partial class FogBarrierReadabilityOverlay : Node2D
{
    internal static readonly string[] SourcePaths=[
        "Gameplay/Mechanisms/CourtyardSeal", "Gameplay/Mechanisms/BellChain/Door",
        "Gameplay/Mechanisms/UpperBasinShortcutWinch/Barrier", "Gameplay/Doors/AdmissionBarrier",
        "Gameplay/Doors/Entry", "Gameplay/Doors/Exit", "Gameplay/Mechanisms/TemplePressureGate/Door",
        "Gameplay/Mechanisms/ArchivePressureGate/Door"];
    [Export] public float BarrierCueRadius { get; set; }=620f;
    [Export] public Color BoundaryColor { get; set; }=new("cbbd93");
    public int PresentedBodyCount { get; private set; }
    public int PresentedSegmentCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    private readonly HashSet<ulong> _seen=new();

    public override void _Ready() { ProcessMode=ProcessModeEnum.Always; Clear(); }
    public override void _Process(double delta)=>Clear();
    public void PresentBarriers(IReadOnlyList<StaticBody2D> sources,Vector2 actor,HeadlampOcclusionFrame frame)
    {
        Clear();
        if(!Live(this) || GetTree().Paused || sources==null || frame is not { IsComplete:true } ||
            !HeadlampOcclusionSampler.Finite(actor) || !float.IsFinite(BarrierCueRadius) || BarrierCueRadius<=0 ||
            !float.IsFinite(BoundaryColor.R) || !float.IsFinite(BoundaryColor.G) ||
            !float.IsFinite(BoundaryColor.B) || BoundaryColor.A!=1f) return;
        LastOcclusionFrame=frame;
        float radius=Mathf.Min(BarrierCueRadius,620f);
        foreach(StaticBody2D body in sources)
        {
            if(!Live(body) || !body.IsVisibleInTree() || !_seen.Add(body.GetInstanceId()) || !Canonical(body,out bool admission) ||
                body.GetViewport()!=GetViewport() || body.GetWorld2D().GetRid()!=GetWorld2D().GetRid() ||
                !Aligned(body.GetCanvasTransform(),GetCanvasTransform()) || (body.CollisionLayer&CombatCollisionLayers.World)==0) continue;
            int[] owners=body.GetShapeOwners();
            if(owners.Length!=(admission ? 3 : 1)) continue;
            int before=PresentedSegmentCount;
            foreach(int value in owners)
            {
                uint owner=(uint)value;
                if(body.ShapeOwnerGetOwner(owner) is not CollisionShape2D collision || !Live(collision) ||
                    collision.GetParent()!=body || body.GetNodeOrNull<CollisionShape2D>(collision.Name.ToString())!=collision ||
                    (collision.Name!="Collision" && (!admission || (collision.Name!="TopCollision" && collision.Name!="RightCollision"))) ||
                    collision.Disabled || body.IsShapeOwnerDisabled(owner) || body.ShapeOwnerGetShapeCount(owner)!=1 ||
                    collision.Shape is not RectangleShape2D rectangle || body.ShapeOwnerGetShape(owner,0)!=rectangle) continue;
                Transform2D native=body.GlobalTransform*body.ShapeOwnerGetTransform(owner);
                Rect2 rect=rectangle.GetRect();
                if(!Aligned(native,collision.GlobalTransform) || !HeadlampOcclusionSampler.Finite(rect.Size) ||
                    rect.Size.X<2 || rect.Size.Y<2 || rect.Size.X>4096 || rect.Size.Y>4096) continue;
                foreach(HeadlampOccluder surface in frame.Shapes)
                {
                    if(surface.BodyInstanceId!=body.GetInstanceId() || surface.OwnerId!=owner || surface.ShapeIndex!=0 ||
                        surface.LocalBounds!=rect || !Aligned(surface.WorldToLocal,native.AffineInverse())) continue;
                    Transform2D forward=surface.WorldToLocal.AffineInverse();
                    PaintFace(rect.Position,new Vector2(rect.End.X,rect.Position.Y),Vector2.Down,forward,body,actor,frame,surface,radius);
                    PaintFace(new Vector2(rect.Position.X,rect.End.Y),rect.End,Vector2.Up,forward,body,actor,frame,surface,radius);
                    PaintFace(rect.Position,new Vector2(rect.Position.X,rect.End.Y),Vector2.Right,forward,body,actor,frame,surface,radius);
                    PaintFace(new Vector2(rect.End.X,rect.Position.Y),rect.End,Vector2.Left,forward,body,actor,frame,surface,radius);
                    break;
                }
            }
            if(PresentedSegmentCount>before) PresentedBodyCount++;
        }
    }
    private void PaintFace(Vector2 from,Vector2 to,Vector2 inward,Transform2D transform,Node2D body,
        Vector2 actor,HeadlampOcclusionFrame frame,HeadlampOccluder surface,float radius)
    {
        Vector2 localActor=surface.WorldToLocal*actor;
        if(!HeadlampOcclusionSampler.Finite(localActor) || (localActor-from).Dot(inward)>=0) return;
        Vector2 vector=to-from; float length=vector.Length();
        if(!float.IsFinite(length) || length<=0 || length>4096) return;
        Vector2 direction=vector/length;
        for(float distance=0;distance<length;distance+=8)
        {
            Vector2 start=from+direction*distance,end=from+direction*Mathf.Min(length,distance+8);
            Vector2[] world=[transform*start,transform*end,transform*(end+inward*2),transform*(start+inward*2)];
            bool eligible=true;
            Vector2 min=new(float.PositiveInfinity,float.PositiveInfinity),max=new(float.NegativeInfinity,float.NegativeInfinity);
            foreach(Vector2 point in world)
            {
                if(!HeadlampOcclusionSampler.Finite(point) || actor.DistanceSquaredTo(point)>radius*radius) { eligible=false; break; }
                Vector2 pixel=body.GetCanvasTransform()*point;
                if(!HeadlampOcclusionSampler.Finite(pixel)) { eligible=false; break; }
                min=min.Min(pixel); max=max.Max(pixel);
            }
            if(!eligible || !GetViewport().GetVisibleRect().Intersects(new Rect2(min,max-min)) ||
                !frame.CanSeeBodyFacePatch(actor,surface,start,end,2)) continue;
            Vector2[] local=[ToLocal(world[0]),ToLocal(world[1]),ToLocal(world[2]),ToLocal(world[3])];
            foreach(Vector2 point in local) if(!HeadlampOcclusionSampler.Finite(point)) eligible=false;
            if(!eligible) continue;
            RenderingServer.CanvasItemAddPolygon(GetCanvasItem(),local,[BoundaryColor]); PresentedSegmentCount++;
        }
    }
    private static bool Canonical(StaticBody2D body,out bool admission)
    {
        admission=false;
        for(Node ancestor=body.GetParent();ancestor!=null;ancestor=ancestor.GetParent())
            if(ancestor is PrologueSector sector)
            {
                if(!Live(sector)) return false;
                foreach(string path in SourcePaths)
                    if(sector.GetNodeOrNull<StaticBody2D>(path)==body)
                    { admission=path=="Gameplay/Doors/AdmissionBarrier"; return true; }
                return false;
            }
        return false;
    }
    private static bool Aligned(Transform2D a,Transform2D b)=>
        HeadlampOcclusionSampler.Finite(a.X) && HeadlampOcclusionSampler.Finite(a.Y) && HeadlampOcclusionSampler.Finite(a.Origin) &&
        HeadlampOcclusionSampler.Finite(b.X) && HeadlampOcclusionSampler.Finite(b.Y) && HeadlampOcclusionSampler.Finite(b.Origin) &&
        float.IsFinite(a.Determinant()) && a.Determinant()!=0 && float.IsFinite(b.Determinant()) && b.Determinant()!=0 &&
        a.X.DistanceTo(b.X)<.001f && a.Y.DistanceTo(b.Y)<.001f && a.Origin.DistanceTo(b.Origin)<.01f;
    private static bool Live(Node node)
    {
        if(!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for(Node current=node;current!=null;current=current.GetParent())
            if(current.IsQueuedForDeletion() || current.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) return false;
        return true;
    }
    public void Clear()
    { PresentedBodyCount=0; PresentedSegmentCount=0; LastOcclusionFrame=null; _seen.Clear(); RenderingServer.CanvasItemClear(GetCanvasItem()); }
    public override void _ExitTree() { Clear(); RequestReady(); }
}
