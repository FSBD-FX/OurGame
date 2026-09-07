namespace Knight.Abilities;

public readonly record struct MechanicalClawInput(
    bool PrimaryPressed,
    bool PrimaryHeld,
    bool PrimaryReleased,
    bool DetachPressed,
    float SwingAxis,
    float ReelAxis,
    bool ShieldActive,
    bool BoostPressed = false,
    bool Grounded = false);
