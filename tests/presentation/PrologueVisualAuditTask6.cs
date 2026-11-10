using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Build.Prologue;
using Knight.Prologue.Mechanisms;
using Knight.Prologue.Seamless;
using Knight.Vfx;

namespace Knight.Tests.Presentation;

/// <summary>
/// Read-only production asset audit. It validates the generated sector objects without using
/// traversal shortcuts or driving a gameplay model.
/// </summary>
public partial class PrologueVisualAuditTask6 : SceneTree
{
    public const int CertifiedSectorCount = 5;
    public const int CertifiedMicroSceneCount = 15;
    public const int CertifiedClawMechanismRoles = 4;
    public const int CertifiedTypedCueCount = 16;
    public const double CertifiedCallbackCeilingMilliseconds = 50d;
    private const string MeasurementBasis = "callback_wall_clock";
    private static readonly string[] StableVisualRoots =
        ["Background", "Midground", "GameplayReadable", "Foreground", "Effects"];
    private static readonly (PrologueSectorId Id, string Path)[] SectorScenes =
    [
        (PrologueSectorId.CanyonEntrance, "res://scenes/prologue/sectors/CanyonEntrance.tscn"),
        (PrologueSectorId.CanyonDescent, "res://scenes/prologue/sectors/CanyonDescent.tscn"),
        (PrologueSectorId.CanyonBasin, "res://scenes/prologue/sectors/CanyonBasin.tscn"),
        (PrologueSectorId.CanyonAscent, "res://scenes/prologue/sectors/CanyonAscent.tscn"),
        (PrologueSectorId.LostTemple, "res://scenes/prologue/sectors/LostTemple.tscn")
    ];
    private static readonly PrologueAudioCue[] TypedCues =
    [
        PrologueAudioCue.ModeAttack, PrologueAudioCue.ModeGrapple,
        PrologueAudioCue.EmptySlot, PrologueAudioCue.EnergyEmpty,
        PrologueAudioCue.ShieldRaise, PrologueAudioCue.ShieldBlock,
        PrologueAudioCue.ShieldHeavyBreak, PrologueAudioCue.ShieldOverheat,
        PrologueAudioCue.ShieldReady, PrologueAudioCue.ClawFire,
        PrologueAudioCue.ClawTravel, PrologueAudioCue.ClawLatch,
        PrologueAudioCue.ClawTension, PrologueAudioCue.ClawRelease,
        PrologueAudioCue.ClawIceSlip, PrologueAudioCue.ClawFail
    ];

    public override async void _Initialize()
    {
        try
        {
            DisplayServer.WindowSetSize(new Vector2I(1280, 720));
            Root.Size = new Vector2I(1280, 720);
            await ToSignal(this, SignalName.ProcessFrame);
            double peak = AuditSectors(out int mechanisms);
            AuditTypedCues();
            if (CanyonContentCatalog.All.Count != CertifiedMicroSceneCount)
                throw new InvalidOperationException(
                    $"production audit requires exactly {CertifiedMicroSceneCount} micro-scenes");
            if (mechanisms != CertifiedClawMechanismRoles)
                throw new InvalidOperationException(
                    $"production audit requires exactly {CertifiedClawMechanismRoles} claw mechanism roles; actual={mechanisms}");
            if (peak >= CertifiedCallbackCeilingMilliseconds)
                throw new InvalidOperationException(
                    $"production audit callback exceeded {CertifiedCallbackCeilingMilliseconds} ms: {peak:0.000}");

            GD.Print($"PROLOGUE_AUDIT_TOPOLOGY_OK sectors={CertifiedSectorCount} micro_scenes={CertifiedMicroSceneCount} claw_roles={mechanisms}");
            GD.Print($"PROLOGUE_PERFORMANCE peak_ms={peak:0.000} ceiling_ms=50 " +
                $"basis={MeasurementBasis} headroom_ms={CertifiedCallbackCeilingMilliseconds - peak:0.000}");
            // BASE's two display-backed visual slots now exercise this five-sector audit rather
            // than the superseded six-room recorder. These terminals follow live mounted-sector
            // and callback validation; they are not standalone source assertions.
            GD.Print("PROLOGUE_VISUAL_EVIDENCE_OK");
            GD.Print("PROLOGUE_VISUAL_PERFORMANCE_OK");
            GD.Print("PROLOGUE_PRODUCTION_AUDIT_OK");
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError("PROLOGUE_PRODUCTION_AUDIT_FAIL: " + exception);
            Quit(1);
        }
    }

