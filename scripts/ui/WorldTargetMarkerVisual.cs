using System.Collections.Generic;
using Godot;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Guidance;

namespace Knight.Ui;

public partial class WorldTargetMarkerVisual : Control, IPrologueGuidanceVisual
{
    private static readonly string[] ProtectedFormalHudPaths =
    [
        "Health", "ThreadSpool", "ObjectiveRibbon", "Prompt",
        "ModeSelector", "Loadout", "ResourceCluster", "Boss"
    ];

    public string RenderedStepText { get; private set; } = string.Empty;
    public int RenderedTargetCount => _targets.Count;
    public bool IsExpandedTeaching { get; private set; }
    public Rect2 PromptRect { get; private set; }
    public bool HasVisiblePrompt { get; private set; }
    public Rect2 RenderedTextBounds { get; private set; }
    public bool RenderedTextContained { get; private set; }

    private readonly List<PrologueGuidanceTargetView> _targets = new();
    private Label _text;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _text = GetNodeOrNull<Label>("Text");
    }

    public void SetExpandedTeaching(bool expanded) => IsExpandedTeaching = expanded;

    public override void _Notification(int what)
    {
        if (what == NotificationResized && IsInsideTree() && !string.IsNullOrEmpty(RenderedStepText))
            ApplyPromptLayout();
    }

    public void Present(PrologueGuidanceInstruction instruction,
        IReadOnlyList<PrologueGuidanceTargetView> targets)
    {
        RenderedStepText = instruction.StepText;
        _targets.Clear();
        foreach (PrologueGuidanceTargetView target in targets) _targets.Add(target);
        if (_text != null)
        {
            _text.Text = IsExpandedTeaching
                ? instruction.HeadingText + "  /  " + instruction.StepText +
                  (string.IsNullOrEmpty(instruction.HintText)
                      ? string.Empty : "\n" + instruction.HintPrefix + instruction.HintText)
                : "◆ " + instruction.StepText;
        }
        ApplyPromptLayout();
        Visible = instruction.Visible;
        QueueRedraw();
    }

    public void Clear()
    {
        RenderedStepText = string.Empty;
        _targets.Clear();
        IsExpandedTeaching = false;
        PromptRect = default;
        HasVisiblePrompt = false;
        RenderedTextBounds = default;
        RenderedTextContained = false;
        SetPromptContentVisible(false);
        if (_text != null) _text.Text = string.Empty;
        Visible = false;
        QueueRedraw();
    }

    private void ApplyPromptLayout()
    {
        Vector2 viewport = Size;
        if (viewport.X < 1f || viewport.Y < 1f) viewport = GetViewportRect().Size;
        Rect2 safe = new(new Vector2(48, 40), viewport - new Vector2(96, 80));
        Vector2 promptSize = IsExpandedTeaching ? new Vector2(480, 88) : new Vector2(248, 48);
        promptSize.X = Mathf.Min(promptSize.X, safe.Size.X);
        if (_text != null)
        {
            // A compact guidance card is still actionable copy. It must never hide an
            // instruction behind ellipsis merely because the repeated-failure expansion
            // has not been reached yet.
            _text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _text.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            _text.ClipText = false;
        }
        float requiredTextHeight = MeasureRequiredTextHeight(promptSize.X - 28f);
        float verticalPadding = IsExpandedTeaching ? 20f : 10f;
        promptSize.Y = Mathf.Min(safe.Size.Y,
            Mathf.Max(promptSize.Y, requiredTextHeight + verticalPadding));
        var protectedRegions = new List<Rect2>(ResolveProtectedRegions());
        foreach (PrologueGuidanceTargetView target in _targets)
        {
            if (target.ProtectionSize.X <= 0f || target.ProtectionSize.Y <= 0f) continue;
            protectedRegions.Add(new Rect2(target.ViewportPosition - target.ProtectionSize * .5f,
                target.ProtectionSize));
        }
        PromptRect = _targets.Count > 0
            ? PlacePrompt(safe, _targets[0].ViewportPosition, promptSize, protectedRegions)
            : PlacePrompt(safe, safe.GetCenter(), promptSize, protectedRegions);

        HasVisiblePrompt = PromptRect.HasArea();
        if (!HasVisiblePrompt)
        {
            if (_text != null) _text.Text = string.Empty;
            SetPromptContentVisible(false);
            return;
        }

        SetPromptContentVisible(true);

        Control backdrop = GetNodeOrNull<Control>("Backdrop");
        Control topRule = GetNodeOrNull<Control>("TopRule");
        if (backdrop != null)
        {
            backdrop.Position = PromptRect.Position;
            backdrop.Size = PromptRect.Size;
        }
        if (topRule != null)
        {
            topRule.Position = PromptRect.Position;
            topRule.Size = new Vector2(PromptRect.Size.X, 2f);
        }
        if (_text != null)
        {
            _text.Position = PromptRect.Position + new Vector2(14, IsExpandedTeaching ? 10 : 5);
            _text.Size = PromptRect.Size - new Vector2(28, IsExpandedTeaching ? 16 : 10);
            _text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _text.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            _text.ClipText = false;
            _text.VerticalAlignment = VerticalAlignment.Center;
            float contentHeight = MeasureRequiredTextHeight(_text.Size.X);
            RenderedTextBounds = new Rect2(_text.Position,
                new Vector2(_text.Size.X, Mathf.Min(_text.Size.Y, contentHeight)));
            RenderedTextContained = contentHeight <= _text.Size.Y + .5f &&
                PromptRect.HasPoint(RenderedTextBounds.Position) &&
                PromptRect.HasPoint(RenderedTextBounds.End - Vector2.One * .01f);
        }
    }

    private float MeasureRequiredTextHeight(float width)
    {
        if (_text == null || string.IsNullOrEmpty(_text.Text)) return 0f;
        Vector2 prior = _text.Size;
        _text.Size = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, prior.Y));
        // Label shapes fallback glyphs (including CJK) and applies theme line spacing.
        // Primary-font height alone underestimates the actual wrapped Control, leaving
        // the final line outside the backdrop even when our own containment flag passes.
        float height = _text.GetMinimumSize().Y;
        _text.Size = prior;
        return height;
    }

    private void SetPromptContentVisible(bool visible)
    {
        Control backdrop = GetNodeOrNull<Control>("Backdrop");
        Control topRule = GetNodeOrNull<Control>("TopRule");
        if (backdrop != null) backdrop.Visible = visible;
        if (topRule != null) topRule.Visible = visible;
        if (_text != null) _text.Visible = visible;
    }

    internal IReadOnlyList<Rect2> ResolveProtectedRegions()
    {
        Node ancestor = this;
        while (ancestor != null && ancestor is not PrologueLevelController)
            ancestor = ancestor.GetParent();
        PrologueLevelController level = ancestor as PrologueLevelController;
        PrologueRoom room = level?.CurrentRoom;
        RoomFormalVisual art = room?.GetNodeOrNull<RoomFormalVisual>("Art");
        // Continuous sectors do not populate the legacy CurrentRoom / Art branch.
        // Use live world roots so the card avoids the player, tether and attack reads.
        var regions = new List<Rect2>(level?.CurrentSectorRoot != null
            ? RoomProtectionRegionAdapter.CollectGameplay(
                level.CurrentSectorRoot.GetParent() as Node2D ?? level.CurrentSectorRoot,
                level.GetNodeOrNull(level.PlayerRootPath))
            : RoomProtectionRegionAdapter.Collect(art));
        Control formalHud = GetNodeOrNull<Control>("../../FormalHud");
        regions.AddRange(CollectVisibleFormalHudRegions(formalHud));
        return regions;
    }

    internal static IReadOnlyList<Rect2> CollectVisibleFormalHudRegions(Control formalHud)
    {
        var regions = new List<Rect2>();
        if (formalHud == null || !formalHud.Visible) return regions;
        foreach (string path in ProtectedFormalHudPaths)
        {
            Control control = formalHud.GetNodeOrNull<Control>(path);
            if (control?.Visible == true && control.GetGlobalRect().HasArea())
                regions.Add(control.GetGlobalRect());
        }
        return regions;
    }

    internal static Rect2 PlacePrompt(Rect2 safeArea, Vector2 target, Vector2 promptSize,
        IReadOnlyList<Rect2> protectedRegions)
    {
        Rect2 prompt = ProtectedPromptPlacer.Place(safeArea, target, promptSize,
            protectedRegions);
        return prompt.HasArea()
            ? prompt
            : ProtectedPromptPlacer.Place(safeArea, safeArea.GetCenter(), promptSize,
                protectedRegions);
    }

    public override void _Draw()
    {
        if (Size.X < 96f || Size.Y < 96f) return;
        Rect2 safe = new(new Vector2(48, 48), Size - new Vector2(96, 96));
        foreach (PrologueGuidanceTargetView target in _targets)
        {
            Vector2 point = target.IsOnScreen ? target.ViewportPosition : new Vector2(
                Mathf.Clamp(target.ViewportPosition.X, safe.Position.X, safe.End.X),
                Mathf.Clamp(target.ViewportPosition.Y, safe.Position.Y, safe.End.Y));
            Color color = new("d4a83f");
            if (target.IsOnScreen)
            {
                Vector2[] diamond =
                [point + Vector2.Up * 12, point + Vector2.Right * 12,
                 point + Vector2.Down * 12, point + Vector2.Left * 12];
                DrawColoredPolygon(diamond, new Color(color, .78f));
                DrawPolyline([diamond[0], diamond[1], diamond[2], diamond[3], diamond[0]],
                    new Color("f0dfad"), 2f);
                DrawCircle(point, 3f, new Color("9edbd2"));
            }
            else
            {
                Vector2 forward = target.Direction == Vector2.Zero ? Vector2.Right : target.Direction.Normalized();
                Vector2 side = forward.Orthogonal();
                DrawColoredPolygon([point + forward * 18, point - forward * 12 + side * 10,
                    point - forward * 12 - side * 10], color);
            }
        }
    }
}
