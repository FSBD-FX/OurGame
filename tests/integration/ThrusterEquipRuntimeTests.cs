using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Config;
using Knight.Player;
using Knight.Mechanisms;
using Knight.Shared;
using Knight.Build;
using Knight.Abilities;

namespace Knight.Tests.Integration;

public static class ThrusterEquipRuntimeTests
{
    public static async Task RunFormalModule(SceneTree tree)
    {
        string path = "res://scenes/player/Player.tscn";
        string text = File.ReadAllText(ProjectSettings.GlobalizePath(path));
        foreach (string field in new[] { "ModuleId", "BurnDuration", "ThrustAcceleration", "PoweredProjectionLimit",
            "GroundFriction", "AirDragRate", "GroundReverseBrake", "AirReverseBrake" })
            TestAssert.Equal(1, System.Text.RegularExpressions.Regex.Matches(text, "(?m)^" + field + " = ").Count,
                "formal saved Player explicitly authors standard module field " + field);
        await StationaryInstallFreezesAndDrivesRealMotion(tree, null, false, null, path);
        GD.Print("THRUSTER_FORMAL_MODULE_OK formal_saved_player=true explicit_standard_resource=true all_eight_serialized=true automatic_keyboard=true no_injection=true human=false whole_route=false");
    }

    public static async Task RunClawGuards(SceneTree tree)
    {
        MethodInfo install = typeof(PlayerMotor).GetMethod("TryEquipMobilityModule", BindingFlags.Instance | BindingFlags.Public);
        using var definition = new MobilityModuleDefinition { ModuleId = "mobility.rejected.claw.v1" };
        using var f = new EquipFixture(tree, "ActualClawEquipGuards");
        var anchor = new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0f, -260f), AssistRadius = 24f };
        f.Root.AddChild(anchor);
        await f.Start();
        var claw = f.Player.GetNode<MechanicalClawController>("MechanicalClaw");
        var required = new HashSet<MechanicalClawState> { MechanicalClawState.Aim, MechanicalClawState.Windup,
            MechanicalClawState.Travel, MechanicalClawState.Latched, MechanicalClawState.Reel,
            MechanicalClawState.Swing, MechanicalClawState.Boost };
        var visited = new HashSet<MechanicalClawState>();
        Exception failure = null;
        int pins = 0, unpins = 0;
        void Check(string point)
        {
            if (failure != null) return;
            try
            {
                var frame = claw.CurrentFrame;
                ulong authority = claw.AuthorityRevision;
                GrappleSurface2D target = claw.CurrentTarget, resident = claw.ResidencyTarget;
                RejectUnchanged(install, f, definition, "real claw callback equip rejection at " + point);
                object[] arguments = { definition, null };
                TestAssert.True(!(bool)install.Invoke(f.Motor, arguments) &&
                    (MobilityEquipRejection)arguments[1] == MobilityEquipRejection.ClawActive,
                    "real phase/retained target guard is the actual rejection reason at " + point);
                TestAssert.Equal(frame, claw.CurrentFrame, "equip request does not change real claw frame at " + point);
                TestAssert.Equal(authority, claw.AuthorityRevision, "equip request does not change cast authority at " + point);
                TestAssert.True(ReferenceEquals(target, claw.CurrentTarget) && ReferenceEquals(resident, claw.ResidencyTarget),
                    "equip request does not unpin or change target at " + point);
                GD.Print($"THRUSTER_EQUIP_CLAW_REJECT point={point} phase={frame.State} floor={f.Player.IsOnFloor()} cast={frame.CastId} target={target?.GetInstanceId()} resident={resident?.GetInstanceId()} reason=ClawActive");
            }
            catch (Exception exception) { failure = exception; }
        }
        claw.ClawFrameChanged += (frame, _) =>
        {
            if (required.Contains(frame.State) && visited.Add(frame.State)) Check(frame.State.ToString());
        };
        claw.TargetResidencyChanged += (_, resident) =>
        {
            if (resident) pins++; else unpins++;
            Check(resident ? "pin" : "unpin");
        };
        ThrusterPlayerRuntimeTests.KeyEvent(Key.W, true);
        await ThrusterPlayerRuntimeTests.Frames(tree, f.Input, 1);
        TestAssert.True(ReferenceEquals(claw.KeyboardPreview.Surface, anchor), "real keyboard W selects the actual anchor");
        ThrusterPlayerRuntimeTests.KeyEvent(Key.K, true);
        await ThrusterPlayerRuntimeTests.Frames(tree, f.Input, 1);
        ThrusterPlayerRuntimeTests.KeyEvent(Key.K, false);
        for (int tick = 0; tick < 150 && claw.CurrentFrame.State != MechanicalClawState.Swing && failure == null; tick++)
            await ThrusterPlayerRuntimeTests.Frames(tree, f.Input, 1);
        if (failure != null) throw failure;
        TestAssert.Equal(MechanicalClawState.Swing, claw.CurrentFrame.State, "equip rejection does not stop real pull-to-swing progression");
        ThrusterPlayerRuntimeTests.KeyEvent(Key.Space, true);
        await ThrusterPlayerRuntimeTests.Frames(tree, f.Input, 2);
        if (failure != null) throw failure;
        TestAssert.True(required.SetEquals(visited), "all seven actual published claw phases were observed and rejected");
        TestAssert.Equal(1, pins, "equip requests preserve exactly one original pin");
        TestAssert.Equal(1, unpins, "real Space naturally releases its original pin once");
        TestAssert.True(claw.CurrentTarget == null && claw.ResidencyTarget == null &&
            f.Motor.AcceptedJumpCount == 0 && f.Motor.AcceptedDashCount == 0,
            "real boost release finishes without extra ordinary jump or jet");
        GD.Print("THRUSTER_EQUIP_CLAW_OK actual_phases=7 real_pin_unpin=true rejection_reason=ClawActive unchanged_transaction=true automatic_keyboard=true artificial_phases=false human=false");
    }

    public static void PrepareSavedModules()
    {
        string storage = IsolatedPrologueFixture.ValidateStorageRoot();
        string path = Path.Combine(storage, "sentinel-module.tres");
        TestAssert.True(!File.Exists(path), "sentinel author input must be fresh");
        using var module = new MobilityModuleDefinition
        {
            ModuleId = "mobility.short-jet.test.v1", BurnDuration = .23f, ThrustAcceleration = 4321f,
            PoweredProjectionLimit = 765f, GroundFriction = 1234f, AirDragRate = 2.3f,
            GroundReverseBrake = 3456f, AirReverseBrake = 2345f
        };
        TestAssert.Equal(Error.Ok, ResourceSaver.Save(module, path), "save actual nondefault author Resource");
        GD.Print("THRUSTER_SAVED_PREPARE_OK authored_resource=true actual_builder_run=false");
    }

    public static async Task RunSavedModules(SceneTree tree)
    {
        string storage = IsolatedPrologueFixture.ValidateStorageRoot();
        foreach (string variant in new[] { "standard", "sentinel" })
        {
            string serialized = File.ReadAllText(Path.Combine(storage, "relocated-" + variant, "Player.tscn"));
            foreach (string field in new[] { "ModuleId", "BurnDuration", "ThrustAcceleration", "PoweredProjectionLimit",
                "GroundFriction", "AirDragRate", "GroundReverseBrake", "AirReverseBrake" })
                TestAssert.Equal(1, System.Text.RegularExpressions.Regex.Matches(serialized, "(?m)^" + field + " = ").Count,
                    "actual saved module stores each field explicitly, independent of future script defaults: " + variant + "/" + field);
        }
        await StationaryInstallFreezesAndDrivesRealMotion(tree, null, false, null,
            Path.Combine(storage, "relocated-standard", "Player.tscn"));
        await StationaryInstallFreezesAndDrivesRealMotion(tree, null, true, null,
            Path.Combine(storage, "relocated-sentinel", "Player.tscn"));
        GD.Print("THRUSTER_SAVED_MODULE_OK real_builder=true relocated_bytes=true embedded_resource=true all_eight_serialized=true default_cache=true two_modules_actual_motion=true resource_hot_edit_frozen=true human=false whole_route=false");
    }

    public static void RunAuthoringContract()
    {
        MethodInfo output = typeof(BuildPlayer).GetMethod("ValidateCandidateOutput", BindingFlags.Public | BindingFlags.Static);
        TestAssert.True(output != null, "real BuildPlayer validated candidate output entry is unavailable");
        string storage = OS.GetCmdlineUserArgs().Single(a => a.StartsWith("--case-storage-root=", StringComparison.Ordinal))["--case-storage-root=".Length..];
        string empty = Path.Combine(storage, "authoring-empty");
        Directory.CreateDirectory(empty);
        string project = ProjectSettings.GlobalizePath("res://"), user = OS.GetUserDataDir();
        object Parse(string[] args) => output.Invoke(null, new object[] { args, project, user });
        TestAssert.Equal(Path.Combine(empty, "Player.tscn"), (string)Parse(new[] { "--player-output-root=" + empty }),
            "real builder parser admits a unique empty external directory and fixed filename");
        int rejected = 0;
        foreach (string[] args in new[] {
            Array.Empty<string>(), new[] { "--player-output-root=" }, new[] { "--player-output-root=relative" },
            new[] { "--player-output-root=" + empty, "--player-output-root=" + empty },
            new[] { "--player-output-root=" + project }, new[] { "--player-output-root=" + Path.Combine(project, "candidate") },
            new[] { "--player-output-root=" + Directory.GetParent(project.TrimEnd('/', '\\')).FullName },
            new[] { "--player-output-root=" + user }, new[] { "--player-output-root=" + Path.GetPathRoot(empty) },
            new[] { "--player-output-root=" + Path.Combine(storage, "missing") },
            new[] { "--player-output-root=" + empty, "--player-typo=x" } })
        {
            bool failed = false;
            try { Parse(args); } catch (TargetInvocationException e) when (e.InnerException is ArgumentException or InvalidOperationException) { failed = true; }
            TestAssert.True(failed, "invalid candidate output must never fall back to formal Player"); rejected++;
        }
        Directory.CreateDirectory(Path.Combine(empty, "occupied"));
        bool nonemptyRejected = false;
        try { Parse(new[] { "--player-output-root=" + empty }); }
        catch (TargetInvocationException e) when (e.InnerException is InvalidOperationException) { nonemptyRejected = true; }
        TestAssert.True(nonemptyRejected, "nonempty output refuses overwrite");
        MethodInfo migration = typeof(PlayerTuning).GetMethod("ValidateThrusterMigration", BindingFlags.Instance | BindingFlags.Public);
        TestAssert.True(migration != null, "actual author resource legacy mobility override diagnostic is unavailable");
        using var tuning = new PlayerTuning();
        string source = "candidate-author::PlayerTuning";
        string[] Check() => ((System.Collections.Generic.IReadOnlyList<string>)migration.Invoke(tuning, new object[] { source })).ToArray();
        TestAssert.Equal(0, Check().Length, "legacy defaults remain legal");
        tuning.Gravity = .01f;
        TestAssert.Equal(0, Check().Length, "unrelated legitimate gravity override remains legal");
        foreach (string field in new[] { "GroundDeceleration", "DashSpeed", "DashSeconds" })
        {
            PropertyInfo property = typeof(PlayerTuning).GetProperty(field);
            float before = (float)property.GetValue(tuning); property.SetValue(tuning, before + 1f);
            string[] errors = Check();
            TestAssert.True(errors.Length == 1 && errors[0].Contains(source, StringComparison.Ordinal) &&
                errors[0].Contains(field, StringComparison.Ordinal), "legacy nondefault reports actual author source and field: " + field);
            property.SetValue(tuning, before);
        }
        GD.Print($"THRUSTER_AUTHORING_CONTRACT_OK output_rejections={rejected + 1} unique_external=true no_fallback=true legacy_three_fields=true unrelated_override_legal=true actual_builder_run=false saved_resource_proved=false");
    }

    public static async Task RunInitialModule(SceneTree tree)
    {
        PropertyInfo initial = typeof(PlayerMotor).GetProperty("InitialMobilityModule", BindingFlags.Instance | BindingFlags.Public);
        TestAssert.True(initial != null && initial.PropertyType == typeof(MobilityModuleDefinition) &&
            initial.GetCustomAttribute<ExportAttribute>() != null, "actual Motor initial module Resource export is unavailable");
        await StationaryInstallFreezesAndDrivesRealMotion(tree, null, true, initial);
        await StationaryInstallFreezesAndDrivesRealMotion(tree, null, false, initial);
        GD.Print("THRUSTER_INITIAL_MODULE_OK exported_resource=true initial_values_frozen=true two_modules_actual_motion=true saved_resource_proved=false human=false");
    }

    public static async Task Run(SceneTree tree)
    {
        MethodInfo install = typeof(PlayerMotor).GetMethod("TryEquipMobilityModule", BindingFlags.Instance | BindingFlags.Public);
        TestAssert.True(install != null, "actual Motor module equip entry is unavailable");
        await StationaryInstallFreezesAndDrivesRealMotion(tree, install, true);
        await StationaryInstallFreezesAndDrivesRealMotion(tree, install, false);
        GD.Print("THRUSTER_EQUIP_STATIONARY_OK actual_saved_player=true atomic_snapshot=true resource_edit_frozen=true two_modules_actual_motion=true automatic_keyboard=true eligibility_complete=false saved_resource_proved=false human=false");
    }

    public static async Task RunEligibility(SceneTree tree)
    {
        MethodInfo install = typeof(PlayerMotor).GetMethod("TryEquipMobilityModule", BindingFlags.Instance | BindingFlags.Public);
        TestAssert.True(install != null, "actual Motor module equip entry is unavailable");
        await RejectAirborneReplacement(tree, install);
        await CompletedSupportTicksQualify(tree, install);
        await MovingSupportQualifies(tree, install);
        GD.Print("THRUSTER_EQUIP_SUPPORT_OK airborne_rejected=true two_completed_ticks=true repeated_request_not_tick=true moving_platform_relative=true automatic_physics=true eligibility_complete=false saved_resource_proved=false human=false");
    }

    public static async Task RunGuards(SceneTree tree)
    {
        MethodInfo install = typeof(PlayerMotor).GetMethod("TryEquipMobilityModule", BindingFlags.Instance | BindingFlags.Public);
        TestAssert.True(install != null, "actual Motor module equip entry is unavailable");
        using var definition = new MobilityModuleDefinition { ModuleId = "mobility.guard.request.v1" };
        using (var f = new EquipFixture(tree, "QueuedSupportEquip"))
        {
            await f.Start(); f.Floor.QueueFree();
            RejectUnchanged(install, f, definition, "queued support cannot authorize a module replacement");
        }
        foreach (string mode in new[] { "burning", "paused", "disabled", "replacement", "dead", "queued-owner" })
        {
            using var f = new EquipFixture(tree, "EquipGuard-" + mode);
            await f.Start();
            if (mode == "burning")
            {
                ThrusterPlayerRuntimeTests.KeyEvent(Key.Shift, true);
                await ThrusterPlayerRuntimeTests.Frames(tree, f.Input, 1);
                TestAssert.True(f.Motor.IsDashing, "burning equip negative has a real active pulse");
            }
            if (mode == "paused") tree.Paused = true;
            if (mode == "disabled") f.Motor.SetPhysicsProcess(false);
            if (mode == "replacement") f.Motor.ExternalImpulse(Vector2.Zero);
            if (mode == "dead") TestAssert.True(f.Health.ReceiveDamage(new DamageRequest(5,
                Vector2.Zero, 0f, 9101UL, f.Root.GetInstanceId())), "dead equip negative receives real lethal damage");
            if (mode == "queued-owner") f.Root.QueueFree();
            RejectUnchanged(install, f, definition, mode + " equip rejection preserves transaction state");
            if (mode is "paused" or "disabled")
            {
                if (mode == "paused") tree.Paused = false;
                else f.Motor.SetPhysicsProcess(true);
                RejectUnchanged(install, f, definition, mode + " cannot reuse old support evidence immediately after controls resume");
            }
        }
        using (var f = new EquipFixture(tree, "InvalidEquipDefinition"))
        {
            await f.Start();
            RejectUnchanged(install, f, null, "null definition rejects atomically");
            foreach (string field in new[] { "BurnDuration", "ThrustAcceleration", "PoweredProjectionLimit",
                "GroundFriction", "AirDragRate", "GroundReverseBrake", "AirReverseBrake" })
            {
                using var invalid = new MobilityModuleDefinition();
                typeof(MobilityModuleDefinition).GetProperty(field).SetValue(invalid, float.NaN);
                RejectUnchanged(install, f, invalid, "nonfinite " + field + " rejects atomically");
            }
        }
        await RunningThenStoppingQualifies(tree, install, definition);
        foreach (bool beforeInput in new[] { true, false })
            foreach (int keys in new[] { 1, 2, 3 })
                await PendingPhysicalInputRejects(tree, install, definition, beforeInput, keys);
        GD.Print("THRUSTER_EQUIP_GUARDS_OK queued_support=true active_burn=true paused_disabled_fresh=true replacement=true dead_queued_owner=true invalid_atomic=true real_run_and_stop=true native_pending_windows=6 saved_resource_proved=false human=false");
    }

    private static void RejectUnchanged(MethodInfo install, EquipFixture f,
        MobilityModuleDefinition definition, string reason)
    {
        ThrusterPulseSnapshot state = State(f.Motor);
        Vector2 position = f.Player.GlobalPosition, velocity = f.Player.Velocity;
        int hp = f.Health.CurrentHealth;
        ulong landings = f.Motor.LandingCount;
        TestAssert.True(!Equip(install, f.Motor, definition), reason);
        TestAssert.Equal(state, State(f.Motor), reason + ": entire pulse snapshot unchanged");
        TestAssert.Equal(position, f.Player.GlobalPosition, reason + ": position unchanged");
        TestAssert.Equal(velocity, f.Player.Velocity, reason + ": velocity unchanged");
        TestAssert.Equal(hp, f.Health.CurrentHealth, reason + ": health unchanged");
        TestAssert.Equal(landings, f.Motor.LandingCount, reason + ": no new landing");
    }

    private static async Task RunningThenStoppingQualifies(SceneTree tree, MethodInfo install,
        MobilityModuleDefinition definition)
    {
        using var f = new EquipFixture(tree, "ActualRunStopEquip");
        await f.Start();
        ThrusterPlayerRuntimeTests.KeyEvent(Key.D, true);
        await ThrusterPlayerRuntimeTests.Frames(tree, f.Input, 8);
        TestAssert.True(f.Player.IsOnFloor() && f.Player.GetRealVelocity().X > 100f,
            "running negative is real supported tangential motion");
        RejectUnchanged(install, f, definition, "running supported player cannot equip");
        int stoppedTicks = 0;
        Exception failure = null;
        f.Root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100, Observe = () =>
        {
            if (stoppedTicks >= 2 || failure != null || !f.Player.IsOnFloor() || Math.Abs(f.Player.GetRealVelocity().X) >= 1f) return;
            try
            {
                stoppedTicks++;
                if (stoppedTicks == 1) RejectUnchanged(install, f, definition, "first newly stopped tick cannot equip");
                else TestAssert.True(Equip(install, f.Motor, definition), "second newly stopped tick can equip");
            }
            catch (Exception exception) { failure = exception; }
        } });
        ThrusterPlayerRuntimeTests.Release();
        for (int tick = 0; tick < 100 && stoppedTicks < 2 && failure == null; tick++)
            await ThrusterPlayerRuntimeTests.Frames(tree, f.Input, 1);
        if (failure != null) throw failure;
        TestAssert.Equal(2, stoppedTicks, "normal friction actually reaches both distinct stopped ticks");
    }

    private static async Task PendingPhysicalInputRejects(SceneTree tree, MethodInfo install,
        MobilityModuleDefinition definition, bool beforeInput, int keys)
    {
        using var f = new EquipFixture(tree, $"PendingEquip-{beforeInput}-{keys}");
        await f.Start();
        bool sent = false, checkedRequest = false;
        ulong oldSequence = 0UL;
        Exception failure = null;
        void Check()
        {
            if (checkedRequest || failure != null) return;
            checkedRequest = true;
            try
            {
                GD.Print($"THRUSTER_EQUIP_PENDING_DIAGNOSTIC before_input={beforeInput} keys={keys} " +
                    $"physics={Engine.GetPhysicsFrames()} inPhysics={Engine.IsInPhysicsFrame()} " +
                    $"oldSeq={oldSequence} seq={f.Input.CurrentFrameSequence} rawJump={f.Input.RawFrame.JumpPressed} " +
                    $"rawDash={f.Input.RawFrame.DashPressed} jumpEdge={Input.IsActionJustPressed(InputActions.Jump)} " +
                    $"dashEdge={Input.IsActionJustPressed(InputActions.Dash)} jumpHeld={Input.IsActionPressed(InputActions.Jump)} " +
                    $"dashHeld={Input.IsActionPressed(InputActions.Dash)} physicalSpace={Input.IsPhysicalKeyPressed(Key.Space)} " +
                    $"physicalShift={Input.IsPhysicalKeyPressed(Key.Shift)}");
                if (beforeInput)
                    TestAssert.Equal(oldSequence, f.Input.CurrentFrameSequence, "pre-Input request really precedes the new sample");
                else
                {
                    TestAssert.True(f.Input.CurrentFrameSequence > oldSequence, "pre-Motor request sees the newly sampled native input");
                    TestAssert.True(((keys & 1) == 0 || f.Input.RawFrame.JumpPressed) &&
                        ((keys & 2) == 0 || f.Input.RawFrame.DashPressed), "Raw facts retain both real pending keys");
                    if (keys == 3) TestAssert.True(!f.Input.CurrentFrame.JumpPressed && f.Input.CurrentFrame.DashPressed,
                        "actual Shift arbitration consumed ordinary Space while Raw still retains it");
                }
                TestAssert.True(((keys & 1) == 0 || Input.IsActionJustPressed(InputActions.Jump)) &&
                    ((keys & 2) == 0 || Input.IsActionJustPressed(InputActions.Dash)), "native pending action edges actually exist");
                RejectUnchanged(install, f, definition, "pending native Space/Shift prevents equip before action commit");
            }
            catch (Exception exception) { failure = exception; }
        }
        f.Root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = -350, Observe = () =>
        {
            if (sent && beforeInput) Check();
        } });
        f.Root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = -1, Observe = () =>
        {
            if (sent && !beforeInput) Check();
        } });
        TestAssert.True(!Engine.IsInPhysicsFrame(), "native key events are delivered between physics steps, not during a running step");
        oldSequence = f.Input.CurrentFrameSequence;
        if ((keys & 1) != 0) ThrusterPlayerRuntimeTests.KeyEvent(Key.Space, true);
        if ((keys & 2) != 0) ThrusterPlayerRuntimeTests.KeyEvent(Key.Shift, true);
        sent = true;
        await ThrusterPlayerRuntimeTests.Frames(tree, f.Input, 2);
        if (failure != null) throw failure;
        TestAssert.True(sent && checkedRequest, "both scheduler observers execute the real pending-input boundary");
        TestAssert.Equal((keys & 2) != 0 ? 1 : 0, f.Motor.AcceptedDashCount, "pending input naturally executes its one winning pulse");
        TestAssert.Equal(keys == 1 ? 1 : 0, f.Motor.AcceptedJumpCount, "equip rejection does not swallow the original ordinary jump");
        GD.Print($"THRUSTER_EQUIP_PENDING_OK before_input={beforeInput} keys={keys} physical_events=true");
    }

    private sealed class EquipFixture : IDisposable
    {
        private readonly SceneTree _tree;
        private readonly RectangleShape2D _shape = new() { Size = new Vector2(1200f, 20f) };
        internal Node2D Root { get; }
        internal StaticBody2D Floor { get; }
        internal CharacterBody2D Player { get; }
        internal PlayerInputReader Input => Player.GetNode<PlayerInputReader>("Input");
        internal PlayerMotor Motor => Player.GetNode<PlayerMotor>("Motor");
        internal HealthComponent Health => Player.GetNode<HealthComponent>("Health");

        internal EquipFixture(SceneTree tree, string name)
        {
            _tree = tree; Root = new Node2D { Name = name };
            Floor = new StaticBody2D { Position = new Vector2(0f, 60f), CollisionLayer = CombatCollisionLayers.World };
            Floor.AddChild(new CollisionShape2D { Shape = _shape }); Root.AddChild(Floor);
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position = new Vector2(0f, 14f); Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Root.AddChild(Player);
        }

        internal async Task Start()
        {
            ThrusterPlayerRuntimeTests.Release(); _tree.Root.AddChild(Root);
            await ThrusterPlayerRuntimeTests.Frames(_tree, Input, 8);
            TestAssert.True(Player.IsOnFloor() && Player.Velocity == Vector2.Zero, "equip fixture starts on legal stationary support");
        }

        public void Dispose()
        {
            ThrusterPlayerRuntimeTests.Release(); _tree.Paused = false;
            if (GodotObject.IsInstanceValid(Root)) Root.Free();
            _shape.Dispose();
        }
    }

    private static async Task RejectAirborneReplacement(SceneTree tree, MethodInfo install)
    {
        using var definition = new MobilityModuleDefinition { ModuleId = "mobility.rejected.air.v1" };
        var root = new Node2D { Name = "AirborneEquipRejection" };
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, -500f); player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 2);
            TestAssert.True(!player.IsOnFloor() && player.Velocity.Y > 0f, "equip negative is an actual automatic airborne fall");
            ThrusterPulseSnapshot before = State(motor);
            Vector2 position = player.GlobalPosition, velocity = player.Velocity;
            TestAssert.True(!Equip(install, motor, definition), "airborne module replacement must be rejected");
            TestAssert.Equal(before, State(motor), "air rejection preserves the entire module and use state");
            TestAssert.Equal(position, player.GlobalPosition, "air rejection cannot relocate player");
            TestAssert.Equal(velocity, player.Velocity, "air rejection cannot freeze or replace free-fall velocity");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }

    private static async Task CompletedSupportTicksQualify(SceneTree tree, MethodInfo install)
    {
        using var shape = new RectangleShape2D { Size = new Vector2(1200f, 20f) };
        using var definition = new MobilityModuleDefinition { ModuleId = "mobility.two-support-ticks.v1" };
        var root = new Node2D { Name = "TwoRealCompletedSupportTicks" };
        var floor = new StaticBody2D { Position = new Vector2(0f, 60f), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = shape }); root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, 14f); player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        int completedSupports = 0;
        ulong firstStamp = 0UL, secondStamp = 0UL;
        bool firstRejected = false, repeatedRejected = false, secondAccepted = false;
        Exception failure = null;
        root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100, Observe = () =>
        {
            if (!player.IsOnFloor() || completedSupports >= 2 || failure != null) return;
            try
            {
                completedSupports++;
                ThrusterPulseSnapshot before = State(motor);
                if (completedSupports == 1)
                {
                    firstStamp = Engine.GetPhysicsFrames();
                    firstRejected = !Equip(install, motor, definition);
                    repeatedRejected = !Equip(install, motor, definition);
                    TestAssert.True(firstRejected && repeatedRejected, "one real support tick and repeated requests cannot qualify equip");
                    TestAssert.Equal(before, State(motor), "repeated first-tick rejection cannot alter installed state");
                }
                else
                {
                    secondStamp = Engine.GetPhysicsFrames();
                    TestAssert.True(secondStamp > firstStamp, "second support is a distinct completed native physics tick");
                    secondAccepted = Equip(install, motor, definition);
                    TestAssert.True(secondAccepted, "second real stationary supported tick qualifies equip");
                    TestAssert.Equal(before.ConfigurationEpoch + 1UL, State(motor).ConfigurationEpoch,
                        "qualified second tick commits exactly one module epoch");
                }
            }
            catch (Exception exception) { failure = exception; }
        } });
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 4);
            if (failure != null) throw failure;
            TestAssert.True(firstRejected && repeatedRejected && secondAccepted,
                "actual first and second support callbacks both execute their distinct equip outcomes");
            GD.Print($"THRUSTER_EQUIP_TWO_TICKS_OK first={firstStamp} second={secondStamp} no_request_counting=true");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }

    private static async Task MovingSupportQualifies(SceneTree tree, MethodInfo install)
    {
        using var shape = new RectangleShape2D { Size = new Vector2(1200f, 40f) };
        using var definition = new MobilityModuleDefinition { ModuleId = "mobility.carried-stand.v1" };
        var root = new Node2D { Name = "MovingCarriageModuleEquip" };
        var lift = new CanyonCounterweightLift
        {
            Position = new Vector2(0f, 70f), LowerPosition = new Vector2(0f, 70f),
            UpperPosition = new Vector2(240f, 70f), TravelSeconds = 2f,
            EndpointDwellSeconds = 0f, ProcessPhysicsPriority = -400
        };
        lift.AddChild(new CollisionShape2D { Shape = shape }); root.AddChild(lift);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, 14f); player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 8);
            lift.Activate(); await ThrusterPlayerRuntimeTests.Frames(tree, input, 30);
            TestAssert.True(player.IsOnFloor() && player.GetRealVelocity().X > 100f && player.GetPlatformVelocity().X > 100f,
                "carriage equip positive actually rides native horizontal motion, not a static fake");
            TestAssert.True(Math.Abs(player.GetRealVelocity().X - player.GetPlatformVelocity().X) < 1f,
                "real moving passenger is tangentially stationary relative to support");
            Vector2 position = player.GlobalPosition, velocity = player.Velocity;
            ThrusterPulseSnapshot before = State(motor);
            TestAssert.True(Equip(install, motor, definition), "real carried stationary player may replace a module");
            TestAssert.Equal(before.ConfigurationEpoch + 1UL, State(motor).ConfigurationEpoch, "carriage install commits one epoch");
            TestAssert.Equal(position, player.GlobalPosition, "carriage install itself does not teleport");
            TestAssert.Equal(velocity, player.Velocity, "carriage install itself does not replace velocity");
            GD.Print($"THRUSTER_EQUIP_MOVING_OK world={player.GetRealVelocity()} support={player.GetPlatformVelocity()} real_carriage=true");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }

    internal static ThrusterPulseSnapshot State(PlayerMotor motor) =>
        (ThrusterPulseSnapshot)(typeof(PlayerMotor).GetProperty("ThrusterState")?.GetValue(motor)
            ?? throw new InvalidOperationException("Actual Motor read-only thruster state is unavailable"));

    internal static bool Equip(MethodInfo install, PlayerMotor motor, MobilityModuleDefinition module)
    {
        object[] arguments = { module, null };
        return (bool)install.Invoke(motor, arguments);
    }

    private static async Task StationaryInstallFreezesAndDrivesRealMotion(SceneTree tree,
        MethodInfo install, bool sentinel, PropertyInfo initial = null, string savedPath = null)
    {
        using var shape = new RectangleShape2D { Size = new Vector2(1200f, 20f) };
        using var ownedDefinition = sentinel ? new MobilityModuleDefinition
        {
            ModuleId = "mobility.short-jet.test.v1", BurnDuration = .23f, ThrustAcceleration = 4321f,
            PoweredProjectionLimit = 765f, GroundFriction = 1234f, AirDragRate = 2.3f,
            GroundReverseBrake = 3456f, AirReverseBrake = 2345f
        } : new MobilityModuleDefinition();
        MobilityModuleDefinition definition = ownedDefinition;
        // Literal independent expected settings, not definition.ToSettings().
        MobilityModuleSettings expected = sentinel
            ? new("mobility.short-jet.test.v1", .23f, 4321f, 765f, 1234f, 2.3f, 3456f, 2345f)
            : new("mobility.short-jet.standard.v1", .18f, 5600f, 850f, 1800f, 1.6f, 4000f, 3000f);
        var root = new Node2D { Name = sentinel ? "SentinelModuleInstall" : "StandardModuleInstall" };
        var floor = new StaticBody2D
        {
            Position = new Vector2(0f, 60f), CollisionLayer = CombatCollisionLayers.World
        };
        floor.AddChild(new CollisionShape2D { Shape = shape }); root.AddChild(floor);
        var player = GD.Load<PackedScene>(savedPath ?? "res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0f, 14f); player.GetNode<Camera2D>("Camera2D").Enabled = false;
        root.AddChild(player);
        var input = player.GetNode<PlayerInputReader>("Input");
        var motor = player.GetNode<PlayerMotor>("Motor");
        var health = player.GetNode<HealthComponent>("Health");
        initial?.SetValue(motor, definition);
        bool observedStart = false;
        Vector2 firstVelocity = Vector2.Zero;
        root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 100, Observe = () =>
        {
            if (observedStart || !input.RawFrame.DashPressed || motor.AcceptedDashCount != 1) return;
            observedStart = true; firstVelocity = player.Velocity;
        } });
        ThrusterPlayerRuntimeTests.Release(); tree.Root.AddChild(root);
        try
        {
            if (savedPath != null)
            {
                definition = motor.InitialMobilityModule;
                TestAssert.True(definition != null, "real builder saved an explicit initial module; Standard fallback is not proof");
                TestAssert.Equal(expected, definition.ToSettings(), "all eight saved module fields match independent literals");
                TestAssert.True(definition.ResourcePath.Replace('\\', '/').StartsWith(savedPath.Replace('\\', '/') + "::", StringComparison.OrdinalIgnoreCase),
                    "module is embedded in the relocated scene, not linked to original author input");
                GD.Print($"THRUSTER_SAVED_RESOURCE path={definition.ResourcePath} id={definition.ModuleId} no_injection=true");
            }
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 8);
            TestAssert.True(player.IsOnFloor() && player.Velocity == Vector2.Zero,
                "module installation begins at real stationary support");
            ThrusterPulseSnapshot before = State(motor);
            Vector2 position = player.GlobalPosition, velocity = player.Velocity;
            int hp = health.CurrentHealth;
            ulong landings = motor.LandingCount;
            float fallHeight = motor.LastFallHeight;
            if (initial == null && savedPath == null)
                TestAssert.True(Equip(install, motor, definition), "stationary living player accepts a valid module");
            ThrusterPulseSnapshot after = State(motor);
            TestAssert.Equal(expected, after.Module, "actual Motor consumes all eight installed values");
            TestAssert.Equal(initial == null && savedPath == null ? before.ConfigurationEpoch + 1UL : 1UL,
                after.ConfigurationEpoch, "initial snapshot starts at epoch one; successful equip advances it once");
            TestAssert.Equal(before with { Module = after.Module, ConfigurationEpoch = after.ConfigurationEpoch,
                Revision = after.Revision }, after, "install preserves lifecycle, burn serial, budget and all pulse bookkeeping");
            TestAssert.Equal(position, player.GlobalPosition, "module install does not relocate player");
            TestAssert.Equal(velocity, player.Velocity, "module install does not replace velocity");
            TestAssert.Equal(hp, health.CurrentHealth, "module install does not heal or damage");
            TestAssert.Equal(landings, motor.LandingCount, "module install does not invent a landing");
            TestAssert.Equal(fallHeight, motor.LastFallHeight, "module install does not reset fall origin evidence");
            definition.ModuleId = "mobility.short-jet.edited.v2";
            definition.ThrustAcceleration = 9876f; definition.AirDragRate = .6f;
            TestAssert.Equal(expected, State(motor).Module, "author Resource edits cannot hot-change installed snapshot");
            ThrusterPlayerRuntimeTests.KeyEvent(Key.Shift, true);
            await ThrusterPlayerRuntimeTests.Frames(tree, input, 2);
            TestAssert.True(observedStart && motor.AcceptedDashCount == 1, "one actual physical Shift drives the installed module");
            // Starting from zero, grounded horizontal pulse has no passive loss,
            // gravity or ordinary drive: acceleration/60, well below either cap.
            TestAssert.Near(sentinel ? 72.0166667f : 93.3333333f, firstVelocity.X, .001f,
                "real first completed physics step uses independent installed thrust expectation");
            TestAssert.Near(0f, firstVelocity.Y, .001f, "horizontal installed module preserves grounded vertical motion");
            TestAssert.Equal(expected, State(motor).Module, "actual burn remains on frozen settings after Resource edit");
            GD.Print($"THRUSTER_EQUIP_MODULE_MOTION id={expected.ModuleId} first={firstVelocity} epoch={after.ConfigurationEpoch} actual_keyboard=true");
        }
        finally { ThrusterPlayerRuntimeTests.Release(); root.Free(); }
    }
}
