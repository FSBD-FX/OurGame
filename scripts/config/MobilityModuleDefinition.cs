using System;
using System.Collections.Generic;
using Godot;
using Knight.Player;

namespace Knight.Config;

[Tool, GlobalClass]
public partial class MobilityModuleDefinition : Resource
{
    public string ModuleId { get; set; } = MobilityModuleSettings.Standard.ModuleId;
    public float BurnDuration { get; set; } = MobilityModuleSettings.Standard.BurnDuration;
    public float ThrustAcceleration { get; set; } = MobilityModuleSettings.Standard.ThrustAcceleration;
    public float PoweredProjectionLimit { get; set; } = MobilityModuleSettings.Standard.PoweredProjectionLimit;
    public float GroundFriction { get; set; } = MobilityModuleSettings.Standard.GroundFriction;
    public float AirDragRate { get; set; } = MobilityModuleSettings.Standard.AirDragRate;
    public float GroundReverseBrake { get; set; } = MobilityModuleSettings.Standard.GroundReverseBrake;
    public float AirReverseBrake { get; set; } = MobilityModuleSettings.Standard.AirReverseBrake;

    // Dynamic native storage keeps every authored value, including Standard,
    // explicit on disk. Ordinary export defaults would be omitted by the text
    // saver. These entries are type metadata, not a second parameter authority.
    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        var properties = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (string name in new[] { nameof(ModuleId), nameof(BurnDuration), nameof(ThrustAcceleration),
            nameof(PoweredProjectionLimit), nameof(GroundFriction), nameof(AirDragRate),
            nameof(GroundReverseBrake), nameof(AirReverseBrake) })
            properties.Add(new Godot.Collections.Dictionary
            {
                ["name"] = name,
                ["type"] = (int)(name == nameof(ModuleId) ? Variant.Type.String : Variant.Type.Float),
                ["usage"] = (int)PropertyUsageFlags.Default
            });
        return properties;
    }

    public override Variant _Get(StringName property) => Value(ToSettings(), property);

    public override bool _Set(StringName property, Variant value)
    {
        if (property == nameof(ModuleId)) { ModuleId = value.AsString(); return true; }
        if (property == nameof(BurnDuration)) { BurnDuration = value.AsSingle(); return true; }
        if (property == nameof(ThrustAcceleration)) { ThrustAcceleration = value.AsSingle(); return true; }
        if (property == nameof(PoweredProjectionLimit)) { PoweredProjectionLimit = value.AsSingle(); return true; }
        if (property == nameof(GroundFriction)) { GroundFriction = value.AsSingle(); return true; }
        if (property == nameof(AirDragRate)) { AirDragRate = value.AsSingle(); return true; }
        if (property == nameof(GroundReverseBrake)) { GroundReverseBrake = value.AsSingle(); return true; }
        if (property == nameof(AirReverseBrake)) { AirReverseBrake = value.AsSingle(); return true; }
        return false;
    }

    // Inspector revert remains the real valid Standard value. It does not
    // override the script-default lookup used by ResourceSaver.
    public override bool _PropertyCanRevert(StringName property) => Value(MobilityModuleSettings.Standard, property).VariantType != Variant.Type.Nil;
    public override Variant _PropertyGetRevert(StringName property) => Value(MobilityModuleSettings.Standard, property);

    private static Variant Value(MobilityModuleSettings settings, StringName property)
    {
        if (property == nameof(ModuleId)) return settings.ModuleId;
        if (property == nameof(BurnDuration)) return settings.BurnDuration;
        if (property == nameof(ThrustAcceleration)) return settings.ThrustAcceleration;
        if (property == nameof(PoweredProjectionLimit)) return settings.PoweredProjectionLimit;
        if (property == nameof(GroundFriction)) return settings.GroundFriction;
        if (property == nameof(AirDragRate)) return settings.AirDragRate;
        if (property == nameof(GroundReverseBrake)) return settings.GroundReverseBrake;
        if (property == nameof(AirReverseBrake)) return settings.AirReverseBrake;
        return default;
    }

    public MobilityModuleSettings ToSettings() => new(ModuleId, BurnDuration,
        ThrustAcceleration, PoweredProjectionLimit, GroundFriction, AirDragRate,
        GroundReverseBrake, AirReverseBrake);

    public IReadOnlyList<string> Validate() => Validate(ToSettings());

    public static IReadOnlyList<string> Validate(MobilityModuleSettings settings)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(settings.ModuleId) ||
            !string.Equals(settings.ModuleId, settings.ModuleId.Trim(), StringComparison.Ordinal))
            errors.Add($"{nameof(ModuleId)} must be a nonblank stable identity without surrounding whitespace");
        Positive(errors, nameof(BurnDuration), settings.BurnDuration);
        Positive(errors, nameof(ThrustAcceleration), settings.ThrustAcceleration);
        Positive(errors, nameof(PoweredProjectionLimit), settings.PoweredProjectionLimit);
        NonNegative(errors, nameof(GroundFriction), settings.GroundFriction);
        NonNegative(errors, nameof(AirDragRate), settings.AirDragRate);
        Positive(errors, nameof(GroundReverseBrake), settings.GroundReverseBrake);
        Positive(errors, nameof(AirReverseBrake), settings.AirReverseBrake);
        return errors;
    }

    private static void Positive(ICollection<string> errors, string field, float value)
    {
        if (!float.IsFinite(value) || value <= 0f)
            errors.Add($"{field} must be finite and greater than zero");
    }

    private static void NonNegative(ICollection<string> errors, string field, float value)
    {
        if (!float.IsFinite(value) || value < 0f)
            errors.Add($"{field} must be finite and nonnegative");
    }
}
