using System;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Player;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static class FirstGateEntryTerrainRuntimeTests
{
    public static async Task Run(SceneTree tree,string savedLeaf)
    {
        await Route(tree,savedLeaf,false);
        await Route(tree,savedLeaf,true);
        await Route(tree,savedLeaf,false,catchRoute:true);
    }
    private static async Task Route(SceneTree tree,string savedLeaf,bool branch,bool catchRoute=false)
    {
        var root=new Node2D {Name=branch?"EntryBranchAProof":"EntryMainProof"};
        var packed=GD.Load<PackedScene>(savedLeaf);
        var terrain=packed.Instantiate<Node2D>();root.AddChild(terrain);
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position=FirstGateEntryTerrain.Spawn; // Sole initial position; never changed after entering tree.
        player.GetNode<Camera2D>("Camera2D").Enabled=false;
        root.AddChild(player);
        var input=player.GetNode<PlayerInputReader>("Input");
        var health=player.GetNode<HealthComponent>("Health");
        var motor=player.GetNode<PlayerMotor>("Motor");
        int injuries=0; health.Damaged+=OnDamage;
        void OnDamage(int remaining,Vector2 impulse)=>injuries++;
        KeyboardRouteInputDriver driver=null;
        ulong identity=0;int ticks=0;
        tree.Root.AddChild(root);
        try
        {
            identity=player.GetInstanceId();driver=KeyboardRouteInputDriver.Begin(player);
            async Task Step()
            {
                await KeyboardActionRuntimeTests.Frames(tree,input,1);driver.ObserveFrame();ticks++;
                TestAssert.True(ticks<=2100 && health.CurrentHealth>0 && player.GetInstanceId()==identity,
                    $"continuous entry route stays alive/same actor/bounded, branch={branch} ticks={ticks} pos={player.Position}");
            }
            async Task Until(Func<bool> reached,int budget,string label)
            {
                int n=0;while(!reached() && n++<budget)await Step();
                GD.Print($"ENTRY_ROUTE_OBSERVATION branch={branch} stage={label} pos={player.Position} floor={player.IsOnFloor()} hp={health.CurrentHealth} ticks={ticks}");
                TestAssert.True(reached(),$"ENTRY_ROUTE: {label} reachable under real keyboard/physics, branch={branch} pos={player.Position}");
            }
            async Task Move(bool right,float x,string label)
            {
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,true);
                await Until(()=>right?player.Position.X>=x:player.Position.X<=x,700,label);
                driver.Command(right?InputActions.MoveRight:InputActions.MoveLeft,false);
            }
            await Until(()=>player.IsOnFloor(),60,"01-authored-spawn-support");
            foreach(var (from,to,want,label) in new[]{
                (new Vector2(192,400),new Vector2(192,600),new Vector2(192,512),"outdoor floor"),
                (new Vector2(600,1100),new Vector2(600,800),new Vector2(600,928),"03 native roof"),
                (new Vector2(200,1450),new Vector2(0,1450),new Vector2(128,1450),"turn native wall"),
                (new Vector2(2800,1600),new Vector2(3000,1600),new Vector2(2880,1600),"sealed04 boundary")})
            {
                using var query=PhysicsRayQueryParameters2D.Create(from,to,CombatCollisionLayers.World);
                var hit=root.GetWorld2D().DirectSpaceState.IntersectRay(query);
                TestAssert.True(hit.Count>0 && hit["position"].AsVector2().DistanceTo(want)<.2f,"native enclosure ray "+label);
            }
            if(branch)
            {
                await Move(true,1510,"A-grotto-entry");
                await Until(()=>player.IsOnFloor() && player.Position.Y is >850 and <870,90,"A-grotto-support");
                await Move(true,2070,"A-shaft");
                await Until(()=>player.IsOnFloor() && player.Position.Y>1180,120,"A-lower-rejoin-corridor");
            }
            else
            {
                await Move(true,1160,"02-rift");
                await Until(()=>player.IsOnFloor() && player.Position.Y>800,120,"02-rift-support");
            }
            await Move(false,608,"03-leftward-side-cave");
            await Until(()=>player.IsOnFloor() && player.Position.Y is >1200 and <1230,120,"03-side-cave-support");
            await Move(false,280,"03-lower-turn");
            await Until(()=>player.IsOnFloor() && player.Position.Y is >1650 and <1670,120,"04-lower-beam-support");
            await Move(true,1605,"04-gap-takeoff");
            await Until(()=>player.IsOnFloor() && Math.Abs(player.Velocity.X)<1,30,"04-braked-takeoff");
            if(catchRoute)
            {
                await Move(true,1660,"04-deliberately-walk-into-catch");
                await Until(()=>player.IsOnFloor() && player.Position.Y is >1810 and <1830,120,"04-native-catch-support");
                driver.Command(InputActions.MoveRight,true);driver.Command(InputActions.Jump,true);
                await Until(()=>player.IsOnFloor() && player.Position.Y is >1730 and <1750,100,"04-first-grounded-catch-step");
                driver.Command(InputActions.Jump,false);driver.Command(InputActions.MoveRight,false);
                for(int i=0;i<4;i++)await Step();
            }
            driver.Command(InputActions.MoveRight,true);driver.Command(InputActions.Jump,true);
            await Until(()=>player.Position.X>1780 && player.IsOnFloor() && player.Position.Y<1670,120,
                catchRoute?"04-second-grounded-return-jump":"04-single-jump128px-gap");
            driver.Command(InputActions.Jump,false);
            await Until(()=>player.Position.X>=2660 && player.IsOnFloor(),500,"04-physical-end-before-future-section05");
            driver.Command(InputActions.MoveRight,false);
            int expectedJumps=catchRoute?2:1;
            TestAssert.True(motor.AcceptedJumpCount==expectedJumps && motor.AcceptedDashCount==0 && injuries==0 && health.CurrentHealth==5,
                $"entry route uses one real jump/no dash/no hidden death or recovery, branch={branch} jumps={motor.AcceptedJumpCount} injuries={injuries}");
            GD.Print($"ENTRY_TERRAIN_ROUTE_OK branch={branch} catch={catchRoute} same_player={identity} ticks={ticks} hp=5 injuries=0 jumps={expectedJumps} dash=0 teleports=0 campaign_complete=false human=false");
        }
        finally
        {
            driver?.Dispose();health.Damaged-=OnDamage;
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,root);
        }
    }
}
