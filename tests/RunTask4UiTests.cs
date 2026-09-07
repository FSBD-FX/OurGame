using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Localization;
using Knight.Prologue;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Settings;
using Knight.Shared;
using Knight.Ui;
using Knight.Vfx;

namespace Knight.Tests;

/// <summary>
/// Task 4's independently-owned acceptance runner.  It deliberately avoids the user-owned
/// untracked RunTask11UiTests.cs and proves the guidance/reticle contract from a HEAD-only tree.
/// </summary>
public partial class RunTask4UiTests : SceneTree
{
    private static readonly IReadOnlyDictionary<StringName, string> Glyphs =
        new Dictionary<StringName, string>
        {
            [InputActions.MoveLeft] = "A",
            [InputActions.MoveRight] = "D",
            [InputActions.MoveUp] = "W",
            [InputActions.MoveDown] = "S",
            [InputActions.Jump] = "SPACE",
            [InputActions.SelectAttackMode] = "Q",
            [InputActions.SelectGrappleMode] = "E",
            [InputActions.PrimaryAction] = "LMB",
            [InputActions.Shield] = "RMB",
            [InputActions.Slot1] = "1",
            [InputActions.Slot2] = "2",
            [InputActions.Slot3] = "3",
            [InputActions.Slot4] = "4"
        };

