using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Godot;

namespace Knight.Presentation;

public sealed class PlayerAnimationMap
{
    private static readonly IReadOnlyDictionary<string, PlayerAnimationClip> Known =
        new ReadOnlyDictionary<string, PlayerAnimationClip>(new Dictionary<string, PlayerAnimationClip>(StringComparer.Ordinal)
        {
            ["Idle"] = PlayerAnimationClip.Idle,
            ["StartRun"] = PlayerAnimationClip.StartRun,
            ["Run"] = PlayerAnimationClip.Run,
            ["Brake"] = PlayerAnimationClip.Brake,
            ["JumpStart"] = PlayerAnimationClip.JumpStart,
            ["Move"] = PlayerAnimationClip.Move,
            ["Rise"] = PlayerAnimationClip.Rise,
            ["Apex"] = PlayerAnimationClip.Apex,
            ["Fall"] = PlayerAnimationClip.Fall,
            ["SoftLand"] = PlayerAnimationClip.SoftLand,
            ["HardLand"] = PlayerAnimationClip.HardLand,
            ["Land"] = PlayerAnimationClip.Land,
            ["Dash"] = PlayerAnimationClip.Dash,
            ["GroundAttack"] = PlayerAnimationClip.GroundAttack,
            ["AirAttack"] = PlayerAnimationClip.AirAttack,
            ["DownAttack"] = PlayerAnimationClip.DownAttack,
            ["ThreadCast"] = PlayerAnimationClip.ThreadCast,
            ["ThreadPreview"] = PlayerAnimationClip.ThreadPreview,
            ["ThreadWindup"] = PlayerAnimationClip.ThreadWindup,
            ["ThreadThrow"] = PlayerAnimationClip.ThreadThrow,
            ["ThreadLatch"] = PlayerAnimationClip.ThreadLatch,
            ["ThreadApply"] = PlayerAnimationClip.ThreadApply,
            ["ThreadFailed"] = PlayerAnimationClip.ThreadFailed,
            ["Hurt"] = PlayerAnimationClip.Hurt,
            ["Shield"] = PlayerAnimationClip.Shield,
            ["Dead"] = PlayerAnimationClip.Dead,
            ["Recovery"] = PlayerAnimationClip.Recovery
        });

    private readonly IReadOnlyDictionary<EntityVisualState, PlayerAnimationClip> _states;
    private readonly IReadOnlyDictionary<PlayerLocomotionState, PlayerAnimationClip> _locomotion;
    private readonly IReadOnlyDictionary<PlayerActionState, PlayerAnimationClip> _actions;

    private PlayerAnimationMap(
        int version,
        IReadOnlyDictionary<EntityVisualState, PlayerAnimationClip> states,
        IReadOnlyDictionary<PlayerLocomotionState, PlayerAnimationClip> locomotion,
        IReadOnlyDictionary<PlayerActionState, PlayerAnimationClip> actions,
        IReadOnlySet<PlayerAnimationClip> declaredClips)
    {
        Version = version;
        _states = states;
        _locomotion = locomotion;
        _actions = actions;
        DeclaredClips = declaredClips;
    }

    public int Version { get; }
    public IReadOnlySet<PlayerAnimationClip> DeclaredClips { get; }

    public static PlayerAnimationMap Load(string resourcePath)
    {
        string json = Godot.FileAccess.GetFileAsString(resourcePath);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException($"Player animation map is missing or empty: {resourcePath}");
        return Parse(json, resourcePath);
    }

    internal static PlayerAnimationMap Parse(string json, string source)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException($"Player animation map is missing or empty: {source}");
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            int version = root.GetProperty("version").GetInt32();
            if (version != 2)
                throw new InvalidDataException(
                    $"Unsupported player animation map version '{version}' in {source}.");
            JsonElement clips = root.GetProperty("clips");
            var declared = new HashSet<PlayerAnimationClip>();
            foreach (JsonElement row in clips.EnumerateArray())
            {
                string name = row.GetProperty("name").GetString() ?? string.Empty;
                if (!Known.TryGetValue(name, out PlayerAnimationClip clip))
                    throw new InvalidDataException($"Unknown player animation clip '{name}'.");
                double fps = row.GetProperty("fps").GetDouble();
                bool loop = row.GetProperty("loop").GetBoolean();
                if (!double.IsFinite(fps) || fps <= 0d)
                    throw new InvalidDataException(
                        $"Player animation clip '{name}' has invalid fps '{fps}'.");
                if (Math.Abs(fps - clip.Fps) > .000001d || loop != clip.Loop)
                    throw new InvalidDataException(
                        $"Player animation clip '{name}' metadata does not match its runtime contract.");
                if (!declared.Add(clip))
                    throw new InvalidDataException($"Duplicate player animation clip '{name}'.");
            }

