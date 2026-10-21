using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBasinQualitySampleTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await BasinQualitySampleEntryTests.ContextualGuideFollowsSessionLifecycle(this);
            GD.Print("BASIN_QUALITY_SAMPLE_GUIDE_OK");
            await BasinQualitySampleEntryTests.AuthoredBasinStartPersistsCheckpointBeforePhysics(this);
            GD.Print("BASIN_QUALITY_SAMPLE_AUTHORED_START_OK");
            await BasinQualitySampleEntryTests.RunProductionLifecycle(this);
            GD.Print("BASIN_QUALITY_SAMPLE_LIFECYCLE_OK");
            await BasinQualitySampleEntryTests.CampWaitsForPlayerApproach(this);
            GD.Print("BASIN_QUALITY_SAMPLE_CAMP_APPROACH_OK");
            await BasinQualitySampleEntryTests.RunSaveIsolationAndLocalization(this);
            GD.Print("BASIN_QUALITY_SAMPLE_ISOLATION_OK");
            await BasinQualitySampleEntryTests.DefaultMainStillStartsAtEntrance(this);
            GD.Print("BASIN_QUALITY_SAMPLE_DEFAULT_MAIN_OK");
            TestSceneTeardown.DrainManagedWrappersAtProcessExit("basin-quality-sample");
            GD.Print("BASIN_QUALITY_SAMPLE_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
