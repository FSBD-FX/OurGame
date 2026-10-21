namespace Knight.Mechanisms;

public readonly record struct CanyonMechanismFrame(
    bool Telegraphing, bool HazardActive, bool Traversable, float Phase01);
