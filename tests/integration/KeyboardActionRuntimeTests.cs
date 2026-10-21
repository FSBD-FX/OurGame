using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Abilities;

namespace Knight.Tests.Integration;

public static class KeyboardActionRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        var root = new Node2D { Name = "KeyboardActionFixture" };
        var floor = new StaticBody2D { Position = new Vector2(0, 60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(800, 20) } });
        root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0, 14);
        root.AddChild(player);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var combat = player.GetNode<PlayerCombat>("Combat");
        var shield = player.GetNode<PlayerShieldController>("Shield");
        int attacks = 0;
        combat.AttackStarted += (_, _) => attacks++;
        try
        {
            await Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor(), "keyboard fixture rests on real support");
            KeyEvent(Key.J, true);
            await Frames(tree, input, 3);
            TestAssert.Equal(1, attacks, "real physical J attacks without Q/E or a pointer event");
            KeyEvent(Key.J, false);
            await Frames(tree, input, 30);
            KeyEvent(Key.L, true);
            await Frames(tree, input, 5);
            TestAssert.True(shield.IsActive, "real physical L raises the energy shield without mouse");
            KeyEvent(Key.L, false);
            await Frames(tree, input, 4);
            TestAssert.True(!shield.IsActive, "releasing L closes the held shield");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.J, true); KeyEvent(Key.K, true); KeyEvent(Key.Q, true);
            await Frames(tree, input, 3);
            TestAssert.Equal(2, attacks, "J+K+Q produces exactly one attack");
            TestAssert.Equal(MechanicalClawState.Idle,
                player.GetNode<MechanicalClawController>("MechanicalClaw").CurrentFrame.State,
                "accepted J consumes competing K rather than casting or queueing a claw");
            KeyEvent(Key.J, false); KeyEvent(Key.K, false); KeyEvent(Key.Q, false);
            await Frames(tree, input, 30);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            tree.Paused = true;
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.J, true); // deliberately resume before this edge naturally expires
            tree.Paused = false;
            await Frames(tree, input, 3);
            TestAssert.Equal(2, attacks, "a paused J edge held through resume never replays");
            KeyEvent(Key.J, false); await Frames(tree, input, 2);
            KeyEvent(Key.J, true); await Frames(tree, input, 3);
            TestAssert.Equal(3, attacks, "fresh J after pause is accepted once");
            KeyEvent(Key.J, false); await Frames(tree, input, 30);
            var motor = player.GetNode<PlayerMotor>("Motor");
            TestAssert.True(player.IsOnFloor(), "ordinary guard-jump begins on real support");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            KeyEvent(Key.L, true); KeyEvent(Key.Space, true);
            await Frames(tree, input, 4);
            TestAssert.True(shield.IsActive && !player.IsOnFloor() && motor.AcceptedJumpCount == 1,
                "real L+Space preserves one ordinary grounded jump while guarding");
            KeyEvent(Key.Space, false); await Frames(tree, input, 2);
            KeyEvent(Key.Space, true); await Frames(tree, input, 3);
            TestAssert.Equal(1, motor.AcceptedJumpCount, "guarded airborne Space does not create a second jump");
            KeyEvent(Key.Space, false); KeyEvent(Key.L, false);
            await Frames(tree, input, 100);
            var energy = player.GetNode<PlayerEnergyController>("Energy");
            // Prior attacks spend real energy. Let normal regeneration finish so this
            // test isolates overheat, rather than legitimately unaffordable attacks.
            for (int tick=0; tick<300 && energy.EnergyRatio < .999f; tick++) await Frames(tree,input,1);
            KeyEvent(Key.L, true);
            for (int tick=0; tick<300 && !energy.IsOverheated; tick++) await Frames(tree,input,1);
            TestAssert.True(energy.IsOverheated && !shield.IsActive && input.Current.ShieldHeld,
                "sustained real L overheats and closes guard naturally");
            var modes = player.GetNode<PlayerActionModeController>("ActionModes");
            TestAssert.True(modes.CanAffordAttack, "natural overheat case has a real affordable attack budget");
            GD.Print($"KEYBOARD_OVERHEAT_BEFORE energy={energy.EnergyRatio * energy.Tuning.MaxEnergy} " +
                $"canGuard={shield.CanBeginGuard} canAttack={modes.CanBeginAttack} sample={input.CurrentFrameSequence}");
            KeyEvent(Key.J, true);
            for (int tick=0; tick<3; tick++)
            {
                await Frames(tree, input, 1);
                GD.Print($"KEYBOARD_OVERHEAT_SAMPLE attack={input.Current.AttackPressed} shield={input.Current.ShieldHeld} " +
                    $"source={input.Current.KeyboardActions} energy={energy.EnergyRatio * energy.Tuning.MaxEnergy} " +
                    $"reason={modes.LastRejection} count={attacks} sample={input.CurrentFrameSequence}");
            }
            TestAssert.Equal(4, attacks, "fresh J can attack after guard overheat while L stays held");
            GD.Print("KEYBOARD_ACTION_RUNTIME_OK");
        }
        finally
        {
            KeyEvent(Key.J, false); KeyEvent(Key.K, false); KeyEvent(Key.L, false); KeyEvent(Key.Q, false);
            KeyEvent(Key.Space, false);
            tree.Paused = false;
            root.QueueFree();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
        }
    }

    public static void KeyEvent(Key key, bool pressed)
    {
        Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = key, Keycode = key, Pressed = pressed });
        Input.FlushBufferedEvents();
    }

    public static async Task Frames(SceneTree tree, PlayerInputReader input, int count)
    {
        for (int tick = 0; tick < count; tick++)
        {
            ulong before = input.CurrentFrameSequence;
            for (int wait = 0; wait < 8 && before == input.CurrentFrameSequence; wait++)
                await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            TestAssert.True(input.CurrentFrameSequence > before, "real input sampling advances");
        }
    }
}
