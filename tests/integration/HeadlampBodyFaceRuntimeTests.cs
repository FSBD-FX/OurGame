using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Presentation.Fog;

namespace Knight.Tests.Integration;

/// <summary>Only a contained terminal face is exempt, never an entire moving body.</summary>
public static class HeadlampBodyFaceRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        MethodInfo method=typeof(HeadlampOcclusionFrame).GetMethod("CanSeeBodyFacePatch",
            [typeof(Vector2),typeof(HeadlampOccluder),typeof(Vector2),typeof(Vector2),typeof(float)]);
        TestAssert.True(method?.ReturnType==typeof(bool),
            "rotating native bodies need a contained terminal-face visibility contract, not an AABB or blanket body exclusion");
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        var world=new Node2D();
        var body=new StaticBody2D { Position=new Vector2(3456.125f,987.25f),Rotation=.57f,
            Scale=new Vector2(-1.15f,.8f),CollisionLayer=CombatCollisionLayers.World };
        world.AddChild(body); tree.Root.AddChild(world);
        uint owner=body.CreateShapeOwner(body);
        body.ShapeOwnerSetTransform(owner,new Transform2D(.19f,new Vector2(11,-7)));
        body.ShapeOwnerAddShape(owner,new RectangleShape2D { Size=new Vector2(120,40) });
        using var sampler=new HeadlampOcclusionSampler(world);
        try
        {
            await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
            HeadlampOcclusionFrame frame=sampler.Capture();
            TestAssert.True(frame.IsComplete && frame.Shapes.Count==1 && frame.PhysicsQueries==0,
                "fixture uses one exact original native owner, with no CollisionShape child or added queries");
            HeadlampOccluder surface=frame.Shapes[0];
            Transform2D transform=surface.WorldToLocal.AffineInverse();
            bool See(Vector2 actor,Vector2 from,Vector2 to,float depth=2,HeadlampOccluder? target=null,
                HeadlampOcclusionFrame snapshot=null)
            {
                try { return (bool)method.Invoke(snapshot??frame,[transform*actor,target??surface,from,to,depth]); }
                catch(TargetInvocationException error) when(error.InnerException!=null)
                { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
            }
            foreach(var face in new[] {
                (new Vector2(-90,0),new Vector2(-60,-8),new Vector2(-60,8)),
                (new Vector2(90,0),new Vector2(60,-8),new Vector2(60,8)),
                (new Vector2(0,-50),new Vector2(-8,-20),new Vector2(8,-20)),
                (new Vector2(0,50),new Vector2(-8,20),new Vector2(8,20)) })
            {
                TestAssert.True(See(face.Item1,face.Item2,face.Item3),
                    "each local rectangle face remains visible under rotation, negative scale and native owner offset");
                TestAssert.True(See(face.Item1,face.Item3,face.Item2),"face endpoint order cannot change visibility");
                TestAssert.True(!frame.CanSee(transform*face.Item1,transform*((face.Item2+face.Item3)*.495f)),
                    "ordinary LOS correctly contacts the solid, proving a terminal patch exception is necessary");
                TestAssert.True(!See(Vector2.Zero,face.Item2,face.Item3) &&
                    !See(-face.Item1,face.Item2,face.Item3),"inside and opposite-side actors cannot promote a far face");
            }
            TestAssert.True(!See(new(-90,0),new(-59,-8),new(-59,8)) &&
                !See(new(-90,0),new(-60,-21),new(-60,8)) &&
                !See(new(-90,0),new(-60,-8),new(-59,8)) &&
                !See(new(-90,0),new(-60,0),new(-60,0)),
                "interior marks, out-of-bounds ends, diagonals and degenerate marks are not terminal faces");
            foreach(float depth in new[] { 0f,-1f,8.01f,121f,float.NaN,float.PositiveInfinity })
                TestAssert.True(!See(new(-90,0),new(-60,-8),new(-60,8),depth),"invalid/excessive patch depth fails closed");
            TestAssert.True(!See(new(float.NaN,0),new(-60,-8),new(-60,8)) &&
                !See(new(-90,0),new(float.NaN,0),new(-60,8)),"nonfinite actor/patch fails closed");
            TestAssert.True(!See(new(-90,0),new(-60,-8),new(-60,8),target:surface with { ShapeIndex=1 }) &&
                !See(new(-90,0),new(-60,-8),new(-60,8),target:surface with { BodyInstanceId=0 }),
                "unproven owner/index and zero body identity cannot authorize a face");
            TestAssert.True(!See(new(-90,0),new(-60,-8),new(-60,8),snapshot:
                new HeadlampOcclusionFrame([surface],false,"fixture-incomplete")),"incomplete geometry fails closed");

            // Another shape of the SAME body/owner is still a real blocker.
            body.ShapeOwnerAddShape(owner,new RectangleShape2D { Size=new Vector2(150,2) });
            HeadlampOcclusionFrame blocked=sampler.Capture();
            TestAssert.True(!See(new(-90,0),new(-60,-8),new(-60,8),snapshot:blocked),
                "a second native shape index in the same owner/body blocks the full face hull");
            body.ShapeOwnerRemoveShape(owner,1);
            uint other=body.CreateShapeOwner(body);
            body.ShapeOwnerSetTransform(other,body.ShapeOwnerGetTransform(owner)*new Transform2D(0,new Vector2(-75,0)));
            body.ShapeOwnerAddShape(other,new RectangleShape2D { Size=new Vector2(2,20) });
            TestAssert.True(!See(new(-90,0),new(-60,-8),new(-60,8),snapshot:sampler.Capture()),
                "a second native owner in the same body is not blanket-excluded");
            body.RemoveShapeOwner(other);

            // Needle lies between the endpoint rays, inside the actual drawn quad's hull.
            var needle=new StaticBody2D { Transform=transform*new Transform2D(0,new Vector2(-70,.1f)),
                CollisionLayer=CombatCollisionLayers.World };
            needle.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(.2f,.2f) } });
            world.AddChild(needle);
            TestAssert.True(!See(new(-90,0),new(-60,-8),new(-60,8),snapshot:sampler.Capture()),
                "subpixel blocker between endpoint rays occludes the entire actual patch hull");
            world.RemoveChild(needle); needle.Free();
            body.Position+=new Vector2(300,0);
            TestAssert.True(!See(new(-90,0),new(-60,-8),new(-60,8),snapshot:sampler.Capture()),
                "old transform snapshot member cannot authorize paint on a translated body");
            TestAssert.True(See(new(-90,0),new(-60,-8),new(-60,8)),"the old frame remains immutable after movement");
        }
        finally { world.GetParent()?.RemoveChild(world); world.Free(); }
        GD.Print("HEADLAMP_NATIVE_BODY_FACE_PATCH_OK physics_queries=0");
    }
}
