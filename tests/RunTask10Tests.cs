using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;
using Knight.Audio;

namespace Knight.Tests;

public partial class RunTask10Tests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            if (HasArgument("--stale-controls"))
            {
                InputGlyphResolverTests.Task10InputMapHasNoLegacyActions();
                GD.Print("TASK10_STALE_CONTROLS_OK legacy_actions=0");
                Quit(0);
                return;
            }

            if (HasArgument("--fix1-wiring-red"))
            {
                await PrologueAudioDirectorTests.Task10ProductionPublishersReachDirectors(this);
                GD.Print("TASK10_FIX1_PRODUCTION_WIRING_OK");
                Quit(0);
                return;
            }

            if (HasArgument("--fix2-action-red"))
            {
                await PrologueAudioDirectorTests.Task10ActionCueBaselineAndReentryAreExact(this);
                GD.Print("TASK10_FIX2_ACTION_CUES_OK false_first=0 energy_reentry=2 vfx=2");
                Quit(0);
                return;
            }

            if (HasArgument("--fix1-sector-red"))
            {
                PrologueProductionAssetRuntimeTests.Task10EmptySectorSetFailsClosed();
                GD.Print("TASK10_FIX1_SECTOR_SET_OK count=5");
                Quit(0);
                return;
            }

            if (HasArgument("--fix1-language-red"))
            {
                await PrologueAccessibilityTests.Task10ProductionEnglishFlowsToPause(this);
                GD.Print("TASK10_FIX1_PRODUCTION_LANGUAGE_OK");
                Quit(0);
                return;
            }

            if (HasArgument("--ui-structure"))
            {
                await PrologueUiRuntimeTests.Task10StableHudPathsExist(this);
                GD.Print("TASK10_UI_STRUCTURE_OK reticle_states=6 resolutions=4");
                Quit(0);
                return;
            }

            if (HasArgument("--cue-vfx"))
            {
                PrologueAudioManifestTests.Task10TypedCueNamesExist();
                PrologueAudioDirector cueAudio = PrologueAudioDirector.CreateRuntimeTree();
                Root.AddChild(cueAudio);
                await ToSignal(this, SignalName.ProcessFrame);
                PrologueAudioDirectorTests.Task10TransitionsAreEntryOnly(cueAudio);
                cueAudio.Shutdown();
                cueAudio.QueueFree();
                for (int frame = 0; frame < 5; frame++)
                    await ToSignal(this, SignalName.ProcessFrame);
                await PrologueVfxRuntimeTests.Task10ReplaceableEffectRootsExist(this);
                await PrologueVfxRuntimeTests.Task10TransitionsAreEntryOnlyAndClean(this);
                await PrologueAudioDirectorTests.Task10ActionCueBaselineAndReentryAreExact(this);
                await PrologueAudioDirectorTests.Task10ProductionPublishersReachDirectors(this);
                GD.Print("TASK10_CUE_VFX_OK typed_cues=16 cleanup_edges=4");
                Quit(0);
                return;
            }

            if (HasArgument("--vfx-roots"))
            {
                await PrologueVfxRuntimeTests.Task10ReplaceableEffectRootsExist(this);
                GD.Print("TASK10_VFX_ROOTS_OK roots=6");
                Quit(0);
                return;
            }

            if (HasArgument("--replacement"))
            {
                ProductionAssetManifestTests.Task10ReplacementSlotsAreExplicit();
                PrologueProductionAssetRuntimeTests.Task10ReplacementBoundaryMutationsFail();
                await PrologueProductionAssetRuntimeTests.Run(this);
                GD.Print("TASK10_REPLACEMENT_OK fail_closed=True");
                Quit(0);
                return;
            }

            if (HasArgument("--legacy-thread-fixture"))
            {
                await ThreadCastRuntimeTests.Run(this);
                GD.Print("TASK10_CURRENT_GRAPPLE_FIXTURE_OK");
                Quit(0);
                return;
            }

            if (HasArgument("--player-formal"))
            {
                await PlayerFormalVisualTests.Run(this);
                GD.Print("TASK10_CURRENT_ATTACK_PRESENTATION_OK");
                Quit(0);
                return;
            }

            if (HasArgument("--cleanup-fixtures"))
            {
                await BellChainRuntimeTests.Run(this);
                await GraveLampMothRuntimeTests.Run(this);
                GD.Print("TASK10_CURRENT_INPUT_CLEANUP_OK suites=2");
                Quit(0);
                return;
            }

            InputGlyphResolverTests.Task10InputMapHasNoLegacyActions();
            PrologueHudViewModelTests.Run();
            PrologueEnergyHeatStyleTests.Run();
            await BossDoorVisibilityTests.Run(this);
            await PrologueUiRuntimeTests.Task10StableHudPathsExist(this);
            await PrologueFormalHudTests.Run(this);
            await PrologueGuidanceRuntimeTests.ContinuousGuidanceProtectsLiveGameplay(this);
            PrologueAudioManifestTests.Task10TypedCueNamesExist();
            PrologueAudioDirector focusedAudio = PrologueAudioDirector.CreateRuntimeTree();
            Root.AddChild(focusedAudio);
            await ToSignal(this, SignalName.ProcessFrame);
            PrologueAudioDirectorTests.Task10TransitionsAreEntryOnly(focusedAudio);
            focusedAudio.Shutdown();
            focusedAudio.QueueFree();
            for (int frame = 0; frame < 5; frame++)
                await ToSignal(this, SignalName.ProcessFrame);
            await PrologueVfxRuntimeTests.Task10ReplaceableEffectRootsExist(this);
            await PrologueVfxRuntimeTests.Task10TransitionsAreEntryOnlyAndClean(this);
            await PrologueAudioDirectorTests.Task10ActionCueBaselineAndReentryAreExact(this);
            await PrologueAudioDirectorTests.Task10ProductionPublishersReachDirectors(this);
            ProductionAssetManifestTests.Task10ReplacementSlotsAreExplicit();
            PrologueProductionAssetRuntimeTests.Task10ReplacementBoundaryMutationsFail();
            await PrologueProductionAssetRuntimeTests.Run(this);
            await PrologueAccessibilityTests.Task10ProductionEnglishFlowsToPause(this);
            GD.Print("TASK10_FIX2_FOCUSED_OK false_first=0 energy_reentry=2 prompt_resolutions=4");
            GD.Print("TASK10_FIX1_FOCUSED_OK cues=16 sectors=5 languages=2 resolutions=4");
            GD.Print("TASK10_FOCUSED_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static bool HasArgument(string expected)
    {
        foreach (string argument in OS.GetCmdlineUserArgs())
            if (argument == expected) return true;
        return false;
    }
}
