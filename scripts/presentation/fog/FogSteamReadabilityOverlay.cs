using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Prologue.Seamless;

namespace Knight.Presentation.Fog;

/// <summary>
/// Display-only native steam boundary. The real jet owns timing and damage;
/// this layer neither creates a safe window nor illuminates the room behind it.
/// </summary>
public partial class FogSteamReadabilityOverlay : Node2D
{
    [Export] public float SteamCueRadius { get; set; } = 620f;
    public int PresentedJetCount { get; private set; }
    public int PresentedSegmentCount { get; private set; }
    public HeadlampOcclusionFrame LastOcclusionFrame { get; private set; }
    private readonly HashSet<ulong> _seen = new();
    private static readonly Color Warning = new(1f, .73f, .28f);
    private static readonly Color Danger = new(1f, .53f, .17f);

    public override void _Ready() { ProcessMode = ProcessModeEnum.Always; Clear(); }
    public override void _Process(double delta) => Clear();

    public void PresentSteam(IReadOnlyList<CanyonSteamJet> sources, Vector2 actor, HeadlampOcclusionFrame frame)
    {
        Clear();
        if (!Live(this) || GetTree().Paused || sources == null || frame is not { IsComplete: true } ||
            !HeadlampOcclusionSampler.Finite(actor) || !float.IsFinite(SteamCueRadius) || SteamCueRadius <= 0f) return;
        LastOcclusionFrame = frame;
        float radius = Mathf.Min(SteamCueRadius, 620f);
        foreach (CanyonSteamJet jet in sources)
        {
            if (!Live(jet) || !jet.IsVisibleInTree() || jet.DisabledForRun || !SimulationActive(jet) || !_seen.Add(jet.GetInstanceId()) ||
                !CanonicalSource(jet) || jet.GetViewport() != GetViewport() ||
                jet.GetWorld2D().GetRid() != GetWorld2D().GetRid() ||
                !Aligned(jet.GetCanvasTransform(), GetCanvasTransform()) ||
                (jet.CollisionLayer & CombatCollisionLayers.EnemyAttack) == 0 ||
                (jet.CollisionMask & CombatCollisionLayers.Player) == 0) continue;
            CollisionShape2D collision = jet.GetNodeOrNull<CollisionShape2D>("Collision");
            if (!Live(collision)) continue;
            bool warning = jet.Frame.Telegraphing && !jet.Frame.HazardActive && jet.Frame.Traversable &&
                collision.Disabled && !jet.Monitoring;
            bool danger = !jet.Frame.Telegraphing && jet.Frame.HazardActive && !jet.Frame.Traversable &&
                !collision.Disabled && jet.Monitoring;
            if (!warning && !danger) continue;
            // Read the CollisionObject's current native owner, not merely the
            // CollisionShape node transform. Unsupported/multiple shapes close
            // this source rather than substituting a guessed bounding box.
            int[] owners = jet.GetShapeOwners();
            if (owners.Length != 1) continue;
            uint owner = (uint)owners[0];
            if (jet.ShapeOwnerGetOwner(owner) != collision ||
                jet.IsShapeOwnerDisabled(owner) != warning || jet.ShapeOwnerGetShapeCount(owner) != 1 ||
                jet.ShapeOwnerGetShape(owner, 0) is not RectangleShape2D shape || shape != collision.Shape) continue;
            Rect2 bounds = shape.GetRect();
            Transform2D transform = jet.GlobalTransform * jet.ShapeOwnerGetTransform(owner);
            if (!HeadlampOcclusionSampler.Finite(bounds.Position) || !HeadlampOcclusionSampler.Finite(bounds.End) ||
                bounds.Size.X < 8f || bounds.Size.Y < 8f || bounds.Size.X > 512f || bounds.Size.Y > 512f ||
                !HeadlampOcclusionSampler.Finite(transform.X) || !HeadlampOcclusionSampler.Finite(transform.Y) ||
                !HeadlampOcclusionSampler.Finite(transform.Origin) || !float.IsFinite(transform.Determinant()) ||
                transform.Determinant() == 0f) continue;
            int before = PresentedSegmentCount;
            float width = warning ? 2f : 3f;
            Color color = warning ? Warning : Danger;
            // Short segments allow genuinely visible portions to remain legible
            // without leaking a wall-hidden portion. All paint stays INSIDE the
            // actual rectangle. Broken amber = warning; solid wider orange = hit.
            for (float x = bounds.Position.X; x < bounds.End.X; x += warning ? 16f : 8f)
            {
                float length = Mathf.Min(8f, bounds.End.X - x);
                Paint(new Rect2(x,bounds.Position.Y,length,width),transform,jet,actor,frame,radius,color);
                Paint(new Rect2(x,bounds.End.Y-width,length,width),transform,jet,actor,frame,radius,color);
            }
            for (float y = bounds.Position.Y; y < bounds.End.Y; y += warning ? 16f : 8f)
            {
                float length = Mathf.Min(8f, bounds.End.Y - y);
                Paint(new Rect2(bounds.Position.X,y,width,length),transform,jet,actor,frame,radius,color);
                Paint(new Rect2(bounds.End.X-width,y,width,length),transform,jet,actor,frame,radius,color);
            }
            if (PresentedSegmentCount > before) PresentedJetCount++;
        }
    }

