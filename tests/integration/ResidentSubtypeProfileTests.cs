using System;
using System.IO;
using System.Linq;
using Godot;
using Knight.Build.Prologue;
using Knight.Enemies.Residents;
using Knight.Prologue.Residents;

namespace Knight.Tests.Integration;

public static class ResidentSubtypeProfileTests
{
    private sealed record Expected(string Id, ResidentFamily Family, ResidentArchetype Role,
        ResidentCarrierKind Carrier, ResidentBoneWeakness Weakness = ResidentBoneWeakness.None,
        float Hidden = 0, float Manifest = 0);

    // Literal identities and policies, not a table calculated from the catalog.
    // Durations are serialization/consumer test values, NOT approved game balance.
    private static readonly Expected[] Cases =
    {
        new("rock-wanderer", ResidentFamily.Native, ResidentArchetype.Wanderer, ResidentCarrierKind.Wanderer),
        new("ridge-charger", ResidentFamily.Native, ResidentArchetype.Charger, ResidentCarrierKind.Charger),
        new("rift-flyer", ResidentFamily.Native, ResidentArchetype.Flyer, ResidentCarrierKind.Air),
        new("cliff-diver", ResidentFamily.Native, ResidentArchetype.Diver, ResidentCarrierKind.Air),
        new("scrap-wanderer", ResidentFamily.Scrap, ResidentArchetype.Wanderer, ResidentCarrierKind.Wanderer),
        new("scrap-charger", ResidentFamily.Scrap, ResidentArchetype.Charger, ResidentCarrierKind.Charger),
        new("scrap-flyer", ResidentFamily.Scrap, ResidentArchetype.Flyer, ResidentCarrierKind.Air),
        new("scrap-diver", ResidentFamily.Scrap, ResidentArchetype.Diver, ResidentCarrierKind.Air),
        new("scrap-scatter", ResidentFamily.Scrap, ResidentArchetype.Turret, ResidentCarrierKind.Scatter),
        new("scrap-heavy", ResidentFamily.Scrap, ResidentArchetype.Heavy, ResidentCarrierKind.Heavy),
        new("precision-burst", ResidentFamily.Precision, ResidentArchetype.Turret, ResidentCarrierKind.Burst),
        new("precision-laser", ResidentFamily.Precision, ResidentArchetype.Turret, ResidentCarrierKind.Laser),
        new("precision-missile", ResidentFamily.Precision, ResidentArchetype.Turret, ResidentCarrierKind.Missile),
        new("precision-flyer", ResidentFamily.Precision, ResidentArchetype.Flyer, ResidentCarrierKind.Air),
        new("precision-charger", ResidentFamily.Precision, ResidentArchetype.Charger, ResidentCarrierKind.Charger),
        new("precision-heavy", ResidentFamily.Precision, ResidentArchetype.Heavy, ResidentCarrierKind.Heavy),
        new("bone-wanderer", ResidentFamily.Bone, ResidentArchetype.Wanderer, ResidentCarrierKind.Wanderer, ResidentBoneWeakness.RecoveryOrThreadStunned),
        new("bone-charger", ResidentFamily.Bone, ResidentArchetype.Charger, ResidentCarrierKind.Charger, ResidentBoneWeakness.RecoveryOrThreadExposed),
        new("bone-diver", ResidentFamily.Bone, ResidentArchetype.Diver, ResidentCarrierKind.Air, ResidentBoneWeakness.RecoveryOrThreadStunned),
        new("bone-heavy", ResidentFamily.Bone, ResidentArchetype.Heavy, ResidentCarrierKind.Heavy, ResidentBoneWeakness.RearOrExposed),
        new("lost-echo", ResidentFamily.Ghost, ResidentArchetype.Wanderer, ResidentCarrierKind.Wanderer, Hidden: .6f, Manifest: .8f),
        new("bound-ghost", ResidentFamily.Ghost, ResidentArchetype.Flyer, ResidentCarrierKind.Air, Hidden: .7f, Manifest: 2.4f),
        new("ghost-diver", ResidentFamily.Ghost, ResidentArchetype.Diver, ResidentCarrierKind.Air, Hidden: .9f, Manifest: 2.8f),
        new("ghost-tower", ResidentFamily.Ghost, ResidentArchetype.Turret, ResidentCarrierKind.Sentry, Hidden: 1.2f, Manifest: 4f)
    };

