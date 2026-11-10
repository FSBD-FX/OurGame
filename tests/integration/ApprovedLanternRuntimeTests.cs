using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Presentation;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Settings;
using Knight.Ui;
using Knight.Localization;
using Knight.Mechanisms;

namespace Knight.Tests.Integration;

/// <summary>Existing accepted PNG binding, not approval of the complete character.</summary>
public static class ApprovedLanternRuntimeTests
{
    private const string ApprovedPath = "res://assets/player/thread-spool-bearer/lantern.png";

    public static async Task RunVisible(SceneTree tree)
    {
        TestAssert.True(DisplayServer.GetName() != "headless" &&
            RenderingServer.GetCurrentRenderingMethod() == "gl_compatibility",
            "approved-lantern visible sample requires an actual Compatibility display");
        ulong started = Time.GetTicksMsec();
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        Exception primary = null;
        try
        {
            IsolatedPrologueFixture.Configure(level, "approved-lantern-visible");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false;
            flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await RenderFrames(tree, 3);
            TestAssert.Equal(PrologueGameFlowState.Title, flow.State,
                "saved normal level first shows its real title");
            flow.StartNew();
            while (flow.State == PrologueGameFlowState.Intro && Time.GetTicksMsec() - started < 22000)
                await RenderFrames(tree, 1);
            TestAssert.True(flow.State == PrologueGameFlowState.Playing && !level.ResidentPreparationPending,
                "sample waits for real normal session activation within its finite display time");
            var presentation = level.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            var formal = level.Player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            var lantern = formal.GetNode<PlayerFormalLayer>("Lantern");
            var fog = level.GetNode<PrologueFogController>("FogController");
            TestAssert.True(ReferenceEquals(presentation.ActiveVisual, formal),
                "visible sample uses the saved Player's real active formal visual");
            AssertBound(lantern);

            // Only this isolated display fixture freezes actor motion. These are
            // explicit scripted poses, never OS input or natural movement proof.
            level.Player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
            level.Player.Velocity = Vector2.Zero;
            presentation.SetPhysicsProcess(false);
            presentation.SetProcess(false);
            level.Player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
            level.GetViewport().GetCamera2D()?.ResetSmoothing();
            string output = IsolatedPrologueFixture.ValidateStorageRoot();
            foreach (int facing in new[] { 1, -1 })
            {
                presentation.Present(new EntityPresentationFrame(EntityVisualState.Idle, facing, true));
                await RenderFrames(tree, 3);
                Transform2D transform = level.GetViewport().GetFinalTransform() * lantern.GetGlobalTransformWithCanvas();
                Vector2 origin = formal.LanternWorldPosition;
                TestAssert.True(lantern.IsVisibleInTree() && formal.Scale.X * facing > 0 &&
                    lantern.Scale.X > 0 && fog.PresentedFrameCount > 0 &&
                    fog.LampWorldPosition.DistanceTo(origin) < .001f,
                    "displayed bitmap and actual fog source share the posed normal layer");
                using Image first = level.GetViewport().GetTexture().GetImage();
                await RenderFrames(tree, 2);
                using Image normal = level.GetViewport().GetTexture().GetImage();
                lantern.Hide();
                await RenderFrames(tree, 2);
                using Image hidden = level.GetViewport().GetTexture().GetImage();
                lantern.Show();
                TestAssert.True(origin.DistanceTo(formal.LanternWorldPosition) < .001f &&
                    transform.IsEqualApprox(level.GetViewport().GetFinalTransform() * lantern.GetGlobalTransformWithCanvas()),
                    "lamp-only negative control never moves actor/camera or changes scale");
                Rect2 bounds = PixelBounds(transform, new Rect2(-12, -12, 24, 24));
                int changed = ChangedPixels(normal, hidden, bounds);
                int noise = ChangedPixels(first, normal, bounds);
                TestAssert.True(changed >= 12 && changed > noise * 2,
                    $"actual lamp Draw must exceed stable-frame noise in its viewport ROI; facing={facing} changed={changed} noise={noise} bounds={bounds}");
                string filename = facing > 0 ? "approved-lantern-right.png" : "approved-lantern-left.png";
                string destination = Path.Combine(output, filename);
                TestAssert.True(!File.Exists(destination) && normal.SavePng(destination) == Error.Ok,
                    "retain original full-viewport PNG without overwriting prior evidence");
                GD.Print($"APPROVED_LANTERN_PIXEL_PROBE facing={facing} changed={changed} noise={noise} file={filename} scripted_pose=true");
                await RenderFrames(tree, 2);
                ulong holdUntil = facing > 0 ? Math.Min(started + 16000, Time.GetTicksMsec() + 4000) : started + 24000;
                while (Time.GetTicksMsec() < holdUntil) await RenderFrames(tree, 1);
            }
        }
        catch (Exception exception) { primary = exception; throw; }
        finally { await ReleaseOwnedLevel(tree, level, primary); }
        GD.Print("APPROVED_LANTERN_VISIBLE_OK saved_normal_level=true pixel_negative_control=true poses=2 os_input=false natural_movement=false whole_art=false human=false");
    }

