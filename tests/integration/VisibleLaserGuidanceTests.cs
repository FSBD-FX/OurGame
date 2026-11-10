using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Localization;
using Knight.Player;
using Knight.Presentation;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Tests.Integration;

public static class VisibleLaserGuidanceTests
{
    public static async Task Run(SceneTree tree)
    {
        TestAssert.Equal("headless",DisplayServer.GetName(),"semantic-only laser fixture requires the actual headless display backend");
        RunModel();
        await RunNative(tree);
    }

    // Actual saved controller/collector and generated enemy. Poses/objective and
    // unrelated actors are isolated here; not traversal, natural rendering or pixels.
    private static async Task RunNative(SceneTree tree)
    {
        var level=GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        PackedScene actorScene=null;
        var owned=new HashSet<Resource>();
        var inputEvents=InputMap.ActionGetEvents(InputActions.KeyboardShield);
        using var inputEventsOwner=(Godot.Collections.Array)inputEvents;
        var fog=level.GetNode<PrologueFogController>("FogController");
        var guide=level.GetNode<PrologueGuidanceController>("Guidance");
        var cues=level.GetNode<FogReadabilityOverlay>("ProtectedCues/Attacks");
        var flow=level.GetNode<PrologueGameFlowController>("GameFlow");
        var world=level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        var completion=level.GetNode<PrologueCompletionController>("Completion");
        try
        {
            IsolatedPrologueFixture.Configure(level,"visible-laser-echo-headless");
            flow.HeadlessAutoStart=false; flow.AutomationAutoStart=false;
            tree.Root.AddChild(level);
            await Frames(3);
            TestAssert.True(flow.State==PrologueGameFlowState.Title && cues.ReadVisibleLaser(fog.LastOcclusionFrame)==VisibleLaserWarning.None,
                "real isolated saved title has no visible laser evidence");
            flow.StartNew(); await Frames(3);
            TestAssert.True(world.RestoreCheckpoint("canyon.v2.temple-forecourt"),"actual continuous world restores its authored Temple checkpoint for UI staging");
            await Frames(8);
            TestAssert.True(level.CurrentSector==PrologueSectorId.LostTemple && flow.State==PrologueGameFlowState.Playing,
                "semantic fixture is in actual saved LostTemple, not a Basin-only context");
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            level.Player.GlobalPosition=new Vector2(5880,1080); level.Player.Velocity=Vector2.Zero;
            var camera=level.Player.GetNode<Camera2D>("Camera2D"); camera.Enabled=true; camera.ResetSmoothing(); camera.ForceUpdateScroll();
            completion.RestoreSession(PrologueObjective.PullTempleMechanism);
            // Isolate only this test's source; do not change the production map or population.
            foreach(var id in world.MountedSectorIds)
                if(world.TryGetMountedSector(id,out var sector) && sector.GetNodeOrNull("Gameplay/Actors") is { } actors)
                {
                    var children=actors.GetChildren(); using var childrenOwner=(Godot.Collections.Array)children;
                    foreach(Node child in children) child.Free();
                }
            actorScene=ResourceLoader.Load<PackedScene>("res://scenes/enemies/PrismLaserGunner.tscn",cacheMode:ResourceLoader.CacheMode.Ignore);
            var actor=actorScene.Instantiate<RangedWeaponEnemy>();
            owned.Add(actor.Settings); FogRangedSavedControllerRuntimeTests.CollectShapes(actor,owned);
            actor.Name="ECHORealLaser";
            var container=level.CurrentSectorRoot.GetNode<Node2D>("Gameplay/Actors");
            actor.Position=container.ToLocal(new Vector2(5720,1073));
            actor.ConfigureEncounterEnvelope(new Rect2(4800,700,2200,1000)); container.AddChild(actor);
            // Retain the actual closed-door failure as a native negative control.
            // UI staging then places both parties on its east side; no door,
            // production actor, LOS rule or route contract is changed.
            var fixtureExclude=new Godot.Collections.Array<Rid>{actor.GetRid()};
            using(var fixtureExcludeOwner=(Godot.Collections.Array)fixtureExclude)
            {
                var sourceSpace=actor.GetWorld2D().DirectSpaceState;
                Vector2 oldMuzzle=actor.GetNode<Node2D>("Muzzle").GlobalPosition;
                using var blockedRay=PhysicsRayQueryParameters2D.Create(oldMuzzle,level.Player.GlobalPosition,CombatCollisionLayers.World,fixtureExclude);
                blockedRay.CollideWithBodies=true; blockedRay.CollideWithAreas=false; blockedRay.HitFromInside=true;
                using var blockedHit=sourceSpace.IntersectRay(blockedRay);
                TestAssert.True(blockedHit.ContainsKey("collider") && blockedHit["collider"].AsGodotObject()==
                    level.CurrentSectorRoot.GetNode<StaticBody2D>("Gameplay/Mechanisms/BellChain/Door") &&
                    !RangedAttackGeometry.HasClearWorldPath(sourceSpace,oldMuzzle,level.Player.GlobalPosition,fixtureExclude),
                    "old staged laser is genuinely blocked by the unchanged saved BellChain door");
                actor.GlobalPosition=new Vector2(6000,1073);
                TestAssert.True(RangedAttackGeometry.HasClearWorldPath(sourceSpace,actor.GetNode<Node2D>("Muzzle").GlobalPosition,level.Player.GlobalPosition,fixtureExclude),
                    "semantic staging stays on the same saved floor and clear side of the real door");
            }
            int sourceSteps=0;
            ulong physicsBefore=Engine.GetPhysicsFrames();
            void ObserveSourceStep(RangedWeaponFrame _)=>sourceSteps++;
            actor.FrameChanged+=ObserveSourceStep;
            try
            {
                await Until(()=>actor.CurrentFrame.AttackTimeline.Phase==AttackPhase.Telegraph && !actor.CurrentFrame.AttackTimeline.HasLockedTarget,60);
            }
            catch
            {
                // Failure-only independent observations; not AI inputs, new
                // visibility authorization, or a reason to extend the wait.
                try { LogSourcePremise(actor,camera,sourceSteps,physicsBefore); }
                catch(Exception diagnostic)
                { GD.Print($"VISIBLE_LASER_SOURCE_PREMISE_FAULT type={diagnostic.GetType().Name} original_failure_preserved=true"); }
                throw;
            }
            finally { actor.FrameChanged-=ObserveSourceStep; }
            var leaves=new List<AttackTelegraphVisual>(); actor.AppendAuthorizedAttackLeaves(leaves);
            TestAssert.True(leaves.Count==1 && leaves[0].CurrentFrame.Timeline.Kind==EnemyAttackKind.PrismLaserBeam &&
                leaves[0].CurrentFrame.Shape.Kind==AttackShapeKind.Capsule &&
                fog.LastOcclusionFrame?.IsComplete==true && ReferenceEquals(cues.LastOcclusionFrame,fog.LastOcclusionFrame) &&
                fog.LastOcclusionFrame.CanSee(level.Player.GlobalPosition,leaves[0].GlobalPosition) &&
                cues.GetChildren().OfType<AttackTelegraphVisual>().Any(c=>c.CurrentFrame.Timeline.Kind==EnemyAttackKind.PrismLaserBeam),
                "native premise: actual authorized generated warning is collected through the saved world and complete current snapshot");
            PrologueObjective objective=completion.CurrentObjective;
            AssertWarning(VisibleLaserWarning.Tracking,"VISIBLE_LASER_RUNTIME_RED: actual tracking warning must reach current Temple ECHO");
            var formalHud=level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            var unobstructedHud=formalHud.CurrentModel;
            foreach(var blockedHud in new[]{unobstructedHud with {RecoveryVisible=true},
                unobstructedHud with {CompletionVisible=true},unobstructedHud with {BossVisible=true}})
            {
                Submit(); formalHud.Present(blockedHud); guide._Process(0);
                TestAssert.True(!IsLaser(),"VISIBLE_LASER_HOLD_RED: actual recovery/completion/Boss HUD must revoke the prior pre-draw card before its one-use hold");
                formalHud.Present(unobstructedHud);
            }
            Submit();
            foreach(GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                guide.SetLanguage(language);
                InputMap.ActionEraseEvents(InputActions.KeyboardShield);
                using(var changed=new InputEventKey{PhysicalKeycode=Key.H,Keycode=Key.H}) InputMap.ActionAddEvent(InputActions.KeyboardShield,changed);
                Submit();
                TestAssert.True(guide.CurrentInstruction.StepText.Contains("H") && !guide.CurrentInstruction.StepText.Contains('{') &&
                    guide.CurrentInstruction.StepText.Contains(language==GameLanguage.English?"tracking":"追踪",StringComparison.OrdinalIgnoreCase),
                    "same submitted source uses actual changed InputMap shield glyph and actual controller language");
            }
            InputMap.ActionEraseEvents(InputActions.KeyboardShield); foreach(var ev in inputEvents) InputMap.ActionAddEvent(InputActions.KeyboardShield,ev);
            guide.SetLanguage(GameLanguage.English); Submit();
            var marker=level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            TestAssert.True(marker.HasVisiblePrompt && marker.RenderedTextContained && marker.RenderedStepText==guide.CurrentInstruction.StepText,
                "existing fixed view has the complete current semantic text (not pixel acceptance)");
            using(var other=new HeadlampOcclusionSampler(level.GetNode<Node2D>("WorldRoot")))
                TestAssert.Equal(VisibleLaserWarning.None,cues.ReadVisibleLaser(other.Capture()),"equal-geometry but different snapshot cannot authorize text");
            var copy=cues.GetChildren().OfType<AttackTelegraphVisual>().First(c=>c.CurrentFrame.Timeline.Kind==EnemyAttackKind.PrismLaserBeam);
            copy.PresentBeforeDraw(default);
            TestAssert.Equal(VisibleLaserWarning.None,cues.ReadVisibleLaser(fog.LastOcclusionFrame),"overwritten visible copy withdraws the semantic receipt");
            Submit(); guide._Process(0);
            TestAssert.True(IsLaser(),"one pre-draw write receipt preserves current text through the next actual controller entry");
            guide._Process(0);
            TestAssert.True(!IsLaser(),"without another current submission the old warning cannot persist indefinitely");
            Submit();
            actor.Visible=false; Submit(); AssertWarning(VisibleLaserWarning.None,"hidden real source"); actor.Visible=true; Submit();
            cues.Visible=false; Submit(); AssertWarning(VisibleLaserWarning.None,"hidden protected consumer"); cues.Visible=true;
            var layer=cues.GetParent() as CanvasLayer; TestAssert.True(layer!=null,"saved protected consumer belongs to layer2");
            layer.Visible=false; Submit(); AssertWarning(VisibleLaserWarning.None,"hidden protected layer"); layer.Visible=true;
            cues.SetProcess(false); Submit(); AssertWarning(VisibleLaserWarning.None,"disabled protected consumer"); cues.SetProcess(true);
            Vector2 original=actor.GlobalPosition;
            actor.GlobalPosition=level.Player.GlobalPosition+new Vector2(-700,-7); Submit(); AssertWarning(VisibleLaserWarning.None,"source outside620 range"); actor.GlobalPosition=original;
            camera.Offset=new Vector2(2000,0); camera.ForceUpdateScroll(); Submit(); AssertWarning(VisibleLaserWarning.None,"entire actual warning shape outside logical viewport");
            camera.Offset=Vector2.Zero; camera.ForceUpdateScroll();
            using(var wallShape=new RectangleShape2D{Size=new Vector2(24,160)})
            {
                var wall=new StaticBody2D{Name="EchoOcclusionControl",Position=new Vector2((original.X+level.Player.GlobalPosition.X)*.5f,1073),CollisionLayer=CombatCollisionLayers.World,CollisionMask=0};
                wall.AddChild(new CollisionShape2D{Shape=wallShape}); level.GetNode<Node2D>("WorldRoot").AddChild(wall);
                try
                {
                    Submit(); TestAssert.True(!fog.LastOcclusionFrame.CanSee(level.Player.GlobalPosition,actor.GlobalPosition),"control is a real current native occluder");
                    AssertWarning(VisibleLaserWarning.None,"actual wall blocks source visibility");
                }
                finally{wall.Free();}
            }
            Submit();
            var pause=level.GetNode<ProloguePauseMenu>("PauseMenu"); pause.Open(); Submit();
            TestAssert.True(tree.Paused && !guide.CurrentInstruction.Visible && cues.ReadVisibleLaser(fog.LastOcclusionFrame)==VisibleLaserWarning.None,
                "actual pause withdraws warning and teaching"); pause.Execute(ProloguePauseCommand.Resume); Submit();
            guide.SetGuidanceEnabled(false); Submit(); TestAssert.True(!guide.CurrentInstruction.Visible,"disabled teaching stays hidden");
            guide.SetGuidanceEnabled(true); Submit();
            await Until(()=>actor.CurrentFrame.AttackTimeline.Phase==AttackPhase.Telegraph && actor.CurrentFrame.AttackTimeline.HasLockedTarget,100);
            AssertWarning(VisibleLaserWarning.Locked,"actual model lock");
            await Until(()=>actor.CurrentFrame.AttackTimeline.Phase==AttackPhase.Active,100);
            AssertWarning(VisibleLaserWarning.Active,"actual firing beam");
            TestAssert.Equal(objective,completion.CurrentObjective,"warning never completes the original objective");
            actor.QueueFree(); Submit(); AssertWarning(VisibleLaserWarning.None,"queued owner withdraws before native flush");
            flow.ReturnToTitle(); await Frames(2);
            TestAssert.True(flow.State==PrologueGameFlowState.Title && !IsLaser() && cues.ReadVisibleLaser(fog.LastOcclusionFrame)==VisibleLaserWarning.None,
                "actual return-to-title cannot retain warning");
            GD.Print("VISIBLE_LASER_GUIDANCE_NATIVE_OK saved_temple=true generated_source=true manual_predraw=true states=3 native_visibility=true lifecycle=true live_glyphs=true headless_semantics=true pixels=false human=false route=false");

            bool IsLaser()=>guide.CurrentInstruction.ContextTitle==PrologueStrings.Get("guidance.laser_title",guide.Language);
            void AssertWarning(VisibleLaserWarning expected,string label)
            {
                TestAssert.Equal(expected,cues.ReadVisibleLaser(fog.LastOcclusionFrame),label+" overlay");
                TestAssert.Equal(expected!=VisibleLaserWarning.None,IsLaser(),label+" actual controller");
                if(expected!=VisibleLaserWarning.None) TestAssert.True(guide.CurrentInstruction.Targets.Count==0 && guide.CurrentInstruction.HintText=="",label+" markerless");
            }
        }
        finally
        {
            tree.Paused=false;
            InputMap.ActionEraseEvents(InputActions.KeyboardShield); foreach(var ev in inputEvents) InputMap.ActionAddEvent(InputActions.KeyboardShield,ev);
            try
            {
                if(GodotObject.IsInstanceValid(level))
                { if(level.IsInsideTree()) await TestSceneTeardown.FreeSceneAfterAudioDrain(tree,level); else level.Free(); }
            }
            finally
            {
                foreach(var resource in owned) if(GodotObject.IsInstanceValid(resource)) resource.Dispose();
                actorScene?.Dispose();
            }
        }

        void Submit()
        {
            // Invoke the real connected producer synchronously, not a fabricated
            // overlay permission or a claim that headless drew normal pixels.
            RenderingServer.Singleton.EmitSignal(RenderingServer.SignalName.FramePreDraw);
        }
        async Task Frames(int n)
        {
            for(int i=0;i<n;i++)
            {
                await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame);
                await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
                Submit();
            }
        }
        async Task Until(Func<bool> condition,int limit)
        { for(int i=0;i<limit && !condition();i++) await Frames(1); TestAssert.True(condition(),"actual source reaches required state within original bounded fixture wait"); }

