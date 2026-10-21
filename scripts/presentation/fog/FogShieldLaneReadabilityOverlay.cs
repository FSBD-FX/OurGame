using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;

namespace Knight.Presentation.Fog;

/// <summary>
/// Display-only boundary for the original persistent polling damage lane.
/// Unlike a steam Area, this source cannot hurt when its physics callback stops.
/// </summary>
public partial class FogShieldLaneReadabilityOverlay : Node2D
{
    [Export] public float LaneCueRadius { get; set; }=620f;
    public int PresentedLaneCount { get; private set; }
    public int PresentedSegmentCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    private readonly HashSet<ulong> _seen=new();
    private readonly HashSet<ulong> _nearbyVisibleLanes=new();
    private static readonly Color Danger=new(.52f,.35f,.22f);
    private static readonly Color DangerCorner=new(.88f,.57f,.25f);
    private static readonly Color SourceGlow=new(1f,.76f,.34f);
    private static readonly Color Current=new(1f,.56f,.21f,.7f);
    private static readonly Color Trace=new(1f,.68f,.3f,.4f);
    private float _flowDistance;

    public override void _Ready() { ProcessMode=ProcessModeEnum.Always; Clear(); }
    public override void _Process(double delta)
    {
        // Cosmetic flow only: the native field stays dangerous between damage
        // polls. Never pulse its visibility or imply a cooldown/safe interval.
        if(!GetTree().Paused && double.IsFinite(delta) && delta>0)
            _flowDistance=Mathf.PosMod(_flowDistance+(float)System.Math.Min(delta,.1)*42f,64f);
        Clear();
    }

    public void PresentLanes(IReadOnlyList<CanyonShieldLaneHazard> sources,Vector2 actor,HeadlampOcclusionFrame frame)
    {
        Clear();
        if(!Live(this) || !IsVisibleInTree() || GetTree().Paused || sources==null || frame is not { IsComplete:true } ||
            !HeadlampOcclusionSampler.Finite(actor) || !float.IsFinite(LaneCueRadius) || LaneCueRadius<=0f) return;
        LastOcclusionFrame=frame;
        float radius=Mathf.Min(LaneCueRadius,620f);
        foreach(CanyonShieldLaneHazard lane in sources)
        {
            // Damage lives in _PhysicsProcess, not overlap signals. KeepActive,
            // Monitorable and the Area's own layer have different meanings here.
            if(!Live(lane) || !lane.IsVisibleInTree() || !lane.IsPhysicsProcessing() || !lane.CanProcess() ||
                !lane.Monitoring || lane.Damage<=0 || (lane.CollisionMask&CombatCollisionLayers.Player)==0 ||
                !_seen.Add(lane.GetInstanceId()) || !CanonicalSource(lane) ||
                lane.GetViewport()!=GetViewport() || lane.GetWorld2D().GetRid()!=GetWorld2D().GetRid() ||
                !Aligned(lane.GetCanvasTransform(),GetCanvasTransform())) continue;
            CollisionShape2D collision=lane.GetNodeOrNull<CollisionShape2D>("Collision");
            if(!Live(collision) || collision.Disabled) continue;
            int[] owners=lane.GetShapeOwners();
            if(owners.Length!=1) continue;
            uint owner=(uint)owners[0];
            if(lane.ShapeOwnerGetOwner(owner)!=collision || lane.IsShapeOwnerDisabled(owner) ||
                lane.ShapeOwnerGetShapeCount(owner)!=1 ||
                lane.ShapeOwnerGetShape(owner,0) is not RectangleShape2D shape || shape!=collision.Shape) continue;
            Rect2 bounds=shape.GetRect();
            Transform2D transform=lane.GlobalTransform*lane.ShapeOwnerGetTransform(owner);
            if(!HeadlampOcclusionSampler.Finite(bounds.Position) || !HeadlampOcclusionSampler.Finite(bounds.End) ||
                bounds.Size.X<8f || bounds.Size.Y<8f || bounds.Size.X>512f || bounds.Size.Y>512f ||
                !HeadlampOcclusionSampler.Finite(transform.X) || !HeadlampOcclusionSampler.Finite(transform.Y) ||
                !HeadlampOcclusionSampler.Finite(transform.Origin) || !float.IsFinite(transform.Determinant()) ||
                transform.Determinant()==0f) continue;
            int before=PresentedSegmentCount;
            // Never infer warning or safe windows from PulseCount/cooldown. The
            // original native footprint is continuously dangerous while running.
            for(float x=bounds.Position.X;x<bounds.End.X;x+=8f)
            {
                float length=Mathf.Min(8f,bounds.End.X-x);
                Paint(new Rect2(x,bounds.Position.Y,length,2f),transform,lane,actor,frame,radius,Danger);
                Paint(new Rect2(x,bounds.End.Y-2f,length,2f),transform,lane,actor,frame,radius,Danger);
            }
            for(float y=bounds.Position.Y;y<bounds.End.Y;y+=8f)
            {
                float length=Mathf.Min(8f,bounds.End.Y-y);
                Paint(new Rect2(bounds.Position.X,y,2f,length),transform,lane,actor,frame,radius,Danger);
                Paint(new Rect2(bounds.End.X-2f,y,2f,length),transform,lane,actor,frame,radius,Danger);
            }
            // Short, solid native corners carry the warning hierarchy while
            // the thin continuous perimeter stays quieter than the discharge.
            // Small shapes retain proportionate corners; no paint crosses the
            // actual damage rectangle or bypasses full-patch wall occlusion.
            float cornerLength=Mathf.Min(12f,Mathf.Min(bounds.Size.X,bounds.Size.Y)*.25f);
            foreach(float x in new[] { bounds.Position.X,bounds.End.X-cornerLength })
                foreach(float y in new[] { bounds.Position.Y,bounds.End.Y-2f })
                    Paint(new Rect2(x,y,cornerLength,2f),transform,lane,actor,frame,radius,DangerCorner);
            foreach(float x in new[] { bounds.Position.X,bounds.End.X-2f })
                foreach(float y in new[] { bounds.Position.Y,bounds.End.Y-cornerLength })
                    Paint(new Rect2(x,y,2f,cornerLength),transform,lane,actor,frame,radius,DangerCorner);
            PaintDirection(bounds,transform,lane,actor,frame,radius);
            if(PresentedSegmentCount>before) PresentedLaneCount++;
        }
    }