    private static Rect2 PixelBounds(Transform2D transform, Rect2 local)
    {
        Rect2 result = new(transform * local.Position, Vector2.Zero);
        result = result.Expand(transform * new Vector2(local.End.X, local.Position.Y));
        result = result.Expand(transform * new Vector2(local.Position.X, local.End.Y));
        return result.Expand(transform * local.End).Grow(2);
    }

    private static int ChangedPixels(Image a, Image b, Rect2 bounds)
    {
        TestAssert.True(a.GetSize() == b.GetSize() && bounds.Position.X >= 0 && bounds.Position.Y >= 0 &&
            bounds.End.X < a.GetWidth() && bounds.End.Y < a.GetHeight(),
            "lamp draw rectangle is fully inside the real viewport");
        int count = 0;
        for (int y = (int)Math.Floor(bounds.Position.Y); y < (int)Math.Ceiling(bounds.End.Y); y++)
            for (int x = (int)Math.Floor(bounds.Position.X); x < (int)Math.Ceiling(bounds.End.X); x++)
            {
                Color one = a.GetPixel(x, y), two = b.GetPixel(x, y);
                if (Math.Abs(one.R - two.R) + Math.Abs(one.G - two.G) + Math.Abs(one.B - two.B) > .04f) count++;
            }
        return count;
    }

    private static async Task RenderFrames(SceneTree tree, int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }

    public static async Task RunBinding(SceneTree tree)
    {
        await AssertCleanupOwnership(tree);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn")
            .Instantiate<CharacterBody2D>();
        try
        {
            tree.Root.AddChild(player);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            var formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            var lantern = formal.GetNode<PlayerFormalLayer>("Lantern");
            AssertBound(lantern);
            TestAssert.True(lantern.Scale.DistanceTo(Vector2.One) < .0001f && lantern.ZIndex == 3,
                "accepted bitmap uses existing positive-scale layer and Z, not an extra mirror/scale");
            TestAssert.True(lantern.Position.DistanceTo(new Vector2(-14, -42)) < .0001f,
                "saved normal Player retains the existing lantern pivot");
            foreach (int facing in new[] { 1, -1 })
            {
                formal.ApplyFrame(new EntityPresentationFrame(EntityVisualState.Move, facing, true));
                AssertBound(lantern);
                TestAssert.True(formal.LanternWorldPosition.DistanceTo(lantern.GlobalPosition) < .0001f &&
                    Math.Sign(formal.Scale.X) == facing && lantern.Scale.X > 0,
                    "same bitmap and real lamp origin follow the formal root mirror exactly once");
            }
        }
        finally
        {
            player.GetParent()?.RemoveChild(player);
            player.Free();
        }
        await HeadlampFogRuntimeTests.Run(tree);
        await AssertInitialEntryReadability(tree);
        GD.Print("APPROVED_LANTERN_BINDING_OK accepted_png=true saved_player=true canvas24=true mirror=true actual_fog_origin=true pixels=false human=false");
    }

