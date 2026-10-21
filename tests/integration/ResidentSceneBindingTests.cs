using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Build.Prologue;
using Knight.Combat;
using Knight.Enemies.Moth;
using Knight.Enemies.Residents;
using Knight.Prologue.Residents;
using Knight.Tools;

namespace Knight.Tests.Integration;

public static class ResidentSceneBindingTests
{
    public static async Task Run(SceneTree tree, string admittedStorage)
    {
        CheckOwnedPackRetiresCapturedShapes(admittedStorage);
        string leaves = BuildCandidatePackage(admittedStorage);
        string profiles = Path.Combine(admittedStorage, "leaf-source-profiles");
        await CheckRelocatedPackage(tree, admittedStorage, leaves, profiles);
        await CheckSavedLeaves(tree, leaves);
    }

    // Test-only explicit inputs; never a production balance or publication.
    public static string BuildCandidatePackage(string admittedStorage)
    {
        string profiles = Path.Combine(admittedStorage, "leaf-source-profiles");
        string leaves = Path.Combine(admittedStorage, "resident-leaves");
        TestAssert.True(!Directory.Exists(profiles) && !Directory.Exists(leaves), "saved-leaf proof has fresh isolated outputs");
        Directory.CreateDirectory(profiles); Directory.CreateDirectory(leaves);
        foreach (ResidentSubtypeDefinition type in CanyonResidentCatalog.Subtypes)
        {
            // Explicit fixture input, not formal game balance or generated art.
            using var profile = new ResidentSubtypeProfile
            {
                SubtypeId = type.Id, Family = type.Family, Archetype = type.Archetype, Carrier = type.Carrier,
                BoneWeakness = type.Family != ResidentFamily.Bone ? ResidentBoneWeakness.None : type.Archetype switch
                {
                    ResidentArchetype.Charger => ResidentBoneWeakness.RecoveryOrThreadExposed,
                    ResidentArchetype.Heavy => ResidentBoneWeakness.RearOrExposed,
                    _ => ResidentBoneWeakness.RecoveryOrThreadStunned
                },
                GhostHiddenSeconds = type.Family == ResidentFamily.Ghost ? 1.2f : 0,
                GhostManifestSeconds = type.Family == ResidentFamily.Ghost ? 4f : 0
            };
            TestAssert.Equal(Error.Ok, ResourceSaver.Save(profile, Path.Combine(profiles, type.Id + ".tres")), "save explicit builder input");
        }
        BuildResidentSubtypeScenes.BuildInto(leaves, profiles);
        TestAssert.Equal(24, Directory.GetFiles(leaves, "*.tscn").Length, "builder produces every actual subtype leaf");
        return leaves;
    }

    private static async Task CheckSavedLeaves(SceneTree tree, string leaves)
    {
        foreach (ResidentSubtypeDefinition type in CanyonResidentCatalog.Subtypes)
        {
            string path = Path.Combine(leaves, type.Id + ".tscn");
            PackedScene saved = GD.Load<PackedScene>(path);
            Node copy = saved.Instantiate();
            try
            {
                ResidentSceneBinding binding = copy.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
                TestAssert.True(binding?.Profile != null, "actual saved leaf retains exported profile " + type.Id);
                TestAssert.Equal(type.Id, binding.Profile.SubtypeId, "leaf binds intended real subtype");
                TestAssert.Equal(ResidentSceneState.ExplicitPlaceholder, binding.PresentationState, "old artwork never becomes formal through identity metadata");
                Node original = GD.Load<PackedScene>(type.ProbeScenePath).Instantiate();
                try { TestAssert.Equal(original.GetNode("VisualRoot").GetChildCount(), copy.GetNode("VisualRoot").GetChildCount(), "leaf does not add a second renderer"); }
                finally { original.Free(); }
            }
            finally { copy.Free(); }
        }
        GD.Print("RESIDENT_LEAF_ROUNDTRIP_OK scenes=24 exported_profiles=24 added_renderers=0 publication=false");

        foreach (ResidentSubtypeDefinition type in CanyonResidentCatalog.Subtypes)
        {
            string path = Path.Combine(leaves, type.Id + ".tscn");
            PackedScene saved = GD.Load<PackedScene>(path);
            ResidentSubtypeDefinition expected = type with { SceneState = ResidentSceneState.ExplicitPlaceholder, ScenePath = path };
            ResidentPopulationDefinition slot = Slot(type.Id);
            CharacterBody2D actor = ResidentSceneBinding.Instantiate(saved, expected, slot, true);
            try
            {
                bool configured = false;
                try { ((IResidentEnemyAgent)actor).ConfigureResidence(actor.GetNode<ResidentSceneBinding>("ResidentBinding").Profile.Bind(type, slot)); }
                catch (InvalidOperationException) { configured = true; }
                TestAssert.True(configured, "saved leaf loader commits identity before Ready " + type.Id);
                await MountAndCheck(tree, actor, type);
            }
            finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
        }
        GD.Print("RESIDENT_LEAF_MOUNT_OK scenes=24 carriers=9 actual_ready=true stable_ids=fixture_only full_motion=false");
        RunNegatives(leaves);
        GD.Print("RESIDENT_SCENE_BINDING_OK scenes=24 mounted=24 map_spawned=0 formal_visuals=0 full_population=false");
    }

