using Godot;
using Knight.Prologue;

namespace Knight.Mechanisms;

public partial class CanyonSteamValve : Node2D, IThreadInteractionReceiver, ICheckpointResettable
{
    [Export] public string ActivationId { get; set; } = "canyon.v2.steam-valve-lower";
    [Export] public NodePath PairedJetPath { get; set; } = new("../SteamJet");
    public bool IsActivated { get; private set; }
    private PrologueProgress _progress;

    public void ApplyProgress(PrologueProgress progress)
    {
        _progress = progress;
        IsActivated = progress?.ActivatedMechanisms.Contains(ActivationId) == true;
        CanyonSteamJet jet = PairedJet();
        if (IsActivated) jet?.DisableForRun();
        else jet?.EnableForNewRun();
    }

    public bool TryReceiveThread(ThreadInteraction interaction) => TryActivate();

    // The explicit manual control and the original remote claw share the same
    // persisted state transition, not a fabricated thread interaction.
    public bool TryActivate()
    {
        if (IsActivated) return true;
        IsActivated = true;
        _progress?.ActivateMechanism(ActivationId);
        PairedJet()?.DisableForRun();
        return true;
    }

    public void ResetForCheckpoint()
    {
        IsActivated = _progress?.ActivatedMechanisms.Contains(ActivationId) == true;
        if (IsActivated) PairedJet()?.DisableForRun();
    }

    private CanyonSteamJet PairedJet() => GetNodeOrNull<CanyonSteamJet>(PairedJetPath);
}
