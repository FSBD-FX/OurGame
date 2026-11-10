using System;
using System.Collections.Generic;
using Godot;
using Knight.Player;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Tests.Presentation;

public partial class SeamlessWorldProofCapture : SceneTree
{
    private const int FinalFrame = 720;

    private static readonly Color[] SectorColors =
    [
        new("263a4a"),
        new("31505a"),
        new("5b4a3d"),
        new("485b45"),
        new("4e3d57")
    ];

    private PrologueLevelController _level;
    private SeamlessPrologueWorld _world;
    private CharacterBody2D _player;
    private SeamlessProofDriver _driver;
    private Label _state;
    private Label _mounted;
    private PrologueSectorId _lastSector;
    private int _frame;
    private ProofPhase _phase = ProofPhase.ToTempleForRestore;
    private int _completionFrame = -1;
    private int _maxMounted;
    private bool _adjacentSmoothObserved;
    private Vector2 _authoredPlayerPosition;
    private Rect2 _entranceBounds;
    private Rect2 _basinBounds;
    private Rect2 _templeBounds;
    private readonly Dictionary<PrologueSectorId, Rect2> _worldBounds = new();

    public override void _Initialize()
    {
        try
        {
            LoadGeneratedLevel();
        }
        catch (Exception exception)
        {
            GD.PushError("TASK7_PROOF_INIT_FAIL: " + exception);
            Quit(1);
        }
    }

    public override bool _Process(double delta)
    {
        if (_world == null || !GodotObject.IsInstanceValid(_world)) return false;
        _frame++;
        try
        {
            if (_frame == 1) StartAfterReady();
            UpdateProof();
            if (_completionFrame >= 0 && _frame >= _completionFrame + 90)
            {
                GD.Print($"TASK7_PROOF_COMPLETE frames={_frame} max_mounted={_maxMounted} " +
                    $"player_parent={_player.GetParent().Name} retired={_world.RetiredSectorCount} " +
                    "adjacent_smooth=1 direct_basin=1 direct_temple=1 " +
                    "far_restore=1 far_new_run=1");
                Quit(0);
            }
            else if (_frame >= FinalFrame)
            {
                throw new InvalidOperationException(
                    "The automatic physics route did not complete both far relocation proofs; " +
                    $"position={_player.GlobalPosition} velocity={_player.Velocity} " +
                    $"sector={_world.CurrentSector} mounted={Names(_world.MountedSectorIds)}.");
            }
        }
        catch (Exception exception)
        {
            GD.PushError($"TASK7_PROOF_FAIL frame={_frame}: {exception}");
            Quit(1);
        }
        return false;
    }