    // The caller releases only its OWN circle and node graph. A private packed
    // scene retained by the helper must not keep that native circle alive.
    // No cache changes, GC, delay, borrowed Dispose or production-only test hook.
    private static void CheckOwnedPackRetiresCapturedShapes(string storage)
    {
        bool savedReleased = Check(false), rejectedReleased = Check(true);
        TestAssert.True(savedReleased && rejectedReleased,
            $"save helper retires its captured caller-owned shapes before return; success={savedReleased}, rejected={rejectedReleased}");
        GD.Print("RESIDENT_PACK_OWNERSHIP_OK cases=2 native_scalar_ids=true caller_owned_shapes_only=true cache_or_gc_changed=false all_resource_leaks_fixed=false");

        bool Check(bool rejected)
        {
            var shape = new CircleShape2D { Radius = 9 };
            ulong shapeId = shape.GetInstanceId();
            var root = new StaticBody2D { Name = "OwnedPackShapeRoot" };
            string path = Path.Combine(storage, rejected ? "owned-pack-rejected.tscn" : "owned-pack-saved.tscn");
            try
            {
                root.AddChild(new CollisionShape2D { Name = "Shape", Shape = shape });
                if (rejected)
                {
                    string nestedPath = Path.Combine(storage, "owned-pack-nested.tscn");
                    var nestedRoot = new Node { Name = "NestedRoot" };
                    using var nestedPack = new PackedScene();
                    try
                    {
                        TestAssert.Equal(Error.Ok, nestedPack.Pack(nestedRoot), "owned nested control packs");
                        TestAssert.Equal(Error.Ok, ResourceSaver.Save(nestedPack, nestedPath), "owned nested control saves");
                    }
                    finally { nestedRoot.Free(); }
                    using var nestedSaved = ResourceLoader.Load<PackedScene>(nestedPath, cacheMode: ResourceLoader.CacheMode.Ignore);
                    Node nested = nestedSaved.Instantiate();
                    TestAssert.True(!string.IsNullOrEmpty(nested.SceneFilePath), "rejection control uses a real saved nested instance");
                    nested.AddChild(new Node { Name = "UnownedDescendant" }); root.AddChild(nested);
                }
                Error actual = SceneBuildUtil.PackAndSave(root, path);
                TestAssert.Equal(rejected ? Error.InvalidData : Error.Ok, actual, "real helper preserves success/rejection behavior");
                TestAssert.Equal(!rejected, File.Exists(path), "rejected helper does not write an incomplete scene");
            }
            finally { root.Free(); shape.Dispose(); }
            return !GodotObject.IsInstanceIdValid(shapeId);
        }
    }

