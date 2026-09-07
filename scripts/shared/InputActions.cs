using Godot;

namespace Knight.Shared;

public static class InputActions
{
    public static readonly StringName MoveLeft = "move_left";
    public static readonly StringName MoveRight = "move_right";
    public static readonly StringName MoveUp = "move_up";
    public static readonly StringName MoveDown = "move_down";
    public static readonly StringName Jump = "jump";
    public static readonly StringName Dash = "dash";
    // Compatibility identifiers for isolated legacy fixtures only. ConfigureProject removes these
    // actions and PlayerInputReader never samples them in production.
    public static readonly StringName Attack = "attack";
    public static readonly StringName Thread = "thread";
    public static readonly StringName SelectAttackMode = "select_attack_mode";
    public static readonly StringName SelectGrappleMode = "select_grapple_mode";
    public static readonly StringName PrimaryAction = "primary_action";
    public static readonly StringName Shield = "shield";
    public static readonly StringName Slot1 = "slot_1";
    public static readonly StringName Slot2 = "slot_2";
    public static readonly StringName Slot3 = "slot_3";
    public static readonly StringName Slot4 = "slot_4";
    public static readonly StringName Ember = "ember";
    public static readonly StringName EchoRecord = "echo_record";
    public static readonly StringName EchoReplay = "echo_replay";
    public static readonly StringName Pause = "pause";
}
