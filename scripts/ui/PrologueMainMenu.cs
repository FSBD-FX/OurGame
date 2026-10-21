using Godot;
using Knight.Localization;
using Knight.Settings;
using Knight.Save;

namespace Knight.Ui;

public partial class PrologueMainMenu : Control
{
    public bool BasinQualitySampleMode { get; set; }
    public PrologueSaveLoadStatus? MigrationStatus { get; set; }

    public void Present(bool hasSave, GameLanguage language)
    {
        Visible = true;
        Button continueButton = GetNode<Button>("Panel/Buttons/Continue");
        continueButton.Disabled = !hasSave;
        GetNode<Label>("Panel/Title").Text = "THE NAMELESS BELFRY";
        GetNode<Label>("Panel/Subtitle").Text = BasinQualitySampleMode
            ? language == GameLanguage.ChineseSimplified
                ? "盆地品质样片 · 开发中"
                : "BASIN QUALITY SAMPLE · IN DEVELOPMENT"
            : language == GameLanguage.ChineseSimplified
                ? "无名钟楼 · 序章"
                : "A mechanical-fable prologue";
        SetButton("Start", "menu.start", language);
        SetButton("Continue", "menu.continue", language);
        SetButton("Settings", "menu.settings", language);
        SetButton("Controls", "menu.controls", language);
        SetButton("Quit", "menu.quit", language);
        GetNode<Label>("Panel/Footer").Text = language == GameLanguage.ChineseSimplified
            ? "钟声会记住每一次归来"
            : "THE BELLS REMEMBER EVERY RETURN";
        if (!BasinQualitySampleMode && MigrationStatus.HasValue)
        {
            bool chinese = language == GameLanguage.ChineseSimplified;
            GetNode<Button>("Panel/Buttons/Start").Text = chinese
                ? "开始峡谷（旧存档保留）" : "Start canyon (old save kept)";
            continueButton.Text = chinese ? "从映射存档点继续" : "Continue from mapped checkpoint";
            continueButton.Disabled = !hasSave || MigrationStatus != PrologueSaveLoadStatus.MigratableLegacy;
            GetNode<Label>("Panel/Footer").Text = MigrationStatus == PrologueSaveLoadStatus.Recovered
                ? chinese ? "开始前将原异常存档备份到同一文件夹。"
                    : "Unreadable slot: same-folder backup before starting."
                : chinese ? "旧存档保留，请选择进入峡谷的方式。"
                    : "Your old save is kept. Choose how to enter.";
        }
    }

    private void SetButton(string name, string key, GameLanguage language) =>
        GetNode<Button>($"Panel/Buttons/{name}").Text = PrologueStrings.Get(key, language);
}
