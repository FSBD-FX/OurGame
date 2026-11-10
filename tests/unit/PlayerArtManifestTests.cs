using System;
using System.IO;
using Knight.Presentation;

namespace Knight.Tests.Unit;

public static class PlayerArtManifestTests
{
    public static void Run()
    {
        ParsesAbsoluteFrameRectangles();
        RejectsMismatchedFrameCounts();
        RejectsOutOfBoundsRectangles();
        RejectsMissingAnimationMetadata();
        RejectsNonPositiveFps();
    }

    private static void ParsesAbsoluteFrameRectangles()
    {
        PlayerArtManifest manifest = PlayerArtManifestParser.Parse(ValidJson(), "res://assets/player/test.png");

        TestAssert.Equal("thread-spool-bearer", manifest.CharacterId, "character id is preserved");
        TestAssert.Equal("res://assets/player/test.png", manifest.TextureResourcePath,
            "texture resource path is preserved");
        TestAssert.Equal(2, manifest.Clips["idle"].Frames.Count, "idle uses manifest rectangles");
        TestAssert.Equal(new PlayerAtlasRect(256, 0, 256, 256), manifest.Clips["idle"].Frames[1],
            "second idle rectangle remains absolute");
        TestAssert.True(manifest.Clips["idle"].Loop, "idle loop flag is preserved");
        TestAssert.True(!manifest.Clips["jump"].Loop, "jump one-shot flag is preserved");
    }

    private static string ValidJson() => """
    {
      "characterId":"thread-spool-bearer",
      "game_input":"player-atlas.png",
      "animation":{"rows":{
        "idle":{"frames":2,"fps":6,"loop":true},
        "jump":{"frames":1,"fps":10,"loop":false}
      }},
      "frame_layout":{"sheetWidth":512,"sheetHeight":512,"rows":{
        "idle":[{"x":0,"y":0,"w":256,"h":256},{"x":256,"y":0,"w":256,"h":256}],
        "jump":[{"x":0,"y":256,"w":256,"h":256}]
      }}
    }
    """;

    private static void RejectsMismatchedFrameCounts() => ExpectInvalid(
        ValidJson().Replace("\"frames\":2", "\"frames\":3"), "idle");

    private static void RejectsOutOfBoundsRectangles() => ExpectInvalid(
        ValidJson().Replace("\"x\":256,\"y\":0", "\"x\":400,\"y\":0"), "idle");

    private static void RejectsMissingAnimationMetadata() => ExpectInvalid(
        ValidJson().Replace(
            "\"jump\":{\"frames\":1,\"fps\":10,\"loop\":false}",
            "\"unused\":{\"frames\":1,\"fps\":10,\"loop\":false}"),
        "jump");

    private static void RejectsNonPositiveFps() => ExpectInvalid(
        ValidJson().Replace("\"fps\":6", "\"fps\":0"), "idle");

    private static void ExpectInvalid(string json, string clip)
    {
        try
        {
            PlayerArtManifestParser.Parse(json, "res://assets/player/test.png");
            throw new InvalidOperationException("expected manifest rejection");
        }
        catch (InvalidDataException exception)
        {
            TestAssert.True(exception.Message.Contains(clip), "rejection names the invalid clip");
        }
    }
}
