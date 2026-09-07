using System;

namespace Knight.Presentation;

public static class PlayerPoseMixer
{
    public static PlayerPose Mix(PlayerPose locomotion, PlayerPose action, float actionWeight)
    {
        locomotion.Validate();
        action.Validate();
        if (float.IsNaN(actionWeight) || float.IsInfinity(actionWeight))
            throw new ArgumentOutOfRangeException(nameof(actionWeight));

        float weight = Math.Clamp(actionWeight, 0f, 1f);
        PlayerPoseOwnership mixedOwnership = weight <= 0f
            ? locomotion.Ownership
            : locomotion.Ownership | action.Ownership;
        return PlayerPose.FromSlots(
            mixedOwnership,
            MixSlot(PlayerPoseSlot.Body),
            MixSlot(PlayerPoseSlot.NearArm),
            MixSlot(PlayerPoseSlot.FarArm),
            MixSlot(PlayerPoseSlot.NearLeg),
            MixSlot(PlayerPoseSlot.FarLeg),
            MixSlot(PlayerPoseSlot.Weapon),
            MixSlot(PlayerPoseSlot.Mantle),
            MixSlot(PlayerPoseSlot.ShoulderCore),
            MixSlot(PlayerPoseSlot.Lantern),
            MixSlot(PlayerPoseSlot.Tether));

        PoseTransform2D MixSlot(PlayerPoseSlot slot) => action.Owns(slot)
            ? PoseTransform2D.Interpolate(locomotion.Get(slot), action.Get(slot), weight)
            : locomotion.Get(slot);
    }
}