            foreach (PlayerAnimationClip required in Known.Values)
            {
                if (!declared.Contains(required))
                    throw new InvalidDataException($"Player animation map is missing '{required.Name}'.");
            }

            JsonElement states = root.GetProperty("states");
            var mapping = new Dictionary<EntityVisualState, PlayerAnimationClip>();
            foreach (EntityVisualState state in Enum.GetValues<EntityVisualState>())
            {
                string name = states.GetProperty(state.ToString()).GetString() ?? string.Empty;
                if (!Known.TryGetValue(name, out PlayerAnimationClip clip))
                    throw new InvalidDataException($"Invalid player state mapping '{state}' -> '{name}'.");
                mapping.Add(state, clip);
            }

            JsonElement locomotionRows = root.GetProperty("locomotion");
            var locomotion = new Dictionary<PlayerLocomotionState, PlayerAnimationClip>();
            foreach (PlayerLocomotionState state in Enum.GetValues<PlayerLocomotionState>())
            {
                string name = locomotionRows.GetProperty(state.ToString()).GetString() ?? string.Empty;
                if (!Known.TryGetValue(name, out PlayerAnimationClip clip))
                    throw new InvalidDataException($"Invalid player locomotion mapping '{state}' -> '{name}'.");
                locomotion.Add(state, clip);
            }

            JsonElement actionRows = root.GetProperty("actions");
            var actions = new Dictionary<PlayerActionState, PlayerAnimationClip>();
            foreach (PlayerActionState state in Enum.GetValues<PlayerActionState>())
            {
                if (state == PlayerActionState.None) continue;
                string name = actionRows.GetProperty(state.ToString()).GetString() ?? string.Empty;
                if (!Known.TryGetValue(name, out PlayerAnimationClip clip))
                    throw new InvalidDataException($"Invalid player action mapping '{state}' -> '{name}'.");
                actions.Add(state, clip);
            }

            return new PlayerAnimationMap(
                version,
                new ReadOnlyDictionary<EntityVisualState, PlayerAnimationClip>(mapping),
                new ReadOnlyDictionary<PlayerLocomotionState, PlayerAnimationClip>(locomotion),
                new ReadOnlyDictionary<PlayerActionState, PlayerAnimationClip>(actions),
                declared);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or
            KeyNotFoundException)
        {
            throw new InvalidDataException(
                $"Player animation map JSON is malformed: {source}", exception);
        }
    }

    public PlayerAnimationClip Resolve(EntityPresentationFrame frame)
    {
        if (frame.State == EntityVisualState.Attack)
            return ResolveAttack(frame.Grounded, aimingDown: false);
        return _states[frame.State];
    }

    public PlayerAnimationClip ResolveLocomotion(PlayerLocomotionFrame frame) => _locomotion[frame.State];

    public PlayerAnimationClip ResolveAction(PlayerActionFrame frame) => frame.State == PlayerActionState.None
        ? PlayerAnimationClip.Idle
        : _actions[frame.State];

    public PlayerAnimationClip ResolveAttack(bool grounded, bool aimingDown)
    {
        if (grounded) return PlayerAnimationClip.GroundAttack;
        return aimingDown ? PlayerAnimationClip.DownAttack : PlayerAnimationClip.AirAttack;
    }

    public PlayerAnimationClip ResolveCue(PresentationCue cue, EntityPresentationFrame frame) => cue switch
    {
        PresentationCue.AttackStarted => ResolveAttack(frame.Grounded, aimingDown: false),
        PresentationCue.Downstrike => ResolveAttack(grounded: false, aimingDown: true),
        PresentationCue.ThreadFired or PresentationCue.ThreadHit or
        PresentationCue.ThreadPreview or PresentationCue.ThreadWindup or
        PresentationCue.ThreadTravel or PresentationCue.ThreadLatched or
        PresentationCue.ThreadApplied or PresentationCue.ThreadRecovery => PlayerAnimationClip.ThreadCast,
        PresentationCue.Hurt => PlayerAnimationClip.Hurt,
        PresentationCue.Died => PlayerAnimationClip.Dead,
        PresentationCue.ThreadFailed => PlayerAnimationClip.Recovery,
        _ => Resolve(frame)
    };

    public static bool IsThreadCastCue(PresentationCue cue) => cue is
        PresentationCue.ThreadFired or PresentationCue.ThreadHit or
        PresentationCue.ThreadPreview or PresentationCue.ThreadWindup or
        PresentationCue.ThreadTravel or PresentationCue.ThreadLatched or
        PresentationCue.ThreadApplied or PresentationCue.ThreadRecovery;
}
