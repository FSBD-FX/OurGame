using System;
using Godot;
using Knight.Prologue.Guidance;
using Knight.Prologue.Seamless;
using Knight.Ui;

namespace Knight.Prologue.Sample;

public partial class BasinQualitySampleEntry : Node
{
    public const string DefaultStorageDirectory = "user://knight/basin-quality-sample";
    public const string SectorScenePath = "res://scenes/prologue/sample/BasinQualitySampleSector.tscn";
    public static readonly Vector2 AuthoredStart = new(2380f, 2014f);

    [Export] public string StorageDirectory { get; set; } = DefaultStorageDirectory;

    public PrologueLevelController Level { get; private set; }

    public override void _Ready()
    {
        string storage = string.IsNullOrWhiteSpace(StorageDirectory)
            ? throw new InvalidOperationException("Basin sample storage directory is required.")
            : StorageDirectory.Trim().TrimEnd('/', '\\');
        Level = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn")
            .Instantiate<PrologueLevelController>();
        Level.Name = "Level";
        Level.FreezeWorldWhenGameplayInactive = true;

        Node2D playerRoot = Level.GetNode<Node2D>("PlayerRoot");
        CharacterBody2D player = playerRoot.GetNode<CharacterBody2D>("Player");
        player.Position = AuthoredStart;
        SeamlessPrologueWorld world =
            Level.GetNode<SeamlessPrologueWorld>("WorldRoot/Sectors");
        world.InitialSector = PrologueSectorId.CanyonBasin;
        // Pin the entire old geometry/camera contract before any shared runtime enters the tree.
        world.LayoutVersion = 1;
        world.Definitions = BasinSampleLegacyLayout.CreateDefinitions();

        PrologueGameFlowController flow =
            Level.GetNode<PrologueGameFlowController>("GameFlow");
        flow.BasinQualitySampleMode = true;
        flow.HeadlessAutoStart = false;
        flow.AutomationAutoStart = false;
        flow.SavePath = Join(storage, "save.json");
        flow.SettingsPath = Join(storage, "settings.json");

        Level.GetNode<PrologueCompletionController>("Completion").BasinQualitySampleMode = true;
        Level.GetNode<PrologueUiRoot>("ProductUi").BasinQualitySampleMode = true;
        PrologueGuidanceController sampleGuidance =
            Level.GetNode<PrologueGuidanceController>("Guidance");
        sampleGuidance.SetGuidanceEnabled(false);
        sampleGuidance.ProcessMode = ProcessModeEnum.Disabled;
        ProloguePlaytestRecorder recorder =
            Level.GetNodeOrNull<ProloguePlaytestRecorder>("PlaytestRecorder");
        if (recorder != null) recorder.OutputDirectory = Join(storage, "playtests");

        ProloguePauseMenu pause = Level.GetNode<ProloguePauseMenu>("PauseMenu");
        Control panel = Level.GetNode<Control>("PauseMenu/Panel");
        var returnToTitle = new Button
        {
            Name = "ReturnToTitle",
            Position = new Vector2(244f, 168f),
            Size = new Vector2(210f, 44f),
            Text = "返回标题"
        };
        panel.AddChild(returnToTitle);
        returnToTitle.Pressed += () =>
        {
            pause.Execute(ProloguePauseCommand.Resume);
            flow.ReturnToTitle();
        };
        Button guidance = Level.GetNode<Button>("PauseMenu/Panel/GuidanceToggle");
        guidance.Visible = false;
        guidance.Disabled = true;
        guidance.FocusMode = Control.FocusModeEnum.None;

        Node2D worldRoot = Level.GetNode<Node2D>("WorldRoot");
        worldRoot.ProcessMode = ProcessModeEnum.Disabled;
        worldRoot.Visible = false;
        playerRoot.ProcessMode = ProcessModeEnum.Disabled;
        playerRoot.Visible = false;
        Level.GetNode<CanvasLayer>("Hud").Visible = false;

        AddChild(Level);
        Level.AddChild(new BasinSampleGuideController { Name = "SampleGuide" });
    }

    private static string Join(string directory, string file) => $"{directory}/{file}";
}
