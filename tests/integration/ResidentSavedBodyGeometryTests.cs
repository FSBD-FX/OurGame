using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Godot;
using Knight.Build.Prologue;
using Knight.Prologue.Residents;
using Knight.Tests.Fixtures;

namespace Knight.Tests.Integration;

internal static class ResidentSavedBodyGeometryTests
{
    internal static void Run(string storage)
    {
        CheckOriginalSourceCustody();
        // Literal rectangles are independent of both readers. Changing saved
        // bodies must change the actual native comparison, not an expected helper.
        var cases = new (string Path, Rect2 Bounds)[]
        {
            (CanyonEncounterCatalog.MothScene, new(-16, -16, 32, 32)),
            (CanyonEncounterCatalog.RustShellScene, new(-18, -32, 36, 64)),
            (CanyonEncounterCatalog.LaborerScene, new(-28, -42, 56, 84)),
            (CanyonEncounterCatalog.SkitterScene, new(-14, -23, 28, 22)),
            (CanyonEncounterCatalog.SentryScene, new(-25, -39, 50, 68)),
            (CanyonEncounterCatalog.ScatterScene, new(-25, -39, 50, 68)),
            (CanyonEncounterCatalog.BurstScene, new(-25, -39, 50, 68)),
            (CanyonEncounterCatalog.MissileScene, new(-25, -39, 50, 68)),
            (CanyonEncounterCatalog.LaserScene, new(-25, -39, 50, 68))
        };
        foreach (var item in cases)
        {
            var saved = GD.Load<PackedScene>(item.Path);
            TestAssert.True(ResidentSavedBodyGeometry.TryRead(saved, out Rect2 data, out string reason),
                "actual saved body must be readable without instantiating AI: " + item.Path + ":" + reason);
            AssertBounds(item.Bounds, data, item.Path + " literal");
            CharacterBody2D old = saved.Instantiate<CharacterBody2D>();
            try
            {
                TestAssert.True(TryNativeBodyBounds(old, Transform2D.Identity, out var native),
                    item.Path + ": actual constructor preserves the complete previous body contract");
                AssertBounds(item.Bounds, native, item.Path + " actual native body");
            }
            finally { old.Free(); }
        }
        TestAssert.Equal(9, CanyonEncounterCatalog.Normal.SelectMany(x => x.Enemies)
            .Select(x => x.ScenePath).Distinct(StringComparer.Ordinal).Count(), "the comparison covers every original carrier");
        var reservations = new List<ResidentKeepOut>(); var errors = new List<string>();
        var capture = typeof(CanyonResidentManager).GetMethod("CaptureOriginalSpawns", BindingFlags.NonPublic | BindingFlags.Static);
        TestAssert.True(capture != null, "reservation comparison reaches the real manager consumer");
        using var originalSources = new ResidentOriginalSceneSources(cases.Select(x => x.Path), path => GD.Load<PackedScene>(path));
        capture.Invoke(null, new object[] { reservations, errors, (Func<string, PackedScene>)originalSources.Get });
        TestAssert.Equal(0, errors.Count, "all real original carrier reservations remain available");
        TestAssert.Equal(36, reservations.Count, "all26 original slots and10 alternate reservations are preserved");
        TestAssert.Equal(36, reservations.Select(x => x.SourceId).Distinct().Count(), "original primary/alternate identities stay unique");
        foreach (var encounter in CanyonEncounterCatalog.Normal)
        for (int index = 0; index < encounter.Enemies.Count; index++)
        {
            var enemy = encounter.Enemies[index];
            Rect2 nativeLiteral = cases.Single(x => x.Path == enemy.ScenePath).Bounds;
            var points = new List<(string Tag, Vector2 Point)> { ("primary", enemy.WorldPosition) };
            if (enemy.AlternateWorldPosition is Vector2 other) points.Add(("alternate", other));
            foreach (var point in points)
            {
                string id = "old-spawn:" + encounter.StableId + ":" + index + ":" + point.Tag;
                var reservation = reservations.Single(x => x.SourceId == id);
                TestAssert.Equal(ResidentKeepOutKind.OriginalEnemy, reservation.Kind, id + " retains old enemy reservation kind");
                Rect2 expected = new(nativeLiteral.Position + point.Point, nativeLiteral.Size);
                if (id == "old-spawn:canyon.v2.encounter.counterweight-lift:1:primary") expected = new(4682, 3392, 36, 68);
                if (id == "old-spawn:canyon.v2.encounter.counterweight-lift:1:alternate") expected = new(4612, 3392, 36, 68);
                AssertBounds(expected, reservation.WorldBounds, id);
            }
        }

        var sentinel = new SavedBodyConstructorProbe { Name = "ConstructorSentinel" };
        try
        {
            Body(sentinel);
            using var packed = Pack(sentinel);
            int before = SavedBodyConstructorProbe.Constructions;
            TestAssert.True(ResidentSavedBodyGeometry.TryRead(packed, out var bounds, out _), "sentinel saved body is valid");
            AssertBounds(new(-10, -15, 20, 30), bounds, "sentinel");
            TestAssert.Equal(before, SavedBodyConstructorProbe.Constructions, "saved-data reading must not execute the actor constructor");
            Node instance = packed.Instantiate();
            try { TestAssert.Equal(before + 1, SavedBodyConstructorProbe.Constructions,
                "positive control: the same saved scene really executes its constructor when instantiated"); }
            finally { instance.Free(); }
            // The reader cannot execute arbitrary scripts. The authored-carrier
            // parity oracle must catch runtime-only structural changes instead.
            SavedBodyConstructorProbe.AddUnexpectedBody = true;
            Node mutated = null;
            try
            {
                before = SavedBodyConstructorProbe.Constructions;
                TestAssert.True(ResidentSavedBodyGeometry.TryRead(packed, out _, out _) &&
                    SavedBodyConstructorProbe.Constructions == before,
                    "saved reading still does not execute a structural constructor");
                mutated = packed.Instantiate();
                TestAssert.True(SavedBodyConstructorProbe.Constructions == before + 1 &&
                    !TryNativeBodyBounds((CharacterBody2D)mutated, Transform2D.Identity, out _),
                    "full native parity rejects the constructor-added second actor body shape");
            }
            finally { SavedBodyConstructorProbe.AddUnexpectedBody = false; mutated?.Free(); }
        }
        finally { sentinel.Free(); }

        int negatives = 0;
        void Reject(string name, Action<CharacterBody2D> mutate)
        {
            var root = new CharacterBody2D { Name = "RejectedBody" };
            try
            {
                Body(root); mutate(root);
                using var saved = Pack(root);
                TestAssert.True(!ResidentSavedBodyGeometry.TryRead(saved, out _, out var reason) &&
                    !string.IsNullOrWhiteSpace(reason), "unsupported saved structure must fail closed: " + name);
                negatives++;
            }
            finally { root.Free(); }
        }
        Reject("missing", root => root.GetNode("BodyCollider").Free());
        Reject("extra-static-body", root =>
        {
            var body = new StaticBody2D { Name = "ExtraStaticBody" }; root.AddChild(body); Body(body);
        });
        Reject("grouped-extra-rigid-body", root =>
        {
            var group = new Node2D { Name = "Group" }; root.AddChild(group);
            var body = new RigidBody2D { Name = "ExtraRigidBody" }; group.AddChild(body); Body(body);
        });
        Reject("area-nested-extra-character", root =>
        {
            var area = new Area2D { Name = "Area" }; root.AddChild(area);
            var body = new CharacterBody2D { Name = "ExtraCharacter" }; area.AddChild(body); Body(body);
        });
        Reject("extra", root => root.AddChild(new CollisionShape2D { Name = "ExtraBody", Shape = new CircleShape2D { Radius = 2 } }));
        Reject("polygon", root => root.AddChild(new CollisionPolygon2D { Name = "Polygon", Polygon = new[] { Vector2.Zero, Vector2.Right * 8, Vector2.Down * 8 } }));
        Reject("disabled", root => root.GetNode<CollisionShape2D>("BodyCollider").Disabled = true);
        Reject("one-way", root => root.GetNode<CollisionShape2D>("BodyCollider").OneWayCollision = true);
        Reject("scale", root => root.GetNode<CollisionShape2D>("BodyCollider").Scale = new(2, 1));
        Reject("rotation", root => root.GetNode<CollisionShape2D>("BodyCollider").Rotation = .2f);
        Reject("top-level", root => root.GetNode<CollisionShape2D>("BodyCollider").TopLevel = true);
        Reject("unsupported-shape", root => root.GetNode<CollisionShape2D>("BodyCollider").Shape = new SegmentShape2D { A = Vector2.Zero, B = Vector2.Down * 20 });
        Reject("nested-body", root =>
        {
            var group = new Node2D { Name = "Group" }; root.AddChild(group);
            root.GetNode("BodyCollider").Reparent(group);
        });
        void AddInstance(CharacterBody2D root, bool physics, bool placeholder)
        {
            var visual = new Node2D { Name = "NestedVisual" };
            try
            {
                if (physics) visual.AddChild(new StaticBody2D { Name = "HiddenBody" });
                using var saved = Pack(visual);
                string path = Path.Combine(storage, "saved-body-nested-" + physics + "-" + placeholder + ".tscn");
                TestAssert.Equal(Error.Ok, ResourceSaver.Save(saved, path), "save a real private nested scene");
                Node instance = GD.Load<PackedScene>(path).Instantiate();
                instance.SetSceneInstanceLoadPlaceholder(placeholder);
                root.AddChild(instance);
            }
            finally { visual.Free(); }
        }
        Reject("nested-physics-instance", root => AddInstance(root, true, false));
        Reject("nested-placeholder", root => AddInstance(root, false, true));
        Reject("skew", root => root.GetNode<CollisionShape2D>("BodyCollider").Skew = .2f);
        Reject("scripted-collider", root =>
        {
            var script = new GDScript { SourceCode = "extends CollisionShape2D\n" };
            TestAssert.Equal(Error.Ok, script.Reload(), "private collider script compiles");
            root.GetNode("BodyCollider").SetScript(script);
        });
        Reject("scripted-shape", root =>
        {
            var script = new GDScript { SourceCode = "extends RectangleShape2D\n" };
            TestAssert.Equal(Error.Ok, script.Reload(), "private shape script compiles");
            root.GetNode<CollisionShape2D>("BodyCollider").Shape.SetScript(script);
        });
        Reject("root-top-level", root => root.TopLevel = true);
        var areaRoot = new CharacterBody2D { Name = "AreaIsNotBody" };
        try
        {
            Body(areaRoot);
            areaRoot.Position = new(901, -72); areaRoot.Rotation = .7f; areaRoot.Scale = new(1.5f, 2f);
            var area = new Area2D { Name = "Hurtbox" }; areaRoot.AddChild(area);
            area.AddChild(new CollisionShape2D { Name = "NotTheBody", Shape = new CircleShape2D { Radius = 100 } });
            using var saved = Pack(areaRoot);
            TestAssert.True(ResidentSavedBodyGeometry.TryRead(saved, out var bounds, out _), "area shapes are not the physical actor body");
            AssertBounds(new(-10, -15, 20, 30), bounds, "area ownership");
            var originalShape = areaRoot.GetNode<CollisionShape2D>("BodyCollider").Shape as RectangleShape2D;
            originalShape.Size = new(40, 10);
            TestAssert.True(ResidentSavedBodyGeometry.TryRead(saved, out var changed, out _), "fresh saved resource mutation is read");
            AssertBounds(new(-20, -5, 40, 10), changed, "no stale scalar cache after shape mutation");
            var replacementPose = new Transform2D(.3f, new Vector2(47, 90));
            TestAssert.True(TryNativeBodyBounds(areaRoot, replacementPose, out var posed),
                "native body accepts the replacement root pose");
            AssertBounds(replacementPose * changed, posed,
                "replacement pose is used once and saved root transform is not multiplied again");
        }
        finally { areaRoot.Free(); }
        var visualRoot = new CharacterBody2D { Name = "PureVisualInstance" };
        try
        {
            Body(visualRoot); AddInstance(visualRoot, false, false);
            using var saved = Pack(visualRoot);
            SceneState state = saved.GetState();
            TestAssert.True(Enumerable.Range(1, state.GetNodeCount() - 1)
                .Any(index => state.GetNodeInstance(index) != null), "private fixture retains a genuine nested PackedScene");
            TestAssert.True(ResidentSavedBodyGeometry.TryRead(saved, out var bounds, out _), "verified visual nested scene remains supported");
            AssertBounds(new(-10, -15, 20, 30), bounds, "nested visual ownership");
        }
        finally { visualRoot.Free(); }
        var nonbody = new Node2D { Name = "NotCharacterBody" };
        try
        {
            Body(nonbody); using var saved = Pack(nonbody);
            TestAssert.True(!ResidentSavedBodyGeometry.TryRead(saved, out _, out _), "nonbody root fails closed"); negatives++;
        }
        finally { nonbody.Free(); }
        GD.Print($"RESIDENT_SAVED_BODY_GEOMETRY_OK carriers=9 reservations=36 literal_native_comparison=true constructor_executions=0 positive_constructor_control=true negatives={negatives} area_ownership=true normal_route=false");
    }

