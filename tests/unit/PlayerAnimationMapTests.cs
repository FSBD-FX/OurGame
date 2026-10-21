using Knight.Presentation;

namespace Knight.Tests.Unit;

public static class PlayerAnimationMapTests
{
    public static void Run()
    {
        PlayerAnimationMap map = PlayerAnimationMap.Load("res://assets/player/formal/player-animation-map.json");
        JsonMetadataIsParsedAndValidated(map);
        EveryTypedLocomotionAndActionHasADeclaredClip(map);
        TestAssert.Equal(PlayerAnimationClip.Idle, map.Resolve(new(EntityVisualState.Idle, 1, true)), "idle mapping");
        TestAssert.Equal(PlayerAnimationClip.Move, map.Resolve(new(EntityVisualState.Move, 1, true)), "move mapping");
        TestAssert.Equal(PlayerAnimationClip.Rise, map.Resolve(new(EntityVisualState.Rise, 1, false)), "rise mapping");
        TestAssert.Equal(PlayerAnimationClip.Fall, map.Resolve(new(EntityVisualState.Fall, 1, false)), "fall mapping");
        TestAssert.Equal(PlayerAnimationClip.Dash, map.Resolve(new(EntityVisualState.Dash, 1, false)), "dash mapping");
        TestAssert.Equal(PlayerAnimationClip.GroundAttack,
            map.Resolve(new(EntityVisualState.Attack, 1, true)), "ground attack mapping");
        TestAssert.Equal(PlayerAnimationClip.AirAttack,
            map.Resolve(new(EntityVisualState.Attack, 1, false)), "air attack mapping");
        TestAssert.Equal(PlayerAnimationClip.Hurt, map.Resolve(new(EntityVisualState.Hurt, 1, true)), "hurt mapping");
        TestAssert.Equal(PlayerAnimationClip.Dead, map.Resolve(new(EntityVisualState.Dead, 1, false)), "dead mapping");
        TestAssert.Equal(PlayerAnimationClip.ThreadCast, map.ResolveCue(PresentationCue.ThreadFired,
            new(EntityVisualState.Idle, 1, true)), "thread cast cue mapping");
        foreach (PresentationCue cue in new[]
                 {
                     PresentationCue.ThreadPreview,
                     PresentationCue.ThreadWindup,
                     PresentationCue.ThreadTravel,
                     PresentationCue.ThreadLatched,
                     PresentationCue.ThreadApplied,
                     PresentationCue.ThreadRecovery
                 })
        {
            TestAssert.Equal(PlayerAnimationClip.ThreadCast,
                map.ResolveCue(cue, new(EntityVisualState.Move, 1, true)),
                $"{cue} preserves the thread-cast clip instead of resolving locomotion");
        }
        TestAssert.Equal(PlayerAnimationClip.DownAttack, map.ResolveAttack(grounded: false, aimingDown: true),
            "down attack mapping");
        TestAssert.Equal(PlayerAnimationClip.Recovery,
            map.ResolveCue(PresentationCue.ThreadFailed, new(EntityVisualState.Move, 1, true)),
            "thread failure uses the distinct formal recovery/failure pose rather than locomotion");
        TestAssert.True(map.DeclaredClips.Contains(PlayerAnimationClip.Land) &&
            map.DeclaredClips.Contains(PlayerAnimationClip.Recovery), "land and recovery clips are declared");
    }