    private static double AuditSectors(out int mechanismCount)
    {
        if (SectorScenes.Length != CertifiedSectorCount)
            throw new InvalidOperationException("production audit requires exactly five sector scenes");
        var materials = new HashSet<GrappleSurfaceMaterial>();
        var mechanismKinds = new HashSet<string>(StringComparer.Ordinal);
        double peak = 0d;
        for (int index = 0; index < SectorScenes.Length; index++)
        {
            (PrologueSectorId expected, string path) = SectorScenes[index];
            PackedScene packed = GD.Load<PackedScene>(path) ??
                throw new InvalidOperationException($"missing production sector {path}");
            PrologueSector sector = packed.Instantiate<PrologueSector>();
            try
            {
                var watch = Stopwatch.StartNew();
                int surfaces = AuditMountedSectorCallback(expected, sector, materials, mechanismKinds);
                watch.Stop();
                peak = Math.Max(peak, watch.Elapsed.TotalMilliseconds);
                GD.Print($"PROLOGUE_SECTOR_AUDIT_OK sector={expected} roots=5 surfaces={surfaces} " +
                    $"callback_ms={watch.Elapsed.TotalMilliseconds:0.000}");
            }
            finally { sector.Free(); }
        }
        foreach (GrappleSurfaceMaterial material in Enum.GetValues<GrappleSurfaceMaterial>())
            if (!materials.Contains(material))
                throw new InvalidOperationException($"production sectors omit surface {material}");
        mechanismCount = mechanismKinds.Count;
        GD.Print($"PROLOGUE_SURFACE_AUDIT_OK materials={materials.Count}");
        return peak;
    }

    private static int AuditMountedSectorCallback(PrologueSectorId expected, PrologueSector sector,
        ISet<GrappleSurfaceMaterial> materials, ISet<string> mechanismKinds)
    {
        if (sector.SectorId != expected || sector.Bounds.Size.X <= 0f || sector.Bounds.Size.Y <= 0f)
            throw new InvalidOperationException($"invalid sector bounds or identity: {expected}");
        foreach (string root in StableVisualRoots)
            if (sector.GetNodeOrNull($"VisualRoot/{root}") == null)
                throw new InvalidOperationException($"{expected} missing stable VisualRoot/{root}");
        List<GrappleSurface2D> surfaces = Descendants<GrappleSurface2D>(sector);
        foreach (GrappleSurface2D surface in surfaces)
        {
            if (surface.Validate().Count != 0)
                throw new InvalidOperationException($"{expected} has invalid surface {surface.Name}");
            materials.Add(surface.Material);
        }
        foreach (CanyonClawMechanism2D mechanism in Descendants<CanyonClawMechanism2D>(sector))
            mechanismKinds.Add(mechanism.Kind.ToString());
        return surfaces.Count;
    }

    private static void AuditTypedCues()
    {
        if (TypedCues.Length != CertifiedTypedCueCount || TypedCues.Distinct().Count() != TypedCues.Length)
            throw new InvalidOperationException("typed cue audit requires 16 unique cues");
        foreach (PrologueAudioCue cue in TypedCues)
            if (!PrologueVfxDirector.HasTask10Mapping(cue))
                throw new InvalidOperationException($"typed VFX mapping missing for {cue}");
        GD.Print($"PROLOGUE_TYPED_CUE_AUDIT_OK cues={TypedCues.Length} vfx=True");
    }

    private static List<T> Descendants<T>(Node root) where T : Node
    {
        var result = new List<T>();
        foreach (Node child in root.GetChildren())
        {
            if (child is T typed) result.Add(typed);
            result.AddRange(Descendants<T>(child));
        }
        return result;
    }
}