    private void LoadGeneratedLevel()
    {
        PackedScene scene = GD.Load<PackedScene>("res://scenes/prologue/PrologueLevel.tscn") ??
            throw new InvalidOperationException("Generated PrologueLevel.tscn is missing.");
        _level = scene.Instantiate<PrologueLevelController>();
        _world = _level.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors") ??
            throw new InvalidOperationException(
                "Generated prologue must contain WorldRoot/Sectors.");
        if (_level.SeamlessWorldPath.ToString() != "WorldRoot/Sectors" ||
            _level.GetNodeOrNull("RoomRoot") != null ||
            _level.RoomDefinitions.Count != 0 || _level.RequiredRooms.Count != 0)
            throw new InvalidOperationException(
                "Generated prologue still exposes legacy main-path room switching.");

        var definitions = new List<PrologueSectorDefinition>();
        foreach (PrologueSectorDefinition definition in _world.Definitions)
            definitions.Add(definition);
        _ = new PrologueSectorStreamModel(definitions);
        if (definitions.Count != SectorColors.Length)
            throw new InvalidOperationException("Generated prologue must contain five definitions.");
        for (int index = 0; index < definitions.Count; index++)
        {
            PrologueSectorDefinition definition = definitions[index];
            PrologueSector placeholder = definition.Scene?.Instantiate<PrologueSector>() ??
                throw new InvalidOperationException(
                    $"Generated placeholder {definition.Id} is missing its sector root.");
            bool valid = placeholder.ValidateContract(definition).Count == 0;
            placeholder.Free();
            if (!valid)
                throw new InvalidOperationException(
                    $"Generated placeholder {definition.Id} violates its definition.");
            _worldBounds.Add(definition.Id, definition.WorldBounds);
            definition.Scene = PackSector(definition.Id, index, definition.Bounds);
        }
        _entranceBounds = _worldBounds[PrologueSectorId.CanyonEntrance];
        _basinBounds = _worldBounds[PrologueSectorId.CanyonBasin];
        _templeBounds = _worldBounds[PrologueSectorId.LostTemple];
        GD.Print("TASK7_PROOF_GENERATED_CONTRACT_OK path=WorldRoot/Sectors " +
            "definitions=5 legacy_main_path=False");

        _player = _level.GetNode<CharacterBody2D>("PlayerRoot/Player");
        _authoredPlayerPosition = _player.GlobalPosition;
        _driver = new SeamlessProofDriver
        {
            Name = "AutomaticPhysicsDriver",
            Speed = 1500f,
            DriveAxis = 0f,
            ProcessPhysicsPriority = 50
        };
        _player.AddChild(_driver);
        _world.CameraLimitBlendRate = 4f;
        _world.ProcessPhysicsPriority = 100;

        Node gameFlow = _level.GetNodeOrNull("GameFlow");
        if (gameFlow != null)
        {
            _level.RemoveChild(gameFlow);
            gameFlow.Free();
        }
        CanvasLayer overlay = BuildOverlay();
        _state = overlay.GetNode<Label>("State");
        _mounted = overlay.GetNode<Label>("Mounted");
        _level.AddChild(overlay);
        Root.AddChild(_level);

    }

    private void StartAfterReady()
    {
        // Packed player callbacks enable themselves in _Ready. Disable unrelated owners only
        // after that lifecycle point, then begin one automatic physics route from authored spawn.
        _player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
        _player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        _player.GetNode<PlayerCombat>("Combat").SetPhysicsProcess(false);
        _player.GetNode<PlayerShieldController>("Shield").SetPhysicsProcess(false);
        Node productUi = _level.GetNodeOrNull("ProductUi");
        if (productUi != null)
            foreach (Node child in productUi.GetChildren())
                if (child is CanvasItem canvasItem) canvasItem.Visible = false;
        CanvasLayer authoredHud = _level.GetNodeOrNull<CanvasLayer>("Hud");
        if (authoredHud != null) authoredHud.Visible = false;
        _player.GlobalPosition = _authoredPlayerPosition;
        _player.Velocity = Vector2.Zero;
        _driver.DriveAxis = 1f;
        _lastSector = _world.CurrentSector;
        _maxMounted = _world.MountedSectorIds.Count;
        GD.Print($"TASK7_PROOF_SECTOR frame={_frame} sector={_lastSector} " +
            $"mounted={Names(_world.MountedSectorIds)}");
    }

