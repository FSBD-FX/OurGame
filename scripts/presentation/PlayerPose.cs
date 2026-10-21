using System;

namespace Knight.Presentation;

public enum PlayerPoseSlot
{
    Body,
    NearArm,
    FarArm,
    NearLeg,
    FarLeg,
    Weapon,
    Mantle,
    ShoulderCore,
    Lantern,
    Tether
}

[Flags]
public enum PlayerPoseOwnership : ushort
{
    None = 0,
    Body = 1 << (int)PlayerPoseSlot.Body,
    NearArm = 1 << (int)PlayerPoseSlot.NearArm,
    FarArm = 1 << (int)PlayerPoseSlot.FarArm,
    NearLeg = 1 << (int)PlayerPoseSlot.NearLeg,
    FarLeg = 1 << (int)PlayerPoseSlot.FarLeg,
    Weapon = 1 << (int)PlayerPoseSlot.Weapon,
    Mantle = 1 << (int)PlayerPoseSlot.Mantle,
    ShoulderCore = 1 << (int)PlayerPoseSlot.ShoulderCore,
    Lantern = 1 << (int)PlayerPoseSlot.Lantern,
    Tether = 1 << (int)PlayerPoseSlot.Tether,
    All = Body | NearArm | FarArm | NearLeg | FarLeg | Weapon |
        Mantle | ShoulderCore | Lantern | Tether
}

