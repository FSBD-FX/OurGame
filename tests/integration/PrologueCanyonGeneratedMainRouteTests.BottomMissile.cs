using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Enemies.Ranged;
using Knight.Player;
using Knight.Presentation;

namespace Knight.Tests.Integration;

public static partial class PrologueCanyonGeneratedMainRouteTests
{
    internal static bool Task10BottomControlActorRootReadable(Node actors, SceneTree tree)
    {
        if (!GodotObject.IsInstanceValid(tree) || tree.Paused || !GodotObject.IsInstanceValid(actors) ||
            !actors.IsInsideTree() || actors.GetTree() != tree) return false;
        // Match every early invalidation boundary of the underlying observer:
        // a still-live child can have a queued ancestor before delete flush.
        for (Node ancestor = actors; ancestor != null; ancestor = ancestor.GetParent())
            if (!GodotObject.IsInstanceValid(ancestor) || ancestor.IsQueuedForDeletion()) return false;
        return true;
    }

    internal static bool Task10BottomControlPreparationQuiet(bool observationValid, Task10EncounterDanger danger)
        => observationValid && !danger.Threat && danger.MissingGuardSourceCount == 0 &&
           !danger.NearContactUnknownActive && !danger.NearContactOverlappingActive &&
           danger.CandidateObserverFaults == 0; // Instantaneous only, never a future-safe allowance.

    internal static Vector2 Task10BottomVerticalGuardIntent(Vector2 sourceDelta, float clearance, float bodyHeight)
    {
        float squared=sourceDelta.LengthSquared();
        if(!sourceDelta.IsFinite() || !float.IsFinite(squared) || squared<=0 ||
            !float.IsFinite(clearance) || clearance<0 || !float.IsFinite(bodyHeight) || bodyHeight<=0 ||
            clearance>2f*bodyHeight || sourceDelta.Y==0) return Vector2.Zero;
        Vector2 direction=sourceDelta.Y>0 ? Vector2.Down : Vector2.Up;
        float threshold=Mathf.Cos(Mathf.DegToRad(PlayerShieldSettings.Approved.FrontArcDegrees*.5f));
        return direction.Dot(sourceDelta.Normalized())>=threshold-.0001f ? direction : Vector2.Zero;
    }

    private static Vector2 ReadTask10BottomFlightGuard(Node actors,CharacterBody2D player,
        PlayerShieldController shield,List<AttackTelegraphVisual> leaves,
        out ulong ownerId,out ulong attackId,out ulong projectileId)
    {
        ownerId=attackId=projectileId=0;
        try
        {
            Task10GuardCapsule? capsule=Task10CurrentGuardCapsule(player);
            if(!capsule.HasValue) return Vector2.Zero;
            Task10EncounterDanger danger=Task10ObserveEncounterDanger(actors,
                "canyon.v2.encounter.steam-valves",null,false,leaves,guardCapsule:capsule);
            if(danger.Phase!=AttackPhase.Active || !Task10MixedDefendWindow(shield,danger)) return Vector2.Zero;
            var children=actors.GetChildren();
            using Godot.Collections.Array backing=(Godot.Collections.Array)children;
            foreach(Node child in children)
            {
                if(child is not RangedWeaponEnemy owner || owner.GetInstanceId()!=danger.SourceActorId ||
                    owner.Role!=RangedWeaponRole.Missile || !owner.HasPresentationAuthority ||
                    owner.GetWorld2D()!=player.GetWorld2D()) continue;
                leaves.Clear(); owner.AppendAuthorizedAttackLeaves(leaves);
                foreach(AttackTelegraphVisual leaf in leaves)
                {
                    if(!GodotObject.IsInstanceValid(leaf) || !leaf.IsInsideTree() || leaf.IsQueuedForDeletion() ||
                        leaf.GetParent() is not RangedWeaponProjectile projectile || projectile.IsRetired ||
                        projectile.IsQueuedForDeletion() || projectile.GetParent()!=owner ||
                        leaf.CurrentFrame.Timeline.Phase!=AttackPhase.Active ||
                        leaf.CurrentFrame.Timeline.AttackId!=danger.AttackId ||
                        leaf.CurrentFrame.Shape.Origin.DistanceSquaredTo(danger.GuardSource)>.01f) continue;
                    Vector2 direction=Task10BottomVerticalGuardIntent(
                        leaf.CurrentFrame.Shape.Origin-player.GetNode<Hurtbox>("Hurtbox").GlobalPosition,
                        Task10GuardCircleClearance(leaf.CurrentFrame.Shape,capsule.Value),
                        capsule.Value.Height*capsule.Value.Transform.Y.Length());
                    if(direction==Vector2.Zero) continue;
                    ownerId=owner.GetInstanceId(); attackId=danger.AttackId; projectileId=projectile.GetInstanceId();
                    return direction;
                }
            }
            return Vector2.Zero;
        }
        finally { leaves.Clear(); } // No borrowed leaf or projectile is retained across a physics wait.
    }

