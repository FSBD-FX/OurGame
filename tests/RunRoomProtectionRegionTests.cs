using System;
using Godot;
using Knight.Tests.Integration;

namespace Knight.Tests;

public partial class RunRoomProtectionRegionTests : SceneTree
{
    public override async void _Initialize()
    {
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            RoomProtectionRegionRuntimeTests.Run(this);
            GD.Print("ROOM_PROTECTION_REGIONS_OK fullRoute=false performanceCertification=false");
            Quit(0);
        }
        catch (Exception exception) { GD.PushError(exception.ToString()); Quit(1); }
    }
}
