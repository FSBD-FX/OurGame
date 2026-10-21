using System;
using Godot;
using Knight.Enemies;
using Knight.Enemies.Laborer;
using Knight.Enemies.Moth;
using Knight.Enemies.Ranged;
using Knight.Enemies.Residents;
using Knight.Enemies.Sentry;
using Knight.Enemies.Skitter;

namespace Knight.Prologue.Residents;

public enum ResidentCarrierKind { Unspecified, Wanderer, Charger, Air, Heavy, Sentry, Scatter, Burst, Missile, Laser }

// Persistent gameplay identity only. This is not a visual or placement certificate.
[GlobalClass]
public partial class ResidentSubtypeProfile : Resource
{
    [Export] public string SubtypeId { get; set; } = "";
    [Export] public ResidentFamily Family { get; set; }
    [Export] public ResidentArchetype Archetype { get; set; }
    [Export] public ResidentCarrierKind Carrier { get; set; }
    [Export] public ResidentBoneWeakness BoneWeakness { get; set; }
    [Export] public float GhostHiddenSeconds { get; set; }
    [Export] public float GhostManifestSeconds { get; set; }

    public ResidentPopulationDefinition Bind(ResidentSubtypeDefinition expected,
        ResidentPopulationDefinition slot)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(slot);
        if (string.IsNullOrWhiteSpace(SubtypeId) || SubtypeId != expected.Id || SubtypeId != slot.SubtypeId ||
            string.IsNullOrWhiteSpace(slot.StableId) || slot.Placement == null ||
            !Enum.IsDefined(Family) || Family != expected.Family || Family != slot.Family ||
            !Enum.IsDefined(Archetype) || Archetype != expected.Archetype || Archetype != slot.Archetype ||
            !Enum.IsDefined(slot.RouteNode) || !Enum.IsDefined(slot.Sector) ||
            !Enum.IsDefined(Carrier) || Carrier == ResidentCarrierKind.Unspecified || Carrier != expected.Carrier ||
            !CarrierSupportsRole(Carrier, Archetype) || !Enum.IsDefined(BoneWeakness))
            throw new ArgumentException("Saved subtype profile and authoritative slot/carrier must agree.", nameof(slot));

        ResidentCombatProfile combat = null;
        if (Family == ResidentFamily.Bone)
        {
            ResidentBoneWeakness required = Archetype switch
            {
                ResidentArchetype.Wanderer or ResidentArchetype.Diver => ResidentBoneWeakness.RecoveryOrThreadStunned,
                ResidentArchetype.Charger => ResidentBoneWeakness.RecoveryOrThreadExposed,
                ResidentArchetype.Heavy => ResidentBoneWeakness.RearOrExposed,
                _ => ResidentBoneWeakness.None
            };
            if (required == ResidentBoneWeakness.None || BoneWeakness != required ||
                GhostHiddenSeconds != 0 || GhostManifestSeconds != 0)
                throw new ArgumentException("Bone profile must preserve the supported exclusive posture weakness.");
            combat = new(BoneWeakness);
        }
        else if (Family == ResidentFamily.Ghost)
        {
            if (Carrier is not (ResidentCarrierKind.Wanderer or ResidentCarrierKind.Air or ResidentCarrierKind.Sentry) ||
                BoneWeakness != ResidentBoneWeakness.None || !Duration(GhostHiddenSeconds) || !Duration(GhostManifestSeconds))
                throw new ArgumentException("Ghost profile must preserve supported exclusive finite phases.");
            combat = new(GhostHiddenSeconds: GhostHiddenSeconds, GhostManifestSeconds: GhostManifestSeconds);
        }
        else if (BoneWeakness != ResidentBoneWeakness.None || GhostHiddenSeconds != 0 || GhostManifestSeconds != 0)
            throw new ArgumentException("Ordinary families cannot silently ignore another family's combat policy.");

        if (slot.CombatProfile != null && slot.CombatProfile != combat)
            throw new ArgumentException("Caller policy conflicts with the saved subtype profile.", nameof(slot));
        // Do not retain this mutable Resource in the actor or invent a placement.
        return slot with { CombatProfile = combat };
    }

    private static bool Duration(float value) => float.IsFinite(value) && value >= 1f / 60 && value <= 60;

    private static bool CarrierSupportsRole(ResidentCarrierKind carrier, ResidentArchetype role) => carrier switch
    {
        ResidentCarrierKind.Wanderer => role == ResidentArchetype.Wanderer,
        ResidentCarrierKind.Charger => role == ResidentArchetype.Charger,
        ResidentCarrierKind.Air => role is ResidentArchetype.Flyer or ResidentArchetype.Diver,
        ResidentCarrierKind.Heavy => role == ResidentArchetype.Heavy,
        ResidentCarrierKind.Sentry or ResidentCarrierKind.Scatter or ResidentCarrierKind.Burst or
            ResidentCarrierKind.Missile or ResidentCarrierKind.Laser => role == ResidentArchetype.Turret,
        _ => false
    };

    public void ConfigureActor(Node actor, ResidentSubtypeDefinition expected,
        ResidentPopulationDefinition slot)
    {
        ResidentPopulationDefinition bound = Bind(expected, slot);
        if (!GodotObject.IsInstanceValid(actor) || actor.IsInsideTree() || actor is not IResidentEnemyAgent agent)
            throw new ArgumentException("Configure a live resident actor before mounting it.", nameof(actor));
        for (Node ancestor = actor; ancestor != null; ancestor = ancestor.GetParent())
            if (!GodotObject.IsInstanceValid(ancestor) || ancestor.IsQueuedForDeletion())
                throw new ArgumentException("Cannot configure a retiring resident graph.", nameof(actor));
        bool matches = Carrier switch
        {
            ResidentCarrierKind.Wanderer => actor is RustShellSquire,
            ResidentCarrierKind.Charger => actor is CanyonSkitter,
            ResidentCarrierKind.Air => actor is GraveLampMoth,
            ResidentCarrierKind.Heavy => actor is BellBackLaborer,
            ResidentCarrierKind.Sentry => actor is BellBoltSentry,
            ResidentCarrierKind.Scatter => actor is RivetScatterGunner,
            ResidentCarrierKind.Burst => actor is BeltBurstGunner,
            ResidentCarrierKind.Missile => actor is FurnaceMissileGunner,
            ResidentCarrierKind.Laser => actor is PrismLaserGunner,
            _ => false
        };
        if (!matches) throw new ArgumentException("Saved subtype requires its actual body/weapon carrier.", nameof(actor));
        // Existing adapter performs full body/path validation and its once-only
        // commit. It still owns native motion, health and initialization order.
        agent.ConfigureResidence(bound);
    }
}
