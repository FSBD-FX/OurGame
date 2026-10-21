using Godot;
using Knight.Presentation;

namespace Knight.Config;

public enum PresentationDiagnosticCode { None, InvalidVisualRoot, InvalidCueRoot }

// Values only: rejected native instances are still freed at the rejection site.
public readonly record struct PresentationDiagnostic(PresentationDiagnosticCode Code, string Message);

[GlobalClass]
public partial class PresentationProfile : Resource
{
    [Export] public PackedScene VisualScene { get; set; }
    [Export] public Godot.Collections.Dictionary<StringName, PackedScene> CueScenes { get; set; } = new();

    public IEntityVisual2D Resolve(Node visualRoot) => ResolveCore(visualRoot, out _, ReportDiagnostic);

    public IEntityVisual2D Resolve(Node visualRoot, out PresentationDiagnostic diagnostic) =>
        ResolveCore(visualRoot, out diagnostic, null);

    private IEntityVisual2D ResolveCore(Node visualRoot, out PresentationDiagnostic diagnostic,
        System.Action<PresentationDiagnostic> report)
    {
        diagnostic = default;
        string rootLabel = visualRoot.IsInsideTree() ? visualRoot.GetPath().ToString() : visualRoot.Name.ToString();
        CanvasItem fallbackItem = null;
        IEntityVisual2D fallback = null;
        CanvasItem authoredItem = null;
        IEntityVisual2D authored = null;
        foreach (Node child in visualRoot.GetChildren())
        {
            if (child is IEntityVisual2D entityVisual)
            {
                bool isDiagnostic = child.Name.ToString().Contains("Placeholder", System.StringComparison.OrdinalIgnoreCase) ||
                    child.GetType().Name.Contains("Placeholder", System.StringComparison.OrdinalIgnoreCase) ||
                    child.GetType().Name.Contains("Debug", System.StringComparison.OrdinalIgnoreCase);
                if (isDiagnostic && fallback == null)
                {
                    fallback = entityVisual;
                    fallbackItem = child as CanvasItem;
                }
                else if (!isDiagnostic && authored == null)
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

            diagnostic = new(PresentationDiagnosticCode.InvalidVisualRoot,
                $"PresentationProfile: VisualScene root must implement IEntityVisual2D for {rootLabel}.");
            report?.Invoke(diagnostic);
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

    public bool TryInstantiateCue(PresentationCue cue, out Node2D effect) =>
        TryInstantiateCueCore(cue, out effect, out _, ReportDiagnostic);

    public bool TryInstantiateCue(PresentationCue cue, out Node2D effect, out PresentationDiagnostic diagnostic) =>
        TryInstantiateCueCore(cue, out effect, out diagnostic, null);

    private bool TryInstantiateCueCore(PresentationCue cue, out Node2D effect,
        out PresentationDiagnostic diagnostic, System.Action<PresentationDiagnostic> report)
    {
        diagnostic = default;
        effect = null;
        if (!CueScenes.TryGetValue(cue.ToString(), out PackedScene scene) || scene == null) return false;

        Node instance = scene.Instantiate();
        if (instance is Node2D node2D)
        {
            effect = node2D;
            return true;
        }

        diagnostic = new(PresentationDiagnosticCode.InvalidCueRoot,
            $"PresentationProfile: cue '{cue}' root must be Node2D.");
        report?.Invoke(diagnostic);
        instance.Free();
        return false;
    }

    private static void ReportDiagnostic(PresentationDiagnostic diagnostic) => GD.PushWarning(diagnostic.Message);
}