    // Catches a builder retaining absolute case-input/output references: an
    // unchanged byte copy must bind profiles in its NEW folder, not the donor.
    private static async Task CheckRelocatedPackage(SceneTree tree, string storage, string leaves, string inputs)
    {
        TestAssert.Equal(24, Directory.GetFiles(leaves, "*.tres").Length,
            "portable resident package includes its explicit authoring profiles");
        string relocated = Path.Combine(storage, "relocated-resident-package");
        TestAssert.True(!Directory.Exists(relocated), "relocation destination is fresh");
        Directory.CreateDirectory(relocated);
        foreach (ResidentSubtypeDefinition type in CanyonResidentCatalog.Subtypes)
        {
            string profileName = type.Id + ".tres";
            TestAssert.True(File.ReadAllBytes(Path.Combine(inputs, profileName)).SequenceEqual(
                File.ReadAllBytes(Path.Combine(leaves, profileName))), "builder does not rewrite explicit profile balance");
            foreach (string extension in new[] { ".tres", ".tscn" })
                File.Copy(Path.Combine(leaves, type.Id + extension), Path.Combine(relocated, type.Id + extension), false);
        }
        TestAssert.Equal(48, Directory.GetFiles(relocated).Length, "portable package contains only twenty-four scene/profile pairs");
        foreach (ResidentSubtypeDefinition type in CanyonResidentCatalog.Subtypes)
        {
            string path = Path.Combine(relocated, type.Id + ".tscn");
            using var saved = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
            TestAssert.True(saved != null, "moved scene loads without rebuilding or rewriting references");
            ResidentSubtypeDefinition expected = type with { SceneState = ResidentSceneState.ExplicitPlaceholder, ScenePath = path };
            CharacterBody2D actor = ResidentSceneBinding.Instantiate(saved, expected, Slot(type.Id), true);
            try
            {
                ResidentSceneBinding binding = actor.GetNode<ResidentSceneBinding>("ResidentBinding");
                string actualProfilePath = binding.Profile.ResourcePath.Replace('\\', '/');
                TestAssert.True(actualProfilePath == Path.Combine(relocated, type.Id + ".tres").Replace('\\', '/') ||
                    actualProfilePath.StartsWith(path.Replace('\\', '/') + "::", StringComparison.Ordinal),
                    "moved leaf resolves its profile within the moved package, never the old case " + type.Id);
                TestAssert.True(actor.GetNodeOrNull("VisualRoot") != null,
                    "project visual/script dependencies survive package relocation");
                if (type.Id == "lost-echo") await MountAndCheck(tree, actor, type);
            }
            finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
        }
        GD.Print("RESIDENT_LEAF_PORTABLE_OK scenes=24 profiles=24 copied_bytes_unchanged=true relocated_bindings=24 native_ready=1 publication=false");
    }

    private static async Task MountAndCheck(SceneTree tree, CharacterBody2D actor, ResidentSubtypeDefinition type)
    {
        using var world = new World2D();
        var viewport = new SubViewport { Name = "SavedLeafNativeWorld", Size = new(1024, 640), World2D = world };
        var scope = new Node2D(); viewport.AddChild(scope);
        using var floorShape = new RectangleShape2D { Size = new(1000, 40) };
        var floor = new StaticBody2D { Position = new(500, 360), CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0 };
        floor.AddChild(new CollisionShape2D { Shape = floorShape }); scope.AddChild(floor);
        actor.Position = new(320, 300); scope.AddChild(actor); tree.Root.AddChild(viewport);
        try
        {
            await Frames(tree, 6);
            TestAssert.True(actor.IsNodeReady() && actor.GetWorld2D() == world && world != tree.Root.FindWorld2D(), "saved leaf actually initialized in independent native world");
            var health = actor.GetNode<HealthComponent>("Health"); int hp = health.CurrentHealth;
            TestAssert.True(hp > 0, "saved leaf initializes original real Health");
            if (actor is GraveLampMoth)
                TestAssert.True(actor.GlobalPosition.DistanceTo(new(320, 300)) > .5f, "loaded configured air actor really cruises instead of default Hover");
            var resident = (IResidentEnemyAgent)actor;
            resident.SetResidentDormant(true); await Frames(tree, 2); resident.SetResidentDormant(false);
            TestAssert.Equal(hp, health.CurrentHealth, "loaded identity wake does not heal or reset health");
            if (type.Id == "lost-echo")
            {
                var hurt = actor.GetNode<Hurtbox>("Hurtbox");
                TestAssert.True(!hurt.Receive(new DamageRequest(1, Vector2.Right, 0, 8101)), "saved Ghost policy is actually Hidden after Ready");
                await Frames(tree, 80);
                TestAssert.True(hurt.Receive(new DamageRequest(1, Vector2.Right, 0, 8102)), "real phase clock opens saved Ghost damage policy");
                TestAssert.Equal(hp - 1, health.CurrentHealth, "manifest request reaches original Health, not metadata only");
                GD.Print("RESIDENT_LEAF_GHOST_NATIVE hidden_rejected=true manifest_damaged=true actual_physics=true native_area=false");
            }
            if (type.Id == "bone-charger")
            {
                TestAssert.True(!actor.GetNode<Hurtbox>("Hurtbox").Receive(new DamageRequest(1, Vector2.Right, 0, 8201)), "saved Bone neutral posture is actually guarded after Ready");
                TestAssert.Equal(hp, health.CurrentHealth, "Bone profile was consumed before initialization");
                GD.Print("RESIDENT_LEAF_BONE_NATIVE neutral_guarded=true actual_ready=true full_counter=false");
            }
        }
        finally
        {
            viewport.QueueFree(); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(!GodotObject.IsInstanceValid(actor), "owned native leaf really freed before owned fixture shapes");
        }
    }

