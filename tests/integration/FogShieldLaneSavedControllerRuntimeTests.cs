using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Prologue.Traversal;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

/// <summary>Actual saved source, mounted collector and flow lifecycle; controlled positions, not traversal.</summary>
public static class FogShieldLaneSavedControllerRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        try
        {
            var paths=IsolatedPrologueFixture.Configure(level,"fog-saved-shield-lane");
            var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
            tree.Root.AddChild(level); await Frames(tree,5);
            var cues=level.GetNodeOrNull<FogShieldLaneReadabilityOverlay>("ProtectedCues/ShieldLane");
            TestAssert.True(cues!=null,"saved formal parent needs the actual persistent native damage boundary on layer2");
            var layer=level.GetNode<CanvasLayer>("ProtectedCues");
            TestAssert.True(layer.Layer==2 && layer.FollowViewportEnabled && layer.FollowViewportScale==1f &&
                cues.PresentedLaneCount==0 && cues.LastOcclusionFrame==null,"Title does not expose a dormant damage lane");
            flow.StartNew(); await Frames(tree,5);
            ulong playerId=level.Player.GetInstanceId();
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(2260,3810)),
                "original checkpoint adapter mounts Basin for this presentation fixture");
            await Frames(tree,8);
            var lane=level.CurrentSectorRoot.GetNode<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
            TestAssert.Equal(new Vector2(2260,3930),lane.GlobalPosition,"original saved lane world position is unchanged");
            TestAssert.Equal(new Vector2(260,120),((RectangleShape2D)lane.GetNode<CollisionShape2D>("Collision").Shape).Size,
                "original saved native damage dimensions are unchanged");
            TestAssert.True(lane.CollisionLayer==0 && lane.CollisionMask==2 && lane.Monitoring && lane.IsPhysicsProcessing(),
                "real mounted source remains the original running layer-zero body polling hazard");
            var controller=level.GetNode<PrologueFogController>("FogController");
            Position(level,new Vector2(2260,3810)); await Frames(tree,3);
            Current(level,cues,controller,playerId,"original mounted lane");
            var guide=level.GetNode<PrologueGuidanceController>("Guidance");
            var completion=level.GetNode<PrologueCompletionController>("Completion");
            var objective=completion.CurrentObjective;
            string tutorials=string.Join("|",level.Progress.CompletedTutorials.OrderBy(id=>id.ToString()));
            string savedTutorials=ReadSavedTutorials(flow.SavePath);
            Guide(level,true,"visible nearby lane");
            using(Image actual=tree.Root.GetTexture().GetImage())
            {
                // Reference renderer: suppress only the competing ordinary
                // guidance tick, not fog publication or the actual scene/input.
                guide.SetProcess(false); await Frames(tree,3);
                Guide(level,true,"reference lane card without the competing early tick");
                Capture(tree,paths.CaseRoot,"guidance-raster-candidate.png");
                using Image expected=tree.Root.GetTexture().GetImage();
                var card=level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
                Rect2 rect=card.GetNode<Label>("Text").GetGlobalRect();
                int changed=0;
                for(int y=Math.Max(0,(int)rect.Position.Y);y<Math.Min(actual.GetHeight(),(int)rect.End.Y);y++)
                for(int x=Math.Max(0,(int)rect.Position.X);x<Math.Min(actual.GetWidth(),(int)rect.End.X);x++)
                {
                    Color a=actual.GetPixel(x,y),b=expected.GetPixel(x,y);
                    bool letterA=a.R>.65f && a.G>.6f && a.B>.4f;
                    bool letterB=b.R>.65f && b.G>.6f && b.B>.4f;
                    if((letterA || letterB) && Mathf.Abs(a.R-b.R)+Mathf.Abs(a.G-b.G)+Mathf.Abs(a.B-b.B)>.25f) changed++;
                }
                guide.SetProcess(true);
                TestAssert.True(changed<80,
                    $"actual lane guidance glyph pixels replace generic text, not only Label.Text; changed={changed}");
            }
            await Frames(tree,3);
            Capture(tree,paths.CaseRoot,"shield-lane-guidance-cn.png");
            guide.SetLanguage(GameLanguage.English); await Frames(tree,2);
            Guide(level,true,"English card fits");
            Capture(tree,paths.CaseRoot,"shield-lane-guidance-en.png");
            guide.SetLanguage(GameLanguage.ChineseSimplified); await Frames(tree,2);
            using(var keyboard=KeyboardRouteInputDriver.Begin(level.Player))
            {
                keyboard.Command(InputActions.MoveRight,true);
                keyboard.Command(InputActions.KeyboardShield,true);
                await Frames(tree,4);
                TestAssert.True(level.Player.GetNode<PlayerShieldController>("Shield").IsActive,
                    "real engine keyboard input activates the original shield");
                Guide(level,true,"holding the shield must not restore the unrelated claw card");
                keyboard.ReleaseAll(); await Frames(tree,2);
            }
            guide.SetGuidanceEnabled(false); await Frames(tree,2); Guide(level,false,"guidance disabled");
            guide.SetGuidanceEnabled(true); await Frames(tree,2); Guide(level,true,"guidance enabled");
            cues.Visible=false; await Frames(tree,2); Guide(level,false,"hidden cue cannot authorize help");
            cues.Visible=true; await Frames(tree,2); Guide(level,true,"actual cue visible again");
            layer.Visible=false; await Frames(tree,2); Guide(level,false,"hidden cue layer cannot authorize help");
            layer.Visible=true; await Frames(tree,2); Guide(level,true,"actual cue layer visible again");
            level.RemoveChild(controller);
            try
            {
                await Frames(tree,3); Guide(level,false,"missing fog publisher never leaves a cached lane card");
            }
            finally
            {
                // Reparent even on a failed assertion so the owning level's
                // existing teardown also releases this temporarily detached node.
                if(GodotObject.IsInstanceValid(controller) && !controller.IsInsideTree()) level.AddChild(controller);
            }
            await Frames(tree,3); Guide(level,true,"re-entered fog publisher restores current evidence");
            lane.Visible=false; await Frames(tree,2); Guide(level,false,"hidden original source");
            lane.Visible=true; await Frames(tree,2); Guide(level,true,"visible source returns");
            lane.Monitoring=false; await Frames(tree,2); Guide(level,false,"monitoring stopped");
            lane.Monitoring=true; await Frames(tree,2); Guide(level,true,"monitoring restored");
            var nativeCollision=lane.GetNode<CollisionShape2D>("Collision");
            nativeCollision.Disabled=true; await Frames(tree,2); Guide(level,false,"native collision disabled");
            nativeCollision.Disabled=false; await Frames(tree,2); Guide(level,true,"native collision enabled");
            lane.Damage=0; await Frames(tree,2); Guide(level,false,"source has no damage");
            lane.Damage=1; await Frames(tree,2); Guide(level,true,"original damage restored");
            Position(level,new Vector2(2260,3500)); await Frames(tree,3); Guide(level,false,"visible but not nearby");
            Position(level,new Vector2(2000,3810)); await Frames(tree,3); Guide(level,true,"left approach");
            // Synthetic blocker only in this presentation fixture: no authored terrain is edited.
            var wall=new StaticBody2D { Name="GuidanceOcclusionFixture",CollisionLayer=1,CollisionMask=0 };
            using(var shape=new RectangleShape2D { Size=new Vector2(20,800) })
            {
                wall.AddChild(new CollisionShape2D { Shape=shape });
                level.GetNode<Node2D>("WorldRoot").AddChild(wall); wall.GlobalPosition=new Vector2(2090,3930);
                try
                {
                    await Frames(tree,3); Guide(level,false,"fully blocked lane cannot advertise a hidden route");
                }
                finally { wall.Free(); }
            }
            await Frames(tree,3); Guide(level,true,"removing actual blocker restores local help");
            Position(level,new Vector2(2260,3810)); await Frames(tree,3);
            TestAssert.True(completion.CurrentObjective==objective && tutorials==
                string.Join("|",level.Progress.CompletedTutorials.OrderBy(id=>id.ToString())) &&
                savedTutorials==ReadSavedTutorials(flow.SavePath),
                "temporary danger help never completes or advances a saved tutorial");
            // Controlled restore only in this fixture. The transition itself
            // must come from the actual shield rising edge, not a model write.
            completion.RestoreSession(PrologueObjective.LearnShield); await Frames(tree,2);
            TestAssert.Equal(PrologueObjective.LearnShield,completion.CurrentObjective,
                "visible card alone does not complete the pending shield lesson");
            Guide(level,true,"actual pending shield objective");
            using(var keyboard=KeyboardRouteInputDriver.Begin(level.Player))
            {
                keyboard.Command(InputActions.MoveRight,true);
                keyboard.Command(InputActions.KeyboardShield,true); await Frames(tree,4);
                TestAssert.Equal(PrologueObjective.ClimbCanyon,completion.CurrentObjective,
                    "actual shield input still completes the original shield lesson");
                Guide(level,true,"local help survives the real LearnShield to Climb edge");
                keyboard.ReleaseAll(); await Frames(tree,2);
            }
            lane.SetPhysicsProcess(false); await Frames(tree,2);
            TestAssert.Equal(0,cues.PresentedLaneCount,"stopping actual mounted polling withdraws danger without disabling its geometry");
            Guide(level,false,"stopped physics withdraws lane guidance too");
            lane.SetPhysicsProcess(true); await Frames(tree,2); Current(level,cues,controller,playerId,"restarted original polling");
            lane.Monitorable=false; await Frames(tree,2); Current(level,cues,controller,playerId,"Monitorable is not this source's damage switch");
            lane.Monitorable=true;
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu"); pause.Open(); await Frames(tree,2);
            TestAssert.True(cues.PresentedLaneCount==0 && cues.LastOcclusionFrame==null,"real pause clears lane commands and provenance");
            Guide(level,false,"pause withdraws the contextual prompt");
            pause.Execute(ProloguePauseCommand.Resume); await Frames(tree,2); Current(level,cues,controller,playerId,"actual resume");
            ulong retiredId=lane.GetInstanceId();
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.LostTemple,new Vector2(5920,1080)),
                "actual legal temple checkpoint stream retires the Basin source");
            await Frames(tree,8);
            TestAssert.True(!world.TryGetMountedSector(PrologueSectorId.CanyonBasin,out _) && cues.PresentedLaneCount==0,
                "retired instance is not retained by the collector");
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin,new Vector2(2260,3810)),
                "actual adapter remounts original Basin rather than a copied hazard");
            await Frames(tree,8); Position(level,new Vector2(2260,3810)); await Frames(tree,3);
            lane=level.CurrentSectorRoot.GetNode<CanyonShieldLaneHazard>("Gameplay/ShieldLane");
            TestAssert.True(lane.GetInstanceId()!=retiredId,"stream remount is a fresh actual source, not stale cached identity");
            Current(level,cues,controller,playerId,"freshly remounted original lane");
            // Actual saved player/field screenshots for the above-fog clarity
            // change. Controlled poses with the existing disabled motor, not
            // movement or jump evidence; original damage polling stays enabled.
            foreach(var shot in new[] {
                (new Vector2(2180,3960),"coil-actor-entry-cn.png"),
                (new Vector2(2260,3960),"coil-actor-center-cn.png"),
                (new Vector2(2300,3910),"coil-actor-upper-cn.png") })
            {
                Position(level,shot.Item1); await Frames(tree,2);
                Current(level,cues,controller,playerId,"actor inside current clarity capture");
                TestAssert.True(lane.Monitoring && lane.IsPhysicsProcessing() && lane.Damage==1 &&
                    level.Player.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth>0,
                    "clarity capture retains an alive real actor and active original damage field");
                Capture(tree,paths.CaseRoot,shot.Item2);
            }
            Position(level,new Vector2(2260,3810)); await Frames(tree,2);
            flow.ReturnToTitle(); await Frames(tree,2);
            TestAssert.True(cues.PresentedLaneCount==0 && cues.LastOcclusionFrame==null &&
                !level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,"ReturnToTitle withdraws danger and leaves unfinished formal fog hidden");
            GD.Print("HEADLAMP_FOG_CONTROLLER_SAVED_SHIELD_LANE_OK pixels=false fixture=true traversal=false formal_visible=false");
            GD.Print("SHIELD_LANE_GUIDANCE_RUNTIME_OK languages=true shield_held=true geometry=true pause=true no_progress_mutation=true fixture=true OS_origin=false");
        }
        finally
        {
            tree.Paused=false;
            if(GodotObject.IsInstanceValid(level))
            {
                if(level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level);
                else level.Free();
            }
        }
    }
    private static void Position(PrologueLevelController level,Vector2 position)
    {
        level.Player.GlobalPosition=position; level.Player.Velocity=Vector2.Zero;
        level.Player.GetNode<Camera2D>("Camera2D").ResetSmoothing();
    }
    private static void Guide(PrologueLevelController level,bool expected,string label)
    {
        var guide=level.GetNode<PrologueGuidanceController>("Guidance");
        var visual=level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        bool current=guide.CurrentInstruction.ContextTitle is "持续电磁场" or "LIVE CURRENT";
        TestAssert.True(current==expected,$"{label}: lane card expected={expected} actual={guide.CurrentInstruction}");
        if(!expected) return;
        Label text=visual.GetNode<Label>("Text");
        TestAssert.True(visual.IsVisibleInTree() && visual.HasVisiblePrompt && visual.RenderedTextContained &&
            visual.RenderedStepText==guide.CurrentInstruction.StepText && visual.RenderedTargetCount==0 &&
            guide.CurrentInstruction.Targets.Count==0 && guide.CurrentInstruction.HintText=="" &&
            text.Text.Contains(guide.CurrentInstruction.StepText,StringComparison.Ordinal) &&
            text.GetMinimumSize().Y<=text.Size.Y+.5f && visual.PromptRect.Grow(.5f).Encloses(text.GetRect()),
            $"{label}: full live card is readable without a map marker or clipped text");
    }
    private static void Capture(SceneTree tree,string root,string name)
    {
        string path=Path.Combine(root,name);
        TestAssert.True(!File.Exists(path),"guidance screenshots never overwrite prior evidence");
        using Image image=tree.Root.GetTexture().GetImage();
        TestAssert.Equal(Error.Ok,image.SavePng(path),"retain actual saved-scene guidance pixels");
    }
    private static string ReadSavedTutorials(string path)
    {
        using JsonDocument save=JsonDocument.Parse(File.ReadAllText(path));
        return string.Join("|",save.RootElement.GetProperty("completedTutorials").EnumerateArray()
            .Select(item=>item.GetString()).OrderBy(id=>id,StringComparer.Ordinal));
    }
    private static void Current(PrologueLevelController level,FogShieldLaneReadabilityOverlay cues,
        PrologueFogController controller,ulong playerId,string label)
    {
        TestAssert.True(cues.PresentedLaneCount==1 && ReferenceEquals(cues.LastOcclusionFrame,controller.LastOcclusionFrame) &&
            level.Player.GetInstanceId()==playerId && level.Player.GetNode<PlayerPresentationCoordinator>("Presentation").FrameObserverFailureCount==0 &&
            level.GetNode<PrologueFogCanvas>("Fog/Canvas").Visible,
            $"{label}: same real player, current native frame and single actual lane; count={cues.PresentedLaneCount} player={level.Player.GlobalPosition}");
    }
    private static async Task Frames(SceneTree tree,int count)
    {
        for(int index=0;index<count;index++)
        {
            await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        }
    }
}