    // Passive evidence only. The original Bottom input/guard/control budgets
    // do not consume this trace and no diagnostic result grants an action.
    private sealed class BottomMissileObservation : IDisposable
    {
        private readonly CharacterBody2D _player;
        private readonly Node _basinActors;
        private readonly StaticBody2D _step, _walkway;
        private readonly Func<string> _phase;
        private readonly PlayerInputReader _input;
        private readonly PlayerShieldController _shield;
        private readonly PlayerEnergyController _energy;
        private readonly HealthComponent _health;
        private readonly List<string> _rows = new(512);
        private readonly List<Action> _detach = new();
        private readonly Dictionary<ulong, (RangedWeaponEnemy Owner, RangedWeaponProjectile Projectile)> _projectiles = new();
        private readonly Dictionary<ulong, Action> _projectileDetach = new();
        private readonly HashSet<ulong> _owners = new();
        private readonly List<AttackTelegraphVisual> _leaves = new();
        private int _total, _dropped, _faults, _roots;
        private bool _disposed;
        private RangedWeaponEnemy _explosionOwner;
        private RangedWeaponProjectile _explosionProjectile;
        private ulong _explosionPhysics;

        internal BottomMissileObservation(Node world, Node basinActors, CharacterBody2D player,
            StaticBody2D step, StaticBody2D walkway, Func<string> phase)
        {
            _player=player; _basinActors=basinActors; _step=step; _walkway=walkway; _phase=phase;
            _input=player.GetNode<PlayerInputReader>("Input");
            _shield=player.GetNode<PlayerShieldController>("Shield");
            _energy=player.GetNode<PlayerEnergyController>("Energy");
            _health=player.GetNode<HealthComponent>("Health");
            Safe(() =>
            {
                void Resolved(PlayerShieldResult result,ulong transition)
                {
                    if(_explosionOwner!=null && _explosionPhysics==Engine.GetPhysicsFrames())
                        Safe(()=>Record($"shield-result guarded={result.Guarded} damage={result.DamageAmount} " +
                            $"transition={transition} explosion_scope=true request_identity_directly_observed=false",
                            _explosionOwner,_explosionProjectile));
                }
                _shield.ShieldResolved+=Resolved;
                _detach.Add(()=>_shield.ShieldResolved-=Resolved);
                var roots=world.FindChildren("Actors", "", true, false);
                using Godot.Collections.Array rootsBacking=(Godot.Collections.Array)roots;
                foreach(Node root in roots)
                {
                    _roots++;
                    void Entered(Node child) { if(child is RangedWeaponEnemy owner) Safe(()=>Track(owner)); }
                    root.ChildEnteredTree+=Entered;
                    _detach.Add(()=> { if(GodotObject.IsInstanceValid(root)) root.ChildEnteredTree-=Entered; });
                    var children=root.GetChildren();
                    using Godot.Collections.Array backing=(Godot.Collections.Array)children;
                    foreach(Node child in children) if(child is RangedWeaponEnemy owner) Track(owner);
                }
            });
        }
        private void Safe(Action action) { try { action(); } catch(Exception) { _faults++; } }
        private static bool Live(Node node) => GodotObject.IsInstanceValid(node) && node.IsInsideTree();
        private bool Full(StaticBody2D support)
        {
            if(!Live(_player) || !Live(support) || !_player.IsOnFloor() ||
                !TryTask10CapsuleCorridor(_player,support.GetNode<CollisionShape2D>("Collision"),out _,out _,out _,out _)) return false;
            for(int i=0;i<_player.GetSlideCollisionCount();i++)
            {
                using var contact=_player.GetSlideCollision(i);
                if(contact.GetCollider()==support && contact.GetNormal().Y<-.7f) return true;
            }
            return false;
        }
        private void Track(RangedWeaponEnemy owner)
        {
            if(owner.Role!=RangedWeaponRole.Missile || !_owners.Add(owner.GetInstanceId())) return;
            ulong lastAttack=ulong.MaxValue;
            AttackPhase lastPhase=(AttackPhase)(-1);
            void Published(AttackPresentationFrame frame)
            {
                if(frame.Timeline.AttackId==lastAttack && frame.Timeline.Phase==lastPhase) return;
                lastAttack=frame.Timeline.AttackId; lastPhase=frame.Timeline.Phase;
                Safe(()=>Record("publication",owner,null,frame));
            }
            void Shot(ulong attack,Vector2 point) => Safe(()=>Record("shot-emitted",owner,null,null,attack,point));
            void Entered(Node child) { if(child is RangedWeaponProjectile projectile) Safe(()=>TrackProjectile(owner,projectile)); }
            owner.AttackFrameChanged+=Published; owner.ShotEmitted+=Shot; owner.ChildEnteredTree+=Entered;
            _detach.Add(()=>
            {
                owner.AttackFrameChanged-=Published; owner.ShotEmitted-=Shot;
                if(GodotObject.IsInstanceValid(owner)) owner.ChildEnteredTree-=Entered;
            });
            Record("owner-bound",owner);
            var children=owner.GetChildren();
            using Godot.Collections.Array backing=(Godot.Collections.Array)children;
            foreach(Node child in children) if(child is RangedWeaponProjectile projectile) TrackProjectile(owner,projectile);
        }
        private void TrackProjectile(RangedWeaponEnemy owner,RangedWeaponProjectile projectile)
        {
            ulong id=projectile.GetInstanceId();
            if(_projectiles.ContainsKey(id)) return;
            _projectiles[id]=(owner,projectile);
            void Exploded(Vector2 point,float radius)
            {
                _explosionOwner=owner; _explosionProjectile=projectile; _explosionPhysics=Engine.GetPhysicsFrames();
                Safe(()=>Record("explosion-event-before-damage",owner,projectile,null,0,point,radius));
            }
            void Retired(RangedWeaponProjectile current)
            {
                Safe(()=>Record("retired",owner,current));
                if(_explosionProjectile==current) { _explosionOwner=null; _explosionProjectile=null; _explosionPhysics=0; }
                if(_projectileDetach.Remove(id,out Action detach)) Safe(detach);
                _projectiles.Remove(id);
            }
            projectile.ExplosionResolved+=Exploded; projectile.Retired+=Retired;
            _projectileDetach[id]=()=> { projectile.ExplosionResolved-=Exploded; projectile.Retired-=Retired; };
            Record("child-enter-or-existing-not-flight-proof",owner,projectile);
        }
        private void Record(string edge,RangedWeaponEnemy owner=null,RangedWeaponProjectile projectile=null,
            AttackPresentationFrame? publication=null,ulong emitted=0,Vector2? point=null,float? radius=null,
            Task10EncounterDanger? danger=null)
        {
            if(_disposed) return;
            _total++;
            if(_rows.Count==512) { _dropped++; return; }
            AttackTelegraphVisual leaf=Live(projectile) ? projectile.Telegraph : null;
            AttackPresentationFrame frame=publication ?? (Live(leaf) ? leaf.CurrentFrame : default);
            bool authorized=false;
            if(Live(owner) && Live(leaf))
            {
                try { owner.AppendAuthorizedAttackLeaves(_leaves); authorized=_leaves.Contains(leaf); }
                finally { _leaves.Clear(); }
            }
            Task10GuardCapsule? capsule=Task10CurrentGuardCapsule(_player);
            float clearance=capsule.HasValue ? Task10GuardCircleClearance(frame.Shape,capsule.Value) : float.PositiveInfinity;
            Vector2? projectilePosition=Live(projectile) ? projectile.GlobalPosition : null;
            _rows.Add($"BOTTOM_MISSILE_OBS row={_rows.Count+1}/512 edge={edge} physics={Engine.GetPhysicsFrames()} " +
                $"phase={_phase()} sample={_input.CurrentFrameSequence} player={_player.GlobalPosition} velocity={_player.Velocity} " +
                $"hp={_health.CurrentHealth} step_full={Full(_step)} walkway_full={Full(_walkway)} " +
                $"move={_input.CurrentFrame.MoveAxis} jump={_input.CurrentFrame.JumpPressed}/{_input.CurrentFrame.JumpHeld} " +
                $"shield={_input.CurrentFrame.ShieldHeld}/{_shield.IsActive} guard_direction={_shield.GuardDirection} " +
                $"raw_L={Input.IsPhysicalKeyPressed(Key.L)} energy={_energy.EnergyRatio:R} can_guard={_shield.CanBeginGuard} " +
                $"owner={(Live(owner)?owner.GetInstanceId():0)} scene={(Live(owner)?owner.SceneFilePath:"none")} " +
                $"actor_state={(Live(owner)?owner.CurrentFrame.State.ToString():"none")} " +
                $"projectile={(Live(projectile)?projectile.GetInstanceId():0)} xy={projectilePosition} " +
                $"travel={(Live(projectile)?projectile.TravelledDistance:float.NaN):R} retired={(Live(projectile)?projectile.IsRetired:false)} " +
                $"leaf={(Live(leaf)?leaf.GetInstanceId():0)} authorized={authorized} attack={frame.Timeline.AttackId} emitted={emitted} " +
                $"timeline={frame.Timeline.Phase}/{frame.Timeline.PhaseRemainingSeconds:R} " +
                $"shape={frame.Shape.Kind} origin={frame.Shape.Origin} extent={frame.Shape.Extent} rotation={frame.Shape.RotationRadians:R} " +
                $"capsule_valid={capsule.HasValue} clearance_finite={float.IsFinite(clearance)} clearance={clearance:R} " +
                $"event_xy={point} event_radius={radius} danger={danger?.Threat} danger_owner={danger?.SourceActorId} " +
                $"danger_attack={danger?.AttackId} guard_source={danger?.GuardSource} safe_counter={danger?.SafeCounterSeconds}");
        }
        internal void Sample()
        {
            if(_disposed) return;
            Safe(()=>
            {
                Task10EncounterDanger danger;
                try { danger=Task10ObserveEncounterDanger(_basinActors,"canyon.v2.encounter.steam-valves",null,false,
                    _leaves,guardCapsule:Task10CurrentGuardCapsule(_player)); }
                finally { _leaves.Clear(); }
                Record("caller-pre-wait",danger:danger);
                foreach(var (owner,projectile) in _projectiles.Values)
                    if(Live(projectile) && !projectile.IsRetired) Record("flight-sample",owner,projectile);
            });
        }
        public void Dispose()
        {
            if(_disposed) return;
            _disposed=true;
            _explosionOwner=null; _explosionProjectile=null;
            foreach(Action detach in _projectileDetach.Values) Safe(detach);
            _projectileDetach.Clear(); _projectiles.Clear();
            foreach(Action detach in _detach) Safe(detach);
            _detach.Clear(); _leaves.Clear();
            GD.Print($"BOTTOM_MISSILE_OBS_BEGIN rows={_rows.Count} total={_total} dropped={_dropped} faults={_faults} " +
                $"roots_at_entry={_roots} missile_owners={_owners.Count} scope=initial-mounted-missiles+original-basin-danger " +
                "input_changed=false forecast_proved=false no_leaf_across_await=true");
            foreach(string row in _rows) GD.Print(row);
            GD.Print("BOTTOM_MISSILE_OBS_END diagnostic_only=true input_granted=false");
        }
    }
}
