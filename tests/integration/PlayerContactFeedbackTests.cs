using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Config;
using Knight.Enemies.Laborer;
using Knight.Player;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Shared;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class PlayerContactFeedbackTests
{
    public static async Task Run(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        PlayerAttackContactGeometryTests.Run(tree);
        // This is a controlled contact fixture, not proof of a natural approach route.
        // Start, AI, input sampling, combat, collision and VFX remain production code.
        string directory = Path.GetFullPath(Path.Combine(Path.GetTempPath(),
            "knight-player-contact-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        BasinQualitySampleEntry entry = null;
        PlayerCombat combat = null;
        PlayerPresentationCoordinator coordinator = null;
        PresentationProfile originalProfile = null;
        PlayerCombat.AttackConnectedEventHandler connectedHandler = null;
        try
        {
            ReleaseInputs();
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            entry = GD.Load<PackedScene>("res://scenes/prologue/sample/BasinQualitySample.tscn")
                .Instantiate<BasinQualitySampleEntry>();
            entry.StorageDirectory = directory;
            tree.Root.AddChild(entry);
            await Frames(tree, 2);
            var level = entry.Level;
            level.GetNode<Button>("ProductUi/MainMenu/Panel/Buttons/Start")
                .EmitSignal(Button.SignalName.Pressed);
            await Frames(tree, 3);
            TestAssert.Equal(PrologueGameFlowState.Playing,
                level.GetNode<PrologueGameFlowController>("GameFlow").State,
                "actual sample Start reaches Playing before the controlled contact fixture");

            CharacterBody2D player = level.Player;
            var enemy = level.CurrentSectorRoot.GetNode<BellBackLaborer>(
                "Gameplay/Actors/BellBackLaborer");
            var target = enemy.GetNode<Hurtbox>("Hurtbox");
            var targetShape = target.GetNode<CollisionShape2D>("HurtboxCollider");
            combat = player.GetNode<PlayerCombat>("Combat");
            var attackShape = combat.GetNode<CollisionShape2D>("AttackHitbox/AttackCollider");
            var vfx = level.GetNode<PrologueVfxDirector>("Vfx");
            var audio = level.GetNode<PrologueAudioDirector>("Audio");
            coordinator = player.GetNode<PlayerPresentationCoordinator>("Presentation");
            originalProfile = coordinator.Profile;
            coordinator.Profile = (PresentationProfile)originalProfile.Duplicate();
            coordinator.Profile.CueScenes = originalProfile.CueScenes.Duplicate();
            var authoredEffect = new RecordingContactReadyEffect { Name = "ContactReadyEffect" };
            var effectScene = new PackedScene();
            try
            {
                TestAssert.Equal(Error.Ok, effectScene.Pack(authoredEffect),
                    "custom ready-observing contact effect packs successfully");
            }
            finally { authoredEffect.Free(); }
            coordinator.Profile.CueScenes[PresentationCue.AttackHit.ToString()] = effectScene;

            // The only controlled placement: put the player in normal horizontal reach.
            // Do not freeze either actor, inject damage, call TryConnect, or emit a hit.
            player.GlobalPosition = enemy.GlobalPosition + new Vector2(-72f, 0f);
            player.Velocity = Vector2.Zero;
            Send(InputActions.SelectAttackMode, true);
            Send(InputActions.Slot1, true);
            Send(InputActions.MoveRight, true);
            await Frames(tree, 1);
            ReleaseInputs();
            await Frames(tree, 1);
            vfx.ClearTransientEffects();

            int connections = 0;
            PrologueVfxBurst actualBurst = null;
            RecordingContactReadyEffect customEffect = null;
            Vector2 burstPosition = default, playerPosition = default, targetLocal = default;
            Vector2 attackLocal = default, audioPosition = default;
            PrologueAudioCue audioCue = default;
            connectedHandler = hurtbox =>
            {
                if (hurtbox != target) return;
                connections++;
                actualBurst = vfx.GetNode<Node2D>("World").GetChildren()
                    .OfType<PrologueVfxBurst>()
                    .LastOrDefault(node => node.Name.ToString().StartsWith("AttackHitBurst",
                        StringComparison.Ordinal) && !node.IsQueuedForDeletion());
                if (actualBurst == null) return;
                burstPosition = actualBurst.GlobalPosition;
                playerPosition = player.GlobalPosition;
                targetLocal = targetShape.ToLocal(burstPosition);
                attackLocal = attackShape.ToLocal(burstPosition);
                audioCue = audio.LastCue;
                audioPosition = audio.LastWorldPosition;
                customEffect = player.GetNode<Node2D>("VisualRoot/Effects").GetChildren()
                    .OfType<RecordingContactReadyEffect>().SingleOrDefault();
            };
            combat.AttackConnected += connectedHandler;
            Send(InputActions.PrimaryAction, true);
            await Frames(tree, 2);
            Send(InputActions.PrimaryAction, false);
            for (int frame = 0; frame < 60 && connections == 0; frame++) await Frames(tree, 1);

            TestAssert.Equal(1, connections, "real sample InputMap attack connects once to the real Laborer");
            TestAssert.True(actualBurst != null, "actual connection creates an AttackHitBurst under Level/Vfx/World");
            GD.Print($"PLAYER_CONTACT_ACTUAL burst={burstPosition} player={playerPosition} " +
                $"target_local={targetLocal} attack_local={attackLocal}");
            TestAssert.True(burstPosition.DistanceTo(playerPosition) > 20f,
                "actual AttackHitBurst must mark enemy contact, not the player's body center");

            // Independently check the authored capsule boundary, not the production
            // contact resolver or a copied choice of a contact-pair index.
            var capsule = (CapsuleShape2D)targetShape.Shape;
            float segmentHalf = capsule.Height * .5f - capsule.Radius;
            Vector2 nearestAxis = new(0f, Mathf.Clamp(targetLocal.Y, -segmentHalf, segmentHalf));
            TestAssert.Near(capsule.Radius, targetLocal.DistanceTo(nearestAxis), 2f,
                "actual burst lies at the target capsule's contacted surface");
            Vector2 halfAttack = ((RectangleShape2D)attackShape.Shape).Size * .5f;
            TestAssert.True(Mathf.Abs(attackLocal.X) <= halfAttack.X + 2f &&
                Mathf.Abs(attackLocal.Y) <= halfAttack.Y + 2f,
                "surface feedback remains beside the real attacking rectangle, not a remote target edge");
            TestAssert.True(actualBurst.Radius <= 20f,
                "short contact feedback cannot retain the old large 34px impact ring envelope");
            TestAssert.True(actualBurst.IsContactSpark,
                "actual hit burst selects the short directional drawing branch, not the generic halo");
            TestAssert.Equal(PrologueAudioCue.PlayerHit, audioCue, "contact retains the existing hit sound cue");
            TestAssert.True(audioPosition.DistanceTo(playerPosition) < .01f,
                "visual contact relocation does not move the existing audio source");
            TestAssert.True(customEffect != null && customEffect.ReadyObserved,
                "actual hit instantiates the configured cue scene and runs its real _Ready callback");
            GD.Print($"PLAYER_CONTACT_CUSTOM_READY ready={customEffect.ReadyPosition} contact={burstPosition}");
            TestAssert.True(customEffect.ReadyPosition.DistanceTo(burstPosition) < .01f,
                "configured contact cue must already be at the contact during _Ready, not moved there after AddChild");
            await Frames(tree, 20);
            TestAssert.True(!GodotObject.IsInstanceValid(actualBurst) || actualBurst.IsQueuedForDeletion(),
                "contact feedback expires without leaving a persistent mark");
            GD.Print("PLAYER_CONTACT_FEEDBACK_OK controlled_placement=True real_input=True");
        }
        finally
        {
            ReleaseInputs();
            tree.Paused = false;
            if (GodotObject.IsInstanceValid(combat) && connectedHandler != null)
                combat.AttackConnected -= connectedHandler;
            if (GodotObject.IsInstanceValid(coordinator) && originalProfile != null)
                coordinator.Profile = originalProfile;
            if (GodotObject.IsInstanceValid(entry))
            {
                if (entry.IsInsideTree())
                {
                    if (GodotObject.IsInstanceValid(entry.Level))
                        await TestSceneTeardown.DrainAudioOwner(tree, entry.Level);
                    entry.QueueFree();
                    await Frames(tree, 2);
                }
                else entry.Free();
            }
            string tempRoot = Path.GetFullPath(Path.GetTempPath());
            if (Path.GetDirectoryName(directory)?.TrimEnd(Path.DirectorySeparatorChar)
                    .Equals(tempRoot.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) == true &&
                Path.GetFileName(directory).StartsWith("knight-player-contact-", StringComparison.Ordinal) &&
                Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static void Send(StringName action, bool held)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = held, Strength = held ? 1f : 0f });
        Input.FlushBufferedEvents();
    }

    private static void ReleaseInputs()
    {
        foreach (StringName action in new[] { InputActions.MoveLeft, InputActions.MoveRight,
            InputActions.MoveUp, InputActions.MoveDown, InputActions.Jump, InputActions.Dash,
            InputActions.SelectAttackMode, InputActions.SelectGrappleMode, InputActions.PrimaryAction,
            InputActions.Shield, InputActions.Slot1 }) Send(action, false);
    }

    private static async Task Frames(SceneTree tree, int count)
    {
        for (int index = 0; index < count; index++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