    private void PaintDirection(Rect2 bounds,Transform2D transform,CanyonShieldLaneHazard lane,
        Vector2 actor,HeadlampOcclusionFrame frame,float radius)
    {
        if(bounds.Size.X<100f || bounds.Size.Y<60f) return;
        Transform2D inverse=transform.AffineInverse();
        Vector2 center=transform*bounds.GetCenter();
        Vector2 rightLocal=inverse.BasisXform(Vector2.Right);
        float rightDistance=Mathf.Min(Mathf.Abs(rightLocal.X)>.0001f ? bounds.Size.X*.5f/Mathf.Abs(rightLocal.X) : float.PositiveInfinity,
            Mathf.Abs(rightLocal.Y)>.0001f ? bounds.Size.Y*.5f/Mathf.Abs(rightLocal.Y) : float.PositiveInfinity);
        // DamageRequest travels world-left; shield logic faces the world-right
        // source. Mirror/rotation of the native rectangle must not flip that.
        float sourceX=center.X+rightDistance-Mathf.Min(10f,rightDistance*.25f);
        // Readable device silhouette, not a new collision or a bigger hazard.
        // Extremely narrow native transforms keep the original minimal rail;
        // every full polygon below still passes the same native/occlusion gates.
        if(float.IsFinite(rightDistance) && rightDistance>=45f)
        {
            Color iron=new(.14f,.22f,.22f,.98f);
            Color shadow=new(.08f,.12f,.13f,.98f);
            Color rim=new(.34f,.39f,.35f,.98f);
            Color copper=new(.64f,.43f,.20f,.98f);
            Color coil=new(.93f,.64f,.28f);
            Stroke(new Vector2(sourceX-11f,center.Y-45f),new Vector2(sourceX-11f,center.Y+45f),32f,iron);
            Stroke(new Vector2(sourceX-24f,center.Y-43f),new Vector2(sourceX-24f,center.Y+43f),6f,shadow);
            Stroke(new Vector2(sourceX-11f,center.Y-49f),new Vector2(sourceX-11f,center.Y-42f),34f,rim);
            Stroke(new Vector2(sourceX-11f,center.Y+42f),new Vector2(sourceX-11f,center.Y+49f),34f,rim);
            Stroke(new Vector2(sourceX-23f,center.Y-47f),new Vector2(sourceX-5f,center.Y-47f),2f,copper);
            Stroke(new Vector2(sourceX-23f,center.Y+47f),new Vector2(sourceX-5f,center.Y+47f),2f,shadow);
            // Three cracked insulator banks. The missing middle winding and
            // open collar make it read as damaged machinery, not a pristine UI.
            for(int bank=-1;bank<=1;bank++)
            {
                float y=center.Y+bank*30f;
                Stroke(new Vector2(sourceX-20f,y-9f),new Vector2(sourceX-1f,y-9f),4f,copper);
                Stroke(new Vector2(sourceX-20f,y+9f),new Vector2(sourceX-1f,y+9f),4f,copper);
                for(int winding=-1;winding<=1;winding++)
                {
                    if(bank==0 && winding==0) continue;
                    Stroke(new Vector2(sourceX-18f,y+winding*5f),new Vector2(sourceX-4f,y+winding*5f-2f),2f,coil);
                }
                Stroke(new Vector2(sourceX-22f,y-6f),new Vector2(sourceX-17f,y-2f),2f,shadow);
                Stroke(new Vector2(sourceX-17f,y-2f),new Vector2(sourceX-20f,y+3f),2f,shadow);
            }
            // A broken central winding has two visible ends and a dark gap;
            // restrained, fixed detail avoids grain or flickering noise.
            Stroke(new Vector2(sourceX-18f,center.Y),new Vector2(sourceX-13f,center.Y-2f),2f,coil);
            Stroke(new Vector2(sourceX-7f,center.Y+2f),new Vector2(sourceX-4f,center.Y-1f),2f,copper);
            // Two continuous strands, with a world-left travelling ripple.
            // Motion changes their shape, never the visibility/damage duty cycle.
            float leftX=Mathf.Max(center.X-rightDistance+24f,actor.X-radius+4f);
            float rightX=Mathf.Min(sourceX-21f,actor.X+radius-4f);
            float span=rightX-leftX;
            // Clip cosmetic work to the already-authorized radius. At most
            // 1232 world pixels / 16 per segment: bounded without undersampling
            // the 64px wave when a level designer widens the native field.
            int segments=float.IsFinite(span) && span>0f
                ? Mathf.Clamp(Mathf.CeilToInt(span/16f),1,78) : 0;
            for(int strand=-1;strand<=1;strand+=2)
            {
                float y=center.Y+strand*12f;
                for(int segment=0;segment<segments;segment++)
                {
                    float x=Mathf.Lerp(leftX,rightX,(float)segment/segments);
                    float next=Mathf.Lerp(leftX,rightX,(float)(segment+1)/segments);
                    Vector2 from=new(x,y+Mathf.Sin((x+_flowDistance)*Mathf.Tau/64f)*2.5f);
                    Vector2 to=new(next,y+Mathf.Sin((next+_flowDistance)*Mathf.Tau/64f)*2.5f);
                    Stroke(from,to,4f,new Color(1f,.48f,.13f,.20f),softenAtActor:true);
                    Stroke(from,to,1.25f,new Color(1f,.82f,.46f,.86f),softenAtActor:true);
                }
            }
        }
        Stroke(new Vector2(sourceX,center.Y-47f),new Vector2(sourceX,center.Y+47f),3f,SourceGlow);
        for(int row=-2;row<=2;row++)
            Stroke(new Vector2(sourceX-7f,center.Y+row*20f),new Vector2(sourceX+4f,center.Y+row*20f),3f,SourceGlow);
        for(int row=0;row<2;row++)
        {
            float y=bounds.Position.Y+bounds.Size.Y*(row==0 ? .26f : .74f);
            for(float x=bounds.Position.X+48f;x<bounds.End.X-30f;x+=64f)
            {
                Vector2 station=transform*new Vector2(x,y);
                Stroke(station+new Vector2(-8f,0f),station+new Vector2(3f,-6f),2f,Current,softenAtActor:true);
                Stroke(station+new Vector2(-8f,0f),station+new Vector2(3f,6f),2f,Current,softenAtActor:true);
                Vector2 tracer=station+new Vector2(20f-_flowDistance,10f);
                Stroke(tracer,tracer+new Vector2(8f,0f),1.5f,Trace,softenAtActor:true);
            }
        }
        void Stroke(Vector2 from,Vector2 to,float width,Color color,bool softenAtActor=false)
        {
            Vector2 normal=(to-from).Orthogonal().Normalized()*(width*.5f);
            Vector2[] polygon=[from+normal,to+normal,to-normal,from-normal];
            // All four points lie inside a convex native rectangle, so the
            // complete stroke does too. No emitter or spark expands danger.
            foreach(Vector2 point in polygon)
                if(!bounds.HasPoint(inverse*point)) return;
            if(softenAtActor)
            {
                // These cosmetic strokes live above fog, hence above the real
                // player too. Soften only current near its body-centered origin;
                // the native field, perimeter and solid source are never cut out.
                // Nearest segment distance (including width) avoids an opaque
                // long stroke crossing the core just because its midpoint misses.
                Vector2 radii=new(28f,44f);
                Vector2 start=(from-actor)/radii,span=(to-from)/radii;
                float lengthSquared=span.LengthSquared();
                float t=lengthSquared>0f ? Mathf.Clamp(-start.Dot(span)/lengthSquared,0f,1f) : 0f;
                float distance=(start+span*t).Length()-width/(2f*28f);
                color.A*=Mathf.SmoothStep(1f,1.6f,distance);
                if(color.A<=0f) return;
            }
            PaintPolygon(polygon,lane,actor,frame,radius,color);
        }
    }

