using Godot;
using Knight.Settings;

namespace Knight.Ui;

public partial class PrologueRecoveryView : Control
{
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    private string _reason = string.Empty;

    public void SetLanguage(GameLanguage language)
    {
        _language = System.Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        if (Visible) ApplyText();
    }

    public void Present(string reason)
    {
        _reason = reason ?? string.Empty;
        ApplyText();
        Visible = true;
    }

    public void Clear() => Visible = false;

    private void ApplyText()
    {
        bool english = _language == GameLanguage.English;
        Label title = GetNodeOrNull<Label>("Title");
        Label reason = GetNodeOrNull<Label>("Reason");
        if (title != null) title.Text = english ? "GRAVE LAMP RELIT" : "墓灯重燃";
        if (reason != null) reason.Text = LocalizeReason(_reason, english);
    }

    private static string LocalizeReason(string reason, bool english)
    {
        string normalized = string.IsNullOrWhiteSpace(reason) ? "CHECKPOINT RESTORED" : reason.Trim();
        return normalized.ToUpperInvariant() switch
        {
            "DEATH RESET" => english ? "FALLEN · CHECKPOINT RESTORED" : "倒下 · 墓灯重燃",
            "FALL RESET" => english ? "FALLEN · CHECKPOINT RESTORED" : "坠落 · 墓灯重燃",
            "RETURNED TO CHECKPOINT" => english ? "RETURNED TO CHECKPOINT" : "已返回墓灯",
            "CHECKPOINT RESTORED" => english ? "CHECKPOINT RESTORED" : "墓灯重燃",
            _ => normalized
        };
    }
}
