using Godot;
using Knight.Presentation;
using Knight.Prologue;

namespace Knight.Config;

[GlobalClass]
public partial class RoomPresentationProfile : Resource
{
    [Export] public PackedScene VisualScene { get; set; }
    [Export] public PrologueRoomId Room { get; set; }

    public Node2D Resolve(Node2D visualRoot)
    {
        Node2D fallback = visualRoot.GetNodeOrNull<Node2D>("Placeholder");
        if (VisualScene != null)
        {
            Node instance = VisualScene.Instantiate();
            if (instance is Node2D authored)
            {
                authored.Name = "Override";
                visualRoot.AddChild(authored);
                if (fallback != null) fallback.Visible = false;
                if (authored is RoomFormalVisual formal)
                    formal.Present(RoomVisualCatalog.For(Room), Vector2.Zero, []);
                return authored;
            }

            GD.PushWarning($"RoomPresentationProfile: VisualScene root must be Node2D for '{visualRoot.Name}'.");
            instance.Free();
        }

        if (fallback != null)
        {
            if (PrologueProductionMode.AllowDiagnosticFallbacks)
            {
                fallback.Visible = true;
                return fallback;
            }
            fallback.Visible = false;
            throw new System.InvalidOperationException(
                $"RoomPresentationProfile: production room art is missing or invalid under '{visualRoot.Name}'.");
        }
        throw new System.InvalidOperationException(
            $"RoomPresentationProfile: no Placeholder exists under '{visualRoot.Name}'.");
    }
}
