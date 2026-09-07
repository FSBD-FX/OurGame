using Godot;
using Knight.Presentation;

namespace Knight.Config;

[GlobalClass]
public partial class PresentationProfile : Resource
{
    [Export] public PackedScene VisualScene { get; set; }
    [Export] public Godot.Collections.Dictionary<StringName, PackedScene> CueScenes { get; set; } = new();

    public IEntityVisual2D Resolve(Node visualRoot)
    {
        string rootLabel = visualRoot.IsInsideTree() ? visualRoot.GetPath().ToString() : visualRoot.Name.ToString();
        CanvasItem fallbackItem = null;
        IEntityVisual2D fallback = null;
        CanvasItem authoredItem = null;
        IEntityVisual2D authored = null;
        foreach (Node child in visualRoot.GetChildren())
        {
            if (child is IEntityVisual2D entityVisual)
            {
                bool diagnostic = child.Name.ToString().Contains("Placeholder", System.StringComparison.OrdinalIgnoreCase) ||
                    child.GetType().Name.Contains("Placeholder", System.StringComparison.OrdinalIgnoreCase) ||
                    child.GetType().Name.Contains("Debug", System.StringComparison.OrdinalIgnoreCase);
                if (diagnostic && fallback == null)
                {
                    fallback = entityVisual;
                    fallbackItem = child as CanvasItem;
                }
                else if (!diagnostic && authored == null)
                {
                    authored = entityVisual;
                    authoredItem = child as CanvasItem;
                }
            }
        }

        if (VisualScene != null)
        {
            Node instance = VisualScene.Instantiate();
            if (instance is IEntityVisual2D overrideVisual)
            {
                instance.Name = "Override";
                visualRoot.AddChild(instance);
                if (fallbackItem != null) fallbackItem.Visible = false;
                return overrideVisual;
            }

            GD.PushWarning($"PresentationProfile: VisualScene root must implement IEntityVisual2D for {rootLabel}.");
            instance.Free();
        }

        if (authored != null)
        {
            if (authoredItem != null) authoredItem.Visible = true;
            if (fallbackItem != null) fallbackItem.Visible = false;
            return authored;
        }

        if (fallback != null)
        {
            if (PrologueProductionMode.AllowDiagnosticFallbacks)
            {
                if (fallbackItem != null) fallbackItem.Visible = true;
                return fallback;
            }
            if (fallbackItem != null) fallbackItem.Visible = false;
            throw new System.InvalidOperationException(
                $"PresentationProfile: production visual is missing or invalid under {rootLabel}.");
        }

        throw new System.InvalidOperationException(
            $"PresentationProfile: no IEntityVisual2D available under {rootLabel}.");
    }

    public bool TryInstantiateCue(PresentationCue cue, out Node2D effect)
    {
        effect = null;
        if (!CueScenes.TryGetValue(cue.ToString(), out PackedScene scene) || scene == null) return false;

        Node instance = scene.Instantiate();
        if (instance is Node2D node2D)
        {
            effect = node2D;
            return true;
        }

        GD.PushWarning($"PresentationProfile: cue '{cue}' root must be Node2D.");
        instance.Free();
        return false;
    }
}
