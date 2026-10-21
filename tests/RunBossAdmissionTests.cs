using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunBossAdmissionTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            await ToSignal(this, SignalName.ProcessFrame);
            await BossAdmissionRuntimeTests.Run(this);
            GD.Print("BOSS_ADMISSION_ENTITY_OK controlled_metadata=true natural_route=false");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
