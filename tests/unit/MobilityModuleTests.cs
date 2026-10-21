using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using Knight.Config;

namespace Knight.Tests.Unit;

public static class MobilityModuleTests
{
    // This is a runtime component boundary, not a source-text test. Before the
    // module exists it must produce a behavioural assertion, not a compile error.
    public static void Run()
    {
        Type type = typeof(PlayerTuning).Assembly.GetType("Knight.Config.MobilityModuleDefinition");
        TestAssert.True(type != null, "Mobility module definition is unavailable");
        TestAssert.True(typeof(Resource).IsAssignableFrom(type), "Mobility module is a real Resource");
        SnapshotDoesNotHotReload(type);
        StandardProfileIsValid(type);
        InvalidFieldsAreRejected(type);
        ZeroPassiveDragIsValid(type);
        NativeStorageAndRevertRemainValid();
        GD.Print("THRUSTER_MODULE_CASES_OK snapshot_immutable=true invalid_values_rejected=true owned_resources_disposed=true player_wired=false");
    }

    private static void SnapshotDoesNotHotReload(Type type)
    {
        using Resource definition = Create(type);
        var values = new Dictionary<string, object>
        {
            ["ModuleId"] = "mobility.short-jet.test.v1",
            ["BurnDuration"] = .23f,
            ["ThrustAcceleration"] = 4321f,
            ["PoweredProjectionLimit"] = 765f,
            ["GroundFriction"] = 1234f,
            ["AirDragRate"] = 2.3f,
            ["GroundReverseBrake"] = 3456f,
            ["AirReverseBrake"] = 2345f
        };
        foreach (var pair in values) Set(definition, pair.Key, pair.Value);
        object first = Snapshot(definition);
        TestAssert.Equal(0, Validate(definition).Count, "nondefault profile is accepted");
        foreach (var pair in values)
            TestAssert.Equal(pair.Value, Get(first, pair.Key), "snapshot consumes " + pair.Key);
        Set(definition, "ModuleId", "mobility.short-jet.edited.v2");
        Set(definition, "ThrustAcceleration", 9876f);
        Set(definition, "AirDragRate", .6f);
        object second = Snapshot(definition);
        TestAssert.Equal("mobility.short-jet.test.v1", Get(first, "ModuleId"), "editing Resource keeps old identity");
        TestAssert.Equal(4321f, Get(first, "ThrustAcceleration"), "editing Resource does not hot-reload old thrust");
        TestAssert.Equal(2.3f, Get(first, "AirDragRate"), "editing Resource does not hot-reload old drag");
        TestAssert.Equal(9876f, Get(second, "ThrustAcceleration"), "explicit new snapshot sees edited thrust");
        TestAssert.Equal(.6f, Get(second, "AirDragRate"), "explicit new snapshot sees edited drag");
    }

