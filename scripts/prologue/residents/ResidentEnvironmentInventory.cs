using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Interactions;
using Knight.Mechanisms;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.World;

namespace Knight.Prologue.Residents;

// Conservative future environment reservations, NOT a resident/path certificate.
public sealed class ResidentEnvironmentSnapshot
{
    public IReadOnlyList<ResidentKeepOut> Regions { get; }
    public IReadOnlyList<string> Issues { get; }
    public bool IsCompletePlacementProof => false;
    public ResidentEnvironmentSnapshot(IEnumerable<ResidentKeepOut> regions, IEnumerable<string> issues)
    {
        Regions = Array.AsReadOnly(regions.ToArray());
        Issues = Array.AsReadOnly(issues.ToArray());
    }
}

public static class ResidentEnvironmentInventory
{
    // These are NEW resident-placement policies, not old trigger dimensions.
    private const float RewardApproachMargin = 40f;
    private static readonly Rect2 CheckpointLocalProtection = new(-120,-144,240,192);

    public static ResidentEnvironmentSnapshot Capture(Node2D worldRoot)
    {
        var regions = new List<ResidentKeepOut>();
        var issues = new List<string>();
        if (!GodotObject.IsInstanceValid(worldRoot) || !worldRoot.IsInsideTree())
            return new(regions,new[] { "invalid_world" });
        foreach (Node node in Walk(worldRoot))
        {
            switch (node)
            {
                case KillZone recovery: ReserveBody(recovery,"recovery",ResidentKeepOutKind.Hazard); break;
                case CanyonSteamJet steam: ReserveBody(steam,"steam",ResidentKeepOutKind.Hazard); break;
                case CanyonShieldLaneHazard electric: ReserveBody(electric,"electric",ResidentKeepOutKind.Hazard); break;
                case CanyonGearPiston piston:
                    Linear(piston,piston.RetractedPosition,piston.ExtendedPosition);
                    if (!piston.RouteBounds.HasPoint(piston.RetractedPosition) || !piston.RouteBounds.HasPoint(piston.ExtendedPosition))
                        issues.Add("invalid_motion:"+Path(piston));
                    break;
                case CanyonCounterweightLift lift: Linear(lift,lift.LowerPosition,lift.UpperPosition); break;
                case CanyonPendulumHazard pendulum: Arc(pendulum); break;
                case CanyonPressurePlateGate pressure:
                    ConditionalAt(pressure,pressure.DoorPath);
                    // These public contracts are already WORLD rectangles.
                    Add("interaction-plate:"+Path(pressure),ResidentKeepOutKind.PlayerInteraction,pressure.PlateWorldRect);
                    Add("interaction-interior:"+Path(pressure),ResidentKeepOutKind.PlayerInteraction,pressure.InteriorWorldRect);
                    Add("interaction-anti-crush:"+Path(pressure),ResidentKeepOutKind.PlayerInteraction,pressure.AntiCrushWorldRect);
                    break;
                case CanyonShortcutWinch winch:
                    ConditionalAt(winch,winch.BarrierCollisionPath);
                    Add("interaction-corridor:"+Path(winch),ResidentKeepOutKind.PlayerInteraction,winch.GlobalTransform*winch.CorridorBounds);
                    if (!Valid(winch.GlobalTransform)) issues.Add("invalid_transform:"+Path(winch));
                    break;
                case ThreadTarget2D target:
                    if (!Enum.IsDefined(target.Kind)) issues.Add("invalid_interaction_target:"+Path(target));
                    else if (target.Kind==ThreadTargetKind.Mechanism) ReserveTarget(target);
                    break;
                case ManualSteamValveControl manual: ReserveManual(manual); break;
                case CanyonSealGate seal: ReserveBody(seal,"conditional",ResidentKeepOutKind.ConditionalSolid); break;
                case BellChainMechanism bell: ConditionalAt(bell,bell.DoorCollisionPath); break;
                case BossArenaController arena:
                    ConditionalAt(arena,arena.EntryDoorCollisionPath);
                    ConditionalAt(arena,arena.ExitDoorCollisionPath);
                    if (arena.RequiresFragmentAdmission) ConditionalAt(arena,arena.AdmissionBarrierCollisionPath);
                    break;
                case QuestFragment fragment: ReserveBody(fragment,"fragment",ResidentKeepOutKind.FragmentApproach,RewardApproachMargin); break;
                case PrologueSupplyCache supply: ReserveBody(supply,"supply",ResidentKeepOutKind.SupplyApproach,RewardApproachMargin); break;
                case TraversalSegment2D segment:
                    Add("launch:"+Path(segment),ResidentKeepOutKind.TraversalLaunch,segment.GlobalTransform*segment.LaunchZone);
                    Add("landing:"+Path(segment),ResidentKeepOutKind.TraversalLanding,segment.GlobalTransform*segment.LandingZone);
                    if (!Valid(segment.GlobalTransform)) issues.Add("invalid_transform:"+Path(segment));
                    break;
            }
        }
        // These layout values are already WORLD coordinates, not sector local.
        for (int i=0;i<CanyonWorldLayout.EncounterRestBounds.Count;i++)
            Add("rest:"+i,ResidentKeepOutKind.Protected,CanyonWorldLayout.EncounterRestBounds[i]);
        foreach (var checkpoint in CanyonWorldLayout.CheckpointWorldPositions)
            Add("checkpoint:"+checkpoint.Key,ResidentKeepOutKind.Protected,
                new Rect2(checkpoint.Value+CheckpointLocalProtection.Position,CheckpointLocalProtection.Size));
        foreach (CanyonRouteNodeDefinition route in CanyonWorldLayout.Nodes.Where(x=>x.Id==CanyonRouteNodeId.BossHall))
            Add("boss:"+route.Id,ResidentKeepOutKind.Protected,route.WorldBounds);
        return new(regions,issues.Distinct(StringComparer.Ordinal));

        string Path(Node node)=>worldRoot.GetPathTo(node).ToString();
        void Add(string id,ResidentKeepOutKind kind,Rect2 bounds)
        {
            if (!Positive(bounds)) { issues.Add("invalid_bounds:"+id); return; }
            if (regions.Any(x=>x.SourceId==id)) { issues.Add("duplicate_source:"+id); return; }
            regions.Add(new(id,kind,bounds));
        }
        CollisionShape2D[] Shapes(CollisionObject2D body)
        {
            CollisionShape2D[] shapes=Walk(body).OfType<CollisionShape2D>().Where(x=>NearestBody(x)==body).ToArray();
            if (shapes.Length==0 || Walk(body).OfType<CollisionPolygon2D>().Any(x=>NearestBody(x)==body))
                issues.Add("invalid_shape:"+Path(body));
            return shapes;
        }
        bool ShapeValid(CollisionShape2D shape)
        {
            if (shape.Shape is not (RectangleShape2D or CircleShape2D or CapsuleShape2D) || !Positive(shape.Shape.GetRect()))
            { issues.Add("invalid_shape:"+Path(shape));return false; }
            if (!Valid(shape.GlobalTransform)) { issues.Add("invalid_transform:"+Path(shape));return false; }
            return true;
        }
        void ReserveBody(CollisionObject2D body,string prefix,ResidentKeepOutKind kind,float grow=0)
        {
            foreach (CollisionShape2D shape in Shapes(body))
                if (ShapeValid(shape)) Add(prefix+":"+Path(shape),kind,(shape.GlobalTransform*shape.Shape.GetRect()).Grow(grow));
        }
        Node Receiver(ThreadTarget2D target)=>string.IsNullOrEmpty(target.PullReceiverPath?.ToString())
            ? null : target.GetNodeOrNull<Node>(target.PullReceiverPath);
        void ReserveTarget(ThreadTarget2D target)
        {
            string id=Path(target);
            if (target.Validate().Count!=0 || !Valid(target.GlobalTransform))
                issues.Add("invalid_interaction_target:"+id);
            if (Receiver(target) is not IThreadInteractionReceiver)
                issues.Add("invalid_interaction_receiver:"+id);
            CollisionObject2D[] bodies=Walk(target).OfType<CollisionObject2D>().ToArray();
            CollisionShape2D[] shapes=Walk(target).OfType<CollisionShape2D>().ToArray();
            if (bodies.Length!=1 || bodies[0] is not Area2D || shapes.Length!=1 ||
                NearestBody(shapes[0])!=bodies[0] || Walk(target).OfType<CollisionPolygon2D>().Any() ||
                !ShapeValid(shapes[0]))
            { issues.Add("invalid_interaction_shape:"+id);return; }
            // Retain disabled targets: an initial mechanism state is not permission
            // to occupy the place the player needs in a later state.
            ReserveBody(bodies[0],"interaction-target",ResidentKeepOutKind.PlayerInteraction);
        }
        void ReserveManual(ManualSteamValveControl manual)
        {
            ThreadTarget2D target=manual.InteractionNode as ThreadTarget2D;
            string id=target==null?Path(manual):Path(target);
            if (target==null || target.Kind!=ThreadTargetKind.Mechanism || target.Validate().Count!=0 ||
                !Valid(target.GlobalTransform) || target.Material is GrappleSurfaceMaterial.Ice or GrappleSurfaceMaterial.Forbidden ||
                Receiver(target) is not CanyonSteamValve valve || string.IsNullOrWhiteSpace(valve.ActivationId) ||
                string.IsNullOrEmpty(valve.PairedJetPath?.ToString()) ||
                valve.GetNodeOrNull<CanyonSteamJet>(valve.PairedJetPath)==null)
            { issues.Add("invalid_manual_control:"+id);return; }
            // Q measures world distance, not a scaled/rotated target-local shape.
            float radius=EnvironmentInteractionModel.DefaultRadius;
            Add("interaction-manual:"+id,ResidentKeepOutKind.PlayerInteraction,
                new Rect2(target.GlobalPosition-new Vector2(radius,radius),new Vector2(radius*2,radius*2)));
        }
        void ConditionalAt(Node owner,NodePath path)
        {
            Node node=owner.GetNodeOrNull<Node>(path);
            CollisionObject2D body=node as CollisionObject2D;
            if (node is CollisionShape2D shape) body=NearestBody(shape);
            if (body==null) { issues.Add("missing_conditional:"+Path(owner));return; }
            // AdmissionBarrier has three shapes. Never just reserve its primary.
            ReserveBody(body,"conditional",ResidentKeepOutKind.ConditionalSolid);
        }
        bool MotionParts(Node2D body,out Transform2D parentWorld,out CollisionShape2D[] shapes)
        {
            parentWorld=Transform2D.Identity;shapes=Shapes((CollisionObject2D)body);
            if (!Valid(body.Transform)||!Valid(body.GlobalTransform)) { issues.Add("invalid_motion:"+Path(body));return false; }
            parentWorld=body.GlobalTransform*body.Transform.AffineInverse();
            return Valid(parentWorld);
        }
        void Linear(Node2D body,Vector2 from,Vector2 to)
        {
            if (!from.IsFinite()||!to.IsFinite()) { issues.Add("invalid_motion:"+Path(body));return; }
            if (!MotionParts(body,out Transform2D parentWorld,out CollisionShape2D[] shapes)) return;
            Transform2D start=body.Transform,end=body.Transform;start.Origin=from;end.Origin=to;
            foreach (CollisionShape2D shape in shapes)
            {
                if (!ShapeValid(shape)) continue;
                Transform2D relative=body.GlobalTransform.AffineInverse()*shape.GlobalTransform;
                Rect2 local=shape.Shape.GetRect();
                Rect2 a=parentWorld*start*relative*local,b=parentWorld*end*relative*local;
                Add("linear:"+Path(shape),ResidentKeepOutKind.MovingOccupancy,a.Merge(b));
            }
        }
        void Arc(CanyonPendulumHazard body)
        {
            if (!float.IsFinite(body.MaxAngleDegrees)) { issues.Add("invalid_motion:"+Path(body));return; }
            if (!MotionParts(body,out Transform2D parentWorld,out CollisionShape2D[] shapes)) return;
            double span=Math.Min(Math.Abs((double)body.MaxAngleDegrees),180d)*Math.PI/180d;
            Transform2D stretch=new(0,body.Scale,body.Skew,Vector2.Zero);
            Vector2 origin=parentWorld*body.Position;
            foreach (CollisionShape2D shape in shapes)
            {
                if (!ShapeValid(shape)) continue;
                Transform2D relative=body.GlobalTransform.AffineInverse()*shape.GlobalTransform;
                Rect2 box=shape.Shape.GetRect();
                Vector2 minimum=new(float.PositiveInfinity,float.PositiveInfinity),maximum=new(float.NegativeInfinity,float.NegativeInfinity);
                foreach (Vector2 corner in new[] {box.Position,new Vector2(box.End.X,box.Position.Y),box.End,new Vector2(box.Position.X,box.End.Y)})
                {
                    // Rotation is applied AFTER the body's scale/skew, BEFORE
                    // ancestor axes. This also handles non-uniform ancestors.
                    Vector2 p=stretch*(relative*corner);
                    var angles=new List<double> {-span,span};
                    Extremes(parentWorld.X.X*p.X+parentWorld.Y.X*p.Y,-parentWorld.X.X*p.Y+parentWorld.Y.X*p.X);
                    Extremes(parentWorld.X.Y*p.X+parentWorld.Y.Y*p.Y,-parentWorld.X.Y*p.Y+parentWorld.Y.Y*p.X);
                    foreach (double angle in angles)
                    {
                        double cosine=Math.Cos(angle),sine=Math.Sin(angle);
                        var rotated=new Vector2((float)(p.X*cosine-p.Y*sine),(float)(p.X*sine+p.Y*cosine));
                        Vector2 point=origin+parentWorld.X*rotated.X+parentWorld.Y*rotated.Y;
                        minimum=minimum.Min(point);maximum=maximum.Max(point);
                    }
                    void Extremes(double a,double b)
                    {
                        double first=Math.Atan2(b,a);
                        for (int k=-2;k<=2;k++) { double angle=first+k*Math.PI;if (angle>=-span&&angle<=span) angles.Add(angle); }
                    }
                }
                Add("pendulum:"+Path(shape),ResidentKeepOutKind.MovingOccupancy,new Rect2(minimum,maximum-minimum));
            }
        }
    }

    private static bool Positive(Rect2 box)=>box.Position.IsFinite()&&box.Size.IsFinite()&&box.Size.X>0&&box.Size.Y>0;
    private static bool Valid(Transform2D pose)=>pose.IsFinite()&&float.IsFinite(pose.Determinant())&&MathF.Abs(pose.Determinant())>.000001f;
    private static CollisionObject2D NearestBody(Node node)
    {
        for (Node parent=node.GetParent();parent!=null;parent=parent.GetParent())
            if (parent is CollisionObject2D body) return body;
        return null;
    }
    private static IEnumerable<Node> Walk(Node root)
    {
        yield return root;
        using Godot.Collections.Array children=(Godot.Collections.Array)root.GetChildren();
        for (int i=0;i<children.Count;i++) foreach (Node child in Walk(children[i].AsGodotObject() as Node)) yield return child;
    }
}
