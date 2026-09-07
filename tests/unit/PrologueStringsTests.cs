using System;
using System.Collections.Generic;
using System.IO;
using Knight.Localization;
using Knight.Settings;

namespace Knight.Tests.Unit;

public static class PrologueStringsTests
{
    public static void Run()
    {
        CurrentStringTableIsCompleteAndBilingual();
        MissingTranslationsAreRejected();
        MissingKeysAreRejectedAtLookup();
    }

    private static void CurrentStringTableIsCompleteAndBilingual()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "localization", "prologue-ui.json");
        TestAssert.True(File.Exists(path), "prologue UI string table exists");
        PrologueStrings.Load(File.ReadAllText(path));
        TestAssert.Equal("继续", PrologueStrings.Get("menu.continue", GameLanguage.ChineseSimplified),
            "Chinese menu string");
        TestAssert.Equal("Continue", PrologueStrings.Get("menu.continue", GameLanguage.English),
            "English menu string");
        TestAssert.Equal("第一关引导",
            PrologueStrings.Get("guidance.chrome.heading", GameLanguage.ChineseSimplified),
            "Chinese expanded-guidance chrome");
        TestAssert.Equal("First-Level Guidance",
            PrologueStrings.Get("guidance.chrome.heading", GameLanguage.English),
            "English expanded-guidance chrome");
        TestAssert.Equal(PrologueStrings.KeyCount(GameLanguage.ChineseSimplified),
            PrologueStrings.KeyCount(GameLanguage.English), "both languages expose the same key count");
        TestAssert.True(PrologueStrings.KeyCount(GameLanguage.English) >= 30,
            "formal UI table covers menus, HUD, settings and completion");
    }

    private static void MissingTranslationsAreRejected()
    {
        const string json = "{\"version\":1,\"languages\":{\"zh-CN\":{\"menu.start\":\"开始\"},\"en\":{}}}";
        try
        {
            PrologueStrings.Load(json);
        }
        catch (InvalidDataException exception)
        {
            TestAssert.True(exception.Message.Contains("menu.start", StringComparison.Ordinal),
                "missing translation names its key");
            return;
        }
        throw new InvalidOperationException("Assertion failed: missing translation is rejected");
    }

    private static void MissingKeysAreRejectedAtLookup()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "assets", "localization", "prologue-ui.json");
        PrologueStrings.Load(File.ReadAllText(path));
        try
        {
            PrologueStrings.Get("missing.key", GameLanguage.English);
        }
        catch (KeyNotFoundException) { return; }
        throw new InvalidOperationException("Assertion failed: missing localization key is rejected");
    }
}
