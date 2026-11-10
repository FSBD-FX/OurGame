using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Presentation;
using Knight.Tools;

namespace Knight.Build.Prologue;

public partial class BuildBellChainMechanism : SceneTree
{
    public override void _Initialize()
    {
        var mechanism = new BellChainMechanism { Name = "BellChain" };
        var target = new ThreadTarget2D
        {
            Name = "ThreadTarget", Position = new Vector2(0, -80),
            Kind = ThreadTargetKind.Mechanism, PullReceiverPath = new NodePath(".."),
            FallbackAimAssistRange = 260f,
            Material = GrappleSurfaceMaterial.TempleMetal,
            AssistRadius = MechanicalClawSettings.Default.MainAnchorAssistRadius
        };
        var area = new Area2D
        {
            Name = "TargetCollision", CollisionLayer = CombatCollisionLayers.ThreadTarget,
            CollisionMask = 0, Monitoring = true
        };
        area.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new CircleShape2D { Radius = 28 }
        });
        target.AddChild(area);
        target.AddChild(new LegacyThreadInteractionAdapter { Name = "LegacyThreadInteractionAdapter" });
        var door = new StaticBody2D
        {
            Name = "Door", Position = new Vector2(150, 0),
            CollisionLayer = CombatCollisionLayers.World, CollisionMask = 0
        };
        door.AddChild(new CollisionShape2D
        {
            Name = "Collision", Shape = new RectangleShape2D { Size = new Vector2(50, 180) }
        });
        var visualRoot = new Node2D { Name = "VisualRoot" };
        PackedScene chainScene = GD.Load<PackedScene>("res://scenes/presentation/BellChainVisual.tscn");
        PackedScene doorScene = GD.Load<PackedScene>("res://scenes/presentation/ShortcutDoorVisual.tscn");
        if (chainScene == null || doorScene == null)
        {
            GD.PushError("BuildBellChainMechanism: formal mechanism visuals must be built first.");
            Quit(1);
            return;
        }
        PrologueMechanismVisual formal = chainScene.Instantiate<PrologueMechanismVisual>();
        formal.Name = "Formal";
        PrologueMechanismVisual doorVisual = doorScene.Instantiate<PrologueMechanismVisual>();
        doorVisual.Name = "Door";
        doorVisual.Position = new Vector2(150, 0);
        visualRoot.AddChild(formal);
        visualRoot.AddChild(doorVisual);
        visualRoot.AddChild(new Node2D { Name = "Effects" });
        mechanism.AddChild(target); mechanism.AddChild(door); mechanism.AddChild(new Node { Name = "Presentation" });
        mechanism.AddChild(visualRoot);

        const string path = "res://scenes/mechanisms/BellChainMechanism.tscn";
        Error error = SceneBuildUtil.PackAndSave(mechanism, path); mechanism.Free();
        if (error != Error.Ok) { GD.PushError($"BuildBellChainMechanism failed: {error}"); Quit(1); return; }
        BellChainMechanism copy = GD.Load<PackedScene>(path).Instantiate<BellChainMechanism>();
        ThreadTarget2D generatedTarget = copy.GetNodeOrNull<ThreadTarget2D>("ThreadTarget");
        bool valid = generatedTarget?.Kind == ThreadTargetKind.Mechanism &&
            generatedTarget.Material == GrappleSurfaceMaterial.TempleMetal &&
            Mathf.IsEqualApprox(generatedTarget.FallbackAimAssistRange, 260f) &&
            generatedTarget.GetNodeOrNull<LegacyThreadInteractionAdapter>(
                "LegacyThreadInteractionAdapter") != null &&
            copy.GetNodeOrNull<CollisionShape2D>("Door/Collision") != null &&
            copy.GetNodeOrNull<PrologueMechanismVisual>("VisualRoot/Formal")?.Kind == MechanismVisualKind.BellChain &&
            copy.GetNodeOrNull<PrologueMechanismVisual>("VisualRoot/Door")?.Kind == MechanismVisualKind.ShortcutDoor &&
            copy.FindChildren("*", "Polygon2D", true, false).Count == 0 &&
            copy.GetNodeOrNull<Node2D>("VisualRoot/Effects") != null;
        copy.Free();
        if (!valid) { GD.PushError("BuildBellChainMechanism contract validation failed."); Quit(1); return; }
        GD.Print("BuildBellChainMechanism: packed and validated."); Quit(0);
    }
}
