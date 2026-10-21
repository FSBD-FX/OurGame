using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunKeyboardControlsTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            SentryCounterEnergyBudgetTests.Run();
            if (Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--driver-only"))
            {
                KeyboardInputTraceBufferTests.Run();
                KeyboardInputTraceSidecarTests.Run();
                await KeyboardRouteDriverRuntimeTests.Run(this);
                GD.Print("KEYBOARD_ROUTE_DRIVER_ONLY_OK"); Quit(0); return;
            }
            if (!Array.Exists(OS.GetCmdlineUserArgs(), value => value == "--runtime-only"))
            {
                KeyboardInputTraceBufferTests.Run();
                KeyboardInputTraceSidecarTests.Run();
                PlayerActionCommandModelTests.Run();
                PlayerKeyboardAimModelTests.Run();
                EnvironmentInteractionModelTests.Run();
                KeyboardInputTelemetryTests.Run();
                PrologueObjectiveTests.Run();
                PrologueHudViewModelTests.Run();
                PrologueStringsTests.Run();
                InputGlyphResolverTests.Run();
                ProloguePlaytestSessionTests.Run();
                PrologueGuidanceModelTests.RunTempleTerrainGuidance();
            }
            await PrologueUiRuntimeTests.Run(this);
            await KeyboardRouteDriverRuntimeTests.Run(this);
            await KeyboardTeachingRuntimeTests.RunUi(this);
            KeyboardTeachingRuntimeTests.RunGuidance();
            string teachingStorage = await KeyboardTeachingRuntimeTests.RunProgress(this);
            await KeyboardTeachingRuntimeTests.RunSampleGuide(this, teachingStorage);
            await KeyboardActionRuntimeTests.Run(this);
            await KeyboardClawTargetTests.Run(this);
            await KeyboardClawRuntimeTests.Run(this);
            await EnvironmentInteractionRuntimeTests.Run(this);
            // The wrapper requires the saved-level Q suite in its own cold
            // process after this gauntlet; do not reuse retired fixture resources.
            GD.Print("KEYBOARD_CONTROLS_OK");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
