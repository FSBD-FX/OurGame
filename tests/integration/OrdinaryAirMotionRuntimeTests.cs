using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Knight.Combat;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static class OrdinaryAirMotionRuntimeTests
{
    private static readonly string[] Slots={"Body","NearArm","FarArm","NearLeg","FarLeg","Weapon","Mantle","ShoulderCore","Lantern","Tether"};

    private readonly record struct CarrierFoot(Vector2 Position,float Rotation,Vector2 Ankle,Vector2 Sole);

    public static async Task RunNonzeroAirHandoff(SceneTree tree)
    {
        var formal=GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn").Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(formal);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        try
        {
            int cases=0;
            foreach(int hz in new[]{30,60,120})
            foreach(int facing in new[]{1,-1})
            foreach(float entry in new[]{0f,.35f,1f})
            foreach(var action in new[]{PlayerActionFrame.None,
                new PlayerActionFrame(PlayerActionState.AirAttack,PlayerActionPhase.Contact,1,false,Vector2.Right),
                new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Contact,1,false,Vector2.Right)})
            {
                for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,facing,true));
                var secondary=new SecondaryMotionFrame(PoseTransform2D.Identity with {Position=new(1,2),RotationRadians=.03f},
                    PoseTransform2D.Identity with {Position=new(2,-1),RotationRadians=-.02f},
                    PoseTransform2D.Identity with {RotationRadians=.04f},PoseTransform2D.Identity with {Position=new(1,-1)});
                var jet=new PlayerPresentationFrame(new(EntityVisualState.Fall,facing,false),new(PlayerLocomotionState.Fall,1),action,secondary)
                    {AirJet=new(true,true,Vector2.Right,.12f*facing,.6f,5,4,-1.5f*facing),PresentationTimeSeconds=0};
                formal.ApplyPlayerFrame(jet);formal.ApplyPlayerFrame(jet with {PresentationTimeSeconds=.2});
                jet=jet with {PresentationTimeSeconds=.4};formal.ApplyPlayerFrame(jet);
                var root=formal.GlobalTransform;float oldBody=formal.CurrentPose.Body.RotationRadians;
                var before=LoadGeometry(formal);
                var ordinary=jet with {AirJet=default,OrdinaryAir=new(true,false,false,entry,-.03f*facing,-.2f,-1.5f,-.8f,2*facing)};
                formal.ApplyPlayerFrame(ordinary);
                AssertLoadHeld(formal,before,"nonzero entry");
                AssertLoadGrips(formal,action.State!=PlayerActionState.Shield);
                TestAssert.Equal(ordinary,formal.CurrentPlayerFrame,"visual handoff cannot rewrite source flags/lease");
                double offset=0;
                for(int i=1;i<=hz*2/15;i++)
                {
                    var current=ordinary with {PresentationTimeSeconds=.4+i/(double)hz+offset,OrdinaryAir=ordinary.OrdinaryAir with {EntryWeight=1}};
                    formal.ApplyPlayerFrame(current);AssertLoadGrips(formal,action.State!=PlayerActionState.Shield);
                    if(i==hz/15)
                    {
                        TestAssert.Near(oldBody-.075f*facing,formal.CurrentPose.Body.RotationRadians,.00005f,
                            "AIR_LOAD_HANDOFF_RED: common body load reaches half target at 1/15 second independent of frame rate");
                        var middle=LoadGeometry(formal);formal.ApplyPlayerFrame(current);AssertLoadHeld(formal,middle,"midpoint repeat");
                        formal.ApplyPlayerFrame(current with {PresentationTimeSeconds=current.PresentationTimeSeconds-.001});
                        AssertLoadHeld(formal,middle,"midpoint rollback");offset=-.001;
                    }
                }
                TestAssert.Near(oldBody-.15f*facing,formal.CurrentPose.Body.RotationRadians,.00005f,"finite bridge reaches ordinary body target");
                foreach(string name in new[]{"NearLeg","FarLeg"})
                {
                    var leg=formal.GetNode<PlayerFormalLayer>(name);
                    Vector2 target=leg.FreeLegSoleLocal+new Vector2(2*facing,name=="NearLeg"?1.5f:.8f);
                    TestAssert.True(leg.LegSoleLocal.DistanceTo(target)<.003f,"finite foot handoff actually reaches ordinary articulated target");
                }
                TestAssert.Equal(root,formal.GlobalTransform,"nonzero handoff cannot translate/rotate/scale physical root");cases++;
            }
            // The actual production models can publish a zero-time no-lease gap:
            // Jet clears immediately while ordinary has no positive-time target yet.
            var jetModel=new PlayerAirJetPoseModel();var ordinaryModel=new PlayerOrdinaryAirMotion();
            var motion=new PlayerGroundSample(true,Vector2.Zero,0,Transform2D.Identity,Vector2.Zero,Vector2.Up)
                {PhysicsTick=1,PhysicsStepSeconds=1f/60};
            var source=new ThrusterPresentationFrame(new(71,1,1,1),Vector2.Right,true,false,false,0);
            var actual=jetModel.Step(new(71,new(75,100),motion,1,false,false,source),1f/60);
            ordinaryModel.Step(new(new(71,0,true),motion,new(75,100),1,false,false,true),1f/60);
            var gapJet=jetModel.Step(new(71,new(75,100),motion,1,false,false,default),0);
            var gapOrdinary=ordinaryModel.Step(new(new(71,0,true),motion,new(75,100),1,false,false,false),0);
            TestAssert.True(actual.Active && !gapJet.Active && !gapOrdinary.Active,"real models must produce the zero-time inactive gap precondition");
            for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,1,true));
            var gapBase=new PlayerPresentationFrame(new(EntityVisualState.Fall,1,false),new(PlayerLocomotionState.Fall,1),
                PlayerActionFrame.None,SecondaryMotionFrame.Identity){AirJet=actual,PresentationTimeSeconds=0};
            formal.ApplyPlayerFrame(gapBase);formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=.2});
            gapBase=gapBase with {PresentationTimeSeconds=.4};formal.ApplyPlayerFrame(gapBase);
            var gap=gapBase with {AirJet=gapJet,OrdinaryAir=gapOrdinary};var prior=LoadGeometry(formal);
            formal.ApplyPlayerFrame(gap);AssertLoadHeld(formal,prior,"zero-time real-model gap");
            TestAssert.True(!formal.CurrentPlayerFrame.AirJet.Active && !formal.CurrentPlayerFrame.OrdinaryAir.Active &&
                formal.CurrentPlayerFrame.Mobility==default,"frozen visual residual cannot resurrect a public jet/ordinary/lease");
            var sameTick=ordinaryModel.Step(new(new(71,0,true),motion,new(75,100),1,false,false,false),1f/60);
            TestAssert.True(!sameTick.Active,"a positive render step at the same physical sample still has no ordinary target");
            var positiveGap=gap with {PresentationTimeSeconds=.4+1d/60,OrdinaryAir=sameTick};
            formal.ApplyPlayerFrame(positiveGap);
            TestAssert.Near(actual.TorsoLeanRadians*.95703125f,formal.CurrentPose.Body.RotationRadians,.00005f,
                "AIR_POSITIVE_GAP_RED: valid same-physics render gap must ease pure residual, not immediately clear it");
            AssertLoadGrips(formal,true);var positivePose=LoadGeometry(formal);
            formal.ApplyPlayerFrame(positiveGap);AssertLoadHeld(formal,positivePose,"repeated positive gap time");
            var replacement=positiveGap with {OrdinaryAir=new(true,false,false,1,-.03f,-.2f,-1,-.5f,1)};
            formal.ApplyPlayerFrame(replacement);AssertLoadHeld(formal,positivePose,"ordinary after real same-tick positive gap");
            formal.ApplyPlayerFrame(replacement with {PresentationTimeSeconds=.4+1d/15});
            var half=LoadGeometry(formal);formal.ApplyPlayerFrame(replacement with {PresentationTimeSeconds=.4+1d/15});
            AssertLoadHeld(formal,half,"gap bridge midway");
            // Compatibility callers without a timestamp must finish in finite steps.
            for(int i=0;i<20;i++)formal.ApplyPlayerFrame(replacement with {PresentationTimeSeconds=null});
            TestAssert.Near(-.03f,formal.CurrentPose.Body.RotationRadians,.00005f,"null-time handoff must not lock at a cached pose");
            AssertLoadGrips(formal,true);
            foreach(int hz in new[]{30,60,120})
            {
                for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,1,true));
                formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=0});formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=.2});formal.ApplyPlayerFrame(gapBase);
                formal.ApplyPlayerFrame(gap);
                for(int i=1;i<=hz*2/15;i++)
                {
                    formal.ApplyPlayerFrame(gap with {PresentationTimeSeconds=.4+i/(double)hz});AssertLoadGrips(formal,true);
                    TestAssert.True(!formal.CurrentPlayerFrame.AirJet.Active && formal.CurrentPlayerFrame.Mobility==default,"gap recovery remains visual-only at every sample");
                }
                TestAssert.Near(0,formal.CurrentPose.Body.RotationRadians,.00005f,"no-target gap releases fully in finite time");
                foreach(string name in new[]{"NearLeg","FarLeg"})
                {
                    var leg=formal.GetNode<PlayerFormalLayer>(name);
                    TestAssert.True(leg.LegSoleLocal.DistanceTo(leg.FreeLegSoleLocal)<.003f,"no-target finite recovery ends at free articulated feet");
                }
                formal.ApplyPlayerFrame(replacement with {PresentationTimeSeconds=.7});
                TestAssert.Near(-.03f,formal.CurrentPose.Body.RotationRadians,.00005f,"completed gap cannot replay expired jet into a later ordinary frame");
            }
            foreach(string stop in new[]{"ground","dead","recovery","invalid","invalid-jet","latch","apply","new-jet"})
            {
                for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,1,true));
                formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=0});
                formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=.2});
                formal.ApplyPlayerFrame(gapBase);
                formal.ApplyPlayerFrame(replacement);
                var stopped=replacement with {PresentationTimeSeconds=.4};float wanted=0;
                if(stop=="ground")stopped=stopped with {Base=new(EntityVisualState.Fall,1,true)};
                if(stop=="dead")stopped=stopped with {Locomotion=new(PlayerLocomotionState.Dead,1)};
                if(stop=="recovery")stopped=stopped with {Locomotion=new(PlayerLocomotionState.Recovery,1)};
                if(stop=="invalid")stopped=stopped with {OrdinaryAir=stopped.OrdinaryAir with {TorsoLeanRadians=float.NaN}};
                if(stop=="invalid-jet") {stopped=stopped with {AirJet=new(true,true,Vector2.Right,float.NaN,0,0,0,0)};wanted=-.03f;}
                if(stop=="latch")stopped=stopped with {Action=new(PlayerActionState.ThreadLatch,PlayerActionPhase.Contact,1,false,Vector2.Right)};
                if(stop=="apply")stopped=stopped with {Action=new(PlayerActionState.ThreadApply,PlayerActionPhase.Contact,1,false,Vector2.Right)};
                if(stop=="new-jet") {stopped=stopped with {AirJet=new(true,true,Vector2.Left,-.08f,-.4f,2,3,1)};wanted=-.08f;}
                formal.ApplyPlayerFrame(stopped);
                TestAssert.Near(wanted,formal.CurrentPose.Body.RotationRadians,.00005f,"forced cancellation/new jet cannot replay old load: "+stop);
            }
            // World-sign rebasing of the pure load, not a claim that all turn
            // silhouettes or cached leg axes are world-continuous.
            for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,1,true));
            formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=0});
            formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=.2});formal.ApplyPlayerFrame(gapBase);
            formal.ApplyPlayerFrame(replacement);
            var midpoint=replacement with {PresentationTimeSeconds=.4+1d/15};formal.ApplyPlayerFrame(midpoint);
            float angleBeforeFlip=formal.CurrentPose.Body.RotationRadians;
            formal.ApplyPlayerFrame(midpoint with {Base=new(EntityVisualState.Fall,-1,false),
                OrdinaryAir=midpoint.OrdinaryAir with {TorsoLeanRadians=.03f,FootTrail=-1}});
            TestAssert.Near(-angleBeforeFlip,formal.CurrentPose.Body.RotationRadians,.00005f,"facing remaps cached common load without reversing its world sign");
            AssertLoadGrips(formal,true);
            // State and template change together: the leg template is captured
            // once, independently of the newly accepted common load.
            for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,1,true));
            formal.ApplyPlayerFrame(replacement with {PresentationTimeSeconds=0});
            formal.ApplyPlayerFrame(replacement with {PresentationTimeSeconds=.2});
            formal.ApplyPlayerFrame(replacement);
            var compound=gapBase with {Locomotion=new(PlayerLocomotionState.Apex,1),AirJet=actual with {TorsoLeanRadians=-.08f}};
            formal.ApplyPlayerFrame(compound);
            TestAssert.Near(-.08f,formal.GetNode<Node2D>("NearLeg").Rotation,.00005f,"compound exit starts from old neutral leg template, not a twice-loaded pose");
            formal.ApplyPlayerFrame(compound with {PresentationTimeSeconds=.4+1d/15});
            TestAssert.Near(-.12f,formal.GetNode<Node2D>("NearLeg").Rotation,.00005f,"compound exit advances one half-step of Apex template");
            var control=GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn").Instantiate<PlayerFormalVisual>();
            tree.Root.AddChild(control);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
            try
            {
                for(int i=0;i<24;i++){formal.ApplyFrame(new(EntityVisualState.Idle,1,true));control.ApplyFrame(new(EntityVisualState.Idle,1,true));}
                formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=0});formal.ApplyPlayerFrame(gapBase with {PresentationTimeSeconds=.2});formal.ApplyPlayerFrame(gapBase);
                formal.ApplyPlayerFrame(replacement);formal.ApplyPlayerFrame(midpoint);
                control.ApplyPlayerFrame(replacement with {PresentationTimeSeconds=0});control.ApplyPlayerFrame(replacement with {PresentationTimeSeconds=.2});control.ApplyPlayerFrame(midpoint);
                formal.PlayCue(PresentationCue.Hurt);control.PlayCue(PresentationCue.Hurt);
                TestAssert.Near(control.CurrentPose.Body.RotationRadians,formal.CurrentPose.Body.RotationRadians,.00005f,"cue reset discards bridge history before evaluating current valid ordinary load");
                TestAssert.True(control.CurrentPose.Body.Position.DistanceTo(formal.CurrentPose.Body.Position)<.003f,"cue cannot retain previous jet pelvis shift");
                CheckActionGripHandoff(formal, control);
            }
            finally{control.GetParent()?.RemoveChild(control);control.Free();}
            GD.Print($"AIR_LOAD_HANDOFF_OK cases={cases} real_model_gap=true positive_same_tick_gap=true entry_weights=0,.35,1 rates=30,60,120 midpoint_freeze=true finite_null=true cancellation=8 facing_rebase=true compound_exit=true cue_reset=true " +
                "world_geometry=true action_secondary=true rigid=true grips=true source_unchanged=true human=false");
        }
        finally{formal.GetParent()?.RemoveChild(formal);formal.Free();}
    }

    private static Vector2[] LoadGeometry(PlayerFormalVisual formal)
    {
        var points=new List<Vector2>();
        foreach(string name in Slots)
        {
            var node=formal.GetNode<Node2D>(name);points.Add(node.GlobalPosition);points.Add(node.ToGlobal(Vector2.Right));points.Add(node.ToGlobal(Vector2.Up));
        }
        foreach(string name in new[]{"NearLeg","FarLeg"})
        {
            var leg=formal.GetNode<PlayerFormalLayer>(name);points.Add(leg.ToGlobal(leg.LegAnkleLocal));points.Add(leg.ToGlobal(leg.LegSoleLocal));
        }
        return points.ToArray();
    }

    private static void CheckActionGripHandoff(PlayerFormalVisual formal, PlayerFormalVisual control)
    {
        var weapon = formal.GetNode<PlayerFormalLayer>("Weapon");
        var near = formal.GetNode<PlayerFormalLayer>("NearArm");
        var far = formal.GetNode<PlayerFormalLayer>("FarArm");
        Vector2 free = new(0f, 19f);
        int cases = 0;
        foreach (int hz in new[] { 30, 60, 120 })
        foreach (int facing in new[] { 1, -1 })
        {
            for (int i = 0; i < 24; i++)
            {
                formal.ApplyFrame(new(EntityVisualState.Idle, facing, true));
                control.ApplyFrame(new(EntityVisualState.Idle, facing, true));
            }
            Vector2 aim = new(facing, 0);
            var secondary = new SecondaryMotionFrame(PoseTransform2D.Identity with { Position = new(1, 2), RotationRadians = .03f },
                PoseTransform2D.Identity with { Position = new(2, -1), RotationRadians = -.02f },
                PoseTransform2D.Identity with { RotationRadians = .04f }, PoseTransform2D.Identity with { Position = new(1, -1) });
            var jet = new PlayerPresentationFrame(new(EntityVisualState.Fall, facing, false), new(PlayerLocomotionState.Fall, 1),
                new(PlayerActionState.AirAttack, PlayerActionPhase.Contact, 1, false, aim), secondary)
            { AirJet = new(true, true, aim, .12f * facing, .6f, 5, 4, -1.5f * facing), PresentationTimeSeconds = 0 };
            Apply(jet); Apply(jet with { PresentationTimeSeconds = .2 });
            Apply(jet with { PresentationTimeSeconds = .4 });
            AssertLoadGrips(formal, true);

            var shield = jet with { Action = new(PlayerActionState.Shield, PlayerActionPhase.Contact, 1, false, aim),
                AirJet = default, OrdinaryAir = new(true, false, false, 1, -.03f * facing, -.2f, -1.5f, -.8f, 2 * facing),
                PresentationTimeSeconds = .4 };
            Apply(shield);
            // A terminal Shield frame intentionally changes authored pose; only its carrier must hold.
            CheckLoad(.12f * facing, "shield entry");
            AssertLoadGrips(formal, true);
            Repeat(shield, "shield entry repeat");
            int releaseFrames = hz / 15;
            float prior = 1;
            for (int i = 1; i <= releaseFrames; i++)
            {
                var sample = shield with { PresentationTimeSeconds = .4 + i / (double)hz };
                Apply(sample);
                float grip = OffhandFraction();
                TestAssert.True(grip < prior && !weapon.WeaponSecondaryGripContact,
                    $"AIR_ACTION_GRIP_HANDOFF: offhand must release over elapsed time; hz={hz}, facing={facing}, i={i}, grip={grip}, prior={prior}");
                if (i < releaseFrames) TestAssert.True(grip > .001f && grip < .999f, "release must visibly traverse intermediate poses");
                if (i == 1) Repeat(sample, "partial release repeat");
                prior = grip;
            }
            CheckLoad(.045f * facing, "shield release / carrier midpoint");
            TestAssert.True(far.ArmWristLocal.DistanceTo(free) < .003f, "shield offhand actually reaches its free wrist");

            var released = shield with { Action = PlayerActionFrame.None, PresentationTimeSeconds = .4 + 1d / 15 };
            Apply(released);
            TestAssert.True(far.ArmWristLocal.DistanceTo(free) < .003f && !weapon.WeaponSecondaryGripContact,
                "None starts at the displayed free hand without instant re-grip");
            Repeat(released, "re-grip entry repeat");
            prior = 0;
            for (int i = 1; i <= hz / 6; i++)
            {
                var sample = released with { PresentationTimeSeconds = released.PresentationTimeSeconds + i / (double)hz };
                Apply(sample);
                float grip = OffhandFraction();
                TestAssert.True(grip > prior, "None must progressively re-grip, not freeze the offhand");
                if (i < hz / 6) TestAssert.True(grip < .999f && !weapon.WeaponSecondaryGripContact,
                    "secondary contact must not be published while the hand is still returning");
                if (i == 1) Repeat(sample, "partial re-grip repeat");
                if (i == releaseFrames) CheckLoad(-.03f * facing, "carrier settled during re-grip");
                prior = grip;
            }
            AssertLoadGrips(formal, true);
            cases++;
        }
        GD.Print($"AIR_ACTION_GRIP_HANDOFF_OK cases={cases} rates=30,60,120 facings=2 primary_bitmap=true offhand_release_regrip=true same_clock_geometry=true isolated_load=true actual_input=false human=false");

        void Apply(PlayerPresentationFrame frame)
        {
            formal.ApplyPlayerFrame(frame);
            control.ApplyPlayerFrame(frame with { AirJet = frame.AirJet with { TorsoLeanRadians = 0, PelvisLag = 0 },
                OrdinaryAir = frame.OrdinaryAir with { TorsoLeanRadians = 0, PelvisOffset = 0 } });
            AssertLoadGrips(formal, false);
            TestAssert.Equal(frame, formal.CurrentPlayerFrame, "visual ownership change cannot rewrite input frames or lease");
        }
        void CheckLoad(float expected, string label) => TestAssert.Near(expected,
            formal.CurrentPose.Body.RotationRadians - control.CurrentPose.Body.RotationRadians, .00005f, label);
        float OffhandFraction()
        {
            Vector2 pixel = weapon.WeaponDrawRect.Position + (new Vector2(548, 798) - weapon.WeaponSourceRect.Position) * (25f / 287f);
            Vector2 axis = far.ToLocal(weapon.ToGlobal(pixel)) - free;
            TestAssert.True(axis.LengthSquared() > 1f, "offhand free and contact endpoints must be distinct");
            Vector2 offset = far.ArmWristLocal - free;
            float fraction = offset.Dot(axis) / axis.LengthSquared();
            TestAssert.True(offset.DistanceTo(axis * fraction) < .003f && fraction >= -.001f && fraction <= 1.001f,
                "offhand must remain on the free-to-bitmap contact segment");
            return fraction;
        }
        void Repeat(PlayerPresentationFrame frame, string label)
        {
            var bodyAndFeet = LoadGeometry(formal);
            var wristsAndElbows = new[] { near.ToGlobal(near.ArmWristLocal), near.ToGlobal(near.ArmElbowLocal),
                far.ToGlobal(far.ArmWristLocal), far.ToGlobal(far.ArmElbowLocal) };
            bool contact = weapon.WeaponSecondaryGripContact;
            Apply(frame);
            AssertLoadHeld(formal, bodyAndFeet, label);
            var after = new[] { near.ToGlobal(near.ArmWristLocal), near.ToGlobal(near.ArmElbowLocal),
                far.ToGlobal(far.ArmWristLocal), far.ToGlobal(far.ArmElbowLocal) };
            for (int i = 0; i < after.Length; i++)
                TestAssert.True(wristsAndElbows[i].DistanceTo(after[i]) < .003f, $"{label}: wrist/elbow {i} moved at the same clock");
            TestAssert.Equal(contact, weapon.WeaponSecondaryGripContact, $"{label}: contact changed at the same clock");
        }
    }
    private static void AssertLoadHeld(PlayerFormalVisual formal,Vector2[] before,string label)
    {
        var after=LoadGeometry(formal);float maximum=0;
        for(int i=0;i<before.Length;i++)maximum=Mathf.Max(maximum,before[i].DistanceTo(after[i]));
        TestAssert.True(maximum<.003f,$"AIR_LOAD_HANDOFF_RED: {label} must retain actual nonzero ten-slot/world-foot pose, max_delta={maximum}");
    }
    private static void AssertLoadGrips(PlayerFormalVisual formal,bool secondary)
    {
        foreach(string name in new[]{"NearLeg","FarLeg"})AssertRigid(formal.GetNode<PlayerFormalLayer>(name));
        foreach(string name in Slots)TestAssert.Equal(Vector2.One,formal.GetNode<Node2D>(name).Scale,"air load cannot stretch image layers");
        var weapon=formal.GetNode<PlayerFormalLayer>("Weapon");
        Vector2 Grip(float x)=>weapon.ToGlobal(weapon.WeaponDrawRect.Position+(new Vector2(x,798)-weapon.WeaponSourceRect.Position)*(25f/287f));
        var near=formal.GetNode<PlayerFormalLayer>("NearArm");var far=formal.GetNode<PlayerFormalLayer>("FarArm");
        TestAssert.True(Grip(835).DistanceTo(near.ToGlobal(near.ArmWristLocal))<.003f,"nonzero bridge keeps actual bitmap primary grip");
        if(secondary)TestAssert.True(Grip(548).DistanceTo(far.ToGlobal(far.ArmWristLocal))<.003f && weapon.WeaponSecondaryGripContact,
            "nonzero bridge keeps secondary grip only for actions that own it");
    }

    public static async Task RunCarrierHandoff(SceneTree tree)
    {
        var formal=GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn").Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(formal);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        try
        {
            int cases=0;
            foreach(int facing in new[]{1,-1})
            foreach(var state in new[]{PlayerLocomotionState.JumpStart,PlayerLocomotionState.Rise,
                PlayerLocomotionState.Apex,PlayerLocomotionState.Fall})
            {
                for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,facing,true));
                var jet=new PlayerPresentationFrame(new(EntityVisualState.Fall,facing,false),new(state,1),
                    PlayerActionFrame.None,SecondaryMotionFrame.Identity)
                    {AirJet=new(true,true,Vector2.Right,0,0,0,0,0),PresentationTimeSeconds=0};
                formal.ApplyPlayerFrame(jet);
                formal.ApplyPlayerFrame(jet with {PresentationTimeSeconds=.2});
                jet=jet with {PresentationTimeSeconds=.4};formal.ApplyPlayerFrame(jet);
                var before=CaptureCarrier(formal);var root=formal.GlobalTransform;
                var ordinary=jet with {AirJet=default,OrdinaryAir=new(true,false,false,0,0,0,0,0,0)};
                formal.ApplyPlayerFrame(ordinary);
                AssertCarrierHeld(formal,before,"enter",state,facing);
                formal.ApplyPlayerFrame(ordinary);
                AssertCarrierHeld(formal,before,"repeat",state,facing);
                formal.ApplyPlayerFrame(ordinary with {PresentationTimeSeconds=.39});
                AssertCarrierHeld(formal,before,"rollback",state,facing);
                formal.ApplyPlayerFrame(ordinary with {PresentationTimeSeconds=.7,OrdinaryAir=ordinary.OrdinaryAir with {EntryWeight=1}});
                foreach(string name in new[]{"NearLeg","FarLeg"})
                    TestAssert.Near(0,formal.GetNode<Node2D>(name).Rotation,.002f,"ordinary template reaches neutral without whole-leg rotation");
                var settled=CaptureCarrier(formal);
                formal.ApplyPlayerFrame(jet with {PresentationTimeSeconds=.7});
                AssertCarrierHeld(formal,settled,"exit",state,facing);
                formal.ApplyPlayerFrame(jet with {PresentationTimeSeconds=1});
                var returned=CaptureCarrier(formal);
                foreach(string name in new[]{"NearLeg","FarLeg"})
                {
                    TestAssert.Near(before[name].Rotation,returned[name].Rotation,.002f,"legacy air template returns after timed blend");
                    AssertRigid(formal.GetNode<PlayerFormalLayer>(name));
                }
                TestAssert.Equal(root,formal.GlobalTransform,"template transition never moves or scales the rig root");
                cases++;
            }
            // Rejected raw Active must not become a transition boundary. JumpStart
            // deliberately changes phase, making an erroneous reset observable.
            foreach(int rejected in new[]{0,1,2,3})
            {
                for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,1,true));
                var initial=new PlayerPresentationFrame(new(EntityVisualState.Fall,1,false),new(PlayerLocomotionState.JumpStart,.2f),
                    PlayerActionFrame.None,SecondaryMotionFrame.Identity){PresentationTimeSeconds=0};
                if(rejected==0)initial=initial with {Base=new(EntityVisualState.Fall,1,true)};
                if(rejected==1)initial=initial with {AirJet=new(true,true,Vector2.Right,0,0,0,0,0)};
                if(rejected==2)initial=initial with {Action=new(PlayerActionState.ThreadLatch,PlayerActionPhase.Contact,1,false)};
                formal.ApplyPlayerFrame(initial);
                formal.ApplyPlayerFrame(initial with {PresentationTimeSeconds=.2});
                formal.ApplyPlayerFrame(initial with {PresentationTimeSeconds=.4});
                var changed=initial with {Locomotion=new(PlayerLocomotionState.JumpStart,.8f),PresentationTimeSeconds=.5,
                    OrdinaryAir=new(true,false,false,0,0,0,0,0,0)};
                if(rejected==3)changed=changed with {OrdinaryAir=changed.OrdinaryAir with {TorsoLeanRadians=float.NaN}};
                formal.ApplyPlayerFrame(changed);
                TestAssert.Near(-.28f*.8f,formal.GetNode<Node2D>("NearLeg").Rotation,.002f,
                    "rejected ordinary Active cannot reset actual legacy phase progression");
            }
            GD.Print($"AIR_CARRIER_TEMPLATE_OK cases={cases} rejected=4 world_feet=true repeat_rollback=true reversible=true rigid=true root_unchanged=true nonzero_postmix_accepted=false human=false");
        }
        finally{formal.GetParent()?.RemoveChild(formal);formal.Free();}
    }

    private static Dictionary<string,CarrierFoot> CaptureCarrier(PlayerFormalVisual formal)
    {
        var result=new Dictionary<string,CarrierFoot>();
        foreach(string name in new[]{"NearLeg","FarLeg"})
        {
            var leg=formal.GetNode<PlayerFormalLayer>(name);
            result.Add(name,new(leg.Position,leg.Rotation,leg.ToGlobal(leg.LegAnkleLocal),leg.ToGlobal(leg.LegSoleLocal)));
        }
        return result;
    }

    private static void AssertCarrierHeld(PlayerFormalVisual formal,Dictionary<string,CarrierFoot> before,
        string boundary,PlayerLocomotionState state,int facing)
    {
        var after=CaptureCarrier(formal);
        foreach(string name in new[]{"NearLeg","FarLeg"})
        {
            var old=before[name];var current=after[name];
            GD.Print($"AIR_CARRIER_TEMPLATE_OBS boundary={boundary} state={state} facing={facing} leg={name} " +
                $"rotation_delta={current.Rotation-old.Rotation} ankle_delta={current.Ankle.DistanceTo(old.Ankle)} sole_delta={current.Sole.DistanceTo(old.Sole)}");
            TestAssert.True(current.Position.DistanceTo(old.Position)<.002f && Mathf.Abs(current.Rotation-old.Rotation)<.002f &&
                current.Ankle.DistanceTo(old.Ankle)<.002f && current.Sole.DistanceTo(old.Sole)<.002f,
                $"AIR_CARRIER_TEMPLATE_RED: zero-time {boundary} must preserve actual {state}/{facing}/{name} world foot, not only local sole");
            AssertRigid(formal.GetNode<PlayerFormalLayer>(name));
        }
    }

    public static async Task RunDraw(SceneTree tree)
    {
        var formal=GD.Load<PackedScene>("res://scenes/presentation/PlayerFormalVisual.tscn").Instantiate<PlayerFormalVisual>();
        tree.Root.AddChild(formal);await tree.ToSignal(tree,SceneTree.SignalName.ProcessFrame);
        try
        {
            int cases=0,attachments=0,soles=0;
            foreach(int facing in new[]{1,-1})
            foreach(var action in new[]{PlayerActionFrame.None,
                new PlayerActionFrame(PlayerActionState.AirAttack,PlayerActionPhase.Burst,.4f,false,Vector2.Right),
                new PlayerActionFrame(PlayerActionState.AirAttack,PlayerActionPhase.Contact,1,false,Vector2.Right),
                new PlayerActionFrame(PlayerActionState.Shield,PlayerActionPhase.Contact,1,false,Vector2.Right)})
            {
                for(int i=0;i<24;i++)formal.ApplyFrame(new(EntityVisualState.Idle,facing,true));
                var frame=new PlayerPresentationFrame(new(EntityVisualState.Rise,facing,false),
                    new(PlayerLocomotionState.Rise,1),action,SecondaryMotionFrame.Identity)
                {
                    OrdinaryAir=new(true,false,true,1,0,0,0,0,0),PresentationTimeSeconds=0,
                    Gait=new(true,.5f,0,new(999,999),new(-999,999),true,true)
                };
                formal.ApplyPlayerFrame(frame);
                formal.ApplyPlayerFrame(frame with {PresentationTimeSeconds=.2});
                frame=frame with {PresentationTimeSeconds=.4};formal.ApplyPlayerFrame(frame);
                var original=new Dictionary<string,(Vector2 Position,float Rotation,Vector2 RestPosition)>();
                for(int i=0;i<Slots.Length;i++)
                {
                    var node=formal.GetNode<Node2D>(Slots[i]);
                    original.Add(Slots[i],(node.Position,node.Rotation,node.Position-formal.CurrentPose.Get((PlayerPoseSlot)i).Position));
                }
                Vector2 pivot=(original["NearLeg"].RestPosition+original["FarLeg"].RestPosition)*.5f;
                Vector2 root=formal.GlobalPosition;
                var air=new PlayerOrdinaryAirFrame(true,false,true,1,.04f*facing,.5f,8,6,-2*facing);
                formal.ApplyPlayerFrame(frame with {OrdinaryAir=air});
                foreach(string name in Slots)
                {
                    var node=formal.GetNode<Node2D>(name);var old=original[name];
                    Vector2 wanted=pivot+(old.Position-pivot).Rotated(air.TorsoLeanRadians)+new Vector2(0,.5f);
                    TestAssert.True(node.Position.DistanceTo(wanted)<.002f,
                        "ORDINARY_AIR_DRAW_RED: actual ordinary-air load must move every attachment coherently: "+name);
                    TestAssert.Near(old.Rotation+air.TorsoLeanRadians,node.Rotation,.002f,"ordinary air common attachment rotation");
                    TestAssert.Equal(Vector2.One,node.Scale,"ordinary air cannot stretch body images");attachments++;
                }
                TestAssert.Equal(root,formal.GlobalPosition,"ordinary air pose never moves collision/root");
                foreach(string name in new[]{"NearLeg","FarLeg"})
                {
                    var leg=formal.GetNode<PlayerFormalLayer>(name);
                    var wanted=leg.FreeLegSoleLocal+new Vector2(air.FootTrail,name=="NearLeg"?-8:-6);
                    TestAssert.True(leg.LegSoleLocal.DistanceTo(wanted)<.002f,
                        "ORDINARY_AIR_DRAW_RED: tuck/reach must reach actual joint/sole Draw, not stale ground support");
                    AssertRigid(leg);soles++;
                }
                foreach(var state in new[]{PlayerLocomotionState.Dead,PlayerLocomotionState.Recovery})
                {
                    var end=frame with {Locomotion=new(state,1),OrdinaryAir=default};
                    formal.ApplyPlayerFrame(end);var baseline=formal.CurrentPose;
                    formal.ApplyPlayerFrame(end with {OrdinaryAir=air});
                    TestAssert.Equal(baseline,formal.CurrentPose,"death/recovery rejects retained ordinary-air load");
                }
                var ground=frame with {Base=new(EntityVisualState.Idle,facing,true),Gait=default};
                formal.ApplyPlayerFrame(ground);var grounded=formal.CurrentPose;
                formal.ApplyPlayerFrame(ground with {OrdinaryAir=air});
                TestAssert.Equal(grounded,formal.CurrentPose,"ground contact revokes free-air common load");
                var jet=frame with {OrdinaryAir=default,AirJet=new(true,true,Vector2.Right,.06f,.2f,4,3,-1)};
                formal.ApplyPlayerFrame(jet);var jetOnly=formal.CurrentPose;
                formal.ApplyPlayerFrame(jet with {OrdinaryAir=air});
                TestAssert.Equal(jetOnly,formal.CurrentPose,"measured airjet never doubles ordinary-air load");
                cases++;
            }
            formal.ApplyFrame(new(EntityVisualState.Idle,1,true));
            var near=formal.GetNode<PlayerFormalLayer>("NearLeg");
            Vector2 priorUp=new Vector2(-.2f,-1).Normalized();
            near.SetLegSoleLocal(new(3,23),priorUp);
            Vector2 priorAnkle=near.LegAnkleLocal;
            var entry=new PlayerPresentationFrame(new(EntityVisualState.Rise,1,false),new(PlayerLocomotionState.Rise,1),
                PlayerActionFrame.None,SecondaryMotionFrame.Identity)
                {OrdinaryAir=new(true,true,true,0,0,0,10,8,-2),PresentationTimeSeconds=0};
            formal.ApplyPlayerFrame(entry);
            TestAssert.True(near.LegAnkleLocal.DistanceTo(priorAnkle)<.002f,
                "ORDINARY_AIR_ENTRY_RED: takeoff must retain the displayed local ankle before easing into tuck");
            var firstSole=near.LegSoleLocal;
            formal.ApplyPlayerFrame(entry);
            TestAssert.Equal(firstSole,near.LegSoleLocal,"zero-time repeated Draw does not advance entry geometry");
            formal.ApplyPlayerFrame(entry with {OrdinaryAir=entry.OrdinaryAir with {EntryWeight=.5f}});
            Vector2 target=near.FreeLegSoleLocal+new Vector2(-2,-10)+Vector2.Up*4;
            TestAssert.True(near.LegAnkleLocal.DistanceTo(priorAnkle.Lerp(target,.5f))<.002f,
                "entry blends reachable ankles, not pinned old world soles or translated leg images");
            AssertRigid(near);
            GD.Print($"ORDINARY_AIR_DRAW_OK cases={cases} attachments={attachments} soles={soles} entry_continuous=true rigid_links=true priorities=true headless_geometry=true human=false");
        }
        finally{formal.GetParent()?.RemoveChild(formal);formal.Free();}
    }

    private static void AssertRigid(PlayerFormalLayer leg)
    {
        TestAssert.Near(18,leg.LegHipLocal.DistanceTo(leg.LegKneeLocal),.002f,"ordinary upper leg rigid");
        TestAssert.Near(18,leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal),.002f,"ordinary lower leg rigid");
        TestAssert.True(!leg.LegReachClamped,"ordinary joint target cannot be concealed by reach clamp");
        TestAssert.Equal(Vector2.One,leg.Scale,"ordinary leg has no image stretch");
    }

    private readonly record struct Foot(Vector2 Sole,Vector2 Free,Vector2 World,float Upper,float Lower,bool Clamped,Vector2 Scale);
    private readonly record struct AirSample(PlayerPresentationFrame Frame,Vector2 Velocity,bool Grounded,
        ulong Sequence,int Jumps,int Dashes,Foot Near,Foot Far);

    public static async Task RunJump(SceneTree tree)
    {
        int samples=0,landings=0;
        foreach(int facing in new[]{1,-1})
        foreach(bool fullHold in new[]{true,false})
        {
            using var fixture=new Fixture(tree);await fixture.Start();
            ThrusterPlayerRuntimeTests.KeyEvent(facing<0?Key.A:Key.D,true);await fixture.Frames(6);
            ThrusterPlayerRuntimeTests.KeyEvent(facing<0?Key.A:Key.D,false);await fixture.Frames(12);
            TestAssert.True(fixture.Player.IsOnFloor() && fixture.Presentation.LastPlayerFrame.Base.Facing==facing,
                "ordinary jump direction must be established by real ground movement, not transform injection");
            var captured=new List<AirSample>();
            void Published(PlayerPresentationFrame frame)=>captured.Add(Capture(fixture,frame));
            fixture.Presentation.FramePublished+=Published;
            try
            {
                ThrusterPlayerRuntimeTests.KeyEvent(Key.Space,true);await fixture.Frames(1);
                bool holding=fullHold;
                if(!fullHold)ThrusterPlayerRuntimeTests.KeyEvent(Key.Space,false);
                bool airborne=false,landed=false;
                for(int i=0;i<100 && !landed;i++)
                {
                    await fixture.Frames(1);
                    if(holding && fixture.Player.Velocity.Y>=0f){ThrusterPlayerRuntimeTests.KeyEvent(Key.Space,false);holding=false;}
                    airborne|=!fixture.Player.IsOnFloor();landed=airborne && fixture.Player.IsOnFloor();
                }
                ThrusterPlayerRuntimeTests.KeyEvent(Key.Space,false);
                await fixture.Frames(4);
                TestAssert.True(airborne && landed && fixture.Motor.AcceptedJumpCount==1 &&
                    fixture.Motor.AcceptedJumpSequence==1 && fixture.Motor.AcceptedDashCount==0,
                    "ORDINARY_AIR_NATIVE_RED: one actual Space must complete ordinary jump and physical landing without a Dash/second jump");
                AirSample? rise=null,fall=null,apex=null,contact=null;int starts=0,measured=0;
                foreach(var sample in captured)
                {
                    if(sample.Frame.OrdinaryAir.JumpStarted)starts++;
                    var air=sample.Frame.OrdinaryAir;
                    if(air.Active && air.EntryWeight>=.999f)
                    {
                        TestAssert.True(air.JumpArc && !sample.Grounded && sample.Sequence==1 && sample.Jumps==1 && sample.Dashes==0,
                            "ordinary measured arc must keep actual single accepted cause");
                        foreach(var foot in new[]{(Value:sample.Near,Lift:air.NearLift),(Value:sample.Far,Lift:air.FarLift)})
                        {
                            TestAssert.True(foot.Value.Sole.DistanceTo(foot.Value.Free+new Vector2(air.FootTrail,-foot.Lift))<.003f,
                                "ORDINARY_AIR_NATIVE_RED: same published sample must reach actual tuck/unfold foot Draw");
                            TestAssert.Near(18,foot.Value.Upper,.003f,"real ordinary upper link rigid");
                            TestAssert.Near(18,foot.Value.Lower,.003f,"real ordinary lower link rigid");
                            TestAssert.True(!foot.Value.Clamped && foot.Value.Scale==Vector2.One,"real ordinary joints cannot clamp/stretch");
                        }
                        if(sample.Velocity.Y< -150 && (!rise.HasValue || air.NearLift>rise.Value.Frame.OrdinaryAir.NearLift))rise=sample;
                        if(Mathf.Abs(sample.Velocity.Y)<60)apex=sample;
                        if(sample.Velocity.Y>200)fall=sample;
                        measured++;
                    }
                    if(sample.Grounded && sample.Frame.LandingLoad.Active && sample.Frame.Gait.Weight>=.999f)contact=sample;
                }
                GD.Print($"ORDINARY_AIR_NATIVE_OBS facing={facing} full_hold={fullHold} starts={starts} measured={measured} rise={rise.HasValue} apex={apex.HasValue} fall={fall.HasValue} contact={contact.HasValue} jumps={fixture.Motor.AcceptedJumpCount} sequence={fixture.Motor.AcceptedJumpSequence} landing_count={fixture.Motor.LandingCount} impact_speed={fixture.Motor.LastLandingImpact.Speed} impact_reliable={fixture.Motor.LastLandingImpact.IsReliable}");
                TestAssert.True(starts==1 && measured>12 && (!fullHold || rise.HasValue) && fall.HasValue && apex.HasValue && contact.HasValue,
                    "ORDINARY_AIR_NATIVE_RED: fresh cause and complete rise/apex/fall/contact phases must all be observed");
                if(fullHold)TestAssert.True(rise.Value.Frame.OrdinaryAir.NearLift>fall.Value.Frame.OrdinaryAir.NearLift+1,
                    "actual falling legs unfold from rising tuck, not one fixed air image");
                var landing=contact.Value;
                TestAssert.True(!landing.Frame.OrdinaryAir.Active &&
                    landing.Near.World.DistanceTo(landing.Frame.Gait.NearSoleWorld)<.03f &&
                    landing.Far.World.DistanceTo(landing.Frame.Gait.FarSoleWorld)<.03f,
                    "measured landing support takes both real feet back from free flight");
                TestAssert.Equal(0,fixture.Presentation.FrameObserverFailureCount,"native sample capture cannot hide faults");
                GD.Print($"ORDINARY_AIR_NATIVE_FACTS facing={facing} full_hold={fullHold} samples={measured} starts={starts} rise_lift={rise?.Frame.OrdinaryAir.NearLift} fall_lift={fall.Value.Frame.OrdinaryAir.NearLift} contact=true sequence=1 jumps=1 dashes=0");
                samples+=measured;landings++;
            }
            finally{fixture.Presentation.FramePublished-=Published;}
        }
        using(var fixture=new Fixture(tree))
        {
            await fixture.Start();var captured=new List<AirSample>();
            void Published(PlayerPresentationFrame frame)=>captured.Add(Capture(fixture,frame));
            fixture.Presentation.FramePublished+=Published;
            try
            {
                ThrusterPlayerRuntimeTests.KeyEvent(Key.D,true);
                for(int i=0;i<90 && fixture.Player.IsOnFloor();i++)await fixture.Frames(1);
                ThrusterPlayerRuntimeTests.KeyEvent(Key.D,false);
                TestAssert.True(!fixture.Player.IsOnFloor() && fixture.Motor.AcceptedJumpCount==0,"coyote fixture really walked off before Space");
                await fixture.Frames(1);
                ThrusterPlayerRuntimeTests.KeyEvent(Key.Space,true);await fixture.Frames(1);
                ThrusterPlayerRuntimeTests.KeyEvent(Key.Space,false);await fixture.Frames(4);
                bool acceptedFromAir=false;
                foreach(var sample in captured)
                    acceptedFromAir|=sample.Frame.OrdinaryAir.JumpStarted && sample.Frame.OrdinaryAir.JumpArc &&
                        sample.Frame.Locomotion.State==PlayerLocomotionState.JumpStart && !sample.Grounded;
                TestAssert.True(fixture.Motor.AcceptedJumpCount==1 && fixture.Motor.AcceptedDashCount==0 && acceptedFromAir,
                    "ORDINARY_AIR_COYOTE_RED: actual accepted coyote jump gets one launch, unlike prior walk-off");
                TestAssert.Equal(0,fixture.Presentation.FrameObserverFailureCount,"coyote capture cannot hide faults");
            }
            finally{fixture.Presentation.FramePublished-=Published;}
        }
        GD.Print($"ORDINARY_AIR_NATIVE_OK facings=2 landings={landings} full_jumps=2 short_jumps=2 coyote=true real_space=true same_sample=true rigid_draw=true no_velocity_injection=true human=false");
    }

    private static AirSample Capture(Fixture fixture,PlayerPresentationFrame frame)
    {
        var formal=(PlayerFormalVisual)fixture.Presentation.ActiveVisual;
        Foot Read(string name)
        {
            var leg=formal.GetNode<PlayerFormalLayer>(name);
            return new(leg.LegSoleLocal,leg.FreeLegSoleLocal,leg.ToGlobal(leg.LegSoleLocal),
                leg.LegHipLocal.DistanceTo(leg.LegKneeLocal),leg.LegKneeLocal.DistanceTo(leg.LegAnkleLocal),leg.LegReachClamped,leg.Scale);
        }
        return new(frame,fixture.Player.Velocity,fixture.Player.IsOnFloor(),fixture.Motor.AcceptedJumpSequence,
            fixture.Motor.AcceptedJumpCount,fixture.Motor.AcceptedDashCount,Read("NearLeg"),Read("FarLeg"));
    }

    public static async Task RunCause(SceneTree tree)
    {
        using var fixture=new Fixture(tree);
        await fixture.Start();
        var departures=new List<(PlayerPresentationFrame Frame,int Jumps,int Dashes)>();
        bool hadSupport=true;
        void Published(PlayerPresentationFrame frame)
        {
            bool grounded=fixture.Player.IsOnFloor();
            if(hadSupport && !grounded)
                departures.Add((frame,fixture.Motor.AcceptedJumpCount,fixture.Motor.AcceptedDashCount));
            hadSupport=grounded;
        }
        fixture.Presentation.FramePublished+=Published;
        try
        {
            ThrusterPlayerRuntimeTests.KeyEvent(Key.D,true);
            for(int i=0;i<90 && departures.Count==0;i++)await fixture.Frames(1);
            ThrusterPlayerRuntimeTests.KeyEvent(Key.D,false);
            TestAssert.True(departures.Count==1,"finite-ledge fixture must actually lose its only floor");
            var departed=departures[0];
            GD.Print($"ORDINARY_AIR_CAUSE_FACTS kind=walk_off jumps={departed.Jumps} dashes={departed.Dashes} state={departed.Frame.Locomotion.State} real_keyboard=true velocity_injection=false human=false");
            TestAssert.True(departed.Jumps==0 && departed.Dashes==0,
                "walk-off cannot borrow a jump/dash to satisfy cause proof");
            TestAssert.True(departed.Frame.Locomotion.State!=PlayerLocomotionState.JumpStart,
                "ORDINARY_AIR_CAUSE_RED: real walk-off must not fabricate an accepted jump launch");
            TestAssert.Equal(0,fixture.Presentation.FrameObserverFailureCount,"cause proof cannot hide observer exceptions");
        }
        finally{fixture.Presentation.FramePublished-=Published;}
        GD.Print("ORDINARY_AIR_CAUSE_OK walk_off=true real_keyboard=true no_jump=true no_dash=true human=false");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SceneTree _tree;
        private readonly RectangleShape2D _floorShape=new(){Size=new(200,20)};
        internal Node2D Root {get;}=new(){Name="OrdinaryAirMotionFixture"};
        internal CharacterBody2D Player {get;}
        internal PlayerMotor Motor=>Player.GetNode<PlayerMotor>("Motor");
        internal PlayerPresentationCoordinator Presentation=>Player.GetNode<PlayerPresentationCoordinator>("Presentation");
        internal Fixture(SceneTree tree)
        {
            _tree=tree;
            var floor=new StaticBody2D {Position=new(0,60),CollisionLayer=CombatCollisionLayers.World};
            floor.AddChild(new CollisionShape2D {Shape=_floorShape});Root.AddChild(floor);
            Player=GD.Load<PackedScene>("res://scenes/player/Player.tscn").Instantiate<CharacterBody2D>();
            Player.Position=new(0,14);
            Player.GetNode<Camera2D>("Camera2D").Enabled=false;Root.AddChild(Player);
        }
        internal async Task Start()
        {
            ThrusterPlayerRuntimeTests.Release();_tree.Root.AddChild(Root);await Frames(8);
            TestAssert.True(Player.IsOnFloor(),"ordinary-air fixture starts on real finite support");
            TestAssert.Equal(0,Motor.AcceptedJumpCount,"ordinary-air fixture starts with no jump");
        }
        internal Task Frames(int count)=>ThrusterPlayerRuntimeTests.Frames(_tree,Player.GetNode<PlayerInputReader>("Input"),count);
        public void Dispose()
        {
            ThrusterPlayerRuntimeTests.Release();_tree.Paused=false;
            if(GodotObject.IsInstanceValid(Root))Root.Free();_floorShape.Dispose();
        }
    }
}
