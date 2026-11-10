using Godot;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Art;

public partial class BuildPlayerFormalVisual : SceneTree
{
    private const string ScenePath = "res://scenes/presentation/PlayerFormalVisual.tscn";
    private const string AtlasPath = "res://assets/player/formal/player-layers.png";

    private readonly record struct LayerSpec(string Name, PlayerFormalLayerKind Kind, Vector2 Position, int ZIndex);

    private static readonly LayerSpec[] Layers =
    [
        new("FarLeg", PlayerFormalLayerKind.FarLeg, new Vector2(-8, 17), 0),
        new("FarArm", PlayerFormalLayerKind.FarArm, new Vector2(-12, -2), 1),
        new("Tether", PlayerFormalLayerKind.Tether, new Vector2(-11, -28), 2),
        new("Lantern", PlayerFormalLayerKind.Lantern, new Vector2(-14, -42), 3),
        new("Body", PlayerFormalLayerKind.Body, new Vector2(0, 0), 4),
        new("NearLeg", PlayerFormalLayerKind.NearLeg, new Vector2(8, 17), 5),
        new("Mantle", PlayerFormalLayerKind.Mantle, new Vector2(-3, -1), 6),
        new("ShoulderCore", PlayerFormalLayerKind.ShoulderCore, new Vector2(12, -7), 7),
        new("NearArm", PlayerFormalLayerKind.NearArm, new Vector2(13, -2), 8),
        new("Weapon", PlayerFormalLayerKind.Weapon, new Vector2(14, 7), 9)
    ];

    public override void _Initialize()
    {
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://scenes/presentation"));
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://assets/player/formal"));

        Error atlasError = BuildAtlas();
        if (atlasError != Error.Ok)
        {
            GD.PushError($"BuildPlayerFormalVisual: failed to create player atlas: {atlasError}");
            Quit(1);
            return;
        }

        var root = new PlayerFormalVisual
        {
            Name = "PlayerFormalVisual",
            AnimationMapPath = "res://assets/player/formal/player-animation-map.json"
        };
        foreach (LayerSpec spec in Layers)
        {
            root.AddChild(new PlayerFormalLayer
            {
                Name = spec.Name,
                Kind = spec.Kind,
                Position = spec.Position,
                ZIndex = spec.ZIndex
            });
        }
        // This is a stable future-art anchor. Runtime travel remains owned by the existing
        // Player/VisualRoot/ThreadShuttle visual, so adding it here cannot alter cast timing.
        root.AddChild(new Node2D { Name = "ThreadShuttle", Position = new Vector2(20, -16), ZIndex = 10 });

        Error saveError = SceneBuildUtil.PackAndSave(root, ScenePath);
        root.Free();
        if (saveError != Error.Ok)
        {
            GD.PushError($"BuildPlayerFormalVisual: failed to save formal player scene: {saveError}");
            Quit(1);
            return;
        }

        PlayerFormalVisual copy = GD.Load<PackedScene>(ScenePath).Instantiate<PlayerFormalVisual>();
        bool valid = copy.AnimationMapPath == "res://assets/player/formal/player-animation-map.json";
        int previousZ = int.MinValue;
        foreach (LayerSpec spec in Layers)
        {
            PlayerFormalLayer layer = copy.GetNodeOrNull<PlayerFormalLayer>(spec.Name);
            valid &= layer?.Kind == spec.Kind &&
                ValidateRigNode(layer, spec.Position, spec.ZIndex, ref previousZ);
        }
        valid &= ValidateRigNode(copy.GetNodeOrNull<Node2D>("ThreadShuttle"),
            new Vector2(20, -16), 10, ref previousZ);
        valid &= ValidateActionSupport(copy);
        copy.Free();
        if (!valid)
        {
            GD.PushError("BuildPlayerFormalVisual contract validation failed.");
            Quit(1);
            return;
        }

        GD.Print("BuildPlayerFormalVisual: ten-layer player rig and compact atlas packed and validated.");
        Quit(0);
    }

    private static bool ValidateRigNode(Node2D node, Vector2 expectedPivot, int expectedZ, ref int previousZ)
    {
        if (node == null || node.Position != expectedPivot ||
            node.ZIndex != expectedZ || node.ZIndex <= previousZ ||
            node.Scale.X <= 0f || node.Scale.Y <= 0f ||
            !Mathf.IsEqualApprox(node.Scale.X, node.Scale.Y) ||
            !float.IsFinite(node.Position.X) || !float.IsFinite(node.Position.Y)) return false;
        previousZ = node.ZIndex;
        return true;
    }

    private static bool ValidateActionSupport(PlayerFormalVisual visual)
    {
        var baseFrame = new EntityPresentationFrame(EntityVisualState.Idle, 1, true);
        visual.ApplyPlayerFrame(new PlayerPresentationFrame(
            baseFrame,
            new PlayerLocomotionFrame(PlayerLocomotionState.Idle, 0f),
            new PlayerActionFrame(PlayerActionState.GroundAttack, PlayerActionPhase.Contact,
                .5f, false, Vector2.Right),
            SecondaryMotionFrame.Identity));
        bool attackSupported = visual.CurrentPlayerFrame.Action.State == PlayerActionState.GroundAttack &&
            !IsIdentity(visual.CurrentActionPose.NearArm);

        visual.ApplyPlayerFrame(new PlayerPresentationFrame(
            baseFrame,
            new PlayerLocomotionFrame(PlayerLocomotionState.Idle, 0f),
            new PlayerActionFrame(PlayerActionState.ThreadThrow, PlayerActionPhase.Burst,
                .5f, false, Vector2.Right),
            SecondaryMotionFrame.Identity));
        return attackSupported &&
            visual.CurrentPlayerFrame.Action.State == PlayerActionState.ThreadThrow &&
            !IsIdentity(visual.CurrentActionPose.NearArm);
    }

    private static bool IsIdentity(PoseTransform2D pose) =>
        pose.Position.LengthSquared() <= .0001f && Mathf.Abs(pose.RotationRadians) <= .0001f;

    private static Error BuildAtlas()
    {
        Image image = Image.CreateEmpty(512, 256, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);
        Color ink = new("11161a");
        Color cloth = new("1d3431");
        Color bone = new("e5ddc7");
        Color brass = new("b68a42");
        Color glow = new("75e0d6");
        for (int index = 0; index < Layers.Length; index++)
        {
            int column = index % 5;
            int row = index / 5;
            var rect = new Rect2I(8 + column * 100, 8 + row * 128, 96, 112);
            image.FillRect(rect, new Color(ink, 0.96f));
            image.FillRect(new Rect2I(rect.Position + new Vector2I(3, 3), rect.Size - new Vector2I(6, 6)),
                index is 6 or 9 ? cloth : bone.Darkened(index % 3 * 0.08f));
            image.FillRect(new Rect2I(rect.Position + new Vector2I(8, 8), new Vector2I(80, 5)), brass);
            image.FillRect(new Rect2I(rect.Position + new Vector2I(42, 45), new Vector2I(12, 22)), glow);
        }
        return image.SavePng(ProjectSettings.GlobalizePath(AtlasPath));
    }
}
