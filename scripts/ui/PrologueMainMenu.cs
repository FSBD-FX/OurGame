using Godot;
using Knight.Localization;
using Knight.Settings;

namespace Knight.Ui;

public partial class PrologueMainMenu : Control
{
    public void Present(bool hasSave, GameLanguage language)
    {
        Visible = true;
        Button continueButton = GetNode<Button>("Panel/Buttons/Continue");
        continueButton.Disabled = !hasSave;
        GetNode<Label>("Panel/Title").Text = "THE NAMELESS BELFRY";
        GetNode<Label>("Panel/Subtitle").Text = language == GameLanguage.ChineseSimplified
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
    }

    private void SetButton(string name, string key, GameLanguage language) =>
        GetNode<Button>($"Panel/Buttons/{name}").Text = PrologueStrings.Get(key, language);
}
