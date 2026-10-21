namespace Knight.Player;

public sealed class PlayerActionSelectionModel
{
    public PlayerActionSelectionState Step(PlayerActionSelectionState state,
        bool attackModePressed, bool grappleModePressed, int requestedSlot)
    {
        PlayerActionMode mode = grappleModePressed ? PlayerActionMode.Grapple :
            attackModePressed ? PlayerActionMode.Attack : state.Mode;
        int slot = requestedSlot is >= 1 and <= 4 ? requestedSlot : state.ActiveSlot;
        return new PlayerActionSelectionState(mode, slot);
    }
}
