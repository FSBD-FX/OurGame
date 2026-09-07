using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Knight.Audio;

namespace Knight.Tests.Unit;

public static class PrologueAudioManifestTests
{
    private static readonly string[] Task10TypedCues =
    [
        "ModeAttack", "ModeGrapple", "EmptySlot", "EnergyEmpty",
        "ShieldRaise", "ShieldBlock", "ShieldHeavyBreak", "ShieldOverheat", "ShieldReady",
        "ClawFire", "ClawTravel", "ClawLatch", "ClawTension", "ClawRelease", "ClawIceSlip",
        "ClawFail"
    ];
    private static readonly string[] RequiredBuses = ["Master", "Music", "SFX", "Ambience", "UI"];
    private static readonly string[] RequiredStagedCues =
    [
        "RustShellTelegraph", "MothTelegraph", "LaborerTelegraph",
        "BossSweepTelegraph", "BossLeapTelegraph", "BossGroundWaveTelegraph", "BossHammerTelegraph",
        "RustShellRecovery", "MothRecovery", "LaborerRecovery", "BossRecovery",
        "ThreadPreview", "ThreadWindup", "ThreadTravel", "ThreadLatch", "ThreadTension"
    ];

    public static void Run()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "assets", "audio", "prologue", "audio-manifest.json");
        TestAssert.True(File.Exists(path), "prologue audio manifest exists");
        PrologueAudioManifest manifest = PrologueAudioManifest.Load(path);
        TestAssert.Equal(1, manifest.Version, "audio manifest has the supported version");
        TestAssert.Equal(48000, manifest.SampleRate, "all original audio masters use 48 kHz");
        TestAssert.Equal("Knight local procedural synthesis", manifest.Source,
            "audio manifest declares the project-owned synthesis source");
        TestAssert.Equal("Project-owned", manifest.License,
            "audio manifest declares its project-owned license");
        foreach (string bus in RequiredBuses)
            TestAssert.True(manifest.Buses.Contains(bus), $"audio manifest includes {bus} bus");

        foreach (string cueName in RequiredStagedCues)
        {
            TestAssert.True(Enum.TryParse(cueName, out PrologueAudioCue cue),
                $"audio cue enum exposes staged cue {cueName}");
            TestAssert.True(manifest.Cues.TryGetValue(cue.ToString(), out List<string> stagedVariants) &&
                            stagedVariants.Count > 0,
                $"audio manifest covers staged cue {cueName}");
        }

        foreach (PrologueAudioCue cue in Enum.GetValues<PrologueAudioCue>())
            TestAssert.True(manifest.Cues.TryGetValue(cue.ToString(), out List<string> variants) && variants.Count > 0,
                $"audio manifest covers cue {cue}");

        foreach (PrologueMusicState state in new[] { PrologueMusicState.Exploration, PrologueMusicState.BossPhaseOne,
                     PrologueMusicState.BossPhaseTwo })
            TestAssert.True(manifest.Music.TryGetValue(state.ToString(), out List<string> layers) && layers.Count > 0,
                $"audio manifest covers music state {state}");

        TestAssert.True(manifest.Ambience.Count >= 6, "all six prologue rooms have authored ambience");
        foreach (string relative in manifest.AllPaths())
        {
            string absolute = Path.Combine(root, relative.Replace("res://", string.Empty).Replace('/', Path.DirectorySeparatorChar));
            TestAssert.True(File.Exists(absolute), $"declared audio file exists: {relative}");
            using var reader = new BinaryReader(File.OpenRead(absolute));
            TestAssert.Equal("RIFF", new string(reader.ReadChars(4)), $"audio file is RIFF WAV: {relative}");
            reader.BaseStream.Position = 8;
            TestAssert.Equal("WAVE", new string(reader.ReadChars(4)), $"audio file is WAVE: {relative}");
            reader.BaseStream.Position = 24;
            TestAssert.Equal(48000, reader.ReadInt32(), $"audio file sample rate is 48 kHz: {relative}");
            TestAssert.True(reader.BaseStream.Length > 2048, $"audio file is nonempty: {relative}");
        }

        ValidatePerFileMetadata(root, path, manifest);
        ValidateUiFrequencyIsolation(root, path, manifest);
        ValidateWarningMix();
        ValidateGroundWaveOnset();
    }

    public static void Task10TypedCueNamesExist()
    {
        string root = Directory.GetCurrentDirectory();
        string manifestPath = Path.Combine(root, "assets", "audio", "prologue", "audio-manifest.json");
        PrologueAudioManifest manifest = PrologueAudioManifest.Load(manifestPath);
        var declared = new HashSet<string>(StringComparer.Ordinal);
        foreach (string cueName in Task10TypedCues)
        {
            TestAssert.True(Enum.TryParse(cueName, out PrologueAudioCue _),
                $"Task10 audio cue enum exposes exact typed cue {cueName}");
            TestAssert.True(manifest.Cues.TryGetValue(cueName, out List<string> paths) && paths.Count == 1,
                $"Task10 manifest declares one deterministic WAV for exact cue {cueName}");
            TestAssert.Equal("SFX", PrologueAudioDirector.BusForCue(Enum.Parse<PrologueAudioCue>(cueName)),
                $"Task10 exact cue {cueName} routes to the SFX bus");
            declared.Add(paths[0].Replace("res://", string.Empty));
        }
        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (string family in new[] { "mode", "shield", "claw" })
        {
            string directory = Path.Combine(root, "assets", "audio", "prologue", "sfx", family);
            foreach (string file in Directory.EnumerateFiles(directory, "*.wav", SearchOption.TopDirectoryOnly))
                actual.Add(Path.GetRelativePath(root, file).Replace('\\', '/'));
        }
        TestAssert.True(declared.SetEquals(actual) && actual.Count == 16,
            "Task10 fixed-seed mode/shield/claw directories contain the exact sixteen declared WAVs");
        Run();
    }

    public static void ValidateWarningMix()
    {
        (string root, PrologueAudioManifest manifest) = LoadCurrentManifest();
        string[] dangerCues =
        [
            "RustShellTelegraph", "MothTelegraph", "LaborerTelegraph",
            "BossSweepTelegraph", "BossLeapTelegraph", "BossGroundWaveTelegraph", "BossHammerTelegraph"
        ];
        double minimumRms = double.MaxValue;
        double maximumRms = double.MinValue;
        double minimumFirst100Rms = double.MaxValue;
        double maximumFirst100Rms = double.MinValue;
        foreach (string cue in dangerCues)
        {
            foreach (string relative in manifest.Cues[cue])
            {
                short[] samples = ReadSamples(root, relative, out int sampleRate);
                double rms = RmsDbfs(samples, samples.Length);
                double first100 = RmsDbfs(samples, Math.Min(samples.Length, sampleRate / 10));
                minimumRms = Math.Min(minimumRms, rms);
                maximumRms = Math.Max(maximumRms, rms);
                minimumFirst100Rms = Math.Min(minimumFirst100Rms, first100);
                maximumFirst100Rms = Math.Max(maximumFirst100Rms, first100);
            }
        }
        TestAssert.True(maximumRms - minimumRms <= 5.0,
            $"danger-warning full-cue RMS spread stays within 5 dB; min={minimumRms:F2} max={maximumRms:F2}");
        TestAssert.True(maximumFirst100Rms - minimumFirst100Rms <= 5.0,
            $"danger-warning first-100-ms RMS spread stays within 5 dB; " +
            $"min={minimumFirst100Rms:F2} max={maximumFirst100Rms:F2}");
    }

    public static void ValidateGroundWaveOnset()
    {
        (string root, PrologueAudioManifest manifest) = LoadCurrentManifest();
        string relative = manifest.Cues["BossGroundWaveTelegraph"][0];
        short[] samples = ReadSamples(root, relative, out int sampleRate);
        int window = sampleRate / 100;
        double onsetMilliseconds = double.PositiveInfinity;
        for (int offset = 0; offset + window <= samples.Length; offset += window)
        {
            var slice = new short[window];
            Array.Copy(samples, offset, slice, 0, window);
            if (RmsDbfs(slice, slice.Length) < -12.0) continue;
            onsetMilliseconds = offset * 1000.0 / sampleRate;
            break;
        }
        TestAssert.True(onsetMilliseconds <= 100.0,
            $"Boss Ground Wave recognizable -12 dBFS onset lands within first 100 ms; " +
            $"actual={onsetMilliseconds:F1}ms");
    }

    private static void ValidatePerFileMetadata(string root, string manifestPath, PrologueAudioManifest manifest)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        TestAssert.True(document.RootElement.TryGetProperty("fileMetadata", out JsonElement metadata) &&
                        metadata.ValueKind == JsonValueKind.Object,
            "audio manifest declares per-file deterministic metadata");

        foreach (string relative in manifest.AllPaths())
        {
            TestAssert.True(metadata.TryGetProperty(relative, out JsonElement entry),
                $"audio manifest metadata covers exact file {relative}");
            TestAssert.Equal(relative, entry.GetProperty("path").GetString(),
                $"audio metadata preserves exact resource path {relative}");
            TestAssert.Equal(48000, entry.GetProperty("sampleRate").GetInt32(),
                $"audio metadata records 48 kHz for {relative}");
            TestAssert.True(entry.GetProperty("durationSeconds").GetDouble() > 0.0,
                $"audio metadata records a finite positive duration for {relative}");
            TestAssert.True(!string.IsNullOrWhiteSpace(entry.GetProperty("family").GetString()),
                $"audio metadata names a sound family for {relative}");
            double peakDbfs = entry.GetProperty("peakDbfs").GetDouble();
            TestAssert.True(double.IsFinite(peakDbfs) && peakDbfs <= -.9,
                $"audio metadata preserves limiter headroom for {relative}");

            string absolute = Path.Combine(root,
                relative.Replace("res://", string.Empty).Replace('/', Path.DirectorySeparatorChar));
            string actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(absolute))).ToLowerInvariant();
            TestAssert.Equal(actualHash, entry.GetProperty("sha256").GetString(),
                $"audio metadata hash matches deterministic WAV bytes for {relative}");
        }
    }

    private static void ValidateUiFrequencyIsolation(string root, string manifestPath,
        PrologueAudioManifest manifest)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement metadata = document.RootElement.GetProperty("fileMetadata");
        string[] uiCues = ["UiMove", "UiConfirm", "UiBack"];
        string[] dangerCues =
        [
            "RustShellTelegraph", "MothTelegraph", "LaborerTelegraph",
            "BossSweepTelegraph", "BossLeapTelegraph", "BossGroundWaveTelegraph", "BossHammerTelegraph"
        ];

        double minimumUiFrequency = double.MaxValue;
        double maximumDangerFrequency = 0.0;
        foreach (string cueName in uiCues)
            foreach (string relative in manifest.Cues[cueName])
                minimumUiFrequency = Math.Min(minimumUiFrequency,
                    AssertDominantFrequency(root, metadata, relative, "ui"));
        foreach (string cueName in dangerCues)
            foreach (string relative in manifest.Cues[cueName])
                maximumDangerFrequency = Math.Max(maximumDangerFrequency,
                    AssertDominantFrequency(root, metadata, relative, "danger-warning"));

        TestAssert.True(minimumUiFrequency >= maximumDangerFrequency + 100.0,
            $"UI cues stay outside the danger-warning dominant frequency family; " +
            $"uiMin={minimumUiFrequency:F0}Hz dangerMax={maximumDangerFrequency:F0}Hz");
    }

    private static double AssertDominantFrequency(string root, JsonElement metadata, string relative,
        string expectedFamily)
    {
        JsonElement entry = metadata.GetProperty(relative);
        TestAssert.Equal(expectedFamily, entry.GetProperty("family").GetString(),
            $"audio metadata classifies {relative} as {expectedFamily}");
        TestAssert.True(entry.TryGetProperty("dominantFrequencyHz", out JsonElement declared),
            $"audio metadata records analyzed dominant frequency for {relative}");
        string absolute = Path.Combine(root,
            relative.Replace("res://", string.Empty).Replace('/', Path.DirectorySeparatorChar));
        double actual = EstimateDominantFrequency(absolute);
        TestAssert.True(Math.Abs(declared.GetDouble() - actual) <= 10.1,
            $"audio metadata dominant frequency matches WAV analysis for {relative}");
        return actual;
    }

    private static double EstimateDominantFrequency(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path));
        reader.BaseStream.Position = 24;
        int sampleRate = reader.ReadInt32();
        reader.BaseStream.Position = 44;
        int count = (int)Math.Min((reader.BaseStream.Length - 44) / 2, sampleRate * .6);
        var samples = new double[count];
        for (int index = 0; index < count; index++) samples[index] = reader.ReadInt16() / 32768.0;

        double winningFrequency = 0.0;
        double winningEnergy = double.MinValue;
        for (int frequency = 40; frequency <= 1400; frequency += 10)
        {
            double coefficient = 2.0 * Math.Cos(2.0 * Math.PI * frequency / sampleRate);
            double previous = 0.0;
            double previousPrevious = 0.0;
            foreach (double sample in samples)
            {
                double current = sample + coefficient * previous - previousPrevious;
                previousPrevious = previous;
                previous = current;
            }
            double energy = previousPrevious * previousPrevious + previous * previous -
                            coefficient * previous * previousPrevious;
            if (energy <= winningEnergy) continue;
            winningEnergy = energy;
            winningFrequency = frequency;
        }
        return winningFrequency;
    }

    private static (string Root, PrologueAudioManifest Manifest) LoadCurrentManifest()
    {
        string root = Directory.GetCurrentDirectory();
        string path = Path.Combine(root, "assets", "audio", "prologue", "audio-manifest.json");
        return (root, PrologueAudioManifest.Load(path));
    }

    private static short[] ReadSamples(string root, string relative, out int sampleRate)
    {
        string absolute = Path.Combine(root,
            relative.Replace("res://", string.Empty).Replace('/', Path.DirectorySeparatorChar));
        using var reader = new BinaryReader(File.OpenRead(absolute));
        reader.BaseStream.Position = 24;
        sampleRate = reader.ReadInt32();
        reader.BaseStream.Position = 44;
        int count = (int)((reader.BaseStream.Length - 44) / 2);
        var samples = new short[count];
        for (int index = 0; index < count; index++) samples[index] = reader.ReadInt16();
        return samples;
    }

    private static double RmsDbfs(short[] samples, int count)
    {
        double energy = 0.0;
        for (int index = 0; index < count; index++) energy += samples[index] * (double)samples[index];
        double rms = Math.Sqrt(energy / Math.Max(1, count)) / 32768.0;
        return 20.0 * Math.Log10(Math.Max(1e-12, rms));
    }
}
