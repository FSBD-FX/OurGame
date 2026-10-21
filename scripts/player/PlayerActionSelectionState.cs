namespace Knight.Player;

public readonly record struct PlayerActionSelectionState(PlayerActionMode Mode, int ActiveSlot)
{
    public PlayerActionSelectionState() : this(PlayerActionMode.Attack, 1) { }
}
