using System;
using Godot;
using Knight.Presentation;
using Knight.Audio;
using Knight.Settings;
using Knight.Vfx;

namespace Knight.Tests.Unit;

public static class PresentationCueCoverageTests
{
    private static readonly PresentationCue[] Required =
    [
        PresentationCue.AttackStarted,
        PresentationCue.AttackHit,
        PresentationCue.Hurt,
        PresentationCue.Guard,
        PresentationCue.Dash,
        PresentationCue.Downstrike,
        PresentationCue.ThreadFired,
        PresentationCue.ThreadHit,
        PresentationCue.ThreadFailed,
        PresentationCue.CheckpointActivated,
        PresentationCue.ShortcutOpened,
        PresentationCue.BossTransition,
        PresentationCue.BossExposed,
        PresentationCue.BossDefeated,
        PresentationCue.RecoveryStarted,
        PresentationCue.RecoveryCompleted
    ];

    public static void Run()
    {
        foreach (PresentationCue cue in Required)
            TestAssert.True(PrologueVfxDirector.HasExplicitMapping(cue), $"VFX cue {cue} has an explicit mapping");
        foreach (PrologueAudioCue cue in new[]
        {
            PrologueAudioCue.ModeAttack, PrologueAudioCue.ModeGrapple, PrologueAudioCue.EmptySlot,
            PrologueAudioCue.EnergyEmpty, PrologueAudioCue.ShieldRaise, PrologueAudioCue.ShieldBlock,
            PrologueAudioCue.ShieldHeavyBreak, PrologueAudioCue.ShieldOverheat, PrologueAudioCue.ShieldReady,
            PrologueAudioCue.ClawFire, PrologueAudioCue.ClawTravel, PrologueAudioCue.ClawLatch,
            PrologueAudioCue.ClawTension, PrologueAudioCue.ClawRelease, PrologueAudioCue.ClawIceSlip,
            PrologueAudioCue.ClawFail, PrologueAudioCue.ThreadFail, PrologueAudioCue.ThreadPreview
        })
            TestAssert.True(PrologueVfxDirector.HasTask10Mapping(cue),
                $"Task10 typed VFX cue {cue} has an explicit mapping independent of color");

        var camera = new CameraFeedbackController { IntensityScale = .5f };
        CameraFeedback applied = camera.Request(new CameraFeedback(80f, 2f));
        TestAssert.Near(6f, applied.Intensity, .001f,
            "camera request clamps to 12px before applying the accessibility scale");
        TestAssert.Near(.35f, applied.DurationSeconds, .001f, "camera feedback duration clamps to 350ms");

        var director = new PrologueVfxDirector();
        director.Configure(new GameSettingsData { ScreenShake = .25f, ReduceFlashes = true });
        VfxCueStyle reduced = director.ResolveStyle(PresentationCue.BossTransition);
        TestAssert.True(!reduced.FullScreenFlash, "reduced-flash mode disables the full-screen flash");
        TestAssert.Near(.25f, director.ShakeScale, .001f, "screen-shake setting reaches the VFX director");
        director.Free();
        camera.Free();
    }
}
