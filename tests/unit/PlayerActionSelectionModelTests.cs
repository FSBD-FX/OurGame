using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerActionSelectionModelTests
{
    public static void Run()
    {
        var model = new PlayerActionSelectionModel();
        var initial = new PlayerActionSelectionState(PlayerActionMode.Attack, 1);
        TestAssert.Equal(initial, new PlayerActionSelectionState(), "selection defaults to attack slot one");
        TestAssert.Equal(initial, model.Step(initial, false, false, 0), "selection is sticky");
        TestAssert.Equal(PlayerActionMode.Grapple,
            model.Step(initial, false, true, 0).Mode, "E selects grapple mode");
        TestAssert.Equal(PlayerActionMode.Attack,
            model.Step(initial with { Mode = PlayerActionMode.Grapple }, true, false, 0).Mode,
            "Q selects attack mode");
        TestAssert.Equal(4, model.Step(initial, false, false, 4).ActiveSlot, "slot four selects");
        TestAssert.Equal(1, model.Step(initial, false, false, 5).ActiveSlot,
            "out-of-range slot request is ignored");
    }
}
