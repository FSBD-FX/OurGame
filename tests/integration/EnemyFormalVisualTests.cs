using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Knight.Combat;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static class EnemyFormalVisualTests
{
    public static void Run()
    {
        AttackTelegraphRuntimeTests.Run();
        OrdinaryEnemiesPublishStableReadableLayerContracts();
        StableAccessoryContractCannotMaskTypedActionPoses();
        MothAccessoryPublishesOneCropPerIndependentWing();
        OrdinaryEnemyAtlasesContainSeparatedSemanticPixels();
        RustShellMapsEveryState();
        GraveLampMothMapsEveryState();
        MothTelegraphFoldsOpposedRealWingHinges();
        BellBackLaborerMapsEveryState();
        GeneratedEnemySlotsOwnRenderingAndTypedAttackTransforms();
        AttackFramesLeadEnemySilhouettesAndKeepCorruptionBehindThem();
        EnemyAttackPhaseBoundariesLeadPrimaryAndBoundSecondaryMotion();
        EnemyCorruptionBoundsEncloseDrawnCirclesAndHurtCuesUsePhysicsFrameBudget();
        EnemyIdleAndEveryAttackPhaseExposeTheirDrawnCorruptionBounds();
        GeneratedEnemiesMountOnlyFormalVisuals();
    }

    private static void MothAccessoryPublishesOneCropPerIndependentWing()
    {
        GraveLampMothFormalVisual visual = Load<GraveLampMothFormalVisual>("GraveLampMothFormalVisual");
        Node2D accessory = visual.GetNode<Node2D>("Accessory");
        string[] sources = accessory.GetMeta("motion_sources").AsStringArray();
        TestAssert.Equal(2, sources.Length,
            "Moth Accessory publishes exactly two independently animated wing sources");

        string atlasPath = ProductionEnemyAtlasPath();
        string absolute = Path.Combine(Directory.GetCurrentDirectory(),
            atlasPath["res://".Length..].Replace('/', Path.DirectorySeparatorChar));
        Image atlas = Image.LoadFromFile(absolute);
        var crops = new List<Rect2I>();
        var before = new Transform2D[sources.Length];
        for (int index = 0; index < sources.Length; index++)
        {
            string sublayerName = index == 0 ? "FarWing" : "NearWing";
            Node2D sublayer = accessory.GetNodeOrNull<Node2D>(sublayerName);
            TestAssert.True(sublayer != null,
                $"Moth Accessory/{sublayerName} publishes its own semantic pixel-to-motion mapping");
            TestAssert.Equal(sources[index], sublayer!.GetMeta("motion_source").AsString(),
                $"Moth Accessory/{sublayerName} maps one crop to one typed wing source");
            TestAssert.Equal(atlasPath, sublayer.GetMeta("atlas_path").AsString(),
                $"Moth Accessory/{sublayerName} resolves through the production enemy atlas");
            Rect2 value = sublayer.GetMeta("atlas_source").AsRect2();
            var crop = new Rect2I((int)value.Position.X, (int)value.Position.Y,
                (int)value.Size.X, (int)value.Size.Y);
            TestAssert.True(crop.Size.X > 0 && crop.Size.Y > 0 && crop.End.X <= atlas.GetWidth() &&
                crop.End.Y <= atlas.GetHeight() && OpaqueBounds(atlas, crop).HasArea(),
                $"Moth Accessory/{sublayerName} owns authored pixels in its dedicated crop");
            foreach (Rect2I prior in crops)
                TestAssert.True(!prior.Intersects(crop),
                    $"Moth Accessory/{sublayerName} crop is independent from the other wing crop");
            crops.Add(crop);
            before[index] = visual.GetNode<Node2D>(sources[index]).Transform;
        }

        Present(visual, Attack(EnemyAttackKind.MothDive, AttackPhase.Telegraph, .75f));
        Node2D farWing = visual.GetNode<Node2D>(sources[0]);
        Node2D nearWing = visual.GetNode<Node2D>(sources[1]);
        TestAssert.True(TransformDelta(before[0], farWing.Transform) > .01f &&
            TransformDelta(before[1], nearWing.Transform) > .01f && farWing.Rotation * nearWing.Rotation < 0f,
            "Moth independently mapped FarWing and NearWing fold on opposed typed hinges");
        visual.Free();
    }

    private static void OrdinaryEnemiesPublishStableReadableLayerContracts()
    {
        AssertReadableLayerContract<RustShellFormalVisual>("RustShellFormalVisual",
            new Rect2(-42f, -42f, 104f, 68f));
        AssertReadableLayerContract<GraveLampMothFormalVisual>("GraveLampMothFormalVisual",
            new Rect2(-58f, -40f, 116f, 78f));
        AssertReadableLayerContract<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual",
            new Rect2(-55f, -72f, 150f, 116f));
    }

    private static void OrdinaryEnemyAtlasesContainSeparatedSemanticPixels()
    {
        string atlasPath = ProductionEnemyAtlasPath();
        string absolute = Path.Combine(Directory.GetCurrentDirectory(),
            atlasPath["res://".Length..].Replace('/', Path.DirectorySeparatorChar));
        Image atlas = Image.LoadFromFile(absolute);
        var sources = new List<Rect2I>();
        AssertSemanticAtlas<RustShellFormalVisual>("RustShellFormalVisual", atlasPath, atlas, sources,
            ("Body", "74402c"), ("Weapon", "d8d0b9"), ("Corruption", "315b55"),
            ("Accessory", "ad6e42"), ("Effects", "e1dbc7"));
        AssertSemanticAtlas<GraveLampMothFormalVisual>("GraveLampMothFormalVisual", atlasPath, atlas, sources,
            ("Body", "c39a4c"), ("Weapon", "c8a755"), ("Corruption", "315b55"),
            ("Accessory/FarWing", "294b4c"), ("Accessory/NearWing", "294b4c"),
            ("Effects", "e1dbc7"));
        AssertSemanticAtlas<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual", atlasPath, atlas, sources,
            ("Body", "574438"), ("Weapon", "dfbf70"), ("Corruption", "315b55"),
            ("Accessory", "b88a42"), ("Effects", "e1dbc7"));
        AssertStableAtlasPath<RustShellFormalVisual>("RustShellFormalVisual", atlasPath);
        AssertStableAtlasPath<GraveLampMothFormalVisual>("GraveLampMothFormalVisual", atlasPath);
        AssertStableAtlasPath<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual", atlasPath);

        for (int y = 0; y < atlas.GetHeight(); y++)
        for (int x = 0; x < atlas.GetWidth(); x++)
        {
            if (atlas.GetPixel(x, y).A <= .05f) continue;
            int owners = sources.Count(source => source.HasPoint(new Vector2I(x, y)));
            TestAssert.True(owners == 1,
                $"production enemy atlas opaque pixel ({x},{y}) belongs to exactly one semantic crop");
        }
    }

    private static void AssertStableAtlasPath<T>(string sceneName, string atlasPath) where T : Node2D
    {
        T visual = Load<T>(sceneName);
        foreach (string role in new[] { "Body", "Weapon", "Corruption", "Accessory", "Effects" })
            TestAssert.Equal(atlasPath, visual.GetNode<Node2D>(role).GetMeta("atlas_path").AsString(),
                $"{sceneName}/{role} resolves to the manifest-declared production enemy atlas");
        visual.Free();
    }

    private static string ProductionEnemyAtlasPath()
    {
        string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "assets", "production",
            "prologue-assets.json");
        ProductionAssetManifest manifest = ProductionAssetManifestParser.Parse(File.ReadAllText(manifestPath));
        return manifest.Assets.Single(asset => asset.Category == "enemies").RuntimePath;
    }

    private static void AssertSemanticAtlas<T>(string sceneName, string atlasPath, Image atlas,
        List<Rect2I> sources,
        params (string Role, string SignatureRgb)[] expected) where T : Node2D
    {
        T visual = Load<T>(sceneName);
        foreach ((string role, string signatureRgb) in expected)
        {
            Node2D layer = visual.GetNode<Node2D>(role);
            Rect2 sourceValue = layer.GetMeta("atlas_source").AsRect2();
            var source = new Rect2I((int)sourceValue.Position.X, (int)sourceValue.Position.Y,
                (int)sourceValue.Size.X, (int)sourceValue.Size.Y);
            TestAssert.True(sourceValue == new Rect2(source.Position, source.Size) &&
                source.Position.X >= 0 && source.Position.Y >= 0 &&
                source.End.X <= atlas.GetWidth() && source.End.Y <= atlas.GetHeight(),
                $"{sceneName}/{role} publishes an integral in-bounds production-atlas crop");

            Rect2I pixels = OpaqueBounds(atlas, source);
            TestAssert.True(pixels.Size.X > 0 && pixels.Size.Y > 0,
                $"{sceneName}/{role} crop contains actual authored pixels");
            TestAssert.True(ContainsRgb(atlas, source, new Color(signatureRgb)),
                $"{sceneName}/{role} production-atlas crop owns its expected semantic value-group pixels");
            TestAssert.True(pixels.Position.X >= source.Position.X + 2 &&
                pixels.Position.Y >= source.Position.Y + 2 &&
                pixels.End.X <= source.End.X - 2 && pixels.End.Y <= source.End.Y - 2,
                $"{sceneName}/{role} pixels have a transparent isolation gutter for safe replacement cropping");
            foreach (Rect2I prior in sources)
                TestAssert.True(!prior.Intersects(source),
                    $"{sceneName}/{role} production semantic crop does not overlap another actor or role");
            sources.Add(source);
            TestAssert.Equal(atlasPath, layer.GetMeta("atlas_path").AsString(),
                $"{sceneName}/{role} resolves to the manifest-declared production enemy atlas");
            TestAssert.True(layer.GetMeta("atlas_content").AsRect2() == new Rect2(pixels.Position, pixels.Size),
                $"{sceneName}/{role} publishes its measured alpha-content bounds rather than guessed metadata");
            Vector2 atlasOrigin = layer.GetMeta("atlas_origin").AsVector2();
            TestAssert.True(source.HasPoint(new Vector2I((int)atlasOrigin.X, (int)atlasOrigin.Y)),
                $"{sceneName}/{role} publishes an in-crop alignment origin");
            Rect2 reconstructed = new Rect2((Vector2)pixels.Position - atlasOrigin, pixels.Size);
            Rect2 declared = layer.GetMeta("draw_bounds").AsRect2().Grow(2f);
            TestAssert.True(declared.Encloses(reconstructed),
                $"{sceneName}/{role} draw bounds reconstruct the actual semantic pixels around atlas_origin");
        }

        visual.Free();
    }

    private static Rect2I OpaqueBounds(Image image, Rect2I source)
    {
        int minX = source.End.X;
        int minY = source.End.Y;
        int maxX = source.Position.X - 1;
        int maxY = source.Position.Y - 1;
        for (int y = source.Position.Y; y < source.End.Y; y++)
        for (int x = source.Position.X; x < source.End.X; x++)
        {
            if (image.GetPixel(x, y).A <= .05f) continue;
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }
        return maxX < minX ? new Rect2I() : new Rect2I(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static bool ContainsRgb(Image image, Rect2I source, Color expected)
    {
        for (int y = source.Position.Y; y < source.End.Y; y++)
        for (int x = source.Position.X; x < source.End.X; x++)
        {
            Color actual = image.GetPixel(x, y);
            if (actual.A > .05f && Mathf.Abs(actual.R - expected.R) < .01f &&
                Mathf.Abs(actual.G - expected.G) < .01f && Mathf.Abs(actual.B - expected.B) < .01f)
                return true;
        }
        return false;
    }

    private static void StableAccessoryContractCannotMaskTypedActionPoses()
    {
        AssertMovingAccessory<RustShellFormalVisual>("RustShellFormalVisual", EnemyAttackKind.RustShellThrust,
            "Shell");
        AssertMovingAccessory<GraveLampMothFormalVisual>("GraveLampMothFormalVisual", EnemyAttackKind.MothDive,
            "Wings/FarWing", "Wings/NearWing");
        AssertMovingRole<GraveLampMothFormalVisual>("GraveLampMothFormalVisual", "Weapon",
            "GraveLampLens", EnemyAttackKind.MothDive, "Lamp");
        AssertMovingAccessory<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual",
            EnemyAttackKind.LaborerSweep, "Bell");
    }

    private static void AssertMovingAccessory<T>(string sceneName, EnemyAttackKind attack,
        params string[] movingSources) where T : Node2D
        => AssertMovingRole<T>(sceneName, "Accessory", "IdentityPlate", attack, movingSources);

    private static void AssertMovingRole<T>(string sceneName, string role, string forbiddenStaticChild,
        EnemyAttackKind attack, params string[] movingSources) where T : Node2D
    {
        T visual = Load<T>(sceneName);
        Node2D stableRole = visual.GetNode<Node2D>(role);
        TestAssert.True(stableRole.GetNodeOrNull(forbiddenStaticChild) == null,
            $"{sceneName}/{role} has no static duplicate silhouette that can mask typed action motion");
        string[] declaredSources = stableRole.GetMeta("motion_sources").AsStringArray();
        TestAssert.Equal(movingSources.Length, declaredSources.Length,
            $"{sceneName}/{role} publishes every typed motion source");
        var sources = new Node2D[movingSources.Length];
        var before = new Transform2D[movingSources.Length];
        for (int index = 0; index < movingSources.Length; index++)
        {
            TestAssert.Equal(movingSources[index], declaredSources[index],
                $"{sceneName}/{role} motion source {index} is deterministic");
            sources[index] = visual.GetNode<Node2D>(movingSources[index]);
            before[index] = sources[index].Transform;
        }
        Present(visual, Attack(attack, AttackPhase.Telegraph, .75f));
        for (int index = 0; index < movingSources.Length; index++)
            TestAssert.True(TransformDelta(before[index], sources[index].Transform) > .01f,
                $"{sceneName}/{movingSources[index]} visibly participates in its typed telegraph pose");
        visual.Free();
    }

    private static void AssertReadableLayerContract<T>(string sceneName, Rect2 expectedProfile)
        where T : Node2D
    {
        T visual = Load<T>(sceneName);
        string[] roles = ["Body", "Weapon", "Corruption", "Accessory", "Effects"];
        foreach (string role in roles)
        {
            Node2D layer = visual.GetNodeOrNull<Node2D>(role);
            TestAssert.True(layer != null, $"{sceneName} exposes stable top-level {role} layer");
            TestAssert.Equal(role.ToLowerInvariant(), layer!.GetMeta("semantic_layer").AsString(),
                $"{sceneName}/{role} publishes its deterministic semantic layer");
            TestAssert.Equal(ProductionEnemyAtlasPath(), layer.GetMeta("atlas_path").AsString(),
                $"{sceneName}/{role} resolves through the production enemy atlas authority");
            if (layer.HasMeta("atlas_source"))
            {
                Rect2 source = layer.GetMeta("atlas_source").AsRect2();
                TestAssert.True(source.Size.X > 0f && source.Size.Y > 0f &&
                    source.Position.X >= 0f && source.Position.Y >= 0f &&
                    source.End.X <= 768f && source.End.Y <= 160f,
                    $"{sceneName}/{role} publishes a bounded global production-atlas source region");
            }
            else
            {
                TestAssert.True(visual is GraveLampMothFormalVisual && role == "Accessory" &&
                    layer.GetNodeOrNull<Node2D>("FarWing") != null &&
                    layer.GetNodeOrNull<Node2D>("NearWing") != null,
                    $"{sceneName}/{role} without an aggregate crop publishes its semantic sublayers");
            }
            TestAssert.True(layer.GetMeta("pivot").AsVector2().IsEqualApprox(layer.Position),
                $"{sceneName}/{role} publishes the actual replacement pivot");
        }

        Rect2 profile = visual.GetMeta("profile_bounds").AsRect2();
        TestAssert.True(profile == expectedProfile,
            $"{sceneName} publishes its hand-authored enemy-specific profile bounds");
        Rect2 body = DrawBounds(visual.GetNode<Node2D>("Body"));
        Rect2 weapon = DrawBounds(visual.GetNode<Node2D>("Weapon"));
        Rect2 corruption = DrawBounds(visual.GetNode<Node2D>("Corruption"));
        TestAssert.True(profile.Encloses(body) && profile.Encloses(weapon) && profile.Encloses(corruption),
            $"{sceneName} profile encloses body, weapon, and corruption geometry");
        TestAssert.True(corruption.End.X <= weapon.Position.X &&
            corruption.End.X <= profile.Position.X + profile.Size.X * .52f,
            $"{sceneName} medium corruption stays on the back/non-weapon outline");
        float corruptionRatio = (corruption.Size.X * corruption.Size.Y) /
            (profile.Size.X * profile.Size.Y);
        TestAssert.True(corruptionRatio >= .03f && corruptionRatio <= .18f,
            $"{sceneName} corruption remains visibly medium but subordinate to the profile");

        Node2D facing = visual.GetNodeOrNull<Node2D>("Effects/FacingIndicator");
        Node2D feet = visual.GetNodeOrNull<Node2D>("Effects/FootContact");
        TestAssert.True(facing != null && HasVisibleGeometry(facing) &&
            DrawBounds(facing).Position.X > body.GetCenter().X,
            $"{sceneName} has a visible weapon-side facing indicator without warning color");
        TestAssert.True(feet != null && HasVisibleGeometry(feet) &&
            DrawBounds(feet).End.Y >= body.End.Y - 2f,
            $"{sceneName} has a visible feet/contact cue at the bottom of the silhouette");
        visual.Free();
    }

    private static bool HasVisibleGeometry(Node node)
    {
        if (node is Polygon2D polygon && polygon.Visible && polygon.Polygon.Length >= 3 && polygon.Color.A > .05f)
            return true;
        if (node is Line2D line && line.Visible && line.Points.Length >= 2 &&
            line.Width > 0f && line.DefaultColor.A > .05f) return true;
        foreach (Node child in node.GetChildren())
            if (HasVisibleGeometry(child)) return true;
        return false;
    }

    private static Rect2 DrawBounds(Node2D layer)
    {
        Rect2 local = layer.GetMeta("draw_bounds").AsRect2();
        return new Rect2(local.Position + layer.Position, local.Size);
    }

    private static void GeneratedEnemySlotsOwnRenderingAndTypedAttackTransforms()
    {
        AssertFunctionalRig<RustShellFormalVisual>("RustShellFormalVisual",
            EnemyAttackKind.RustShellThrust, "Weapon",
            ("Corruption", EnemyFormalPartKind.RustCorruption),
            ("Body", EnemyFormalPartKind.RustBody),
            ("Shell", EnemyFormalPartKind.RustShell),
            ("Weapon", EnemyFormalPartKind.RustWeapon));
        AssertFunctionalRig<GraveLampMothFormalVisual>("GraveLampMothFormalVisual",
            EnemyAttackKind.MothDive, "Wings/FarWing",
            ("Corruption", EnemyFormalPartKind.MothCorruption),
            ("Wings/FarWing", EnemyFormalPartKind.MothFarWing),
            ("Body", EnemyFormalPartKind.MothBody),
            ("Wings/NearWing", EnemyFormalPartKind.MothNearWing),
            ("Lamp", EnemyFormalPartKind.MothLamp));
        AssertFunctionalRig<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual",
            EnemyAttackKind.LaborerSweep, "Weapon",
            ("Corruption", EnemyFormalPartKind.LaborerCorruption),
            ("Bell", EnemyFormalPartKind.LaborerBell),
            ("Body", EnemyFormalPartKind.LaborerBody),
            ("Feet", EnemyFormalPartKind.LaborerFeet),
            ("Weapon", EnemyFormalPartKind.LaborerWeapon));
    }

    private static void AssertFunctionalRig<T>(string sceneName, EnemyAttackKind attackKind,
        string primarySlot, params (string Name, EnemyFormalPartKind Kind)[] expectedSlots) where T : Node2D
    {
        T visual = Load<T>(sceneName);
        int corruptionZ = int.MaxValue;
        foreach ((string name, EnemyFormalPartKind kind) in expectedSlots)
        {
            EnemyFormalVisualPart part = visual.GetNodeOrNull<EnemyFormalVisualPart>(name);
            TestAssert.True(part != null && part.Kind == kind,
                $"{sceneName}/{name} is the real typed renderer at the stable replacement slot");
            if (name == "Corruption") corruptionZ = part!.ZIndex;
        }
        foreach ((string name, _) in expectedSlots)
            if (name != "Corruption")
                TestAssert.True(corruptionZ < visual.GetNode<Node2D>(name).ZIndex,
                    $"{sceneName}/Corruption renders behind {name}");

        Node2D primary = visual.GetNode<Node2D>(primarySlot);
        Transform2D before = primary.Transform;
        var futureArt = new Node2D { Position = new Vector2(7f, -3f), Rotation = .17f };
        primary.AddChild(futureArt);
        Transform2D futureBefore = primary.Transform * futureArt.Transform;
        Present(visual, Attack(attackKind, AttackPhase.Telegraph, .75f));
        Transform2D after = primary.Transform;
        TestAssert.True(TransformDelta(before, after) > .01f,
            $"{sceneName}/{primarySlot} itself owns the typed attack transform");
        TestAssert.True(futureArt.Position == new Vector2(7f, -3f) &&
            Mathf.IsEqualApprox(futureArt.Rotation, .17f) &&
            TransformDelta(futureBefore, primary.Transform * futureArt.Transform) > .01f,
            $"future art inserted at {sceneName}/{primarySlot} inherits the stable transform boundary");
        visual.Free();
    }

    private static float TransformDelta(Transform2D first, Transform2D second) =>
        first.Origin.DistanceTo(second.Origin) +
        Mathf.Abs(Mathf.AngleDifference(first.Rotation, second.Rotation));

    private static void RustShellMapsEveryState()
    {
        RustShellFormalVisual visual = Load<RustShellFormalVisual>("RustShellFormalVisual");
        AssertRust(visual, RustShellState.Idle, "idle");
        AssertRust(visual, RustShellState.Chase, "chase");
        AssertRust(visual, RustShellState.Telegraph, "telegraph");
        TestAssert.Equal("rust_crouch", visual.CurrentPoseMarker,
            "Rust-Shell telegraph exposes its crouch pose marker");
        AssertRust(visual, RustShellState.Attack, "attack");
        TestAssert.Equal("rust_thrust", visual.CurrentPoseMarker,
            "Rust-Shell attack exposes its thrust pose marker");
        AssertRust(visual, RustShellState.Recover, "recover");
        AssertRust(visual, RustShellState.Stunned, "stunned");
        AssertRust(visual, RustShellState.Dead, "dead");
        visual.Free();
    }

    private static void GraveLampMothMapsEveryState()
    {
        GraveLampMothFormalVisual visual = Load<GraveLampMothFormalVisual>("GraveLampMothFormalVisual");
        AssertMoth(visual, GraveLampMothState.Hover, "hover");
        AssertMoth(visual, GraveLampMothState.Align, "fly");
        AssertMoth(visual, GraveLampMothState.Telegraph, "telegraph");
        TestAssert.Equal("moth_folded_wings", visual.CurrentPoseMarker,
            "Moth telegraph exposes its folded-wing pose marker");
        AssertMoth(visual, GraveLampMothState.Dive, "dive");
        TestAssert.Equal("moth_dive", visual.CurrentPoseMarker,
            "Moth attack exposes its dive pose marker");
        AssertMoth(visual, GraveLampMothState.Recover, "recover");
        AssertMoth(visual, GraveLampMothState.ThreadStunned, "thread_pull");
        AssertMoth(visual, GraveLampMothState.Dead, "dead");
        visual.Free();
    }

    private static void MothTelegraphFoldsOpposedRealWingHinges()
    {
        GraveLampMothFormalVisual visual = Load<GraveLampMothFormalVisual>("GraveLampMothFormalVisual");
        EnemyFormalVisualPart farWing = visual.GetNodeOrNull<EnemyFormalVisualPart>("Wings/FarWing");
        EnemyFormalVisualPart nearWing = visual.GetNodeOrNull<EnemyFormalVisualPart>("Wings/NearWing");
        TestAssert.True(visual.GetNodeOrNull<Node2D>("Wings") != null,
            "Moth preserves the stable Wings translation anchor");
        TestAssert.True(farWing != null && farWing.Kind == EnemyFormalPartKind.MothFarWing,
            "Moth generated FarWing is the real typed renderer at Wings/FarWing");
        TestAssert.True(nearWing != null && nearWing.Kind == EnemyFormalPartKind.MothNearWing,
            "Moth generated NearWing is the real typed renderer at Wings/NearWing");

        Vector2 farTip = new(-50f, -3f);
        Vector2 nearTip = new(50f, -3f);
        float idleEndpointDistance = farWing.ToGlobal(farTip).DistanceTo(nearWing.ToGlobal(nearTip));
        Transform2D farIdle = farWing.Transform;
        Transform2D nearIdle = nearWing.Transform;

        Present(visual, Attack(EnemyAttackKind.MothDive, AttackPhase.Telegraph, .75f));

        TestAssert.True(TransformDelta(farIdle, farWing.Transform) > .01f &&
            TransformDelta(nearIdle, nearWing.Transform) > .01f,
            "Moth Telegraph moves both real wing hinges");
        TestAssert.True(farWing.Rotation * nearWing.Rotation < 0f &&
            Mathf.IsEqualApprox(Mathf.Abs(farWing.Rotation), Mathf.Abs(nearWing.Rotation)),
            "Moth Telegraph folds FarWing and NearWing with opposed equal hinge rotations");
        float foldedEndpointDistance = farWing.ToGlobal(farTip).DistanceTo(nearWing.ToGlobal(nearTip));
        TestAssert.True(foldedEndpointDistance < idleEndpointDistance,
            "Moth Telegraph brings the real folded wing endpoints closer than idle");
        visual.Free();
    }

    private static void BellBackLaborerMapsEveryState()
    {
        BellBackLaborerFormalVisual visual = Load<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual");
        AssertLaborer(visual, BellBackLaborerState.Idle, "idle");
        AssertLaborer(visual, BellBackLaborerState.Approach, "approach");
        AssertLaborer(visual, BellBackLaborerState.Guard, "guard");
        AssertLaborer(visual, BellBackLaborerState.Telegraph, "telegraph");
        TestAssert.Equal("laborer_counter_twist", visual.CurrentPoseMarker,
            "Laborer telegraph exposes its counter-twist pose marker");
        AssertLaborer(visual, BellBackLaborerState.Attack, "attack");
        TestAssert.Equal("laborer_sweep", visual.CurrentPoseMarker,
            "Laborer attack exposes its sweep pose marker");
        AssertLaborer(visual, BellBackLaborerState.Exposed, "exposed");
        AssertLaborer(visual, BellBackLaborerState.Recover, "recover");
        AssertLaborer(visual, BellBackLaborerState.Dead, "dead");
        visual.Free();
    }

    private static void AttackFramesLeadEnemySilhouettesAndKeepCorruptionBehindThem()
    {
        RustShellFormalVisual rust = Load<RustShellFormalVisual>("RustShellFormalVisual");
        Present(rust, Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Telegraph, .75f));
        PoseTransform2D rustWindupBody = Transform(rust, "CurrentBodyTransform");
        PoseTransform2D rustWindupWeapon = Transform(rust, "CurrentWeaponTransform");
        TestAssert.True(rustWindupBody.Position.Y > 4f && rustWindupWeapon.Position.X < -10f,
            "Rust lowers its body and retracts its needle before the thrust");
        Present(rust, Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Active, .25f));
        TestAssert.True(Transform(rust, "CurrentWeaponTransform").Position.X > rustWindupWeapon.Position.X + 4f,
            "Rust thrust advances the primary needle beyond its windup silhouette");
        AssertCorruptionBehindPrimary(rust, "Rust");
        rust.Free();

        GraveLampMothFormalVisual moth = Load<GraveLampMothFormalVisual>("GraveLampMothFormalVisual");
        Present(moth, Attack(EnemyAttackKind.MothDive, AttackPhase.Telegraph, .75f));
        TestAssert.True(Mathf.Abs(moth.CurrentFarWingTransform.RotationRadians) > .5f &&
            moth.CurrentFarWingTransform.RotationRadians * moth.CurrentNearWingTransform.RotationRadians < 0f &&
            Transform(moth, "CurrentLampTransform").Scale.X > 1.08f,
            "Moth folds its wings and locks its eye lamp before diving");
        Present(moth, Attack(EnemyAttackKind.MothDive, AttackPhase.Active, .75f));
        TestAssert.True(Transform(moth, "CurrentBodyTransform").RotationRadians > .5f,
            "Moth dive commits the body after the locked-lamp windup");
        AssertCorruptionBehindPrimary(moth, "Moth");
        moth.Free();

        BellBackLaborerFormalVisual laborer = Load<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual");
        Present(laborer, Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Telegraph, .75f));
        TestAssert.True(Transform(laborer, "CurrentFeetTransform").Position.Y > 2f &&
            Transform(laborer, "CurrentBodyTransform").RotationRadians < -.12f &&
            Transform(laborer, "CurrentBellTransform").RotationRadians > .08f,
            "Laborer plants its feet and counter-rotates the back bell before sweeping");
        Present(laborer, Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Active, .75f));
        TestAssert.True(Transform(laborer, "CurrentWeaponTransform").RotationRadians > -.1f,
            "Laborer sweep releases the weapon after its planted counter-twist");
        AssertCorruptionBehindPrimary(laborer, "Laborer");
        laborer.Free();
    }

    private static void EnemyAttackPhaseBoundariesLeadPrimaryAndBoundSecondaryMotion()
    {
        RustShellFormalVisual rust = Load<RustShellFormalVisual>("RustShellFormalVisual");
        rust.PresentAttack(Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Telegraph, .999f));
        PoseTransform2D rustTelegraph = rust.CurrentWeaponTransform;
        rust.PresentAttack(Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Active, 0f));
        AssertContinuous(rustTelegraph, rust.CurrentWeaponTransform, .20f,
            "Rust Telegraph end to Active start keeps its primary needle continuous");
        rust.PresentAttack(Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Active, .999f));
        PoseTransform2D rustActive = rust.CurrentWeaponTransform;
        rust.PresentAttack(Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Recovery, 0f));
        AssertContinuous(rustActive, rust.CurrentWeaponTransform, .20f,
            "Rust Active end to Recovery start keeps its primary needle continuous");
        rust.PresentAttack(Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Recovery, .75f));
        AssertDistinct(rustActive, rust.CurrentWeaponTransform,
            "Rust Recovery resolves to a readable counter-pose instead of holding Active");
        rust.PresentAttack(Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Telegraph, .10f));
        AssertPrimaryLeadsAndIsBounded(rust.CurrentBodyTransform, rust.CurrentShellTransform, "Rust");
        AssertContactWindow(rust, EnemyAttackKind.RustShellThrust, "Rust");
        rust.Free();

        GraveLampMothFormalVisual moth = Load<GraveLampMothFormalVisual>("GraveLampMothFormalVisual");
        moth.PresentAttack(Attack(EnemyAttackKind.MothDive, AttackPhase.Telegraph, .999f));
        PoseTransform2D mothTelegraph = moth.CurrentBodyTransform;
        moth.PresentAttack(Attack(EnemyAttackKind.MothDive, AttackPhase.Active, 0f));
        AssertContinuous(mothTelegraph, moth.CurrentBodyTransform, .20f,
            "Moth Telegraph end to Active start keeps its body continuous");
        moth.PresentAttack(Attack(EnemyAttackKind.MothDive, AttackPhase.Active, .999f));
        PoseTransform2D mothActive = moth.CurrentBodyTransform;
        moth.PresentAttack(Attack(EnemyAttackKind.MothDive, AttackPhase.Recovery, 0f));
        AssertContinuous(mothActive, moth.CurrentBodyTransform, .20f,
            "Moth Active end to Recovery start keeps its body continuous");
        moth.PresentAttack(Attack(EnemyAttackKind.MothDive, AttackPhase.Recovery, .75f));
        AssertDistinct(mothActive, moth.CurrentBodyTransform,
            "Moth Recovery resolves to a readable counter-pose instead of holding Active");
        moth.PresentAttack(Attack(EnemyAttackKind.MothDive, AttackPhase.Telegraph, .10f));
        AssertPrimaryLeadsAndIsBounded(moth.CurrentBodyTransform, moth.CurrentFarWingTransform, "Moth");
        AssertContactWindow(moth, EnemyAttackKind.MothDive, "Moth");
        moth.Free();

        BellBackLaborerFormalVisual laborer = Load<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual");
        laborer.PresentAttack(Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Telegraph, .999f));
        PoseTransform2D laborerTelegraph = laborer.CurrentWeaponTransform;
        laborer.PresentAttack(Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Active, 0f));
        AssertContinuous(laborerTelegraph, laborer.CurrentWeaponTransform, .20f,
            "Laborer Telegraph end to Active start keeps its hammer continuous");
        laborer.PresentAttack(Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Active, .999f));
        PoseTransform2D laborerActive = laborer.CurrentWeaponTransform;
        laborer.PresentAttack(Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Recovery, 0f));
        AssertContinuous(laborerActive, laborer.CurrentWeaponTransform, .20f,
            "Laborer Active end to Recovery start keeps its hammer continuous");
        laborer.PresentAttack(Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Recovery, .75f));
        AssertDistinct(laborerActive, laborer.CurrentWeaponTransform,
            "Laborer Recovery resolves to a readable counter-pose instead of holding Active");
        laborer.PresentAttack(Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Telegraph, .10f));
        AssertPrimaryLeadsAndIsBounded(laborer.CurrentWeaponTransform, laborer.CurrentBellTransform, "Laborer");
        AssertContactWindow(laborer, EnemyAttackKind.LaborerSweep, "Laborer");
        laborer.Free();
    }

    private static void AssertContinuous(PoseTransform2D first, PoseTransform2D second, float limit, string message)
    {
        TestAssert.True(first.Position.DistanceTo(second.Position) < limit &&
            Mathf.Abs(Mathf.AngleDifference(first.RotationRadians, second.RotationRadians)) < limit,
            message);
    }

    private static void AssertDistinct(PoseTransform2D first, PoseTransform2D second, string message)
    {
        TestAssert.True(first.Position.DistanceTo(second.Position) > .5f ||
            Mathf.Abs(Mathf.AngleDifference(first.RotationRadians, second.RotationRadians)) > .08f,
            message);
    }

    private static void AssertPrimaryLeadsAndIsBounded(PoseTransform2D primary, PoseTransform2D secondary, string label)
    {
        TestAssert.True(primary.Position.Length() <= 48f && Mathf.Abs(primary.RotationRadians) <= 1.7f &&
            secondary.Position.Length() <= 12f && Mathf.Abs(secondary.RotationRadians) <= .8f,
            $"{label} primary and delayed secondary transforms remain bounded");
        TestAssert.True(primary.Position.Length() + Mathf.Abs(primary.RotationRadians) > .01f &&
            secondary.Position.Length() + Mathf.Abs(secondary.RotationRadians) <
            primary.Position.Length() + Mathf.Abs(primary.RotationRadians),
            $"{label} primary silhouette leads the delayed secondary motion at phase start");
    }

    private static void AssertContactWindow(Node visual, EnemyAttackKind kind, string label)
    {
        Present(visual, Attack(kind, AttackPhase.Active, 0f));
        float first = Scalar(visual, "CurrentContactIntensity");
        Present(visual, Attack(kind, AttackPhase.Active, .08f));
        float afterWindow = Scalar(visual, "CurrentContactIntensity");
        TestAssert.True(first > .5f && afterWindow == 0f,
            $"{label} contact is a fixed attack-frame window rather than an Active-long render timer");
    }

    private static float Scalar(Node visual, string property)
    {
        object value = visual.GetType().GetProperty(property)?.GetValue(visual);
        TestAssert.True(value is float, $"{visual.GetType().Name} exposes deterministic {property}");
        return (float)value;
    }

    private static AttackPresentationFrame Attack(EnemyAttackKind kind, AttackPhase phase, float elapsed)
    {
        return new AttackPresentationFrame(new AttackTimelineFrame(701, kind, phase, 1f,
            1f - elapsed, Vector2.Zero, false), default);
    }

    private static void Present(Node visual, AttackPresentationFrame frame)
    {
        var method = visual.GetType().GetMethod("PresentAttack");
        TestAssert.True(method != null, $"{visual.GetType().Name} consumes the authoritative attack frame");
        method!.Invoke(visual, [frame]);
    }

    private static PoseTransform2D Transform(Node visual, string property)
    {
        var value = visual.GetType().GetProperty(property)?.GetValue(visual);
        TestAssert.True(value is PoseTransform2D,
            $"{visual.GetType().Name} exposes its actual {property} for formal silhouette verification");
        return (PoseTransform2D)value!;
    }

    private static void AssertCorruptionBehindPrimary(Node visual, string label)
    {
        PoseTransform2D corruption = Transform(visual, "CurrentCorruptionTransform");
        PoseTransform2D weapon = Transform(visual, "CurrentWeaponTransform");
        object value = visual.GetType().GetProperty("CurrentCorruptionBounds")?.GetValue(visual);
        TestAssert.True(value is Rect2, $"{label} exposes the conservative actual corruption draw bounds");
        Rect2 bounds = (Rect2)value!;
        TestAssert.True(corruption.Position.X < weapon.Position.X - 3f && bounds.End.X < 4f &&
            bounds.End.Y < 16f,
            $"{label} corruption stays behind the weapon, face heading, and planted-feet bounds");
    }

    private static void EnemyCorruptionBoundsEncloseDrawnCirclesAndHurtCuesUsePhysicsFrameBudget()
    {
        RustShellFormalVisual rust = Load<RustShellFormalVisual>("RustShellFormalVisual");
        rust.PresentAttack(Attack(EnemyAttackKind.RustShellThrust, AttackPhase.Active, .25f));
        AssertBoundsEncloseDrawnCircle(rust, new Rect2(-35f, -16f, 20f, 20f), "Rust");
        AssertHurtCueFrameBudget(rust, EnemyAttackKind.RustShellThrust, "Rust");
        rust.Free();

        GraveLampMothFormalVisual moth = Load<GraveLampMothFormalVisual>("GraveLampMothFormalVisual");
        moth.PresentAttack(Attack(EnemyAttackKind.MothDive, AttackPhase.Active, .25f));
        AssertBoundsEncloseDrawnCircle(moth, new Rect2(-49f, -17f, 22f, 22f), "Moth");
        AssertHurtCueFrameBudget(moth, EnemyAttackKind.MothDive, "Moth");
        moth.Free();

        BellBackLaborerFormalVisual laborer = Load<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual");
        laborer.PresentAttack(Attack(EnemyAttackKind.LaborerSweep, AttackPhase.Active, .25f));
        AssertBoundsEncloseDrawnCircle(laborer, new Rect2(-44f, -21f, 24f, 24f), "Laborer");
        AssertHurtCueFrameBudget(laborer, EnemyAttackKind.LaborerSweep, "Laborer");
        laborer.Free();
    }

    private static void EnemyIdleAndEveryAttackPhaseExposeTheirDrawnCorruptionBounds()
    {
        RustShellFormalVisual rust = Load<RustShellFormalVisual>("RustShellFormalVisual");
        AssertBoundsEncloseDrawnCircle(rust, new Rect2(-19f, -13f, 20f, 20f), "Rust idle");
        AssertBoundsForEveryAttackPhase(rust, EnemyAttackKind.RustShellThrust,
            new Rect2(-35f, -16f, 20f, 20f), "Rust");
        rust.Free();

        GraveLampMothFormalVisual moth = Load<GraveLampMothFormalVisual>("GraveLampMothFormalVisual");
        AssertBoundsEncloseDrawnCircle(moth, new Rect2(-31f, -14f, 22f, 22f), "Moth idle");
        AssertBoundsForEveryAttackPhase(moth, EnemyAttackKind.MothDive,
            new Rect2(-49f, -17f, 22f, 22f), "Moth");
        moth.Free();

        BellBackLaborerFormalVisual laborer = Load<BellBackLaborerFormalVisual>("BellBackLaborerFormalVisual");
        AssertBoundsEncloseDrawnCircle(laborer, new Rect2(-24f, -17f, 24f, 24f), "Laborer idle");
        AssertBoundsForEveryAttackPhase(laborer, EnemyAttackKind.LaborerSweep,
            new Rect2(-44f, -21f, 24f, 24f), "Laborer");
        laborer.Free();
    }

    private static void AssertBoundsForEveryAttackPhase(Node visual, EnemyAttackKind kind,
        Rect2 drawnCircleBounds, string label)
    {
        foreach (AttackPhase phase in new[] { AttackPhase.Telegraph, AttackPhase.Active, AttackPhase.Recovery })
        {
            Present(visual, Attack(kind, phase, .4f));
            AssertBoundsEncloseDrawnCircle(visual, drawnCircleBounds, $"{label} {phase}");
        }
    }

    private static void AssertBoundsEncloseDrawnCircle(Node visual, Rect2 drawnCircleBounds, string label)
    {
        object value = visual.GetType().GetProperty("CurrentCorruptionBounds")?.GetValue(visual);
        TestAssert.True(value is Rect2, $"{label} exposes actual conservative corruption draw bounds");
        Rect2 bounds = (Rect2)value!;
        TestAssert.True(bounds.Position.X <= drawnCircleBounds.Position.X &&
            bounds.Position.Y <= drawnCircleBounds.Position.Y &&
            bounds.End.X >= drawnCircleBounds.End.X && bounds.End.Y >= drawnCircleBounds.End.Y,
            $"{label} conservative corruption bounds enclose the transformed drawn circle");
    }

    private static void AssertHurtCueFrameBudget(Node visual, EnemyAttackKind kind, string label)
    {
        visual.Call("PlayCue", (int)PresentationCue.Hurt);
        TestAssert.True(Scalar(visual, "CurrentHurtFlashIntensity") > .5f,
            $"{label} Hurt cue immediately produces a visible white flash");
        Present(visual, Attack(kind, AttackPhase.Active, .08f));
        TestAssert.True(Scalar(visual, "CurrentContactIntensity") == 0f &&
            Scalar(visual, "CurrentHurtFlashIntensity") > .5f,
            $"{label} Hurt flash is independent from the expired attack contact envelope");
        visual.Call("_PhysicsProcess", 1d / 60d);
        visual.Call("_PhysicsProcess", 1d / 60d);
        visual.Call("_PhysicsProcess", 1d / 60d);
        TestAssert.True(Scalar(visual, "CurrentHurtFlashIntensity") == 0f,
            $"{label} Hurt flash clears after its deterministic physics-frame budget");
        visual.Call("PlayCue", (int)PresentationCue.AttackHit);
        TestAssert.True(Scalar(visual, "CurrentHurtFlashIntensity") == 0f,
            $"{label} non-Hurt cues do not fabricate a Hurt white flash");
    }

    private static void GeneratedEnemiesMountOnlyFormalVisuals()
    {
        AssertGenerated<RustShellFormalVisual>("res://scenes/enemies/RustShellSquire.tscn",
            "Hurtbox/HurtboxCollider", "AttackHitbox/AttackCollider", "ThreadTarget", "BodyCollider");
        AssertGenerated<GraveLampMothFormalVisual>("res://scenes/enemies/GraveLampMoth.tscn",
            "Hurtbox/HurtboxCollider", "AttackHitbox/AttackCollider", "ThreadTarget", "BodyCollider");
        AssertGenerated<BellBackLaborerFormalVisual>("res://scenes/enemies/BellBackLaborer.tscn",
            "Hurtbox/HurtboxCollider", "AttackHitbox/AttackCollider", "ThreadTarget/TargetCollision/Collision",
            "BodyCollider");
    }

    private static void AssertRust(RustShellFormalVisual visual, RustShellState state, string expected)
    {
        visual.ApplyRustShellFrame(new RustShellFrame(state, 0f, state == RustShellState.Attack, true), 1);
        TestAssert.Equal(expected, visual.CurrentAnimation, $"Rust-Shell {state} formal animation");
    }

    private static void AssertMoth(GraveLampMothFormalVisual visual, GraveLampMothState state, string expected)
    {
        visual.ApplyMothFrame(new GraveLampMothFrame(state, 0f, 0f, state == GraveLampMothState.Dive, true), 1, false);
        TestAssert.Equal(expected, visual.CurrentAnimation, $"Grave-Lamp Moth {state} formal animation");
    }

    private static void AssertLaborer(BellBackLaborerFormalVisual visual, BellBackLaborerState state, string expected)
    {
        visual.ApplyLaborerFrame(new BellBackLaborerFrame(state, 0f, state == BellBackLaborerState.Attack,
            state is BellBackLaborerState.Guard or BellBackLaborerState.Telegraph, true), 1);
        TestAssert.Equal(expected, visual.CurrentAnimation, $"Bell-Back Laborer {state} formal animation");
    }

    private static T Load<T>(string sceneName) where T : Node
    {
        string path = $"res://scenes/presentation/{sceneName}.tscn";
        PackedScene scene = GD.Load<PackedScene>(path);
        TestAssert.True(scene != null, $"{sceneName} leaf scene loads");
        return scene.Instantiate<T>();
    }

    private static void AssertGenerated<T>(string path, params string[] gameplayPaths) where T : Node
    {
        Node actor = GD.Load<PackedScene>(path).Instantiate();
        TestAssert.True(actor.GetNodeOrNull<T>("VisualRoot/Formal") != null,
            $"{path} mounts its formal visual");
        TestAssert.Equal(0, actor.FindChildren("*", "*PlaceholderVisual", true, false).Count,
            $"{path} contains no placeholder visual scripts");
        foreach (string gameplayPath in gameplayPaths)
            TestAssert.True(actor.GetNodeOrNull(gameplayPath) != null, $"{path} preserves {gameplayPath}");
        actor.Free();
    }
}