    private void UpdateProof()
    {
        _maxMounted = Math.Max(_maxMounted, _world.MountedSectorIds.Count);
        if (_frame == 1 || _frame % 120 == 0)
            GD.Print($"TASK7_PROOF_SAMPLE frame={_frame} position={_player.GlobalPosition} " +
                $"velocity={_player.Velocity} driver={_driver.IsPhysicsProcessing()} " +
                $"motor={_player.GetNode<PlayerMotor>("Motor").IsPhysicsProcessing()} " +
                $"sector={_world.CurrentSector} mounted={Names(_world.MountedSectorIds)}");
        if (_world.MountedSectorIds.Count > 3)
            throw new InvalidOperationException("Mounted sector count exceeded three.");
        if (_player.GetParent()?.Name != "PlayerRoot")
            throw new InvalidOperationException("Persistent player left PlayerRoot.");
        AssertViewportAndPlayerCovered();

        if (_world.CurrentSector != _lastSector)
        {
            PrologueSectorId previous = _lastSector;
            _lastSector = _world.CurrentSector;
            GD.Print($"TASK7_PROOF_SECTOR frame={_frame} sector={_lastSector} " +
                $"mounted={Names(_world.MountedSectorIds)}");
            if (_phase == ProofPhase.ToTempleForRestore &&
                Math.Abs((int)_lastSector - (int)previous) == 1 && !_adjacentSmoothObserved)
            {
                Rect2 target = _worldBounds[_lastSector];
                Rect2 camera = CameraLimits();
                if (camera.Position.DistanceTo(target.Position) <= 1.1f &&
                    camera.End.DistanceTo(target.End) <= 1.1f)
                    throw new InvalidOperationException(
                        $"Adjacent physical crossing {previous}->{_lastSector} snapped camera limits.");
                _adjacentSmoothObserved = true;
                GD.Print($"TASK7_PROOF_ADJACENT_SMOOTH_OK frame={_frame} " +
                    $"route={previous}->{_lastSector} camera={camera} target={target}");
            }
        }

        if (_phase == ProofPhase.ToTempleForRestore &&
            _world.CurrentSector == PrologueSectorId.LostTemple &&
            _player.GlobalPosition.X >= _templeBounds.End.X - 36f)
        {
            _driver.DriveAxis = 0f;
            if (!_world.RestoreCheckpoint())
                throw new InvalidOperationException("Far initial-checkpoint restore was rejected.");
            AssertAuthorizedEntranceRelocation("restore");
            GD.Print($"TASK7_PROOF_FAR_RESTORE_OK frame={_frame} " +
                $"player={_player.GlobalPosition} camera={CameraLimits()} " +
                $"mounted={Names(_world.MountedSectorIds)}");
            if (!_adjacentSmoothObserved)
                throw new InvalidOperationException(
                    "Physical route reached LostTemple without observing a smooth adjacent crossing.");
            _phase = ProofPhase.DirectToBasin;
            _driver.PendingTeleport = new Vector2(
                _basinBounds.GetCenter().X, _authoredPlayerPosition.Y);
        }
        else if (_phase == ProofPhase.DirectToBasin &&
            _world.CurrentSector == PrologueSectorId.CanyonBasin)
        {
            AssertAuthorizedDirectRelocation(PrologueSectorId.CanyonBasin,
                _basinBounds, "entrance-to-basin");
            GD.Print($"TASK7_PROOF_DIRECT_BASIN_OK frame={_frame} " +
                $"player={_player.GlobalPosition} camera={CameraLimits()} " +
                $"mounted={Names(_world.MountedSectorIds)}");
            _phase = ProofPhase.DirectToTemple;
            _driver.PendingTeleport = new Vector2(
                _templeBounds.GetCenter().X, _authoredPlayerPosition.Y);
        }
        else if (_phase == ProofPhase.DirectToTemple &&
            _world.CurrentSector == PrologueSectorId.LostTemple)
        {
            AssertAuthorizedDirectRelocation(PrologueSectorId.LostTemple,
                _templeBounds, "basin-to-temple");
            GD.Print($"TASK7_PROOF_DIRECT_TEMPLE_OK frame={_frame} " +
                $"player={_player.GlobalPosition} camera={CameraLimits()} " +
                $"mounted={Names(_world.MountedSectorIds)}");
            _level.StartNewGame();
            DisableGameplayOwners();
            AssertAuthorizedEntranceRelocation("new run");
            GD.Print($"TASK7_PROOF_FAR_NEW_RUN_OK frame={_frame} " +
                $"player={_player.GlobalPosition} camera={CameraLimits()} " +
                $"mounted={Names(_world.MountedSectorIds)}");
            _phase = ProofPhase.Complete;
            _completionFrame = _frame;
        }

        _state.Text = $"CURRENT  {_world.CurrentSector}\n" +
            $"PLAYER   {_player.GlobalPosition.X,7:0.0} px\n" +
            $"PARENT   {_player.GetParent().Name}";
        _mounted.Text = $"MOUNTED  {Names(_world.MountedSectorIds)}\n" +
            $"RETIRED  {_world.RetiredSectorCount}   MAX  {_maxMounted}/3\n" +
            (_phase switch
            {
                ProofPhase.ToTempleForRestore => "ROUTE    ENTRANCE → TEMPLE → RESTORE",
                ProofPhase.DirectToBasin => "ROUTE    DIRECT ENTRANCE → BASIN",
                ProofPhase.DirectToTemple => "ROUTE    DIRECT BASIN → TEMPLE",
                _ => "ROUTE    SMOOTH + DIRECT + RECOVERY VERIFIED"
            });
    }

