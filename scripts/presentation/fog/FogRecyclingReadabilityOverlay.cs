using System;
using System.Collections.Generic;
using Godot;
using Knight.Mechanisms;
using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

// Sparse edge/direction marks only. Original Areas own collision, timing and
// force; this consumer neither reveals a room nor manufactures a safe window.
public partial class FogRecyclingReadabilityOverlay : Node2D
{
    [Export] public float CueRadius { get; set; } = 620f;
    public int PresentedGearCount { get; private set; }
    public int PresentedPadCount { get; private set; }
    public int PresentedIncineratorCount { get; private set; }
    public int PresentedSegmentCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    private readonly HashSet<ulong> _seen=new();
    private static readonly Color GearColor=new("dfaa69"), PadColor=new("d4bc82");

    public override void _Ready() { ProcessMode=ProcessModeEnum.Always; Clear(); }
    public override void _Process(double delta) => Clear();

    public void PresentSources(IReadOnlyList<CollisionObject2D> sources, Vector2 actor, HeadlampOcclusionFrame frame)
    {
        Clear();
        if(!Live(this) || GetTree().Paused || sources==null || frame is not { IsComplete:true } ||
            !actor.IsFinite() || !float.IsFinite(CueRadius) || CueRadius<=0) return;
        LastOcclusionFrame=frame;
        float radius=Mathf.Min(CueRadius,620f);
        foreach(CollisionObject2D source in sources)
        {
            if(source is not (CanyonFixedGear or CanyonSpringboard or CanyonIncinerator) ||
                !Live(source) || !source.IsVisibleInTree() || !source.CanProcess() ||
                !_seen.Add(source.GetInstanceId()) || !Canonical(source) ||
                source.GetViewport()!=GetViewport() || source.GetWorld2D().GetRid()!=GetWorld2D().GetRid() ||
                !source.GetCanvasTransform().IsEqualApprox(GetCanvasTransform()) ||
                !PhysicsServer2D.AreaGetTransform(source.GetRid()).IsEqualApprox(source.GlobalTransform)) continue;
            int before=PresentedSegmentCount;
            if(source is CanyonFixedGear gear && gear.IsDangerTelegraphActive)
            {
                // The stroke stays within the actual circle, unlike an AABB
                // warning that would suggest danger in its empty corners.
                float edge=gear.DangerRadius-1.1f;
                for(int i=0;i<48;i++)
                    Paint(Vector2.Right.Rotated(i*Mathf.Tau/48)*edge,
                        Vector2.Right.Rotated((i+1)*Mathf.Tau/48)*edge,source,actor,frame,radius,GearColor,2f);
                if(PresentedSegmentCount>before) PresentedGearCount++;
            }
            else if(source is CanyonIncinerator pool && pool.IsDangerTelegraphActive)
            {
                Rect2 bounds=pool.DangerBounds;
                // Short true-boundary strokes remain range/occlusion gated;
                // no whole-room glow or undiscovered-map reveal is added.
                for(int i=0;i<16;i++)
                {
                    float x=bounds.Position.X+bounds.Size.X*i/16f;
                    float next=bounds.Position.X+bounds.Size.X*(i+1)/16f;
                    Paint(new(x,bounds.Position.Y),new(next,bounds.Position.Y),source,actor,frame,radius,GearColor,2f);
                }
                Paint(bounds.Position,new(bounds.Position.X,bounds.End.Y),source,actor,frame,radius,GearColor,2f);
                Paint(new(bounds.End.X,bounds.Position.Y),bounds.End,source,actor,frame,radius,GearColor,2f);
                if(PresentedSegmentCount>before) PresentedIncineratorCount++;
            }
            else if(source is CanyonSpringboard pad && pad.IsDangerTelegraphActive)
            {
                Paint(new(-pad.TriggerWidth*.5f+2,-4),new(pad.TriggerWidth*.5f-2,-4),
                    source,actor,frame,radius,PadColor,3f);
                Vector2 direction=pad.EffectiveLaunchVelocity.Normalized();
                Vector2 tail=new(0,-18),tip=tail+direction*16,side=direction.Orthogonal()*4;
                Paint(tail,tip,source,actor,frame,radius,PadColor,2f);
                Paint(tip,tip-direction*6+side,source,actor,frame,radius,PadColor,2f);
                Paint(tip,tip-direction*6-side,source,actor,frame,radius,PadColor,2f);
                if(PresentedSegmentCount>before) PresentedPadCount++;
            }
        }
    }

    private void Paint(Vector2 from,Vector2 to,Node2D source,Vector2 actor,
        HeadlampOcclusionFrame frame,float radius,Color color,float width)
    {
        Vector2 a=source.ToGlobal(from),b=source.ToGlobal(to);
        if(!a.IsFinite() || !b.IsFinite() || actor.DistanceSquaredTo(a)>radius*radius ||
            actor.DistanceSquaredTo(b)>radius*radius || !frame.CanSeeSegment(actor,a,b)) return;
        Vector2 pixelA=source.GetCanvasTransform()*a,pixelB=source.GetCanvasTransform()*b;
        if(!pixelA.IsFinite() || !pixelB.IsFinite()) return;
        Rect2 visible=GetViewport().GetVisibleRect();
        if(!visible.HasPoint(pixelA) && !visible.HasPoint(pixelB)) return;
        RenderingServer.CanvasItemAddLine(GetCanvasItem(),ToLocal(a),ToLocal(b),color,width,true);
        PresentedSegmentCount++;
    }

    private static bool Canonical(CollisionObject2D source) =>
        source.GetParent()?.GetParent()?.GetParent() is PrologueSector sector && Live(sector) &&
        sector.GetNodeOrNull<Node>("Gameplay/Mechanisms")==source.GetParent();

    private static bool Live(Node node)
    {
        if(!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for(Node current=node;current!=null;current=current.GetParent())
            if(current.IsQueuedForDeletion() || current.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) return false;
        return true;
    }

    public void Clear()
    {
        PresentedGearCount=0; PresentedPadCount=0; PresentedIncineratorCount=0; PresentedSegmentCount=0;
        LastOcclusionFrame=null; _seen.Clear(); RenderingServer.CanvasItemClear(GetCanvasItem());
    }
}
