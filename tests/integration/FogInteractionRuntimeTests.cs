using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Interactions;
using Knight.Mechanisms;
using Knight.Player;
using Knight.Presentation.Fog;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Settings;

namespace Knight.Tests.Integration;

// Kills missing/below-fog text, duplicated world text, stale selection, a second
// occlusion capture, and retained glyphs after pause, source retirement or Q.
// Controlled saved-sector placement is NOT a title-to-exit route or human proof.
public static class FogInteractionRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await using var f = await Fixture.Create(tree);
        // HasChar only checks the base font, whereas shaping can use a native
        // system fallback. Verify the fonts actually selected for these words.
        using (var line = new TextLine())
        {
            line.AddString("Q 关闭蒸汽阀", ThemeDB.FallbackFont, 16);
            line.GetLineWidth();
            var glyphs = TextServerManager.GetPrimaryInterface().ShapedTextGetGlyphs(line.GetRid());
            int realCharacters = 0;
            foreach (var glyph in glyphs)
            {
                GD.Print($"FOG_Q_SHAPED_GLYPH {glyph}");
                var flags = (TextServer.GraphemeFlag)glyph["flags"].AsInt64();
                if (flags.HasFlag(TextServer.GraphemeFlag.Virtual)) continue;
                TestAssert.True(flags.HasFlag(TextServer.GraphemeFlag.Valid) &&
                    glyph["font_rid"].AsRid().IsValid && glyph["index"].AsInt64() > 0,
                    "actual text shaping selects a real glyph font, not a missing-character box");
                realCharacters++;
            }
            TestAssert.Equal(7, realCharacters, "seven actual Q/action characters including the space are shaped");
        }
        foreach (var sample in new[] {
            (GameLanguage.English, "Q Close steam valve"),
            (GameLanguage.ChineseSimplified, "Q 关闭蒸汽阀") })
        {
            f.Level.SetLanguage(sample.Item1);
            TestAssert.Equal(sample.Item2, f.Interaction.CurrentPromptText, "same actual candidate supplies the localized action");
            using Image actual = await f.Pixels();
            f.Overlay.Hide();
            using Image blank = await f.Pixels();
            f.ReferenceText = sample.Item2;
            using Image reference = await f.Pixels();
            CompareGlyphs(actual, reference, blank, "protected glyphs equal an independently positioned literal reference");
            f.ReferenceText = null;
            f.Overlay.Show();
            f.ProtectedLayer.Layer = 0;
            using Image below = await f.Pixels();
            TestAssert.True(BrightPixels(actual) > 60 && BrightPixels(below) == 0,
                "same real text below fog is genuinely dimmed; the old world prompt cannot duplicate protected glyphs");
            f.ProtectedLayer.Layer = 2;
        }
        GD.Print("FOG_Q_LOCALIZED_GLYPHS_AND_LAYER_NEGATIVE_OK");

        StaticBody2D wall = f.AddWall(new Vector2(3160, 4060), new Vector2(40, 8));
        using (Image blocked = await f.Pixels())
            TestAssert.True(f.Interaction.CurrentCandidate == null && BrightPixels(blocked) == 0,
                "real native wall blocks the actual candidate and removes every protected glyph");
        wall.Free();
        using (Image restored = await f.Pixels())
            TestAssert.True(BrightPixels(restored) > 60, "removing the object wall restores the same genuine candidate");

        f.Level.GetNode<ProloguePauseMenu>("PauseMenu").Open();
        using (Image paused = await f.Pixels())
            TestAssert.True(tree.Paused && f.Interaction.CurrentCandidate == null && BrightPixels(paused) == 0,
                "the real pause path leaves no Q glyphs above fog");
        f.Level.GetNode<ProloguePauseMenu>("PauseMenu").Execute(ProloguePauseCommand.Resume);
        await f.Frames(3);
        using (Image resumed = await f.Pixels())
            TestAssert.True(BrightPixels(resumed) > 60, "the original live source returns after real resume");

        Node parent = f.Target.GetParent();
        parent.RemoveChild(f.Target);
        using (Image detached = await f.Pixels())
            TestAssert.True(BrightPixels(detached) == 0, "instance-valid detached target leaves no cached prompt");
        parent.AddChild(f.Target);
        await f.Frames(3);
        using (Image reentered = await f.Pixels())
            TestAssert.True(BrightPixels(reentered) > 60, "same target reentry restores its live opt-in source");

        Node sourceParent = f.Interaction.GetParent();
        sourceParent.RemoveChild(f.Interaction);
        using (Image detachedSource = await f.Pixels())
            TestAssert.True(BrightPixels(detachedSource) == 0, "detached original Q controller cannot leave protected glyphs");
        sourceParent.AddChild(f.Interaction);
        await f.Frames(3);
        using (Image sourceReturned = await f.Pixels())
            TestAssert.True(BrightPixels(sourceReturned) > 60, "reentered original Q controller reacquires only one renderer");
        await f.CheckRendererReentry();

        KeyboardActionRuntimeTests.KeyEvent(Key.A, true);
        await f.Frames(26);
        KeyboardActionRuntimeTests.KeyEvent(Key.A, false);
        await f.Frames(5);
        using (Image outside = await f.Pixels())
            TestAssert.True(f.Player.GlobalPosition.DistanceTo(f.Target.GlobalPosition) > 80 &&
                f.Interaction.CurrentCandidate == null && BrightPixels(outside) == 0,
                "normal walking outside the nearby radius removes the original target's text");
        KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
        for (int n = 0; n < 80 && f.Player.GlobalPosition.X < 3150; n++) await f.Frames(1);
        KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
        await f.Frames(8);
        using (Image returned = await f.Pixels())
            TestAssert.True(BrightPixels(returned) > 60, "normal physical return restores the same original candidate");
        KeyboardActionRuntimeTests.KeyEvent(Key.Q, true);
        await f.Frames(3);
        KeyboardActionRuntimeTests.KeyEvent(Key.Q, false);
        using (Image consumed = await f.Pixels())
            TestAssert.True(f.Valve.IsActivated && f.Jet.DisabledForRun &&
                f.Interaction.CurrentCandidate == null && BrightPixels(consumed) == 0,
                "actual Q consumes the original valve and removes its glyphs on the next raster");
        GD.Print("FOG_Q_NATIVE_LIFECYCLE_AND_CONSUMPTION_OK");
        await f.CheckDeathRetiresPrompt();
    }

    private static int BrightPixels(Image image)
    {
        int count = 0;
        for (int y = 177; y < 205; y++)
            for (int x = 150; x < 362; x++)
                if (image.GetPixel(x, y).G > .8f) count++;
        return count;
    }

    private static void CompareGlyphs(Image actual, Image reference, Image blank, string message)
    {
        int glyphs = 0;
        for (int y = 177; y < 205; y++)
            for (int x = 150; x < 362; x++)
            {
                Color a = actual.GetPixel(x, y), r = reference.GetPixel(x, y), b = blank.GetPixel(x, y);
                TestAssert.True(Mathf.Abs(b.R-.09f) < .008f && Mathf.Abs(b.G-.09f) < .008f && Mathf.Abs(b.B-.09f) < .008f,
                    $"exclusive renderer leaves no dim duplicated world glyph at {x},{y}: {b}");
                TestAssert.True(Mathf.Abs(a.R-r.R) < .012f && Mathf.Abs(a.G-r.G) < .012f && Mathf.Abs(a.B-r.B) < .012f,
                    $"{message}: {x},{y} actual={a} literal={r}");
                if (r.G - b.G > .4f) glyphs++;
            }
        TestAssert.True(glyphs > 60, "actual action words produce many readable glyph pixels, not just a circle or a property");
        Color area = actual.GetPixel(368, 200), empty = blank.GetPixel(368, 200);
        TestAssert.True(Mathf.Abs(area.G-empty.G) < .008f, "protecting text does not reveal the adjacent area");
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SceneTree _tree;
        private readonly SubViewport _viewport;
        private Camera2D _camera;
        private Node2D _world, _reference;
        private PrologueFogCanvas _fog;
        private PrologueFogController _controller;
        private HeadlampOcclusionFrame _previous;
        private Exception _failure;
        private bool _subscribed;
        public PrologueLevelController Level;
        public CharacterBody2D Player;
        public EnvironmentInteractionController Interaction;
        public ThreadTarget2D Target;
        public CanyonSteamValve Valve;
        public CanyonSteamJet Jet;
        public CanvasLayer ProtectedLayer;
        public Node2D Overlay;
        public string ReferenceText;
        private Fixture(SceneTree tree)
        {
            _tree = tree;
            _viewport = new SubViewport { Size = new Vector2I(512,384), World2D = new World2D(),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        }
        public static async Task<Fixture> Create(SceneTree tree)
        {
            var f = new Fixture(tree);
            try { await f.Initialize(); return f; }
            catch { await f.DisposeAsync(); throw; }
        }
        private async Task Initialize()
        {
            var background = new CanvasLayer { Layer = -10 };
            background.AddChild(new ColorRect { Size = new Vector2(512,384), Color = new Color(.2f,.2f,.2f) });
            _viewport.AddChild(background);
            Level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn").Instantiate<PrologueLevelController>();
            IsolatedPrologueFixture.Configure(Level, "fog-real-q-glyphs");
            var flow = Level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.HeadlessAutoStart = false; flow.AutomationAutoStart = false;
            _viewport.AddChild(Level); _tree.Root.AddChild(_viewport);
            await Frames(5); flow.StartNew(); await Frames(8);
            Player = Level.Player;
            var world = Level.GetNode<SeamlessPrologueWorld>(Level.SeamlessWorldPath);
            TestAssert.True(world.RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(3160,4080)),
                "controlled saved-sector fixture restores the original supported walkway");
            await Frames(12);
            Interaction = Player.GetNode<EnvironmentInteractionController>("EnvironmentInteraction");
            Target = Level.CurrentSectorRoot.GetNode<ThreadTarget2D>("Gameplay/GrappleSurfaces/SteamValveControl");
            Valve = Level.CurrentSectorRoot.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve");
            Jet = Level.CurrentSectorRoot.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
            ProtectedLayer = Level.GetNode<CanvasLayer>("ProtectedCues");
            Overlay = ProtectedLayer.GetNodeOrNull<Node2D>("Interaction");
            TestAssert.True(Overlay != null && Overlay.GetType().FullName == "Knight.Presentation.Fog.FogInteractionReadabilityOverlay",
                "saved parent contains its dedicated real Q prompt renderer above fog");
            TestAssert.True(Player.IsOnFloor() && Interaction.CurrentCandidate?.InteractionNode == Target,
                "original living stationary saved player selects its actual opted-in valve");
            _world = Level.GetNode<Node2D>("WorldRoot");
            foreach (Node sector in _world.GetNode("Sectors").GetChildren())
                sector.GetNodeOrNull<CanvasItem>("VisualRoot")?.Hide();
            HideOtherLayers(Level);
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            _camera = new Camera2D { Position = new Vector2(3160,3992), PositionSmoothingEnabled = false };
            _viewport.AddChild(_camera); _camera.MakeCurrent(); _camera.ForceUpdateScroll();
            _controller = Level.GetNode<PrologueFogController>("FogController");
            AddWall(new Vector2(3160,4025), new Vector2(240,8));
            var layer = new CanvasLayer { Layer = 1 };
            _fog = new PrologueFogCanvas { Size = new Vector2(512,384) };
            layer.AddChild(_fog); _viewport.AddChild(layer);
            var referenceLayer = new CanvasLayer { Layer = 2 };
            _reference = new Node2D(); referenceLayer.AddChild(_reference); _viewport.AddChild(referenceLayer);
            RenderingServer.FramePreDraw += Present; _subscribed = true;
        }
        private void HideOtherLayers(Node node)
        {
            if (node is CanvasLayer layer && layer != ProtectedLayer) layer.Visible = false;
            foreach (Node child in node.GetChildren()) HideOtherLayers(child);
        }
        public StaticBody2D AddWall(Vector2 center, Vector2 size)
        {
            var body = new StaticBody2D { Position = center, CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
            body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = size } });
            _world.AddChild(body); return body;
        }
        private void Present()
        {
            try
            {
                HideOtherLayers(Level);
                TestAssert.True(!Level.GetNode<Control>("Fog/Canvas").IsVisibleInTree(),
                    "saved formal fog is effectively hidden; independent Q pixel fixture cannot double-apply fog");
                _camera.MakeCurrent(); _camera.ForceUpdateScroll();
                Transform2D canvas = _world.GetCanvasTransform();
                TestAssert.True((canvas * new Vector2(3160,4000)).DistanceTo(new Vector2(256,200)) < .01f,
                    "actual camera maps the independently specified Q text baseline to256,200");
                if (_controller.IsInsideTree() && !_tree.Paused &&
                    Level.GetNode<PrologueGameFlowController>("GameFlow").State == PrologueGameFlowState.Playing)
                {
                    var frame = _controller.LastOcclusionFrame;
                    TestAssert.True(frame is { IsComplete:true } && !ReferenceEquals(frame, _previous),
                        "test fog consumes the saved controller's single fresh snapshot, never a separate capture");
                    _previous = frame;
                    TestAssert.True(!frame.CanSee(Player.GlobalPosition, new Vector2(3160,4000)),
                        "real canopy occludes the label offset, making the layer negative-control meaningful");
                    _fog.Present(Player.GlobalPosition, _controller.LampWorldPosition, Vector2.Left,
                        (_viewport.GetFinalTransform() * canvas).AffineInverse(), frame);
                    if (Interaction.CurrentCandidate is { } current)
                        TestAssert.True(frame.CanSee(Player.GlobalPosition, current.InteractionNode.GlobalPosition),
                            "the same native snapshot still sees the actual eligible object");
                    var property = Overlay.GetType().GetProperty("LastOcclusionFrame");
                    TestAssert.True(property != null && (!Overlay.IsInsideTree() || Interaction.CurrentCandidate == null ||
                        ReferenceEquals(frame, property.GetValue(Overlay))),
                        "saved Q renderer uses the identical authoritative current fog snapshot");
                }
                RenderingServer.CanvasItemClear(_reference.GetCanvasItem());
                if (ReferenceText != null)
                    ThemeDB.FallbackFont.DrawString(_reference.GetCanvasItem(), new Vector2(156,200), ReferenceText,
                        HorizontalAlignment.Center, 200, 16, new Color(.9f,1f,.93f));
            }
            catch (Exception error) { _failure ??= error; }
        }
        public async Task Frames(int count)
        {
            for (int n = 0; n < count; n++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.PhysicsFrame);
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        public async Task CheckRendererReentry()
        {
            Node parent = Overlay.GetParent();
            parent.RemoveChild(Overlay);
            using (Image absent = await Pixels())
                TestAssert.True(BrightPixels(absent) == 0, "unloaded overlay clears all old above-fog glyphs");
            parent.AddChild(Overlay);
            using (Image returned = await Pixels())
                TestAssert.True(BrightPixels(returned) > 60, "same overlay reentry restores its single live prompt");
            parent = _controller.GetParent();
            parent.RemoveChild(_controller);
            TestAssert.True((int)Overlay.GetType().GetProperty("PresentedCueCount").GetValue(Overlay) == 0,
                "fog controller exit immediately clears its still-mounted sibling Q canvas");
            using (Image absent = await Pixels())
                TestAssert.True(BrightPixels(absent) == 0, "unloaded fog controller leaves no stale protected text");
            parent.AddChild(_controller);
            // Reentry resubscribes the production handler; this observation must
            // remain after it, rather than reading a previous preDraw snapshot.
            RenderingServer.FramePreDraw -= Present;
            RenderingServer.FramePreDraw += Present;
            using (Image returned = await Pixels())
                TestAssert.True(BrightPixels(returned) > 60, "same fog controller reentry reacquires a fresh current prompt");
            GD.Print("FOG_Q_SOURCE_AND_RENDERER_REENTRY_OK");
        }
        public async Task CheckDeathRetiresPrompt()
        {
            var flow = Level.GetNode<PrologueGameFlowController>("GameFlow");
            flow.ReturnToTitle(); flow.StartNew(); await Frames(8);
            TestAssert.True(Level.GetNode<SeamlessPrologueWorld>(Level.SeamlessWorldPath)
                .RestoreCheckpoint(PrologueSectorId.CanyonBasin, new Vector2(3160,4080)),
                "fresh isolated new session restores the controlled Q diagnostic position");
            await Frames(12);
            Target = Level.CurrentSectorRoot.GetNode<ThreadTarget2D>("Gameplay/GrappleSurfaces/SteamValveControl");
            Valve = Level.CurrentSectorRoot.GetNode<CanyonSteamValve>("Gameplay/Mechanisms/SteamValve");
            Jet = Level.CurrentSectorRoot.GetNode<CanyonSteamJet>("Gameplay/Mechanisms/SteamJet");
            using (Image before = await Pixels())
                TestAssert.True(!Valve.IsActivated && BrightPixels(before) > 60,
                    "real new-game reset restores an actual visible prompt before lethal damage");
            HealthComponent health = Player.GetNode<HealthComponent>("Health");
            bool sawDeath = false, withdrawnAtDeath = false;
            void OnDeath() { sawDeath = true; withdrawnAtDeath = Interaction.CurrentCandidate == null; }
            health.Died += OnDeath;
            try
            {
                TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero, 0, 77101)),
                    "real damage pipeline accepts this first lethal hit");
                TestAssert.True(sawDeath && withdrawnAtDeath, "real death immediately withdraws the authoritative candidate");
                // Capture the first new raster, not an arbitrary later frame
                // after the real recovery has already reset health and position.
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (_failure != null) ExceptionDispatchInfo.Capture(_failure).Throw();
                using Image after = _viewport.GetTexture().GetImage();
                TestAssert.True(BrightPixels(after) == 0,
                    "first rendered frame after actual death leaves no previous Q glyphs");
            }
            finally { health.Died -= OnDeath; }
            GD.Print("FOG_Q_REAL_DEATH_FIRST_RASTER_OK");
        }
        public async Task<Image> Pixels()
        {
            for (int n = 0; n < 2; n++)
            {
                await _tree.ToSignal(_tree, SceneTree.SignalName.ProcessFrame);
                await _tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                if (_failure != null) ExceptionDispatchInfo.Capture(_failure).Throw();
            }
            return _viewport.GetTexture().GetImage();
        }
        public async ValueTask DisposeAsync()
        {
            foreach (Key key in new[] {Key.A,Key.D,Key.Q}) KeyboardActionRuntimeTests.KeyEvent(key,false);
            _tree.Paused = false;
            if (_subscribed) RenderingServer.FramePreDraw -= Present;
            if (GodotObject.IsInstanceValid(Level) && Level.IsInsideTree()) await TestSceneTeardown.DrainAudioOwner(_tree, Level);
            if (GodotObject.IsInstanceValid(Overlay) && Overlay.GetParent() == null) Overlay.Free();
            if (GodotObject.IsInstanceValid(Interaction) && Interaction.GetParent() == null) Interaction.Free();
            if (GodotObject.IsInstanceValid(_controller) && _controller.GetParent() == null) _controller.Free();
            if (GodotObject.IsInstanceValid(Target) && Target.GetParent() == null) Target.Free();
            if (GodotObject.IsInstanceValid(_viewport)) { _viewport.GetParent()?.RemoveChild(_viewport); _viewport.Free(); }
        }
    }
}
