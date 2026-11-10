using System.Collections.Generic;
using System.IO;
using Godot;
using Knight.Boss;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static class FallenBellkeeperFormalVisualTests
{
    public static void Run()
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/presentation/FallenBellkeeperFormalVisual.tscn");
        TestAssert.True(scene != null, "formal Fallen Bellkeeper leaf scene loads");
        FallenBellkeeperFormalVisual visual = scene.Instantiate<FallenBellkeeperFormalVisual>();
        BossPublishesAsymmetricFivePartAtlasContract(visual);
        BossAtlasContainsSeparatedSemanticPixels(visual);
        AssertFrame(visual, FallenBellkeeperState.Dormant, "dormant", true);
        AssertFrame(visual, FallenBellkeeperState.Intro, "intro", false);
        AssertFrame(visual, FallenBellkeeperState.PhaseOne, "open_phase", false);
        AssertAttackPose(visual, FallenBellkeeperAttack.Sweep, AttackPhase.Telegraph, "boss_sweep_windup");
        AssertAttackPose(visual, FallenBellkeeperAttack.Sweep, AttackPhase.Active, "boss_sweep_release");
        AssertAttackPose(visual, FallenBellkeeperAttack.LeapSlam, AttackPhase.Telegraph, "boss_leap_crouch");
        AssertAttackPose(visual, FallenBellkeeperAttack.LeapSlam, AttackPhase.Active, "boss_leap_impact");
        AssertAttackPose(visual, FallenBellkeeperAttack.GroundWave, AttackPhase.Telegraph, "boss_groundwave_raise");
        AssertAttackPose(visual, FallenBellkeeperAttack.GroundWave, AttackPhase.Active, "boss_groundwave_release");
        AssertFrame(visual, FallenBellkeeperState.Transition, "transition", false);
        AssertFrame(visual, FallenBellkeeperState.PhaseTwoArmored, "armor_closed", true);
        AssertFrame(visual, FallenBellkeeperState.HammerTelegraph, "hammer_telegraph", true);
        AssertAttackPose(visual, FallenBellkeeperAttack.HammerThrow, AttackPhase.Telegraph, "boss_hammer_windup");
        EveryBossAttackMovesAllReadablePartsUniquely(visual);
        BossPhaseBoundariesAndContactWindowRemainFrameDriven(visual);
        AssertFrame(visual, FallenBellkeeperState.HammerThrown, "hammer_thrown", true);
        AssertFrame(visual, FallenBellkeeperState.Exposed, "exposed", false);
        visual.PlayCue(PresentationCue.WeakpointOpened);
        TestAssert.Equal("chain_pull", visual.CurrentAnimation, "Boss chain-pull cue has a unique clip");
        visual.PlayCue(PresentationCue.Hurt);
        TestAssert.Equal("recovery", visual.CurrentAnimation, "Boss recovery cue has a unique clip");
        AssertFrame(visual, FallenBellkeeperState.Defeated, "defeated", false);
        TestAssert.True(visual.GetNodeOrNull<Node2D>("ArmorShell") != null &&
            visual.GetNodeOrNull<Node2D>("Hammer") != null &&
            visual.GetNodeOrNull<Node2D>("Chain") != null &&
            visual.GetNodeOrNull<Node2D>("Core") != null,
            "Boss armor, hammer, chain and core remain independent visual nodes");
        visual.Free();

        FallenBellkeeper boss = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn")
            .Instantiate<FallenBellkeeper>();
        TestAssert.True(boss.GetNodeOrNull<FallenBellkeeperFormalVisual>("VisualRoot/Formal") != null,
            "generated Boss mounts the formal visual");
        TestAssert.Equal(0, boss.FindChildren("*", "FallenBellkeeperPlaceholderVisual", true, false).Count,
            "generated Boss contains no placeholder visual");
        TestAssert.True(boss.GetNodeOrNull("Health") != null && boss.GetNodeOrNull("Hurtbox/HurtboxCollider") != null &&
            boss.GetNodeOrNull("AttackHitbox/AttackCollider") != null &&
            boss.GetNodeOrNull("ChainTarget/TargetCollision/Collision") != null,
            "formal Boss preserves health, hitbox and chain gameplay paths");
        boss.Free();
    }

    private static void BossPublishesAsymmetricFivePartAtlasContract(FallenBellkeeperFormalVisual visual)
    {
        string[] parts = ["Body", "ArmorShell", "Hammer", "Chain", "Core"];
        foreach (string name in parts)
        {
            Node2D part = visual.GetNode<Node2D>(name);
            TestAssert.Equal(name.ToLowerInvariant(), part.GetMeta("semantic_layer").AsString(),
                $"Boss {name} publishes its semantic atlas layer");
            Rect2 source = part.GetMeta("atlas_source").AsRect2();
            TestAssert.True(source.Size.X > 0f && source.Size.Y > 0f && source.End.X <= 512f &&
                source.End.Y <= 256f, $"Boss {name} source region stays inside the 512x256 atlas");
            TestAssert.True(part.GetMeta("pivot").AsVector2().IsEqualApprox(part.Position),
                $"Boss {name} publishes its actual replacement pivot");
        }

        Rect2 profile = visual.GetMeta("profile_bounds").AsRect2();
        Rect2 hammer = visual.GetNode<Node2D>("Hammer").GetMeta("draw_bounds").AsRect2();
        hammer.Position += visual.GetNode<Node2D>("Hammer").Position;
        Rect2 core = visual.GetNode<Node2D>("Core").GetMeta("draw_bounds").AsRect2();
        core.Position += visual.GetNode<Node2D>("Core").Position;
        TestAssert.True(profile == new Rect2(-86f, -82f, 220f, 165f) &&
            profile.GetCenter().X > 0f && hammer.End.X > core.End.X + 80f,
            "Boss remains a broad asymmetric five-part profile with a clear weapon side");
        TestAssert.True(visual.GetNodeOrNull<Polygon2D>("Body/FacingIndicator") != null &&
            visual.GetNodeOrNull<Line2D>("Body/FootContact") != null,
            "Boss body visibly publishes facing and foot-contact cues without adding rig parts");
    }

    private static void BossAtlasContainsSeparatedSemanticPixels(FallenBellkeeperFormalVisual visual)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "boss",
            "fallen-bellkeeper", "fallen-bellkeeper.png");
        Image atlas = Image.LoadFromFile(path);
        (string Role, string SignatureRgb)[] expected =
        [
            ("Body", "40353a"), ("Core", "75e3d4"), ("ArmorShell", "9f7437"),
            ("Chain", "d9b968"), ("Hammer", "9f7437")
        ];
        var sources = new List<Rect2I>();
        foreach ((string role, string signatureRgb) in expected)
        {
            Node2D part = visual.GetNode<Node2D>(role);
            Rect2 value = part.GetMeta("atlas_source").AsRect2();
            var source = new Rect2I((int)value.Position.X, (int)value.Position.Y,
                (int)value.Size.X, (int)value.Size.Y);
            foreach (Rect2I prior in sources)
                TestAssert.True(!prior.Intersects(source), $"Boss {role} semantic crop is non-overlapping");
            sources.Add(source);
            Rect2I pixels = OpaqueBounds(atlas, source);
            TestAssert.True(pixels.Size.X > 0 && pixels.Size.Y > 0 &&
                pixels.Position.X >= source.Position.X + 2 && pixels.Position.Y >= source.Position.Y + 2 &&
                pixels.End.X <= source.End.X - 2 && pixels.End.Y <= source.End.Y - 2,
                $"Boss {role} owns isolated authored pixels inside its semantic crop");
            TestAssert.True(ContainsRgb(atlas, source, new Color(signatureRgb)),
                $"Boss {role} crop owns its expected semantic value-group pixels");
            TestAssert.True(part.GetMeta("atlas_content").AsRect2() == new Rect2(pixels.Position, pixels.Size),
                $"Boss {role} publishes measured alpha-content bounds");
            Vector2 origin = part.GetMeta("atlas_origin").AsVector2();
            TestAssert.True(source.HasPoint(new Vector2I((int)origin.X, (int)origin.Y)),
                $"Boss {role} publishes an in-crop alignment origin");
            Rect2 reconstructed = new Rect2((Vector2)pixels.Position - origin, pixels.Size);
            TestAssert.True(part.GetMeta("draw_bounds").AsRect2().Grow(2f).Encloses(reconstructed),
                $"Boss {role} draw bounds reconstruct its actual semantic pixels");
        }

        for (int y = 0; y < atlas.GetHeight(); y++)
        for (int x = 0; x < atlas.GetWidth(); x++)
        {
            if (atlas.GetPixel(x, y).A <= .05f) continue;
            int owners = 0;
            foreach (Rect2I source in sources)
                if (source.HasPoint(new Vector2I(x, y))) owners++;
            TestAssert.True(owners == 1,
                $"Boss opaque atlas pixel ({x},{y}) belongs to exactly one semantic crop");
        }
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

    private static void AssertFrame(FallenBellkeeperFormalVisual visual, FallenBellkeeperState state,
        string expectedAnimation, bool armorClosed)
    {
        visual.ApplyBossFrame(new FallenBellkeeperFrame(state, FallenBellkeeperAttack.None, false,
            armorClosed, state == FallenBellkeeperState.HammerThrown, true), 1);
        TestAssert.Equal(expectedAnimation, visual.CurrentAnimation,
            $"Fallen Bellkeeper {state} formal animation");
    }

    private static void AssertAttackPose(FallenBellkeeperFormalVisual visual, FallenBellkeeperAttack attack,
        AttackPhase phase, string expected)
    {
        var timeline = new AttackTimelineFrame(12, attack switch
        {
            FallenBellkeeperAttack.Sweep => EnemyAttackKind.BossSweep,
            FallenBellkeeperAttack.LeapSlam => EnemyAttackKind.BossLeapSlam,
            FallenBellkeeperAttack.GroundWave => EnemyAttackKind.BossGroundWave,
            _ => EnemyAttackKind.BossHammerThrow
        }, phase, 1f, .5f, Vector2.Zero, phase != AttackPhase.Telegraph);
        visual.ApplyBossFrame(new FallenBellkeeperFrame(
            attack == FallenBellkeeperAttack.HammerThrow
                ? FallenBellkeeperState.HammerTelegraph
                : FallenBellkeeperState.PhaseOne,
            attack, phase == AttackPhase.Active, false, false, true, timeline), 1);
        TestAssert.Equal(expected, visual.CurrentAttackPose, $"Boss {attack} {phase} pose marker");
    }

    private static void EveryBossAttackMovesAllReadablePartsUniquely(FallenBellkeeperFormalVisual visual)
    {
        var poses = new System.Collections.Generic.Dictionary<FallenBellkeeperAttack,
            (Transform2D Body, Transform2D Armor, Transform2D Hammer, Transform2D Chain)>();
        foreach (FallenBellkeeperAttack attack in new[]
                 {
                     FallenBellkeeperAttack.Sweep,
                     FallenBellkeeperAttack.LeapSlam,
                     FallenBellkeeperAttack.GroundWave,
                     FallenBellkeeperAttack.HammerThrow
                 })
        {
            EnemyAttackKind kind = attack switch
            {
                FallenBellkeeperAttack.Sweep => EnemyAttackKind.BossSweep,
                FallenBellkeeperAttack.LeapSlam => EnemyAttackKind.BossLeapSlam,
                FallenBellkeeperAttack.GroundWave => EnemyAttackKind.BossGroundWave,
                _ => EnemyAttackKind.BossHammerThrow
            };
            var timeline = new AttackTimelineFrame(900 + (ulong)attack, kind, AttackPhase.Active,
                1f, .65f, Vector2.Zero, true);
            visual.ApplyBossFrame(new FallenBellkeeperFrame(FallenBellkeeperState.PhaseOne,
                attack, true, false, false, true, timeline), 1);
            poses.Add(attack, (
                visual.GetNode<Node2D>("Body").Transform,
                visual.GetNode<Node2D>("ArmorShell").Transform,
                visual.GetNode<Node2D>("Hammer").Transform,
                visual.GetNode<Node2D>("Chain").Transform));
        }

        foreach ((FallenBellkeeperAttack firstAttack, var first) in poses)
        foreach ((FallenBellkeeperAttack secondAttack, var second) in poses)
        {
            if (firstAttack >= secondAttack) continue;
            TestAssert.True(TransformDelta(first.Body, second.Body) > .01f &&
                TransformDelta(first.Armor, second.Armor) > .01f &&
                TransformDelta(first.Hammer, second.Hammer) > .01f &&
                TransformDelta(first.Chain, second.Chain) > .01f,
                $"Boss {firstAttack} and {secondAttack} keep Body, Armor, Hammer, and Chain transforms distinct");
        }
    }

    private static float TransformDelta(Transform2D first, Transform2D second) =>
        first.Origin.DistanceTo(second.Origin) +
        Mathf.Abs(Mathf.AngleDifference(first.Rotation, second.Rotation));

    private static void BossPhaseBoundariesAndContactWindowRemainFrameDriven(FallenBellkeeperFormalVisual visual)
    {
        visual.PresentAttack(BossAttack(AttackPhase.Telegraph, .999f));
        Transform2D telegraphHammer = visual.GetNode<Node2D>("Hammer").Transform;
        visual.PresentAttack(BossAttack(AttackPhase.Active, 0f));
        AssertContinuous(telegraphHammer, visual.GetNode<Node2D>("Hammer").Transform,
            "Boss Telegraph end to Active start keeps the hammer continuous");
        visual.PresentAttack(BossAttack(AttackPhase.Active, .999f));
        Transform2D activeHammer = visual.GetNode<Node2D>("Hammer").Transform;
        visual.PresentAttack(BossAttack(AttackPhase.Recovery, 0f));
        AssertContinuous(activeHammer, visual.GetNode<Node2D>("Hammer").Transform,
            "Boss Active end to Recovery start keeps the hammer continuous");
        visual.PresentAttack(BossAttack(AttackPhase.Recovery, .75f));
        TestAssert.True(TransformDelta(activeHammer, visual.GetNode<Node2D>("Hammer").Transform) > .5f,
            "Boss Recovery resolves to a readable counter-pose instead of holding Active");
        visual.PresentAttack(BossAttack(AttackPhase.Active, 0f));
        float contact = Scalar(visual, "CurrentContactIntensity");
        visual.PresentAttack(BossAttack(AttackPhase.Active, .08f));
        TestAssert.True(contact > .5f && Scalar(visual, "CurrentContactIntensity") == 0f,
            "Boss contact is a fixed attack-frame window rather than an Active-long render timer");
    }

    private static AttackPresentationFrame BossAttack(AttackPhase phase, float elapsed) => new(
        new AttackTimelineFrame(222, EnemyAttackKind.BossGroundWave, phase, 1f, 1f - elapsed,
            Vector2.Zero, true), default);

    private static void AssertContinuous(Transform2D first, Transform2D second, string message) =>
        TestAssert.True(first.Origin.DistanceTo(second.Origin) < .25f &&
            Mathf.Abs(Mathf.AngleDifference(first.Rotation, second.Rotation)) < .25f, message);

    private static float Scalar(Node visual, string property)
    {
        object value = visual.GetType().GetProperty(property)?.GetValue(visual);
        TestAssert.True(value is float, $"{visual.GetType().Name} exposes deterministic {property}");
        return (float)value;
    }
}