    private static void CheckOriginalSourceCustody()
    {
        int acquisitions = 0;
        PackedScene Load(string path) { acquisitions++; return GD.Load<PackedScene>(path); }
        using (var sources = new ResidentOriginalSceneSources(new[] { CanyonEncounterCatalog.MothScene }, Load))
        {
            PackedScene first = sources.Get(CanyonEncounterCatalog.MothScene);
            PackedScene second = sources.Get(CanyonEncounterCatalog.MothScene);
            TestAssert.True(acquisitions == 1 && ReferenceEquals(first, second),
                "one owner acquires its same valid original scene only once, not once per admission check");
            TestAssert.True(ResidentSavedBodyGeometry.TryRead(second, out var body, out _) && body == new Rect2(-16, -16, 32, 32),
                "retained actual saved source still reaches the real complete-body reader");
            bool rejected = false;
            try { sources.Get(CanyonEncounterCatalog.RustShellScene); }
            catch (ArgumentException) { rejected = true; }
            TestAssert.True(rejected && acquisitions == 1, "unknown sources cannot grow the bounded original catalog");
            sources.Dispose();
            TestAssert.True(GodotObject.IsInstanceValid(first), "source release must never dispose a borrowed scene");
            rejected = false;
            try { sources.Get(CanyonEncounterCatalog.MothScene); }
            catch (ObjectDisposedException) { rejected = true; }
            TestAssert.True(rejected, "retired owner cannot reacquire sources");
        }
        using (var replacement = new ResidentOriginalSceneSources(new[] { CanyonEncounterCatalog.MothScene }, Load))
        {
            replacement.Get(CanyonEncounterCatalog.MothScene);
            TestAssert.Equal(2, acquisitions, "replacement owner has independent source acquisition, not static caching");
        }
        var root = new CharacterBody2D { Name = "OriginalSourceCustodyControl" };
        var replacementRoot = new CharacterBody2D { Name = "ReplacementSourceCustodyControl" };
        try
        {
            Body(root); Body(replacementRoot);
            using var first = Pack(root);
            using var second = Pack(replacementRoot);
            int privateLoads = 0;
            PackedScene current = first;
            using var sources = new ResidentOriginalSceneSources(new[] { "private-body" }, _ => { privateLoads++; return current; });
            TestAssert.True(ResidentSavedBodyGeometry.TryRead(sources.Get("private-body"), out var initial, out _) &&
                initial == new Rect2(-10, -15, 20, 30), "private source has literal initial geometry");
            ((RectangleShape2D)root.GetNode<CollisionShape2D>("BodyCollider").Shape).Size = new(40, 10);
            TestAssert.True(ResidentSavedBodyGeometry.TryRead(sources.Get("private-body"), out var changed, out _) &&
                changed == new Rect2(-20, -5, 40, 10) && privateLoads == 1,
                "same retained scene must still read live shape edits; never memoize stale Rect2 bounds");
            first.Dispose(); current = second;
            TestAssert.True(ReferenceEquals(second, sources.Get("private-body")) && privateLoads == 2,
                "a truly invalid owned control wrapper must be reacquired");
            sources.Dispose();
            TestAssert.True(GodotObject.IsInstanceValid(second), "clearing source custody cannot free a borrowed replacement");
        }
        finally { root.Free(); replacementRoot.Free(); }
        int retries = 0;
        using (var sources = new ResidentOriginalSceneSources(new[] { "retry" }, _ =>
        {
            retries++;
            if (retries == 1) return null;
            if (retries == 2) throw new InvalidOperationException("owned source retry control");
            return GD.Load<PackedScene>(CanyonEncounterCatalog.MothScene);
        }))
        {
            TestAssert.True(sources.Get("retry") == null, "a missing source remains missing");
            bool rejected = false;
            try { sources.Get("retry"); }
            catch (InvalidOperationException) { rejected = true; }
            TestAssert.True(rejected && GodotObject.IsInstanceValid(sources.Get("retry")) && retries == 3,
                "null and failed acquisition must not poison the next legitimate request");
        }
        ResidentOriginalSceneSources retiredDuringLoad = null;
        retiredDuringLoad = new(new[] { "retire-during-load" }, _ =>
        {
            var borrowed = GD.Load<PackedScene>(CanyonEncounterCatalog.MothScene);
            retiredDuringLoad.Dispose();
            return borrowed;
        });
        bool retiredRejected = false;
        try { retiredDuringLoad.Get("retire-during-load"); }
        catch (ObjectDisposedException) { retiredRejected = true; }
        finally { retiredDuringLoad.Dispose(); }
        TestAssert.True(retiredRejected, "source acquisition cannot return or retain a resource after a reentrant owner retirement");
        GD.Print("RESIDENT_ORIGINAL_SOURCE_CUSTODY_OK bounded=true same_owner_once=true fresh_shape_read=true invalid_reacquired=true failure_retry=true borrowed_not_disposed=true independent_owners=true");
    }

