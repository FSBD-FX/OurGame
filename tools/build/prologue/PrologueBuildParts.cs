using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Prologue;
using Knight.Presentation;
using Knight.Prologue.Traversal;
using Knight.World;

namespace Knight.Build.Prologue;

public static class PrologueBuildParts
{
    public static TraversalSegment2D TraversalSegment(Node parent, string id, TraversalSegmentKind kind,
        TraversalAbility required, Rect2 launchZone, Rect2 landingZone, NodePath assistTargetPath = default)
    {
        var segment = new TraversalSegment2D
        {
            Name = id,
            SegmentId = id,
            Kind = kind,
            RequiredAbilities = required,
            LaunchZone = launchZone,
            LandingZone = landingZone,
            AssistTargetPath = assistTargetPath ?? new NodePath()
        };
        parent.AddChild(segment);
        return segment;
    }

    public static StaticBody2D Platform(Node parent, string name, Vector2 center, Vector2 size, Color color)
    {
        var body = new StaticBody2D
        {
            Name = name,
            Position = center,
            CollisionLayer = CombatCollisionLayers.World,
            CollisionMask = CombatCollisionLayers.None
        };
        body.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = size }
        });
        body.AddChild(Rect("Body", Vector2.Zero, size, color));
        body.AddChild(Rect("Rim", new Vector2(0, -size.Y / 2f + 3), new Vector2(size.X, 6), new Color("b7d29c")));
        parent.AddChild(body);
        return body;
    }

    public static Marker2D Entrance(Node parent, string name, Vector2 position)
    {
        var marker = new Marker2D { Name = name, Position = position };
        parent.AddChild(marker);
        return marker;
    }

    public static RoomExit Exit(Node parent, string name, StringName exitId, Vector2 position, Color color)
    {
        var exit = new RoomExit
        {
            Name = name,
            ExitId = exitId,
            Position = position,
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        exit.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(48, 112) }
        });
        exit.AddChild(Rect("Readability", Vector2.Zero, new Vector2(18, 96), color));
        parent.AddChild(exit);
        return exit;
    }

    public static TutorialPromptTrigger Tutorial(Node parent, string name, StringName id, string text,
        Vector2 position, Vector2 size)
    {
        var trigger = new TutorialPromptTrigger
        {
            Name = name,
            TutorialId = id,
            Text = text,
            Position = position,
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        trigger.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = size }
        });
        parent.AddChild(trigger);
        return trigger;
    }

    public static KillZone KillZone(Node parent, Rect2 area)
    {
        var zone = new KillZone
        {
            Name = "KillZone",
            Position = area.GetCenter(),
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        zone.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = area.Size }
        });
        parent.AddChild(zone);
        return zone;
    }

    public static CheckpointTrigger Checkpoint(Node parent, Vector2 position, StringName entranceId)
    {
        var checkpoint = new CheckpointTrigger
        {
            Name = "Checkpoint",
            EntranceId = entranceId,
            Position = position,
            CollisionLayer = CombatCollisionLayers.None,
            CollisionMask = CombatCollisionLayers.Player,
            Monitoring = true
        };
        checkpoint.AddChild(new CollisionShape2D
        {
            Name = "Collision",
            Shape = new RectangleShape2D { Size = new Vector2(52, 104) }
        });
        PackedScene lampScene = GD.Load<PackedScene>("res://scenes/presentation/GraveLampVisual.tscn");
        if (lampScene == null) throw new System.InvalidOperationException(
            "GraveLampVisual must be built before prologue rooms.");
        PrologueMechanismVisual lamp = lampScene.Instantiate<PrologueMechanismVisual>();
        lamp.Name = "Visual";
        lamp.Position = new Vector2(0, -22);
        checkpoint.AddChild(lamp);
        parent.AddChild(checkpoint);
        return checkpoint;
    }

    public static ThreadTarget2D Anchor(Node parent, string name, Vector2 position)
    {
        var target = new ThreadTarget2D { Name = name, Position = position, Kind = ThreadTargetKind.Anchor };
        var area = new Area2D
        {
            Name = "TargetCollision",
            CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = CombatCollisionLayers.None,
            Monitoring = true
        };
        area.AddChild(new CollisionShape2D { Name = "Collision", Shape = new CircleShape2D { Radius = 25 } });
        target.AddChild(area);
        PackedScene anchorScene = GD.Load<PackedScene>("res://scenes/presentation/ThreadAnchorVisual.tscn");
        if (anchorScene == null) throw new System.InvalidOperationException(
            "ThreadAnchorVisual must be built before prologue rooms.");
        PrologueMechanismVisual formal = anchorScene.Instantiate<PrologueMechanismVisual>();
        formal.Name = "Formal";
        target.AddChild(formal);
        parent.AddChild(target);
        return target;
    }

    public static Node2D Presentation(string roomName, Rect2 bounds, Color backdrop, Color silhouette)
    {
        var presentation = new Node2D { Name = "Presentation", ZIndex = -20 };
        var root = new Node2D { Name = "VisualRoot" };
        var placeholder = new Node2D { Name = "Placeholder" };
        placeholder.AddChild(new Polygon2D
        {
            Name = "Backdrop",
            Color = backdrop,
            Polygon = [bounds.Position, new Vector2(bounds.End.X, bounds.Position.Y), bounds.End,
                new Vector2(bounds.Position.X, bounds.End.Y)]
        });
        placeholder.AddChild(new Polygon2D
        {
            Name = "TowerSilhouette",
            Color = silhouette,
            Polygon = [new Vector2(bounds.Position.X + 420, bounds.Position.Y),
                new Vector2(bounds.Position.X + 650, bounds.Position.Y),
                new Vector2(bounds.Position.X + 740, bounds.End.Y),
                new Vector2(bounds.Position.X + 260, bounds.End.Y)]
        });
        placeholder.AddChild(new Label
        {
            Name = "RoomLabel",
            Text = roomName,
            Position = bounds.Position + new Vector2(42, 36),
            Modulate = new Color(0.75f, 0.82f, 0.76f, 0.4f)
        });
        root.AddChild(placeholder);
        presentation.AddChild(root);
        return presentation;
    }

    public static RoomFormalVisual AddFormalArt(PrologueRoom room, Node2D geometry, Rect2 bounds)
    {
        RoomVisualDescriptor descriptor = RoomVisualCatalog.For(room.RoomId);
        Color backColor = descriptor.Shadow.Lightened(.11f);
        Color midColor = descriptor.Stone.Darkened(.16f);
        Color rimColor = descriptor.Copper.Lightened(.24f);
        var art = new RoomFormalVisual
        {
            Name = "Art", RoomKey = RoomKey(room.RoomId), Room = room.RoomId,
            LandmarkId = descriptor.Landmark, CameraBasis = new Vector2(1280, 720), ZIndex = -1
        };
        var far = new Node2D { Name = "Far", ZIndex = -30 };
        var back = new Node2D { Name = "Back", ZIndex = -24 };
        var mid = new Node2D { Name = "Mid", ZIndex = -16 };
        var platforms = new Node2D { Name = "Platforms", ZIndex = -4 };
        var props = new Node2D { Name = "Props", ZIndex = -2 };
        var front = new Node2D { Name = "Front", ZIndex = 12 };
        var atmosphere = new Node2D { Name = "Atmosphere", ZIndex = -8 };
        foreach (Node2D group in new[] { far, back, mid, platforms, props, front, atmosphere })
            art.AddChild(group);
        far.SetMeta("depth_role", "far");
        back.SetMeta("depth_role", "far");
        mid.SetMeta("depth_role", "mid");
        platforms.SetMeta("depth_role", "play");
        props.SetMeta("depth_role", "play");
        front.SetMeta("depth_role", "front");
        atmosphere.SetMeta("depth_role", "atmosphere");

        far.AddChild(Rect("Backdrop", bounds.GetCenter(), bounds.Size, descriptor.Shadow));
        AddRoomSilhouette(back, descriptor, bounds, backColor);
        for (int index = 0; index < 4; index++)
        {
            float x = bounds.Position.X + 210f + index * (bounds.Size.X - 420f) / 3f;
            mid.AddChild(Rect($"StructuralRib{index + 1}", new Vector2(x, bounds.GetCenter().Y),
                new Vector2(index % 2 == 0 ? 30f : 18f, bounds.Size.Y * .74f), midColor));
        }
        AddLandmark(mid, props, descriptor, bounds, rimColor, midColor);

        foreach (Node child in geometry.GetChildren())
        {
            if (child is not StaticBody2D platform) continue;
            CollisionShape2D collision = platform.GetNodeOrNull<CollisionShape2D>("Collision");
            if (collision?.Shape is not RectangleShape2D rectangle) continue;
            Polygon2D skin = Rect(platform.Name, platform.Position, rectangle.Size, descriptor.Stone);
            skin.AddChild(Rect("LitEdge", new Vector2(0, -rectangle.Size.Y / 2f + 4f),
                new Vector2(rectangle.Size.X, Mathf.Min(8f, rectangle.Size.Y)), rimColor));
            platforms.AddChild(skin);
            foreach (Node platformChild in platform.GetChildren())
                if (platformChild is CanvasItem canvas && platformChild.Name != "Collision") canvas.Visible = false;
        }

        AddForegroundLeaf(front, "LeftForeground", new Vector2(bounds.Position.X + 90f, bounds.GetCenter().Y),
            bounds.Size.Y, false, backColor);
        AddForegroundLeaf(front, "RightForeground", new Vector2(bounds.End.X - 90f, bounds.GetCenter().Y),
            bounds.Size.Y, true, backColor);
        int seed = 31 + (int)descriptor.Room * 37;
        atmosphere.SetMeta("deterministic_seed", seed);
        atmosphere.SetMeta("atmosphere_kind", descriptor.Atmosphere.ToString());
        for (int index = 0; index < 18; index++)
        {
            float x = bounds.Position.X + 55f + ((index * 137f + seed * 11f) % (bounds.Size.X - 110f));
            float y = bounds.Position.Y + 55f + ((index * 83f + seed * 7f) % (bounds.Size.Y - 110f));
            atmosphere.AddChild(Circle($"Dust{index + 1}", new Vector2(x, y), index % 3 + 1.5f,
                new Color(descriptor.Copper.Lightened(.18f), 0.24f), 8));
        }

        Node2D placeholder = room.GetNode<Node2D>("Presentation/VisualRoot/Placeholder");
        placeholder.Visible = false;
        Label roomLabel = placeholder.GetNodeOrNull<Label>("RoomLabel");
        if (roomLabel != null) roomLabel.Visible = false;
        room.AddChild(art);
        art.Present(descriptor, Vector2.Zero, []);
        return art;
    }

    private static void AddRoomSilhouette(Node2D back, RoomVisualDescriptor descriptor, Rect2 bounds,
        Color color)
    {
        Vector2 center = bounds.GetCenter();
        switch (descriptor.Room)
        {
            case PrologueRoomId.AwakeningCourtyard:
                back.AddChild(Rect("OpenSky", center + new Vector2(0, -80),
                    new Vector2(bounds.Size.X * .58f, bounds.Size.Y * .58f), color.Darkened(.18f)));
                back.AddChild(Rect("LeftBelfry", new Vector2(bounds.Position.X + 160, center.Y),
                    new Vector2(320, bounds.Size.Y), color));
                back.AddChild(Rect("RightBelfry", new Vector2(bounds.End.X - 150, center.Y),
                    new Vector2(300, bounds.Size.Y), color));
                break;
            case PrologueRoomId.BrokenToothCorridor:
                back.AddChild(Rect("LowCeiling", new Vector2(center.X, bounds.Position.Y + 70),
                    new Vector2(bounds.Size.X, 140), color));
                back.AddChild(Rect("CompressedWall", center + new Vector2(0, 170),
                    new Vector2(bounds.Size.X, bounds.Size.Y * .32f), color.Darkened(.1f)));
                back.AddChild(Rect("GearShaftVoid", center + new Vector2(180, -20),
                    new Vector2(640, 210), descriptor.Shadow.Lightened(.04f)));
                break;
            case PrologueRoomId.UpperPendulum:
                back.AddChild(Rect("WindShaft", center, new Vector2(bounds.Size.X * .42f, bounds.Size.Y),
                    color.Darkened(.18f)));
                back.AddChild(Rect("LeftTower", new Vector2(bounds.Position.X + 220, center.Y),
                    new Vector2(440, bounds.Size.Y), color));
                back.AddChild(Rect("RightTower", new Vector2(bounds.End.X - 210, center.Y),
                    new Vector2(420, bounds.Size.Y), color));
                break;
            case PrologueRoomId.LowerBuriedMachinery:
                back.AddChild(Rect("BuriedVault", center, new Vector2(bounds.Size.X, bounds.Size.Y), color));
                back.AddChild(Circle("BoilerVoid", center + new Vector2(380, -70), 190,
                    descriptor.Shadow.Lightened(.05f), 32));
                back.AddChild(Rect("EarthLoad", new Vector2(center.X, bounds.Position.Y + 75),
                    new Vector2(bounds.Size.X, 150), color.Darkened(.12f)));
                break;
            case PrologueRoomId.BellChainMechanism:
                back.AddChild(Rect("ChainWell", center, new Vector2(620, bounds.Size.Y),
                    color.Darkened(.2f)));
                back.AddChild(Rect("LeftMechanism", new Vector2(bounds.Position.X + 250, center.Y),
                    new Vector2(500, bounds.Size.Y), color));
                back.AddChild(Rect("RightMechanism", new Vector2(bounds.End.X - 230, center.Y),
                    new Vector2(460, bounds.Size.Y), color));
                break;
            case PrologueRoomId.FallenBellkeeperArena:
                back.AddChild(Rect("ArenaApse", center, new Vector2(bounds.Size.X, bounds.Size.Y), color));
                back.AddChild(Circle("BellBacklight", center + new Vector2(210, -75), 225,
                    descriptor.Copper.Darkened(.38f), 36));
                back.AddChild(Rect("BrokenRoof", new Vector2(center.X, bounds.Position.Y + 55),
                    new Vector2(bounds.Size.X, 110), color.Darkened(.16f)));
                break;
        }
    }

    private static void AddLandmark(Node2D mid, Node2D props, RoomVisualDescriptor descriptor, Rect2 bounds,
        Color accent, Color metal)
    {
        var landmark = new Node2D { Name = "Landmark" };
        landmark.SetMeta("landmark_id", descriptor.Landmark);
        mid.AddChild(landmark);
        Vector2 center = bounds.GetCenter();
        switch (descriptor.Room)
        {
            case PrologueRoomId.AwakeningCourtyard:
                Vector2 clock = center + new Vector2(210, -105);
                landmark.AddChild(Circle("ClockFace", clock, 118, metal, 30));
                landmark.AddChild(new Line2D { Name = "BrokenMinuteHand", Points = [clock, clock + new Vector2(68, -42)], Width = 9, DefaultColor = accent });
                landmark.AddChild(new Line2D { Name = "FallenHourHand", Points = [clock, clock + new Vector2(-18, 76)], Width = 12, DefaultColor = accent.Darkened(.22f) });
                break;
            case PrologueRoomId.BrokenToothCorridor:
                Vector2 gear = center + new Vector2(110, -40);
                Vector2 driveCenter = gear - new Vector2(150, 0);
                Vector2 severedCenter = gear + new Vector2(85, 20);
                landmark.AddChild(ToothedGear("DriveGear", driveCenter, 68, 98, 10, metal));
                landmark.AddChild(ToothedGear("SeveredGear", severedCenter, 84, 122, 9,
                    metal.Darkened(.1f)));
                landmark.AddChild(new Line2D
                {
                    Name = "DriveAxleStub", Points = [driveCenter, driveCenter + new Vector2(58, 0)],
                    Width = 18, DefaultColor = accent
                });
                landmark.AddChild(new Line2D
                {
                    Name = "SeveredAxleStub",
                    Points = [severedCenter - new Vector2(68, 0), severedCenter],
                    Width = 18, DefaultColor = accent.Darkened(.18f)
                });
                break;
            case PrologueRoomId.UpperPendulum:
                Vector2 beam = new(center.X + 90, bounds.Position.Y + 80);
                landmark.AddChild(Rect("PendulumBeam", beam, new Vector2(560, 38), metal));
                landmark.AddChild(new Line2D { Name = "LongChain", Points = [beam, beam + new Vector2(-80, 410)], Width = 8, DefaultColor = accent });
                landmark.AddChild(Circle("PendulumBob", beam + new Vector2(-80, 430), 62, metal.Lightened(.08f), 20));
                break;
            case PrologueRoomId.LowerBuriedMachinery:
                Vector2 pipe = center + new Vector2(160, -45);
                landmark.AddChild(new Line2D { Name = "CopperMain", Points = [pipe + new Vector2(-320, -160), pipe + new Vector2(-320, 70), pipe + new Vector2(300, 70)], Width = 28, DefaultColor = descriptor.Copper });
                landmark.AddChild(new Line2D { Name = "CopperReturn", Points = [pipe + new Vector2(-190, -170), pipe + new Vector2(-190, -20), pipe + new Vector2(260, -20)], Width = 16, DefaultColor = descriptor.Copper.Darkened(.16f) });
                landmark.AddChild(Circle("FungusBloom", pipe + new Vector2(70, 46), 48, new Color("78936a"), 12));
                break;
            case PrologueRoomId.BellChainMechanism:
                Vector2 chain = center + new Vector2(260, -10);
                landmark.AddChild(new Line2D { Name = "VerticalChain", Points = [chain + new Vector2(0, -350), chain + new Vector2(0, 260)], Width = 24, DefaultColor = accent });
                landmark.AddChild(Circle("Counterweight", chain + new Vector2(0, 80), 72, metal, 18));
                landmark.AddChild(Rect("ChainGuide", chain + new Vector2(0, -170), new Vector2(150, 56), descriptor.Copper));
                break;
            case PrologueRoomId.FallenBellkeeperArena:
                Vector2 bell = center + new Vector2(210, -72);
                landmark.AddChild(Circle("Backlight", bell, 190, descriptor.Copper.Darkened(.28f), 32));
                landmark.AddChild(new Polygon2D { Name = "BrokenBellLeft", Color = metal, Polygon = [bell + new Vector2(-150, 95), bell + new Vector2(-92, -100), bell + new Vector2(-18, -145), bell + new Vector2(-34, 100)] });
                landmark.AddChild(new Polygon2D { Name = "BrokenBellRight", Color = metal.Darkened(.12f), Polygon = [bell + new Vector2(18, -142), bell + new Vector2(100, -92), bell + new Vector2(155, 98), bell + new Vector2(42, 100)] });
                break;
        }
        AddLowFrequencyProps(props, descriptor, bounds, accent, metal);
    }

    private static void AddLowFrequencyProps(Node2D props, RoomVisualDescriptor descriptor, Rect2 bounds,
        Color accent, Color metal)
    {
        int count = descriptor.Room is PrologueRoomId.UpperPendulum or PrologueRoomId.BellChainMechanism ? 7 : 5;
        for (int index = 0; index < count; index++)
        {
            float x = bounds.Position.X + 190f + index * (bounds.Size.X - 380f) / Mathf.Max(1, count - 1);
            float length = 70f + (index % 3) * 42f;
            props.AddChild(new Line2D { Name = $"Suspension{index + 1}", Position = new Vector2(x, bounds.Position.Y), Points = [Vector2.Zero, new Vector2(0, length)], Width = index % 2 == 0 ? 4f : 2f, DefaultColor = new Color(accent, .62f) });
            if (index % 2 == 0)
                props.AddChild(Circle($"Fitting{index + 1}", new Vector2(x, bounds.Position.Y + length + 18f), 15f + index % 3 * 4f, new Color(metal, .8f), 12));
        }
    }

    private static void AddForegroundLeaf(Node2D front, string name, Vector2 position, float height,
        bool right, Color color)
    {
        var leaf = new ForegroundOccluder
        {
            Name = name, Position = position, CoverageRect = new Rect2(-90f, -height * .5f, 180f, height)
        };
        float direction = right ? -1f : 1f;
        leaf.AddChild(new Polygon2D
        {
            Name = "Arch", Color = color,
            Polygon = [new Vector2(-90, -height * .5f), new Vector2(90, -height * .5f),
                new Vector2(90 + direction * 105f, height * .5f), new Vector2(-90, height * .5f)]
        });
        front.AddChild(leaf);
    }

    private static string RoomKey(PrologueRoomId room) => room switch
    {
        PrologueRoomId.AwakeningCourtyard => "awakening-courtyard",
        PrologueRoomId.BrokenToothCorridor => "broken-tooth-corridor",
        PrologueRoomId.UpperPendulum => "upper-pendulum",
        PrologueRoomId.LowerBuriedMachinery => "lower-buried-machinery",
        PrologueRoomId.BellChainMechanism => "bell-chain-mechanism",
        PrologueRoomId.FallenBellkeeperArena => "fallen-bellkeeper-arena",
        _ => throw new System.ArgumentOutOfRangeException(nameof(room), room, null)
    };

    private static Polygon2D Circle(string name, Vector2 center, float radius, Color color, int sides)
    {
        Vector2[] points = new Vector2[Mathf.Max(3, sides)];
        for (int index = 0; index < points.Length; index++)
        {
            float angle = Mathf.Tau * index / points.Length;
            points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return new Polygon2D { Name = name, Position = center, Color = color, Polygon = points };
    }

    private static Polygon2D ToothedGear(string name, Vector2 center, float rootRadius, float toothRadius,
        int teeth, Color color)
    {
        Vector2[] points = new Vector2[teeth * 4];
        for (int index = 0; index < points.Length; index++)
        {
            float angle = Mathf.Tau * index / points.Length;
            float radius = index % 4 is 1 or 2 ? toothRadius : rootRadius;
            points[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return new Polygon2D { Name = name, Position = center, Color = color, Polygon = points };
    }

    public static ColorRect HudPanel(string name, Vector2 position, Vector2 size)
    {
        return new ColorRect
        {
            Name = name,
            Position = position,
            Size = size,
            Color = new Color(0.025f, 0.045f, 0.075f, 0.82f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
    }

    public static Polygon2D Rect(string name, Vector2 center, Vector2 size, Color color) => new()
    {
        Name = name,
        Position = center,
        Color = color,
        Polygon = [-size / 2, new Vector2(size.X / 2, -size.Y / 2), size / 2,
            new Vector2(-size.X / 2, size.Y / 2)]
    };
}
