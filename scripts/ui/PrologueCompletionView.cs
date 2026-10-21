using Godot;
using Knight.Settings;

namespace Knight.Ui;

public partial class PrologueCompletionView : Control
{
    private GameLanguage _language = GameLanguage.ChineseSimplified;
    private string _time = "00:00:00";
    private int _deaths;
    private bool _hiddenFound;
    private bool _shortcutOpen;
    private bool _developmentSample;

    public void SetLanguage(GameLanguage language)
    {
        _language = System.Enum.IsDefined(language) ? language : GameLanguage.ChineseSimplified;
        ApplyText();
    }

    public void Present(string time, int deaths, bool hiddenFound, bool shortcutOpen)
    {
        _time = time ?? "00:00:00";
        _deaths = deaths;
        _hiddenFound = hiddenFound;
        _shortcutOpen = shortcutOpen;
        ApplyText();
        Visible = true;
        GetNodeOrNull<Button>("Panel/ReturnToTitle")?.GrabFocus();
    }

    public void PresentDevelopmentSample()
    {
        _developmentSample = true;
        ApplyText();
        Visible = true;
        GetNodeOrNull<Button>("Panel/ReturnToTitle")?.GrabFocus();
    }

    private void ApplyText()
    {
        bool english = _language == GameLanguage.English;
        Label title = GetNodeOrNull<Label>("Panel/Title");
        Label subtitle = GetNodeOrNull<Label>("Panel/Subtitle");
        Label stats = GetNodeOrNull<Label>("Panel/Stats");
        Button returnButton = GetNodeOrNull<Button>("Panel/ReturnToTitle");
        Button replayButton = GetNodeOrNull<Button>("Panel/Replay");
        if (_developmentSample)
        {
            if (title != null)
            {
                title.Text = english ? "DEVELOPMENT SAMPLE END" : "开发样片 · 本次体验到此结束";
                title.AddThemeFontSizeOverride("font_size", english ? 18 : 24);
            }
            if (subtitle != null) subtitle.Text = english
                ? "WORK IN PROGRESS — NOT THE COMPLETE GAME" : "制作中 · 这不是正式完整游戏";
            if (stats != null) stats.Text = english
                ? "Your checkpoint is saved.\nReturn to the title to continue, or start a fresh sample."
                : "检查点已保存。\n返回标题可继续探索，也可以重新开始样片。";
            if (returnButton != null) returnButton.Text = english ? "RETURN TO TITLE" : "返回标题";
            if (replayButton != null) replayButton.Text = english ? "REPLAY SAMPLE" : "重新体验样片";
            return;
        }
        if (title != null)
        {
            title.Text = english
                ? "THE NAMELESS BELFRY · PROLOGUE COMPLETE" : "无名钟楼 · 序章完成";
            title.AddThemeFontSizeOverride("font_size", english ? 18 : 26);
        }
        if (subtitle != null) subtitle.Text = english
            ? "THE LAST GRAVEKEEPER STILL HEARS THE BELLS" : "最后的守墓人仍在钟声深处";
        if (stats != null)
        {
            stats.Text = english
                ? $"TIME  {_time}\nDEATHS  {_deaths}\nHIDDEN ROUTE  {(_hiddenFound ? "FOUND" : "NOT FOUND")}\nBELL-CHAIN SHORTCUT  {(_shortcutOpen ? "OPEN" : "CLOSED")}"
                : $"用时  {_time}\n死亡  {_deaths}\n隐藏路线  {(_hiddenFound ? "已发现" : "未发现")}\n钟链捷径  {(_shortcutOpen ? "已开启" : "未开启")}";
        }
        if (returnButton != null) returnButton.Text = english ? "RETURN TO TITLE" : "返回标题";
        if (replayButton != null) replayButton.Text = english ? "PLAY AGAIN" : "再次游玩";
    }

    public void Clear() { _developmentSample = false; Visible = false; }
}
