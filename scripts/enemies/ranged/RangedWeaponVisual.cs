using System;
using System.Collections.Generic;
using Godot;
using Knight.Combat;
using Knight.Presentation;

namespace Knight.Enemies.Ranged;

public partial class RangedWeaponVisual : Node2D, IRangedWeaponVisual2D
{
    [Export] public RangedWeaponRole Role { get; set; }
    [Export] public Texture2D TorsoTexture { get; set; }
    [Export] public Texture2D HeadTexture { get; set; }
    [Export] public Texture2D UpperArmTexture { get; set; }
    [Export] public Texture2D ForearmTexture { get; set; }
    [Export] public Texture2D ThighTexture { get; set; }
    [Export] public Texture2D ShinTexture { get; set; }
    [Export] public Texture2D FootTexture { get; set; }
    [Export] public Texture2D WeaponTexture { get; set; }
    private readonly RangedCharacterRig _rig = new();
    private RangedWeaponRole _rigRole;
    private float _hurt;
    public RangedRigPose CurrentRig { get { EnsureRole(); return _rig.Pose; } }

    public void PresentWeaponFrame(RangedWeaponRole role, RangedWeaponFrame frame)
    {
        Role = role;
        ApplyWeaponFrame(frame);
    }
    public void ResetWeaponPresentation(RangedWeaponRole role)
    {
        Role = _rigRole = role;
        _rig.Reset(role);
        _hurt = 0f;
        QueueRedraw();
    }
    public void ClearAttackPresentation() { _rig.ClearAttack(); QueueRedraw(); }

    private void EnsureRole()
    {
        if (_rigRole == Role) return;
        _rigRole = Role;
        _rig.Reset(Role);
    }

    public void ApplyWeaponFrame(RangedWeaponFrame frame)
    {
        EnsureRole();
        Transform2D world = IsInsideTree() ? GlobalTransform : Transform;
        if (float.IsFinite(world.Determinant()) && MathF.Abs(world.Determinant()) > .00001f)
            frame = frame with { AimDirection = world.AffineInverse().BasisXform(frame.AimDirection) };
        _rig.Apply(frame);
        QueueRedraw();
    }

    public void ApplyFrame(EntityPresentationFrame frame) => QueueRedraw();
    public void PlayCue(PresentationCue cue)
    {
        if (cue == PresentationCue.Hurt) _hurt = .16f;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float seconds = double.IsFinite(delta) && delta > 0d && delta < 1d ? (float)delta : 0f;
        EnsureRole();
        _rig.Advance(seconds);
        _hurt = Mathf.Max(0f, _hurt - seconds);
        QueueRedraw();
    }

