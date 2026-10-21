using System;
using Godot;
using Knight.Tests.Integration;
using Knight.Tests.Unit;

namespace Knight.Tests;

public partial class RunPlayerCombatReadabilityTests : SceneTree
{
    public override async void _Initialize()
    {
        try
        {
            if (Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--cloak-only"))
            {
                SecondaryMotionModelTests.Run();
                PlayerPoseMixerTests.Run();
                PlayerCombatPoseTests.Run();
                await PlayerMantleMotionTests.Run(this);
                GD.Print("PLAYER_MANTLE_MOTION_TESTS_OK");
                Quit(0);
                return;
            }
            if (Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--facing-only"))
            {
                await PlayerShieldFacingAttackTests.Run(this);
                Quit(0);
                return;
            }
            bool contactOnly = Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--contact-only");
            bool poseOnly = Array.Exists(OS.GetCmdlineUserArgs(), argument => argument == "--pose-only");
            if (!contactOnly) PlayerCombatPoseTests.Run();
            if (!contactOnly && !poseOnly)
            {
                PlayerPresentationTests.Run();
                PlayerPoseMixerTests.Run();
                PlayerAnimationMapTests.Run();
                await PlayerCombatReadabilityTests.Run(this);
                await PlayerShieldAttackHandoffTests.Run(this);
                await PlayerShieldFacingAttackTests.Run(this);
            }
            if (!poseOnly) await PlayerContactFeedbackTests.Run(this);
            GD.Print("PLAYER_COMBAT_READABILITY_TESTS_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
