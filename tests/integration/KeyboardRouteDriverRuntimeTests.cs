using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Player;
using Knight.Shared;
using Knight.Tests.Presentation;

namespace Knight.Tests.Integration;

public static class KeyboardRouteDriverRuntimeTests
{
    public static async Task Run(SceneTree tree)
    {
        await VerifyLegacyTrace(tree);
        await VerifyApplicationFocusTrace(tree);
        var root = new Node2D { Name = "KeyboardRouteDriverFixture" };
        var floor = new StaticBody2D { Position = new Vector2(0,60), CollisionLayer = CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(800,20) } });
        root.AddChild(floor);
        var player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position = new Vector2(0,14);
        root.AddChild(player);
        var anchor = new GrappleSurface2D { Name="UpperAnchor", Position = new Vector2(0,-130) };
        root.AddChild(anchor);
        tree.Root.AddChild(root);
        var input = player.GetNode<PlayerInputReader>("Input");
        var combat = player.GetNode<PlayerCombat>("Combat");
        int attacks = 0;
        combat.AttackStarted += (_,_) => attacks++;
        using var driver = KeyboardRouteInputDriver.Begin(player);
        try
        {
            await Frames(tree,8);
            driver.Command(InputActions.SelectAttackMode,true);
            driver.Command(InputActions.SelectAttackMode,false);
            driver.Command(InputActions.PrimaryAction,true);
            await Frames(tree,3);
            TestAssert.True(attacks==1 && input.Current.KeyboardActions && !input.Current.PrimaryHeld,
                "normal-route driver sends physical J rather than legacy mode/primary actions");
            driver.Command(InputActions.PrimaryAction,false);
            await Frames(tree,30);
            driver.Command(InputActions.SelectGrappleMode,true);
            driver.Command(InputActions.SelectGrappleMode,false);
            driver.AimAt(player,anchor.GlobalPosition);
            driver.Command(InputActions.PrimaryAction,true);
            await Frames(tree,3);
            driver.Command(InputActions.PrimaryAction,false);
            await Frames(tree,20);
            var claw = player.GetNode<MechanicalClawController>("MechanicalClaw");
            TestAssert.True(claw.CurrentFrame.LockedTargetRuntimeId==anchor.GetInstanceId() &&
                claw.CurrentFrame.State is MechanicalClawState.Latched or MechanicalClawState.Reel or MechanicalClawState.Swing,
                "normal-route driver uses real W+K and the production visible target resolver");
            driver.Command(InputActions.PrimaryAction,true);
            await Frames(tree,3);
            driver.Command(InputActions.PrimaryAction,false);
            await Frames(tree,90);
            TestAssert.True(player.IsOnFloor(), "keyboard route K cancellation returns to actual support");
            driver.PointerButton(MouseButton.Right,true,player,player.GlobalPosition+Vector2.Up*80);
            await Frames(tree,3);
            var shield = player.GetNode<PlayerShieldController>("Shield");
            TestAssert.True(shield.IsActive && shield.GuardDirection.Dot(Vector2.Up)>.99f &&
                input.Current.KeyboardShieldHeld && !input.Current.LegacyShieldHeld && !input.HasObservedMouseMotion,
                "normal-route driver sends W+L without pointer motion or RMB");
            driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero);
            await Frames(tree,3);
            TestAssert.True(!shield.IsActive && !Input.IsActionPressed(InputActions.MoveUp),
                "keyboard route releases temporary shield aim as well as L");
            float beforeGuardX=player.GlobalPosition.X;
            for(int frame=0;frame<12;frame++)
            {
                driver.PointerButton(MouseButton.Right,true,player,player.GlobalPosition+Vector2.Right*80);
                await Frames(tree,1);
            }
            TestAssert.True(shield.IsActive && input.Current.MoveAxis==0f &&
                Mathf.Abs(player.GlobalPosition.X-beforeGuardX)<2f && shield.GuardDirection.Dot(Vector2.Right)>.99f,
                "neutral horizontal guard uses actual facing without forcing continuous movement");
            driver.PointerButton(MouseButton.Right,false,player,Vector2.Zero);
            await Frames(tree,3);
            Vector2 keyboardAim = input.KeyboardAimDirection;
            input.ClearSampledFrame();
            TestAssert.True(input.Current.Equals(default(PlayerInputFrame)), "normal lifecycle can withdraw its sampled frame");
            driver.ObserveFrame();
            Input.ParseInputEvent(new InputEventMouseMotion { Position = new Vector2(-10000f, 10000f), Relative = new Vector2(-200f, 300f) });
            Input.FlushBufferedEvents();
            await Frames(tree,3);
            TestAssert.True(input.HasObservedMouseMotion && input.MouseMotionSequence > 0 &&
                input.Current.KeyboardActions && input.KeyboardAimDirection == keyboardAim &&
                !input.Current.PrimaryPressed && !input.Current.PrimaryHeld && !input.Current.LegacyShieldHeld &&
                !input.Current.AttackModePressed && !input.Current.GrappleModePressed,
                "ambient pointer motion is observed but cannot change keyboard aim or consume a legacy action");
            driver.ObserveFrame();
            TestAssert.True(driver.ObservedMouseMotionSamples == input.MouseMotionSequence,
                "route witness separately retains actual observed ambient motion instead of claiming zero observed events");
            ulong pausedSample=input.CurrentFrameSequence;
            tree.Paused=true;
            try
            {
                driver.Command(InputActions.Attack,true); await Frames(tree,3);
                using JsonDocument pausedTrace=JsonDocument.Parse(driver.Trace.CreateJson("fixture"));
                TestAssert.True(input.CurrentFrameSequence==pausedSample &&
                    pausedTrace.RootElement.GetProperty("records").EnumerateArray().Any(row =>
                        Is(row,"received","J",true) && row.GetProperty("reader_sample_sequence").GetUInt64()==pausedSample),
                    "paused passive receipt does not imply production reader sampled or consumed the key");
                driver.Command(InputActions.Attack,false);
            }
            finally { tree.Paused=false; }
            await Frames(tree,3);
            // Characterize the route adapter's ownership boundary, not OS focus:
            // native input may withdraw a key while the adapter still owns its hold.
            driver.Command(InputActions.MoveUp,true); await Frames(tree,3);
            TestAssert.True(Input.IsPhysicalKeyPressed(Key.W) && input.KeyboardAimDirection.Dot(Vector2.Up)>.99f,
                "owned W first reaches the real native input and production sample");
            using (JsonDocument trace = JsonDocument.Parse(driver.Trace.CreateJson("fixture")))
            {
                JsonElement[] records = trace.RootElement.GetProperty("records").EnumerateArray().ToArray();
                // Parse may enqueue; only the original Flush boundary promises delivery.
                // Match the current injection, not a W edge from earlier claw/shield steps.
                ulong eventId = records.Last(row => Is(row,"after_flush","W",true)).GetProperty("event_id").GetUInt64();
                JsonElement[] edge = records.Where(row => row.GetProperty("event_id").GetUInt64()==eventId).ToArray();
                bool hasBoundaries = edge.Length==4 && edge.Any(row => Is(row,"before_parse","W",true)) &&
                    edge.Any(row => Is(row,"after_parse","W",true)) &&
                    edge.Any(row => Is(row,"received","W",true) && row.GetProperty("raw_physical_down").GetBoolean() &&
                        row.GetProperty("known_injected_id_match").GetBoolean()) &&
                    Is(edge.Last(),"after_flush","W",true) && edge.Last().GetProperty("raw_physical_down").GetBoolean();
                if (!hasBoundaries) GD.Print("KEYBOARD_INPUT_TRACE_FIXTURE_FAILURE "+trace.RootElement.GetRawText());
                TestAssert.True(hasBoundaries,
                    "real W injection retains enqueue and actual received/flush boundaries without assuming immediate parse delivery");
            }
            int edgesBeforeWithdrawal = driver.KeyDownCount;
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode=Key.W, Keycode=Key.W, Pressed=false });
            Input.FlushBufferedEvents(); await Frames(tree,3);
            TestAssert.True(!Input.IsPhysicalKeyPressed(Key.W) && input.Current.Aim==Vector2.Zero,
                "controlled native withdrawal removes W without changing the route's intent");
            bool rejectedWithdrawal = false;
            try { driver.ObserveFrame(); }
            catch (System.InvalidOperationException error)
            { rejectedWithdrawal = error.Message.Contains("keyboard route owned key was withdrawn") && error.Message.Contains("key=W"); }
            TestAssert.True(rejectedWithdrawal && !Input.IsPhysicalKeyPressed(Key.W) && driver.KeyDownCount==edgesBeforeWithdrawal,
                "route rejects a withdrawn owned hold without silently reissuing keys or accepting input continuity");
            string frozenTrace = driver.Trace.CreateJson("fixture");
            using (JsonDocument trace = JsonDocument.Parse(frozenTrace))
            {
                JsonElement data = trace.RootElement;
                TestAssert.True(data.GetProperty("frozen").GetBoolean() &&
                    data.GetProperty("freeze_reason").GetString()=="owned_key_withdrawn" &&
                    data.GetProperty("records").EnumerateArray().Any(row => Is(row,"received","W",false)) &&
                    Is(data.GetProperty("records").EnumerateArray().Last(),"owned_guard","W",false),
                    "controlled release reaches passive observer and original guard freezes first failure before cleanup");
            }
            driver.Command(InputActions.MoveUp,false); await Frames(tree,3);
            driver.ReleaseAll();
            TestAssert.Equal(frozenTrace, driver.Trace.CreateJson("fixture"), "cleanup cannot overwrite first withdrawn-key evidence");
            driver.ObserveFrame();
            var observer=tree.Root.GetNode<KeyboardRouteInputObserver>("KeyboardRouteInputObserver");
            Window window=observer.GetWindow();
            int enteredConnections=window.GetSignalConnectionList(Window.SignalName.FocusEntered).Count;
            int exitedConnections=window.GetSignalConnectionList(Window.SignalName.FocusExited).Count;
            driver.Dispose();
            TestAssert.True(KeyboardRouteInputDriver.Current==null && !observer.IsProcessingInput() && observer.IsQueuedForDeletion() &&
                window.GetSignalConnectionList(Window.SignalName.FocusEntered).Count==enteredConnections-1 &&
                window.GetSignalConnectionList(Window.SignalName.FocusExited).Count==exitedConnections-1,
                "driver dispose disables passive input and disconnects both native focus signals before scene teardown");
            TestAssert.Equal(frozenTrace, driver.Trace.CreateJson("fixture"), "dispose retains the original frozen failure");
            var retiredPlayer=new CharacterBody2D();
            var lateDriver=KeyboardRouteInputDriver.Begin(retiredPlayer);
            System.Exception lateFailure=null;
            bool lateDown=false,lateReleased=false;
            try
            {
                retiredPlayer.Free();
                lateDriver.Command(InputActions.Attack,true);
                lateDown=Input.IsPhysicalKeyPressed(Key.J);
                lateDriver.Command(InputActions.Attack,false);
                lateReleased=!Input.IsPhysicalKeyPressed(Key.J);
            }
            catch(System.Exception error) { lateFailure=error; }
            finally { try { lateDriver.Dispose(); } catch(System.Exception error) { lateFailure??=error; } }
            TestAssert.True(lateFailure==null && lateDown && lateReleased,
                "retired diagnostic target cannot prevent original native key injection and cleanup");
            using JsonDocument retiredTrace=JsonDocument.Parse(lateDriver.Trace.CreateJson("fixture"));
            TestAssert.Equal("observer_error",retiredTrace.RootElement.GetProperty("freeze_reason").GetString(),
                "retired observer target is retained as ancillary failure instead of a route exception");
            GD.Print("KEYBOARD_INPUT_TRACE_REAL_WITHDRAWAL_OK");
            GD.Print("KEYBOARD_ROUTE_OWNED_INPUT_WITHDRAWAL_OK");
            GD.Print("KEYBOARD_ROUTE_AMBIENT_MOTION_IGNORED_OK");
            GD.Print("KEYBOARD_ROUTE_DRIVER_RUNTIME_OK");
        }
        finally { driver.Dispose(); root.QueueFree(); await Frames(tree,3); }
    }
    internal static async Task RunIndependentFocus(SceneTree tree)
    {
        // Real native key withdrawal, not a claimed OS deactivation reproduction.
        var root=new Node2D { Name="IndependentFocusFixture" };
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        root.AddChild(player); tree.Root.AddChild(root);
        var trace=new KeyboardFocusTraceBuffer();
        var observer=new KeyboardRouteFocusObserver { Name="IndependentFocusObserver", Trace=trace };
        tree.Root.AddChild(observer);
        KeyboardRouteInputDriver driver=null;
        try
        {
            await Frames(tree,3);
            driver=KeyboardRouteInputDriver.Begin(player);
            driver.Command(InputActions.MoveUp,true); await Frames(tree,2);
            TestAssert.True(Input.IsPhysicalKeyPressed(Key.W),"independent focus fixture begins with actual held native W");
            Input.ParseInputEvent(new InputEventKey { PhysicalKeycode=Key.W,Keycode=Key.W,Pressed=false });
            Input.FlushBufferedEvents();
            bool rejected=false;
            try { driver.ObserveFrame(); }
            catch (System.InvalidOperationException error) { rejected=error.Message.Contains("owned key was withdrawn key=W"); }
            TestAssert.True(rejected && driver.Trace.Frozen && !Input.IsPhysicalKeyPressed(Key.W),
                "independent focus observation retains original strict withdrawal failure without repressing W");
            string frozen=driver.Trace.CreateJson("fixture");
            int keyDowns=driver.KeyDownCount;
            driver.Dispose();
            tree.Paused=true;
            observer.Notification(checked((int)Node.NotificationApplicationFocusOut));
            observer.OnFocusExited(); // Controlled callback seam, NOT an OS window signal claim.
            observer.Notification(checked((int)Node.NotificationApplicationFocusIn));
            observer.Notification(int.MaxValue);
            using (JsonDocument json=JsonDocument.Parse(trace.CreateJson("fixture")))
            {
                JsonElement[] rows=json.RootElement.GetProperty("records").EnumerateArray().ToArray();
                TestAssert.True(rows.Length==4 && rows[0].GetProperty("phase").GetString()=="observer_ready" &&
                    rows[1].GetProperty("phase").GetString()=="application_focus_out" &&
                    rows[2].GetProperty("phase").GetString()=="window_focus_exited" &&
                    rows[3].GetProperty("phase").GetString()=="application_focus_in" &&
                    !observer.IsProcessing() && !observer.IsPhysicsProcessing() && !observer.IsProcessingInput(),
                    "independent event-only focus observer survives original driver disposal and pause");
                TestAssert.True(rows.All(row => row.GetProperty("own_process_id").GetInt32()==System.Environment.ProcessId) &&
                    rows.All(row => row.GetProperty("foreground_status").GetString() is "observed" or "no_foreground" or "raced" or "query_failed"),
                    "Windows read-only adapter produces native status/own PID without focus acquisition");
            }
            TestAssert.True(!Input.IsPhysicalKeyPressed(Key.W) && driver.KeyDownCount==keyDowns &&
                KeyboardRouteInputDriver.Current==null,"focus evidence never restores withdrawn input or takes driver ownership");
            TestAssert.Equal(frozen,driver.Trace.CreateJson("fixture"),"new focus events leave the first strict keyboard tail byte-identical");
            observer.Stop("fixture_stop");
            string stopped=trace.CreateJson("fixture");
            observer.Notification(checked((int)Node.NotificationApplicationFocusOut)); observer.OnFocusExited(); observer.Stop("again");
            TestAssert.Equal(stopped,trace.CreateJson("fixture"),"independent observer stop prevents later events and is idempotent");
        }
        finally
        {
            tree.Paused=false; driver?.Dispose(); observer.Stop("fixture_finally");
            root.QueueFree(); await Frames(tree,3);
        }
        GD.Print("KEYBOARD_INDEPENDENT_FOCUS_OK real_key_withdrawal=true synthetic_focus_callbacks=true os_deactivation_proven=false polling=false");
    }
    private static async Task VerifyApplicationFocusTrace(SceneTree tree)
    {
        var root=new Node2D { Name="KeyboardApplicationFocusTraceFixture" };
        var floor=new StaticBody2D { Position=new Vector2(0,60),CollisionLayer=CombatCollisionLayers.World };
        floor.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(800,20) } });
        root.AddChild(floor);
        var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
        player.Position=new Vector2(0,14); root.AddChild(player); tree.Root.AddChild(root);
        KeyboardRouteInputDriver driver=null;
        try
        {
            await Frames(tree,8);
            driver=KeyboardRouteInputDriver.Begin(player);
            driver.Command(InputActions.MoveRight,true); await Frames(tree,3);
            var observer=tree.Root.GetNode<KeyboardRouteInputObserver>("KeyboardRouteInputObserver");
            var input=player.GetNode<PlayerInputReader>("Input");
            ulong sequence=input.CurrentFrameSequence;
            bool windowFocus=player.GetWindow().HasFocus();
            int keyDowns=driver.KeyDownCount;
            using JsonDocument before=JsonDocument.Parse(driver.Trace.CreateJson("fixture"));
            ulong recorded=before.RootElement.GetProperty("total_recorded").GetUInt64();
            // Notification() targets this observer only, not the main loop or
            // OS. This proves the hook, NEVER native focus/reset causality.
            tree.Paused=true;
            try
            {
                observer.Notification(checked((int)Node.NotificationApplicationFocusOut));
                observer.Notification(checked((int)Node.NotificationApplicationFocusIn));
                observer.Notification(int.MaxValue); // Unrelated notifications are ignored.
            }
            finally { tree.Paused=false; }
            using JsonDocument trace=JsonDocument.Parse(driver.Trace.CreateJson("fixture"));
            JsonElement[] rows=trace.RootElement.GetProperty("records").EnumerateArray().ToArray();
            JsonElement[] added=rows.Where(row => row.GetProperty("ordinal").GetUInt64()>recorded).ToArray();
            TestAssert.True(added.Length==2 &&
                added[0].GetProperty("phase").GetString()=="application_focus_out" &&
                added[1].GetProperty("phase").GetString()=="application_focus_in",
                "observer retains application-focus notifications separately from window-focus signals and ignores other notifications");
            TestAssert.True(added.All(row => row.GetProperty("reader_sample_sequence").GetUInt64()==sequence &&
                row.GetProperty("raw_mask").GetUInt64()==2UL && row.GetProperty("owned_mask").GetUInt64()==2UL &&
                row.GetProperty("focused").GetBoolean()==windowFocus &&
                row.GetProperty("event_id").GetUInt64()==0 && row.GetProperty("event_kind").ValueKind==JsonValueKind.Null) &&
                input.CurrentFrameSequence==sequence && Input.IsPhysicalKeyPressed(Key.D) &&
                driver.KeyDownCount==keyDowns && player.GetWindow().HasFocus()==windowFocus &&
                !observer.IsProcessing() && !observer.IsPhysicsProcessing(),
                "synthetic observer-only notifications passively retain raw state without sampling, replay, focus change or process loops");
            driver.ObserveFrame();
            driver.Dispose();
            string frozen=driver.Trace.CreateJson("fixture");
            observer.Notification(checked((int)Node.NotificationApplicationFocusOut));
            observer.Notification(checked((int)Node.NotificationApplicationFocusIn));
            TestAssert.Equal(frozen,driver.Trace.CreateJson("fixture"),
                "stopped observer cannot append application notifications after original disposal freeze");
            TestAssert.True(!Input.IsPhysicalKeyPressed(Key.D) && KeyboardRouteInputDriver.Current==null,
                "application-notification fixture releases its actual owned key before the next fixture");
        }
        finally { tree.Paused=false; driver?.Dispose(); root.QueueFree(); await Frames(tree,3); }
        GD.Print("KEYBOARD_APPLICATION_FOCUS_TRACE_OK synthetic_observer_notification_only=true os_focus_proven=false");
    }
    private static async Task VerifyLegacyTrace(SceneTree tree)
    {
        // Each deliberately rejected source owns a separate trace/lifetime. Never
        // reset a frozen trace to continue the ordinary keyboard runtime fixture.
        InputEvent[] sources = [
            new InputEventMouseButton { ButtonIndex=MouseButton.Left,Pressed=true },
            new InputEventAction { Action=InputActions.PrimaryAction,Pressed=true },
            new InputEventJoypadButton { ButtonIndex=JoyButton.RightShoulder,Pressed=true }
        ];
        string[] kinds=["mouse_button","action","joypad_button"];
        for(int index=0;index<sources.Length;index++)
        {
            // A fresh real player isolates attack timers, energy and movement as
            // well as the trace; do not carry deliberate legacy attacks onward.
            var root=new Node2D { Name="LegacyTraceFixture" };
            var floor=new StaticBody2D { Position=new Vector2(0,60),CollisionLayer=CombatCollisionLayers.World };
            floor.AddChild(new CollisionShape2D { Shape=new RectangleShape2D { Size=new Vector2(800,20) } });
            root.AddChild(floor);
            var player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            player.Position=new Vector2(0,14); root.AddChild(player); tree.Root.AddChild(root);
            KeyboardRouteInputDriver driver=null;
            InputEvent source=sources[index];
            try
            {
                await Frames(tree,8);
                driver=KeyboardRouteInputDriver.Begin(player);
                var input=player.GetNode<PlayerInputReader>("Input");
                driver.Command(InputActions.MoveRight,true); await Frames(tree,3);
                Input.ParseInputEvent(source); Input.FlushBufferedEvents(); await Frames(tree,3);
                bool rejected=false;
                try { driver.ObserveFrame(); }
                catch(System.InvalidOperationException error)
                { rejected=error.Message.Contains("keyboard normal route cannot consume mouse or legacy mode/primary input"); }
                TestAssert.True(rejected && input.Current.PrimaryHeld && !input.Current.KeyboardActions,
                    "original route guard rejects actual non-keyboard primary without replay or suppression");
                string frozen=driver.Trace.CreateJson("fixture");
                using JsonDocument trace=JsonDocument.Parse(frozen);
                JsonElement data=trace.RootElement;
                if(data.GetProperty("freeze_reason").GetString()!="legacy_input_consumed")
                    GD.Print("KEYBOARD_LEGACY_TRACE_FIXTURE_FAILURE "+frozen);
                TestAssert.True(data.GetProperty("frozen").GetBoolean() &&
                    data.GetProperty("freeze_reason").GetString()=="legacy_input_consumed",
                    "first legacy guard freezes evidence before any cleanup can hide the source");
                JsonElement[] rows=data.GetProperty("records").EnumerateArray().ToArray();
                // Viewport delivery may transform/copy an event. The controlled
                // fixture has exactly one pressed event of this kind; its ID is
                // retained, never required to equal the original native object.
                JsonElement[] receipts=rows.Where(row => row.GetProperty("phase").GetString()=="received" &&
                    row.GetProperty("event_kind").GetString()==kinds[index] && row.GetProperty("pressed").GetBoolean()).ToArray();
                if(receipts.Length!=1) GD.Print("KEYBOARD_LEGACY_RECEIPT_FIXTURE_FAILURE "+frozen);
                TestAssert.Equal(1,receipts.Length,"controlled non-keyboard press reaches passive receipt even if event identity changes");
                JsonElement received=receipts[0];
                TestAssert.True(received.GetProperty("event_kind").GetString()==kinds[index] &&
                    received.GetProperty("pressed").GetBoolean() &&
                    received.GetProperty("raw_primary_held").GetBoolean() &&
                    !received.GetProperty("known_injected_id_match").GetBoolean() &&
                    rows.Last().GetProperty("phase").GetString()=="legacy_guard" &&
                    rows.Last().GetProperty("sampled_primary_held").GetBoolean() &&
                    !rows.Last().GetProperty("sampled_keyboard_actions").GetBoolean(),
                    "passive receipt identifies non-keyboard event type and last production sample without claiming OS origin");
                if(index==0) TestAssert.True(received.GetProperty("event_code").GetInt32()==(int)MouseButton.Left &&
                    received.GetProperty("raw_mouse_left_down").GetBoolean(),"mouse receipt retains native button identity and state");
                if(index==1) TestAssert.Equal(InputActions.PrimaryAction.ToString(),received.GetProperty("event_action").GetString(),
                    "action receipt retains the actual action name");
                if(index==2) TestAssert.Equal((int)JoyButton.RightShoulder,received.GetProperty("event_code").GetInt32(),
                    "joypad receipt retains the actual button index");
                source.Set("pressed",false); Input.ParseInputEvent(source); Input.FlushBufferedEvents();
                driver.ReleaseAll(); await Frames(tree,3);
                TestAssert.True(!Input.IsActionPressed(InputActions.PrimaryAction) &&
                    KeyboardRouteInputDriver.TraceKeys.All(key => !Input.IsPhysicalKeyPressed(key)) &&
                    (index!=0 || !Input.IsMouseButtonPressed(MouseButton.Left)) &&
                    (index!=2 || !Input.IsJoyButtonPressed(source.Device,JoyButton.RightShoulder)),
                    "controlled legacy cleanup releases native primary, its button and every owned key before the next source");
                TestAssert.Equal(frozen,driver.Trace.CreateJson("fixture"),"legacy cleanup cannot rewrite the first-failure tail");
                driver.Command(InputActions.Attack,true); await Frames(tree,3);
                driver.Command(InputActions.Attack,false); await Frames(tree,3);
                driver.ObserveFrame();
            }
            finally
            {
                source.Set("pressed",false); Input.ParseInputEvent(source); Input.FlushBufferedEvents();
                driver?.Dispose(); root.QueueFree(); await Frames(tree,3);
            }
        }
        GD.Print("KEYBOARD_LEGACY_EVENT_TRACE_OK sources=mouse,action,joypad freeze_before_cleanup=true");
    }
    private static bool Is(JsonElement row,string phase,string key,bool pressed) =>
        row.GetProperty("phase").GetString()==phase && row.GetProperty("key").GetString()==key &&
        row.GetProperty("pressed").GetBoolean()==pressed;
    private static async Task Frames(SceneTree tree,int count)
    {
        for(int i=0;i<count;i++)
        { await tree.ToSignal(tree,SceneTree.SignalName.PhysicsFrame); await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame); }
    }
}