    private static void StandardProfileIsValid(Type type)
    {
        using Resource definition = Create(type);
        TestAssert.Equal(0, Validate(definition).Count, "standard module can be installed after validation");
        object snapshot = Snapshot(definition);
        MethodInfo validate = type.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static,
            null, new[] { snapshot.GetType() }, null);
        TestAssert.True(validate != null, "immutable snapshot has the same validation boundary");
        var errors = (IReadOnlyList<string>)validate.Invoke(null, new[] { snapshot });
        TestAssert.Equal(0, errors.Count, "standard immutable snapshot passes validation");
    }

    private static void InvalidFieldsAreRejected(Type type)
    {
        string[] positive = { "BurnDuration", "ThrustAcceleration", "PoweredProjectionLimit",
            "GroundReverseBrake", "AirReverseBrake" };
        foreach (string field in positive)
            foreach (float bad in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Rejects(type, field, bad);
        foreach (string field in new[] { "GroundFriction", "AirDragRate" })
            foreach (float bad in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Rejects(type, field, bad);
        foreach (string bad in new[] { null, "", " ", " mobility.short-jet.v1", "mobility.short-jet.v1 " })
            Rejects(type, "ModuleId", bad);
    }

    private static void Rejects(Type type, string field, object bad)
    {
        using Resource definition = Create(type);
        Set(definition, field, bad);
        IReadOnlyList<string> errors = Validate(definition);
        TestAssert.True(errors.Count > 0, "invalid " + field + " cannot enter a motion module");
        bool named = false;
        foreach (string error in errors) named |= error.Contains(field, StringComparison.Ordinal);
        TestAssert.True(named, "invalid module diagnostic identifies " + field);
        object stillInvalid = Get(definition, field);
        TestAssert.True(Equals(bad, stillInvalid), "validation must not silently repair " + field);
    }

    private static void ZeroPassiveDragIsValid(Type type)
    {
        using Resource definition = Create(type);
        Set(definition, "GroundFriction", 0f);
        Set(definition, "AirDragRate", 0f);
        TestAssert.Equal(0, Validate(definition).Count, "zero passive drag supports independent thrust controls");
    }

    private static Resource Create(Type type) => (Resource)Activator.CreateInstance(type);
    private static void NativeStorageAndRevertRemainValid()
    {
        using var module = new MobilityModuleDefinition();
        Godot.Collections.Array<Godot.Collections.Dictionary> properties = module.GetPropertyList();
        foreach (string field in new[] { "ModuleId", "BurnDuration", "ThrustAcceleration", "PoweredProjectionLimit",
            "GroundFriction", "AirDragRate", "GroundReverseBrake", "AirReverseBrake" })
        {
            int stored = 0;
            foreach (Godot.Collections.Dictionary property in properties)
                if (property["name"].AsString() == field &&
                    (((PropertyUsageFlags)property["usage"].AsInt64()) & PropertyUsageFlags.Storage) != 0) stored++;
            TestAssert.Equal(1, stored, "native Resource has exactly one storage property for " + field);
            TestAssert.True(module.PropertyCanRevert(field), "editor revert is available for " + field);
            if (field == "ModuleId")
                TestAssert.Equal(module.Get(field).AsString(), module.PropertyGetRevert(field).AsString(),
                    "editor identity revert is real Standard");
            else TestAssert.Equal(module.Get(field).AsSingle(), module.PropertyGetRevert(field).AsSingle(),
                "editor numeric revert is valid Standard, not an invented invalid saver default: " + field);
        }
        module.Set("ThrustAcceleration", 4321f);
        TestAssert.Equal(4321f, module.ThrustAcceleration, "native setter and C# author property share one authority");
        TestAssert.Equal(5600f, module.PropertyGetRevert("ThrustAcceleration").AsSingle(), "edited thrust still reverts to independent Standard literal");
        using var clone = (MobilityModuleDefinition)module.Duplicate(true);
        TestAssert.Equal(module.ToSettings(), clone.ToSettings(), "native duplicate retains all eight actual values");
        GD.Print("THRUSTER_MODULE_STORAGE_OK native_fields=8 single_storage_authority=true valid_editor_revert=true duplicate_values=true");
    }
    private static PropertyInfo Property(object value, string field) =>
        value.GetType().GetProperty(field) ?? throw new InvalidOperationException("Missing module field " + field);
    private static void Set(object value, string field, object data) => Property(value, field).SetValue(value, data);
    private static object Get(object value, string field) => Property(value, field).GetValue(value);
    private static object Snapshot(Resource definition) =>
        definition.GetType().GetMethod("ToSettings", Type.EmptyTypes)?.Invoke(definition, null)
        ?? throw new InvalidOperationException("Module cannot produce immutable settings");
    private static IReadOnlyList<string> Validate(Resource definition) =>
        (IReadOnlyList<string>)(definition.GetType().GetMethod("Validate", Type.EmptyTypes)?.Invoke(definition, null)
        ?? throw new InvalidOperationException("Module validation is unavailable"));
}
