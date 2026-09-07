using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Boss;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Vfx;

namespace Knight.Tests.Integration;

public static class PrologueEnvironmentRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        PackedScene kit = GD.Load<PackedScene>("res://scenes/presentation/PrologueEnvironmentKit.tscn");
        TestAssert.True(kit != null, "formal environment kit loads");
        RoomFormalVisual art = kit.Instantiate<RoomFormalVisual>();
        float before = art.AtmospherePhase;
        art.AdvanceAtmosphere(0.5f);
        TestAssert.True(art.AtmospherePhase > before,
            "formal room atmosphere advances deterministically");

        ForegroundOccluder occluder = art.GetNodeOrNull<ForegroundOccluder>("Front/ForegroundOccluder");
        TestAssert.True(occluder != null, "environment kit includes a foreground occluder");
        occluder.SetPlayerBehind(true);
        occluder.AdvanceFade(1.0f);
        TestAssert.True(occluder.CurrentOpacity <= 0.35f,
            "foreground fades enough to preserve combat readability when player is behind it");
        occluder.SetPlayerBehind(false);
        occluder.AdvanceFade(1.0f);
        TestAssert.True(occluder.CurrentOpacity >= 0.7f,
            "foreground restores after the player leaves the occluded area");
        art.Free();

        PrologueRoom courtyard = GD.Load<PackedScene>(
            "res://scenes/prologue/rooms/AwakeningCourtyard.tscn").Instantiate<PrologueRoom>();
        tree.Root.AddChild(courtyard);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        RoomFormalVisual roomArt = courtyard.GetNode<RoomFormalVisual>("Art");
        RoomVisualDescriptor descriptor = RoomVisualCatalog.For(PrologueRoomId.AwakeningCourtyard);
        roomArt.Present(descriptor, new Vector2(20f, -10f), []);
        Vector2 farPosition = roomArt.GetNode<Node2D>("Far").Position;
        TestAssert.Near(-1.6f, farPosition.X, .0001f,
            "far role applies the courtyard descriptor horizontal parallax at camera velocity");
        TestAssert.Near(.8f, farPosition.Y, .0001f,
            "far role applies the courtyard descriptor vertical parallax at camera velocity");
        roomArt._Process(0f);
        TestAssert.Near(-1.6f, roomArt.GetNode<Node2D>("Far").Position.X, .0001f,
            "live protected-region discovery does not erase caller-supplied camera parallax");
        TestAssert.Equal(roomArt.GetNode<Node2D>("Far").Position,
            roomArt.GetNode<Node2D>("Back").Position,
            "Far and Back remain one stable far-depth role");
        Node2D platformLeaf = roomArt.GetNode<Node2D>("Platforms");
        TestAssert.Equal(Vector2.Zero, platformLeaf.Position,
            "non-zero camera velocity never shifts collision-derived platform skins");
        foreach (Node child in courtyard.GetNode("Geometry").GetChildren())
        {
            if (child is not StaticBody2D platform) continue;
            Polygon2D skin = platformLeaf.GetNodeOrNull<Polygon2D>(new NodePath(platform.Name.ToString()));
            if (skin == null) continue;
            TestAssert.Equal(platform.Position, platformLeaf.Position + skin.Position,
                $"non-zero parallax keeps {platform.Name} skin aligned to real collision");
        }

        ForegroundOccluder[] leaves =
        [
            roomArt.GetNode<ForegroundOccluder>("Front/LeftForeground"),
            roomArt.GetNode<ForegroundOccluder>("Front/RightForeground")
        ];
        Rect2 combatRegion = new(-620f, -40f, 180f, 180f);
        roomArt.Present(descriptor, Vector2.Zero, [combatRegion]);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        leaves[0].AdvanceFade(1f);
        leaves[1].AdvanceFade(1f);
        TestAssert.True(leaves[0].PlayerBehind,
            "caller-supplied named combat region survives multiple live _Process frames");
        TestAssert.True(leaves[0].CurrentOpacity <= .35f,
            "only the foreground leaf overlapping the named combat region fades");
        TestAssert.True(leaves[1].CurrentOpacity >= .7f,
            "a non-overlapping foreground leaf stays readable instead of fading the whole room");
        roomArt.Present(descriptor, Vector2.Zero, []);
        leaves[0].AdvanceFade(1f);
        TestAssert.True(leaves[0].CurrentOpacity >= .7f,
            "Present is safe with zero protected regions and restores the faded leaf");
        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, courtyard);

        await AssertLiveProtectionAdapter(tree);
    }

    private static async Task AssertLiveProtectionAdapter(SceneTree tree)
    {
        var harness = new Node2D { Name = "LevelRoot" };
        var playerRoot = new Node2D { Name = "PlayerRoot" };
        var roomRoot = new Node2D { Name = "RoomRoot" };
        var currentRoom = new Node2D { Name = "AwakeningCourtyard" };
        harness.AddChild(playerRoot);
        harness.AddChild(roomRoot);
        roomRoot.AddChild(currentRoom);
        var art = new RoomFormalVisual { Name = "Art", Room = PrologueRoomId.AwakeningCourtyard };
        foreach (string name in new[] { "Far", "Back", "Mid", "Platforms", "Props", "Front", "Atmosphere" })
            art.AddChild(new Node2D { Name = name });
        currentRoom.AddChild(art);
        tree.Root.AddChild(harness);

        (string Name, Vector2 Center)[] cases =
        [
            ("Player", new Vector2(100, 100)),
            ("Enemy", new Vector2(230, 100)),
            ("ThreadTarget", new Vector2(360, 100)),
            ("ThreadLine", new Vector2(490, 100)),
            // Keep the independently toggled telegraph outside the live Boss hammer silhouette.
            ("AttackTelegraph", new Vector2(620, 360)),
            ("BossFeet", new Vector2(750, 100)),
            ("GroundDanger", new Vector2(880, 100)),
            ("GroupOnlyPlayer", new Vector2(1270, 100)),
            ("BossHammer", new Vector2(620, 100))
        ];
        foreach ((string name, Vector2 center) in cases)
        {
            var leaf = new ForegroundOccluder
            {
                Name = name, Position = center, CoverageRect = new Rect2(-48, -64, 96, 128),
                FadeSpeed = 100f
            };
            leaf.AddChild(new Polygon2D
            {
                Name = "Column", Color = new Color("243438"),
                Polygon = [new Vector2(-48, -64), new Vector2(48, -64), new Vector2(48, 64), new Vector2(-48, 64)]
            });
            art.GetNode<Node2D>("Front").AddChild(leaf);
        }
        Vector2 inactiveCenter = new(1010, 100);
        var inactiveLeaf = new ForegroundOccluder
        {
            Name = "InactiveAttack", Position = inactiveCenter,
            CoverageRect = new Rect2(-48, -64, 96, 128), FadeSpeed = 100f
        };
        inactiveLeaf.AddChild(new Polygon2D
        {
            Name = "Column", Color = new Color("243438"),
            Polygon = [new Vector2(-48, -64), new Vector2(48, -64), new Vector2(48, 64), new Vector2(-48, 64)]
        });
        art.GetNode<Node2D>("Front").AddChild(inactiveLeaf);
        Vector2 unrelatedCenter = new(1140, 100);
        var unrelatedLeaf = new ForegroundOccluder
        {
            Name = "UnrelatedScene", Position = unrelatedCenter,
            CoverageRect = new Rect2(-48, -64, 96, 128), FadeSpeed = 100f
        };
        unrelatedLeaf.AddChild(new Polygon2D
        {
            Name = "Column", Color = new Color("243438"),
            Polygon = [new Vector2(-48, -64), new Vector2(48, -64), new Vector2(48, 64), new Vector2(-48, 64)]
        });
        art.GetNode<Node2D>("Front").AddChild(unrelatedLeaf);

        var player = new CharacterBody2D { Name = "Player", Position = cases[0].Center };
        player.AddToGroup("player");
        playerRoot.AddChild(player);

        var enemy = new CharacterBody2D
        {
            Name = "ActiveEnemy", Position = cases[1].Center,
            CollisionLayer = CombatCollisionLayers.Enemy
        };
        currentRoom.AddChild(enemy);

        var target = new ThreadTarget2D { Name = "LiveThreadTarget", Position = cases[2].Center };
        currentRoom.AddChild(target);

        var line = new ThreadLineVisual { Name = "ThreadLine" };
        player.AddChild(line);

        var telegraph = new AttackTelegraphVisual { Name = "CurrentAttackTelegraph" };
        currentRoom.AddChild(telegraph);

        FallenBellkeeper boss = GD.Load<PackedScene>("res://scenes/boss/FallenBellkeeper.tscn")
            .Instantiate<FallenBellkeeper>();
        boss.Name = "LiveFallenBellkeeper";
        boss.Position = cases[5].Center;
        boss.Gravity = 0f;
        currentRoom.AddChild(boss);

        var groundDanger = new Area2D
        {
            Name = "CurrentGroundDanger", Position = cases[6].Center,
            CollisionLayer = CombatCollisionLayers.EnemyAttack
        };
        groundDanger.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(110, 34) }
        });
        currentRoom.AddChild(groundDanger);

        var groupOnlyPlayer = new Node2D
        {
            Name = "GroupOnlyPlayer", Position = cases[7].Center
        };
        groupOnlyPlayer.AddToGroup("player");
        playerRoot.AddChild(groupOnlyPlayer);

        var inactiveAttack = new Area2D
        {
            Name = "InactiveAttack", Position = inactiveCenter,
            CollisionLayer = CombatCollisionLayers.EnemyAttack,
            Monitoring = false, Monitorable = false
        };
        inactiveAttack.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(110, 34) }
        });
        currentRoom.AddChild(inactiveAttack);

        var unrelatedScene = new Node2D { Name = "UnrelatedSceneInstance" };
        unrelatedScene.AddChild(new CharacterBody2D
        {
            Name = "UnrelatedEnemy", Position = unrelatedCenter,
            CollisionLayer = CombatCollisionLayers.Enemy
        });
        harness.AddChild(unrelatedScene);

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        line.Present(cases[3].Center - new Vector2(45, 0), cases[3].Center + new Vector2(45, 0),
            .8f, ThreadLineState.Connected);
        var activeTelegraphFrame = new AttackPresentationFrame(
            new AttackTimelineFrame(91, EnemyAttackKind.BossGroundWave, AttackPhase.Telegraph,
                1f, .5f, cases[4].Center, true),
            new AttackShapeFrame(AttackShapeKind.GroundBand, cases[4].Center,
                new Vector2(100, 36), 0f, []));
        telegraph.Present(activeTelegraphFrame);

        var reusableRegions = new List<Rect2>
        {
            new Rect2(-9999f, -9999f, 1f, 1f)
        };
        RoomProtectionRegionAdapter.CollectInto(art, reusableRegions);
        TestAssert.True(reusableRegions.Count >= cases.Length,
            "live protection adapter fills a caller-owned reusable buffer");
        TestAssert.True(!reusableRegions.Contains(new Rect2(-9999f, -9999f, 1f, 1f)),
            "live protection adapter clears stale caller-owned buffer entries");
        List<Rect2> originalBuffer = reusableRegions;
        player.Position += new Vector2(12f, 0f);
        RoomProtectionRegionAdapter.CollectInto(art, reusableRegions);
        TestAssert.True(ReferenceEquals(originalBuffer, reusableRegions),
            "live protection adapter preserves caller-owned buffer identity across updates");

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        int stableTopologyRefreshCount = art.ProtectionCandidateRefreshCount;
        art._Process(0f);
        art._Process(0f);
        TestAssert.Equal(stableTopologyRefreshCount, art.ProtectionCandidateRefreshCount,
            "stable room topology reuses the protected-region candidate index across frames");
        foreach ((string name, _) in cases)
        {
            ForegroundOccluder leaf = art.GetNode<ForegroundOccluder>($"Front/{name}");
            TestAssert.True(leaf.PlayerBehind,
                $"live read-only adapter protects {name} across _Process frames");
        }
        ForegroundOccluder telegraphLeaf = art.GetNode<ForegroundOccluder>("Front/AttackTelegraph");
        telegraphLeaf.AdvanceFade(1f);
        TestAssert.True(telegraphLeaf.CurrentOpacity <= .35f,
            "active telegraph is never covered by an opaque foreground leaf");
        TestAssert.True(!inactiveLeaf.PlayerBehind,
            "non-monitoring attack geometry is not treated as a current protected attack shape");
        TestAssert.True(!unrelatedLeaf.PlayerBehind,
            "presentation search boundary excludes unrelated sibling scene instances");

        int liveStateRefreshCount = art.ProtectionCandidateRefreshCount;
        ForegroundOccluder enemyLeaf = art.GetNode<ForegroundOccluder>("Front/Enemy");
        enemy.Visible = false;
        art._Process(0f);
        TestAssert.True(!enemyLeaf.PlayerBehind,
            "cached candidate visibility is read live without a topology refresh");
        enemy.Visible = true;
        enemy.CollisionLayer = 0;
        art._Process(0f);
        TestAssert.True(!enemyLeaf.PlayerBehind,
            "cached candidate collision layer is read live without a topology refresh");
        enemy.CollisionLayer = CombatCollisionLayers.Enemy;
        art._Process(0f);
        TestAssert.True(enemyLeaf.PlayerBehind,
            "restoring a cached enemy layer protects it immediately");

        ForegroundOccluder groupOnlyPlayerLeaf =
            art.GetNode<ForegroundOccluder>("Front/GroupOnlyPlayer");
        groupOnlyPlayer.RemoveFromGroup("player");
        art._Process(0f);
        TestAssert.True(!groupOnlyPlayerLeaf.PlayerBehind,
            "cached group-only player membership is read live without a topology refresh");
        groupOnlyPlayer.AddToGroup("player");
        art._Process(0f);
        TestAssert.True(groupOnlyPlayerLeaf.PlayerBehind,
            "restoring cached group-only player membership protects it immediately");

        ForegroundOccluder lineLeaf = art.GetNode<ForegroundOccluder>("Front/ThreadLine");
        telegraph.Present(default);
        art._Process(0f);
        line.Clear();
        art._Process(0f);
        TestAssert.True(!lineLeaf.PlayerBehind,
            "cached thread-line state is read live without a topology refresh");
        line.Present(cases[3].Center - new Vector2(45, 0), cases[3].Center + new Vector2(45, 0),
            .8f, ThreadLineState.Connected);
        art._Process(0f);
        TestAssert.True(lineLeaf.PlayerBehind,
            "restoring a cached thread line protects it immediately");

        telegraph.Present(activeTelegraphFrame);
        art._Process(0f);
        TestAssert.True(telegraphLeaf.PlayerBehind,
            "restoring a cached active telegraph protects it immediately");
        telegraph.Present(default);
        art._Process(0f);
        TestAssert.True(!telegraphLeaf.PlayerBehind,
            "cached telegraph phase is read live without a topology refresh");
        ForegroundOccluder hammerLeaf = art.GetNode<ForegroundOccluder>("Front/BossHammer");
        FallenBellkeeperFormalPart hammer = boss.GetNode<FallenBellkeeperFormalPart>(
            "VisualRoot/Formal/Hammer");
        TestAssert.True(hammerLeaf.PlayerBehind,
            "clearing an independent telegraph does not remove visible Boss hammer protection");
        hammer.Visible = false;
        art._Process(0f);
        TestAssert.True(!hammerLeaf.PlayerBehind,
            "cached Boss hammer visibility is read live without a topology refresh");
        hammer.Visible = true;
        art._Process(0f);
        TestAssert.True(hammerLeaf.PlayerBehind,
            "restoring a cached Boss hammer protects its silhouette immediately");
        telegraph.Present(activeTelegraphFrame);
        art._Process(0f);
        TestAssert.True(telegraphLeaf.PlayerBehind,
            "restoring a cached active telegraph protects it immediately");
        TestAssert.Equal(liveStateRefreshCount, art.ProtectionCandidateRefreshCount,
            "visibility, layer, group, thread, telegraph and weapon state changes never rebuild the candidate index");

        int candidateChildCount = currentRoom.GetChildCount();
        currentRoom.RemoveChild(enemy);
        enemy.Free();
        var replacementEnemy = new CharacterBody2D
        {
            Name = "ActiveEnemyReplacement", Position = cases[1].Center,
            CollisionLayer = CombatCollisionLayers.Enemy
        };
        currentRoom.AddChild(replacementEnemy);
        TestAssert.Equal(candidateChildCount, currentRoom.GetChildCount(),
            "candidate replacement preserves room child count for topology-event coverage");
        int beforeCandidateReplacementRefresh = art.ProtectionCandidateRefreshCount;
        art._Process(0f);
        TestAssert.Equal(beforeCandidateReplacementRefresh + 1, art.ProtectionCandidateRefreshCount,
            "same-count candidate replacement invalidates the candidate index exactly once");
        TestAssert.True(enemyLeaf.PlayerBehind,
            "same-count candidate replacement is indexed and protected immediately");

        Node2D front = art.GetNode<Node2D>("Front");
        var replacementSlot = new Node2D { Name = "ReplacementSlot" };
        front.AddChild(replacementSlot);
        art._Process(0f);
        int replacementChildCount = front.GetChildCount();
        front.RemoveChild(replacementSlot);
        replacementSlot.Free();
        var replacementOccluder = new ForegroundOccluder
        {
            Name = "ReplacementOccluder",
            Position = player.Position,
            CoverageRect = new Rect2(-48, -64, 96, 128),
            FadeSpeed = 100f
        };
        front.AddChild(replacementOccluder);
        TestAssert.Equal(replacementChildCount, front.GetChildCount(),
            "foreground replacement preserves child count for cache-invalidation coverage");
        int beforeReplacementRefresh = art.ProtectionCandidateRefreshCount;
        art._Process(0f);
        TestAssert.Equal(beforeReplacementRefresh + 1, art.ProtectionCandidateRefreshCount,
            "same-count foreground replacement invalidates the candidate index exactly once");
        TestAssert.True(replacementOccluder.PlayerBehind,
            "same-count foreground replacement receives live protected regions immediately");

        await TestSceneTeardown.FreeSceneAfterAudioDrain(tree, harness);
    }
}
