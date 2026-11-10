using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Knight.Abilities;
using Knight.Audio;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;
using Knight.Vfx;
using Knight.Settings;
using Knight.Config;

namespace Knight.Tests.Integration;

// Saved formal Player and automatic physics/native InputMap keys, never OS keys.
// Deliberately suspending only rendering proves the journal isn't a render edge.
public static class ThrusterPresentationRuntimeTests
{
    private readonly record struct NaturalCarrierSample(PlayerPresentationFrame Frame,Vector2 Root,
        Transform2D VisualTransform,Vector2 NearAnkle,Vector2 NearSole,Vector2 FarAnkle,Vector2 FarSole,
        float NearRotation,float FarRotation,float BodyAngle,Vector2 BodyPosition,float GripGap,bool Rigid);

    public static async Task RunNaturalAirCarrierHandoff(SceneTree tree)
    {
        int cases=0;
        foreach(int facing in new[]{1,-1})
        {
            using var fixture=new Fixture(tree,false);await fixture.Start();
            var coordinator=fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            var formal=(PlayerFormalVisual)coordinator.ActiveVisual;
            var samples=new List<NaturalCarrierSample>();
            void Published(PlayerPresentationFrame frame)
            {
                var near=formal.GetNode<PlayerFormalLayer>("NearLeg");var far=formal.GetNode<PlayerFormalLayer>("FarLeg");
                var nearArm=formal.GetNode<PlayerFormalLayer>("NearArm");var farArm=formal.GetNode<PlayerFormalLayer>("FarArm");
                var weapon=formal.GetNode<PlayerFormalLayer>("Weapon");
                Vector2 Grip(float x)=>weapon.ToGlobal(weapon.WeaponDrawRect.Position+
                    (new Vector2(x,798)-weapon.WeaponSourceRect.Position)*(25f/287f));
                float gap=Mathf.Max(Grip(835).DistanceTo(nearArm.ToGlobal(nearArm.ArmWristLocal)),
                    Grip(548).DistanceTo(farArm.ToGlobal(farArm.ArmWristLocal)));
                bool rigid=true;
                foreach(var leg in new[]{near,far})rigid&=!leg.LegReachClamped && leg.Scale==Vector2.One &&
                    Mathf.Abs(leg.LegHipLocal.DistanceTo(leg.LegKneeLocal)-18)<.003f &&
                    Mathf.Abs(leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal)-18)<.003f;
                samples.Add(new(frame,fixture.Player.GlobalPosition,formal.GlobalTransform,
                    near.ToGlobal(near.LegAnkleLocal),near.ToGlobal(near.LegSoleLocal),
                    far.ToGlobal(far.LegAnkleLocal),far.ToGlobal(far.LegSoleLocal),near.Rotation,far.Rotation,
                    formal.CurrentPose.Body.RotationRadians,formal.CurrentPose.Body.Position,gap,rigid));
            }
            coordinator.FramePublished+=Published;
            try
            {
                SendKey(facing<0?Key.A:Key.D,true);SendKey(Key.Shift,true);
                await fixture.Frames(1);SendKey(Key.Shift,false);SendKey(facing<0?Key.A:Key.D,false);
                int boundary=-1;
                for(int i=0;i<100 && boundary<0;i++)
                {
                    await fixture.Frames(1);
                    for(int j=1;j<samples.Count;j++)
                        if(samples[j-1].Frame.AirJet.Active && samples[j-1].Frame.Mobility.Coasting &&
                            !samples[j].Frame.AirJet.Active && samples[j].Frame.OrdinaryAir.Active){boundary=j;break;}
                }
                TestAssert.True(boundary>0,"AIR_CARRIER_NATURAL_RED: real airborne Shift must naturally end coast and enter ordinary air");
                var old=samples[boundary-1];var next=samples[boundary];
                TestAssert.True(old.Frame.Locomotion.State==PlayerLocomotionState.Fall && next.Frame.Locomotion.State==PlayerLocomotionState.Fall &&
                    old.Frame.PresentationTimeSeconds<next.Frame.PresentationTimeSeconds && !fixture.Player.IsOnFloor() &&
                    fixture.Motor.AcceptedDashCount==1 && fixture.Motor.AcceptedJumpCount==0 &&
                    !fixture.Motor.AirDashAvailable && old.Frame.Mobility.Identity.OwnerId!=0,
                    "natural carrier proof needs same Fall, positive time and one actual Shift without jump/refund");
                TestAssert.True(Mathf.Abs(old.Frame.AirJet.TorsoLeanRadians)>.000001f && old.Rigid && next.Rigid &&
                    old.GripGap<.003f && next.GripGap<.003f && coordinator.FrameObserverFailureCount==0,
                    "natural nonzero carrier observes real rigid limbs and both weapon grips without hidden callback faults");
                // Use the actually displayed common angle, not the raw ordinary
                // target that the finite visual bridge has not yet adopted.
                TestAssert.Near(old.NearRotation-old.BodyAngle,
                    next.NearRotation-next.BodyAngle,.002f,"first natural ordinary frame retains old near-leg template");
                TestAssert.Near(old.FarRotation-old.BodyAngle,
                    next.FarRotation-next.BodyAngle,.002f,"first natural ordinary frame retains old far-leg template");
                TestAssert.Near(old.Frame.AirJet.TorsoLeanRadians,old.BodyAngle,.002f,"old Fall body isolates AirJet postmix");
                TestAssert.Near(old.BodyAngle,next.BodyAngle,.000001f,"AIR_LOAD_NATURAL_RED: first ordinary sample preserves displayed nonzero common load");
                foreach(var pair in new[]{(Old:old.NearSole,Next:next.NearSole),(Old:old.FarSole,Next:next.FarSole),
                    (Old:old.NearAnkle,Next:next.NearAnkle),(Old:old.FarAnkle,Next:next.FarAnkle)})
                    TestAssert.True((old.VisualTransform.AffineInverse()*pair.Old).DistanceTo(next.VisualTransform.AffineInverse()*pair.Next)<.003f,
                        "AIR_LOAD_NATURAL_RED: body root actually moves, while the first load/foot handoff must not jump inside that transform");
                await fixture.Frames(20);var settled=samples[^1];
                TestAssert.True(settled.Frame.OrdinaryAir.Active && !settled.Frame.AirJet.Active && settled.Rigid && settled.GripGap<.003f,
                    "natural handoff continues into real ordinary flight rather than a permanent frozen carrier");
                TestAssert.Near(settled.Frame.OrdinaryAir.TorsoLeanRadians,settled.BodyAngle,.00005f,"natural finite handoff reaches live ordinary target");
                TestAssert.True(fixture.Motor.AcceptedDashCount==1 && fixture.Motor.AcceptedJumpCount==0 && coordinator.FrameObserverFailureCount==0,
                    "settling the visual carrier cannot accept new movement actions or hide callback errors");
                GD.Print($"AIR_CARRIER_NATURAL_OBS facing={facing} samples={samples.Count} old_time={old.Frame.PresentationTimeSeconds} new_time={next.Frame.PresentationTimeSeconds} " +
                    $"old_jet_angle={old.Frame.AirJet.TorsoLeanRadians} old_jet_lag={old.Frame.AirJet.PelvisLag} new_ordinary_angle={next.Frame.OrdinaryAir.TorsoLeanRadians} " +
                    $"old_near_world={old.NearSole} new_near_world={next.NearSole} old_far_world={old.FarSole} new_far_world={next.FarSole} " +
                    $"root_step={next.Root-old.Root} near_pose_step={(next.NearSole-next.Root)-(old.NearSole-old.Root)} far_pose_step={(next.FarSole-next.Root)-(old.FarSole-old.Root)} " +
                    $"body_pose_step={next.BodyPosition-old.BodyPosition} old_visual={old.VisualTransform} new_visual={next.VisualTransform} grip_gap={next.GripGap}");
                cases++;
            }
            finally{coordinator.FramePublished-=Published;Release();}
        }
        GD.Print($"AIR_CARRIER_NATURAL_OK cases={cases} actual_saved_player=true internal_keys=true same_fall=true positive_time=true shifts=1 jumps=0 rigid=true grips=true template_held=true nonzero_handoff_geometry=true finite_settle=true human=false normal_speed=false");
    }

    private readonly record struct AirActionSample(PlayerPresentationFrame Frame, Vector2 NearHip, Vector2 FarHip,
        float PrimaryGap, float SecondaryGap, float FreeGap, bool Contact,
        Vector2 NearWrist, Vector2 NearElbow, bool RigidArms);

    public static async Task RunNaturalAirActionHandoff(SceneTree tree)
    {
        int cases = 0;
        int shapedArms = 0;
        foreach (int facing in new[] { 1, -1 })
        {
            using var fixture = new Fixture(tree, false);
            await fixture.Start();
            var coordinator = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            var combat = fixture.Player.GetNode<PlayerCombat>("Combat");
            var shield = fixture.Player.GetNode<PlayerShieldController>("Shield");
            var formal = (PlayerFormalVisual)coordinator.ActiveVisual;
            var nearLeg = formal.GetNode<PlayerFormalLayer>("NearLeg");
            var farLeg = formal.GetNode<PlayerFormalLayer>("FarLeg");
            var near = formal.GetNode<PlayerFormalLayer>("NearArm");
            var far = formal.GetNode<PlayerFormalLayer>("FarArm");
            var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
            var samples = new List<AirActionSample>();
            int attacks = 0, hits = 0, direction = -1;
            ulong attackId = 0;
            void Started(ulong id, int aim) { attacks++; attackId = id; direction = aim; }
            void Connected(Hurtbox _) => hits++;
            void Published(PlayerPresentationFrame frame)
            {
                Vector2 BitmapGrip(float x) => weapon.ToGlobal(weapon.WeaponDrawRect.Position +
                    (new Vector2(x, 798) - weapon.WeaponSourceRect.Position) * (25f / 287f));
                static bool Rigid(PlayerFormalLayer arm) => arm.Scale == Vector2.One &&
                    Mathf.Abs(arm.ArmElbowLocal.Length() - 24f) < .002f &&
                    Mathf.Abs(arm.ArmElbowLocal.DistanceTo(arm.ArmWristLocal) - 24f) < .002f;
                samples.Add(new(frame, nearLeg.Position, farLeg.Position,
                    BitmapGrip(835).DistanceTo(near.ToGlobal(near.ArmWristLocal)),
                    BitmapGrip(548).DistanceTo(far.ToGlobal(far.ArmWristLocal)),
                    far.ArmWristLocal.DistanceTo(new Vector2(0, 19)), weapon.WeaponSecondaryGripContact,
                    near.ArmWristLocal, near.ArmElbowLocal, Rigid(near) && Rigid(far)));
            }
            combat.AttackStarted += Started;
            combat.AttackConnected += Connected;
            coordinator.FramePublished += Published;
            try
            {
                Key movement = facing < 0 ? Key.A : Key.D;
                SendKey(movement, true); SendKey(Key.Shift, true);
                await fixture.Frames(1);
                SendKey(Key.Shift, false); SendKey(movement, false);
                for (int i = 0; i < 80 && !(fixture.Motor.ThrusterPresentation.Coasting &&
                         fixture.Motor.ThrusterPresentation.CoastRemaining <= .22f); i++) await fixture.Frames(1);
                TestAssert.True(fixture.Motor.ThrusterPresentation.Coasting &&
                    fixture.Motor.ThrusterPresentation.CoastRemaining is > .10f and <= .22f,
                    "natural action fixture requires actual late coast before pressing J");
                int attackStart = samples.Count;
                Vector2 restingElbow = near.ArmElbowLocal;
                SendKey(Key.J, true); await fixture.Frames(1); SendKey(Key.J, false);
                TestAssert.True(attacks == 1 && attackId != 0 && direction == (int)AttackDirection.Horizontal,
                    "real J must accept exactly one horizontal attack");

                int boundary = -1;
                for (int i = 0; i < 40 && boundary < 0; i++)
                {
                    await fixture.Frames(1);
                    for (int j = 1; j < samples.Count; j++)
                        if (samples[j - 1].Frame.AirJet.Active && samples[j - 1].Frame.Mobility.Coasting &&
                            !samples[j].Frame.AirJet.Active && samples[j].Frame.OrdinaryAir.Active) { boundary = j; break; }
                }
                TestAssert.True(boundary > 0, "real attack must reach the natural coast expiry boundary");
                var before = samples[boundary - 1]; var after = samples[boundary];
                foreach (var side in new[] { before, after })
                    TestAssert.True(side.Frame.Action.State == PlayerActionState.AirAttack && side.Frame.Base.Facing == facing &&
                        side.Frame.Locomotion.State == PlayerLocomotionState.Fall && side.Contact && side.SecondaryGap < .003f,
                        "AirAttack retains both hands at the real same-Fall carrier boundary");
                TestAssert.True(before.Frame.PresentationTimeSeconds < after.Frame.PresentationTimeSeconds &&
                    Mathf.Abs(before.Frame.AirJet.TorsoLeanRadians) > .000001f &&
                    before.NearHip.DistanceTo(after.NearHip) < .003f && before.FarHip.DistanceTo(after.FarHip) < .003f,
                    "AIR_ACTION_NATURAL: nonzero carrier retains both actual hip attachments under live attack ownership");

                int guardStart = samples.Count;
                SendKey(Key.L, true);
                for (int i = 0; i < 60 && !samples.Skip(guardStart).Any(FreeShield); i++) await fixture.Frames(1);
                TestAssert.True(shield.IsActive && samples.Skip(guardStart).Any(FreeShield),
                    "real L and actual Shield pose release the offhand to the free endpoint");
                int releaseStart = samples.Count;
                SendKey(Key.L, false);
                for (int i = 0; i < 60 && !samples.Skip(releaseStart).Any(Regripped); i++) await fixture.Frames(1);
                TestAssert.True(!shield.IsActive && samples.Skip(releaseStart).Any(s =>
                        s.Frame.Action.State == PlayerActionState.Shield && s.Frame.Action.Phase == PlayerActionPhase.Recovery) &&
                    samples.Skip(releaseStart).Any(Regripped),
                    "actual L release publishes shield recovery, then None and renewed bitmap grip");
                TestAssert.True(samples.Skip(boundary - 1).All(s => s.PrimaryGap < .003f) &&
                    fixture.Motor.AcceptedDashCount == 1 && fixture.Motor.AcceptedJumpCount == 0 &&
                    !fixture.Motor.AirDashAvailable && !fixture.Player.IsOnFloor() && attacks == 1 && coordinator.FrameObserverFailureCount == 0,
                    "real compound actions keep primary bitmap grip without extra attacks, jumps, refunds or observer failures");
                GD.Print($"AIR_ACTION_NATURAL_OBS facing={facing} attack={attackId} before={before.Frame.Action.Phase} after={after.Frame.Action.Phase} " +
                    $"hip_step={Mathf.Max(before.NearHip.DistanceTo(after.NearHip), before.FarHip.DistanceTo(after.FarHip))} " +
                    $"max_primary_gap={samples.Skip(boundary - 1).Max(s => s.PrimaryGap)} samples={samples.Count}");

                var airAttack = samples.Skip(attackStart).Where(s => s.Frame.Action.State == PlayerActionState.AirAttack).ToArray();
                var phases = airAttack.Select(s => s.Frame.Action.Phase).ToArray();
                var orderedPhases = phases.Where((phase, i) => i == 0 || phase != phases[i - 1]).ToArray();
                bool ordered = orderedPhases.SequenceEqual(new[] { PlayerActionPhase.Anticipation,
                    PlayerActionPhase.Burst, PlayerActionPhase.Contact, PlayerActionPhase.Recovery });
                float minReach = airAttack.Select(s => s.NearWrist.Length()).DefaultIfEmpty(float.NaN).Min();
                float maxReach = airAttack.Select(s => s.NearWrist.Length()).DefaultIfEmpty(float.NaN).Max();
                float contraction = airAttack.Where(s => s.Frame.Action.Phase == PlayerActionPhase.Anticipation)
                    .Select(s => s.NearWrist.Length()).DefaultIfEmpty(float.NaN).Min();
                float extension = airAttack.Where(s => s.Frame.Action.Phase is PlayerActionPhase.Burst or PlayerActionPhase.Contact)
                    .Select(s => s.NearWrist.Length()).DefaultIfEmpty(float.NaN).Max();
                float elbowTravel = airAttack.Select(s => s.NearElbow.DistanceTo(restingElbow)).DefaultIfEmpty(float.NaN).Max();
                bool armsRigid = airAttack.Length > 0 && airAttack.All(s => s.RigidArms);
                bool grips = airAttack.Length > 0 && airAttack.All(s => s.Contact && s.PrimaryGap < .003f && s.SecondaryGap < .003f);
                bool horizontalWhiff = hits == 0 && airAttack.Length > 0 && airAttack.All(s => !s.Frame.Base.Grounded &&
                    s.Frame.Base.Facing == facing && s.Frame.Action.Aim == new Vector2(facing, 0));
                // Same geometry discriminant as the previously verified ground case; this is not a style/amplitude target.
                bool shaped = contraction < 20.75f && extension > 21.5f && elbowTravel > 1f;
                GD.Print(FormattableString.Invariant($"AIR_ATTACK_ARM_NATIVE_MEASURE facing={facing} attack={attackId} samples={airAttack.Length} phases={string.Join(",", orderedPhases)} min_reach={minReach} max_reach={maxReach} anticipation_min={contraction} active_max={extension} elbow_travel={elbowTravel} ordered={ordered} rigid={armsRigid} grips={grips} horizontal_whiff={horizontalWhiff} shaped={shaped}"));
                if (ordered && armsRigid && grips && horizontalWhiff && shaped) shapedArms++;
                cases++;
            }
            finally
            {
                coordinator.FramePublished -= Published;
                combat.AttackStarted -= Started;
                combat.AttackConnected -= Connected;
                Release();
            }
        }
        TestAssert.True(cases == 2 && shapedArms == 2,
            "AIR_ATTACK_ARM_NATIVE_RED: both real horizontal J attacks must contract then extend the shoulder-space wrist and move the elbow through ordered phases with rigid segments and both grips");
        GD.Print($"AIR_ACTION_NATURAL_OK cases={cases} actual_saved_player=true internal_keys=true air_attack_at_expiry=true later_shield_recovery=true regrip=true human=false normal_speed=false");

        static bool FreeShield(AirActionSample s) => s.Frame.Action.State == PlayerActionState.Shield && !s.Contact && s.FreeGap < .003f;
        static bool Regripped(AirActionSample s) => s.Frame.Action.State == PlayerActionState.None && s.Contact && s.SecondaryGap < .003f;
    }

    public static async Task RunNaturalActionReleaseReentry(SceneTree tree)
    {
        int cases = 0;
        foreach (int facing in new[] { 1, -1 })
        {
            using var fixture = new Fixture(tree, false);
            await fixture.Start();
            var coordinator = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            var combat = fixture.Player.GetNode<PlayerCombat>("Combat");
            var formal = (PlayerFormalVisual)coordinator.ActiveVisual;
            var nearArm = formal.GetNode<PlayerFormalLayer>("NearArm");
            var nearLeg = formal.GetNode<PlayerFormalLayer>("NearLeg");
            var farLeg = formal.GetNode<PlayerFormalLayer>("FarLeg");
            var samples = new List<(PlayerPresentationFrame Frame, float Arm, bool CombatActive)>();
            var starts = new List<(ulong Id, int Direction)>();
            void Published(PlayerPresentationFrame frame) => samples.Add((frame,
                nearArm.Rotation - (nearLeg.Position - farLeg.Position).Angle(), combat.IsAttacking));
            void Started(ulong id, int direction) => starts.Add((id, direction));
            coordinator.FramePublished += Published; combat.AttackStarted += Started;
            try
            {
                Key movement = facing < 0 ? Key.A : Key.D;
                SendKey(movement, true); await fixture.Frames(1); SendKey(movement, false);
                await fixture.Frames(14);
                TestAssert.True(samples[^1].Frame.Base.Facing == facing && samples[^1].Frame.OrdinaryAir.Active &&
                    samples[^1].Frame.Locomotion.State == PlayerLocomotionState.Fall, "reentry requires settled real ordinary Fall");
                float neutral = samples[^1].Arm;
                SendKey(Key.J, true); await fixture.Frames(1); SendKey(Key.J, false);
                for (int i = 0; i < 40 && !Released(); i++) await fixture.Frames(1);
                TestAssert.True(starts.Count == 1 && Released(), "first accepted J must naturally end before a second press");
                int secondPress = samples.Count;
                SendKey(Key.J, true); await fixture.Frames(1); SendKey(Key.J, false);
                await fixture.Frames(2);
                int entry = samples.FindIndex(secondPress, s => s.Frame.Action.State == PlayerActionState.AirAttack);
                TestAssert.True(starts.Count == 2 && starts[0].Id != 0 && starts[1].Id != 0 && starts[0].Id != starts[1].Id &&
                    starts.All(s => s.Direction == (int)AttackDirection.Horizontal) && entry > 0,
                    "two real J edges must start different accepted horizontal attacks");
                var before = samples[entry - 1]; var after = samples[entry];
                TestAssert.True(before.Frame.Action.State == PlayerActionState.None && !before.CombatActive && after.CombatActive &&
                    before.Frame.Locomotion.State == PlayerLocomotionState.Fall && after.Frame.Locomotion.State == PlayerLocomotionState.Fall &&
                    before.Frame.OrdinaryAir.Active && after.Frame.OrdinaryAir.Active &&
                    before.Frame.PresentationTimeSeconds <= after.Frame.PresentationTimeSeconds &&
                    after.Frame.Action.Phase == PlayerActionPhase.Anticipation && after.Frame.Action.NormalizedTime < .35f &&
                    neutral - before.Arm > .05f && before.Arm < -.085f,
                    $"fixture must catch early second windup while first release remains visible; facing={facing} neutral={neutral} old={before.Arm} phase={after.Frame.Action.Phase} progress={after.Frame.Action.NormalizedTime}");
                // The new anticipation bends farther in the same direction as the
                // departing arm. A step back toward neutral is not authored progress.
                TestAssert.True(after.Arm <= before.Arm + .002f,
                    $"ACTION_RELEASE_REENTRY_NATURAL_RED: legal second J reversed toward neutral; facing={facing} old={before.Arm} next={after.Arm} progress={after.Frame.Action.NormalizedTime}");
                TestAssert.True(fixture.Motor.AcceptedDashCount == 0 && fixture.Motor.AcceptedJumpCount == 0 &&
                    !fixture.Player.IsOnFloor() && coordinator.FrameObserverFailureCount == 0,
                    "natural reentry does not consume an extra movement action or hide an observer error");
                GD.Print($"ACTION_RELEASE_REENTRY_NATURAL_OBS facing={facing} attacks={starts[0].Id},{starts[1].Id} old={before.Arm} next={after.Arm} progress={after.Frame.Action.NormalizedTime}");
                cases++;
            }
            finally { coordinator.FramePublished -= Published; combat.AttackStarted -= Started; Release(); }

            bool Released() => samples.Any(s => s.Frame.Action.State == PlayerActionState.AirAttack &&
                s.Frame.Action.Phase == PlayerActionPhase.Recovery) && samples[^1].Frame.Action.State == PlayerActionState.None && !combat.IsAttacking;
        }
        GD.Print($"ACTION_RELEASE_REENTRY_NATURAL_OK cases={cases} saved_player=true internal_j=true two_real_ids=true no_jump_or_dash=true human=false");
    }

    public static async Task RunWorldJetLastLeaf(SceneTree tree)
    {
        // Exact original last leaf, including synchronous fixture disposal.
        // No predecessor scenarios, extra wait, drain or borrowed playback.
        await DelayedWorldJet(tree);
        GD.Print("THRUSTER_WORLD_JET_LAST_LEAF_DIAGNOSTIC original_leaf_delegate=true original_inputs_frames=true synchronous_root_free=true no_exit_drain=true diagnostic_only=true human=false");
    }

    public static async Task RunWorldJet(SceneTree tree)
    {
        TestAssert.True(typeof(PrologueVfxDirector).GetProperty("ActiveThrusterEffectCount") != null &&
            typeof(PrologueAudioDirector).GetProperty("ActiveThrusterIdentity") != null,
            "actual world jet has no owned sustained effect/identity voice consumer");
        foreach (bool left in new[] { false, true }) await WorldJet(tree, left);
        await LongWorldJetAndCancel(tree);
        await RejectedWorldJet(tree);
        await DelayedWorldJet(tree);
        GD.Print("THRUSTER_WORLD_JET_OK saved_player=true actual_nozzle_world_axis=true single_owned_effect_voice=true delayed_render_no_replay=true rebind_no_replay=true pause_clear_no_replay=true reentry_death=true cached_audio_unchanged=true headless_only=true human=false");
    }

    private static (PrologueVfxDirector Vfx, PrologueAudioDirector Audio) AddJetDirectors(Fixture fixture)
    {
        var vfx = new PrologueVfxDirector { Name = "Vfx" };
        vfx.AddChild(new Node2D { Name = "World" });
        fixture.Root.AddChild(vfx);
        var audio = PrologueAudioDirector.CreateRuntimeTree(); fixture.Root.AddChild(audio);
        return (vfx, audio);
    }

    private static async Task WorldJet(SceneTree tree, bool left)
    {
        using var fixture = new Fixture(tree, true);
        var (vfx, audio) = AddJetDirectors(fixture);
        await fixture.Start();
        var presentation = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var visual = (PlayerFormalVisual)presentation.ActiveVisual;
        var cached = GD.Load<AudioStreamWav>("res://assets/audio/prologue/sfx/player/dash.wav"); // Borrowed.
        var originalLoop = cached.LoopMode;
        int starts = ReadRigProperty<int>(audio, "ThrusterStartCount"), samples = 0;
        void Observe(PlayerPresentationFrame frame)
        {
            if (!Frame(fixture.Motor).Burning) return;
            samples++;
            TestAssert.Equal(1, ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount"), "one sustained world jet");
            TestAssert.Near(0f, ReadRigProperty<Vector2>(vfx, "ThrusterWorldPosition")
                .DistanceTo(visual.ThrusterNozzleWorldPosition), .001f, "same published pose owns actual world nozzle");
            TestAssert.Equal(-Frame(fixture.Motor).Direction, ReadRigProperty<Vector2>(vfx, "ThrusterWorldDirection"),
                "world exhaust is opposite locked direction, not facing multiplied twice");
        }
        presentation.FramePublished += Observe;
        try
        {
            SendKey(left ? Key.A : Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2);
            var identity = fixture.Motor.ThrusterState.ActiveIdentity;
            TestAssert.True(identity != default && samples > 0 && presentation.FrameObserverFailureCount == 0 &&
                ReadRigProperty<ThrusterIdentity>(audio, "ActiveThrusterIdentity") == identity &&
                ReadRigProperty<bool>(audio, "ThrusterVoicePlaying") &&
                ReadRigProperty<int>(audio, "ThrusterStartCount") == starts + 1,
                "actual burn owns a single native playing voice and same-frame jet");
            TestAssert.True(vfx.LastCue != PresentationCue.Dash && audio.LastCue != PrologueAudioCue.PlayerDash,
                "actual typed jet does not also replay the legacy Dash VFX/audio cue");
            presentation.BindVisualRoot(fixture.Player.GetNode("VisualRoot")); await fixture.Frames(1);
            TestAssert.Equal(starts + 1, ReadRigProperty<int>(audio, "ThrusterStartCount"), "visual rebind cannot replay start");
            vfx.Configure(new GameSettingsData { ReduceFlashes = true });
            TestAssert.True(ReadRigProperty<bool>(vfx, "ThrusterReducedFlash"), "live effect adopts reduce-flash immediately");
            tree.Paused = true;
            for (int frame = 0; frame < 2; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            TestAssert.True(ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0 &&
                !ReadRigProperty<bool>(audio, "ThrusterVoicePlaying") && Frame(fixture.Motor).Burning,
                "pause clears owned transient presentation, not physics burn budget");
            tree.Paused = false; await fixture.Frames(1);
            TestAssert.True(ReadRigProperty<int>(audio, "ThrusterStartCount") == starts + 1 &&
                !ReadRigProperty<bool>(audio, "ThrusterVoicePlaying") &&
                ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 1,
                "resume quietly follows live jet without replaying voice");
            audio.ClearStagedFrameAudio(); vfx.ClearTransientEffects(); await fixture.Frames(1);
            TestAssert.Equal(starts + 1, ReadRigProperty<int>(audio, "ThrusterStartCount"), "room-style clear retains start watermark");
            Release(); await fixture.Frames(15);
            TestAssert.True(ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0 &&
                !ReadRigProperty<bool>(audio, "ThrusterVoicePlaying"), "ended burn leaves no sustained object or voice");
            SendKey(left ? Key.A : Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
            TestAssert.Equal(starts + 2, ReadRigProperty<int>(audio, "ThrusterStartCount"), "new real burn gets exactly one new voice");
            fixture.Root.RemoveChild(fixture.Player); fixture.Root.AddChild(fixture.Player); await fixture.Frames(1);
            TestAssert.True(ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0 &&
                !ReadRigProperty<bool>(audio, "ThrusterVoicePlaying"), "same owner exit stops only its jet");
            var health = fixture.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero, 0f, 711UL,
                fixture.Player.GetInstanceId())), "reentered presentation dies through actual Health");
            await fixture.Frames(1);
            TestAssert.Equal(EntityVisualState.Dead, presentation.LastPlayerFrame.Base.State,
                "presentation death listeners restore on reentry without RequestReady");
            TestAssert.Equal(originalLoop, cached.LoopMode, "cached imported WAV was never modified");
            TestAssert.Equal(0, presentation.FrameObserverFailureCount, "all same-publish effect geometry assertions passed");
        }
        finally { presentation.FramePublished -= Observe; Release(); tree.Paused = false; }
    }

    private static async Task DelayedWorldJet(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true);
        var (vfx, audio) = AddJetDirectors(fixture); await fixture.Start();
        var presentation = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        presentation.SetProcess(false);
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(16); Release();
        TestAssert.True(Facts(fixture.Motor).Length == 2 && !Frame(fixture.Motor).Burning,
            "real start/end occur while render consumer is suspended");
        presentation.SetProcess(true); await fixture.Frames(2);
        TestAssert.True(ReadRigProperty<int>(audio, "ThrusterStartCount") == 0 &&
            ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0,
            "same-drain start/end must not replay a dead burst or sound");
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        TestAssert.Equal(1, ReadRigProperty<int>(audio, "ThrusterStartCount"), "cursor still consumes next fresh start");
    }

    private static async Task LongWorldJetAndCancel(SceneTree tree)
    {
        using var module = new MobilityModuleDefinition { ModuleId = "test.presentation.long-jet", BurnDuration = .65f };
        using var fixture = new Fixture(tree, true); fixture.Motor.InitialMobilityModule = module;
        var (vfx, audio) = AddJetDirectors(fixture); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(27);
        var identity = fixture.Motor.ThrusterState.ActiveIdentity;
        TestAssert.True(identity != default && Frame(fixture.Motor).Burning &&
            ReadRigProperty<bool>(audio, "ThrusterVoicePlaying") &&
            ReadRigProperty<int>(audio, "ThrusterStartCount") == 1 &&
            ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 1,
            "configured long burn sustains one native loop beyond original .2s WAV, never frame-replays Play");
        SendKey(Key.D, false); SendKey(Key.Shift, false); SendKey(Key.A, true); await fixture.Frames(1); Release();
        // ProcessFrame is emitted before node _Process. Wait for the actual
        // published non-burning frame, not a new physics fact paired with old audio.
        var presentation = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        for (int frame = 0; frame < 6 && presentation.LastPlayerFrame.Mobility.Burning; frame++)
            await fixture.Frames(1);
        GD.Print($"THRUSTER_CANCEL_DIAGNOSTIC motor={Frame(fixture.Motor)} end={Facts(fixture.Motor).Last()} " +
            $"published={fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation").LastPlayerFrame.Mobility} " +
            $"audio_identity={ReadRigProperty<ThrusterIdentity>(audio, "ActiveThrusterIdentity")} " +
            $"ends={ReadRigProperty<int>(audio, "ThrusterEndCount")} tail={ReadRigProperty<bool>(audio, "ThrusterTailPlaying")} " +
            $"vfx={ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount")}");
        TestAssert.True(!Frame(fixture.Motor).Burning &&
            Facts(fixture.Motor).Last().Identity == identity &&
            Facts(fixture.Motor).Last().Reason == ThrusterEndReason.ReverseCancelled &&
            ReadRigProperty<int>(audio, "ThrusterEndCount") == 1 &&
            ReadRigProperty<bool>(audio, "ThrusterTailPlaying") &&
            ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0,
            "actual reverse cancel ends same sound identity once and starts only its bounded cancellation tail");
        await fixture.Frames(20);
        TestAssert.True(!ReadRigProperty<bool>(audio, "ThrusterTailPlaying") &&
            ReadRigProperty<int>(audio, "ThrusterStartCount") == 1 &&
            ReadRigProperty<int>(audio, "ThrusterEndCount") == 1, "tail naturally finishes without replay or cleanup drain");
        GD.Print("THRUSTER_WORLD_JET_LONG_CANCEL_OK actual_long_module=true original_wav_duration_exceeded=true one_play=true same_identity_tail=true naturally_finished=true");
    }

    private static async Task RejectedWorldJet(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true);
        var (vfx, audio) = AddJetDirectors(fixture); await fixture.Start();
        SendKey(Key.L, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        TestAssert.True(Facts(fixture.Motor).Length == 0 &&
            ReadRigProperty<int>(audio, "ThrusterStartCount") == 0 &&
            ReadRigProperty<int>(vfx, "ActiveThrusterEffectCount") == 0,
            "rejected guarded Shift creates neither successful jet nor successful voice");
        SendKey(Key.D, true); await fixture.Frames(12); SendKey(Key.D, false); SendKey(Key.A, true);
        await fixture.Frames(2); Release();
        TestAssert.True(Frame(fixture.Motor).Braking && Facts(fixture.Motor).Length == 0 &&
            ReadRigProperty<int>(audio, "ThrusterStartCount") == 0,
            "ordinary reverse brake has no successful jet sound identity");
        GD.Print("THRUSTER_WORLD_JET_REJECTION_OK guarded_shift=true ordinary_brake=true no_success_presentation=true");
    }

    public static async Task RunRigCompatibility(SceneTree tree)
    {
        await PlayerShieldRuntimeTests.RunGuardImpact(tree);
        Knight.Tests.Unit.PlayerPresentationTests.Run();
        Knight.Tests.Unit.SecondaryMotionModelTests.Run();
        Knight.Tests.Unit.PlayerPoseMixerTests.Run();
        Knight.Tests.Unit.PlayerAnimationMapTests.Run();
        PlayerCombatPoseTests.Run();
        // Original named delegates, not copied assertions. This explicit leaf
        // subset does not claim the full level/recovery/legacy case registry.
        var names = new HashSet<string>(StringComparer.Ordinal)
        {
            "SetupAndBasicContract", "TypedActionPhaseBoundaryBlendsWithoutHardCut",
            "AirAndDownAttacksExposeFourDistinctRigStages", "ThreadTerminalPoseReturnsContinuouslyToIdle",
            "ThreadAimUsesFacingLocalMirroredRig", "ActionOverRunKeepsLegsAndSecondarySlotsContinuous",
            "LoopWrapKeepsEveryLocomotionSlotContinuous", "TaskOneSecondaryOutputsReachTheirOwnedFormalSlots",
            "CompatibilityAdaptersRemainVisible", "SixtyFpsFramesKeepEverySlotBounded",
            "FacingLocalSecondaryMotionMirrorsWorldExactly"
        };
        Task7Case[] cases = PlayerFormalVisualTests.Task7Cases(tree).Where(item =>
            names.Contains(item.Name.Substring("PlayerFormalVisualTests.".Length))).ToArray();
        TestAssert.Equal(11, cases.Length, "all eleven explicitly selected original formal leaf cases exist");
        Task7CaseRun run = await Task7CaseRegistry.Execute(cases);
        TestAssert.Equal(11, run.Completed.Count, "every original formal leaf delegate completed");
        await PlayerMantleMotionTests.Run(tree);
        await PlayerFormalVisualTests.RunGroundUpAttack(tree);
        await PlayerFormalVisualTests.RunHurtAttackClock(tree);
        await PlayerFormalVisualTests.RunClawAttackClock(tree);
        await PlayerFormalVisualTests.RunAirUpAttack(tree);
        GD.Print("THRUSTER_RIG_COMPATIBILITY_OK existing_timeline_pose_map=true formal_leaf_cases=11 mantle_cases=2 original_case_delegates=true headless_only=true full_legacy_suite=false human=false");
    }

    public static async Task RunRig(SceneTree tree)
    {
        await RigGroundAttackSecondary(tree);
        TestAssert.True(typeof(PlayerPresentationFrame).GetProperty("Mobility") != null &&
            typeof(PlayerPresentationFrame).GetProperty("MobilityLeanRadians") != null &&
            typeof(PlayerFormalVisual).GetProperty("ThrusterNozzleWorldPosition") != null,
            "single typed rig has no mobility observation/lean/nozzle consumer");
        await PlayerFormalVisualTests.RunDirectionalAirJetOnly(tree);
        await RigAirDirections(tree);
        await RigJetAndActions(tree, false);
        await RigJetAndActions(tree, true);
        await RigOrdinaryBrake(tree);
        await RigRejectedGuard(tree);
        await RigDeath(tree);
        GD.Print("THRUSTER_PRESENTATION_RIG_OK saved_player=true single_typed_consumer=true burn_coast_brake=true ordinary_brake_empty_identity=true action_death_priority=true pause_frozen=true rigid_no_stretch=true actual_nozzle=true headless_transforms_only=true human=false");
    }

    private static async Task RigGroundAttackSecondary(SceneTree tree)
    {
        var weightFaults = new List<string>();
        foreach (bool left in new[] { false, true })
        {
            using var fixture = new Fixture(tree, true); await fixture.Start();
            if (left) { SendKey(Key.A, true); await fixture.Frames(2); Release(); }
            await fixture.Frames(180);
            var player = fixture.Player;
            var combat = player.GetNode<PlayerCombat>("Combat");
            var presentation = player.GetNode<PlayerPresentationCoordinator>("Presentation");
            var visual = (PlayerFormalVisual)presentation.ActiveVisual;
            var health = player.GetNode<HealthComponent>("Health");
            Vector2 origin = player.GlobalPosition;
            int hp = health.CurrentHealth, starts = 0, hits = 0;
            ulong attackId = 0;
            var phases = new HashSet<PlayerActionPhase>();
            var moving = new HashSet<PlayerActionPhase>();
            int samples = 0;
            bool sameFormal = true, supported = true;
            var near = visual.GetNode<PlayerFormalLayer>("NearArm");
            var far = visual.GetNode<PlayerFormalLayer>("FarArm");
            var weapon = visual.GetNode<PlayerFormalLayer>("Weapon");
            var nearLeg = visual.GetNode<PlayerFormalLayer>("NearLeg");
            var farLeg = visual.GetNode<PlayerFormalLayer>("FarLeg");
            Vector2 RestHip() => visual.ToLocal(nearLeg.ToGlobal(nearLeg.LegHipLocal));
            Vector2 restHip = RestHip();
            Vector2 authoredBodyRest = visual.GetNode<Node2D>("Body").Position - visual.CurrentPose.Body.Position;
            float hipMinX = 0, hipMaxX = 0, hipMaxY = 0, soleGap = 0, bodyHipGap = 0;
            int measuredSupport = 0;
            bool rigidLegs = true, weightSameFrame = true;
            double firstTime = double.PositiveInfinity, lastTime = double.NegativeInfinity;
            ulong firstPhysics = ulong.MaxValue, lastPhysics = 0;
            var weightPhases = new HashSet<PlayerActionPhase>();
            var phaseRanges = new Dictionary<PlayerActionPhase, (float MinX, float MaxX, float MaxY, int Samples)>();
            Vector2 restElbow = near.ArmElbowLocal;
            float minReach = float.PositiveInfinity, maxReach = 0f, elbowTravel = 0f, gripGap = 0f;
            bool rigidArms = true;
            void Started(ulong id, int direction) { starts++; attackId = id; }
            void Connected(Hurtbox target) => hits++;
            void Published(PlayerPresentationFrame frame)
            {
                if (frame.Action.State != PlayerActionState.GroundAttack) return;
                samples++; phases.Add(frame.Action.Phase);
                if (frame.Secondary.Lantern.Position.Length() > .001f &&
                    frame.Secondary.Tether.Position.Length() > .001f) moving.Add(frame.Action.Phase);
                sameFormal &= frame.Secondary == visual.CurrentPlayerFrame.Secondary;
                float reach = near.ArmWristLocal.Length();
                minReach = Mathf.Min(minReach, reach); maxReach = Mathf.Max(maxReach, reach);
                elbowTravel = Mathf.Max(elbowTravel, near.ArmElbowLocal.DistanceTo(restElbow));
                const float artScale = 25f / 287f;
                Vector2 primary = weapon.ToGlobal(weapon.WeaponDrawRect.Position +
                    (new Vector2(835, 798) - weapon.WeaponSourceRect.Position) * artScale);
                Vector2 secondary = weapon.ToGlobal(weapon.WeaponDrawRect.Position +
                    (new Vector2(548, 798) - weapon.WeaponSourceRect.Position) * artScale);
                gripGap = Mathf.Max(gripGap, Mathf.Max(primary.DistanceTo(near.ToGlobal(near.ArmWristLocal)),
                    secondary.DistanceTo(far.ToGlobal(far.ArmWristLocal))));
                rigidArms &= Mathf.Abs(near.ArmElbowLocal.Length() - 24f) < .002f &&
                    Mathf.Abs(near.ArmElbowLocal.DistanceTo(near.ArmWristLocal) - 24f) < .002f &&
                    Mathf.Abs(far.ArmElbowLocal.Length() - 24f) < .002f &&
                    Mathf.Abs(far.ArmElbowLocal.DistanceTo(far.ArmWristLocal) - 24f) < .002f &&
                    near.Scale == Vector2.One && far.Scale == Vector2.One && weapon.Scale == Vector2.One;
                supported &= frame.Base.Facing == (left ? -1 : 1) &&
                    player.IsOnFloor() && player.GlobalPosition.DistanceTo(origin) < .01f &&
                    player.Velocity.LengthSquared() < .01f && frame.Secondary.ShoulderSpool == PoseTransform2D.Identity;
                // LegHipLocal is the actual Draw joint at the attachment origin: Body's
                // pre-existing phase rotation cannot impersonate this translation.
                Vector2 hip = RestHip() - restHip;
                hipMinX = Mathf.Min(hipMinX, hip.X); hipMaxX = Mathf.Max(hipMaxX, hip.X);
                hipMaxY = Mathf.Max(hipMaxY, hip.Y);
                // GroundAttack owns Body at weight 1 in every phase; its current
                // action position includes the transition from the authored Idle bob.
                // Subtract that baseline, not a pre-J snapshot of the bobbing Body.
                bodyHipGap = Mathf.Max(bodyHipGap,
                    (visual.GetNode<Node2D>("Body").Position - authoredBodyRest -
                        visual.CurrentActionPose.Body.Position).DistanceTo(hip));
                weightSameFrame &= visual.CurrentPlayerFrame == frame && ReferenceEquals(presentation.ActiveVisual, visual);
                if (frame.PresentationTimeSeconds is double time && double.IsFinite(time))
                { firstTime = Math.Min(firstTime, time); lastTime = Math.Max(lastTime, time); }
                firstPhysics = Math.Min(firstPhysics, Engine.GetPhysicsFrames());
                lastPhysics = Math.Max(lastPhysics, Engine.GetPhysicsFrames());
                if (frame.Gait.Observed && frame.Gait.Weight >= .999f)
                {
                    measuredSupport++; weightPhases.Add(frame.Action.Phase);
                    var range = phaseRanges.TryGetValue(frame.Action.Phase, out var oldRange)
                        ? oldRange : (hip.X, hip.X, hip.Y, 0);
                    phaseRanges[frame.Action.Phase] = (Mathf.Min(range.Item1, hip.X),
                        Mathf.Max(range.Item2, hip.X), Mathf.Max(range.Item3, hip.Y), range.Item4 + 1);
                    foreach (var item in new[] { (nearLeg, frame.Gait.NearSoleWorld), (farLeg, frame.Gait.FarSoleWorld) })
                    {
                        var leg = item.Item1;
                        soleGap = Mathf.Max(soleGap, leg.ToGlobal(leg.LegSoleLocal).DistanceTo(item.Item2));
                        rigidLegs &= Mathf.Abs(leg.LegHipLocal.DistanceTo(leg.LegKneeLocal) - 18) < .002f &&
                            Mathf.Abs(leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal) - 18) < .002f &&
                            !leg.LegReachClamped && leg.Scale == Vector2.One;
                    }
                }
            }
            TestAssert.True(player.IsOnFloor() && player.Velocity.LengthSquared() < .01f &&
                presentation.LastPlayerFrame.Secondary.Lantern.Position.Length() < .001f,
                "saved player settles on real support before the independent stationary whiff");
            combat.AttackStarted += Started; combat.AttackConnected += Connected;
            presentation.FramePublished += Published;
            try
            {
                SendKey(Key.J, true); await fixture.Frames(1); SendKey(Key.J, false);
                await fixture.Frames(100);
                foreach (var phase in new[] { PlayerActionPhase.Anticipation, PlayerActionPhase.Burst,
                    PlayerActionPhase.Contact, PlayerActionPhase.Recovery })
                    TestAssert.True(phases.Contains(phase) && moving.Contains(phase),
                        $"GROUND_ATTACK_SECONDARY_NATIVE_RED: actual stationary J drives lantern/tether during {phase}, left={left}");
                GD.Print($"GROUND_ATTACK_ARM_NATIVE_MEASURE left={left} min_reach={minReach} max_reach={maxReach} " +
                    $"elbow_travel={elbowTravel} grip_gap={gripGap} rigid={rigidArms}");
                TestAssert.True(minReach < 20.75f && maxReach > 21.5f && elbowTravel > 1f,
                    "GROUND_ATTACK_ARM_NATIVE_RED: real stationary J must contract then extend the actual near-arm chain, not rotate a fixed elbow/wrist shape");
                TestAssert.True(gripGap < .001f && rigidArms,
                    "actual active elbow/wrist motion preserves both bitmap grip contacts and fixed unscaled arm links");
                await fixture.Frames(180);
                var settled = presentation.LastPlayerFrame;
                TestAssert.True(starts == 1 && attackId != 0 && hits == 0 && samples > 4 &&
                    sameFormal && supported &&
                    !combat.IsAttacking && settled.Action == PlayerActionFrame.None &&
                    settled.Secondary.Lantern.Position.Length() < .001f &&
                    settled.Secondary.Tether.Position.Length() < .001f &&
                    near.ArmWristLocal.DistanceTo(new Vector2(0, 21)) < .001f &&
                    player.GlobalPosition.DistanceTo(origin) < .01f && health.CurrentHealth == hp &&
                    fixture.Motor.AcceptedJumpCount == 0 && fixture.Motor.AcceptedDashCount == 0,
                    "one real whiff returns attachments to rest without moving, hitting, jumping or spending another action");
                GD.Print($"GROUND_ATTACK_SECONDARY_NATIVE_OK left={left} phases=4 same_formal_frame=true " +
                    $"samples={samples} accepted_attacks=1 hits=0 support=true settled=true human=false");
                foreach (var item in phaseRanges)
                    GD.Print($"GROUND_ATTACK_WEIGHT_NATIVE_PHASE left={left} phase={item.Key} min_x={item.Value.MinX} max_x={item.Value.MaxX} max_y={item.Value.MaxY} samples={item.Value.Samples}");
                GD.Print($"GROUND_ATTACK_WEIGHT_NATIVE_MEASURE left={left} min_x={hipMinX} max_x={hipMaxX} max_y={hipMaxY} " +
                    $"supported={measuredSupport}/{samples} phases={weightPhases.Count} sole_gap={soleGap} body_hip_gap={bodyHipGap} " +
                    $"physics={firstPhysics}..{lastPhysics} presentation={firstTime}..{lastTime} same_frame={weightSameFrame} rigid_legs={rigidLegs}");
                // Defer only the new failure until BOTH original J observers and their
                // original settle/cleanup assertions have run; do not hide old failures.
                if (!(hipMinX < -.25f && hipMaxX > .25f && hipMaxY > .5f && hipMaxY <= 2.2f &&
                    measuredSupport == samples && weightPhases.Count == 4 && soleGap < .01f &&
                    bodyHipGap < .002f && rigidLegs && weightSameFrame &&
                    lastPhysics > firstPhysics && lastTime > firstTime && RestHip().DistanceTo(restHip) < .002f))
                    weightFaults.Add($"left={left} hip_x={hipMinX}..{hipMaxX} hip_y={hipMaxY} support={measuredSupport}/{samples} " +
                        $"phases={weightPhases.Count} sole_gap={soleGap} body_hip_gap={bodyHipGap} same_frame={weightSameFrame} rigid_legs={rigidLegs}");
            }
            finally
            {
                Release(); presentation.FramePublished -= Published;
                combat.AttackStarted -= Started; combat.AttackConnected -= Connected;
            }
        }
        TestAssert.True(weightFaults.Count == 0, "GROUND_ATTACK_WEIGHT_NATIVE_RED real saved Player J lacks supported four-phase hip weight transfer: " + string.Join("; ", weightFaults));
        GD.Print("GROUND_ATTACK_WEIGHT_NATIVE_OK facings=2 phases=4 actual_J=true actual_motor=true actual_soles=true fixed_links=true old_grips_retained=true same_frame=true developer_sample=true human=false");
    }

    private static async Task RigAirDirections(SceneTree tree)
    {
        var directions=new (Key[] Keys,Vector2 Direction)[]
        {
            (new[]{Key.W},Vector2.Up),(new[]{Key.S},Vector2.Down),
            (new[]{Key.A},Vector2.Left),(new[]{Key.D},Vector2.Right),
            (new[]{Key.W,Key.A},new Vector2(-1,-1).Normalized()),
            (new[]{Key.W,Key.D},new Vector2(1,-1).Normalized()),
            (new[]{Key.S,Key.A},new Vector2(-1,1).Normalized()),
            (new[]{Key.S,Key.D},new Vector2(1,1).Normalized())
        };
        int completed=0;
        foreach(var item in directions)
        {
            using var fixture=new Fixture(tree,false); await fixture.Start();
            using var observer=new RigObserver(fixture);
            foreach(Key key in item.Keys) SendKey(key,true);
            SendKey(Key.Shift,true);
            RigSample first=await observer.Wait(s=>s.Facts.Burning &&
                s.Facts.Direction.DistanceTo(item.Direction)<.001f,"real air thrust "+item.Direction);
            ThrusterIdentity identity=first.Facts.Identity;
            SendKey(Key.Shift,false);
            RigSample next=await observer.Wait(s=>s.Facts.Burning && s.Facts.Identity==identity &&
                s.Facts.Direction.DistanceTo(item.Direction)<.001f,"same-identity fresh air sample "+item.Direction,3);
            TestAssert.True(identity!=default && !fixture.Player.IsOnFloor() &&
                fixture.Motor.AcceptedDashCount==1 && fixture.Motor.AcceptedJumpCount==0 &&
                !fixture.Motor.AirDashAvailable,
                "AIR_JET_FIXTURE_RED: eight directions must use one real accepted airborne Shift, never a jump/refund");
            GD.Print($"AIR_JET_ACTUAL_SOURCE index={completed} direction={next.Facts.Direction} identity={identity} " +
                $"velocity={next.Velocity} fresh=true dash_count={fixture.Motor.AcceptedDashCount} jumps={fixture.Motor.AcceptedJumpCount}");
            var pose=next.Frame.AirJet;
            TestAssert.True(pose.Active && pose.ForceObserved,
                "AIR_JET_ACTUAL_RED: accepted thrust and physical velocity change must reach the typed air pose");
            TestAssert.True(next.Pose.Body.Position.IsFinite() && next.Pose.Body.Scale==Vector2.One &&
                Mathf.Abs(next.Pose.Body.RotationRadians-pose.TorsoLeanRadians)<.002f,
                "AIR_JET_ACTUAL_RED: real Dash uses directional torso rather than a fixed horizontal template");
            foreach(var pair in new[]{(Foot:next.NearFoot,Fold:pose.NearFold),(Foot:next.FarFoot,Fold:pose.FarFold)})
            {
                var foot=pair.Foot;
                TestAssert.True(foot.Sole.DistanceTo(foot.Free+new Vector2(pose.FootTrail,-pair.Fold))<.002f,
                    "AIR_JET_ACTUAL_RED: same published physics sample must reach actual folded foot Draw");
                TestAssert.Near(18,foot.Upper,.002f,"actual airborne upper link rigid");
                TestAssert.Near(18,foot.Lower,.002f,"actual airborne lower link rigid");
                TestAssert.True(!foot.Clamped && foot.Scale==Vector2.One,"actual air feet cannot clamp or stretch");
            }
            TestAssert.True(next.Nozzle.DistanceTo(next.ExpectedNozzle)<.002f && next.Nozzle.DistanceTo(next.CorePosition)>7.9f,
                "actual nozzle follows the same transformed shoulder, not a root/facing offset");
            TestAssert.True(fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation").FrameObserverFailureCount==0,
                "air-jet proof cannot hide observer exceptions");
            Release(); completed++;
        }
        GD.Print($"AIR_JET_NATIVE_OK directions={completed} real_shift=true same_sample=true same_identity=true jumps=0 no_refund=true directional_torso=true rigid_feet=true actual_nozzle=true headless_geometry=true human=false");
    }

    private readonly record struct AirFoot(Vector2 Sole,Vector2 Free,float Upper,float Lower,bool Clamped,Vector2 Scale);
    private readonly record struct RigSample(PlayerPresentationFrame Frame,
        ThrusterPresentationFrame Facts, Vector2 Velocity, float Rotation, Vector2 Scale, Vector2 Nozzle,
        Vector2 CorePosition)
    {
        public PlayerPose Pose { get; init; }
        public AirFoot NearFoot { get; init; }
        public AirFoot FarFoot { get; init; }
        public Vector2 ExpectedNozzle { get; init; }
    }
    private static T ReadRigProperty<T>(object value, string name) =>
        (T)value.GetType().GetProperty(name).GetValue(value);
    private static ThrusterPresentationFrame Mobility(RigSample sample) =>
        ReadRigProperty<ThrusterPresentationFrame>(sample.Frame, "Mobility");

    private sealed class RigObserver : IDisposable
    {
        private readonly Fixture _fixture;
        private readonly PlayerPresentationCoordinator _presentation;
        internal readonly PlayerFormalVisual Visual;
        internal readonly List<RigSample> Samples = new();
        internal RigObserver(Fixture fixture)
        {
            _fixture = fixture;
            _presentation = fixture.Player.GetNode<PlayerPresentationCoordinator>("Presentation");
            Visual = (PlayerFormalVisual)_presentation.ActiveVisual;
            _presentation.FramePublished += Published;
        }
        private void Published(PlayerPresentationFrame frame) => Samples.Add(new RigSample(frame,
            Frame(_fixture.Motor), _fixture.Player.Velocity, Visual.Rotation, Visual.Scale,
            ReadRigProperty<Vector2>(Visual, "ThrusterNozzleWorldPosition"),
            Visual.GetNode<Node2D>("ShoulderCore").GlobalPosition)
        {
            Pose=Visual.CurrentPose,
            NearFoot=CaptureFoot(Visual.GetNode<PlayerFormalLayer>("NearLeg")),
            FarFoot=CaptureFoot(Visual.GetNode<PlayerFormalLayer>("FarLeg")),
            ExpectedNozzle=Visual.GetNode<Node2D>("ShoulderCore").ToGlobal(new(-8,0))
        });
        private static AirFoot CaptureFoot(PlayerFormalLayer leg)=>new(leg.LegSoleLocal,leg.FreeLegSoleLocal,
            leg.LegHipLocal.DistanceTo(leg.LegKneeLocal),leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal),leg.LegReachClamped,leg.Scale);
        internal async Task<RigSample> Wait(Func<RigSample, bool> predicate, string label, int frames = 6)
        {
            int first = Samples.Count;
            for (int frame = 0; frame < frames; frame++)
            {
                await _fixture.Frames(1);
                for (int index = first; index < Samples.Count; index++)
                {
                    RigSample sample = Samples[index];
                    TestAssert.Equal(sample.Facts, Mobility(sample), "real published frame consumes identical Motor facts");
                    TestAssert.Near(ReadRigProperty<float>(sample.Frame, "MobilityLeanRadians"),
                        sample.Rotation, .0001f, "single typed renderer owns rigid lean");
                    TestAssert.True(Mathf.Abs(sample.Scale.X) == 1f && sample.Scale.Y == 1f,
                        "rig facing only, no nonuniform image stretch");
                    if (predicate(sample)) return sample;
                }
            }
            throw new InvalidOperationException("Actual typed rig never observed " + label);
        }
        public void Dispose() => _presentation.FramePublished -= Published;
    }

    private static async Task RigJetAndActions(SceneTree tree, bool left)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        using var observer = new RigObserver(fixture);
        int sign = left ? -1 : 1;
        SendKey(left ? Key.A : Key.D, true); SendKey(Key.Shift, true);
        RigSample burn = await observer.Wait(s => s.Facts.Burning && s.Rotation * sign > .005f, "forward burn lean");
        GD.Print($"THRUSTER_RIG_NOZZLE_DIAGNOSTIC rotation={burn.Rotation} identity={burn.Facts.Identity} " +
            $"nozzle={burn.Nozzle} core_at_publish={burn.CorePosition} " +
            $"core_at_assert={observer.Visual.GetNode<Node2D>("ShoulderCore").GlobalPosition} " +
            $"same_sample_distance={burn.Nozzle.DistanceTo(burn.CorePosition)} " +
            $"mixed_sample_distance={burn.Nozzle.DistanceTo(observer.Visual.GetNode<Node2D>("ShoulderCore").GlobalPosition)}");
        TestAssert.True(Mathf.Abs(burn.Rotation) <= .2f && burn.Facts.Identity != default &&
            burn.Nozzle.DistanceTo(burn.CorePosition) <= 8.1f,
            "finite jet lean and actual shoulder-core nozzle attachment");
        float rotation = observer.Visual.Rotation;
        ThrusterPresentationFrame frozen = Frame(fixture.Motor);
        int count = observer.Samples.Count;
        tree.Paused = true;
        for (int frame = 0; frame < 3; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(frozen, Frame(fixture.Motor), "pause freezes actual mobility");
        TestAssert.Equal(rotation, observer.Visual.Rotation, "pause freezes visible rig lean");
        TestAssert.Equal(count, observer.Samples.Count, "paused coordinator does not advance a presentation clock");
        tree.Paused = false; Release();
        RigSample coast = await observer.Wait(s => s.Facts.Coasting && !s.Facts.Burning,
            "real coast settle", 18);
        TestAssert.True(coast.Rotation * sign > 0f && coast.Facts.Identity == burn.Facts.Identity,
            "coast pose retains same accepted identity, not another dash");
        SendKey(left ? Key.D : Key.A, true);
        RigSample brake = await observer.Wait(s => s.Facts.Braking && s.Velocity.X * sign > 0f && s.Rotation * sign < -.005f,
            "reverse-brake lean while still moving forward");
        TestAssert.True(brake.Facts.Coasting && Mathf.Abs(brake.Rotation) <= .2f,
            "braking is orthogonal to short coast and has a distinct opposing pose");
        Release(); SendKey(Key.J, true);
        RigSample attack = await observer.Wait(s => s.Frame.Action.State is PlayerActionState.GroundAttack or
            PlayerActionState.AirAttack or PlayerActionState.DownAttack, "real J attack priority");
        TestAssert.Near(0f, attack.Rotation, .0001f, "attack owns visible posture over mobility");
        Release();
    }

    private static async Task RigOrdinaryBrake(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        using var observer = new RigObserver(fixture);
        SendKey(Key.D, true); await fixture.Frames(20); Release(); SendKey(Key.A, true);
        RigSample brake = await observer.Wait(s => s.Facts.Braking && s.Velocity.X > 0f && s.Rotation < 0f,
            "ordinary run reverse brake");
        TestAssert.True(brake.Facts.Identity == default && !brake.Facts.Burning && !brake.Facts.Coasting &&
            Facts(fixture.Motor).Length == 0, "ordinary brake pose never invents a burn identity");
        Release();
    }

    private static async Task RigRejectedGuard(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        using var observer = new RigObserver(fixture);
        SendKey(Key.L, true); SendKey(Key.Shift, true);
        RigSample guard = await observer.Wait(s => s.Frame.Action.State == PlayerActionState.Shield,
            "actual shield priority over rejected Shift");
        TestAssert.True(!guard.Facts.Burning && guard.Facts.Identity == default &&
            Facts(fixture.Motor).Length == 0, "guard-rejected input has no successful jet observation");
        TestAssert.Near(0f, guard.Rotation, .0001f, "shield posture is not tilted by rejected input");
        Release();
    }

    private static async Task RigDeath(SceneTree tree)
    {
        using var fixture = new Fixture(tree, false); await fixture.Start();
        using var observer = new RigObserver(fixture);
        SendKey(Key.D, true); SendKey(Key.Shift, true);
        await observer.Wait(s => s.Facts.Burning, "actual airborne jet before death"); Release();
        var health = fixture.Player.GetNode<HealthComponent>("Health");
        TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero,
            0f, 803UL, fixture.Player.GetInstanceId())), "real Health lethal request");
        RigSample dead = await observer.Wait(s => s.Frame.Locomotion.State == PlayerLocomotionState.Dead,
            "real death priority");
        TestAssert.True(Mobility(dead) == default && dead.Facts == default,
            "death removes mobility observation rather than visually continuing old jet");
        TestAssert.Near(0f, dead.Rotation, .0001f, "death pose is not rotated by old mobility lean");
    }

    public static async Task RunAudioOwner(SceneTree tree)
    {
        foreach (bool complete in new[] { false, true })
        {
            var audio = PrologueAudioDirector.CreateRuntimeTree();
            tree.Root.AddChild(audio);
            try
            {
                // SceneTree _Initialize runs before the root's first Ready.
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                var field = typeof(PrologueAudioDirector).GetField("_tweens",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var list = (List<Tween>)field.GetValue(audio);
                TestAssert.True(list.Count > 0 && list.Any(x => x.IsValid()),
                    "actual enabled audio creates its own native music fade, no fake Tween fixture");
                if (complete)
                {
                    await Finish();
                    TestAssert.Near(0f, audio.GetNode<AudioStreamPlayer>("MusicA").VolumeDb, .01f,
                        "native fade still completes to intended volume");
                    audio.SetMusicState(PrologueMusicState.Silent);
                    await Finish();
                    TestAssert.True(audio.GetNode<AudioStreamPlayer>("MusicA").Stream == null,
                        "native fade-out callback still releases its stream");
                }
                Tween[] created = list.ToArray(); // Read-only aliases, never disposed by test.
                audio.Shutdown();
                TestAssert.True(list.Count == 0 && created.All(x => !GodotObject.IsInstanceValid(x)),
                    "audio Shutdown releases its own active and completed Tween wrappers deterministically");
                audio.Shutdown();

                async Task Finish()
                {
                    for (int frame = 0; frame < 180 && list.Any(x => x.IsValid()); frame++)
                    {
                        await tree.ToSignal(tree, SceneTree.SignalName.PhysicsFrame);
                        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
                    }
                    TestAssert.True(list.All(x => !x.IsValid()), "actual native fade/callback completes within bounded gameplay frames");
                }
            }
            finally
            {
                audio.Free();
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        GD.Print("THRUSTER_AUDIO_OWNER_OK native_fade_completed=true callback_completed=true active_shutdown=true completed_shutdown=true idempotent=true only_owned_wrappers=true no_gc_or_drain=true human=false");
    }

    private static ThrusterPresentationFrame Frame(PlayerMotor motor) =>
        (ThrusterPresentationFrame)typeof(PlayerMotor).GetProperty("ThrusterPresentation").GetValue(motor);
    private static ThrusterPresentationTransition[] Facts(PlayerMotor motor, ulong after = 0UL) =>
        ((IReadOnlyList<ThrusterPresentationTransition>)typeof(PlayerMotor)
            .GetMethod("ReadThrusterTransitionsAfter").Invoke(motor, new object[] { after })).ToArray();

    public static async Task Run(SceneTree tree)
    {
        TestAssert.True(typeof(PlayerMotor).GetProperty("ThrusterPresentation") != null &&
            typeof(PlayerMotor).GetMethod("ReadThrusterTransitionsAfter") != null,
            "actual saved Player typed thruster facts are unavailable");
        await DelayedRenderAndReverseBrake(tree);
        await RejectedGuardAndOrdinaryBrake(tree);
        await ResetAndDeath(tree, false);
        await ResetAndDeath(tree, true);
        await ClawClearsCoast(tree);
        await CollisionClearsCoast(tree, false);
        await CollisionClearsCoast(tree, true);
        await ReentryAndDeath(tree);
        await ReverseAndReplacement(tree, false);
        await ReverseAndReplacement(tree, true);
        await NewJumpClearsCoast(tree);
        await ContinuousGroundJetBrakeJump(tree);
        GD.Print("THRUSTER_PRESENTATION_BOUNDARIES_OK reverse_cancel=true replacement=true jump_exit=true");
        GD.Print("THRUSTER_PRESENTATION_PLAYER_OK saved_player=true automatic_keyboard=true delayed_render_start_end=true finite_coast=true pause_frozen=true actual_brake_crossing=true rejected_guard=true reset_death=true claw_takeover=true axis_collision=true reentry=true human=false audiovisual=false");
    }

    private static async Task DelayedRenderAndReverseBrake(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true);
        await fixture.Start();
        fixture.Player.GetNode("Presentation").SetProcess(false);
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(1);
        ThrusterIdentity identity = fixture.Motor.ThrusterState.ActiveIdentity;
        TestAssert.True(Frame(fixture.Motor).Burning && Frame(fixture.Motor).Identity == identity,
            "accepted real commit produces the identical typed owner/life/epoch/burn");
        Release(); await fixture.Frames(14);
        var facts = Facts(fixture.Motor);
        TestAssert.True(facts.Length == 2 && facts[0].Kind == ThrusterTransitionKind.Started &&
            facts[1].Kind == ThrusterTransitionKind.Ended && facts.All(x => x.Identity == identity) &&
            facts[1].Reason == ThrusterEndReason.Exhausted,
            "start/end both survive without any intermediate presentation processing");
        TestAssert.True(Frame(fixture.Motor).Coasting && !Frame(fixture.Motor).Burning &&
            Frame(fixture.Motor).CoastRemaining <= .45f && fixture.Player.Velocity.X > 300f,
            "actual finite force creates short observation without deleting real residual speed");
        TestAssert.Equal(0, Facts(fixture.Motor, facts[1].Sequence).Length, "reader cursor doesn't replay transitions");
        var frozen = Frame(fixture.Motor); var pulse = fixture.Motor.ThrusterState;
        tree.Paused = true;
        for (int frame = 0; frame < 3; frame++) await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        TestAssert.Equal(frozen, Frame(fixture.Motor), "pause freezes the exact typed coast frame");
        TestAssert.Equal(pulse, fixture.Motor.ThrusterState, "pause preserves physical pulse/budget");
        tree.Paused = false;
        SendKey(Key.A, true); await fixture.Frames(1);
        TestAssert.True(Frame(fixture.Motor).Braking && Frame(fixture.Motor).Coasting,
            "real reverse braking and short coasting are orthogonal");
        bool crossed = false;
        for (int tick = 0; tick < 20; tick++)
        {
            await fixture.Frames(1);
            if (fixture.Player.Velocity.X <= 0f)
            {
                crossed = true;
                TestAssert.True(!Frame(fixture.Motor).Coasting && Frame(fixture.Motor).Identity == default,
                    "actual brake-to-zero exits coast even when the remaining step already reverses");
                break;
            }
        }
        TestAssert.True(crossed && fixture.Motor.AirDashAvailable && Facts(fixture.Motor).Length == 2,
            "observation braking neither spends budget nor invents another start/end");
        Release();
    }

    private static async Task ReverseAndReplacement(SceneTree tree, bool activeReplacement)
    {
        using var fixture = new Fixture(tree, false); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        ThrusterIdentity identity = fixture.Motor.ThrusterState.ActiveIdentity;
        if (!activeReplacement)
        {
            SendKey(Key.A, true); await fixture.Frames(1); Release();
            TestAssert.True(Frame(fixture.Motor).Coasting && Frame(fixture.Motor).Braking &&
                !Frame(fixture.Motor).Burning && fixture.Player.Velocity.X > 0f,
                "real mid-burn opposite control cancels only finite thrust and brakes inherited residual velocity");
        }
        fixture.Motor.ExternalImpulse(new Vector2(-40f, 20f));
        var facts = Facts(fixture.Motor);
        TestAssert.True(facts.Length == 2 && facts[1].Identity == identity &&
            facts[1].Reason == (activeReplacement ? ThrusterEndReason.ExternalReplacement : ThrusterEndReason.ReverseCancelled) &&
            Frame(fixture.Motor) == default && !fixture.Motor.AirDashAvailable,
            "full replacement clears typed observation without duplicate end or budget refund");
        TestAssert.Equal(new Vector2(-40f, 20f), fixture.Player.Velocity,
            "presentation never modifies the real complete replacement");
    }

    private static async Task NewJumpClearsCoast(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(1); Release();
        await fixture.Frames(14);
        TestAssert.True(Frame(fixture.Motor).Coasting && fixture.Player.IsOnFloor(), "jump exit starts from real supported coast");
        SendKey(Key.Space, true); await fixture.Frames(1); Release();
        TestAssert.True(fixture.Motor.AcceptedJumpCount == 1 && fixture.Player.Velocity.Y < 0f &&
            !Frame(fixture.Motor).Coasting && Frame(fixture.Motor).Identity == default && Facts(fixture.Motor).Length == 2,
            "new ordinary jump exits jet observation without inventing another burn or blocking legitimate ascent");
    }

    private static async Task ContinuousGroundJetBrakeJump(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        var player = fixture.Player;
        var motor = fixture.Motor;
        var formal = player.GetNode<PlayerFormalVisual>("VisualRoot/Formal");
        var presentation = player.GetNode<PlayerPresentationCoordinator>("Presentation");
        var input = player.GetNode<PlayerInputReader>("Input");
        var health = player.GetNode<HealthComponent>("Health");
        var initialHealth = health.CurrentHealth;
        ulong landingBefore = motor.LandingCount;
        float groundY = player.Position.Y;
        float startX = player.Position.X;
        ulong jumpPhysics = 0, landingPhysics = 0;
        bool publishedJump = false, publishedLanding = false, presentedLaunch = false;
        ulong Support()
        {
            for (int i = 0; i < player.GetSlideCollisionCount(); i++)
            {
                var contact = player.GetSlideCollision(i);
                if (contact.GetNormal().Dot(Vector2.Up) > .7f &&
                    contact.GetCollider() is GodotObject floor)
                    return floor.GetInstanceId();
            }
            return 0;
        }
        ulong originalSupport = Support(), landingSupport = 0;
        float previousVertical = 0, approachVertical = 0;
        double? lastPublishedTime = null;
        int recoverySamples = 0;
        bool measuredLandingPose = false;
        double? settledSince = null;
        double settledDuration = 0;
        float settledMantle = 0, settledLantern = 0;
        float peakHem = 0, settledHem = 0, collarError = 0;
        var nearLeg = formal.GetNode<PlayerFormalLayer>("NearLeg");
        var farLeg = formal.GetNode<PlayerFormalLayer>("FarLeg");
        var mantle = formal.GetNode<PlayerFormalLayer>("Mantle");
        var lantern = formal.GetNode<Node2D>("Lantern");
        ulong beforeTick = 0;
        Transform2D beforeTransform = default;
        float beforeMargin = 0, beforeStep = 0;
        bool beforeGrounded = false;
        var beforeMotor = new BasinBoardingTickObserver { ProcessPhysicsPriority = -1 };
        beforeMotor.Sample = delta =>
        {
            beforeTick = Engine.GetPhysicsFrames();
            beforeTransform = player.GlobalTransform;
            beforeMargin = player.SafeMargin;
            beforeStep = (float)delta;
            beforeGrounded = player.IsOnFloor();
        };
        fixture.Root.AddChild(beforeMotor);
        var witness = new BasinBoardingTickObserver { ProcessPhysicsPriority = 100 };
        witness.Sample = _ =>
        {
            if (jumpPhysics == 0 && motor.AcceptedJumpCount == 1)
                jumpPhysics = Engine.GetPhysicsFrames();
            if (landingPhysics == 0 && motor.LandingCount > landingBefore)
            {
                landingPhysics = Engine.GetPhysicsFrames();
                landingSupport = Support();
                approachVertical = previousVertical;
                using var diagnostic = new LandingImpactSampler();
                var support = diagnostic.InspectFloorSupport(player);
                int slideCount = player.GetSlideCollisionCount();
                GD.Print($"THRUSTER_CONTINUOUS_CONTACT physics={landingPhysics} slides={slideCount} complete={support.Complete} unique={support.Unique} all_soft={support.AllSoft}");
                // The flat fixture has no moving supports, wind volumes or attached claw.
                // Replay from the captured origin; the post-Motor body itself is not moved.
                var command = (motor.UpdraftCandidateVelocity + motor.UpdraftVelocityDelta) * beforeStep;
                using var probe = new KinematicCollision2D();
                bool overlap = player.TestMove(beforeTransform, Vector2.Zero, probe, beforeMargin, true);
                var moved = player.GlobalPosition - beforeTransform.Origin;
                var totalTravel = Vector2.Zero;
                GD.Print($"THRUSTER_CONTINUOUS_REPLAY before={beforeTick} after={landingPhysics} grounded={beforeGrounded} overlap={overlap} command={command} moved={moved}");
                foreach (bool recovery in new[] { true, false })
                {
                    bool hit = player.TestMove(beforeTransform, command, probe, beforeMargin, recovery);
                    if (hit) GD.Print(FormattableString.Invariant(
                        $"THRUSTER_CONTINUOUS_PREDICTION recovery={recovery} rid={probe.GetColliderRid()} shape={probe.GetColliderShapeIndex()} normal={probe.GetNormal()} travel={probe.GetTravel()} remainder={probe.GetRemainder()} depth={probe.GetDepth():F5}"));
                    else GD.Print($"THRUSTER_CONTINUOUS_PREDICTION recovery={recovery} hit=false");
                }
                for (int i = 0; i < slideCount; i++)
                {
                    var contact = player.GetSlideCollision(i);
                    if (i > 0)
                    {
                        var prior = player.GetSlideCollision(i - 1);
                        var from = beforeTransform;
                        from.Origin += totalTravel;
                        foreach (var candidate in new[]
                        {
                            (Name: "remaining", Motion: prior.GetRemainder()),
                            (Name: "tangent", Motion: prior.GetRemainder().Slide(prior.GetNormal()))
                        })
                        {
                            bool hit = player.TestMove(from, candidate.Motion, probe, beforeMargin, true);
                            bool same = hit && probe.GetColliderRid() == contact.GetColliderRid() &&
                                probe.GetColliderShapeIndex() == contact.GetColliderShapeIndex() &&
                                probe.GetNormal().DistanceTo(contact.GetNormal()) <= .002f &&
                                probe.GetTravel().DistanceTo(contact.GetTravel()) <= .002f &&
                                probe.GetRemainder().DistanceTo(contact.GetRemainder()) <= .002f &&
                                Mathf.Abs(probe.GetDepth() - contact.GetDepth()) <= .002f;
                            GD.Print($"THRUSTER_CONTINUOUS_SUBSTEP index={i} candidate={candidate.Name} hit={hit} match={same} command={candidate.Motion} travel={probe.GetTravel()} remainder={probe.GetRemainder()}");
                        }
                    }
                    totalTravel += contact.GetTravel();
                    GD.Print(FormattableString.Invariant(
                        $"THRUSTER_CONTINUOUS_SLIDE index={i} rid={contact.GetColliderRid()} collider_shape={contact.GetColliderShapeIndex()} normal={contact.GetNormal()} travel={contact.GetTravel()} remainder={contact.GetRemainder()} depth={contact.GetDepth():F5}"));
                }
                GD.Print($"THRUSTER_CONTINUOUS_TRAVEL sum={totalTravel} delta_from_actual={moved-totalTravel}");
            }
            previousVertical = player.Velocity.Y;
        };
        fixture.Root.AddChild(witness);
        void Published(PlayerPresentationFrame frame)
        {
            bool advanced = frame.PresentationTimeSeconds != lastPublishedTime;
            lastPublishedTime = frame.PresentationTimeSeconds;
            void Log(string stage, ulong completedPhysics) => GD.Print(FormattableString.Invariant(
                $"THRUSTER_CONTINUOUS_PUBLISHED stage={stage} completed_physics={completedPhysics} published_physics={Engine.GetPhysicsFrames()} input={input.CurrentFrameSequence} floor={player.IsOnFloor()} vy={player.Velocity.Y:F3} pose={frame.Locomotion.State} jump_started={frame.OrdinaryAir.JumpStarted} jump_arc={frame.OrdinaryAir.JumpArc} landing_load={frame.LandingLoad.Active} coast={frame.Mobility.Coasting} time={frame.PresentationTimeSeconds:F5}"));
            if (!publishedJump && jumpPhysics != 0 && !player.IsOnFloor())
            {
                publishedJump = true;
                presentedLaunch = frame.OrdinaryAir.JumpStarted && frame.OrdinaryAir.JumpArc &&
                    player.Velocity.Y < 0f && !frame.Mobility.Coasting;
                Log("jump", jumpPhysics);
            }
            if (advanced && landingPhysics != 0 && player.IsOnFloor() && recoverySamples < 18)
            {
                measuredLandingPose |= frame.LandingLoad.Active && frame.LandingLoad.Compression > 0f &&
                    frame.Gait.Weight >= .999f;
                if (!publishedLanding)
                {
                    publishedLanding = true;
                    Log("landing", landingPhysics);
                }
                var impact = motor.LastLandingImpact;
                var nearSole = nearLeg.ToGlobal(nearLeg.LegSoleLocal);
                var farSole = farLeg.ToGlobal(farLeg.LegSoleLocal);
                GD.Print(FormattableString.Invariant(
                    $"THRUSTER_CONTINUOUS_LANDING index={recoverySamples++} physics={Engine.GetPhysicsFrames()} support={landingSupport} approach_vy={approachVertical:F3} impact={impact.Reason} impact_speed={impact.Speed:F3} compression={frame.LandingLoad.Compression:F4} gait={frame.Gait.Weight:F3} body_y={formal.CurrentPose.Body.Position.Y:F3} near_y={nearSole.Y:F3} far_y={farSole.Y:F3} mantle_rotation={mantle.Rotation:F4} lantern_y={lantern.Position.Y:F3}"));
            }
            if (advanced && landingPhysics != 0 && frame.PresentationTimeSeconds is double time)
            {
                settledMantle = frame.Secondary.Mantle.Position.Length();
                settledLantern = frame.Secondary.Lantern.Position.Length();
                Vector2 hem = new(-4f, 31f), collar = new(-12f, -3f);
                settledHem = mantle.MapMantlePoint(hem).DistanceTo(hem);
                peakHem = Mathf.Max(peakHem, settledHem);
                collarError = Mathf.Max(collarError, mantle.MapMantlePoint(collar).DistanceTo(collar));
                bool stable = player.IsOnFloor() && player.Velocity.LengthSquared() < .01f &&
                    frame.Locomotion.State == PlayerLocomotionState.Idle && !frame.LandingLoad.Active &&
                    !frame.OrdinaryAir.Active && !frame.Mobility.Coasting &&
                    settledMantle < .05f && settledLantern < .05f &&
                    settledHem < .2f &&
                    Mathf.Abs(frame.Secondary.Mantle.RotationRadians) < .005f &&
                    Mathf.Abs(frame.Secondary.Lantern.RotationRadians) < .005f;
                if (!stable) { settledSince = null; settledDuration = 0; }
                else { settledSince ??= time; settledDuration = time - settledSince.Value; }
            }
        }
        presentation.FramePublished += Published;

        void Sample(string stage)
        {
            var pose = formal.CurrentPlayerFrame;
            GD.Print(FormattableString.Invariant(
                $"THRUSTER_CONTINUOUS_SAMPLE stage={stage} x={player.Position.X:F3} y={player.Position.Y:F3} vx={player.Velocity.X:F3} vy={player.Velocity.Y:F3} floor={player.IsOnFloor()} pose={pose.Locomotion.State} brake={pose.GroundBrake.Strength:F3} jumps={motor.AcceptedJumpCount} dashes={motor.AcceptedDashCount} landings={motor.LandingCount - landingBefore}"));
        }

        SendKey(Key.D, true); await fixture.Frames(6);
        float walkSpeed = player.Velocity.X;
        Sample("walk");
        TestAssert.True(player.IsOnFloor() && player.Position.X > startX && walkSpeed > 0f,
            "continuous trace walks and accelerates on actual support before the jet");
        SendKey(Key.Shift, true); await fixture.Frames(1); Release();
        Sample("jet");
        TestAssert.True(motor.AcceptedDashCount == 1 && Frame(motor).Burning && player.Velocity.X > walkSpeed,
            "the short jet adds to actual incoming walk velocity");
        await fixture.Frames(14);
        Sample("coast");
        float coastX = player.Position.X;
        float coastSpeed = player.Velocity.X;
        TestAssert.True(player.IsOnFloor() && Frame(motor).Coasting && coastSpeed > 0f,
            "on the same ground, releasing the jet retains forward momentum");
        SendKey(Key.A, true); await fixture.Frames(3);
        Sample("reverse");
        TestAssert.True(player.IsOnFloor() && player.Position.X > coastX &&
            player.Velocity.X > 0f && player.Velocity.X < coastSpeed,
            "counter-input reduces forward speed while the body still advances");
        SendKey(Key.A, false); SendKey(Key.Space, true); await fixture.Frames(1); Release();
        Sample("jump");
        TestAssert.True(motor.AcceptedJumpCount == 1 && player.Velocity.Y < 0f && !Frame(motor).Coasting,
            "one ordinary Space press rises from the braking sequence and clears the jet coast");
        for (int tick = 0; tick < 120 && motor.LandingCount == landingBefore; tick++)
            await fixture.Frames(1);
        Sample("landing");
        TestAssert.True(player.IsOnFloor() && motor.LandingCount == landingBefore + 1 &&
            Mathf.Abs(player.Position.Y - groundY) < 0.2f &&
            motor.AcceptedDashCount == 1 && motor.AcceptedJumpCount == 1 && health.CurrentHealth == initialHealth,
            "the same body returns to the original support once without an extra action or damage");
        for (int tick = 0; tick < 120 && (recoverySamples < 18 || settledDuration < .12); tick++)
            await fixture.Frames(1);
        presentation.FramePublished -= Published;
        TestAssert.True(publishedJump && publishedLanding,
            "real coordinator publishes a sample for both completed physical transitions");
        TestAssert.True(originalSupport != 0 && originalSupport == landingSupport &&
            approachVertical > 0 && recoverySamples >= 12,
            "landing observation follows real descent onto the same collision body and covers settling");
        TestAssert.True(presentedLaunch,
            "CONTINUOUS_JET_JUMP_LAUNCH: first airborne publication must show the real accepted launch after supported coast");
        TestAssert.True(measuredLandingPose && !motor.LastLandingImpact.IsReliable,
            "CONTINUOUS_SLIDE_LANDING_LOAD: validated same-shape slide must drive supported compression without changing damage authority");
        GD.Print(FormattableString.Invariant(
            $"THRUSTER_CONTINUOUS_SETTLING physics_since_contact={Engine.GetPhysicsFrames()-landingPhysics} stable_seconds={settledDuration:F4} mantle_residual={settledMantle:F5} lantern_residual={settledLantern:F5} peak_hem={peakHem:F4} hem_residual={settledHem:F5} collar_error={collarError:F5} vx={player.Velocity.X:F4}"));
        TestAssert.True(settledDuration >= .12 && motor.LandingCount == landingBefore + 1 &&
            health.CurrentHealth == initialHealth && peakHem > .2f && collarError < .001f,
            "CONTINUOUS_LANDING_SETTLING: actual idle and attachment springs must remain settled without another landing");
        GD.Print("THRUSTER_CONTINUOUS_INPUT_OK walk=true finite_jet=true forward_coast=true counter_input=true ordinary_jump=true same_support_landing=true human=false audiovisual=false");
    }

    private static async Task RejectedGuardAndOrdinaryBrake(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true); await fixture.Start();
        SendKey(Key.L, true); SendKey(Key.Shift, true); await fixture.Frames(1);
        TestAssert.True(fixture.Player.GetNode<PlayerInputReader>("Input").RawFrame.DashPressed &&
            fixture.Player.GetNode<PlayerInputReader>("Input").RawFrame.ShieldHeld &&
            fixture.Motor.AcceptedDashCount == 0 && Facts(fixture.Motor).Length == 0 &&
            Frame(fixture.Motor).Identity == default && !Frame(fixture.Motor).Burning,
            "raw guard priority suppresses Shift before Motor; no accepted identity or transition");
        Release(); await fixture.Frames(1);
        SendKey(Key.D, true); await fixture.Frames(12); SendKey(Key.D, false); SendKey(Key.A, true);
        await fixture.Frames(1);
        TestAssert.True(Frame(fixture.Motor).Braking && !Frame(fixture.Motor).Coasting &&
            Frame(fixture.Motor).Identity == default,
            "ordinary real reverse braking has no invented old burn identity");
        Release();
    }

    private static async Task ResetAndDeath(SceneTree tree, bool death)
    {
        using var fixture = new Fixture(tree, false); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        var identity = fixture.Motor.ThrusterState.ActiveIdentity;
        if (death)
        {
            var health = fixture.Player.GetNode<HealthComponent>("Health");
            TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero,
                0f, 701UL, fixture.Player.GetInstanceId())), "actual damage kills; no HP rewrite");
        }
        else fixture.Motor.ResetTransientState();
        var facts = Facts(fixture.Motor);
        TestAssert.True(facts.Length == 2 && facts[1].Identity == identity &&
            facts[1].Reason == (death ? ThrusterEndReason.Death : ThrusterEndReason.Reset),
            "real death and explicit reset retain their distinct old-identity end reasons");
        TestAssert.Equal(default(ThrusterPresentationFrame), Frame(fixture.Motor),
            "retired lifecycle leaves no burning, coast, brake or stale direction");
        await fixture.Frames(3);
        TestAssert.Equal(2, Facts(fixture.Motor).Length, "reset/death cannot replay the held or retired pulse");
    }

    private static async Task ClawClearsCoast(SceneTree tree)
    {
        using var fixture = new Fixture(tree, true);
        fixture.Root.AddChild(new GrappleSurface2D { Name = "Anchor", Position = new Vector2(0, -260), AssistRadius = 24 });
        await fixture.Start(); SendKey(Key.W, true); SendKey(Key.Space, true);
        SendKey(Key.Shift, true); await fixture.Frames(1);
        SendKey(Key.Shift, false); SendKey(Key.Space, false); await fixture.Frames(13);
        TestAssert.True(Frame(fixture.Motor).Coasting, "claw test starts with actual upward jet coast");
        SendKey(Key.K, true); await fixture.Frames(1); SendKey(Key.K, false);
        var claw = fixture.Player.GetNode<MechanicalClawController>("MechanicalClaw");
        for (int tick = 0; tick < 150 && claw.CurrentFrame.State is not (MechanicalClawState.Reel or MechanicalClawState.Swing); tick++)
            await fixture.Frames(1);
        TestAssert.True(claw.CurrentFrame.State is MechanicalClawState.Reel or MechanicalClawState.Swing,
            "real claw reaches attached phase, not merely Aim or Travel");
        TestAssert.True(!Frame(fixture.Motor).Coasting && !Frame(fixture.Motor).Burning &&
            !fixture.Motor.AirDashAvailable, "actual claw attachment clears observation without air-use refund");
        Release();
    }

    private static async Task CollisionClearsCoast(SceneTree tree, bool ceiling)
    {
        using var fixture = new Fixture(tree, ceiling);
        fixture.AddSolid(ceiling ? new Vector2(0, -80) : new Vector2(120, -300),
            ceiling ? new Vector2(800, 10) : new Vector2(10, 1600));
        bool observed = false, retained = false;
        fixture.Root.AddChild(new ThrusterPostMotorObserver { ProcessPhysicsPriority = 110, Observe = () =>
        {
            if (!(ceiling ? fixture.Player.IsOnCeiling() : fixture.Player.IsOnWall())) return;
            observed = true;
            if (Frame(fixture.Motor).Coasting && !retained)
            {
                retained = true;
                GD.Print($"THRUSTER_AXIS_CONTACT_DIAGNOSTIC ceiling={ceiling} count={fixture.Player.GetSlideCollisionCount()} velocity={fixture.Player.Velocity} platform={fixture.Player.GetPlatformVelocity()} frame={Frame(fixture.Motor)}");
                for (int index = 0; index < fixture.Player.GetSlideCollisionCount(); index++)
                {
                    var contact = fixture.Player.GetSlideCollision(index); // Borrowed.
                    GD.Print($"THRUSTER_AXIS_CONTACT_DETAIL index={index} normal={contact.GetNormal()} depth={contact.GetDepth()} travel={contact.GetTravel()} remainder={contact.GetRemainder()} colliderVelocity={contact.GetColliderVelocity()} type={contact.GetCollider()?.GetClass()}");
                }
            }
        } });
        await fixture.Start(); SendKey(ceiling ? Key.W : Key.D, true); SendKey(Key.Shift, true);
        await fixture.Frames(20); Release();
        TestAssert.True(observed && !retained && !fixture.Motor.IsDashing && !Frame(fixture.Motor).Coasting,
            "saved capsule reaches thin wall/low ceiling and finite burn ends with no blocked coast");
    }

    private static async Task ReentryAndDeath(SceneTree tree)
    {
        using var fixture = new Fixture(tree, false); await fixture.Start();
        SendKey(Key.D, true); SendKey(Key.Shift, true); await fixture.Frames(2); Release();
        var identity = fixture.Motor.ThrusterState.ActiveIdentity;
        fixture.Root.RemoveChild(fixture.Player); fixture.Root.AddChild(fixture.Player);
        var ended = Facts(fixture.Motor);
        TestAssert.True(ended.Length == 2 && ended[1].Identity == identity &&
            ended[1].Reason.ToString() == "OwnerRetired" && !Frame(fixture.Motor).Burning,
            "same Player reparent retires exact burn without misreporting HP death");
        await fixture.Frames(1);
        var health = fixture.Player.GetNode<HealthComponent>("Health");
        ulong before = fixture.Motor.ThrusterState.Lifecycle;
        TestAssert.True(health.ReceiveDamage(new DamageRequest(health.MaxHealth, Vector2.Zero,
            0f, 702UL, fixture.Player.GetInstanceId())), "reentered Player dies through Health");
        TestAssert.True(fixture.Motor.ThrusterState.Lifecycle > before,
            "death subscription remains alive after tree reentry without RequestReady");
        TestAssert.Equal(2, Facts(fixture.Motor).Length, "retired burn is not ended again on later death");
    }

    private static void SendKey(Key key, bool pressed) => ThrusterPlayerRuntimeTests.KeyEvent(key, pressed);
    private static void Release() => ThrusterPlayerRuntimeTests.Release();
    private sealed class Fixture : IDisposable
    {
        private readonly SceneTree _tree;
        private readonly bool _supported;
        private readonly List<Shape2D> _shapes = new();
        internal Node2D Root { get; } = new() { Name = "ThrusterPresentationPhysics" };
        internal CharacterBody2D Player { get; }
        internal PlayerMotor Motor => Player.GetNode<PlayerMotor>("Motor");
        internal Fixture(SceneTree tree, bool supported)
        {
            _tree = tree; _supported = supported;
            if (supported) AddSolid(new Vector2(0, 60), new Vector2(4000, 20));
            Player = GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position = new Vector2(0, supported ? 14 : -500);
            Player.GetNode<Camera2D>("Camera2D").Enabled = false;
            Root.AddChild(Player);
        }
        internal void AddSolid(Vector2 position, Vector2 size)
        {
            var shape = new RectangleShape2D { Size = size }; _shapes.Add(shape);
            var body = new StaticBody2D { Position = position, CollisionLayer = CombatCollisionLayers.World };
            body.AddChild(new CollisionShape2D { Shape = shape }); Root.AddChild(body);
        }
        internal async Task Start()
        {
            Release(); _tree.Root.AddChild(Root); await Frames(_supported ? 8 : 1);
            TestAssert.Equal(_supported, Player.IsOnFloor(), "fixture starts with intended real support");
        }
        internal Task Frames(int count) => ThrusterPlayerRuntimeTests.Frames(_tree,
            Player.GetNode<PlayerInputReader>("Input"), count);
        public void Dispose()
        {
            // Scalar diagnosis of the actually owned copy, no playback lookup,
            // borrowed disposal, extra frame or retained wrapper across teardown.
            ulong loopId = 0UL, voiceId = 0UL, audioId = 0UL;
            ulong rootId = Root.GetInstanceId();
            bool trace = Array.IndexOf(OS.GetCmdlineUserArgs(), "--trace-thruster-audio-owners") >= 0;
            if (trace && Root.GetNodeOrNull<PrologueAudioDirector>("Audio") is PrologueAudioDirector audio)
            {
                var loop = typeof(PrologueAudioDirector).GetField("_thrusterLoop",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(audio) as GodotObject;
                loopId = loop?.GetInstanceId() ?? 0UL;
                audioId = audio.GetInstanceId();
                voiceId = audio.GetNodeOrNull<AudioStreamPlayer>("ThrusterVoice")?.GetInstanceId() ?? 0UL;
                GD.Print($"THRUSTER_OWNED_COPY_ID stage=before-root-free id={loopId} present={loopId != 0UL} " +
                    $"root={rootId} audio={audioId} voice={voiceId} diagnostic_only=true");
            }
            Release(); _tree.Paused = false; if (GodotObject.IsInstanceValid(Root)) Root.Free();
            foreach (var shape in _shapes) shape.Dispose();
            if (trace) GD.Print($"THRUSTER_OWNED_COPY_ID stage=after-root-free id={loopId} " +
                $"valid={loopId != 0UL && GodotObject.IsInstanceIdValid(loopId)} root={rootId} root_valid={GodotObject.IsInstanceIdValid(rootId)} " +
                $"audio={audioId} audio_valid={audioId != 0UL && GodotObject.IsInstanceIdValid(audioId)} " +
                $"voice={voiceId} voice_valid={voiceId != 0UL && GodotObject.IsInstanceIdValid(voiceId)} diagnostic_only=true");
        }
    }
}
