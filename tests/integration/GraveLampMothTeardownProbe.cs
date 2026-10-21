using System;
using Godot;
using Knight.Combat;
using Knight.Enemies.Moth;

namespace Knight.Tests.Integration;

public partial class GraveLampMothTeardownProbe : SceneTree
{
    private const string MothScenePath = "res://scenes/enemies/GraveLampMoth.tscn";
    private const int MothCount = 12;

    public override async void _Initialize()
    {
        try
        {
            var root = new Node2D { Name = "GraveLampMothTeardownFixture" };
            var player = new Node2D { Name = "Player", Position = new Vector2(0f, 140f) };
            player.AddToGroup("player");
            root.AddChild(player);
            int lockedCount = 0;
            using PackedScene mothScene = GD.Load<PackedScene>(MothScenePath);
            for (int index = 0; index < MothCount; index++)
            {
                GraveLampMoth moth = mothScene.Instantiate<GraveLampMoth>();
                moth.Name = $"GraveLampMoth{index}";
                bool counted = false;
                moth.AttackFrameChanged += frame =>
                {
                    if (counted || !frame.Timeline.HasLockedTarget) return;
                    counted = true;
                    lockedCount++;
                };
                root.AddChild(moth);
            }
            Root.AddChild(root);
            await ToSignal(this, SignalName.PhysicsFrame);
            double objectsBeforeLocks = Performance.GetMonitor(Performance.Monitor.ObjectCount);

            for (int frame = 1; frame < 90 && lockedCount < MothCount; frame++)
                await ToSignal(this, SignalName.PhysicsFrame);
            TestAssert.Equal(MothCount, lockedCount,
                "focused Moth teardown exercises every production preflight collision wrapper");
            double objectsAfterLocks = Performance.GetMonitor(Performance.Monitor.ObjectCount);
            GD.Print($"PROLOGUE_MOTH_WRAPPER_COUNT before={objectsBeforeLocks} after={objectsAfterLocks} " +
                $"delta={objectsAfterLocks - objectsBeforeLocks}");
            TestAssert.True(objectsAfterLocks - objectsBeforeLocks <= 1d,
                $"production Moth preflight disposes collision wrappers immediately; " +
                $"before={objectsBeforeLocks}, after={objectsAfterLocks}, " +
                $"delta={objectsAfterLocks - objectsBeforeLocks}");
            root.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            GD.Print("PROLOGUE_MOTH_TEARDOWN_OK count=12");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