    public static void Run(string admittedStorage)
    {
        string output = Path.Combine(admittedStorage, "resident-subtype-profiles");
        TestAssert.True(!Directory.Exists(output), "profile proof output must be fresh isolated storage");
        Directory.CreateDirectory(output);
        foreach (Expected want in Cases)
        {
            using var source = Make(want);
            string path = Path.Combine(output, want.Id + ".tres");
            TestAssert.Equal(Error.Ok, ResourceSaver.Save(source, path), "save actual subtype resource " + want.Id);
            using var saved = ResourceLoader.Load<ResidentSubtypeProfile>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            TestAssert.True(saved != null, "reload actual saved subtype resource " + want.Id);
            TestAssert.Equal(want.Id, saved.SubtypeId, "serialized subtype identity survives disk round-trip");
            TestAssert.Equal(want.Family, saved.Family, "serialized family survives disk round-trip");
            TestAssert.Equal(want.Role, saved.Archetype, "serialized resident role survives disk round-trip");
            TestAssert.Equal(want.Carrier, saved.Carrier, "serialized weapon/body carrier survives disk round-trip");
            TestAssert.Equal(want.Weakness, saved.BoneWeakness, "serialized Bone policy survives disk round-trip");
            TestAssert.Equal(want.Hidden, saved.GhostHiddenSeconds, "serialized Ghost hidden duration survives disk round-trip");
            TestAssert.Equal(want.Manifest, saved.GhostManifestSeconds, "serialized Ghost manifest duration survives disk round-trip");
        }
        GD.Print("RESIDENT_SUBTYPE_ROUNDTRIP_OK profiles=24 disk_reload=true formal_scenes=0");

        foreach (Expected want in Cases)
        {
            using var saved = ResourceLoader.Load<ResidentSubtypeProfile>(Path.Combine(output, want.Id + ".tres"),
                cacheMode: ResourceLoader.CacheMode.Ignore);
            ResidentSubtypeDefinition type = CanyonResidentCatalog.Subtypes.Single(x => x.Id == want.Id);
            ResidentPopulationDefinition slot = Slot(want.Id);
            TestAssert.Equal(want.Carrier, type.Carrier, "catalog independently binds carrier " + want.Id);
            ResidentPopulationDefinition bound = saved.Bind(type, slot);
            TestAssert.Equal(slot.StableId, bound.StableId, "resource does not replace stable slot identity");
            TestAssert.True(ReferenceEquals(slot.Placement, bound.Placement), "resource does not invent or replace placement");
            ResidentCombatProfile expected = want.Family is ResidentFamily.Bone or ResidentFamily.Ghost
                ? new(want.Weakness, want.Hidden, want.Manifest) : null;
            TestAssert.Equal(expected, bound.CombatProfile, "reloaded resource supplies actual immutable family policy " + want.Id);
            saved.GhostManifestSeconds = 59f;
            TestAssert.Equal(expected, bound.CombatProfile, "binding snapshots mutable resource before native actor consumption");
        }
        GD.Print("RESIDENT_SUBTYPE_BIND_OK profiles=24 immutable_snapshot=true placement_invented=false");

        foreach (Expected want in Cases)
        {
            using var saved = ResourceLoader.Load<ResidentSubtypeProfile>(Path.Combine(output, want.Id + ".tres"),
                cacheMode: ResourceLoader.CacheMode.Ignore);
            ResidentSubtypeDefinition type = CanyonResidentCatalog.Subtypes.Single(x => x.Id == want.Id);
            Node actor = GD.Load<PackedScene>(type.ProbeScenePath).Instantiate();
            try
            {
                saved.ConfigureActor(actor, type, Slot(want.Id));
                // Real actor API proves the first configuration was committed;
                // no reflection/private-state/model injection, no mounting claim.
                Reject<InvalidOperationException>(() => ((IResidentEnemyAgent)actor).ConfigureResidence(saved.Bind(type, Slot(want.Id))),
                    "loaded profile configures the real saved actor exactly once before Ready " + want.Id);
            }
            finally { actor.Free(); }
        }
        GD.Print("RESIDENT_SUBTYPE_CONSUMER_OK profiles=24 carriers=9 saved_actors=true mounted=false");
        RunNegatives();
        GD.Print("RESIDENT_SUBTYPE_PROFILE_OK profiles=24 actual_files=true formal_scenes=0 spawned=0 native_motion=false");
    }

