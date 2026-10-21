using System;
using System.IO;
using Godot;
using Knight.Audio;
using Knight.Localization;
using Knight.Settings;

namespace Knight.Tests;

// Real resource-container boundary regression. The virtual path has no loose file.
public partial class RunPackedAudioManifestTests : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            foreach (string invalidPath in new string[] { null, "", "  " })
            {
                bool rejected = false;
                try { PrologueAudioManifest.Load(invalidPath); }
                catch (InvalidDataException) { rejected = true; }
                TestAssert.True(rejected, "missing path keeps the manifest-specific error contract");
            }
            string id = Guid.NewGuid().ToString("N");
            string packedPath = Path.Combine(Path.GetTempPath(), "knight-audio-manifest-" + id + ".pck");
            string virtualPath = "res://packed-audio-test-" + id + "/manifest.json";
            string virtualStrings = "res://packed-audio-test-" + id + "/strings.json";
            using var packer = new PckPacker();
            TestAssert.Equal(Error.Ok, packer.PckStart(packedPath), "create isolated resource pack");
            TestAssert.Equal(Error.Ok, packer.AddFile(virtualPath,
                "res://assets/audio/prologue/audio-manifest.json"), "pack the real audio manifest");
            TestAssert.Equal(Error.Ok, packer.AddFile(virtualStrings,
                "res://assets/localization/prologue-ui.json"), "pack the real localization table");
            TestAssert.Equal(Error.Ok, packer.Flush(), "finish isolated resource pack");
            TestAssert.True(ProjectSettings.LoadResourcePack(packedPath), "mount actual PCK");
            TestAssert.True(!File.Exists(ProjectSettings.GlobalizePath(virtualPath)),
                "fixture must not have a loose filesystem copy");
            TestAssert.True(Godot.FileAccess.FileExists(virtualPath), "manifest exists only in PCK");
            var manifest = PrologueAudioManifest.Load(virtualPath);
            TestAssert.Equal(1, manifest.Version, "load and validate packed manifest");
            var loose = PrologueAudioManifest.Load(ProjectSettings.GlobalizePath(
                "res://assets/audio/prologue/audio-manifest.json"));
            TestAssert.Equal(loose.Cues.Count, manifest.Cues.Count, "loose-path tooling remains supported");
            TestAssert.True(!File.Exists(ProjectSettings.GlobalizePath(virtualStrings)),
                "localization fixture also has no loose copy");
            PrologueStrings.Load(Godot.FileAccess.GetFileAsString(virtualStrings));
            TestAssert.Equal("开始新游戏", PrologueStrings.Get("menu.start", GameLanguage.ChineseSimplified),
                "Chinese menu strings load from packed JSON");
            TestAssert.Equal("New Game", PrologueStrings.Get("menu.start", GameLanguage.English),
                "English menu strings load from packed JSON");
            GD.Print("PACKED_AUDIO_MANIFEST_TESTS_OK pack=" + packedPath);
            Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Quit(1);
        }
    }
}
