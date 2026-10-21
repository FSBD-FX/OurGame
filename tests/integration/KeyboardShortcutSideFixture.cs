using System;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonTraversalTests
{
    // Deliberately local: the initial pose is a fixture, not a Title/return-route proof.
    public static async Task RunKeyboardShortcutSideFixture(SceneTree tree, int selectedStep = 0)
    {
        int tested = 0;
        foreach (int y in new[] { 3796, 3636, 2996, 2676, 2036, 1716 })
        {
            if (selectedStep != 0 && y != selectedStep) continue;
            var root = new Node2D { Name = $"KeyboardSideFixture{y}" };
            var basin = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonBasin.tscn")
                .Instantiate<PrologueSector>();
            var ascent = GD.Load<PackedScene>("res://scenes/prologue/sectors/CanyonAscent.tscn")
                .Instantiate<PrologueSector>();
            basin.Position = new(1280, 3600);
            ascent.Position = new(3840, 720);
            root.AddChild(basin);
            root.AddChild(ascent);
            // Local construction only: the full route has already opened this
            // same winch through K before its return ascent. A closed fixture
            // barrier overlaps/ejects the authored Ascent source platforms.
            var progress = new PrologueProgress();
            var winch = ascent.GetNode<CanyonShortcutWinch>("Gameplay/Mechanisms/UpperBasinShortcutWinch");
            progress.OpenShortcut(winch.ShortcutId.ToString());
            winch.ApplyProgress(progress);
            PrologueSector sector = y >= 3636 ? basin : ascent;
            var support = sector.GetNode<StaticBody2D>($"Gameplay/Collision/ShortcutStep{y}");
            var source = sector.GetNode<StaticBody2D>($"Gameplay/Collision/ShortcutStep{y + 160}");
            var sourceShape = (RectangleShape2D)source.GetNode<CollisionShape2D>("Collision").Shape;
            CharacterBody2D player = NewPlayer();
            player.GetNode<Camera2D>("Camera2D").Enabled = true;
            var capsule = (CapsuleShape2D)player.GetNode<CollisionShape2D>("BodyCollider").Shape;
            player.Position = source.GlobalPosition - new Vector2(0, (sourceShape.Size.Y + capsule.Height) * .5f + .1f);
            if (y == 3796) player.Position = new(4162.065f, player.Position.Y);
            if (y is 2996 or 2036) player.Position = new(3927.558f, player.Position.Y);
            root.AddChild(player);
            tree.Root.AddChild(root);
            using var keyboard = KeyboardRouteInputDriver.Begin(player);
            try
            {
                TestAssert.True(winch.IsOpen && winch.BarrierCollision.Disabled, "local fixture explicitly seeds the already-open shortcut, not a full-route proof");
                GD.Print($"KEYBOARD_SHORTCUT_SIDE_LOCAL_SETUP y={y} player={player.GlobalPosition} source={source.GlobalPosition} target={support.GlobalPosition} seeded_shortcut=true fullRoute=false");
                await WaitForFloor(tree, player);
                string contacts = string.Empty;
                for (int contact = 0; contact < player.GetSlideCollisionCount(); contact++)
                {
                    using var hit = player.GetSlideCollision(contact);
                    contacts += $"{(hit.GetCollider() as Node)?.GetPath()}:{hit.GetNormal()};";
                }
                TestAssert.True(Task4HasFloorContact(player, source),
                    $"local fixture starts on the actual original previous step; y={y} position={player.GlobalPosition} source={source.GlobalPosition} contacts={contacts}");
                int hp = player.GetNode<HealthComponent>("Health").CurrentHealth;
                BeginTask10InputDrive(tree, player, () =>
                    TestAssert.Equal(hp, player.GetNode<HealthComponent>("Health").CurrentHealth,
                        "local side/Boost fixture cannot hide damage or death"));
                await Task4ReachSupport(tree, player, support, true, keyboardShortcutSideBoost: true);
                tested++;
                GD.Print($"KEYBOARD_SHORTCUT_SIDE_LOCAL_STEP_OK y={y} fixture=true fullRoute=false");
            }
            finally
            {
                EndTask10InputDrive();
                await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, root);
            }
        }
        TestAssert.Equal(selectedStep == 0 ? 6 : 1, tested, "selected local fixture actually ran every requested step");
        GD.Print($"KEYBOARD_SHORTCUT_SIDE_LOCAL_OK selected_step={selectedStep} tested={tested} fixture=true fullRoute=false");
    }
}
