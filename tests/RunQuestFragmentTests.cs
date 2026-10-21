using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunQuestFragmentTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            if (Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--fragment-read-only"))
            {
                QuestFragmentCrossProcessTests.ReadOnly();
                GD.Print("QUEST_FRAGMENTS_OK");
                Quit(0);
                return;
            }
            QuestFragmentLoadObservation.Record("prefix-enter");
            QuestFragmentProgressTests.Run();
            QuestFragmentLoadObservation.Record("progress-return");
            QuestFragmentSaveTests.Run();
            QuestFragmentLoadObservation.Record("save-return");
            QuestFragmentStateTests.Run();
            QuestFragmentLoadObservation.Record("state-return");
            PrologueSaveTests.Run();
            QuestFragmentLoadObservation.Record("prologue-save-return");
            ContinuousCanyonMigrationTests.RunStore();
            QuestFragmentLoadObservation.Record("migration-store-return");
            PrologueHudViewModelTests.Run();
            QuestFragmentLoadObservation.Record("hud-return");
            BossAdmissionModelTests.Run();
            QuestFragmentLoadObservation.Record("admission-model-return");
            QuestFragmentPlacementTests.Run();
            QuestFragmentLoadObservation.Record("placement-return");
            await QuestFragmentPersistenceRuntimeTests.Run(this);
            await QuestFragmentCollectionTests.Run(this);
            await BasinSupplyRuntimeTests.Run(this);
            GD.Print("QUEST_FRAGMENTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