    private static void RunNegatives()
    {
        Expected ghost = Cases.Single(x => x.Id == "lost-echo");
        using var profile = Make(ghost);
        ResidentSubtypeDefinition type = CanyonResidentCatalog.Subtypes.Single(x => x.Id == ghost.Id);
        ResidentPopulationDefinition slot = Slot(ghost.Id);
        int count = 0;
        Reject<ArgumentException>(() => profile.Bind(type, slot with { SubtypeId = "bound-ghost" }), "reject wrong slot subtype"); count++;
        Reject<ArgumentException>(() => profile.Bind(type, slot with { Family = ResidentFamily.Bone }), "reject wrong slot family"); count++;
        Reject<ArgumentException>(() => profile.Bind(type, slot with { Archetype = ResidentArchetype.Flyer }), "reject wrong slot role"); count++;
        Reject<ArgumentException>(() => profile.Bind(type with { Carrier = ResidentCarrierKind.Laser }, slot), "reject wrong catalog carrier"); count++;
        Reject<ArgumentException>(() => profile.Bind(type, slot with { Placement = null }), "reject unplaced slot, never origin fallback"); count++;
        Reject<ArgumentException>(() => profile.Bind(type, slot with { CombatProfile = new(GhostHiddenSeconds: 8, GhostManifestSeconds: 9) }), "reject conflicting caller policy"); count++;
        profile.GhostManifestSeconds = float.NaN;
        Reject<ArgumentException>(() => profile.Bind(type, slot), "reject nonfinite manifest"); count++;
        profile.GhostManifestSeconds = 0;
        Reject<ArgumentException>(() => profile.Bind(type, slot), "reject zero manifest"); count++;
        profile.GhostManifestSeconds = .8f; profile.BoneWeakness = ResidentBoneWeakness.RearOrExposed;
        Reject<ArgumentException>(() => profile.Bind(type, slot), "reject mixed Bone and Ghost policy"); count++;
        profile.BoneWeakness = ResidentBoneWeakness.None; profile.Family = (ResidentFamily)999;
        Reject<ArgumentException>(() => profile.Bind(type, slot), "reject undefined family"); count++;
        profile.Family = ResidentFamily.Ghost;
        Node wrong = GD.Load<PackedScene>(CanyonEncounterCatalog.LaserScene).Instantiate();
        try { Reject<ArgumentException>(() => profile.ConfigureActor(wrong, type, slot), "reject actual wrong actor before configuring it"); count++; }
        finally { wrong.Free(); }
        Expected laser = Cases.Single(x => x.Id == "precision-laser");
        using var laserProfile = Make(laser);
        ResidentSubtypeDefinition laserType = CanyonResidentCatalog.Subtypes.Single(x => x.Id == laser.Id);
        Node scatter = GD.Load<PackedScene>(CanyonEncounterCatalog.ScatterScene).Instantiate();
        try
        {
            Reject<ArgumentException>(() => laserProfile.ConfigureActor(scatter, laserType, Slot(laser.Id)), "same turret role cannot disguise wrong real weapon"); count++;
            using var scatterProfile = Make(Cases.Single(x => x.Id == "scrap-scatter"));
            scatterProfile.ConfigureActor(scatter, CanyonResidentCatalog.Subtypes.Single(x => x.Id == "scrap-scatter"), Slot("scrap-scatter"));
            Reject<InvalidOperationException>(() => ((IResidentEnemyAgent)scatter).ConfigureResidence(Slot("scrap-scatter")), "wrong-weapon rejection leaves actor available for one valid bind");
        }
        finally { scatter.Free(); }
        using var bone = Make(Cases.Single(x => x.Id == "bone-charger"));
        bone.BoneWeakness = ResidentBoneWeakness.RecoveryOrThreadStunned;
        Reject<ArgumentException>(() => bone.Bind(CanyonResidentCatalog.Subtypes.Single(x => x.Id == "bone-charger"), Slot("bone-charger")), "reject wrong Bone posture contract"); count++;
        using var ordinary = Make(Cases[0]); ordinary.GhostHiddenSeconds = .5f; ordinary.GhostManifestSeconds = .8f;
        Reject<ArgumentException>(() => ordinary.Bind(CanyonResidentCatalog.Subtypes.Single(x => x.Id == "rock-wanderer"), Slot("rock-wanderer")), "ordinary family cannot silently ignore Ghost fields"); count++;
        GD.Print($"RESIDENT_SUBTYPE_NEGATIVES_OK cases={count} same_consumer=true");
    }

    private static ResidentSubtypeProfile Make(Expected x) => new()
    {
        SubtypeId = x.Id, Family = x.Family, Archetype = x.Role, Carrier = x.Carrier,
        BoneWeakness = x.Weakness, GhostHiddenSeconds = x.Hidden, GhostManifestSeconds = x.Manifest
    };
    private static ResidentPopulationDefinition Slot(string id)
    {
        ResidentPopulationDefinition slot = CanyonResidentCatalog.All.First(x => x.SubtypeId == id);
        Vector2[] points = slot.Archetype == ResidentArchetype.Turret ? new[] { new Vector2(320, 300) }
            : new[] { new Vector2(240, 300), new Vector2(400, slot.Archetype is ResidentArchetype.Flyer or ResidentArchetype.Diver ? 260 : 300) };
        return slot with { Placement = new(new Vector2(320, 300), new Rect2(100, 100, 600, 400), points) };
    }
    private static void Reject<T>(Action action, string label) where T : Exception
    {
        bool rejected = false;
        try { action(); } catch (T) { rejected = true; }
        TestAssert.True(rejected, label);
    }
}
