using System.Collections.Generic;

namespace Knight.Presentation;

public sealed record PlayerAnimationClip(
    string Name,
    double Fps,
    bool Loop,
    IReadOnlyList<PlayerAtlasRect> Frames)
{
    private static readonly IReadOnlyList<PlayerAtlasRect> NoAtlasFrames = [];

    public static PlayerAnimationClip Idle { get; } = Named("Idle", 6d, true);
    public static PlayerAnimationClip StartRun { get; } = Named("StartRun", 12d, false);
    public static PlayerAnimationClip Run { get; } = Named("Run", 12d, true);
    public static PlayerAnimationClip Brake { get; } = Named("Brake", 12d, false);
    public static PlayerAnimationClip JumpStart { get; } = Named("JumpStart", 12d, false);
    public static PlayerAnimationClip Move { get; } = Named("Move", 12d, true);
    public static PlayerAnimationClip Rise { get; } = Named("Rise", 8d, false);
    public static PlayerAnimationClip Apex { get; } = Named("Apex", 8d, false);
    public static PlayerAnimationClip Fall { get; } = Named("Fall", 8d, true);
    public static PlayerAnimationClip SoftLand { get; } = Named("SoftLand", 12d, false);
    public static PlayerAnimationClip HardLand { get; } = Named("HardLand", 12d, false);
    public static PlayerAnimationClip Land { get; } = Named("Land", 12d, false);
    public static PlayerAnimationClip Dash { get; } = Named("Dash", 14d, false);
    public static PlayerAnimationClip GroundAttack { get; } = Named("GroundAttack", 16d, false);
    public static PlayerAnimationClip AirAttack { get; } = Named("AirAttack", 16d, false);
    public static PlayerAnimationClip DownAttack { get; } = Named("DownAttack", 16d, false);
    public static PlayerAnimationClip ThreadCast { get; } = Named("ThreadCast", 14d, false);
    public static PlayerAnimationClip ThreadPreview { get; } = Named("ThreadPreview", 10d, true);
    public static PlayerAnimationClip ThreadWindup { get; } = Named("ThreadWindup", 14d, false);
    public static PlayerAnimationClip ThreadThrow { get; } = Named("ThreadThrow", 18d, false);
    public static PlayerAnimationClip ThreadLatch { get; } = Named("ThreadLatch", 14d, false);
    public static PlayerAnimationClip ThreadApply { get; } = Named("ThreadApply", 12d, false);
    public static PlayerAnimationClip ThreadFailed { get; } = Named("ThreadFailed", 12d, false);
    public static PlayerAnimationClip Hurt { get; } = Named("Hurt", 12d, false);
    public static PlayerAnimationClip Dead { get; } = Named("Dead", 8d, false);
    public static PlayerAnimationClip Recovery { get; } = Named("Recovery", 10d, false);

    private static PlayerAnimationClip Named(string name, double fps, bool loop) =>
        new(name, fps, loop, NoAtlasFrames);
}