    private void DisableGameplayOwners()
    {
        _player.GetNode<PlayerInputReader>("Input").SetPhysicsProcess(false);
        _player.GetNode<PlayerMotor>("Motor").SetPhysicsProcess(false);
        _player.GetNode<PlayerCombat>("Combat").SetPhysicsProcess(false);
        _player.GetNode<PlayerShieldController>("Shield").SetPhysicsProcess(false);
    }

    private void AssertAuthorizedEntranceRelocation(string operation)
    {
        if (_world.CurrentSector != PrologueSectorId.CanyonEntrance)
            throw new InvalidOperationException($"Far {operation} did not select CanyonEntrance.");
        if (_player.GlobalPosition.DistanceTo(_authoredPlayerPosition) > .01f)
            throw new InvalidOperationException(
                $"Far {operation} did not restore the authored player coordinate.");
        Rect2 camera = CameraLimits();
        if (camera.Position.DistanceTo(_entranceBounds.Position) > 1.1f ||
            camera.End.DistanceTo(_entranceBounds.End) > 1.1f)
            throw new InvalidOperationException(
                $"Far {operation} left camera limits interpolating over unloaded space: {camera}.");
        AssertViewportAndPlayerCovered();
    }

    private void AssertAuthorizedDirectRelocation(PrologueSectorId expected,
        Rect2 expectedBounds, string operation)
    {
        if (_world.CurrentSector != expected)
            throw new InvalidOperationException(
                $"Direct {operation} selected {_world.CurrentSector}, expected {expected}.");
        Rect2 camera = CameraLimits();
        if (camera.Position.DistanceTo(expectedBounds.Position) > 1.1f ||
            camera.End.DistanceTo(expectedBounds.End) > 1.1f)
            throw new InvalidOperationException(
                $"Direct {operation} left camera limits interpolating over unloaded space: {camera}.");
        AssertViewportAndPlayerCovered();
    }

    private void AssertViewportAndPlayerCovered()
    {
        if (!PointCoveredByMountedWorld(_player.GlobalPosition))
            throw new InvalidOperationException(
                $"Player {_player.GlobalPosition} is outside all mounted sector bounds.");
        Rect2 camera = CameraLimits();
        Vector2[] samples =
        [
            camera.Position,
            new Vector2(camera.End.X, camera.Position.Y),
            camera.GetCenter(),
            new Vector2(camera.Position.X, camera.End.Y),
            camera.End
        ];
        foreach (Vector2 sample in samples)
            if (!PointCoveredByMountedWorld(sample))
                throw new InvalidOperationException(
                    $"Camera-limit sample {sample} is outside mounted world coverage.");
    }

    private bool PointCoveredByMountedWorld(Vector2 point)
    {
        foreach (PrologueSectorId id in _world.MountedSectorIds)
        {
            Rect2 bounds = _worldBounds[id];
            if (point.X >= bounds.Position.X - .01f && point.X <= bounds.End.X + .01f &&
                point.Y >= bounds.Position.Y - .01f && point.Y <= bounds.End.Y + .01f) return true;
        }
        return false;
    }