public readonly record struct PlayerPose
{
    public static PlayerPose Identity { get; } = new(
        PlayerPoseOwnership.None,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity,
        PoseTransform2D.Identity);

    public PlayerPoseOwnership Ownership { get; }
    public PoseTransform2D Body { get; }
    public PoseTransform2D NearArm { get; }
    public PoseTransform2D FarArm { get; }
    public PoseTransform2D NearLeg { get; }
    public PoseTransform2D FarLeg { get; }
    public PoseTransform2D Weapon { get; }
    public PoseTransform2D Mantle { get; }
    public PoseTransform2D ShoulderCore { get; }
    public PoseTransform2D Lantern { get; }
    public PoseTransform2D Tether { get; }

    private PlayerPose(
        PlayerPoseOwnership ownership,
        PoseTransform2D body,
        PoseTransform2D nearArm,
        PoseTransform2D farArm,
        PoseTransform2D nearLeg,
        PoseTransform2D farLeg,
        PoseTransform2D weapon,
        PoseTransform2D mantle,
        PoseTransform2D shoulderCore,
        PoseTransform2D lantern,
        PoseTransform2D tether)
    {
        Ownership = ownership;
        Body = body;
        NearArm = nearArm;
        FarArm = farArm;
        NearLeg = nearLeg;
        FarLeg = farLeg;
        Weapon = weapon;
        Mantle = mantle;
        ShoulderCore = shoulderCore;
        Lantern = lantern;
        Tether = tether;
    }

    public bool Owns(PlayerPoseSlot slot) =>
        (Ownership & OwnershipFor(slot)) != PlayerPoseOwnership.None;

    public PoseTransform2D Get(PlayerPoseSlot slot) => slot switch
    {
        PlayerPoseSlot.Body => Body,
        PlayerPoseSlot.NearArm => NearArm,
        PlayerPoseSlot.FarArm => FarArm,
        PlayerPoseSlot.NearLeg => NearLeg,
        PlayerPoseSlot.FarLeg => FarLeg,
        PlayerPoseSlot.Weapon => Weapon,
        PlayerPoseSlot.Mantle => Mantle,
        PlayerPoseSlot.ShoulderCore => ShoulderCore,
        PlayerPoseSlot.Lantern => Lantern,
        PlayerPoseSlot.Tether => Tether,
        _ => throw new ArgumentOutOfRangeException(nameof(slot))
    };

    public PlayerPose With(PlayerPoseSlot slot, PoseTransform2D transform) =>
        Replace(slot, transform.Validate(), Ownership);

    public PlayerPose WithOwned(PlayerPoseSlot slot, PoseTransform2D transform) =>
        Replace(slot, transform.Validate(), Ownership | OwnershipFor(slot));

    public PlayerPose WithOwnership(PlayerPoseOwnership ownership)
    {
        if ((ownership & ~PlayerPoseOwnership.All) != 0)
            throw new ArgumentOutOfRangeException(nameof(ownership));
        return new PlayerPose(ownership, Body, NearArm, FarArm, NearLeg, FarLeg,
            Weapon, Mantle, ShoulderCore, Lantern, Tether);
    }

    public PlayerPose Validate()
    {
        WithOwnership(Ownership);
        Body.Validate();
        NearArm.Validate();
        FarArm.Validate();
        NearLeg.Validate();
        FarLeg.Validate();
        Weapon.Validate();
        Mantle.Validate();
        ShoulderCore.Validate();
        Lantern.Validate();
        Tether.Validate();
        return this;
    }

    internal static PlayerPose FromSlots(
        PlayerPoseOwnership ownership,
        PoseTransform2D body,
        PoseTransform2D nearArm,
        PoseTransform2D farArm,
        PoseTransform2D nearLeg,
        PoseTransform2D farLeg,
        PoseTransform2D weapon,
        PoseTransform2D mantle,
        PoseTransform2D shoulderCore,
        PoseTransform2D lantern,
        PoseTransform2D tether) =>
        new(ownership, body, nearArm, farArm, nearLeg, farLeg, weapon, mantle,
            shoulderCore, lantern, tether);

    private PlayerPose Replace(
        PlayerPoseSlot slot,
        PoseTransform2D transform,
        PlayerPoseOwnership ownership) => slot switch
    {
        PlayerPoseSlot.Body => new(ownership, transform, NearArm, FarArm, NearLeg, FarLeg,
            Weapon, Mantle, ShoulderCore, Lantern, Tether),
        PlayerPoseSlot.NearArm => new(ownership, Body, transform, FarArm, NearLeg, FarLeg,
            Weapon, Mantle, ShoulderCore, Lantern, Tether),
        PlayerPoseSlot.FarArm => new(ownership, Body, NearArm, transform, NearLeg, FarLeg,
            Weapon, Mantle, ShoulderCore, Lantern, Tether),
        PlayerPoseSlot.NearLeg => new(ownership, Body, NearArm, FarArm, transform, FarLeg,
            Weapon, Mantle, ShoulderCore, Lantern, Tether),
        PlayerPoseSlot.FarLeg => new(ownership, Body, NearArm, FarArm, NearLeg, transform,
            Weapon, Mantle, ShoulderCore, Lantern, Tether),
        PlayerPoseSlot.Weapon => new(ownership, Body, NearArm, FarArm, NearLeg, FarLeg,
            transform, Mantle, ShoulderCore, Lantern, Tether),
        PlayerPoseSlot.Mantle => new(ownership, Body, NearArm, FarArm, NearLeg, FarLeg,
            Weapon, transform, ShoulderCore, Lantern, Tether),
        PlayerPoseSlot.ShoulderCore => new(ownership, Body, NearArm, FarArm, NearLeg, FarLeg,
            Weapon, Mantle, transform, Lantern, Tether),
        PlayerPoseSlot.Lantern => new(ownership, Body, NearArm, FarArm, NearLeg, FarLeg,
            Weapon, Mantle, ShoulderCore, transform, Tether),
        PlayerPoseSlot.Tether => new(ownership, Body, NearArm, FarArm, NearLeg, FarLeg,
            Weapon, Mantle, ShoulderCore, Lantern, transform),
        _ => throw new ArgumentOutOfRangeException(nameof(slot))
    };

    private static PlayerPoseOwnership OwnershipFor(PlayerPoseSlot slot)
    {
        if (!Enum.IsDefined(slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        return (PlayerPoseOwnership)(1 << (int)slot);
    }
}
