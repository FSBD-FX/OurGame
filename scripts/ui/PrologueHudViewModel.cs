using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Player;

namespace Knight.Ui;

public enum HealthShardState
{
    Empty,
    Filled,
    Broken
}

public sealed record PrologueHudViewModel(
    IReadOnlyList<HealthShardState> HealthSegments,
    float ThreadReadyRatio,
    string Objective,
    string PromptGlyph,
    string PromptText,
    bool BossVisible,
    string BossName,
    int BossCurrent,
    int BossMax,
    int BossPhase,
    bool BossArmored,
    bool BossExposed,
    bool RecoveryVisible,
    string RecoveryReason,
    bool CompletionVisible,
    string CompletionTime,
    int Deaths,
    bool HiddenFound,
    bool ShortcutOpen,
    ThreadCastState ThreadState,
    ThreadFailureReason ThreadFailure,
    string ThreadStateText,
    float ObjectiveTransitionSeconds,
    PlayerActionMode ActionMode,
    int ActiveSlot,
    float EnergyRatio,
    float HeatRatio,
    bool IsOverheated,
    PlayerActionRejectionReason ActionRejection)
{
    public const float ObjectiveExpandDurationSeconds = 2.25f;
    public bool ObjectiveExpanded => !string.IsNullOrWhiteSpace(Objective) &&
        ObjectiveTransitionSeconds < ObjectiveExpandDurationSeconds;

    public static PrologueHudViewModel Gameplay(int health, int maxHealth, float threadReadyRatio,
        string objective, string promptGlyph, string promptText) => new(
            BuildHealth(health, maxHealth), Mathf.Clamp(threadReadyRatio, 0f, 1f),
            objective ?? string.Empty, promptGlyph ?? string.Empty, promptText ?? string.Empty,
            false, string.Empty, 0, 1, 1, false, false,
            false, string.Empty, false, string.Empty, 0, false, false,
            ThreadCastState.Idle, ThreadFailureReason.None, string.Empty, 0f,
            PlayerActionMode.Attack, 1, 1f, 0f, false, PlayerActionRejectionReason.None);

    public PrologueHudViewModel WithHealth(int current, int max) =>
        this with { HealthSegments = BuildHealth(current, max) };

    public PrologueHudViewModel WithThread(float ratio)
    {
        float readyRatio = Mathf.Clamp(ratio, 0f, 1f);
        return this with
        {
            ThreadReadyRatio = readyRatio,
            ThreadStateText = string.Empty
        };
    }

    public PrologueHudViewModel WithThreadCast(ThreadCastState state, ThreadFailureReason failure) =>
        this with
        {
            ThreadState = state,
            ThreadFailure = failure,
            ThreadStateText = string.Empty
        };

    public PrologueHudViewModel WithActionState(PlayerActionMode mode, int activeSlot, float energyRatio,
        float heatRatio, bool isOverheated, PlayerActionRejectionReason rejection) => this with
    {
        ActionMode = mode,
        ActiveSlot = Mathf.Clamp(activeSlot, 1, 4),
        EnergyRatio = Mathf.Clamp(energyRatio, 0f, 1f),
        HeatRatio = Mathf.Clamp(heatRatio, 0f, 1f),
        IsOverheated = isOverheated,
        ActionRejection = rejection
    };

    public PrologueHudViewModel WithObjective(string objective, float transitionSeconds = 0f) =>
        this with
        {
            Objective = objective ?? string.Empty,
            ObjectiveTransitionSeconds = Mathf.Max(0f, transitionSeconds)
        };

    public PrologueHudViewModel AdvanceObjectiveTransition(float deltaSeconds) => this with
    {
        ObjectiveTransitionSeconds = ObjectiveTransitionSeconds + Mathf.Max(0f, deltaSeconds)
    };

    public PrologueHudViewModel WithBoss(string name, int current, int max, int phase,
        bool armored, bool exposed) => this with
    {
        BossVisible = true,
        BossName = name ?? string.Empty,
        BossCurrent = Mathf.Clamp(current, 0, Mathf.Max(1, max)),
        BossMax = Mathf.Max(1, max),
        BossPhase = Mathf.Max(1, phase),
        BossArmored = armored,
        BossExposed = exposed
    };

    public PrologueHudViewModel WithoutBoss() => this with { BossVisible = false };

    public PrologueHudViewModel WithRecovery(string reason) => this with
    {
        RecoveryVisible = true,
        RecoveryReason = reason ?? string.Empty
    };

    public PrologueHudViewModel WithoutRecovery() => this with { RecoveryVisible = false };

    public PrologueHudViewModel WithCompletion(string time, int deaths, bool hiddenFound,
        bool shortcutOpen) => this with
    {
        CompletionVisible = true,
        CompletionTime = time ?? string.Empty,
        Deaths = Mathf.Max(0, deaths),
        HiddenFound = hiddenFound,
        ShortcutOpen = shortcutOpen
    };

    private static IReadOnlyList<HealthShardState> BuildHealth(int current, int max)
    {
        int count = Mathf.Max(1, max);
        int remaining = Mathf.Clamp(current, 0, count);
        var segments = new HealthShardState[count];
        for (int index = 0; index < count; index++)
        {
            if (index < remaining) segments[index] = HealthShardState.Filled;
            else if (index == remaining && remaining < count) segments[index] = HealthShardState.Broken;
            else segments[index] = HealthShardState.Empty;
        }
        return Array.AsReadOnly(segments);
    }

}