    private Rect2 CameraLimits()
    {
        Camera2D camera = _player.GetNode<Camera2D>("Camera2D");
        return new Rect2(camera.LimitLeft, camera.LimitTop,
            camera.LimitRight - camera.LimitLeft,
            camera.LimitBottom - camera.LimitTop);
    }

    private static PackedScene PackSector(PrologueSectorId id, int index, Rect2 bounds)
    {
        var sector = new PrologueSector
        {
            Name = id.ToString(),
            SectorId = id,
            Bounds = bounds
        };
        sector.AddChild(new Polygon2D
        {
            Name = "DiagnosticBand",
            Polygon =
            [
                bounds.Position,
                new Vector2(bounds.End.X, bounds.Position.Y),
                bounds.End,
                new Vector2(bounds.Position.X, bounds.End.Y)
            ],
            Color = SectorColors[index]
        });
        sector.AddChild(new Line2D
        {
            Name = "Seam",
            Points =
            [
                bounds.Position,
                new Vector2(bounds.Position.X, bounds.End.Y)
            ],
            Width = 4f,
            DefaultColor = new Color("d4a83f")
        });
        sector.AddChild(new Label
        {
            Name = "SectorLabel",
            Position = bounds.Position + new Vector2(30f, 30f),
            Text = id.ToString().ToUpperInvariant(),
            Modulate = new Color("f4e7c3")
        });
        Own(sector, sector);
        var packed = new PackedScene();
        Error error = packed.Pack(sector);
        sector.Free();
        if (error != Error.Ok)
            throw new InvalidOperationException($"Could not pack proof sector {id}: {error}");
        return packed;
    }

    private static CanvasLayer BuildOverlay()
    {
        var overlay = new CanvasLayer { Name = "ProofOverlay", Layer = 50 };
        overlay.AddChild(new ColorRect
        {
            Name = "Backdrop",
            Position = new Vector2(24f, 22f),
            Size = new Vector2(1232f, 116f),
            Color = new Color(0.025f, 0.04f, 0.055f, .90f)
        });
        overlay.AddChild(new Label
        {
            Name = "Title",
            Position = new Vector2(46f, 34f),
            Size = new Vector2(1160f, 28f),
            Text = "TASK 7 · PERSISTENT SEAMLESS WORLD",
            Modulate = new Color("d4a83f")
        });
        overlay.AddChild(new Label
        {
            Name = "State",
            Position = new Vector2(46f, 68f),
            Size = new Vector2(430f, 64f)
        });
        overlay.AddChild(new Label
        {
            Name = "Mounted",
            Position = new Vector2(496f, 68f),
            Size = new Vector2(700f, 64f)
        });
        return overlay;
    }

    private static void Own(Node node, Node owner)
    {
        foreach (Node child in node.GetChildren())
        {
            child.Owner = owner;
            Own(child, owner);
        }
    }

    private static string Names(IReadOnlyList<PrologueSectorId> ids)
    {
        var names = new string[ids.Count];
        for (int index = 0; index < ids.Count; index++) names[index] = ids[index].ToString();
        return string.Join(" · ", names);
    }

    private enum ProofPhase { ToTempleForRestore, DirectToBasin, DirectToTemple, Complete }
}

internal partial class SeamlessProofDriver : Node
{
    public float DriveAxis { get; set; }
    public float Speed { get; set; }
    public Vector2? PendingTeleport { get; set; }

    public override void _PhysicsProcess(double delta)
    {
        var player = GetParent<CharacterBody2D>();
        if (PendingTeleport is { } teleport)
        {
            player.GlobalPosition = teleport;
            player.Velocity = Vector2.Zero;
            PendingTeleport = null;
        }
        player.Velocity = new Vector2(DriveAxis * Speed, 0f);
        player.MoveAndSlide();
    }
}
