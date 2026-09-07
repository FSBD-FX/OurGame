using System;
using Godot;
using Knight.Audio;
using Knight.Prologue;

namespace Knight.Tests.Integration;

public partial class PrologueAudioTeardownProbe : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            var root = new Node2D { Name = "PrologueAudioTeardownFixture" };
            PrologueAudioDirector director = PrologueAudioDirector.CreateRuntimeTree();
            director.Name = "Audio";
            root.AddChild(director);
            Root.AddChild(root);
            await ToSignal(this, SignalName.ProcessFrame);

            director.SetMusicState(PrologueMusicState.BossPhaseTwo);
            director.SetRoomAmbience(PrologueRoomId.FallenBellkeeperArena);
            foreach (PrologueAudioCue cue in new[]
            {
                PrologueAudioCue.PlayerAttack,
                PrologueAudioCue.PlayerHit,
                PrologueAudioCue.BossHammer,
                PrologueAudioCue.BossDefeat,
                PrologueAudioCue.Completion
            }) director.Play(cue, Vector2.Zero);
            var visibleSentinel = new Node2D { Name = "VisibleSentinel" };
            root.AddChild(visibleSentinel);
            await TestSceneTeardown.DrainAudioOwner(this, root);
            TestAssert.True(root.IsInsideTree() && visibleSentinel.IsInsideTree() &&
                root.GetNodeOrNull<PrologueAudioDirector>("Audio") == null,
                "audio-owner drain preserves the visible scene while releasing formal audio");
            root.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            GD.Print("PROLOGUE_AUDIO_TEARDOWN_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

}