    private static void RunNegatives(string leaves)
    {
        ResidentSubtypeDefinition type = CanyonResidentCatalog.Subtypes.Single(x => x.Id == "lost-echo");
        string path = Path.Combine(leaves, "lost-echo.tscn"); PackedScene saved = GD.Load<PackedScene>(path);
        ResidentSubtypeDefinition expected = type with { ScenePath = path, SceneState = ResidentSceneState.ExplicitPlaceholder };
        int count = 0;
        Reject(() => ResidentSceneBinding.Instantiate(saved, expected, Slot(type.Id)), "placeholder needs explicit development admission"); count++;
        Reject(() => ResidentSceneBinding.Instantiate(saved, expected with { SceneState = ResidentSceneState.Unimplemented }, Slot(type.Id), true), "unimplemented flag cannot be mounted"); count++;
        Reject(() => ResidentSceneBinding.Instantiate(saved, expected with { ScenePath = type.ProbeScenePath }, Slot(type.Id), true), "probe alias cannot impersonate distinct saved leaf"); count++;
        Reject(() => ResidentSceneBinding.Instantiate(saved, expected with { Family = ResidentFamily.Bone }, Slot(type.Id), true), "saved profile cannot silently change family"); count++;
        Reject(() => ResidentSceneBinding.Instantiate(saved, expected, Slot(type.Id) with { Placement = null }, true), "missing site cannot become origin"); count++;
        foreach (bool duplicate in new[] { false, true })
        {
            Node modified = saved.Instantiate(); string badPath = Path.Combine(leaves, duplicate ? "negative-duplicate.tscn" : "negative-missing.tscn");
            try
            {
                modified.SceneFilePath = string.Empty;
                if (duplicate) modified.AddChild(new ResidentSceneBinding { Name = "DuplicateBinding", Profile = modified.GetNode<ResidentSceneBinding>("ResidentBinding").Profile });
                else { Node binding = modified.GetNode("ResidentBinding"); modified.RemoveChild(binding); binding.Free(); }
                TestAssert.Equal(Error.Ok, SceneBuildUtil.PackAndSave(modified, badPath), "save actual negative leaf graph");
            }
            finally { modified.Free(); }
            PackedScene bad = GD.Load<PackedScene>(badPath);
            Reject(() => ResidentSceneBinding.Instantiate(bad, expected with { ScenePath = badPath }, Slot(type.Id), true), "same loader rejects actual " + (duplicate ? "duplicate" : "missing") + " saved binding"); count++;
        }
        GD.Print($"RESIDENT_LEAF_NEGATIVES_OK cases={count} same_loader=true");
    }

    private static void Reject(Func<CharacterBody2D> create, string label)
    {
        CharacterBody2D actor = null; bool rejected = false;
        try { actor = create(); } catch (ArgumentException) { rejected = true; }
        finally { if (GodotObject.IsInstanceValid(actor)) actor.Free(); }
        TestAssert.True(rejected, label);
    }

    private static ResidentPopulationDefinition Slot(string id)
    {
        ResidentPopulationDefinition slot = CanyonResidentCatalog.All.First(x => x.SubtypeId == id);
        Vector2[] points = slot.Archetype == ResidentArchetype.Turret ? new[] { new Vector2(320, 300) }
            : new[] { new Vector2(240, slot.Archetype is ResidentArchetype.Flyer or ResidentArchetype.Diver ? 260 : 300), new Vector2(400, 300) };
        return slot with { Placement = new(new(320, 300), new Rect2(100, 100, 600, 400), points) };
    }
    private static async Task Frames(SceneTree tree, int frames)
    {
        for (int i = 0; i < frames; i++) { await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame); await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame); }
    }
}