    public override async void _Initialize()
    {
        var failures = new List<string>();
        try
        {
            LoadStrings();
            Check(failures, AssertApprovedClawCopy);
            await CheckAsync(failures, AssertDescentTargetsResolveGeneratedTerrain);
            Check(failures, AssertSixReticleSignaturesAndSoundBindings);
            Check(failures, AssertFeedbackUsesExistingDeterministicCues);
            if (failures.Count > 0)
                throw new InvalidOperationException("TASK4_UI_RED_EXPECTED " + string.Join(" | ", failures));
            GD.Print("TASK11_UI_READABILITY_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }

    private static void Check(List<string> failures, Action assertion)
    {
        try { assertion(); }
        catch (Exception exception)
        {
            failures.Add(exception.Message);
            GD.Print("TASK4_RED_CHECK_FAIL " + exception.Message);
        }
    }

    private static async Task CheckAsync(List<string> failures, Func<Task> assertion)
    {
        try { await assertion(); }
        catch (Exception exception)
        {
            failures.Add(exception.Message);
            GD.Print("TASK4_RED_CHECK_FAIL " + exception.Message);
        }
    }

    private static void AssertApprovedClawCopy()
    {
        PrologueGuidanceInstruction chinese = GrappleInstruction(GameLanguage.ChineseSimplified);
        PrologueGuidanceInstruction english = GrappleInstruction(GameLanguage.English);
        foreach (string token in new[] { "E", "LMB", "A", "D", "W", "S", "SPACE" })
        {
            TestAssert.True(chinese.StepText.Contains(token, StringComparison.Ordinal),
                $"TASK4_COPY_ZH_LIVE_GLYPH_{token}");
            TestAssert.True(english.StepText.Contains(token, StringComparison.Ordinal),
                $"TASK4_COPY_EN_LIVE_GLYPH_{token}");
        }
        foreach (string phrase in new[] { "按住 LMB", "A/D", "W/S", "SPACE 跃升一次", "松开 LMB", "冰面", "封闭表面" })
            TestAssert.True(chinese.StepText.Contains(phrase, StringComparison.Ordinal),
                $"TASK4_COPY_ZH_APPROVED_OPERATION_{phrase}");
        foreach (string phrase in new[] { "hold LMB", "A/D", "W/S", "SPACE", "one boost", "release LMB", "ice", "sealed" })
            TestAssert.True(english.StepText.Contains(phrase, StringComparison.OrdinalIgnoreCase),
                $"TASK4_COPY_EN_APPROVED_OPERATION_{phrase}");

        string all = chinese.StepText + "\n" + english.StepText;
        foreach (string stale in new[]
                 {
                     "GripStone", " J ", " K ", "J/K", "Space disconnect", "double jump", "二段跳",
                     "换房", "room swap", "AwakeningCourtyard", "BrokenToothCorridor", "UpperPendulum",
                     "LowerBuriedMachinery", "BellChainMechanism", "FallenBellkeeperArena"
                 })
            TestAssert.True(!all.Contains(stale, StringComparison.OrdinalIgnoreCase),
                $"TASK4_COPY_EXCLUDES_STALE_{stale}");

        PrologueGuidanceInstruction disabled = PrologueGuidanceModel.Evaluate(new(
            PrologueObjective.LearnGrappleMode, PrologueSectorId.CanyonDescent,
            PrologueBossGuidanceState.None, 0, GameLanguage.English, false), Glyphs);
        TestAssert.True(!disabled.Visible && disabled.Targets.Count == 0,
            "TASK4_COPY_FIRST_LEVEL_SETTING_STILL_HIDES_GUIDANCE");
    }

    private async Task AssertDescentTargetsResolveGeneratedTerrain()
    {
        PrologueSector descent = GD.Load<PackedScene>(
                "res://scenes/prologue/sectors/CanyonDescent.tscn")
            .Instantiate<PrologueSector>();
        Root.AddChild(descent);
        try
        {
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            AssertTerrainTarget(descent, PrologueGuidanceTargetKey.DescentGripStone,
                GrappleSurfaceMaterial.GripStone, "TASK4_TARGET_DESCENT_GRIP_TERRAIN_PROFILE");
            AssertTerrainTarget(descent, PrologueGuidanceTargetKey.DescentIce,
                GrappleSurfaceMaterial.Ice, "TASK4_TARGET_DESCENT_ICE_EXPLICIT_PROFILE");
        }
        finally
        {
            descent.QueueFree();
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static void AssertTerrainTarget(PrologueSector sector, PrologueGuidanceTargetKey key,
        GrappleSurfaceMaterial expectedMaterial, string assertion)
    {
        TestAssert.True(PrologueGuidanceTargetResolver.TryResolve(sector,
                PrologueSectorId.CanyonDescent, key, out Node2D target),
            assertion + "_RESOLVES");
        TestAssert.True(target is GrappleSurface2D profile && profile.UseCollisionHitPoint &&
                        profile.Material == expectedMaterial && profile.GetParent() is StaticBody2D,
            assertion);
    }

    private static void AssertSixReticleSignaturesAndSoundBindings()
    {
        var expected = new[]
        {
            ("Attack", "open-circle", PrologueAudioCue.ModeAttack),
            ("Grippable", "diamond", PrologueAudioCue.ClawLatch),
            ("Ice", "crossed-ice-bars", PrologueAudioCue.ClawIceSlip),
            ("Forbidden", "blocked-hex-crossbar", PrologueAudioCue.ClawFail),
            ("Obstructed", "dashed-obstruction-arc", PrologueAudioCue.ThreadFail),
            ("OutOfRange", "dual-range-ring", PrologueAudioCue.ThreadPreview)
        };
        foreach ((string stateName, _, _) in expected)
            TestAssert.True(Enum.TryParse(stateName, out PrologueAimReticleState _),
                $"TASK4_RETICLE_REQUIRED_STATE_{stateName}");
        PrologueAimReticleState[] distinctStates = Enum.GetValues<PrologueAimReticleState>()
            .Distinct().ToArray();
        TestAssert.Equal(6, distinctStates.Length,
            "TASK4_RETICLE_SIX_SEMANTIC_STATES_INCLUDING_FORBIDDEN");

        PropertyInfo cueProperty = typeof(PrologueAimReticleStyle).GetProperty("AudioCue");
        TestAssert.True(cueProperty != null && cueProperty.PropertyType == typeof(PrologueAudioCue),
            "TASK4_RETICLE_SOUND_SIGNATURE_IS_EXPLICIT");
        var geometry = new HashSet<string>(StringComparer.Ordinal);
        var color = new HashSet<uint>();
        var sound = new HashSet<PrologueAudioCue>();
        foreach ((string stateName, string expectedGeometry, PrologueAudioCue expectedCue) in expected)
        {
            PrologueAimReticleState state = Enum.Parse<PrologueAimReticleState>(stateName);
            PrologueAimReticleStyle style = PrologueAimReticle.StyleFor(state);
            TestAssert.Equal(expectedGeometry, style.GeometrySignature,
                $"TASK4_RETICLE_SIGNATURE_{stateName}");
            TestAssert.Equal(expectedCue, (PrologueAudioCue)cueProperty.GetValue(style),
                $"TASK4_RETICLE_CUE_{stateName}");
            TestAssert.True(geometry.Add(style.GeometrySignature),
                $"TASK4_RETICLE_UNIQUE_GEOMETRY_{state}");
            TestAssert.True(color.Add(style.Color.ToRgba32()),
                $"TASK4_RETICLE_UNIQUE_COLOR_{state}");
            TestAssert.True(sound.Add((PrologueAudioCue)cueProperty.GetValue(style)),
                $"TASK4_RETICLE_UNIQUE_SOUND_{state}");
        }
    }

    private static void AssertFeedbackUsesExistingDeterministicCues()
    {
        MethodInfo cueForFrame = typeof(PrologueAudioDirector).GetMethod("CueForClawFrame",
            BindingFlags.Public | BindingFlags.Static);
        MethodInfo rootForCue = typeof(PrologueVfxDirector).GetMethod("Task10RootForCue",
            BindingFlags.Public | BindingFlags.Static);
        TestAssert.True(cueForFrame != null, "TASK4_AUDIO_FAILURE_CUE_MAPPING_IS_EXPLICIT");
        TestAssert.True(rootForCue != null, "TASK4_VFX_FAILURE_ROOT_MAPPING_IS_EXPLICIT");
        var mappings = new[]
        {
            (MechanicalClawFailure.IceSlip, PrologueAudioCue.ClawIceSlip, "ClawMaterialHit", "ice shards"),
            (MechanicalClawFailure.Forbidden, PrologueAudioCue.ClawFail, "ClawMaterialHit", "dry impact"),
            (MechanicalClawFailure.Obstructed, PrologueAudioCue.ThreadFail, "ClawMaterialHit", "blocked spark"),
            (MechanicalClawFailure.OutOfRange, PrologueAudioCue.ThreadPreview, "RopeTension", "no-impact pulse")
        };
        var colors = new HashSet<uint>();
        foreach ((MechanicalClawFailure failure, PrologueAudioCue expectedCue, string root,
                     string feedback) in mappings)
        {
            MechanicalClawFrame frame = new(1UL, MechanicalClawState.Failed, failure,
                Vector2.Right, 1f, Vector2.Zero, true);
            TestAssert.Equal((PrologueAudioCue?)expectedCue,
                PrologueAudioDirector.CueForClawFrame(frame),
                $"TASK4_AUDIO_{failure}_{feedback}");
            TestAssert.Equal(root, PrologueVfxDirector.Task10RootForCue(expectedCue),
                $"TASK4_VFX_{failure}_{feedback}");
            TestAssert.True(colors.Add(PrologueVfxDirector.Task10Color(expectedCue).ToRgba32()),
                $"TASK4_VFX_{failure}_HAS_DISTINCT_COLOR");
            TestAssert.True(PrologueVfxDirector.Task10RayCount(expectedCue) > 0,
                $"TASK4_VFX_{failure}_HAS_VISIBLE_RAYS");
        }
    }

    private static PrologueGuidanceInstruction GrappleInstruction(GameLanguage language) =>
        PrologueGuidanceModel.Evaluate(new(PrologueObjective.LearnGrappleMode,
            PrologueSectorId.CanyonDescent, PrologueBossGuidanceState.None, 0, language, true), Glyphs);

    private static void LoadStrings()
    {
        string path = ProjectSettings.GlobalizePath("res://assets/localization/prologue-ui.json");
        PrologueStrings.Load(File.ReadAllText(path));
    }
}