public readonly record struct PlayerHudActionState(
    PlayerActionMode Mode,
    int ActiveSlot,
    float EnergyRatio,
    float HeatRatio,
    bool IsOverheated,
    PlayerActionRejectionReason Rejection)
{
    public static readonly PlayerHudActionState Default = new(
        PlayerActionMode.Attack, 1, 1f, 0f, false, PlayerActionRejectionReason.None);
}

public readonly record struct PrologueHudLayout(
    Rect2 HealthRect,
    Rect2 ThreadRect,
    Rect2 ObjectiveRect,
    Rect2 GuidanceRect,
    Rect2 PromptRect,
    Rect2 BossRect,
    Rect2 ModeSelectorRect,
    Rect2 LoadoutRect,
    Rect2 ResourceClusterRect)
{
    public bool BossAvoidsProtectedRegions(IReadOnlyList<Rect2> protectedRegions,
        float clearance = 8f)
    {
        Rect2 guardedBoss = BossRect.Grow(Mathf.Max(0f, clearance));
        foreach (Rect2 region in protectedRegions)
        {
            if (guardedBoss.Intersects(region)) return false;
        }
        return true;
    }

    public static PrologueHudLayout Calculate(Vector2 viewport)
        => Calculate(viewport, 1f);

    public static PrologueHudLayout Calculate(Vector2 viewport, float uiScale)
    {
        float resolutionScale = Mathf.Max(1f, viewport.Y / 720f);
        float scale = resolutionScale * Mathf.Clamp(uiScale, .8f, 1.5f);
        Vector2 margin = new(48f * resolutionScale, 40f * resolutionScale);
        Rect2 health = new(new Vector2(margin.X, margin.Y), new Vector2(220, 72) * scale);
        Rect2 thread = new(health.End, Vector2.Zero);
        Rect2 objective = new(new Vector2(margin.X, health.End.Y + 8f * scale),
            new Vector2(426, 34) * scale);
        Rect2 prompt = new(new Vector2(viewport.X - margin.X - 390f * scale, margin.Y),
            new Vector2(390, 58) * scale);
        // Keep the boss instrument above the world-space combat band.  The former bottom-safe
        // placement cleared the floor itself but covered the sweep and ground-wave silhouettes,
        // making a mechanically valid telegraph unreadable to the player.
        const float objectiveExpandedHeightRatio = 1.42f;
        float bossTop = objective.Position.Y + objective.Size.Y * objectiveExpandedHeightRatio +
            8f * scale;
        Rect2 boss = new(new Vector2((viewport.X - 640f * scale) / 2f, bossTop),
            new Vector2(640, 62) * scale);
        Rect2 guidance = new(new Vector2(margin.X, boss.End.Y + 12f * scale),
            new Vector2(610, 104) * scale);
        const float actionModeWidth = 220f;
        const float actionModeHeight = 36f;
        const float actionResourceWidth = 194f;
        const float actionResourceHeight = 72f;
        const float actionGap = 12f;
        Vector2 actionSize = new(actionModeWidth, actionModeHeight);
        Vector2 resourceSize = new(actionResourceWidth, actionResourceHeight);
        Rect2 modeSelector = new(new Vector2(margin.X, viewport.Y - margin.Y - actionModeHeight * scale),
            actionSize * scale);
        Rect2 loadout = new(new Vector2((viewport.X - actionModeWidth * scale) * .5f,
            viewport.Y - margin.Y - actionModeHeight * scale), actionSize * scale);
        Rect2 resources = new(new Vector2(health.End.X + actionGap * resolutionScale, margin.Y),
            resourceSize * scale);
        if (resources.End.X > prompt.Position.X - actionGap * resolutionScale)
            resources = new Rect2(new Vector2(viewport.X - margin.X - resourceSize.X * scale,
                prompt.End.Y + actionGap * resolutionScale), resourceSize * scale);
        return new PrologueHudLayout(health, thread, objective, guidance, prompt, boss,
            modeSelector, loadout, resources);
    }
}