    private void Paint(Rect2 rect,Transform2D transform,CanyonShieldLaneHazard lane,Vector2 actor,
        HeadlampOcclusionFrame frame,float radius,Color color)
    {
        Vector2[] world=[transform*rect.Position,transform*new Vector2(rect.End.X,rect.Position.Y),
            transform*rect.End,transform*new Vector2(rect.Position.X,rect.End.Y)];
        PaintPolygon(world,lane,actor,frame,radius,color);
    }

    private void PaintPolygon(Vector2[] world,CanyonShieldLaneHazard lane,Vector2 actor,
        HeadlampOcclusionFrame frame,float radius,Color color)
    {
        Vector2 min=new(float.PositiveInfinity,float.PositiveInfinity),max=new(float.NegativeInfinity,float.NegativeInfinity);
        Transform2D canvas=lane.GetCanvasTransform();
        for(int index=0;index<4;index++)
        {
            Vector2 point=world[index];
            if(!HeadlampOcclusionSampler.Finite(point) || actor.DistanceSquaredTo(point)>radius*radius) return;
            Vector2 pixel=canvas*point;
            if(!HeadlampOcclusionSampler.Finite(pixel)) return;
            min=min.Min(pixel); max=max.Max(pixel);
        }
        if(!GetViewport().GetVisibleRect().Intersects(new Rect2(min,max-min))) return;
        if(!frame.CanSeeQuad(actor,world[0],world[1],world[2],world[3])) return;
        Vector2[] local=[ToLocal(world[0]),ToLocal(world[1]),ToLocal(world[2]),ToLocal(world[3])];
        foreach(Vector2 point in local) if(!HeadlampOcclusionSampler.Finite(point)) return;
        RenderingServer.CanvasItemAddPolygon(GetCanvasItem(),local,[color]);
        PresentedSegmentCount++;
        // Reuse a genuinely submitted, wholly visible native patch. A guidance
        // consumer cannot turn a distant or wall-hidden source into a marker.
        Vector2 center=(world[0]+world[1]+world[2]+world[3])*.25f;
        if(actor.DistanceSquaredTo(center)<=220f*220f && GetViewport().GetVisibleRect().HasPoint(canvas*center))
            _nearbyVisibleLanes.Add(lane.GetInstanceId());
    }
    public bool HasNearbyVisibleLane(CanyonShieldLaneHazard lane,HeadlampOcclusionFrame frame)=>
        Live(this) && IsVisibleInTree() && !GetTree().Paused && Live(lane) &&
        ReferenceEquals(frame,LastOcclusionFrame) && frame is { IsComplete:true } &&
        _nearbyVisibleLanes.Contains(lane.GetInstanceId());
    private static bool CanonicalSource(CanyonShieldLaneHazard lane)=>
        lane.GetParent()?.GetParent() is PrologueSector sector && Live(sector) &&
        sector.GetNodeOrNull<CanyonShieldLaneHazard>("Gameplay/ShieldLane")==lane;
    private static bool Aligned(Transform2D a,Transform2D b)=>
        HeadlampOcclusionSampler.Finite(a.X) && HeadlampOcclusionSampler.Finite(a.Y) &&
        HeadlampOcclusionSampler.Finite(a.Origin) && HeadlampOcclusionSampler.Finite(b.X) &&
        HeadlampOcclusionSampler.Finite(b.Y) && HeadlampOcclusionSampler.Finite(b.Origin) &&
        a.X.DistanceTo(b.X)<.001f && a.Y.DistanceTo(b.Y)<.001f && a.Origin.DistanceTo(b.Origin)<.01f;
    private static bool Live(Node node)
    {
        if(!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for(Node current=node;current!=null;current=current.GetParent())
            if(current.IsQueuedForDeletion() || current.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) return false;
        return true;
    }
    public void Clear()
    {
        PresentedLaneCount=0; PresentedSegmentCount=0; LastOcclusionFrame=null; _seen.Clear();
        _nearbyVisibleLanes.Clear();
        RenderingServer.CanvasItemClear(GetCanvasItem());
    }
    public override void _ExitTree() { Clear(); RequestReady(); }
}
