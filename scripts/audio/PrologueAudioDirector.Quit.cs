using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace Knight.Audio;

public partial class PrologueAudioDirector
{
    public bool IsQuiescing { get; private set; }
    private ulong[] _applicationQuitOwnedIds = Array.Empty<ulong>();
    private Task<AudioQuitDrainResult> _applicationQuitPreparation;

    public void BeginApplicationQuit()
    {
        if (IsQuiescing) return;
        // Shutdown emits synchronous notifications. Revoke their start authority first.
        IsQuiescing = true;
        _applicationQuitOwnedIds = new[] { OwnedId(_thrusterLoop), OwnedId(_guidanceCrackleWav) }
            .Where(id => id != 0UL).Distinct().ToArray();
        Shutdown();
        MusicState = PrologueMusicState.Silent;
        ActiveMusicLayerCount = 0;
    }

    public Task<AudioQuitDrainResult> PrepareApplicationQuitAsync()
    {
        if (_applicationQuitPreparation != null) return _applicationQuitPreparation;
        var completion = new TaskCompletionSource<AudioQuitDrainResult>();
        _applicationQuitPreparation = completion.Task;
        try
        {
            BeginApplicationQuit();
            _ = ObserveApplicationQuitDrain(completion);
        }
        catch (Exception error) { completion.TrySetException(error); }
        return _applicationQuitPreparation;
    }

    private async Task ObserveApplicationQuitDrain(TaskCompletionSource<AudioQuitDrainResult> completion)
    {
        try
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree())
            {
                completion.TrySetResult(new(AudioQuitDrainState.Cancelled, 0, 0, false));
                return;
            }
            SceneTree tree = GetTree();
            ulong started = Time.GetTicksMsec();
            var gate = new AudioQuitDrainGate(AudioServer.GetTimeSinceLastMix(), Engine.GetProcessFrames());
            while (true)
            {
                bool owner = GodotObject.IsInstanceValid(this) && IsInsideTree() && !IsQueuedForDeletion() &&
                    GodotObject.IsInstanceValid(tree) && GetTree() == tree;
                ulong now = Time.GetTicksMsec();
                if (now < started) throw new InvalidOperationException("Application quit monotonic clock rolled back.");
                AudioQuitDrainResult result = gate.Observe(new(now - started, Engine.GetProcessFrames(),
                    owner ? AudioServer.GetTimeSinceLastMix() : 0d, owner,
                    owner && AllPlayers().All(player => !GodotObject.IsInstanceValid(player) ||
                        player.Stream == null && !player.Playing),
                    owner && _applicationQuitOwnedIds.All(id => !GodotObject.IsInstanceIdValid(id))));
                if (result.State != AudioQuitDrainState.Waiting)
                {
                    completion.TrySetResult(result);
                    return;
                }
                // A bounded polling yield, not a success timer. Ready still requires
                // an observed mix, later process cleanup, silent voices and released owned IDs.
                await Task.Delay(2);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        catch (Exception error) { completion.TrySetException(error); }
    }

    private static ulong OwnedId(GodotObject resource) =>
        GodotObject.IsInstanceValid(resource) ? resource.GetInstanceId() : 0UL;
}