    private static void EveryTypedLocomotionAndActionHasADeclaredClip(PlayerAnimationMap map)
    {
        (PlayerLocomotionState State, PlayerAnimationClip Clip)[] locomotion =
        [
            (PlayerLocomotionState.Idle, PlayerAnimationClip.Idle),
            (PlayerLocomotionState.StartRun, PlayerAnimationClip.StartRun),
            (PlayerLocomotionState.Run, PlayerAnimationClip.Run),
            (PlayerLocomotionState.Brake, PlayerAnimationClip.Brake),
            (PlayerLocomotionState.JumpStart, PlayerAnimationClip.JumpStart),
            (PlayerLocomotionState.Rise, PlayerAnimationClip.Rise),
            (PlayerLocomotionState.Apex, PlayerAnimationClip.Apex),
            (PlayerLocomotionState.Fall, PlayerAnimationClip.Fall),
            (PlayerLocomotionState.SoftLand, PlayerAnimationClip.SoftLand),
            (PlayerLocomotionState.HardLand, PlayerAnimationClip.HardLand),
            (PlayerLocomotionState.Dash, PlayerAnimationClip.Dash),
            (PlayerLocomotionState.Dead, PlayerAnimationClip.Dead),
            (PlayerLocomotionState.Recovery, PlayerAnimationClip.Recovery)
        ];
        foreach ((PlayerLocomotionState state, PlayerAnimationClip clip) in locomotion)
        {
            TestAssert.Equal(clip, map.ResolveLocomotion(new PlayerLocomotionFrame(state, .4f)),
                $"typed locomotion {state} resolves its declared clip");
            TestAssert.True(map.DeclaredClips.Contains(clip),
                $"typed locomotion clip {clip.Name} is declared by the formal map");
        }

        (PlayerActionState State, PlayerActionPhase Phase, PlayerAnimationClip Clip)[] actions =
        [
            (PlayerActionState.GroundAttack, PlayerActionPhase.Anticipation, PlayerAnimationClip.GroundAttack),
            (PlayerActionState.AirAttack, PlayerActionPhase.Burst, PlayerAnimationClip.AirAttack),
            (PlayerActionState.DownAttack, PlayerActionPhase.Contact, PlayerAnimationClip.DownAttack),
            (PlayerActionState.ThreadPreview, PlayerActionPhase.Anticipation, PlayerAnimationClip.ThreadPreview),
            (PlayerActionState.ThreadWindup, PlayerActionPhase.Anticipation, PlayerAnimationClip.ThreadWindup),
            (PlayerActionState.ThreadThrow, PlayerActionPhase.Burst, PlayerAnimationClip.ThreadThrow),
            (PlayerActionState.ThreadLatch, PlayerActionPhase.Contact, PlayerAnimationClip.ThreadLatch),
            (PlayerActionState.ThreadApply, PlayerActionPhase.Contact, PlayerAnimationClip.ThreadApply),
            (PlayerActionState.ThreadFailed, PlayerActionPhase.Recovery, PlayerAnimationClip.ThreadFailed),
            (PlayerActionState.Hurt, PlayerActionPhase.Contact, PlayerAnimationClip.Hurt),
            (PlayerActionState.Shield, PlayerActionPhase.Contact, PlayerAnimationClip.Shield)
        ];
        foreach ((PlayerActionState state, PlayerActionPhase phase, PlayerAnimationClip clip) in actions)
        {
            TestAssert.Equal(clip, map.ResolveAction(new PlayerActionFrame(state, phase, .4f,
                phase == PlayerActionPhase.Recovery)),
                $"typed action {state} resolves its declared clip");
            TestAssert.True(map.DeclaredClips.Contains(clip),
                $"typed action clip {clip.Name} is declared by the formal map");
        }
        TestAssert.Equal(PlayerAnimationClip.Idle, map.ResolveAction(PlayerActionFrame.None),
            "None action resolves a harmless declared compatibility clip");
    }

    private static void JsonMetadataIsParsedAndValidated(PlayerAnimationMap map)
    {
        TestAssert.Equal(2, map.Version, "formal player animation map parses schema version 2");
        TestAssert.Near(6f, (float)PlayerAnimationClip.Idle.Fps, .0001f,
            "declared Idle fps matches parsed JSON contract");
        TestAssert.True(PlayerAnimationClip.Idle.Loop,
            "declared Idle loop flag matches parsed JSON contract");
        TestAssert.True(!PlayerAnimationClip.GroundAttack.Loop,
            "declared GroundAttack loop flag matches parsed JSON contract");

        string json = Godot.FileAccess.GetFileAsString(
            "res://assets/player/formal/player-animation-map.json");
        AssertMetadataRejected(json.Replace("\"version\": 2", "\"version\": 99"),
            "unsupported version");
        AssertMetadataRejected(json.Replace("\"fps\": 6", "\"fps\": 0"),
            "non-positive fps");
        AssertMetadataRejected(json.Replace("\"loop\": true", "\"loop\": \"true\""),
            "non-boolean loop");
    }

    private static void AssertMetadataRejected(string json, string label)
    {
        try
        {
            _ = PlayerAnimationMap.Parse(json, $"test:{label}");
        }
        catch (System.IO.InvalidDataException)
        {
            return;
        }
        throw new System.InvalidOperationException(
            $"Assertion failed: player animation map rejects {label} metadata");
    }
}
