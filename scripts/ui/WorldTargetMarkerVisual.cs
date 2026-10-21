using System;
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
        "ModeSelector", "Loadout", "ResourceCluster", "Boss", "Minimap"
    ];

    public string RenderedStepText { get; private set; } = string.Empty;
    public int RenderedTargetCount => _targets.Count;
    public bool IsExpandedTeaching { get; private set; }
    public Rect2 PromptRect { get; private set; }
    public bool HasVisiblePrompt { get; private set; }
    public Rect2 RenderedTextBounds { get; private set; }
    public bool RenderedTextContained { get; private set; }
    public bool MarkerlessSafeEdgeEnabled { get; private set; }
    internal Vector2 LastPromptSize { get; private set; }
    // Narrow owner invalidation: resize/visibility can change coverage between guidance ticks.
    public event Action EarlyLessonCoverageChanged;

    private readonly List<PrologueGuidanceTargetView> _targets = new();
    private Label _text;

    public override void _EnterTree()
    {
        VisibilityChanged += NotifyEarlyLessonCoverageChanged;
        BindText();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        BindText();
    }

    public override void _ExitTree()
    {
        VisibilityChanged -= NotifyEarlyLessonCoverageChanged;
        DetachText();
        HasVisiblePrompt = false;
        RenderedTextContained = false;
        NotifyEarlyLessonCoverageChanged();
    }

    private void BindText()
    {
        Label next = GetNodeOrNull<Label>("Text");
        if (ReferenceEquals(next, _text)) return;
        DetachText();
        _text = next;
        if (_text == null) return;
        _text.VisibilityChanged += NotifyEarlyLessonCoverageChanged;
        _text.TreeExiting += OnTextExiting;
    }

    private void DetachText()
    {
        if (GodotObject.IsInstanceValid(_text))
        {
            _text.VisibilityChanged -= NotifyEarlyLessonCoverageChanged;
            _text.TreeExiting -= OnTextExiting;
        }
        _text = null;
    }

    private void OnTextExiting()
    {
        DetachText();
        HasVisiblePrompt = false;
        RenderedTextContained = false;
        NotifyEarlyLessonCoverageChanged();
    }

    private void NotifyEarlyLessonCoverageChanged() => EarlyLessonCoverageChanged?.Invoke();

    public void SetExpandedTeaching(bool expanded) => IsExpandedTeaching = expanded;
    public void SetMarkerlessSafeEdgeEnabled(bool enabled)
    {
        if (MarkerlessSafeEdgeEnabled == enabled) return;
        MarkerlessSafeEdgeEnabled = enabled;
        if (IsInsideTree() && !string.IsNullOrEmpty(RenderedStepText)) ApplyPromptLayout();
    }

    internal static Rect2 PlaceMarkerlessPrompt(Rect2 safeArea, Vector2 promptSize,
        IReadOnlyList<Rect2> protectedRegions)
    {
        foreach (Vector2 anchor in ResolveEdgeProbeAnchors(safeArea, promptSize,
                     protectedRegions))
        {
            Rect2 placed = ProtectedPromptPlacer.Place(safeArea, anchor, promptSize,
                protectedRegions);
            if (placed.HasArea() && TouchesSafeEdge(placed, safeArea)) return placed;
        }
        return default;
    }

    private static IReadOnlyList<Vector2> ResolveEdgeProbeAnchors(Rect2 safeArea,
        Vector2 promptSize, IReadOnlyList<Rect2> protectedRegions)
    {
        var anchors = new List<Vector2>();
        AddVerticalEdgeAnchors(anchors, safeArea, promptSize, protectedRegions,
            safeArea.Position.X);
        AddVerticalEdgeAnchors(anchors, safeArea, promptSize, protectedRegions,
            safeArea.End.X);
        AddHorizontalEdgeAnchors(anchors, safeArea, promptSize, protectedRegions,
            safeArea.Position.Y);
        AddHorizontalEdgeAnchors(anchors, safeArea, promptSize, protectedRegions,
            safeArea.End.Y);
        return anchors;
    }

    private static void AddVerticalEdgeAnchors(List<Vector2> anchors, Rect2 safeArea,
        Vector2 promptSize, IReadOnlyList<Rect2> protectedRegions, float edgeX)
    {
        float stripX = edgeX <= safeArea.GetCenter().X
            ? safeArea.Position.X
            : safeArea.End.X - promptSize.X;
        var boundaries = new List<float> { safeArea.Position.Y, safeArea.End.Y };
        Rect2 strip = new(new Vector2(stripX, safeArea.Position.Y),
            new Vector2(promptSize.X, safeArea.Size.Y));
        AddIntersectingBoundaries(boundaries, strip, protectedRegions, vertical: true,
            safeArea.Position.Y, safeArea.End.Y);
        AddGapMidpoints(anchors, boundaries, value => new Vector2(edgeX, value));
    }

    private static void AddHorizontalEdgeAnchors(List<Vector2> anchors, Rect2 safeArea,
        Vector2 promptSize, IReadOnlyList<Rect2> protectedRegions, float edgeY)
    {
        float stripY = edgeY <= safeArea.GetCenter().Y
            ? safeArea.Position.Y
            : safeArea.End.Y - promptSize.Y;
        var boundaries = new List<float> { safeArea.Position.X, safeArea.End.X };
        Rect2 strip = new(new Vector2(safeArea.Position.X, stripY),
            new Vector2(safeArea.Size.X, promptSize.Y));
        AddIntersectingBoundaries(boundaries, strip, protectedRegions, vertical: false,
            safeArea.Position.X, safeArea.End.X);
        AddGapMidpoints(anchors, boundaries, value => new Vector2(value, edgeY));
    }

    private static void AddIntersectingBoundaries(List<float> boundaries, Rect2 edgeStrip,
        IReadOnlyList<Rect2> protectedRegions, bool vertical, float minimum, float maximum)
    {
        if (protectedRegions == null) return;
        foreach (Rect2 region in protectedRegions)
        {
            if (!region.HasArea() || !region.Intersects(edgeStrip)) continue;
            float start = vertical ? region.Position.Y : region.Position.X;
            float end = vertical ? region.End.Y : region.End.X;
            boundaries.Add(Mathf.Clamp(start, minimum, maximum));
            boundaries.Add(Mathf.Clamp(end, minimum, maximum));
        }
    }

    private static void AddGapMidpoints(List<Vector2> anchors, List<float> boundaries,
        System.Func<float, Vector2> project)
    {
        boundaries.Sort();
        for (int index = 1; index < boundaries.Count; index++)
        {
            float first = boundaries[index - 1];
            float second = boundaries[index];
            if (second - first <= .5f) continue;
            anchors.Add(project((first + second) * .5f));
        }
    }

    private static bool TouchesSafeEdge(Rect2 candidate, Rect2 safeArea)
    {
        const float edgeInset = 16.01f;
        return candidate.Position.X <= safeArea.Position.X + edgeInset ||
               candidate.End.X >= safeArea.End.X - edgeInset ||
               candidate.Position.Y <= safeArea.Position.Y + edgeInset ||
               candidate.End.Y >= safeArea.End.Y - edgeInset;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && IsInsideTree() && !string.IsNullOrEmpty(RenderedStepText))
            ApplyPromptLayout();
    }

    public void Present(PrologueGuidanceInstruction instruction,
        IReadOnlyList<PrologueGuidanceTargetView> targets)
    {
        BindText();
        RenderedStepText = instruction.StepText;
        _targets.Clear();
        foreach (PrologueGuidanceTargetView target in targets) _targets.Add(target);
        if (_text != null)
        {
            _text.Text = IsExpandedTeaching
                ? instruction.HeadingText + "  /  " + instruction.StepText +
                  (string.IsNullOrEmpty(instruction.HintText)
                      ? string.Empty : "\n" + instruction.HintPrefix + instruction.HintText)
                : instruction.StepText;
        }
        ApplyPromptLayout();
        Visible = instruction.Visible;
        NotifyEarlyLessonCoverageChanged();
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
        NotifyEarlyLessonCoverageChanged();
        QueueRedraw();
    }

    private void ApplyPromptLayout()
    {
        BindText();
        Vector2 viewport = Size;
        if (viewport.X < 1f || viewport.Y < 1f) viewport = GetViewportRect().Size;
        Rect2 safe = new(new Vector2(48, 40), viewport - new Vector2(96, 80));
        // One reading column for every target and teaching state. World motion must
        // never choose the position or width of actionable screen-space text.
        Vector2 promptSize = new(320, IsExpandedTeaching ? 88 : 48);
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
        LastPromptSize = promptSize;
        PromptRect = PlaceFixedReadingColumn(safe, promptSize);

        HasVisiblePrompt = PromptRect.HasArea();
        if (!HasVisiblePrompt)
        {
            SetPromptContentVisible(false);
            RenderedTextBounds = default;
            RenderedTextContained = false;
            NotifyEarlyLessonCoverageChanged();
            return;
        }

        SetPromptContentVisible(true);

        Control backdrop = GetNodeOrNull<Control>("Backdrop");
        Control topRule = GetNodeOrNull<Control>("TopRule");
        if (backdrop != null)
        {
            backdrop.Position = PromptRect.Position;
            backdrop.Size = PromptRect.Size;
            if (backdrop is ColorRect colorRect)
                colorRect.Color = new Color(.04f, .065f, .085f, .62f);
        }
        if (topRule != null)
        {
            topRule.Position = PromptRect.Position;
            topRule.Size = new Vector2(PromptRect.Size.X, 2f);
            topRule.Visible = false;
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
        else RenderedTextContained = false;
        NotifyEarlyLessonCoverageChanged();
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
        if (topRule != null) topRule.Visible = false;
        if (_text != null) _text.Visible = visible;
    }

    private Rect2 PlaceFixedReadingColumn(Rect2 safeArea, Vector2 promptSize)
    {
        Control formalHud = GetNodeOrNull<Control>("../../FormalHud");
        var protectedHud = new List<Rect2>(CollectVisibleFormalHudRegions(formalHud));
        // Reserve the boss track before it appears. Its visibility may change
        // during combat; the reading anchor must not jump with it.
        Control boss = formalHud?.GetNodeOrNull<Control>("Boss");
        if (boss != null && boss.GetGlobalRect().HasArea())
            protectedHud.Add(boss.GetGlobalRect());
        float top = safeArea.Position.Y + 132f;
        Control health = formalHud?.GetNodeOrNull<Control>("Health");
        float scale = health != null && health.Size.Y > 0f ? health.Size.Y / 72f : 1f;
        Control objective = formalHud?.GetNodeOrNull<Control>("ObjectiveRibbon");
        if (health != null && health.GetGlobalRect().HasArea())
            top = Mathf.Max(top, health.GetGlobalRect().End.Y + 12f * scale);
        if (objective != null && objective.GetGlobalRect().HasArea())
        {
            // Reserve the expanded ribbon even after it collapses or guidance
            // covers its duplicate text, so neither event shifts the reading anchor.
            Rect2 reserved = objective.GetGlobalRect();
            reserved.Size = new Vector2(reserved.Size.X,
                Mathf.Max(reserved.Size.Y, 34f * 1.42f * scale));
            protectedHud.Add(reserved);
            top = Mathf.Max(top, reserved.End.Y + 12f * scale);
        }
        // Top HUD instruments (including a visible boss bar) reserve the whole
        // rail before text height is considered. Expansion can only grow down.
        foreach (Rect2 region in protectedHud)
        {
            bool crossesRail = region.End.X > safeArea.Position.X - 8f &&
                               region.Position.X < safeArea.Position.X + promptSize.X + 8f;
            if (crossesRail && region.Position.Y < safeArea.GetCenter().Y)
                top = Mathf.Max(top, region.End.Y + 12f * scale);
        }
        Rect2 card = new(new Vector2(safeArea.Position.X, top), promptSize);
        if (!safeArea.Encloses(card)) return default;
        foreach (Rect2 region in protectedHud)
            if (region.HasArea() && card.Intersects(region.Grow(8f))) return default;
        return card;
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
            bool reservedEarlyRibbon = path == "ObjectiveRibbon" &&
                formalHud is PrologueFormalHud { EarlyLessonRibbonSuppressed: true };
            if (control != null && (control.Visible || reservedEarlyRibbon) &&
                (path != "Minimap" || control.IsVisibleInTree()) && control.GetGlobalRect().HasArea())
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
            Color color = new("9edbd2");
            if (target.IsOnScreen)
            {
                DrawCircle(point, 9f, new Color(color, .08f));
                DrawCircle(point, 3f, new Color(color, .48f));
            }
            else
            {
                Vector2 forward = target.Direction == Vector2.Zero ? Vector2.Right : target.Direction.Normalized();
                Vector2 side = forward.Orthogonal();
                DrawColoredPolygon([point + forward * 9, point - forward * 6 + side * 5,
                    point - forward * 6 - side * 5], new Color(color, .4f));
            }
        }
    }
}
