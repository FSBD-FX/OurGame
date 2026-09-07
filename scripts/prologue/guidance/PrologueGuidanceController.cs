using System;
using System.Collections.Generic;
using Godot;
using Knight.Boss;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;

namespace Knight.Prologue.Guidance;

public partial class PrologueGuidanceController : Node
{
    [Export] public NodePath LevelPath { get; set; } = new("..");
    [Export] public NodePath CompletionPath { get; set; } = new("../Completion");
    [Export] public NodePath VisualPath { get; set; } = new("../Hud/Guidance/VisualRoot");

    public bool GuidanceEnabled { get; private set; } = true;
    public GameLanguage Language { get; private set; } = GameLanguage.ChineseSimplified;
    public int SameSectorFailureCount { get; private set; }
    public PrologueGuidanceInstruction CurrentInstruction { get; private set; } =
        PrologueGuidanceInstruction.Hidden;

    private PrologueLevelController _level;
    private PrologueCompletionController _completion;
    private IPrologueGuidanceVisual _visual;
    private PrologueSectorId? _observedSector;
    private readonly HashSet<string> _missingWarnings = new(StringComparer.Ordinal);

    public override void _Ready()
    {
        _level = GetNode<PrologueLevelController>(LevelPath);
        _completion = GetNode<PrologueCompletionController>(CompletionPath);
        _visual = GetNodeOrNull(VisualPath) as IPrologueGuidanceVisual;
        _level.RecoveryCompleted += OnRecoveryCompleted;
        Refresh();
    }

    public override void _Process(double delta) => Refresh();

    public override void _ExitTree()
    {
        if (GodotObject.IsInstanceValid(_level)) _level.RecoveryCompleted -= OnRecoveryCompleted;
    }

    public void SetGuidanceEnabled(bool enabled)
    {
        GuidanceEnabled = enabled;
        Refresh();
    }

    public void ToggleGuidance() => SetGuidanceEnabled(!GuidanceEnabled);

    public void RefreshNow() => Refresh();

    public void SetLanguage(GameLanguage language)
    {
        Language = Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        Refresh();
    }

    private void Refresh()
    {
        if (_level == null || _completion == null || !_level.CurrentSector.HasValue ||
            !GodotObject.IsInstanceValid(_level.CurrentSectorRoot))
        {
            Hide();
            return;
        }

        PrologueSectorId sectorId = _level.CurrentSector.Value;
        if (_observedSector != sectorId)
        {
            _observedSector = sectorId;
            SameSectorFailureCount = 0;
        }
        CurrentInstruction = PrologueGuidanceModel.Evaluate(new PrologueGuidanceContext(
            _completion.CurrentObjective, sectorId, ResolveBossState(_level.CurrentSectorRoot),
            SameSectorFailureCount, Language, GuidanceEnabled), ResolveInputMapGlyphs());
        Present(sectorId);
    }

    private void Present(PrologueSectorId sectorId)
    {
        if (!CurrentInstruction.Visible)
        {
            SetExpandedTeaching(false);
            _visual?.Clear();
            return;
        }
        if (_visual == null) return;

        SetExpandedTeaching(SameSectorFailureCount >= 2);

        var views = new List<PrologueGuidanceTargetView>();
        PrologueSector sector = _level.CurrentSectorRoot;
        foreach (PrologueGuidanceTargetKey key in CurrentInstruction.Targets)
        {
            if (!PrologueGuidanceTargetResolver.TryResolve(sector, sectorId, key, out Node2D target))
            {
                WarnMissing(sectorId, key);
                continue;
            }
            Vector2 screen = target.GetGlobalTransformWithCanvas().Origin;
            Rect2 viewportRect = _level.GetViewportRect();
            Vector2 clamped = new(
                Mathf.Clamp(screen.X, 28f, viewportRect.Size.X - 28f),
                Mathf.Clamp(screen.Y, 28f, viewportRect.Size.Y - 28f));
            Vector2 offset = screen - clamped;
            bool onScreen = viewportRect.HasPoint(screen);
            views.Add(new PrologueGuidanceTargetView(clamped,
                offset.LengthSquared() > .001f ? offset.Normalized() : Vector2.Zero,
                offset.Length(), onScreen));
        }
        _visual.Present(CurrentInstruction, views);
    }

    private void Hide()
    {
        CurrentInstruction = PrologueGuidanceInstruction.Hidden;
        SetExpandedTeaching(false);
        _visual?.Clear();
    }

    private void SetExpandedTeaching(bool expanded)
    {
        if (_visual is WorldTargetMarkerVisual marker) marker.SetExpandedTeaching(expanded);
    }

    private void OnRecoveryCompleted(string reason)
    {
        if (reason is not ("FALL RESET" or "DEATH RESET") || !_level.CurrentSector.HasValue) return;
        if (_observedSector == _level.CurrentSector.Value) SameSectorFailureCount++;
        else
        {
            _observedSector = _level.CurrentSector.Value;
            SameSectorFailureCount = 1;
        }
        Refresh();
    }

    private static IReadOnlyDictionary<StringName, string> ResolveInputMapGlyphs()
    {
        var result = new Dictionary<StringName, string>();
        foreach (StringName action in GuidanceActions)
        {
            string glyph = null;
            foreach (InputEvent inputEvent in InputMap.ActionGetEvents(action))
            {
                if (inputEvent is InputEventKey key)
                {
                    Key code = key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode;
                    glyph = OS.GetKeycodeString(code).ToUpperInvariant();
                    break;
                }
                if (inputEvent is InputEventMouseButton mouse)
                {
                    glyph = mouse.ButtonIndex switch
                    {
                        MouseButton.Left => "LMB",
                        MouseButton.Right => "RMB",
                        _ => $"MOUSE {mouse.ButtonIndex}"
                    };
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(glyph))
                throw new InvalidOperationException(
                    $"First-level guidance requires a live InputMap binding for '{action}'.");
            result.Add(action, glyph);
        }
        return result;
    }

    private static PrologueBossGuidanceState ResolveBossState(PrologueSector sector)
    {
        BossArenaController arena = sector.GetNodeOrNull<BossArenaController>(
            "Gameplay/BossArenaController");
        if (arena == null || !arena.IsEncounterActive) return PrologueBossGuidanceState.Inactive;
        return arena.CurrentBoss?.CurrentFrame.State == FallenBellkeeperState.Exposed
            ? PrologueBossGuidanceState.ChainExposed
            : PrologueBossGuidanceState.Active;
    }

    private void WarnMissing(PrologueSectorId sector, PrologueGuidanceTargetKey key)
    {
        string id = $"{sector}/{key}";
        if (_missingWarnings.Add(id))
            GD.PushWarning($"Guidance target '{key}' is missing in current sector '{sector}'.");
    }

    private static readonly StringName[] GuidanceActions =
    [
        InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveUp, InputActions.MoveDown,
        InputActions.Jump, InputActions.SelectAttackMode, InputActions.SelectGrappleMode,
        InputActions.PrimaryAction, InputActions.Shield, InputActions.Slot1, InputActions.Slot2,
        InputActions.Slot3, InputActions.Slot4
    ];
}