        void LogSourcePremise(RangedWeaponEnemy source,Camera2D intendedCamera,int steps,ulong physicsStart)
        {
            Node2D selected=tree.GetFirstNodeInGroup("player") as Node2D;
            var muzzle=source.GetNode<Node2D>("Muzzle");
            Viewport viewport=source.GetViewport();
            Camera2D activeCamera=viewport.GetCamera2D();
            Vector2 origin=muzzle.GlobalPosition;
            Vector2 target=GodotObject.IsInstanceValid(selected)?selected.GlobalPosition:origin;
            Vector2 canvas=muzzle.GetGlobalTransformWithCanvas().Origin;
            Rect2 view=viewport.GetVisibleRect();
            GD.Print($"VISIBLE_LASER_SOURCE_PREMISE steps={steps} physics_delta={Engine.GetPhysicsFrames()-physicsStart} " +
                $"flow={flow.State} paused={tree.Paused} source_physics={source.IsPhysicsProcessing()} source_can_process={source.CanProcess()} " +
                $"source_mode={source.ProcessMode} source_position={source.GlobalPosition} source_hp={source.GetNode<HealthComponent>("Health").CurrentHealth} " +
                $"state={source.CurrentFrame.State} phase={source.CurrentFrame.AttackTimeline.Phase} locked={source.CurrentFrame.AttackTimeline.HasLockedTarget} " +
                $"selected_is_player={selected==level.Player} selected_id={(GodotObject.IsInstanceValid(selected)?selected.GetInstanceId():0)} " +
                $"selected_hp={selected?.GetNodeOrNull<HealthComponent>("Health")?.CurrentHealth} selected_position={target} " +
                $"muzzle={origin} canvas={canvas} viewport={view} muzzle_in_viewport={view.HasPoint(canvas)} " +
                $"active_camera_id={(GodotObject.IsInstanceValid(activeCamera)?activeCamera.GetInstanceId():0)} intended_camera_id={intendedCamera.GetInstanceId()} " +
                $"camera_center={intendedCamera.GetScreenCenterPosition()} camera_zoom={intendedCamera.Zoom} camera_offset={intendedCamera.Offset} " +
                $"camera_limits={intendedCamera.LimitLeft},{intendedCamera.LimitTop},{intendedCamera.LimitRight},{intendedCamera.LimitBottom} " +
                $"distance={origin.DistanceTo(target)} authored_range={source.Settings.DetectionRange} independent_observation=true");
            var exclude=new Godot.Collections.Array<Rid>{source.GetRid()};
            using var excludeOwner=(Godot.Collections.Array)exclude;
            var space=source.GetWorld2D().DirectSpaceState;
            bool clear=RangedAttackGeometry.HasClearWorldPath(space,origin,target,exclude);
            using var ray=PhysicsRayQueryParameters2D.Create(origin,target,CombatCollisionLayers.World,exclude);
            ray.CollideWithBodies=true; ray.CollideWithAreas=false; ray.HitFromInside=true;
            using var hit=space.IntersectRay(ray);
            Node collider=hit.ContainsKey("collider")?hit["collider"].AsGodotObject() as Node:null;
            string colliderPath="none";
            if(GodotObject.IsInstanceValid(collider)) { using var path=collider.GetPath(); colliderPath=path.ToString(); }
            GD.Print($"VISIBLE_LASER_SOURCE_WORLD independent_clear_path={clear} first_collider={colliderPath} " +
                $"source_world={source.GetWorld2D().GetInstanceId()} player_world={level.Player.GetWorld2D().GetInstanceId()} diagnostic_queries_only=true");
        }
    }

    private static void RunModel()
    {
        PrologueStrings.Load(File.ReadAllText(ProjectSettings.GlobalizePath("res://assets/localization/prologue-ui.json")));
        var keys = new Dictionary<StringName, string>
        {
            [InputActions.MoveLeft]="LEFT", [InputActions.MoveRight]="RIGHT", [InputActions.MoveUp]="UP", [InputActions.MoveDown]="DOWN",
            [InputActions.Jump]="JUMP", [InputActions.Dash]="JET", [InputActions.Attack]="ATTACK", [InputActions.Claw]="CLAW",
            [InputActions.KeyboardShield]="GUARD_LIVE", [InputActions.Slot1]="ONE", [InputActions.Slot2]="TWO",
            [InputActions.Slot3]="THREE", [InputActions.Slot4]="FOUR"
        };
        foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
        foreach (VisibleLaserWarning warning in new[] { VisibleLaserWarning.Tracking, VisibleLaserWarning.Locked, VisibleLaserWarning.Active })
        foreach (MechanicalClawState claw in new[] { MechanicalClawState.Idle, MechanicalClawState.Reel, MechanicalClawState.Swing })
        {
            var context = new PrologueGuidanceContext(PrologueObjective.EnterTemple, PrologueSectorId.LostTemple,
                PrologueBossGuidanceState.Inactive, 3, language, true, claw, VisibleLaser: warning);
            var card = PrologueGuidanceModel.Evaluate(context, keys);
            TestAssert.True(card.Visible && card.Targets.Count == 0 && card.ContextTitle.Contains("ECHO") &&
                card.HintText == "" && card.StepText.Contains("GUARD_LIVE") && !card.StepText.Contains('{'),
                "VISIBLE_LASER_MODEL_RED: visible laser must have localized markerless current-phase advice even while attached");
            string phase = warning switch
            {
                VisibleLaserWarning.Tracking => language == GameLanguage.English ? "tracking" : "追踪",
                VisibleLaserWarning.Locked => language == GameLanguage.English ? "locked" : "锁住",
                _ => language == GameLanguage.English ? "firing" : "发射"
            };
            TestAssert.True(card.StepText.Contains(phase, StringComparison.OrdinalIgnoreCase) &&
                !card.StepText.Contains("invulnerable", StringComparison.OrdinalIgnoreCase) && !card.StepText.Contains("无敌") &&
                card.StepText.Split('\n').Length == 3, "three semantic lines explain actual phase, not guaranteed evasion");
            TestAssert.True(!PrologueGuidanceModel.Evaluate(context with { GuidanceEnabled=false }, keys).Visible &&
                !PrologueGuidanceModel.Evaluate(context with { Objective=PrologueObjective.Complete }, keys).Visible,
                "laser cannot reopen disabled/completed guidance");
            foreach (PrologueBossGuidanceState boss in new[] { PrologueBossGuidanceState.Active, PrologueBossGuidanceState.ChainExposed })
            {
                var boundary = context with { BossState=boss };
                var expected=PrologueGuidanceModel.Evaluate(boundary with { VisibleLaser=VisibleLaserWarning.None }, keys);
                var actual=PrologueGuidanceModel.Evaluate(boundary, keys);
                TestAssert.True(expected.Visible==actual.Visible && expected.StepText==actual.StepText &&
                    expected.HintText==actual.HintText && expected.ContextTitle==actual.ContextTitle &&
                    expected.Targets.SequenceEqual(actual.Targets), "boss instructions retain priority");
            }
            foreach (VisibleLaserWarning invalid in new[] { VisibleLaserWarning.None, (VisibleLaserWarning)999 })
            {
                var boundary=context with { VisibleLaser=invalid };
                var expected=PrologueGuidanceModel.Evaluate(context with { VisibleLaser=VisibleLaserWarning.None },keys);
                var actual=PrologueGuidanceModel.Evaluate(boundary,keys);
                TestAssert.True(expected.StepText==actual.StepText && expected.ContextTitle==actual.ContextTitle &&
                    expected.HintText==actual.HintText, "absent/unknown evidence restores original route/claw advice");
            }
        }
        GD.Print("VISIBLE_LASER_GUIDANCE_MODEL_OK languages=2 states=3 live_bindings=true markerless=true boss_priority=true pixels=false");
    }
}