    private void Paint(Rect2 rect, Transform2D transform, CanyonSteamJet jet, Vector2 actor,
        HeadlampOcclusionFrame frame, float radius, Color color)
    {
        Vector2[] world = [transform * rect.Position, transform * new Vector2(rect.End.X,rect.Position.Y),
            transform * rect.End, transform * new Vector2(rect.Position.X,rect.End.Y)];
        Vector2 min = new(float.PositiveInfinity,float.PositiveInfinity), max = new(float.NegativeInfinity,float.NegativeInfinity);
        Transform2D canvas = jet.GetCanvasTransform();
        for (int index = 0; index < 4; index++)
        {
            Vector2 point = world[index];
            if (!HeadlampOcclusionSampler.Finite(point) || actor.DistanceSquaredTo(point) > radius * radius ||
                !frame.CanSeeSegment(actor,point,world[(index+1)%4])) return;
            Vector2 pixel = canvas * point;
            if (!HeadlampOcclusionSampler.Finite(pixel)) return;
            min = min.Min(pixel); max = max.Max(pixel);
        }
        if (!GetViewport().GetVisibleRect().Intersects(new Rect2(min,max-min))) return;
        Vector2[] local = [ToLocal(world[0]),ToLocal(world[1]),ToLocal(world[2]),ToLocal(world[3])];
        foreach (Vector2 point in local) if (!HeadlampOcclusionSampler.Finite(point)) return;
        RenderingServer.CanvasItemAddPolygon(GetCanvasItem(),local,[color]);
        PresentedSegmentCount++;
    }

    private static bool CanonicalSource(CanyonSteamJet jet)
    {
        Node parent = jet.GetParent();
        if (parent is CanyonSteamDiverter diverter)
        {
            if (!Live(diverter) || diverter.IsHandingOff ||
                (diverter.GetNodeOrNull<CanyonSteamJet>(diverter.LeftJetPath) != jet &&
                 diverter.GetNodeOrNull<CanyonSteamJet>(diverter.RightJetPath) != jet)) return false;
            parent = diverter.GetParent();
        }
        return Live(parent) && parent.GetParent()?.GetParent() is PrologueSector sector && Live(sector) &&
            sector.GetNodeOrNull<Node>("Gameplay/Mechanisms") == parent;
    }
    private static bool Aligned(Transform2D a, Transform2D b) =>
        HeadlampOcclusionSampler.Finite(a.X) && HeadlampOcclusionSampler.Finite(a.Y) &&
        HeadlampOcclusionSampler.Finite(a.Origin) && HeadlampOcclusionSampler.Finite(b.X) &&
        HeadlampOcclusionSampler.Finite(b.Y) && HeadlampOcclusionSampler.Finite(b.Origin) &&
        a.X.DistanceTo(b.X)<.001f && a.Y.DistanceTo(b.Y)<.001f && a.Origin.DistanceTo(b.Origin)<.01f;
    private static bool SimulationActive(CanyonSteamJet jet)
    {
        // SetPhysicsProcess(false) alone does not remove collision; the original
        // diverter intentionally owns its children's clocks that way. Effective
        // Disabled + Remove DOES remove the Area while leaving old flags intact.
        Node.ProcessModeEnum mode = Node.ProcessModeEnum.Inherit;
        for (Node current=jet;current!=null;current=current.GetParent())
            if (mode==Node.ProcessModeEnum.Inherit && current.ProcessMode!=Node.ProcessModeEnum.Inherit)
                mode=current.ProcessMode;
        return mode!=Node.ProcessModeEnum.Disabled || jet.DisableMode!=CollisionObject2D.DisableModeEnum.Remove;
    }
    private static bool Live(Node node)
    {
        if (!IsInstanceValid(node) || !node.IsInsideTree()) return false;
        for (Node current=node;current!=null;current=current.GetParent())
            if (current.IsQueuedForDeletion() || current.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)) return false;
        return true;
    }
    public void Clear()
    {
        PresentedJetCount=0; PresentedSegmentCount=0; LastOcclusionFrame=null; _seen.Clear();
        RenderingServer.CanvasItemClear(GetCanvasItem());
    }
    public override void _ExitTree() { Clear(); RequestReady(); }
}
