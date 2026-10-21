using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Config;
using Knight.Player;

namespace Knight.Tests.Integration;

public static class ThrusterMotionForecastRuntimeTests
{
    public static async Task RunSnapshot(SceneTree tree)
    {
        PropertyInfo property = typeof(PlayerMotor).GetProperty("MotionSettings", BindingFlags.Public | BindingFlags.Instance);
        TestAssert.True(property != null && property.PropertyType == typeof(PlayerMotionSettings) && property.SetMethod == null,
            "actual Motor immutable motion snapshot is unavailable to route consumers");
        using var tuning = new PlayerTuning { MaxRunSpeed = 175f, AirAcceleration = 800f, Gravity = 1600f };
        var root = new Node2D { Name = "ActualMotionSnapshot" };
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, -1000f);
        player.GetNode<Camera2D>("Camera2D").Enabled = false;
        var motor = player.GetNode<PlayerMotor>("Motor");
        motor.Tuning = tuning;
        bool rejectedUnready = false;
        try { property.GetValue(motor); }
        catch (TargetInvocationException exception) when (exception.InnerException is InvalidOperationException)
        { rejectedUnready = true; }
        TestAssert.True(rejectedUnready, "unready Motor never supplies a fabricated default forecast snapshot");
        root.AddChild(player);
        bool armed = false;
        int samples = 0;
        Vector2 samplePosition = default, sampleVelocity = default;
        root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100, Observe = () =>
        {
            if (!armed || samples >= 15) return;
            samples++;
            samplePosition = player.Position;
            sampleVelocity = player.Velocity;
        } });
        try
        {
            ThrusterPlayerRuntimeTests.Release();
            tree.Root.AddChild(root);
            var input = player.GetNode<PlayerInputReader>("Input");
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 1);
            TestAssert.Equal(60, Engine.PhysicsTicksPerSecond, "literal forecast control uses actual native 60Hz");
            var snapshot = (PlayerMotionSettings)property.GetValue(motor);
            TestAssert.True(snapshot.MaxRunSpeed == 175f && snapshot.AirAcceleration == 800f && snapshot.Gravity == 1600f,
                "consumer reads actual nondefault Ready snapshot, not library defaults");
            tuning.MaxRunSpeed = 500f; tuning.AirAcceleration = 50f; tuning.Gravity = 10f;
            TestAssert.True(ReferenceEquals(snapshot, property.GetValue(motor)) && snapshot.MaxRunSpeed == 175f &&
                snapshot.AirAcceleration == 800f && snapshot.Gravity == 1600f,
                "author Resource hot edits never replace actual applied immutable motion snapshot");
            Vector2 start = player.Position;
            motor.ExternalImpulse(new Vector2(600f, 0f));
            armed = true;
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 15);
            armed = false;
            TestAssert.Equal(15, samples, "observe exactly fifteen real completed native moves");
            TestAssert.Near(402.19203f, sampleVelocity.X, .004f, "real airborne neutral uses module exponential drag");
            TestAssert.Near(121.98890f, samplePosition.X - start.X, .004f,
                "real native post-step displacement agrees with independent finite geometric sum");
            TestAssert.Near(400f, sampleVelocity.Y, .004f, "real gravity uses applied snapshot after Resource hot edit");
            TestAssert.True(!player.IsOnFloor() && motor.AcceptedJumpCount == 0 && motor.AcceptedDashCount == 0,
                "airborne control used no jump, jet, floor or artificial per-step motion");
            samples = 0;
            motor.ExternalImpulse(Vector2.Zero);
            ThrusterPlayerRuntimeTests.KeyEvent(Key.D, true);
            armed = true;
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 15);
            armed = false;
            TestAssert.Near(175f, sampleVelocity.X, .004f,
                "real keyboard reaches applied 175 run cap, not hot-edited 500 or default 260");
            GD.Print("THRUSTER_MOTION_SNAPSHOT_OK actual_saved_player=true nondefault_ready_snapshot=true hot_edit_frozen=true native_geometric_sum=true automatic_keyboard=true no_default_fallback=true human=false whole_route=false");
        }
        finally
        {
            armed = false;
            ThrusterPlayerRuntimeTests.Release();
            tree.Paused = false;
            root.Free();
        }
    }
}
