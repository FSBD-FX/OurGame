using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests.Presentation;

// Visible reproduction of actual collision damage. Controlled seam positioning and
// a lethal fixture are diagnostic evidence, not normal player traversal or combat.
public partial class BasinDeathRecoveryProbe : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await BasinQualitySampleEntryTests.LethalAreaContactDefersRecoveryUntilAfterContact(
                this, presentation: true);
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("basin-death-proof");
            GD.Print("BASIN_DEATH_RECOVERY_PROBE_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