    internal static async Task ReleaseOwnedLevel(SceneTree tree, PrologueLevelController level,
        Exception primary, Func<SceneTree, Node, Task> cleanup = null)
    {
        if (!GodotObject.IsInstanceValid(level)) return;
        try
        {
            if (level.IsInsideTree())
                await (cleanup ?? TestSceneTeardown.FreeSceneAfterAudioDrain)(tree, level);
            else level.Free();
        }
        catch (Exception cleanupFailure)
        {
            var failures = new List<Exception>();
            if (primary != null) failures.Add(primary);
            failures.Add(cleanupFailure);
            void Attempt(Action action)
            {
                try { action(); }
                catch (Exception fallback) { failures.Add(fallback); }
            }
            if (GodotObject.IsInstanceValid(level))
            {
                if (level.IsInsideTree())
                {
                    Attempt(level.CancelResidentPreparation);
                    Attempt(level.QueueFree);
                    try
                    {
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    }
                    catch (Exception fallback) { failures.Add(fallback); }
                }
                else Attempt(level.Free);
                if (GodotObject.IsInstanceValid(level))
                    failures.Add(new InvalidOperationException("owned lantern fixture was not released"));
            }
            throw new AggregateException(failures);
        }
    }

    private static async Task AssertInitialEntryReadability(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        Vector2I originalSize = tree.Root.Size, originalContent = tree.Root.ContentScaleSize;
        Exception primary = null;
        try
        {
            tree.Root.Size = new Vector2I(1280, 720);
            tree.Root.ContentScaleSize = new Vector2I(1280, 720);
            IsolatedPrologueFixture.Configure(level, "lantern-initial-entry-readability");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            tree.Root.AddChild(level);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            flow.StartNew();
            await BasinAscentCameraRuntimeTests.WaitForPlaying(tree, level);
            for (int frame = 0; frame < 60 && !level.Player.IsOnFloor(); frame++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.True(level.Player.IsOnFloor(), "real initial entry must settle on its actual saved support");
            level.GetViewport().GetCamera2D()?.ResetSmoothing();
            var formal = level.Player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
            var lamp = formal.GetNode<PlayerFormalLayer>("Lantern");
            var body = formal.GetNode<Node2D>("Body");
            var guidance = level.GetNode<PrologueGuidanceController>("Guidance");
            var visual = level.GetNode<WorldTargetMarkerVisual>("Hud/Guidance/VisualRoot");
            var hud = level.GetNode<PrologueFormalHud>("Hud/FormalHud");
            var pad = level.CurrentSectorRoot.GetNode<CanyonSpringboard>("Gameplay/Mechanisms/CombinationSpringboard");
            var gear = level.CurrentSectorRoot.GetNode<CanyonFixedGear>("Gameplay/Mechanisms/CombinationFixedGear");
            var bodyCollider = level.Player.GetNode<CollisionShape2D>("BodyCollider");
            TestAssert.True(bodyCollider.Shape is CapsuleShape2D && !bodyCollider.Disabled,
                "entry readability uses the actual enabled saved player capsule");
            var capsule = (CapsuleShape2D)bodyCollider.Shape;
            int hp = level.Player.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth;
            var errors = new List<string>();
            int samples = 0;
            foreach (Vector2I resolution in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
            foreach (float uiScale in new[] { .8f, 1f, 1.5f })
            foreach (GameLanguage language in Enum.GetValues<GameLanguage>())
            {
                tree.Root.Size = resolution; tree.Root.ContentScaleSize = resolution;
                hud.SetUiScale(uiScale); hud.SetLanguage(language);
                guidance.SetLanguage(language); guidance.RefreshNow();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                level.GetViewport().GetCamera2D()?.ResetSmoothing();
                Transform2D final = level.GetViewport().GetFinalTransform();
                Rect2 head = PixelBounds(final * body.GetGlobalTransformWithCanvas(), new Rect2(-15, -34, 30, 30));
                Rect2 lantern = PixelBounds(final * lamp.GetGlobalTransformWithCanvas(), lamp.LanternDrawRect);
                Rect2 bodyBounds = PixelBounds(final * bodyCollider.GetGlobalTransformWithCanvas(),
                    new Rect2(-capsule.Radius, -capsule.Height / 2f, capsule.Radius * 2f, capsule.Height));
                Rect2 prompt = visual.GetNode<Control>("Backdrop").GetGlobalRect();
                Label text = visual.GetNode<Label>("Text");
                string expected = PrologueStrings.Get("guidance.recycling_combo", language)
                    .Replace("{jump}", "SPACE").Replace("{move_right}", "D");
                string[] required = language == GameLanguage.English
                    ? new[] { "UP-LEFT", "ALWAYS DANGEROUS", "NO SHIELD", "SPACE: JUMP OVER THE PAD",
                        "RETURN: CLEAR PAD", "D: REVERSE-THRUST BRAKE IN AIR", "LAND, THEN WALK BENEATH THE GEAR" }
                    : new[] { "向左上弹射", "齿轮持续危险", "盾无效", "SPACE：跳过弹板",
                        "返程先越过弹板", "D：空中反推制动", "齿轮下方返回" };
                foreach (string clause in required)
                    TestAssert.True(expected.Contains(clause, StringComparison.Ordinal),
                        "entry device guidance cannot remove required meaning: " + clause);
                TestAssert.True(level.GetNode<PrologueCompletionController>("Completion").CurrentObjective == PrologueObjective.LearnAttackMode &&
                    guidance.CurrentInstruction.Visible &&
                    guidance.CurrentInstruction.ContextTitle == PrologueStrings.Get("guidance.recycling_title", language) &&
                    guidance.CurrentInstruction.StepText == expected && guidance.CurrentInstruction.HintText == string.Empty &&
                    guidance.CurrentInstruction.Targets.Count == 0 && visual.RenderedStepText == expected &&
                    text.Text.Contains(expected, StringComparison.Ordinal) && pad.LaunchSequence == 0 &&
                    pad.HasLiveSensor && pad.IsVisibleInTree() && pad.CanProcess() &&
                    gear.IsDangerTelegraphActive && gear.IsVisibleInTree() && gear.CanProcess() &&
                    level.Player.GetNode<Knight.Combat.HealthComponent>("Health").CurrentHealth == hp,
                    "entry clearance observes the real higher-priority complete Combo, live devices and untouched pending objective; no hiding/relocation shortcut");
                if (!(visual.HasVisiblePrompt && visual.RenderedTextContained && visual.IsVisibleInTree() &&
                    text.GetLineCount() > 0 && !text.ClipText && text.TextOverrunBehavior == TextServer.OverrunBehavior.NoTrimming &&
                    text.GetMinimumSize().Y <= text.Size.Y + .5f &&
                    prompt.Position.DistanceTo(visual.PromptRect.Position) < .01f &&
                    prompt.Size.DistanceTo(visual.PromptRect.Size) < .01f &&
                    !prompt.Intersects(head.Grow(4)) && !prompt.Intersects(lantern.Grow(4)) &&
                    !prompt.Intersects(bodyBounds.Grow(4))))
                    errors.Add($"resolution={resolution} ui={uiScale} language={language} prompt={prompt} head={head} body={bodyBounds} lamp={lantern} lines={text.GetLineCount()}");
                samples++;
                GD.Print($"APPROVED_ENTRY_COPY_LAYOUT resolution={resolution} ui={uiScale} language={language} prompt={prompt} head={head} body={bodyBounds} lamp={lantern} lines={text.GetLineCount()}");
            }
            TestAssert.True(samples == 12 && errors.Count == 0,
                "INITIAL_DEVICE_TEACHING_OCCLUSION_RED real Combo must not obscure saved actor/head/lamp: " + string.Join("; ", errors));
        }
        catch (Exception exception) { primary = exception; throw; }
        finally
        {
            tree.Root.Size = originalSize; tree.Root.ContentScaleSize = originalContent;
            await ReleaseOwnedLevel(tree, level, primary);
        }
        GD.Print("APPROVED_ENTRY_ACTOR_CLEARANCE_OK saved_initial_support=true languages=2 head_and_lamp_clear=true geometry_only=true human=false");
        GD.Print("APPROVED_ENTRY_DEVICE_CLEARANCE_OK samples=12 source_combo=true full_copy=true live_devices=true actor_capsule_head_lamp_clear=true unchanged_pending_objective=true geometry_only=true human=false");
    }

    private static async Task AssertCleanupOwnership(SceneTree tree)
    {
        var level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        var otherOwner = new Node { Name = "UnrelatedLanternTestOwner" };
        var primary = new InvalidOperationException("injected original lamp assertion");
        var cleanup = new InvalidOperationException("injected audio-drain failure");
        Exception observed = null;
        try
        {
            IsolatedPrologueFixture.Configure(level, "lantern-cleanup-failure");
            var flow = level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            tree.Root.AddChild(otherOwner);
            tree.Root.AddChild(level);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            try
            {
                await ReleaseOwnedLevel(tree, level, primary,
                    (_, _) => Task.FromException(cleanup));
            }
            catch (Exception exception) { observed = exception; }
            TestAssert.True(observed is AggregateException aggregate && aggregate.InnerExceptions.Count == 2 &&
                ReferenceEquals(aggregate.InnerExceptions[0], primary) &&
                ReferenceEquals(aggregate.InnerExceptions[1], cleanup) &&
                !GodotObject.IsInstanceValid(level) && GodotObject.IsInstanceValid(otherOwner) && otherOwner.IsInsideTree(),
                $"failed lamp observation retains exactly original+cleanup errors, releases its own root only; observed={observed}");
        }
        finally
        {
            if (GodotObject.IsInstanceValid(level))
            {
                level.CancelResidentPreparation();
                level.QueueFree();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
            otherOwner.GetParent()?.RemoveChild(otherOwner);
            otherOwner.Free();
        }
        GD.Print("APPROVED_LANTERN_CLEANUP_OWNERSHIP_OK original_error=true cleanup_error=true owned_root_released=true unrelated_owner_retained=true");
    }

    internal static void AssertBound(PlayerFormalLayer lantern)
    {
        // Reflection lets this behavioral RED run before the exported hook exists;
        // a missing property is a runtime failed contract, not a compiler failure.
        PropertyInfo binding = typeof(PlayerFormalLayer).GetProperty("LanternTexture");
        TestAssert.True(binding != null && binding.GetCustomAttribute<ExportAttribute>() != null,
            "normal saved Lantern must expose its replaceable approved Texture2D binding");
        var texture = binding.GetValue(lantern) as Texture2D;
        TestAssert.True(texture != null && texture.ResourcePath == ApprovedPath &&
            texture.GetWidth() == 96 && texture.GetHeight() == 96,
            "Ready loads the accepted original 96x96 PNG rather than procedural circles");
        PropertyInfo rectangle = typeof(PlayerFormalLayer).GetProperty("LanternDrawRect");
        TestAssert.True(rectangle?.GetValue(lantern) is Rect2 rect &&
            rect.Position.DistanceTo(new Vector2(-12, -12)) < .0001f &&
            rect.Size.DistanceTo(new Vector2(24, 24)) < .0001f,
            "bitmap canvas is centered and bounded in world units, never stretched 96x96");
    }
}