    // These are also the production draw destinations, usable by replacement
    // art/editor tooling. They describe geometry, not texture alpha coverage.
    public RangedPartGeometry GetPartGeometry(string part, bool far = false)
    {
        RangedRigPose pose = CurrentRig;
        RangedRigLimb limb = part is "thigh" or "shin" ? (far ? pose.FarLeg : pose.NearLeg) : (far ? pose.FarArm : pose.NearArm);
        Transform2D transform = part switch
        {
            "torso" => pose.TorsoTransform, "head" => pose.HeadTransform, "weapon" => pose.WeaponTransform,
            "foot" => new Transform2D(0f, (far ? pose.FarLeg : pose.NearLeg).End),
            "upper-arm" or "thigh" => BoneTransform(limb.Start, limb.Joint),
            "forearm" or "shin" => BoneTransform(limb.Joint, limb.End),
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
        Rect2 canvas = part switch
        {
            "torso" => new(-24f, -25f, 48f, 43f), "head" => new(-13f, -10f, 26f, 20f),
            "weapon" => new(-24f, -26f, 70f, 52f), "foot" => new(-8f, -4f, 16f, 4f),
            "upper-arm" or "forearm" => new(-4f, 0f, 8f, 18f), _ => new(-5f, 0f, 10f, 13f)
        };
        Texture2D texture = TextureFor(part);
        bool use = IsInstanceValid(texture) && texture.GetWidth() > 0 && texture.GetHeight() > 0;
        Rect2 fitted = canvas;
        if (use)
        {
            Vector2 size = texture.GetSize();
            size *= MathF.Min(canvas.Size.X / size.X, canvas.Size.Y / size.Y);
            Vector2 origin = canvas.GetCenter() - size * .5f;
            if (part == "foot") origin.Y = canvas.End.Y - size.Y;
            fitted = new Rect2(origin, size);
        }
        return new(transform, canvas, fitted, use, part == "torso" ? TorsoContour() : part == "head" ? HeadContour() : Array.Empty<Vector2>());
    }

    private static Transform2D BoneTransform(Vector2 start, Vector2 end) => new((end - start).Angle() - Mathf.Pi * .5f, start);
    private Texture2D TextureFor(string part) => part switch { "torso" => TorsoTexture, "head" => HeadTexture,
        "upper-arm" => UpperArmTexture, "forearm" => ForearmTexture, "thigh" => ThighTexture,
        "shin" => ShinTexture, "foot" => FootTexture, "weapon" => WeaponTexture, _ => null };
    private Vector2[] TorsoContour() => Role switch
    {
        RangedWeaponRole.Scatter => [new(-24, 10), new(-24, -13), new(-14, -24), new(14, -24), new(24, -13), new(24, 10), new(13, 17), new(-13, 17)],
        RangedWeaponRole.Burst => [new(-15, 17), new(-18, -16), new(-8, -25), new(11, -22), new(16, 1), new(12, 17)],
        RangedWeaponRole.Missile => [new(-24, 16), new(-24, -7), new(-18, -16), new(17, -16), new(24, -5), new(22, 17)],
        _ => [new(-10, 17), new(-17, -12), new(0, -25), new(17, -12), new(10, 17), new(0, 10)]
    };
    private Vector2[] HeadContour() => Role switch
    {
        RangedWeaponRole.Scatter => [new(-13, 8), new(-13, -4), new(-8, -10), new(8, -10), new(13, -4), new(13, 8)],
        RangedWeaponRole.Burst => [new(-9, 10), new(-11, -7), new(9, -10), new(12, 4), new(6, 10)],
        RangedWeaponRole.Missile => [new(-13, 5), new(-10, -6), new(11, -6), new(13, 5), new(0, 9)],
        _ => [new(0, -10), new(12, 0), new(0, 10), new(-12, 0)]
    };

    private bool DrawPart(string part, Color color, bool far = false)
    {
        RangedPartGeometry geometry = GetPartGeometry(part, far);
        DrawSetTransformMatrix(geometry.Transform);
        if (geometry.UsesTexture) DrawTextureRect(TextureFor(part), geometry.DrawRect, false);
        else if (geometry.Contour is Vector2[] polygon && polygon.Length > 0) DrawPolygon(polygon, [color]);
        else DrawRect(geometry.DrawRect, color);
        DrawSetTransform(Vector2.Zero);
        return geometry.UsesTexture;
    }

    private void DrawLimb(bool arm, bool far, Color plate, Color joint)
    {
        DrawPart(arm ? "upper-arm" : "thigh", plate, far);
        DrawPart(arm ? "forearm" : "shin", plate, far);
        RangedRigPose pose = CurrentRig;
        RangedRigLimb limb = arm ? (far ? pose.FarArm : pose.NearArm) : (far ? pose.FarLeg : pose.NearLeg);
        DrawCircle(limb.Start, 3f, joint); DrawCircle(limb.Joint, 3f, joint);
        if (arm) DrawCircle(limb.End, 4f, joint); else DrawPart("foot", joint, far);
    }

    public override void _Draw()
    {
        // Complete program rigs, still NOT approved final faction-family art.
        RangedRigPose pose = CurrentRig;
        Color steel = new("677278");
        Color dark = new("29363e");
        Color trim = Role switch
        {
            RangedWeaponRole.Scatter => new Color("be8b49"),
            RangedWeaponRole.Burst => new Color("819d70"),
            RangedWeaponRole.Missile => new Color("c26b46"),
            _ => new Color("86c9d8")
        };
        if (pose.State == RangedWeaponState.Stunned) trim = new Color("a784c4");
        if (pose.State == RangedWeaponState.Dead) { steel = new Color("454c50"); trim = new Color("5b5350"); }
        if (_hurt > 0f) steel = new Color("d1a68b");
        DrawLimb(false, true, dark, trim.Darkened(.2f));
        DrawLimb(false, false, steel, trim);
        DrawLimb(true, true, dark, trim.Darkened(.2f));
        bool torsoArt = DrawPart("torso", steel);
        if (!torsoArt)
        {
            DrawSetTransformMatrix(pose.TorsoTransform);
            if (Role == RangedWeaponRole.Scatter) { DrawCircle(new Vector2(0, -5), 13, dark); DrawArc(new Vector2(0, -5), 10, 0, Mathf.Tau, 24, trim, 3, true); }
            else if (Role == RangedWeaponRole.Burst) { DrawRect(new Rect2(-12, -15, 9, 23), dark); for (int i = 0; i < 5; i++) DrawRect(new Rect2(-17, -12 + i * 5, 5, 3), trim); }
            else if (Role == RangedWeaponRole.Missile) { DrawRect(new Rect2(-23, -17, 13, 26), dark); DrawCircle(new Vector2(-16, -14), 7, trim); DrawLine(new Vector2(-8, 8), new Vector2(17, 8), trim, 4, true); }
            else { DrawPolyline([new(-12, -10), new(0, -20), new(12, -10), new(0, 6), new(-12, -10)], trim, 2, true); DrawCircle(new Vector2(0, -8), 4, trim); }
            DrawSetTransform(Vector2.Zero);
        }
        bool headArt = DrawPart("head", dark);
        if (!headArt) { DrawSetTransformMatrix(pose.HeadTransform); DrawLine(new Vector2(-7, 0), new Vector2(7, 0), trim, 3, true); DrawSetTransform(Vector2.Zero); }
        RangedPartGeometry weaponGeometry = GetPartGeometry("weapon");
        DrawSetTransformMatrix(weaponGeometry.Transform);
        Color hot = trim.Lerp(new Color("e58859"), pose.Heat * .7f);
        if (weaponGeometry.UsesTexture) DrawTextureRect(WeaponTexture, weaponGeometry.DrawRect, false);
        else
        switch (Role)
        {
            case RangedWeaponRole.Scatter:
                DrawRect(new Rect2(-13f, -9f, 32f, 22f), dark);
                DrawRect(new Rect2(7f, -12f, 24f, 8f), trim);
                DrawRect(new Rect2(7f, 5f, 24f, 8f), trim);
                DrawLine(new Vector2(31f, -12f), new Vector2(31f, 13f), hot, 4f, true);
                DrawCircle(new Vector2(-7f, 2f), 8f, hot);
                break;
            case RangedWeaponRole.Burst:
                DrawRect(new Rect2(-14f, -8f, 35f, 17f), dark);
                DrawRect(new Rect2(13f, -4f, 30f, 8f), trim);
                DrawRect(new Rect2(36f, -7f, 9f, 14f), hot);
                for (int i = 0; i < 6; i++)
                    DrawCircle(new Vector2(-9f + i * 4f, 14f + i % 2 * 4f), 3f, trim);
                DrawRect(new Rect2(-7f, -16f, 12f, 8f), hot);
                break;
            case RangedWeaponRole.Missile:
                DrawRect(new Rect2(-21f, -15f, 48f, 28f), dark);
                DrawRect(new Rect2(-19f, -12f, 39f, 22f), trim);
                DrawCircle(new Vector2(25f, -1f), 14f, hot);
                DrawCircle(new Vector2(25f, -1f), 9f, dark);
                for (int i = 0; i < 4; i++)
                    DrawLine(new Vector2(-14f + i * 8f, -10f), new Vector2(-14f + i * 8f, 8f), dark, 3f, true);
                DrawLine(new Vector2(-22f, -18f), new Vector2(-12f, -25f), hot, 5f, true);
                break;
            case RangedWeaponRole.Laser:
                DrawPolygon([new(-17f, -11f), new(6f, -16f), new(22f, 0f), new(6f, 16f), new(-17f, 11f)], [dark]);
                DrawPolyline([new(-8f, -8f), new(7f, -12f), new(18f, 0f), new(7f, 12f), new(-8f, 8f)], hot, 3f, true);
                DrawLine(new Vector2(14f, -13f), new Vector2(35f, -8f), trim, 5f, true);
                DrawLine(new Vector2(14f, 13f), new Vector2(35f, 8f), trim, 5f, true);
                DrawCircle(new Vector2(12f, 0f), 5f + pose.Charge * 4f, hot);
                break;
        }
        if (pose.Charge > 0f)
            DrawArc(new Vector2(8f, 0f), 21f, -Mathf.Pi * .5f, -Mathf.Pi * .5f + Mathf.Tau * pose.Charge,
                30, pose.Charge > .5f ? new Color("d35c4b") : new Color("e6bd62"), 2f, true);
        DrawSetTransform(Vector2.Zero);
        DrawLimb(true, false, steel, trim);
        if (pose.State == RangedWeaponState.Stunned)
            DrawPolyline([new(-13f, -38f), new(-6f, -43f), new(2f, -38f), new(10f, -43f)], trim, 3f, true);
    }
}

// A purely cosmetic, owner-managed afterimage. It is never an attack leaf and
// never delivers damage. Inherited process mode freezes it during game pause.
public partial class RangedWeaponImpactRing : Node2D
{
    public float Radius { get; set; } = 56f;
    private float _remaining = .22f;
    public override void _Ready() => QueueRedraw();
    public override void _PhysicsProcess(double delta)
    {
        if (double.IsFinite(delta) && delta > 0d && delta < 1d) _remaining -= (float)delta;
        if (_remaining <= 0f) { QueueFree(); return; }
        QueueRedraw();
    }
    public override void _Draw()
    {
        float ratio = Mathf.Clamp(_remaining / .22f, 0f, 1f);
        Color rim = new("e39253") { A = ratio * .8f };
        DrawArc(Vector2.Zero, Radius * (1f + (1f - ratio) * .12f), 0f, Mathf.Tau, 48, rim, 3f, true);
        rim.A *= .35f;
        DrawCircle(Vector2.Zero, Radius * .72f, rim);
    }
}
