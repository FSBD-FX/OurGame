using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Knight.Audio;

public sealed class PrologueAudioManifest
{
    public int Version { get; set; }
    public int SampleRate { get; set; }
    public string Source { get; set; } = string.Empty;
    public string License { get; set; } = string.Empty;
    public int Seed { get; set; }
    public string Generator { get; set; } = string.Empty;
    public List<string> Buses { get; set; } = new();
    public Dictionary<string, List<string>> Music { get; set; } = new();
    public Dictionary<string, string> Ambience { get; set; } = new();
    public Dictionary<string, List<string>> Cues { get; set; } = new();

    public static PrologueAudioManifest Load(string path)
    {
        if (!File.Exists(path)) throw new InvalidDataException($"Audio manifest is missing: {path}");
        PrologueAudioManifest manifest = JsonSerializer.Deserialize<PrologueAudioManifest>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
            throw new InvalidDataException("Audio manifest is empty.");
        manifest.Validate();
        return manifest;
    }

    public IEnumerable<string> AllPaths() => Music.Values.SelectMany(value => value)
        .Concat(Ambience.Values).Concat(Cues.Values.SelectMany(value => value)).Distinct(StringComparer.Ordinal);

    public void Validate()
    {
        if (Version != 1) throw new InvalidDataException($"Unsupported audio manifest version {Version}.");
        if (SampleRate != 48000) throw new InvalidDataException("Audio masters must use 48000 Hz.");
        if (!string.Equals(License, "Project-owned", StringComparison.Ordinal))
            throw new InvalidDataException("Audio manifest license must be Project-owned.");
        if (string.IsNullOrWhiteSpace(Source) || string.IsNullOrWhiteSpace(Generator))
            throw new InvalidDataException("Audio manifest must declare source and generator provenance.");
        foreach (string path in AllPaths())
            if (!path.StartsWith("res://assets/audio/prologue/", StringComparison.Ordinal))
                throw new InvalidDataException($"Audio path must remain in the project-owned prologue tree: {path}");
    }
}