    private static void Body(Node root) => root.AddChild(new CollisionShape2D
    { Name = "BodyCollider", Shape = new RectangleShape2D { Size = new(20, 30) } });

    private static PackedScene Pack(Node root)
    {
        void Own(Node parent)
        { foreach (Node child in parent.GetChildren()) { child.Owner = root; if (string.IsNullOrEmpty(child.SceneFilePath)) Own(child); } }
        Own(root);
        var saved = new PackedScene();
        TestAssert.Equal(Error.Ok, saved.Pack(root), "private native fixture packs successfully");
        return saved;
    }

    private static void AssertBounds(Rect2 expected, Rect2 actual, string name) =>
        TestAssert.True(expected.Position.DistanceTo(actual.Position) < .001f && expected.Size.DistanceTo(actual.Size) < .001f,
            name + ": expected=" + expected + " actual=" + actual);

    private static bool TryNativeBodyBounds(CharacterBody2D actor, Transform2D pose, out Rect2 bounds)
    {
        var method = typeof(CanyonResidentManager).GetMethod("TryBodyBounds", BindingFlags.NonPublic | BindingFlags.Static);
        TestAssert.True(method != null, "native parity uses the complete previous manager contract");
        object[] arguments = { actor, pose, default(Rect2) };
        bool accepted = (bool)method.Invoke(null, arguments);
        bounds = (Rect2)arguments[2];
        return accepted;
    }
}
