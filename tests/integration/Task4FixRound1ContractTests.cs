using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Audio;
using Knight.Combat;
using Knight.Presentation;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class Task4FixRound1ContractTests
{
    public static async Task RunCase(SceneTree tree, string testCase)
    {
        switch (testCase)
        {
            case "cross-source": await CrossSourceAudioIdentity(tree); break;
            case "source-removal": await SourceIsolatedVfxRemovalAndReuse(tree); break;
            case "compatibility": await SourceQualifiedOneShotCompatibility(tree); break;
            case "rotation": await RotatedAuthoritativeGeometry(tree); break;
            case "recovery": await CompleteRecoveryMapping(tree); break;
            case "expiry": await NaturalExpiryPrunesBookkeeping(tree); break;
            case "proof-route": RealPublisherProofRouteContainsNoSyntheticFrames(); break;
            default:
                await CrossSourceAudioIdentity(tree);
                await SourceIsolatedVfxRemovalAndReuse(tree);
                await SourceQualifiedOneShotCompatibility(tree);
                await RotatedAuthoritativeGeometry(tree);
                await CompleteRecoveryMapping(tree);
                await NaturalExpiryPrunesBookkeeping(tree);
                RealPublisherProofRouteContainsNoSyntheticFrames();
                break;
        }
    }

    private static async Task CrossSourceAudioIdentity(SceneTree tree)
    {
        (Node2D fixture, PrologueAudioDirector director) = await AudioFixture(tree, "CrossSourceAudio");
        var sourceA = new Node2D { Name = "EnemyA" };
        var sourceB = new Node2D { Name = "EnemyB" };
        fixture.AddChild(sourceA);
        fixture.AddChild(sourceB);
        int before = director.FramePlaybackCount;
        AttackPresentationFrame frame = Attack(7, EnemyAttackKind.RustShellThrust, AttackPhase.Telegraph,
            Circle(new Vector2(220, 180)));
        director.PlayAttackFrame(sourceA, frame, sourceA.GlobalPosition);
        director.PlayAttackFrame(sourceB, frame, sourceB.GlobalPosition);
        director.PlayAttackFrame(sourceA, frame, sourceA.GlobalPosition);
        director.PlayAttackFrame(sourceB, frame, sourceB.GlobalPosition);
        TestAssert.Equal(before + 2, director.FramePlaybackCount,
            "cross-source same local AttackId and phase play once per live source");
        await Dispose(tree, fixture);
    }

    private static async Task SourceIsolatedVfxRemovalAndReuse(SceneTree tree)
    {
        (Node2D fixture, PrologueVfxDirector director) = await VfxFixture(tree, "SourceRemovalVfx");
        var sourceA = new Node2D { Name = "EnemyA" };
        var sourceB = new Node2D { Name = "EnemyB" };
        fixture.AddChild(sourceA);
        fixture.AddChild(sourceB);
        AttackPresentationFrame frame = Attack(9, EnemyAttackKind.RustShellThrust, AttackPhase.Telegraph,
            Circle(new Vector2(210, 180)));
        director.PresentAttackFrame(sourceA, frame);
        director.PresentAttackFrame(sourceB, frame);
        TestAssert.Equal(2, director.TypedEffectCount,
            "same local AttackId owns one VFX effect per source");
        director.PresentAttackFrame(sourceA, default);
        TestAssert.Equal(1, director.TypedEffectCount,
            "source A exit removes only source A's same-ID effect");
        sourceA.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        var reusedSource = new Node2D { Name = "EnemyAReusedLocalId" };
        fixture.AddChild(reusedSource);
        director.PresentAttackFrame(reusedSource, frame);
        TestAssert.Equal(2, director.TypedEffectCount,
            "a later source safely reuses a freed source's local AttackId");
        await Dispose(tree, fixture);
    }

    private static async Task SourceQualifiedOneShotCompatibility(SceneTree tree)
    {
        (Node2D fixture, PrologueAudioDirector director) = await AudioFixture(tree, "CompatibilityAudio");
        var enemy = new Node2D { Name = "Enemy" };
        var player = new Node2D { Name = "PlayerCoordinator" };
        fixture.AddChild(enemy);
        fixture.AddChild(player);
        director.PlayAttackFrame(enemy,
            Attack(11, EnemyAttackKind.RustShellThrust, AttackPhase.Active, Circle(new Vector2(180, 180))),
            enemy.GlobalPosition);
        PrologueAudioDirector.PlayPresentationCueFromTree(player, PresentationCue.AttackStarted,
            player.GlobalPosition);
        TestAssert.Equal(PrologueAudioCue.PlayerAttack, director.LastCue,
            "unrelated player AttackStarted is never suppressed by an enemy typed token");

        director.PlayAttackFrame(enemy,
            Attack(12, EnemyAttackKind.RustShellThrust, AttackPhase.Active, Circle(new Vector2(180, 180))),
            enemy.GlobalPosition);
        PrologueAudioDirector.PlayPresentationCueFromTree(enemy, PresentationCue.AttackStarted,
            enemy.GlobalPosition);
        TestAssert.Equal(PrologueAudioCue.RustShellAttack, director.LastCue,
            "adjacent matching legacy cue is suppressed exactly once");
        PrologueAudioDirector.PlayPresentationCueFromTree(enemy, PresentationCue.AttackStarted,
            enemy.GlobalPosition);
        TestAssert.Equal(PrologueAudioCue.PlayerAttack, director.LastCue,
            "consumed compatibility token cannot suppress a second legacy cue");

        director.PlayAttackFrame(enemy,
            Attack(13, EnemyAttackKind.RustShellThrust, AttackPhase.Active, Circle(new Vector2(180, 180))),
            enemy.GlobalPosition);
        PrologueAudioDirector.PlayPresentationCueFromTree(enemy, PresentationCue.Hurt, enemy.GlobalPosition);
        PrologueAudioDirector.PlayPresentationCueFromTree(enemy, PresentationCue.AttackStarted,
            enemy.GlobalPosition);
        TestAssert.Equal(PrologueAudioCue.PlayerAttack, director.LastCue,
            "stale non-adjacent compatibility token suppresses nothing");
        await Dispose(tree, fixture);
    }

    private static async Task RotatedAuthoritativeGeometry(SceneTree tree)
    {
        (Node2D fixture, PrologueVfxDirector director) = await VfxFixture(tree, "RotationVfx");
        var source = new Node2D { Name = "RotatedAttackSource" };
        fixture.AddChild(source);

        float rectangleRotation = .63f;
        Transform2D rectangleWorld = new(rectangleRotation, new Vector2(260, 190));
        var rectangleCollider = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(160, 42) }
        };
        AttackShapeFrame rectangle = AttackShapeFactory.FromCollision(rectangleCollider, rectangleWorld,
            AttackShapeKind.Rectangle);
        director.PresentAttackFrame(source,
            Attack(21, EnemyAttackKind.RustShellThrust, AttackPhase.Telegraph, rectangle));
        Vector2[] rectangleRendered = RenderedWorldOutline(CurrentEffect(director, AttackShapeKind.Rectangle));
        Vector2[] rectangleExpected =
        [
            rectangleWorld * new Vector2(-80, -21), rectangleWorld * new Vector2(80, -21),
            rectangleWorld * new Vector2(80, 21), rectangleWorld * new Vector2(-80, 21)
        ];
        AssertPointsNear(rectangleExpected, rectangleRendered,
            "rotated Rectangle visual global geometry matches collision authority");

        float capsuleRotation = -.48f;
        Transform2D capsuleWorld = new(capsuleRotation, new Vector2(350, 230));
        var capsuleCollider = new CollisionShape2D
        {
            Shape = new CapsuleShape2D { Radius = 18f, Height = 150f }
        };
        AttackShapeFrame capsule = AttackShapeFactory.FromCollision(capsuleCollider, capsuleWorld,
            AttackShapeKind.Capsule);
        director.PresentAttackFrame(source,
            Attack(22, EnemyAttackKind.BossHammerThrow, AttackPhase.Telegraph, capsule));
        Vector2[] capsuleRendered = RenderedWorldOutline(CurrentEffect(director, AttackShapeKind.Capsule));
        float actualCapsuleAxis = PrincipalAxis(capsuleRendered);
        float expectedCapsuleAxis = capsuleRotation + Mathf.Pi * .5f;
        TestAssert.True(AxisDistance(actualCapsuleAxis, expectedCapsuleAxis) <= .02f,
            $"rotated Capsule visual major axis matches collision authority; " +
            $"expected={expectedCapsuleAxis:F3} actual={actualCapsuleAxis:F3}");

        float polygonRotation = .37f;
        Transform2D polygonWorld = new(polygonRotation, new Vector2(430, 210));
        Vector2[] localPolygon = [new(-52, -18), new(42, -31), new(74, 5), new(20, 38), new(-46, 24)];
        var polygonCollider = new CollisionShape2D
        {
            Shape = new ConvexPolygonShape2D { Points = localPolygon }
        };
        AttackShapeFrame polygon = AttackShapeFactory.FromCollision(polygonCollider, polygonWorld,
            AttackShapeKind.ConvexPolygon);
        director.PresentAttackFrame(source,
            Attack(23, EnemyAttackKind.BossSweep, AttackPhase.Telegraph, polygon));
        AssertPointsNear(polygon.Points,
            RenderedWorldOutline(CurrentEffect(director, AttackShapeKind.ConvexPolygon)),
            "rotated ConvexPolygon visual global points match collision authority");

        rectangleCollider.Free();
        capsuleCollider.Free();
        polygonCollider.Free();
        await Dispose(tree, fixture);
    }

    private static async Task CompleteRecoveryMapping(SceneTree tree)
    {
        (Node2D fixture, PrologueAudioDirector director) = await AudioFixture(tree, "RecoveryAudio");
        var source = new Node2D { Name = "AttackSource" };
        fixture.AddChild(source);
        (EnemyAttackKind Kind, string Telegraph, string Active, string Recovery)[] matrix =
        [
            (EnemyAttackKind.RustShellThrust, "RustShellTelegraph", "RustShellAttack", "RustShellRecovery"),
            (EnemyAttackKind.MothDive, "MothTelegraph", "MothDive", "MothRecovery"),
            (EnemyAttackKind.LaborerSweep, "LaborerTelegraph", "LaborerAttack", "LaborerRecovery"),
            (EnemyAttackKind.BossSweep, "BossSweepTelegraph", "BossAttack", "BossRecovery"),
            (EnemyAttackKind.BossLeapSlam, "BossLeapTelegraph", "BossAttack", "BossRecovery"),
            (EnemyAttackKind.BossGroundWave, "BossGroundWaveTelegraph", "BossAttack", "BossRecovery"),
            (EnemyAttackKind.BossHammerThrow, "BossHammerTelegraph", "BossHammer", "BossRecovery")
        ];
        ulong attackId = 100;
        foreach ((EnemyAttackKind kind, string telegraph, string active, string recovery) in matrix)
        {
            foreach ((AttackPhase phase, string expectedName) in new[]
            {
                (AttackPhase.Telegraph, telegraph), (AttackPhase.Active, active),
                (AttackPhase.Recovery, recovery)
            })
            {
                TestAssert.True(Enum.TryParse(expectedName, out PrologueAudioCue expected),
                    $"recovery sound enum declares actor-specific cue {expectedName}");
                director.PlayAttackFrame(source,
                    Attack(attackId++, kind, phase, Circle(new Vector2(200, 180))), source.GlobalPosition);
                TestAssert.Equal(expected, director.LastCue,
                    $"complete attack-kind mapping resolves {kind} {phase} to {expectedName}");
            }
        }
        await Dispose(tree, fixture);
    }

    private static async Task NaturalExpiryPrunesBookkeeping(SceneTree tree)
    {
        (Node2D fixture, PrologueVfxDirector director) = await VfxFixture(tree, "ExpiryVfx");
        var source = new Node2D { Name = "LongRoomSource" };
        fixture.AddChild(source);
        for (ulong id = 1; id <= 64; id++)
            director.PresentAttackFrame(source,
                Attack(id, EnemyAttackKind.RustShellThrust, AttackPhase.Recovery,
                    Circle(new Vector2(160 + id, 180))));
        foreach (Node child in director.GetNode<Node2D>("World").GetChildren())
            if (child is PrologueTypedVfxEffect effect) effect._Process(1d);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        director._Process(0d);
        int count = AttackEffectDictionary(director).Count;
        TestAssert.Equal(0, count,
            "naturally expired attack effects leave no dead source-qualified dictionary wrappers");
        await Dispose(tree, fixture);
    }

    private static void RealPublisherProofRouteContainsNoSyntheticFrames()
    {
        string source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(),
            "tests", "presentation", "PrologueVisualAudit.cs"));
        TestAssert.True(!source.Contains("ProofAttack(", StringComparison.Ordinal) &&
                        !source.Contains("ProofThread(", StringComparison.Ordinal) &&
                        !source.Contains("vfx.PresentAttackFrame(", StringComparison.Ordinal) &&
                        !source.Contains("vfx.PresentThreadFrame(", StringComparison.Ordinal),
            "Task 4 proof route uses no synthetic proof frames or direct director presentation calls");
        foreach (string required in new[]
        {
            "RustShellSquire", "GraveLampMoth", "BellBackLaborer", "FallenBellkeeper",
            "AttackFrameChanged", "CastFrameChanged", "ThreadTarget2D", "Input.ActionPress"
        })
            TestAssert.True(source.Contains(required, StringComparison.Ordinal),
                $"Task 4 proof route names real publisher/input contract {required}");
    }

    private static async Task<(Node2D Fixture, PrologueAudioDirector Director)> AudioFixture(
        SceneTree tree, string name)
    {
        var fixture = new Node2D { Name = name };
        PrologueAudioDirector director = PrologueAudioDirector.CreateRuntimeTree();
        fixture.AddChild(director);
        tree.Root.AddChild(fixture);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        return (fixture, director);
    }

    private static async Task<(Node2D Fixture, PrologueVfxDirector Director)> VfxFixture(
        SceneTree tree, string name)
    {
        var fixture = new Node2D { Name = name };
        PrologueVfxDirector director = GD.Load<PackedScene>("res://scenes/vfx/PrologueVfx.tscn")
            .Instantiate<PrologueVfxDirector>();
        fixture.AddChild(director);
        tree.Root.AddChild(fixture);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        return (fixture, director);
    }

    private static AttackPresentationFrame Attack(ulong id, EnemyAttackKind kind, AttackPhase phase,
        AttackShapeFrame shape) => new AttackPresentationFrame(new AttackTimelineFrame(id, kind, phase, .5f, .4f,
        shape.Origin, true), shape).Validate();

    private static AttackShapeFrame Circle(Vector2 origin) => new(AttackShapeKind.Circle, origin,
        new Vector2(20, 20), 0f, Array.Empty<Vector2>());

    private static PrologueTypedVfxEffect CurrentEffect(PrologueVfxDirector director, AttackShapeKind kind)
    {
        foreach (Node child in director.GetNode<Node2D>("World").GetChildren())
            if (child is PrologueTypedVfxEffect effect && !effect.IsQueuedForDeletion() &&
                effect.AttackShape.Kind == kind) return effect;
        throw new InvalidOperationException($"No live typed {kind} attack effect was rendered.");
    }

    private static Vector2[] RenderedWorldOutline(PrologueTypedVfxEffect effect)
    {
        MethodInfo method = typeof(PrologueTypedVfxEffect).GetMethod("AttackOutline",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new InvalidOperationException("Typed effect exposes no rendered attack outline.");
        Vector2[] local = (Vector2[])method.Invoke(effect, null);
        var world = new Vector2[local.Length];
        for (int index = 0; index < local.Length; index++) world[index] = effect.GlobalTransform * local[index];
        return world;
    }

    private static IDictionary AttackEffectDictionary(PrologueVfxDirector director) =>
        (IDictionary)(typeof(PrologueVfxDirector).GetField("_attackEffects",
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(director) ??
            throw new InvalidOperationException("VFX director has no attack effect dictionary."));

    private static void AssertPointsNear(System.Collections.Generic.IReadOnlyList<Vector2> expected,
        System.Collections.Generic.IReadOnlyList<Vector2> actual, string message)
    {
        TestAssert.Equal(expected.Count, actual.Count, message + " point count");
        for (int index = 0; index < expected.Count; index++)
            TestAssert.True(expected[index].DistanceTo(actual[index]) <= .05f,
                $"{message}; index={index} expected={expected[index]} actual={actual[index]}");
    }

    private static float PrincipalAxis(Vector2[] points)
    {
        Vector2 center = Vector2.Zero;
        foreach (Vector2 point in points) center += point;
        center /= points.Length;
        float xx = 0f, xy = 0f, yy = 0f;
        foreach (Vector2 point in points)
        {
            Vector2 delta = point - center;
            xx += delta.X * delta.X;
            xy += delta.X * delta.Y;
            yy += delta.Y * delta.Y;
        }
        return .5f * Mathf.Atan2(2f * xy, xx - yy);
    }

    private static float AxisDistance(float a, float b)
    {
        float difference = Mathf.Abs(Mathf.AngleDifference(a, b));
        return Mathf.Min(difference, Mathf.Abs(Mathf.Pi - difference));
    }

    private static async Task Dispose(SceneTree tree, Node fixture)
    {
        if (fixture.GetNodeOrNull<PrologueAudioDirector>("Audio") != null)
        {
            await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, fixture);
            return;
        }
        fixture.QueueFree();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }
}
