using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Interactions;
using Knight.Player;

namespace Knight.Tests.Integration;

public static partial class EnvironmentInteractionRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        // Real player and real physical input; fixture objects deliberately have no save paths.
        var root = new Node2D { Name = "EnvironmentInteractionFixture" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(800, 20) } });
        root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14);
        var interaction = player.GetNodeOrNull<EnvironmentInteractionController>("EnvironmentInteraction");
        bool sceneContainsController = interaction != null;
        if (interaction == null)
        {
            interaction = new EnvironmentInteractionController { Name = "EnvironmentInteraction", ProcessPhysicsPriority = -50 };
            player.AddChild(interaction);
        }
        root.AddChild(player);
        var second = new InteractionFixtureObject { Name = "Second", StableId = "b", Position = new Vector2(40, 14) };
        var first = new InteractionFixtureObject { Name = "First", StableId = "a", Position = new Vector2(40, 14) };
        root.AddChild(second);
        root.AddChild(first);
        // SceneTree groups span viewports. A nearby object in a different physics
        // world must never mask the visible object in the player's world.
        var otherWorld = new SubViewport
        {
            Name = "OtherInteractionWorld", World2D = new World2D(), Size = new Vector2I(128, 128)
        };
        var otherWorldObject = new InteractionFixtureObject
        {
            Name = "OtherWorldObject", StableId = "00-other-world", Position = new Vector2(0, 14)
        };
        otherWorld.AddChild(otherWorldObject);
        root.AddChild(otherWorld);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var energy = player.GetNode<PlayerEnergyController>("Energy");
        var modes = player.GetNode<PlayerActionModeController>("ActionModes");
        var health = player.GetNode<HealthComponent>("Health");
        try
        {
            await KeyboardActionRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "interaction fixture stands on real support");
            TestAssert.True(ReferenceEquals(first, interaction.CurrentCandidate),
                "Q preview filters other World2D groups before nearest visible stable ID selection");
            first.Hide();
            TestAssert.True(interaction.CurrentCandidate == null,
                "a candidate hidden after physics selection immediately withdraws its Q prompt");
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            TestAssert.True(ReferenceEquals(second, interaction.CurrentCandidate),
                "a hidden nearer candidate cannot mask the one visible interaction");
            first.Show();
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            first.RemoveFromGroup(EnvironmentInteractionController.InteractableGroup);
            TestAssert.True(interaction.CurrentCandidate == null,
                "removing explicit opt-in immediately withdraws the cached Q candidate");
            first.AddToGroup(EnvironmentInteractionController.InteractableGroup);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            first.Position = new Vector2(160, 14);
            TestAssert.True(interaction.CurrentCandidate == null,
                "moving the same selected object beyond80 immediately withdraws its Q prompt");
            first.Position = new Vector2(40, 14);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.D, true);
            KeyboardActionRuntimeTests.KeyEvent(Key.Q, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            TestAssert.True(first.Interactions == 0 && interaction.CurrentCandidate == null,
                "moving D plus Q cannot operate or advertise a standing-only interaction");
            KeyboardActionRuntimeTests.KeyEvent(Key.D, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 30);
            TestAssert.Equal(0, first.Interactions, "Q held while stopping never replays the rejected moving edge");
            KeyboardActionRuntimeTests.KeyEvent(Key.Q, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            float energyBefore = energy.EnergyRatio;
            int slotBefore = modes.ActiveSlot;
            await Press(tree, input, Key.Q, 20);
            TestAssert.Equal(1, first.Interactions, "PressExecutesExactlyOnce while Q stays held");
            TestAssert.Equal(0, second.Interactions, "one Q press never operates a second candidate");
            TestAssert.Equal(0, otherWorldObject.Interactions, "Q never operates an object in another physics world");
            TestAssert.Near(energyBefore, energy.EnergyRatio, .0001f, "Q does not spend attack, claw or inventory energy");
            TestAssert.Equal(slotBefore, modes.ActiveSlot, "Q does not consume or switch equipment");

            // Real level lifecycle disables the Input node without freeing its
            // last sample. A live but stopped adapter cannot authorize a prompt.
            input.SetPhysicsProcess(false);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(interaction.CurrentCandidate == null,
                "a stopped real Input withdraws Q's cached candidate immediately");
            input.SetPhysicsProcess(true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            TestAssert.True(ReferenceEquals(first, interaction.CurrentCandidate),
                "a fresh live input sample restores the one actual nearby candidate");

            player.RemoveChild(interaction);
            TestAssert.True(interaction.CurrentCandidate == null,
                "a detached interaction controller is safely queryable and exposes no retired candidate");
            player.AddChild(interaction);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            TestAssert.True(ReferenceEquals(first, interaction.CurrentCandidate),
                "the same interaction controller re-enters with fresh live dependencies");

            await Press(tree, input, Key.Q, 3);
            TestAssert.Equal(2, first.Interactions, "a second fresh Q edge executes once again");

            // The standing flag from the previous frame cannot let jump + Q interact.
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, true);
            KeyboardActionRuntimeTests.KeyEvent(Key.Q, true);
            await KeyboardActionRuntimeTests.Frames(tree, input, 4);
            TestAssert.Equal(2, first.Interactions, "Space+Q rejects on the departure frame");
            KeyboardActionRuntimeTests.KeyEvent(Key.Space, false);
            KeyboardActionRuntimeTests.KeyEvent(Key.Q, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            await Press(tree, input, Key.Q, 3);
            TestAssert.Equal(2, first.Interactions, "airborne Q does not execute");
            for (int tick = 0; tick < 100 && !player.IsOnFloor(); tick++)
                await KeyboardActionRuntimeTests.Frames(tree, input, 1);
            TestAssert.True(player.IsOnFloor(), "jump lands normally without relocating the player");

            var blocker = new StaticBody2D { Position = new Vector2(30, 10), CollisionLayer = CombatCollisionLayers.World };
            blocker.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(4, 70) } });
            root.AddChild(blocker);
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            TestAssert.True(interaction.CurrentCandidate == null, "solid world wall hides interaction preview");
            await Press(tree, input, Key.Q, 3);
            TestAssert.Equal(2, first.Interactions, "Q cannot reach through a wall");
            blocker.QueueFree();
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);

            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            tree.Paused = true;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyboardActionRuntimeTests.KeyEvent(Key.Q, true);
            tree.Paused = false;
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            KeyboardActionRuntimeTests.KeyEvent(Key.Q, false);
            await KeyboardActionRuntimeTests.Frames(tree, input, 2);
            TestAssert.Equal(2, first.Interactions, "paused Q held through resume never replays");

            foreach (Key competing in new[] { Key.J, Key.K, Key.L })
            {
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                KeyboardActionRuntimeTests.KeyEvent(competing, true);
                KeyboardActionRuntimeTests.KeyEvent(Key.Q, true);
                await KeyboardActionRuntimeTests.Frames(tree, input, 3);
                KeyboardActionRuntimeTests.KeyEvent(competing, false);
                KeyboardActionRuntimeTests.KeyEvent(Key.Q, false);
                await KeyboardActionRuntimeTests.Frames(tree, input, 30);
            }
            TestAssert.Equal(2, first.Interactions, "J, K and L each consume a simultaneous Q edge without deferred replay");

            // Keep K's actual cancellation/recovery separate from environmental interaction.
            await KeyboardActionRuntimeTests.Frames(tree, input, 10);
            first.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(!ReferenceEquals(first, interaction.CurrentCandidate), "queued-for-deletion candidate is never exposed");
            await Press(tree, input, Key.Q, 3);
            TestAssert.Equal(1, second.Interactions, "unloaded candidate falls back to the one visible live object");
            second.Available = false;
            await Press(tree, input, Key.Q, 3);
            TestAssert.Equal(1, second.Interactions, "unavailable object cannot execute");
            second.Available = true;
            health.ApplyDamage(new DamageRequest(99, Vector2.Zero, 0f, 987654UL));
            await Press(tree, input, Key.Q, 3);
            TestAssert.Equal(1, second.Interactions, "dead player cannot interact");
            health.ResetFull();
            input.ClearSampledFrame();
            await KeyboardActionRuntimeTests.Frames(tree, input, 3);
            await Press(tree, input, Key.Q, 3);
            TestAssert.Equal(2, second.Interactions, "fresh interaction works after health reset and fresh Q");
            TestAssert.True(sceneContainsController, "generated Player scene contains the interaction controller");
            GD.Print("ENVIRONMENT_INTERACTION_RUNTIME_OK");
        }
        finally
        {
            foreach (Key key in new[] { Key.D, Key.Q, Key.Space, Key.J, Key.K, Key.L })
                KeyboardActionRuntimeTests.KeyEvent(key, false);
            tree.Paused = false;
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    private static async Task Press(SceneTree tree, PlayerInputReader input, Key key, int heldFrames)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        KeyboardActionRuntimeTests.KeyEvent(key, true);
        await KeyboardActionRuntimeTests.Frames(tree, input, heldFrames);
        KeyboardActionRuntimeTests.KeyEvent(key, false);
        await KeyboardActionRuntimeTests.Frames(tree, input, 2);
    }

    private sealed partial class InteractionFixtureObject : Node2D, IEnvironmentInteractable
    {
        public string StableId { get; set; }
        public Node2D InteractionNode => this;
        public bool Available { get; set; } = true;
        public int Interactions { get; private set; }
        public override void _Ready() => AddToGroup(EnvironmentInteractionController.InteractableGroup);
        public bool CanInteract(CharacterBody2D actor) => Available;
        public bool TryInteract(CharacterBody2D actor)
        {
            if (!Available) return false;
            Interactions++;
            return true;
        }
    }
}
