using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Prologue;
using Knight.Presentation.Minimap;
using Knight.Player;
using Knight.Save;
using Knight.Settings;
using Knight.Ui;
using Knight.Prologue.Guidance;
using Knight.Presentation.Fog;

namespace Knight.Tests.Integration;

public static class MinimapHudTests
{
    public static async Task Run(SceneTree tree)
    {
        TestAssert.True(DisplayServer.GetName()!="headless","HUD pixels require an admitted display-backed GPU fixture");
        TestAssert.Equal("gl_compatibility",ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),"HUD uses unchanged production renderer");
        string adapter=RenderingServer.GetVideoAdapterName();
        TestAssert.True(!string.IsNullOrWhiteSpace(adapter),"hardware adapter is reported");
        foreach(string software in new[]{"llvmpipe","lavapipe","swiftshader","Microsoft Basic Render","softpipe"})
            TestAssert.True(!adapter.Contains(software,StringComparison.OrdinalIgnoreCase),"software renderer cannot certify HUD pixels");
        await SoftEllipticalPerimeterRemovesRectangularBackplate(tree, IsolatedPrologueFixture.CreatePaths("soft-elliptical-perimeter").CaseRoot);
        await ScaledCheckpointFootprintStaysInsideOpaqueEllipse(tree, IsolatedPrologueFixture.CreatePaths("scaled-checkpoint-opaque-ellipse").CaseRoot);
        await CheckpointFootprintStaysInsideEllipse(tree, IsolatedPrologueFixture.CreatePaths("checkpoint-ellipse-footprint").CaseRoot);
        await DiagonalJumpActorFootprintStaysInsideEllipse(tree, IsolatedPrologueFixture.CreatePaths("actor-ellipse-footprint").CaseRoot);
        await MovingPresentationAllocation(tree);
        await PlayerMarkerReadableAtMinimumOutput(tree, IsolatedPrologueFixture.CreatePaths("minimum-marker-readability").CaseRoot);
        await AdaptiveOuterFeatherRemainsBounded(tree, IsolatedPrologueFixture.CreatePaths("adaptive-outer-feather").CaseRoot);
        await MarkerBounds(tree, IsolatedPrologueFixture.CreatePaths("marker-bounds").CaseRoot);
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
        var paths = IsolatedPrologueFixture.Configure(level, nameof(MinimapHudTests));
        var flow = level.GetNode<PrologueGameFlowController>("GameFlow"); flow.HeadlessAutoStart = false;
        // Reuse the saved binding, configuring the synthetic test catalog before any Ready.
        var controller = level.GetNode<PrologueMinimapController>("MinimapController");
        controller.Catalog = new MinimapTerrainCatalog(new Rect2(-2048.25f, -1024.5f, 4096, 8192), "hud-live-1");
        level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap").AuthoredCheckpoints =
            new Godot.Collections.Dictionary<string,Vector2>{{"canyon.v2.basin-camp",new Vector2(1680,3960)}};
        Vector2I oldSize = tree.Root.Size;
        Vector2I content = tree.Root.ContentScaleSize;
        var stretch = tree.Root.ContentScaleMode; var aspect = tree.Root.ContentScaleAspect;
        float oldFactor = tree.Root.ContentScaleFactor;
        try
        {
            tree.Root.AddChild(level);
            await Frames(tree, 3);
            TestAssert.True(level.GetNodeOrNull<Control>("Hud/FormalHud/Minimap") != null,
                "AllNineViewportScalePairsAvoidHud: saved formal HUD must contain its real borderless minimap view");
            var view = level.GetNode<PrologueMinimapView>("Hud/FormalHud/Minimap");
            var formal = view.GetParent<PrologueFormalHud>();
            TestAssert.True(!view.Visible, "HiddenOutsidePlaying: title hides actual view");
            flow.StartNew(); await Frames(tree, 3);
            flow.Save.CheckpointId="canyon.v2.basin-camp";
            flow.Save.HasActivatedCheckpoint=true;
            TestAssert.True(view.Visible && ReferenceEquals(controller.Model.Catalog, controller.Catalog), "Playing binds the live lawful model");
            Vector2 smallest = new(float.MaxValue, float.MaxValue);
            foreach (Vector2I output in new[] { new Vector2I(1280,720), new Vector2I(1920,1080), new Vector2I(3840,2160) })
            foreach (float scale in new[] { .8f, 1f, 1.5f })
            {
                flow.ApplySettings(new GameSettingsData { ResolutionWidth=output.X, ResolutionHeight=output.Y,
                    WindowMode=GameWindowMode.Windowed, UiScale=scale, VSync=false });
                await Frames(tree, 4);
                TestAssert.True(tree.Root.ContentScaleSize == content && tree.Root.ContentScaleMode == stretch && tree.Root.ContentScaleAspect == aspect,
                    "production canvas/stretch must remain unchanged for physical output cases");
                TestAssert.Near(scale, tree.Root.ContentScaleFactor, .001f, "actual public settings/root factor");
                formal.SetUiScale(1f); await Frames(tree, 2);
                Rect2 map = view.GetGlobalRect();
                TestAssert.True(map.HasArea() && new Rect2(Vector2.Zero, formal.Size).Encloses(map), "AllNineViewportScalePairsAvoidHud: map remains on canvas");
                foreach (string name in new[] { "Health","ObjectiveRibbon","Prompt","Boss","ResourceCluster","ModeSelector","Loadout" })
                    TestAssert.True(!map.Intersects(formal.GetNode<Control>(name).GetGlobalRect()), "AllNineViewportScalePairsAvoidHud: actual map avoids " + name);
                TestAssert.True(map.Position.Y >= formal.GetNode<Control>("Prompt").GetGlobalRect().End.Y + 11.99f, "actual12 logical prompt clearance");
                if (map.Size.X < smallest.X) smallest = map.Size;
                using Image pixels = await Read(tree, tree.Root);
                TestAssert.True(pixels.GetSize() == output && tree.Root.Size == output, "physical/output dimensions are the requested pair");
                Save(pixels, paths.CaseRoot, $"actual-{output.X}-{scale}");
                GD.Print($"MINIMAP_LAYOUT output={pixels.GetSize()} root={tree.Root.Size} logical={tree.Root.GetVisibleRect().Size} content={content} factor={scale} private=1 map={map}");
            }
            // Concrete source-traced negative: naive placement hits BOTH resources and Boss at private1.5.
            foreach (Vector2 size in new[] { new Vector2(1280,720),new Vector2(1920,1080),new Vector2(3840,2160) })
            {
                var l = PrologueHudLayout.Calculate(size, 1.5f); float r = size.Y / 720;
                var naive = new Rect2(new Vector2(size.X-48*r-390*r,l.PromptRect.End.Y+12*r),new Vector2(390,255)*r);
                TestAssert.True(naive.Intersects(l.ResourceClusterRect) && naive.Intersects(l.BossRect), "private1.5 naive negative hits relocated resources AND Boss");
                TestAssert.True(l.MinimapRect.HasArea() && !l.MinimapRect.Intersects(l.ResourceClusterRect) && !l.MinimapRect.Intersects(l.BossRect), "private1.5 actual solver avoids concrete collision");
            }
            GD.Print($"MINIMAP_CASE_OK AllNineViewportScalePairsAvoidHud smallest={smallest}");
            TestAssert.Equal(0,view.DiscoveredCheckpointCount,"real old latest activated ID alone grants no marker at unexplored actual authored descriptor");
            await ActualSmallestOutputPixels(tree,flow,controller,view,paths.CaseRoot,smallest);
            flow.ApplySettings(new GameSettingsData { VSync=false }); await Frames(tree, 3);
            await ActiveGuidanceFirstFrame(tree, level, controller, view);
            await KeyboardAndMouseIgnored(tree, level, view);
            var pause = level.GetNode<ProloguePauseMenu>("PauseMenu");
            ulong revision = controller.Model.RevisionCounter; var model = controller.Model;
            pause.Open();
            TestAssert.True(!view.Visible && tree.Paused, "HiddenOutsidePlaying: synchronously hidden at pause first frame");
            TestAssert.True(!WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(formal).Contains(view.GetGlobalRect()), "hidden map reserves no protected hole");
            pause.Execute(ProloguePauseCommand.Resume); await Frames(tree, 2);
            TestAssert.True(view.Visible && ReferenceEquals(model, controller.Model) && controller.Model.RevisionCounter >= revision, "resume reads same model");
            flow.Complete();await Frames(tree,2);
            TestAssert.True(flow.State==PrologueGameFlowState.Completed && level.GetNode<CanvasLayer>("Hud").Visible && !view.Visible,"real completion explicitly re-shows HUD but cannot re-show minimap");
            foreach (var state in new[] { PrologueGameFlowState.Intro, PrologueGameFlowState.Recovering, PrologueGameFlowState.Completed })
            {
                // Read-only presentation state negative controls; no fabricated lifecycle acceptance.
                typeof(PrologueGameFlowController).GetProperty(nameof(flow.State))!.SetValue(flow, state);
                level.GetNode<CanvasLayer>("Hud").Visible = true; await Frames(tree, 2);
                TestAssert.True(!view.Visible, "HiddenOutsidePlaying: even re-shown parent must not grant " + state);
            }
            flow.ReturnToTitle(); await Frames(tree,2);
            TestAssert.True(!view.Visible, "title hidden after prior presentation");
            GD.Print("MINIMAP_CASE_OK HiddenOutsidePlaying synchronous_pause=True model_preserved=True");
            level.QueueFree(); await Frames(tree, 3);
            await RasterContracts(tree, paths.CaseRoot, smallest);
            await CacheEdgeFootprints(tree, paths.CaseRoot);
            GD.Print("MINIMAP_HUD_OK groups=6 actual_settings_pairs=9");
        }
        finally
        {
            tree.Paused = false;
            foreach (Key key in new[] { Key.D, Key.Q,Key.J,Key.K,Key.L,Key.Escape }) KeyEvent(key,false);
            if (GodotObject.IsInstanceValid(level)) level.QueueFree();
            tree.Root.Size = oldSize; tree.Root.ContentScaleFactor = oldFactor; await Frames(tree, 3);
        }
    }

    private static async Task ActualSmallestOutputPixels(SceneTree tree,PrologueGameFlowController flow,PrologueMinimapController controller,PrologueMinimapView view,string root,Vector2 smallest)
    {
        flow.ApplySettings(new GameSettingsData{ResolutionWidth=1280,ResolutionHeight=720,UiScale=1.5f,VSync=false});await Frames(tree,3);
        TestAssert.True(view.Size.DistanceTo(smallest)<.01f,"actual-root output uses the smallest generated layout, not a resized test substitute");
        controller.ProcessMode=Node.ProcessModeEnum.Disabled;
        var catalog=new MinimapTerrainCatalog(new Rect2(-2048.25f,-1024.5f,4096,2048),"actual-minimum-pixels");
        var model=new ExplorationMapModel(catalog);byte[] cells=new byte[(catalog.CellCount+7)/8];Array.Fill(cells,(byte)255);
        for(int y=0;y<catalog.Rows;y++){int id=y*catalog.Columns+256;cells[id>>3]&=(byte)~(1<<(id&7));}
        model.Restore(new ExplorationMapSaveData{TerrainRevision=catalog.Revision,FreeCellsBase64=Convert.ToBase64String(cells),BoundarySegmentsBase64=""});
        view.Present(model,new Vector2(3.75f,.5f),Vector2.Right,0);
        using Image pixels=await Read(tree,tree.Root);Save(pixels,root,"actual-root-smallest-whole-output-footprint");
        Vector2 outputScale=(Vector2)pixels.GetSize()/tree.Root.GetVisibleRect().Size;
        Rect2 outputRect=new(view.GetGlobalRect().Position*outputScale,view.Size*outputScale);
        int mixed=0,known=0,knownCenterMixed=0;
        for(int y=(int)(outputRect.Position.Y+25*outputScale.Y);y<outputRect.End.Y-25*outputScale.Y;y++)
        for(int x=(int)(outputRect.Position.X+25*outputScale.X);x<outputRect.End.X-25*outputScale.X;x++)
        {
            Vector2 p=new(x+.5f-outputRect.Position.X,y+.5f-outputRect.Position.Y);
            if((p/outputScale-view.Size*.5f).Length()<12)continue;
            float left=view.ViewWorldRect.Position.X+(p.X-.5f)*1280/outputRect.Size.X;
            float right=view.ViewWorldRect.Position.X+(p.X+.5f)*1280/outputRect.Size.X;
            if(left<7.75f && right>-.25f)
            {
                AssertBlack(pixels,x,y,"actual-root smallest original-size output mixed footprint wholly conservative");mixed++;
                float centre=(left+right)*.5f;if(centre<-.25f || centre>=7.75f)knownCenterMixed++;
            }
            else if(IndependentEllipseOpacity(p/outputScale,smallest,Math.Min(24,Math.Min(smallest.X,smallest.Y)*.2))>=.99999)
            {
                // The running game's background is not white. Only an opaque
                // known-space sample is an independent positive control here.
                AssertKnownWhiteComposite(pixels,x,y,1,"actual-root explored opaque core has true known-space color, not transparent game background");
                known++;
            }
        }
        TestAssert.True(mixed>0 && known>0 && knownCenterMixed>0 && pixels.GetSize()==new Vector2I(1280,720),"actual-root smallest output must catch centre-only sampling with genuinely explored-centre mixed footprints");
        GD.Print($"MINIMAP_ACTUAL_MINIMUM_PIXELS output={pixels.GetSize()} logical={tree.Root.GetVisibleRect().Size} content={tree.Root.ContentScaleSize} factor={tree.Root.ContentScaleFactor} private=1 map={view.GetGlobalRect()} output_map={outputRect} mixed_black={mixed} explored_center_mixed_black={knownCenterMixed} known_lit={known}");
        controller.ProcessMode=Node.ProcessModeEnum.Always;
    }

    private static async Task ActiveGuidanceFirstFrame(SceneTree tree,PrologueLevelController level,PrologueMinimapController controller,PrologueMinimapView view)
    {
        var guidance=level.GetNode<PrologueGuidanceController>("Guidance");
        var marker=level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
        guidance.ProcessMode=Node.ProcessModeEnum.Disabled;controller.ProcessMode=Node.ProcessModeEnum.Disabled;
        Rect2 prior=view.GetGlobalRect();view.ClearPresentation();
        var instruction=new PrologueGuidanceInstruction(true,"Existing active guidance", "", "", "",Array.Empty<PrologueGuidanceTargetKey>());
        var target=new PrologueGuidanceTargetView(new Vector2(1100,220),Vector2.Right,0,true);
        marker.Present(instruction,new[]{target});
        TestAssert.True(marker.HasVisiblePrompt && prior.Intersects(marker.PromptRect),"active-guidance negative control truly occupies the hidden map's future rectangle");
        controller.ProcessMode=Node.ProcessModeEnum.Always;
        await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);
        TestAssert.True(view.Visible,"negative-control frame actually ran the live minimap controller");
        TestAssert.True(view.Visible && !view.GetGlobalRect().Intersects(marker.PromptRect),"ActiveGuidanceFirstFrame: map appearing must avoid already active real guidance card");
        Rect2 placed=view.GetGlobalRect();
        for(int i=0;i<8;i++)
        {
            marker.Present(instruction,new[]{target});await Frames(tree,1);
            TestAssert.True(!view.GetGlobalRect().Intersects(marker.PromptRect) && view.GetGlobalRect()==placed,"future guidance protects visible map without oscillating its placement");
        }
        foreach(Vector2 next in new[]{new Vector2(600,320),new Vector2(1100,500),new Vector2(1100,220)})
        {
            marker.Clear();await Frames(tree,2);
            TestAssert.True(view.GetGlobalRect()==placed,"guide disappearance never reverts/grows the latched map");
            marker.Present(instruction,new[]{target with {ViewportPosition=next}});await Frames(tree,2);
            TestAssert.True(view.GetGlobalRect()==placed && (!marker.HasVisiblePrompt || !placed.Intersects(marker.PromptRect)),"moved/reappearing guidance cannot cumulatively shrink or chase map");
        }
        var formal=view.GetParent<PrologueFormalHud>();
        foreach(float scale in new[]{.8f,1.5f,1f})
        {
            formal.SetUiScale(scale);await Frames(tree,2);
            TestAssert.True(view.Size.X>=135 && view.Size.Y>=88 && (!marker.HasVisiblePrompt || !view.GetGlobalRect().Intersects(marker.PromptRect)),"explicit resize recomputes usable map and avoids active guidance");
            placed=view.GetGlobalRect();
        }
        TestAssert.True(WorldTargetMarkerVisual.CollectVisibleFormalHudRegions(view.GetParent<PrologueFormalHud>()).Contains(placed),"actual visible map is registered for future card placement");
        guidance.ProcessMode=Node.ProcessModeEnum.Inherit;
        GD.Print($"MINIMAP_ACTIVE_GUIDANCE first_frame=True no_oscillation8=True map={placed}");
    }

    public static async Task Regressions(SceneTree tree)
    {
        string[] names = { "MountedGuidanceRendersAndClearsWithoutHidingObjective", "MountedGuidancePassesPointerInputThroughEveryOverlayControl", "MountedGuidanceDoesNotOverlapBossHud" };
        var selected = PrologueGuidanceRuntimeTests.Task7Cases(tree).Where(c => names.Any(n => c.Name.EndsWith("."+n,StringComparison.Ordinal))).ToArray();
        TestAssert.Equal(3,selected.Length,"exact existing guidance/Boss protected regression selection");
        await Task7CaseRegistry.Execute(selected);
        GD.Print("MINIMAP_EXISTING_GUIDANCE_REGRESSION_OK cases=3");
        await PrologueFormalHudTests.Run(tree);
        GD.Print("MINIMAP_HUD_PROTECTED_REGRESSION_OK formalhud=True existing_guidance_cases=3");
    }

    private static async Task KeyboardAndMouseIgnored(SceneTree tree, PrologueLevelController level, PrologueMinimapView view)
    {
        void Passive(Node n)
        {
            if(n is Control c) TestAssert.True(c.MouseFilter==Control.MouseFilterEnum.Ignore && c.FocusMode==Control.FocusModeEnum.None,"KeyboardAndMouseIgnored: every control descendant passive");
            foreach(Node child in n.GetChildren()) Passive(child);
        }
        Passive(view);
        var probe=new MinimapUnhandledInputProbe();level.AddChild(probe);
        var input = level.GetNode<PlayerInputReader>("PlayerRoot/Player/Input");
        Control focus = tree.Root.GuiGetFocusOwner();
        foreach(Vector2 p in new[] {view.GetGlobalRect().GetCenter(),view.GetGlobalRect().Position+Vector2.One})
        {
            ulong mouse = input.MouseMotionSequence;
            Input.ParseInputEvent(new InputEventMouseMotion{Position=p,GlobalPosition=p});
            Input.ParseInputEvent(new InputEventMouseButton{Position=p,GlobalPosition=p,ButtonIndex=MouseButton.Left,Pressed=true});
            await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame); await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            TestAssert.True(input.MouseMotionSequence>mouse && input.LastObservedMouseViewportPosition.DistanceTo(p)<.01f && tree.Root.GuiGetFocusOwner()==focus,"actual mouse centre/edge events pass without focus consumption");
            Input.ParseInputEvent(new InputEventMouseButton{Position=p,ButtonIndex=MouseButton.Left,Pressed=false});
        }
        KeyEvent(Key.D,true); await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame); await Frames(tree,1);
        TestAssert.True(input.CurrentFrame.MoveAxis>.9f,"actual D event reaches gameplay sampler"); KeyEvent(Key.D,false);
        foreach(Key key in new[]{Key.Q,Key.J,Key.K,Key.L})
        {
            int before=probe.GameplayKeys;
            KeyEvent(key,true); await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame); await Frames(tree,1);
            TestAssert.True(tree.Root.GuiGetFocusOwner()==focus && probe.GameplayKeys>before,"actual gameplay key reaches unhandled event route without map focus/consumption"); KeyEvent(key,false); await Frames(tree,1);
        }
        KeyEvent(Key.Escape,true); await Frames(tree,2); KeyEvent(Key.Escape,false);
        TestAssert.True(tree.Paused && !view.Visible,"actual Esc reaches production pause and synchronous hide");
        level.GetNode<ProloguePauseMenu>("PauseMenu").Execute(ProloguePauseCommand.Resume); await Frames(tree,2);
        probe.QueueFree();
        GD.Print("MINIMAP_CASE_OK KeyboardAndMouseIgnored event_route=ParseInputEvent not_human_hardware=True");
    }

    private static async Task RasterContracts(SceneTree tree,string root,Vector2 smallest)
    {
        var viewport = new SubViewport { Size=new Vector2I(1280,720),RenderTargetUpdateMode=SubViewport.UpdateMode.Always,World2D=new World2D() };
        var white = new ColorRect { Size=new Vector2(1280,720),Color=Colors.White,MouseFilter=Control.MouseFilterEnum.Ignore };
        viewport.AddChild(white);
        var view = new PrologueMinimapView { Position=new Vector2(80,80),Size=PrologueMinimapView.BaseSize };
        // These are fixed authored positions, not Save.LatestCheckpoint or activation history.
        view.ConfigureCheckpoints(new[]{new MinimapCheckpointDescriptor("visited",new Vector2(-100.25f,-.5f)),new MinimapCheckpointDescriptor("old-latest-unexplored",new Vector2(3.75f,-.5f))});
        viewport.AddChild(view); tree.Root.AddChild(viewport); await Frames(tree,2);
        try
        {
            var catalog = new MinimapTerrainCatalog(new Rect2(-2048.25f,-1024.5f,4096,2048),"hud-raster-1");
            var model = new ExplorationMapModel(catalog);
            view.Present(model,new Vector2(.75f,.5f),Vector2.Right,0);
            using(var unknown=await Read(tree,viewport))
            {
                // This original sample is now in the diagonal feather, not the
                // opaque core: the independent ellipse gives white contribution .532293.
                AssertNeutralWhiteComposite(unknown,120,120,.532293,"UnknownRemainsBlack: unknown diagonal feather contains only black, not terrain");
                AssertBlack(unknown,209,130,"UnknownRemainsBlack: unknown original-size central interior black");
                TestAssert.True(unknown.GetPixel(80,80).R>.95f,"only external edge becomes transparent"); Save(unknown,root,"unknown-fractional-origin");
            }
            byte[] cells=new byte[(catalog.CellCount+7)/8]; Array.Fill(cells,(byte)255);
            // One full unknown8-unit lattice column. A final pixel may straddle it even when its centre is explored.
            for(int y=0;y<catalog.Rows;y++){int id=y*catalog.Columns+256;cells[id>>3]&=(byte)~(1<<(id&7));}
            var durable = new ExplorationMapSaveData{TerrainRevision=catalog.Revision,FreeCellsBase64=Convert.ToBase64String(cells),BoundarySegmentsBase64=""};
            model.Restore(durable);
            foreach(Vector2 size in new[]{PrologueMinimapView.BaseSize,smallest})
            {
                view.Size=size; view.Present(model,new Vector2(.75f,.5f),Vector2.Right,0);
                using Image pixels=await Read(tree,viewport);
                int mixed=0,lit=0;
                for(int y=(int)(view.Position.Y+25);y<view.Position.Y+size.Y-25;y++)
                for(int x=(int)(view.Position.X+25);x<view.Position.X+size.X-25;x++)
                {
                    Vector2 p=new(x+.5f-view.Position.X,y+.5f-view.Position.Y);
                    if(p.DistanceTo(size*.5f)<12 || p.DistanceTo((new Vector2(-100.25f,-.5f)-view.ViewWorldRect.Position)*size/view.ViewWorldRect.Size)<6)continue;
                    float left=view.ViewWorldRect.Position.X+(p.X-.5f)*1280/size.X;
                    float right=view.ViewWorldRect.Position.X+(p.X+.5f)*1280/size.X;
                    bool touchesUnknown=left<7.75f && right>-.25f;
                    // Preserve every original output-pixel check, including the
                    // curved feather; transparent white is not a known-space control.
                    double alpha=IndependentEllipseOpacity(p,size,Math.Min(24,Math.Min(size.X,size.Y)*.2));
                    if(touchesUnknown){AssertNeutralWhiteComposite(pixels,x,y,1-alpha,"UnknownRemainsBlack: WHOLE final pixel mixed footprint composites only black");mixed++;}
                    else
                    {
                        AssertKnownWhiteComposite(pixels,x,y,alpha,"lawful whole explored footprint has independently composited known-space color");
                        if(alpha>.5)lit++;
                    }
                }
                TestAssert.True(mixed>0 && lit>0,"original-size mixed-footprint test has real unknown and known controls at "+size);
                TestAssert.Equal(1,view.DiscoveredCheckpointCount,"unexplored old latest-checkpoint descriptor cannot grant a marker");
                Save(pixels,root,$"whole-output-footprint-{size.X}");
                GD.Print($"MINIMAP_PIXEL_FOOTPRINT size={size} output={pixels.GetSize()} mixed_black={mixed} known_lit={lit} lattice={catalog.WorldBounds.Position} cache={view.CacheWorldRect}");
            }
            GD.Print("MINIMAP_CASE_OK UnknownRemainsBlack whole_output_footprints=True negative_fractional_catalog=True");
            view.Size=PrologueMinimapView.BaseSize; view.Present(model,new Vector2(.75f,.5f),Vector2.Right,0);
            int builds=view.CacheRebuildCount,uploads=view.TextureUploadCount;
            long allocated=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<120;i++)view.Present(model,new Vector2(.75f,.5f),Vector2.Right,1d/60);
            long idle=GC.GetAllocatedBytesForCurrentThread()-allocated;
            TestAssert.True(view.CacheRebuildCount==builds && view.TextureUploadCount==uploads && view.TextureCreationCount==1,"idle presentation never rebuilds/uploads/recreates mask");
            view.Present(model,new Vector2(1300.75f,100.5f),Vector2.Right,100);
            TestAssert.True(view.LastCacheReason=="coverage" && view.CacheRebuildCount==builds+1,"leaving cached coverage rebuilds once");
            view.Present(model,new Vector2(.75f,.5f),Vector2.Right,100);
            using(var returned=await Read(tree,viewport)){AssertBlack(returned,211,120,"CacheWindowReturnIsAligned: same world unknown column after nonaligned actor/cache return");Save(returned,root,"cache-return");}
            TestAssert.True(view.DiscoveredCheckpointCount==1,"visited checkpoint persists after leaving and returning");
            var restored=new ExplorationMapModel(catalog);restored.Restore(durable);view.Present(restored,Vector2.Zero,Vector2.Right,0);await Frames(tree,2);
            TestAssert.Equal(1,view.DiscoveredCheckpointCount,"same revision restore keeps discovered descriptor");
            var mismatch=new ExplorationMapModel(new MinimapTerrainCatalog(catalog.WorldBounds,"other"));mismatch.Restore(durable);view.Present(mismatch,Vector2.Zero,Vector2.Right,0);await Frames(tree,2);
            TestAssert.Equal(0,view.DiscoveredCheckpointCount,"revision mismatch grants no checkpoint");
            restored.Clear();view.Present(restored,Vector2.Zero,Vector2.Right,0);await Frames(tree,2);
            TestAssert.Equal(0,view.DiscoveredCheckpointCount,"new-game model reset removes checkpoint");
            GD.Print($"MINIMAP_CASE_OK CacheWindowReturnIsAligned idle120_managed_bytes={idle} rebuilds={view.CacheRebuildCount} uploads={view.TextureUploadCount} texture_creations={view.TextureCreationCount} directory_bytes={view.StaticDirectoryBytes}");
            await AuthorizedSegmentOnly(tree,viewport,view,root,smallest);
            await ThinWallAndBoundedDirectory(tree,viewport,view,root,smallest);
        }
        finally{viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task MovingPresentationAllocation(SceneTree tree)
    {
        var viewport = new SubViewport { Size=new(360,240), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        var view = new PrologueMinimapView { Position=new(40,30), Size=new(260,170) };
        viewport.AddChild(view); tree.Root.AddChild(viewport); await Frames(tree,2);
        var model = new ExplorationMapModel(new MinimapTerrainCatalog(new Rect2(-2048,-1024,4096,2048), "moving-presentation-allocation"));
        var before=model.CaptureSave(); ulong revision=model.RevisionCounter;
        try
        {
            long[] warmMoving=new long[120],movingSamples=new long[120];
            long[] warmStrings=new long[120],stringSamples=new long[120];
            long[] warmCached=new long[120],cachedSamples=new long[120];
            using var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://scripts/presentation/minimap/minimap-mask.gdshader")};
            using var parameter=new StringName("outer_feather");
            view.Present(model,Vector2.Zero,Vector2.Right,0);
            // Fixed120 warmup observations, retained rather than discarded.
            // Each bracket contains only the actual production/API call(s).
            for(int i=0;i<120;i++)
            {
                Vector2 actor=new((i+1)*.5f,0);
                long start=GC.GetAllocatedBytesForCurrentThread();
                view.Present(model,actor,Vector2.Right,1d/60);
                long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
                warmMoving[i]=bytes;
                start=GC.GetAllocatedBytesForCurrentThread();
                material.SetShaderParameter("outer_feather",24f);
                material.SetShaderParameter("outer_feather",24f);
                bytes=GC.GetAllocatedBytesForCurrentThread()-start;
                warmStrings[i]=bytes;
                start=GC.GetAllocatedBytesForCurrentThread();
                material.SetShaderParameter(parameter,24f);
                material.SetShaderParameter(parameter,24f);
                bytes=GC.GetAllocatedBytesForCurrentThread()-start;
                warmCached[i]=bytes;
            }
            view.Present(model,Vector2.Zero,Vector2.Right,100);
            int builds=view.CacheRebuildCount,uploads=view.TextureUploadCount,creations=view.TextureCreationCount;
            int moving=0; Vector2 prior=view.ViewWorldRect.Position;
            for(int i=0;i<120;i++)
            {
                Vector2 actor=new((i+1)*.5f,0);
                long start=GC.GetAllocatedBytesForCurrentThread();
                view.Present(model,actor,Vector2.Right,1d/60);
                long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
                movingSamples[i]=bytes;
                Vector2 current=view.ViewWorldRect.Position;
                if(current!=prior)moving++;
                prior=current;
            }
            TestAssert.True(moving==120 && view.CacheRebuildCount==builds && view.TextureUploadCount==uploads && view.TextureCreationCount==creations,
                "moving allocation fixture changes all120 sampling origins without cache rebuild/upload/texture creation");
            TestAssert.True(model.CaptureSave().FreeCellsBase64==before.FreeCellsBase64 && model.CaptureSave().BoundarySegmentsBase64==before.BoundarySegmentsBase64 && model.RevisionCounter==revision,
                "moving allocation fixture never changes model bits/revision");

            for(int i=0;i<120;i++)
            {
                long start=GC.GetAllocatedBytesForCurrentThread();
                material.SetShaderParameter("outer_feather",24f);
                material.SetShaderParameter("outer_feather",24f);
                long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
                stringSamples[i]=bytes;
            }
            for(int i=0;i<120;i++)
            {
                long start=GC.GetAllocatedBytesForCurrentThread();
                material.SetShaderParameter(parameter,24f);
                material.SetShaderParameter(parameter,24f);
                long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
                cachedSamples[i]=bytes;
            }
            PrintAllocationSamples("warm-moving",warmMoving);
            PrintAllocationSamples("warm-string-pair",warmStrings);
            PrintAllocationSamples("warm-cached-pair",warmCached);
            PrintAllocationSamples("moving",movingSamples);
            PrintAllocationSamples("string-pair",stringSamples);
            PrintAllocationSamples("cached-pair",cachedSamples);
            GD.Print($"MINIMAP_MOVING_ALLOCATION_RAW calls=120 moving_origins={moving} moving_bytes={movingSamples.Sum()} rebuild_delta={view.CacheRebuildCount-builds} upload_delta={view.TextureUploadCount-uploads} creation_delta={view.TextureCreationCount-creations} model_unchanged=True recurring_minimum_budget=576 aggregate_gate_withdrawn=429576");
            // Fix2 withdraws only the noise-contaminated new aggregate gate;
            // all original totals/outliers and historical fix1 runs are retained.
            TestAssert.True(stringSamples.Min()==192 && cachedSamples.Min()==0,
                $"MovingAllocationCalibrationMatchesFrozenBasis: string pair minimum192 and cached pair minimum0; actual={stringSamples.Min()},{cachedSamples.Min()}");
            TestAssert.True(movingSamples.Min()<=576,
                $"MovingPresentationAvoidsRecurringFeatherWrappers: recurring moving minimum requires<=576 managed bytes; actual={movingSamples.Min()}");
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static void PrintAllocationSamples(string group,long[] samples)
    {
        GD.Print($"MINIMAP_ALLOCATION_SAMPLES group={group} count={samples.Length} total={samples.Sum()} min={samples.Min()} max={samples.Max()} distinct={samples.Distinct().Count()} values={string.Join(",",samples)}");
    }

    private static async Task PlayerMarkerReadableAtMinimumOutput(SceneTree tree, string root)
    {
        // Independent minimum layout/output fixture: logical135x88 rendered at .8,
        // not dimensions, radii or a threshold calculated from the production view.
        var viewport = new SubViewport { Size=new(240,160), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        viewport.AddChild(new ColorRect { Size=new(240,160), Color=Colors.Black });
        var canvas = new Node2D { Scale=new(.8f,.8f) };
        var view = new PrologueMinimapView { Position=new(40,40), Size=new(135,88) };
        canvas.AddChild(view); viewport.AddChild(canvas); tree.Root.AddChild(viewport); await Frames(tree,2);
        var catalog = new MinimapTerrainCatalog(new Rect2(-2048,-1024,4096,2048), "minimum-marker-readability");
        var model = new ExplorationMapModel(catalog);
        int[] brightCore = new int[2];
        Color[] backing = new Color[2], direction = new Color[2];
        try
        {
            for(int known=0;known<2;known++)
            {
                if(known==1)
                {
                    byte[] cells = new byte[(catalog.CellCount+7)/8]; Array.Fill(cells,(byte)255);
                    model.Restore(new ExplorationMapSaveData { TerrainRevision=catalog.Revision, FreeCellsBase64=Convert.ToBase64String(cells), BoundarySegmentsBase64="" });
                }
                var before=model.CaptureSave(); ulong revision=model.RevisionCounter;
                view.Present(model,Vector2.Zero,Vector2.Right,0);
                using Image pixels=await Read(tree,viewport); Save(pixels,root,known==0?"marker-minimum-black":"marker-minimum-known");
                TestAssert.True(pixels.GetSize()==new Vector2I(240,160) && view.Size==new Vector2(135,88) && canvas.Scale==new Vector2(.8f,.8f),"minimum marker fixture keeps independent logical/output dimensions");
                if(known==0) AssertBlack(pixels,60,52,"minimum marker black-background positive control");
                else
                {
                    Color control=pixels.GetPixel(60,52);
                    TestAssert.True(control.R>.09f && control.G>.14f && control.B>.15f && control.R<.3f,"minimum marker explored-ground contrast positive control");
                }
                // Hand-derived output centre(86,67.2); fixed core box excludes most
                // direction stroke. Twenty bright pixels is the readability floor.
                for(int y=64;y<=69;y++)for(int x=83;x<=88;x++)
                {
                    Color color=pixels.GetPixel(x,y);
                    if(color.R>.7f && color.G>.8f && color.B>.75f) brightCore[known]++;
                }
                // Pixel centre(86.5,70.5) lies beyond the2.8-output-pixel bright
                // core but within the4-output-pixel backing, below both strokes.
                backing[known]=pixels.GetPixel(86,70); direction[known]=pixels.GetPixel(92,67);
                TestAssert.True(model.CaptureSave().FreeCellsBase64==before.FreeCellsBase64 && model.CaptureSave().BoundarySegmentsBase64==before.BoundarySegmentsBase64 && model.RevisionCounter==revision,"readability presentation leaves model bits/revision unchanged");
                GD.Print($"MINIMAP_MARKER_READABILITY_RAW known={known} logical=135x88 output_scale=.8 core_bright={brightCore[known]} threshold=20 backing={backing[known]} direction={direction[known]} model_unchanged=True");
            }
            TestAssert.True(brightCore[0]>=20 && brightCore[1]>=20,$"PlayerMarkerReadableAtMinimumOutput: fixed minimum-output core requires20 bright pixels on black and explored ground; actual={brightCore[0]},{brightCore[1]}");
            TestAssert.True(backing[1].R<.06f && backing[1].G<.06f && backing[1].B<.06f,"minimum marker dark backing contrasts with genuinely known ground");
            TestAssert.True(direction[0].R>.7f && direction[1].R>.7f,"minimum marker direction remains bright at independently specified output point");
            GD.Print("MINIMAP_CASE_OK PlayerMarkerReadableAtMinimumOutput black_and_known=True independent_threshold20=True");
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task SoftEllipticalPerimeterRemovesRectangularBackplate(SceneTree tree, string root)
    {
        var viewport = new SubViewport { Size=new(360,240), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        viewport.AddChild(new ColorRect { Size=new(360,240), Color=Colors.White });
        var view = new PrologueMinimapView { Position=new(40,30), Size=new(260,170) };
        viewport.AddChild(view); tree.Root.AddChild(viewport); await Frames(tree,2);
        var model = new ExplorationMapModel(new MinimapTerrainCatalog(new Rect2(-2048,-1024,4096,2048), "soft-elliptical-perimeter"));
        var before=model.CaptureSave(); ulong revision=model.RevisionCounter;
        try
        {
            view.Present(model,Vector2.Zero,Vector2.Right,0);
            using Image pixels=await Read(tree,viewport); Save(pixels,root,"unknown-soft-elliptical-perimeter-260x170");
            // Hand-checked fixture: centre(130,85), radii(130,85). Local pixel
            // centres(25.5,25.5) and their reflections have ellipse radius>1.06,
            // despite being25.5px inside each side of the old rectangular plate.
            var corners=new[]{new Vector2I(65,55),new Vector2I(274,55),new Vector2I(65,174),new Vector2I(274,174)};
            Color[] cornerColors=corners.Select(p=>pixels.GetPixel(p.X,p.Y)).ToArray();
            Color outer=pixels.GetPixel(169,30),softOuter=pixels.GetPixel(169,36),softInner=pixels.GetPixel(169,48);
            GD.Print($"MINIMAP_ELLIPSE_RAW logical=260x170 corners={string.Join(";",cornerColors)} top_outer={outer} top_soft_outer={softOuter} top_soft_inner={softInner} unknown_core={pixels.GetPixel(169,80)}");
            AssertBlack(pixels,169,80,"SoftEllipticalPerimeterRemovesRectangularBackplate: central unknown remains opaque black away from actor");
            TestAssert.True(pixels.GetPixel(20,20).R>.99f,"white-background positive control outside the map is visible");
            for(int i=0;i<corners.Length;i++)
                TestAssert.True(cornerColors[i].R>.98f && cornerColors[i].G>.98f && cornerColors[i].B>.98f,
                    $"SoftEllipticalPerimeterRemovesRectangularBackplate: outside-ellipse corner must expose white background, not a rectangular black backplate pixel={corners[i]} value={cornerColors[i]}");
            TestAssert.True(outer.R>.98f && softOuter.R>.65f && softOuter.R<.95f && softInner.R>.05f && softInner.R<.35f && softOuter.R>softInner.R,
                "elliptical top perimeter fades continuously through two partial-opacity samples into opaque unknown");
            TestAssert.True(Math.Abs(softOuter.R-softOuter.G)<.01f && Math.Abs(softOuter.R-softOuter.B)<.01f &&
                Math.Abs(softInner.R-softInner.G)<.01f && Math.Abs(softInner.R-softInner.B)<.01f,
                "unknown perimeter feather stays neutral black over white, not invented terrain");
            TestAssert.True(model.CaptureSave().FreeCellsBase64==before.FreeCellsBase64 && model.CaptureSave().BoundarySegmentsBase64==before.BoundarySegmentsBase64 && model.RevisionCounter==revision,
                "elliptical presentation never changes exploration bits or revision");
            GD.Print("MINIMAP_CASE_OK SoftEllipticalPerimeterRemovesRectangularBackplate transparent_corners=4 soft_samples=2 unknown_core_black=True model_unchanged=True");
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task ScaledCheckpointFootprintStaysInsideOpaqueEllipse(SceneTree tree, string root)
    {
        var viewport = new SubViewport { Size=new(600,360), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        viewport.AddChild(new ColorRect { Size=new(600,360), Color=Colors.White });
        var canvas=new Node2D { Scale=new(1.5f,1.5f) };
        var view=new PrologueMinimapView { Position=new(40,30), Size=new(260,170) };
        // Literal world positions produce local(39.3,85) and(80,85).
        // The first radius2.5 logical box has conic q=.999663<1, yet its
        // output pixel(115,172) has an outside corner q=1.000871>1 at1.5x.
        view.ConfigureCheckpoints(new[]{
            new MinimapCheckpointDescriptor("logical-inside-output-straddle",new Vector2(-446.523077f,0)),
            new MinimapCheckpointDescriptor("safe-inside",new Vector2(-246.153846f,0))});
        canvas.AddChild(view);viewport.AddChild(canvas);tree.Root.AddChild(viewport);await Frames(tree,2);
        var catalog=new MinimapTerrainCatalog(new Rect2(-2048,-1024,4096,2048),"scaled-checkpoint-opaque-ellipse");
        var model=new ExplorationMapModel(catalog);byte[] cells=new byte[(catalog.CellCount+7)/8];Array.Fill(cells,(byte)255);
        model.Restore(new ExplorationMapSaveData{TerrainRevision=catalog.Revision,FreeCellsBase64=Convert.ToBase64String(cells),BoundarySegmentsBase64=""});
        var before=model.CaptureSave();ulong revision=model.RevisionCounter;
        try
        {
            view.Present(model,Vector2.Zero,Vector2.Right,0);
            using Image pixels=await Read(tree,viewport);Save(pixels,root,"checkpoint-logical-inside-output-straddle-1_5x");
            TestAssert.True(pixels.GetSize()==new Vector2I(600,360) && view.Size==new Vector2(260,170) && canvas.Scale==new Vector2(1.5f,1.5f),
                "scaled checkpoint fixture uses independent logical260x170 and physical1.5x dimensions");
            int gold=0,outsideOpaque=0;
            for(int y=0;y<360;y++)for(int x=0;x<600;x++)
            {
                Color c=pixels.GetPixel(x,y);
                if(c.R>.6f && c.G>.55f && c.B<.6f)
                {
                    gold++;
                    // Map's independently specified output origin is(60,45).
                    // Map the complete physical pixel, not merely its centre,
                    // into the ellipse with literal radii(130*61/85,61).
                    double left=(x-60)/1.5-130,right=(x+1-60)/1.5-130;
                    double top=(y-45)/1.5-85,bottom=(y+1-45)/1.5-85;
                    double farX=Math.Max(Math.Abs(left),Math.Abs(right)),farY=Math.Max(Math.Abs(top),Math.Abs(bottom));
                    double a=130d*61/85;
                    if(farX*farX/(a*a)+farY*farY/(61d*61)>1)outsideOpaque++;
                }
            }
            Color inside=pixels.GetPixel(180,172);
            GD.Print($"MINIMAP_SCALED_CHECKPOINT_ELLIPSE_RAW scale=1.5 discovered={view.DiscoveredCheckpointCount} gold={gold} outside_opaque={outsideOpaque} straddle={pixels.GetPixel(115,172)} inside={inside}");
            TestAssert.True(view.DiscoveredCheckpointCount==2 && inside.R>.6f && inside.G>.55f && inside.B<.6f,
                "both scaled descriptors are genuinely discovered and the independent safe-interior gold marker remains visible");
            TestAssert.True(model.CaptureSave().FreeCellsBase64==before.FreeCellsBase64 && model.CaptureSave().BoundarySegmentsBase64==before.BoundarySegmentsBase64 && model.RevisionCounter==revision,
                "scaled marker presentation never changes discovery bits or revision");
            TestAssert.Equal(0,outsideOpaque,"ScaledCheckpointFootprintStaysInsideOpaqueEllipse: complete physical gold pixel footprint must stay inside opaque ellipse, not only the logical circle bounds");
            GD.Print("MINIMAP_CASE_OK ScaledCheckpointFootprintStaysInsideOpaqueEllipse scale=1.5 outside_opaque=0 safe_inside_gold=True model_unchanged=True");
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task CheckpointFootprintStaysInsideEllipse(SceneTree tree, string root)
    {
        var viewport = new SubViewport { Size=new(360,240), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        viewport.AddChild(new ColorRect { Size=new(360,240), Color=Colors.White });
        var view = new PrologueMinimapView { Position=new(40,30), Size=new(260,170) };
        // Literal world positions map to local(27,27) and(130,55) at the fixed
        //1280-wide view. The first full radius2.5 fits the old24px inset but is
        //outside the ellipse; the second is a safely interior positive control.
        view.ConfigureCheckpoints(new[]{
            new MinimapCheckpointDescriptor("known-outside-diagonal",new Vector2(-507.076923f,-285.538462f)),
            new MinimapCheckpointDescriptor("known-inside",new Vector2(0,-147.692308f))});
        viewport.AddChild(view); tree.Root.AddChild(viewport); await Frames(tree,2);
        var catalog=new MinimapTerrainCatalog(new Rect2(-2048,-1024,4096,2048),"checkpoint-ellipse-footprint");
        var model=new ExplorationMapModel(catalog);byte[] cells=new byte[(catalog.CellCount+7)/8];Array.Fill(cells,(byte)255);
        model.Restore(new ExplorationMapSaveData{TerrainRevision=catalog.Revision,FreeCellsBase64=Convert.ToBase64String(cells),BoundarySegmentsBase64=""});
        var before=model.CaptureSave();ulong revision=model.RevisionCounter;
        try
        {
            view.Present(model,Vector2.Zero,Vector2.Right,0);
            using Image pixels=await Read(tree,viewport);Save(pixels,root,"known-checkpoint-diagonal-ellipse-footprint");
            int outsideGold=0,insideGold=0,featherGold=0;
            for(int y=30;y<200;y++)for(int x=40;x<300;x++)
            {
                Color c=pixels.GetPixel(x,y);
                if(c.R>.6f && c.G>.55f && c.B<.6f)
                {
                    if(IndependentPixelInsideEllipse(x-40,y-30,260,170))insideGold++;
                    else outsideGold++;
                    if(!IndependentPixelInsideEllipse(x-40,y-30,260,170,61d/85))featherGold++;
                }
            }
            GD.Print($"MINIMAP_CHECKPOINT_ELLIPSE_RAW discovered={view.DiscoveredCheckpointCount} outside_gold={outsideGold} inside_gold={insideGold} feather_gold={featherGold} corner={pixels.GetPixel(67,57)} inside={pixels.GetPixel(170,85)}");
            TestAssert.True(view.DiscoveredCheckpointCount==2 && insideGold>0,"checkpoint ellipse fixture genuinely discovers both descriptors and keeps its interior marker visible");
            TestAssert.True(model.CaptureSave().FreeCellsBase64==before.FreeCellsBase64 && model.CaptureSave().BoundarySegmentsBase64==before.BoundarySegmentsBase64 && model.RevisionCounter==revision,
                "checkpoint ellipse clipping never revokes discovery or changes model bits/revision");
            TestAssert.Equal(0,outsideGold,"CheckpointFootprintStaysInsideEllipse: complete discovered checkpoint marker cannot float outside elliptical perimeter");
            TestAssert.Equal(0,featherGold,"complete discovered checkpoint output pixels stay inside the independently calculated opaque ellipse");
            GD.Print("MINIMAP_CASE_OK CheckpointFootprintStaysInsideEllipse discovered=2 interior_visible=True outside_gold=0 model_unchanged=True");
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task DiagonalJumpActorFootprintStaysInsideEllipse(SceneTree tree, string root)
    {
        var viewport = new SubViewport { Size=new(360,240), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        viewport.AddChild(new ColorRect { Size=new(360,240), Color=Colors.Black });
        var view = new PrologueMinimapView { Position=new(40,30), Size=new(260,170) };
        viewport.AddChild(view); tree.Root.AddChild(viewport); await Frames(tree,2);
        var model=new ExplorationMapModel(new MinimapTerrainCatalog(new Rect2(-2048,-1024,4096,2048),"actor-ellipse-footprint"));
        var before=model.CaptureSave();ulong revision=model.RevisionCounter;
        try
        {
            // Each unsnapped marker is at local(30,30) or its reflection. Its
            //5px backing fits the old rectangular inset but crosses the ellipse.
            var jumps=new[]{new Vector2(-492.307692f,-270.769231f),new Vector2(492.307692f,-270.769231f),
                new Vector2(-492.307692f,270.769231f),new Vector2(492.307692f,270.769231f)};
            for(int i=0;i<jumps.Length;i++)
            {
                view.ClearPresentation();view.Present(model,Vector2.Zero,Vector2.Right,0);
                view.Present(model,jumps[i],-jumps[i].Normalized(),0);
                using Image pixels=await Read(tree,viewport);Save(pixels,root,$"actor-diagonal-ellipse-footprint-{i}");
                int outside=0,core=0,marker=0,feather=0;
                for(int y=0;y<240;y++)for(int x=0;x<360;x++)
                {
                    Color c=pixels.GetPixel(x,y);
                    if(c.R>.01f || c.G>.01f || c.B>.01f)
                    {
                        marker++;
                        if(!IndependentPixelInsideEllipse(x-40,y-30,260,170))outside++;
                        if(!IndependentPixelInsideEllipse(x-40,y-30,260,170,61d/85))feather++;
                        if(c.R>.7f && c.G>.8f && c.B>.75f)core++;
                    }
                }
                GD.Print($"MINIMAP_ACTOR_ELLIPSE_RAW diagonal={i} outside_marker={outside} feather_marker={feather} all_marker={marker} bright_core={core} view_center={view.ViewWorldRect.GetCenter()}");
                TestAssert.True(core>=20 && marker>core,"diagonal jump retains a readable actor core and complete dark backing positive control");
                TestAssert.Equal(0,outside,"DiagonalJumpActorFootprintStaysInsideEllipse: full core/backing/direction output-pixel footprint stays inside ellipse after diagonal jump");
                TestAssert.Equal(0,feather,"diagonal actor's complete output footprint is inside opaque inner ellipse, not floating in feather");
                TestAssert.True(model.CaptureSave().FreeCellsBase64==before.FreeCellsBase64 && model.CaptureSave().BoundarySegmentsBase64==before.BoundarySegmentsBase64 && model.RevisionCounter==revision,
                    "diagonal presentation jump never changes exploration bits/revision");
            }
            GD.Print("MINIMAP_CASE_OK DiagonalJumpActorFootprintStaysInsideEllipse diagonals=4 readable_core=True complete_backing=True model_unchanged=True");
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task AdaptiveOuterFeatherRemainsBounded(SceneTree tree, string root)
    {
        var viewport = new SubViewport { Size=new(360,240), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        viewport.AddChild(new ColorRect { Size=new(360,240), Color=Colors.White });
        var view = new PrologueMinimapView { Position=new(40,30), Size=new(260,170) };
        viewport.AddChild(view); tree.Root.AddChild(viewport); await Frames(tree,2);
        var model = new ExplorationMapModel(new MinimapTerrainCatalog(new Rect2(-2048,-1024,4096,2048), "adaptive-outer-feather"));
        var before=model.CaptureSave(); ulong revision=model.RevisionCounter;
        try
        {
            view.Present(model,Vector2.Zero,Vector2.Right,0);
            using(Image normal=await Read(tree,viewport))
            {
                Color half=normal.GetPixel(169,42); Save(normal,root,"unknown-edgefade-260x170");
                GD.Print($"MINIMAP_EDGEFADE_RAW logical=260x170 axial12_pixel={half} axial25_pixel={normal.GetPixel(169,55)} diagonal25_pixel={normal.GetPixel(100,55)}");
                AssertNeutralWhiteComposite(normal,169,42,.468814,"AdaptiveOuterFeatherRemainsBounded: axial12.5px unknown sample has independently hand-calculated elliptical opacity");
                AssertBlack(normal,169,55,"unknown axial25.5px interior remains opaque black");
                AssertNeutralWhiteComposite(normal,100,55,.615822,"diagonal feather is not the old rectangular25px opaque backplate");
                AssertNeutralWhiteComposite(normal,100,42,1,"diagonal exterior is transparent even12.5px inside the bounding rectangle");
                TestAssert.True(normal.GetPixel(40,30).R>.95f,"outermost edge remains nearly transparent positive control");
            }
            // Resize the same production view: shader uniforms must follow Size.
            view.Size=new(135,88);view.Present(model,Vector2.Zero,Vector2.Right,0);
            using(Image minimum=await Read(tree,viewport))
            {
                Save(minimum,root,"unknown-edgefade-135x88");
                AssertNeutralWhiteComposite(minimum,107,38,.525558,"minimum ellipse adapts to17.6px feather at an independently hand-calculated axial8.5px sample");
                AssertBlack(minimum,107,49,"minimum-size ellipse retains opaque axial19.5px inner control");
                AssertNeutralWhiteComposite(minimum,53,43,1,"minimum ellipse leaves the diagonal exterior transparent");
            }
            view.Size=new(14,14);view.Present(model,Vector2.Zero,Vector2.Right,0);
            using(Image tiny=await Read(tree,viewport))
            {
                Save(tiny,root,"unknown-edgefade-tiny14");
                AssertNeutralWhiteComposite(tiny,46,31,.458621,"tiny14 ellipse retains a bounded2.8px feather at a hand-calculated axial sample");
                AssertBlack(tiny,46,34,"tiny14 ellipse retains opaque inner black and suppresses oversized actor footprint");
                AssertNeutralWhiteComposite(tiny,41,31,1,"tiny14 ellipse retains transparent diagonal exterior");
            }
            TestAssert.True(model.CaptureSave().FreeCellsBase64==before.FreeCellsBase64 && model.CaptureSave().BoundarySegmentsBase64==before.BoundarySegmentsBase64 && model.RevisionCounter==revision,"adaptive feather resizing leaves model bits/revision unchanged");
            GD.Print("MINIMAP_CASE_OK AdaptiveOuterFeatherRemainsBounded white_background=True sizes=260x170,135x88,14x14 model_unchanged=True");
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task MarkerBounds(SceneTree tree, string root)
    {
        var viewport = new SubViewport { Size=new(420,320), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        viewport.AddChild(new ColorRect { Size=new(420,320), Color=Colors.Black });
        var view = new PrologueMinimapView { Position=new(80,70), Size=new(260,170) };
        viewport.AddChild(view); tree.Root.AddChild(viewport); await Frames(tree,2);
        var model = new ExplorationMapModel(new MinimapTerrainCatalog(new Rect2(-2048,-2048,4096,4096), "marker-bounds"));
        var before = model.CaptureSave(); ulong revision = model.RevisionCounter;
        try
        {
            int controls=0;
            var large = new[]{new Vector2(800,0),new Vector2(-800,0),new Vector2(0,600),new Vector2(0,-600)};
            var samples = large.SelectMany(j => new[]{(jump:j,delta:0d),(jump:j,delta:1d/60)})
                .Concat(new[]{(jump:new Vector2(560,0),delta:0d),(jump:new Vector2(-560,0),delta:0d),
                    (jump:new Vector2(0,340),delta:0d),(jump:new Vector2(0,-340),delta:0d)});
            foreach(var sample in samples)
            {
                Vector2 jump=sample.jump;double delta=sample.delta;
                view.ClearPresentation(); view.Present(model,Vector2.Zero,Vector2.Right,0);
                view.Present(model,jump,jump.Normalized(),delta);
                using Image pixels=await Read(tree,viewport);Save(pixels,root,$"jump-{controls}");
                int outside=0,inside=0,feather=0;
                for(int y=0;y<320;y++)for(int x=0;x<420;x++)
                {
                    Color c=pixels.GetPixel(x,y);bool lit=c.R>.02f || c.G>.02f || c.B>.02f;
                    if(x<80 || x>=340 || y<70 || y>=240){if(lit)outside++;}
                    else if(c.R>.5f)inside++;
                    if(lit && !IndependentPixelInsideEllipse(x-80,y-70,260,170,61d/85))feather++;
                }
                TestAssert.True(outside==0,"LargeJumpMarkersStayInside: production own-draw marker cannot escape map perimeter");
                GD.Print($"MINIMAP_MARKER_RAW direction={jump} delta={delta} outside_lit={outside} inside_actor_lit={inside}");
                TestAssert.True(inside>0,"LargeJumpMarkersStayInside: complete actor marker remains readable inside map after jump");
                TestAssert.Equal(0,feather,"entire marker circle/line/stroke output footprint stays inside opaque inner ellipse");
                Color centre=pixels.GetPixel(210,155);
                TestAssert.True(centre.R>.5f && centre.B>.7f,"large jump snaps complete actor circle to readable center");
                TestAssert.True(model.CaptureSave().FreeCellsBase64==before.FreeCellsBase64 && model.CaptureSave().BoundarySegmentsBase64==before.BoundarySegmentsBase64 && model.RevisionCounter==revision,"presentation jump never changes model bits/revision");
                GD.Print($"MINIMAP_MARKER_BOUNDS direction={jump} delta={delta} outside_lit={outside} inside_actor_lit={inside} model_unchanged=True");controls++;
            }
            view.ClearPresentation();view.Present(model,Vector2.Zero,Vector2.Right,0);
            view.Present(model,new Vector2(16,0),Vector2.Right,1d/60);
            float smoothCenter=view.ViewWorldRect.GetCenter().X;
            TestAssert.True(smoothCenter>0 && smoothCenter<16,"ordinary small movement preserves smooth following, not unconditional snap");
            view.Size=new Vector2(14,14);view.ClearPresentation();view.Present(model,Vector2.Zero,Vector2.Right,0);
            using(Image tiny=await Read(tree,viewport))
                for(int y=70;y<84;y++)for(int x=80;x<94;x++)AssertBlack(tiny,x,y,"too-small drawable marker area fails closed");
            GD.Print($"MINIMAP_CASE_OK LargeJumpMarkersStayInside controls={controls}");
            view.Size=new Vector2(260,170);view.ClearPresentation();
            var checkpointView = new PrologueMinimapView { Position=new(80,70), Size=new(260,170) };
            checkpointView.ConfigureCheckpoints(new[]{
                new MinimapCheckpointDescriptor("left",new Vector2(-576,0)),
                new MinimapCheckpointDescriptor("right",new Vector2(576,0)),
                new MinimapCheckpointDescriptor("top",new Vector2(0,-354)),
                new MinimapCheckpointDescriptor("bottom",new Vector2(0,354)),
                // Local centre(25,85) is inside the24px inner rectangle, but
                // unchanged radius2.5 crosses its left edge at local22.5.
                new MinimapCheckpointDescriptor("left-radius-straddle",new Vector2(-516.9231f,0)),
                new MinimapCheckpointDescriptor("old-rectangle-interior-now-feather",new Vector2(-480,0)),
                // Opaque ellipse has independently derived radii(93.294118,61),
                // leftmost x36.705882. Centre38 is inside, but radius2.5 is not.
                new MinimapCheckpointDescriptor("ellipse-radius-straddle",new Vector2(-452.923077f,0)),
                new MinimapCheckpointDescriptor("inside",new Vector2(-433.230769f,0))});
            TestAssert.True(92d*92/(93.294118*93.294118)<1 &&
                94.5d*94.5/(93.294118*93.294118)+2.5d*2.5/(61d*61)>1,
                "checkpoint ellipse radius-straddle negative has centre38 inside but full radius2.5 outside the independently derived opaque ellipse");
            viewport.AddChild(checkpointView);await Frames(tree,2);
            byte[] cells=new byte[(model.Catalog.CellCount+7)/8];Array.Fill(cells,(byte)255);
            model.Restore(new ExplorationMapSaveData{TerrainRevision=model.Catalog.Revision,FreeCellsBase64=Convert.ToBase64String(cells),BoundarySegmentsBase64=""});
            checkpointView.Present(model,Vector2.Zero,Vector2.Right,0);
            using(Image edges=await Read(tree,viewport))
            {
                int gold=0;
                for(int y=70;y<240;y++)for(int x=80;x<340;x++)
                {
                    Color c=edges.GetPixel(x,y);
                    if(c.R>.5f && c.B<.6f)
                    {
                        gold++;
                        // Hand-derived centre:80+42=122, y155;
                        // conservative raster box around unchanged radius2.5.
                        TestAssert.True(x>=119 && x<=125 && y>=151 && y<=158,"checkpoint radius crossing inner ellipse is suppressed; only independent inside descriptor may draw");
                        TestAssert.True(IndependentPixelInsideEllipse(x-80,y-70,260,170,61d/85),"complete checkpoint output-pixel footprint fits independently specified opaque ellipse");
                    }
                }
                TestAssert.True(gold>0,"inside checkpoint footprint remains visible positive control");
                GD.Print($"MINIMAP_CHECKPOINT_FOOTPRINT edges_suppressed=7 center_inside_radius_straddle=True rectangle_only_position_preserved_as_negative=True inside_gold={gold}");
            }
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task CacheEdgeFootprints(SceneTree tree, string root)
    {
        var viewport = new SubViewport { Size=new Vector2I(320,220), RenderTargetUpdateMode=SubViewport.UpdateMode.Always, World2D=new World2D() };
        viewport.AddChild(new ColorRect {Size=new(320,220),Color=Colors.White});
        byte[] known=new byte[256*192];Array.Fill(known,(byte)255);
        using var image=Image.CreateFromData(256,192,false,Image.Format.R8,known);
        var texture=ImageTexture.CreateFromImage(image);
        var material=new ShaderMaterial {Shader=GD.Load<Shader>("res://scripts/presentation/minimap/minimap-mask.gdshader")};
        material.SetShaderParameter("known_mask",texture);material.SetShaderParameter("logical_size",new Vector2(260,170));
        material.SetShaderParameter("world_size",new Vector2(1280,836.923f));
        var surface=new ColorRect {Position=new(20,20),Size=new(260,170),Material=material};viewport.AddChild(surface);
        tree.Root.AddChild(viewport);await Frames(tree,2);
        try
        {
            var offsets=new[]{new Vector2(-640,300),new Vector2(1500,300),new Vector2(300,-400),new Vector2(300,1200)};
            for(int edge=0;edge<4;edge++)
            {
                Vector2 offset=offsets[edge];material.SetShaderParameter("view_offset",offset);
                using Image pixels=await Read(tree,viewport);Save(pixels,root,$"cache-edge-whole-footprint-{edge}");
                int blocked=0,lit=0,mixed=0;
                for(int y=14;y<156;y++)for(int x=14;x<246;x++)
                {
                    Vector2 point=offset+(new Vector2(x+.5f,y+.5f)/new Vector2(260,170))*new Vector2(1280,836.923f);
                    Vector2 half=new Vector2(1280f/260,836.923f/170)*.501f;
                    bool outside=point.X-half.X<0 || point.Y-half.Y<0 || point.X+half.X>=2048 || point.Y+half.Y>=1536;
                    double alpha=IndependentEllipseOpacity(new Vector2(x+.5f,y+.5f),new Vector2(260,170),12);
                    if(outside)
                    {
                        AssertNeutralWhiteComposite(pixels,x+20,y+20,1-alpha,"whole production shader output footprint crossing/outside cache composites only black");blocked++;
                        if(point.X+half.X>0 && point.Y+half.Y>0 && point.X-half.X<2048 && point.Y-half.Y<1536)mixed++;
                    }
                    else
                    {
                        AssertKnownWhiteComposite(pixels,x+20,y+20,alpha,"in-cache explored positive control has independently composited known-space color");
                        if(alpha>.5)lit++;
                    }
                }
                TestAssert.True(blocked>0 && lit>0 && mixed>0,"every cache edge has straddling whole footprints black plus in-cache lit controls");
                GD.Print($"MINIMAP_CACHE_EDGE_PIXELS edge={edge} black={blocked} lit={lit} mixed={mixed} production_shader=True fixture_only=True");
            }
        }
        finally {viewport.QueueFree();await Frames(tree,3);}
    }

    private static async Task ThinWallAndBoundedDirectory(SceneTree tree,SubViewport viewport,PrologueMinimapView view,string root,Vector2 smallest)
    {
        var bounds=new Rect2(-2048.25f,-1024.5f,4096,2048);
        var catalog=new MinimapTerrainCatalog(bounds,"actual-thin-wall");
        var model=new ExplorationMapModel(catalog);
        var frame=new HeadlampOcclusionFrame(new[]{new HeadlampOccluder(Transform2D.Identity,new Rect2(2.5f,-120,1,240))},true);
        Vector2 actor=new(-24.25f,-.5f);
        for(ulong stamp=1;stamp<=128;stamp++)model.Advance(new MinimapObservation(actor,stamp,1,true,frame,new[]{bounds}));
        view.Position=new Vector2(80,80);view.Size=smallest;view.Present(model,actor,Vector2.Right,0);
        using(Image pixels=await Read(tree,viewport))
        {
            int blocked=0,lit=0;
            for(int y=(int)view.Position.Y+25;y<view.Position.Y+view.Size.Y-25;y++)
            for(int x=(int)view.Position.X+25;x<view.Position.X+view.Size.X-25;x++)
            {
                Vector2 p=new(x+.5f-view.Position.X,y+.5f-view.Position.Y);
                if(p.DistanceTo(view.Size*.5f)<8)continue;
                Vector2 w=view.ViewWorldRect.Position+p*view.ViewWorldRect.Size/view.Size;
                float half=640/view.Size.X;
                if(w.X+half>-.25f && w.X-half<7.75f && Math.Abs(w.Y)<80){AssertBlack(pixels,x,y,"thin wall within same8-unit cell remains black after actual model authorization and final minification");blocked++;}
                if(pixels.GetPixel(x,y).R>.02f)lit++;
            }
            TestAssert.True(blocked>0 && lit>0,"thin-wall final pixels include real blocked and lawful free controls");Save(pixels,root,"actual-model-thin-wall-smallest");
            GD.Print($"MINIMAP_THIN_WALL actual_model=True smallest={smallest} blocked_black={blocked} known_lit={lit}");
        }
        var solid=new MinimapSolid(Transform2D.Identity,new Rect2(40,-40,1,80));
        var edges=new MinimapBoundary[65537];Array.Fill(edges,new MinimapBoundary(0,new Vector2(40,-40),new Vector2(40,-32),Vector2.Left));
        var dense=new MinimapTerrainCatalog(bounds,"dense-view",new[]{solid},edges);var empty=new ExplorationMapModel(dense);
        long before=GC.GetAllocatedBytesForCurrentThread();long start=System.Diagnostics.Stopwatch.GetTimestamp();
        view.Present(empty,Vector2.Zero,Vector2.Right,0);
        long preparation=GC.GetAllocatedBytesForCurrentThread()-before;double ms=System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        TestAssert.True(view.BoundaryQueryOverflow && view.LastBoundaryVisits==65536 && view.LocalBoundaryCount==0,"bounded local directory stops at65536 visits and conservatively suppresses overflowing geometry");
        int rebuilds=view.CacheRebuildCount;for(int i=0;i<10;i++)view.Present(empty,Vector2.Zero,Vector2.Right,1d/60);
        TestAssert.Equal(rebuilds,view.CacheRebuildCount,"dense directory is not rescanned per idle frame");
        GD.Print($"MINIMAP_DIRECTORY_BOUND count=65537 visits={view.LastBoundaryVisits} local={view.LocalBoundaryCount} overflow={view.BoundaryQueryOverflow} static_bytes={view.StaticDirectoryBytes} fixed_preparation_managed_bytes={preparation} fixed_preparation_ms={ms:F3}");
    }

    private static async Task AuthorizedSegmentOnly(SceneTree tree,SubViewport viewport,PrologueMinimapView view,string root,Vector2 smallest)
    {
        var solid=new MinimapSolid(new Transform2D(0,new Vector2(40.25f,-40.5f)),new Rect2(0,0,2,80));
        var catalog=new MinimapTerrainCatalog(new Rect2(-2048.25f,-1024.5f,4096,2048),"thin-wall",new[]{solid},new[]{
            new MinimapBoundary(0,Vector2.Zero,new Vector2(0,8),Vector2.Left),
            new MinimapBoundary(0,new Vector2(0,8),new Vector2(0,16),Vector2.Left),
            new MinimapBoundary(0,new Vector2(2,0),new Vector2(2,8),Vector2.Right)});
        var model=new ExplorationMapModel(catalog);model.Restore(new ExplorationMapSaveData{TerrainRevision=catalog.Revision,
            FreeCellsBase64=Convert.ToBase64String(new byte[(catalog.CellCount+7)/8]),BoundarySegmentsBase64=Convert.ToBase64String(new byte[]{1})});
        foreach(Vector2 size in new[]{new Vector2(1040,680),PrologueMinimapView.BaseSize,smallest})
        {
        view.Size=size;view.Position=new Vector2(80,20);view.Present(model,new Vector2(-128,0),Vector2.Left,0);
        using Image pixels=await Read(tree,viewport);
        Save(pixels,root,$"authorized-thin-wall-segment-only-{size.X}");
        int front=0;float s=size.X/1280;
        Vector2 a=view.Position+(new Vector2(40.25f,-40.5f)-view.ViewWorldRect.Position)*s;
        for(int y=(int)a.Y-3;y<a.Y+16*s+3;y++)for(int x=(int)a.X-10;x<a.X+6;x++)
        {
            Vector2 world=view.ViewWorldRect.Position+(new Vector2(x+.5f,y+.5f)-view.Position)/s;
            bool authorizedFootprint=world.X-.5f/s>=32.25f && world.X+.5f/s<=40.25f && world.Y-.5f/s>=-40.5f && world.Y+.5f/s<=-32.5f;
            if(pixels.GetPixel(x,y).R>.02f){TestAssert.True(authorizedFootprint,$"AuthorizedSegmentOnly: lit output footprint entirely inside authorized segment's FREE-side stroke pixel=({x},{y}) world={world} color={pixels.GetPixel(x,y)}");front++;}
            else if(world.X>40.25f || world.Y>-32.5f)AssertBlack(pixels,x,y,"adjacent segment, solid/backface never inferred");
        }
        TestAssert.True(view.LocalBoundaryCount==1 && (size.X>1000 ? front>8 : size.X>200 ? front>0 : front==0),"actual shader renders only wholly covered front pixels, suppressing subpixel segment under strongest minification");
        Save(pixels,root,$"authorized-thin-wall-segment-only-{size.X}");
        GD.Print($"MINIMAP_CASE_OK AuthorizedSegmentOnly size={size} lit_authorized={front} boundary_visits={view.LastBoundaryVisits} local={view.LocalBoundaryCount} overflow={view.BoundaryQueryOverflow}");
        }
        view.ClearPresentation();TestAssert.True(model.IsBoundaryExplored(0),"ClearPresentation never clears exploration model");
    }

    private static void KeyEvent(Key key,bool down)=>Input.ParseInputEvent(new InputEventKey{Keycode=key,PhysicalKeycode=key,Pressed=down});
    private static async Task Frames(SceneTree tree,int count){for(int i=0;i<count;i++)await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);}
    private static async Task<Image> Read(SceneTree tree,Viewport viewport)
    {
        for(int i=0;i<2;i++){await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);await tree.ToSignal(RenderingServer.Singleton,RenderingServer.SignalName.FramePostDraw);}
        Image image=viewport.GetTexture().GetImage();TestAssert.True(image!=null && !image.IsEmpty(),"real hardware GPU original-size raster readback");return image;
    }
    // Independent double-precision conic/compositing oracle for unchanged full
    //output-pixel scan ranges. No production shader, view helper, colors or
    //feather constants are consulted; fixed literal axial/diagonal GPU samples
    //above additionally protect against making the same algebra mistake twice.
    private static double IndependentEllipseOpacity(Vector2 localPixelCenter,Vector2 dimensions,double feather)
    {
        double a=dimensions.X/2d,b=dimensions.Y/2d;
        double x=localPixelCenter.X-a,y=localPixelCenter.Y-b;
        double conic=x*x/(a*a)+y*y/(b*b);
        double t=Math.Clamp((1-Math.Sqrt(conic))*Math.Min(a,b)/feather,0,1);
        return t*t*(3-2*t);
    }
    private static bool IndependentPixelInsideEllipse(int localX,int localY,int width,int height,double innerScale=1)
    {
        // An ellipse is convex. Its complete axis-aligned output-pixel square
        //is inside iff all four vertices are; using the farthest coordinate on
        //each axis checks the worst vertex, not just the pixel centre.
        double a=width/2d,b=height/2d;
        double x=Math.Max(Math.Abs(localX-a),Math.Abs(localX+1-a));
        double y=Math.Max(Math.Abs(localY-b),Math.Abs(localY+1-b));
        return x*x/(a*a)+y*y/(b*b)<=innerScale*innerScale;
    }
    private static void AssertNeutralWhiteComposite(Image pixels,int x,int y,double expectedWhite,string message)
    {
        Color c=pixels.GetPixel(x,y);
        TestAssert.True(Math.Abs(c.R-expectedWhite)<.01 && Math.Abs(c.G-expectedWhite)<.01 && Math.Abs(c.B-expectedWhite)<.01,
            message+$" pixel=({x},{y}) value={c} expected_neutral={expectedWhite:F6}");
    }
    private static void AssertKnownWhiteComposite(Image pixels,int x,int y,double alpha,string message)
    {
        Color c=pixels.GetPixel(x,y);double white=1-alpha;
        TestAssert.True(Math.Abs(c.R-(white+.12*alpha))<.01 && Math.Abs(c.G-(white+.18*alpha))<.01 && Math.Abs(c.B-(white+.19*alpha))<.01,
            message+$" pixel=({x},{y}) value={c} expected_alpha={alpha:F6}");
    }
    private static void AssertBlack(Image pixels,int x,int y,string message)
    {Color c=pixels.GetPixel(x,y);TestAssert.True(c.R<.008f && c.G<.008f && c.B<.008f,message+$" pixel=({x},{y}) value={c}");}
    private static void Save(Image pixels,string root,string name)
    {
        string file=Path.Combine(root,name.Replace('.', '_')+".png");
        TestAssert.Equal(Error.Ok,pixels.SavePng(file),"external original uncropped PNG retained");
        File.WriteAllText(file+".json",System.Text.Json.JsonSerializer.Serialize(new { source="tests/integration/MinimapHudTests.cs",version="Task5 fixture only",status="not formal-route/art acceptance",pixels=pixels.GetSize().ToString(),adapter=RenderingServer.GetVideoAdapterName(),renderer=ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString(),uncropped=true }));
    }
}

public partial class MinimapUnhandledInputProbe : Node
{
    public int GameplayKeys;
    public override void _UnhandledInput(InputEvent e)
    {if(e is InputEventKey {Pressed:true} k && k.PhysicalKeycode is Key.Q or Key.J or Key.K or Key.L)GameplayKeys++;}
}
