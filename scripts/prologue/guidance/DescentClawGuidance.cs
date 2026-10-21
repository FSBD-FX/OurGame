using Godot;
using Knight.Abilities;
using Knight.Player;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Guidance;

// Optional current-terrain opportunity; never advances a quest or changes cast rules.
internal static class DescentClawGuidance
{
    private static readonly MechanicalClawTargetResolver Resolver = new();

    public static bool TryResolve(PrologueSector sector, CharacterBody2D player, out Vector2 point)
    {
        point = default;
        if (!GodotObject.IsInstanceValid(sector) || !sector.IsInsideTree() || sector.IsQueuedForDeletion() ||
            sector.SectorId != PrologueSectorId.CanyonDescent || !GodotObject.IsInstanceValid(player) ||
            !player.IsInsideTree() || player.IsQueuedForDeletion() || !player.IsOnFloor() ||
            player.GetWorld2D() != sector.GetWorld2D() ||
            player.GetNodeOrNull<PlayerActionModeController>("ActionModes")?.CanBeginGrapple != true)
            return false;
        bool supportedHere = false;
        for (int index = 0; index < player.GetSlideCollisionCount(); index++)
        {
            KinematicCollision2D collision = player.GetSlideCollision(index);
            supportedHere |= collision.GetCollider() is StaticBody2D support &&
                sector.IsAncestorOf(support) && collision.GetNormal().Y < -.7f;
        }
        if (!supportedHere) return false;

        // This card explicitly teaches W. Use that same production keyboard rule,
        // not a remote mouse cursor or a node-center approximation.
        MechanicalClawTargetResolution resolved = Resolver.ResolveKeyboard(player, Vector2.Up);
        GrappleSurface2D surface = resolved.Surface;
        if (!GodotObject.IsInstanceValid(surface) || !surface.IsAvailable || !surface.IsVisibleInTree() ||
            !sector.IsAncestorOf(surface) || !surface.UseCollisionHitPoint ||
            surface.Material is not (GrappleSurfaceMaterial.GripStone or GrappleSurfaceMaterial.TempleMetal) ||
            !resolved.Snapshot.Exists || !resolved.Snapshot.HasLineOfSight ||
            resolved.Snapshot.SurfaceNormal.Y < .7f ||
            surface.GetParent() is not StaticBody2D body ||
            body.GetNodeOrNull<CollisionShape2D>("Collision") is not { Disabled: false, Shape: not null } ||
            player.GetNodeOrNull<CollisionShape2D>("BodyCollider")?.Shape is not CapsuleShape2D capsule)
            return false;

        MechanicalClawSettings settings = MechanicalClawSettings.Default;
        Vector2 anchor = resolved.Snapshot.WorldAnchor;
        Vector2 dock = MechanicalClawArrivalGeometry.DockPoint(anchor, resolved.Snapshot.SurfaceNormal,
            capsule, settings.MinimumRopeLength, settings.ArrivalMargin);
        if (!dock.IsFinite() || player.GlobalPosition.Y - dock.Y <= settings.ArrivalTolerance ||
            player.TestMove(player.GlobalTransform, dock - player.GlobalPosition, null, player.SafeMargin, false))
            return false;
        Vector2 screen = player.GetGlobalTransformWithCanvas() * player.ToLocal(anchor);
        if (!player.GetViewportRect().Grow(-48f).HasPoint(screen)) return false;
        point = anchor;
        return true;
    }
}
