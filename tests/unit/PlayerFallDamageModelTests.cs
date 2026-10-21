using Knight.Player;

namespace Knight.Tests.Unit;

public static class PlayerFallDamageModelTests
{
    public static void Run()
    {
        // Literal, independently derived thresholds: ordinary height, exact safety edge,
        // and just past successive 400-unit bands. A terminal-speed-only formula fails.
        foreach ((float height, int damage) in new (float, int)[]
        {
            (0f, 0), (94f, 0), (436f, 0), (600f, 0), (600.1f, 1),
            (1000f, 1), (1000.1f, 2), (1400f, 2), (1400.1f, 3),
            (1800.1f, 4), (2200.1f, 5), (3200f, 7)
        })
            TestAssert.Equal(damage, PlayerFallDamageModel.CalculateDamage(height),
                $"height {height} resolves its own fall-damage band");
        TestAssert.Equal(2, PlayerFallDamageModel.CalculateDamage(3200f, maximumDamage: 2),
            "damage respects the supplied health capacity without integer overflow");
        var model = new PlayerFallDamageModel();
        model.ResetOrigin(1000f);
        model.SampleHeight(900f);
        model.SampleHeight(1800f);
        TestAssert.Near(900f, model.FallHeight, .01f, "descent is measured from the actual apex");
        TestAssert.Equal(1, model.ConsumeLanding(1800f, false, 5).Damage,
            "a real landing consumes the apex-based injury");
        TestAssert.Equal(0, model.ConsumeLanding(1800f, false, 5).Damage,
            "consumed landing cannot damage again");
        model.ResetOrigin(100f);
        model.SampleHeight(1500f);
        for (int index = 0; index < 10; index++)
        {
            TestAssert.True(!model.ObserveGrappleBraking(1500f, 780f, 20f, -760f,
                    false, false, .02), "an unlatched cast cannot count as braking");
            TestAssert.True(!model.ObserveGrappleBraking(1500f, 780f, 550f, -230f,
                    true, false, .02), "a still-fast attachment cannot count as safe braking");
            TestAssert.True(!model.ObserveGrappleBraking(1500f, 780f, -10f, -790f,
                    true, true, .02), "dash-driven motion does not masquerade as a brake");
        }
        TestAssert.Near(1400f, model.FallHeight, .01f, "failed mitigation retains the prior fall");
        bool arrested = false;
        for (int index = 0; index < 6; index++)
            arrested |= model.ObserveGrappleBraking(1500f, 780f, 100f, -680f, true, false, .02);
        TestAssert.True(arrested, "sustained causal claw braking rearms the next descent");
        TestAssert.Near(0f, model.FallHeight, .01f, "a successful brake consumes only the old fall");
        model.SampleHeight(2200f);
        model.ResetOrigin(2200f);
        model.SampleHeight(2240f);
        TestAssert.Equal(0, model.ConsumeLanding(2240f, false, 5).Damage,
            "explicit relocation/respawn reset does not turn teleport distance into injury");
    }
}
