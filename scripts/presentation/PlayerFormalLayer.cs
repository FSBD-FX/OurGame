using System.Collections.Generic;
using Godot;

namespace Knight.Presentation;

public enum PlayerFormalLayerKind
{
    Body,
    NearArm,
    FarArm,
    NearLeg,
    FarLeg,
    Weapon,
    Mantle,
    ShoulderCore,
    Lantern,
    Tether
}

public partial class PlayerFormalLayer : Node2D
{
    private static readonly Color Ink = new("11161a");
    private static readonly Color DeepCloth = new("182b2b");
    private static readonly Color ClothLight = new("31534b");
    private static readonly Color ClothBody = new("254139");
    private static readonly Color ClothFold = new("345247");
    private static readonly Color ClothShade = new("1b302d");
    private static readonly Color Bone = new("e5ddc7");
    private static readonly Color BoneShadow = new("978f7d");
    private static readonly Color Brass = new("b68a42");
    private static readonly Color BrassLight = new("e4c276");
    private static readonly Color ThreadGlow = new("75e0d6");

    [Export] public PlayerFormalLayerKind Kind { get; set; }

    public Vector2 LegHipLocal => Vector2.Zero;
    private Vector2? _legSole, _legKnee, _lastLegWorldKnee;
    private Vector2 _legUp=Vector2.Up;
    public Vector2 FreeLegSoleLocal => new(0,Kind==PlayerFormalLayerKind.NearLeg?26:24);
    public Vector2 LegSoleLocal => _legSole??FreeLegSoleLocal;
    public Vector2 LegAnkleLocal => LegSoleLocal+_legUp*4;
    public Vector2 LegKneeLocal => _legKnee??LegSolution(LegAnkleLocal);
    public bool LegReachClamped { get; private set; }
    private static Vector2 LegSolution(Vector2 ankle)
    {
        if(ankle.LengthSquared()<.000001f) return new Vector2(18,0);
        var perpendicular=new Vector2(ankle.Y,-ankle.X).Normalized();
        return ankle*.5f+perpendicular*Mathf.Sqrt(Mathf.Max(0,18*18-ankle.LengthSquared()*.25f));
    }
    public void SetLegSoleLocal(Vector2 sole, Vector2 up)
    {
        if(!sole.IsFinite()) sole=FreeLegSoleLocal;
        if(!up.IsFinite() || up.LengthSquared()<.000001f) up=Vector2.Up;
        up=up.Normalized();
        Vector2 ankle=sole+up*4;
        double length=System.Math.Sqrt((double)ankle.X*ankle.X+(double)ankle.Y*ankle.Y);
        LegReachClamped=length>36;
        if(LegReachClamped) ankle*=(float)(36/length);
        sole=ankle-up*4;
        Vector2 knee=LegSolution(ankle);
        if(_lastLegWorldKnee is Vector2 previous)
        {
            Vector2 opposite=ankle-knee;
            if(ToGlobal(opposite).DistanceSquaredTo(previous)<ToGlobal(knee).DistanceSquaredTo(previous)) knee=opposite;
        }
        _legSole=sole; _legUp=up; _legKnee=knee;
        _lastLegWorldKnee=ToGlobal(knee);
        QueueRedraw();
    }

    // Preserve the existing rig pivot and animation ownership. A saved override
    // can replace this bitmap later without changing the action/fog consumers.
    [Export] public Texture2D LanternTexture { get; set; }
    public Rect2 LanternDrawRect => new(-12f, -12f, 24f, 24f);

    [Export] public Texture2D WeaponTexture { get; set; }
    // Original approved master canvas remains intact. Only transparent padding
    // is excluded by the real Draw region; scale is uniform and rig-relative.
    public Rect2 WeaponSourceRect => new(504f, 756f, 705f, 215f);
    private static readonly Vector2 WeaponSourceGrip = new(835f, 798f);
    private const float WeaponArtScale = 25f / 287f;
    private Vector2 _weaponGripLocal;
    public Rect2 WeaponDrawRect => new(
        _weaponGripLocal + (WeaponSourceRect.Position - WeaponSourceGrip) * WeaponArtScale,
        WeaponSourceRect.Size * WeaponArtScale);

    [Export] public Texture2D WeaponGripTexture { get; set; }
    public Rect2 WeaponGripSourceRect => new(810f, 777f, 49f, 45f);
    public Rect2 WeaponGripDrawRect => new(
        _weaponGripLocal + (WeaponGripSourceRect.Position - WeaponSourceGrip) * WeaponArtScale,
        WeaponGripSourceRect.Size * WeaponArtScale);

    [Export] public Texture2D WeaponSecondaryGripTexture { get; set; }
    public Rect2 WeaponSecondaryGripSourceRect => new(525f, 777f, 47f, 45f);
    public Rect2 WeaponSecondaryGripDrawRect => new(
        _weaponGripLocal + (WeaponSecondaryGripSourceRect.Position - WeaponSourceGrip) * WeaponArtScale,
        WeaponSecondaryGripSourceRect.Size * WeaponArtScale);
    internal static Vector2 WeaponSecondaryGripOffsetLocal => new(-25f, 0f);
    public Vector2 WeaponSecondaryGripLocal => _weaponGripLocal + WeaponSecondaryGripOffsetLocal;
    public bool WeaponSecondaryGripContact { get; private set; }

    public void SetWeaponSecondaryGripContact(bool contact)
    {
        if (WeaponSecondaryGripContact == contact) return;
        WeaponSecondaryGripContact = contact;
        QueueRedraw();
    }

    // Draw owns the articulated chain, not the typed shoulder/rotation slot.
    // Both links have fixed length; neither the node nor bitmap is stretched.
    private Vector2? _armWrist;
    private Vector2? _armElbow;
    private Vector2 FreeArmWrist => new(0f, Kind == PlayerFormalLayerKind.NearArm ? 21f : 19f);
    public Vector2 ArmWristLocal => _armWrist ?? FreeArmWrist;
    public Vector2 ArmElbowLocal => _armElbow ?? DefaultArmElbow(ArmWristLocal);

    private Vector2 DefaultArmElbow(Vector2 wrist)
    {
        float side = Kind == PlayerFormalLayerKind.NearArm ? 1f : -1f;
        if (wrist.LengthSquared() < .000001f) return new Vector2(-24f * side, 0f);
        Vector2 perpendicular = new(-wrist.Y, wrist.X);
        return wrist * .5f + perpendicular.Normalized() * side *
            Mathf.Sqrt(Mathf.Max(0f, 24f * 24f - wrist.LengthSquared() * .25f));
    }
    public Rect2 ArmDrawRect
    {
        get
        {
            Vector2 wrist = ArmWristLocal, elbow = ArmElbowLocal;
            Vector2 minimum = Vector2.Zero.Min(wrist).Min(elbow);
            Vector2 maximum = Vector2.Zero.Max(wrist).Max(elbow);
            float margin = (Kind == PlayerFormalLayerKind.NearArm ? 9f : 8f) * .52f + 2f;
            return new Rect2(minimum, maximum - minimum).Grow(margin);
        }
    }

    internal const float MaxArmReach = 48f; // two authored 24 px links

    public void SetArmWristLocal(Vector2 wrist)
    {
        if (!float.IsFinite(wrist.X) || !float.IsFinite(wrist.Y)) wrist = FreeArmWrist;
        double length = System.Math.Sqrt((double)wrist.X * wrist.X + (double)wrist.Y * wrist.Y);
        if (length > MaxArmReach) wrist *= (float)(MaxArmReach / length);
        if (ArmWristLocal.IsEqualApprox(wrist)) return;
        Vector2 elbow = DefaultArmElbow(wrist);
        if (_armElbow is Vector2 previous)
        {
            // Both elbow solutions have the same exact wrist and link lengths.
            // A fixed side flips at a shoulder crossing; keep the nearer actual
            // local elbow instead. Reads/Draw never advance this scalar history.
            if (wrist.LengthSquared() < .000001f)
                elbow = previous.Normalized() * 24f;
            else
            {
                Vector2 opposite = wrist - elbow;
                if (opposite.DistanceSquaredTo(previous) < elbow.DistanceSquaredTo(previous)) elbow = opposite;
            }
        }
        _armElbow = elbow;
        _armWrist = wrist;
        QueueRedraw();
    }

    public void SetWeaponGripLocal(Vector2 grip)
    {
        if (_weaponGripLocal.IsEqualApprox(grip)) return;
        _weaponGripLocal = grip;
        QueueRedraw();
    }

    private PoseTransform2D _mantleSecondary = PoseTransform2D.Identity;
    private bool _mantleMotionInParentSpace;

    public void SetMantleSecondaryMotion(PoseTransform2D motion, bool inParentSpace = false)
    {
        _mantleSecondary = motion;
        _mantleMotionInParentSpace = inParentSpace;
        QueueRedraw();
    }

    /// <summary>Shared real Draw consumer for silhouette, folds, outline and collar trim.</summary>
    public Vector2 MapMantlePoint(Vector2 restPoint)
    {
        Vector2 mapped = PlayerMantleShape.Map(restPoint, _mantleSecondary);
        if (!_mantleMotionInParentSpace) return mapped;
        if (!float.IsFinite(restPoint.X) || !float.IsFinite(restPoint.Y)) return Vector2.Zero;
        Vector2 displacement = mapped - restPoint;
        if (displacement == Vector2.Zero) return restPoint;
        // Pose/action rotation belongs to the cloth attachment, not to the
        // incoming inertial vector. Transform only the displacement; translating
        // the rest point or the whole node would unpin the shoulder band.
        Transform2D basis = Transform;
        float determinant = basis.Determinant();
        if (!float.IsFinite(determinant) || Mathf.Abs(determinant) < .000001f) return restPoint;
        Vector2 local = basis.AffineInverse().BasisXform(displacement);
        return float.IsFinite(local.X) && float.IsFinite(local.Y) ? restPoint + local : restPoint;
    }

    private Vector2[] MapMantlePoints(Vector2[] restPoints) => MapMantleEdges(restPoints, closed: true);

    private Vector2[] MapMantleEdges(Vector2[] restPoints, bool closed)
    {
        // A fixed vertex alone cannot pin the part of a straight edge above the
        // collar. Split every crossing at the exact rest-band boundary before
        // mapping. Fill includes the closing edge; the authored outline is open.
        var mapped = new List<Vector2>(restPoints.Length + 2);
        for (int index = 0; index < restPoints.Length; index++)
        {
            Vector2 start = restPoints[index];
            mapped.Add(MapMantlePoint(start));
            if (!closed && index == restPoints.Length - 1) continue;
            Vector2 end = restPoints[(index + 1) % restPoints.Length];
            if (!(start.Y < -3f && end.Y > -3f || start.Y > -3f && end.Y < -3f)) continue;
            double fraction = (-3d - start.Y) / ((double)end.Y - start.Y);
            float x = (float)(start.X + ((double)end.X - start.X) * fraction);
            mapped.Add(MapMantlePoint(new Vector2(x, -3f)));
        }
        return mapped.ToArray();
    }

    public override void _Ready()
    {
        if (Kind == PlayerFormalLayerKind.Lantern)
        {
            LanternTexture ??= GD.Load<Texture2D>(
                "res://assets/player/thread-spool-bearer/lantern.png");
            if (LanternTexture == null)
                throw new System.InvalidOperationException("Approved lantern texture is unavailable");
            TextureFilter = TextureFilterEnum.Linear;
        }
        if (Kind == PlayerFormalLayerKind.Weapon)
        {
            WeaponTexture ??= GD.Load<Texture2D>(
                "res://assets/player/thread-spool-bearer/half-bell-cleaver.png");
            if (WeaponTexture == null)
                throw new System.InvalidOperationException("Approved weapon texture is unavailable");
            WeaponGripTexture ??= GD.Load<Texture2D>(
                "res://assets/player/thread-spool-bearer/right-grip-overlay.png");
            if (WeaponGripTexture == null)
                throw new System.InvalidOperationException("Approved weapon grip overlay is unavailable");
            WeaponSecondaryGripTexture ??= GD.Load<Texture2D>(
                "res://assets/player/thread-spool-bearer/left-grip-overlay.png");
            if (WeaponSecondaryGripTexture == null)
                throw new System.InvalidOperationException("Approved secondary grip overlay is unavailable");
            TextureFilter = TextureFilterEnum.Linear;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        switch (Kind)
        {
            case PlayerFormalLayerKind.Body: DrawBody(); break;
            case PlayerFormalLayerKind.NearArm: DrawArm(9f); break;
            case PlayerFormalLayerKind.FarArm: DrawArm(8f); break;
            case PlayerFormalLayerKind.NearLeg: DrawLeg(11f); break;
            case PlayerFormalLayerKind.FarLeg: DrawLeg(10f); break;
            case PlayerFormalLayerKind.Weapon: DrawWeapon(); break;
            case PlayerFormalLayerKind.Mantle: DrawMantle(); break;
            case PlayerFormalLayerKind.ShoulderCore: DrawCore(); break;
            case PlayerFormalLayerKind.Lantern: DrawLantern(); break;
            case PlayerFormalLayerKind.Tether: DrawTether(); break;
        }
    }

    private void DrawBody()
    {
        DrawPolygon([new(-15f, 4f), new(-11f, 24f), new(11f, 24f), new(15f, 4f), new(9f, -5f), new(-9f, -5f)],
            [DeepCloth]);
        // The original undergarment contour frames a broad ceramic chest, not new armor.
        DrawPolygon([new(-11f, 5f), new(-8f, 19f), new(8f, 19f), new(11f, 5f),
                new(7f, -2f), new(-7f, -2f)], [Bone]);
        DrawPolygon([new(4f, -1f), new(10f, 5f), new(7f, 18f), new(1f, 18f)],
            [Bone.Darkened(.18f)]);
        DrawPolygon([new(-7f, -2f), new(7f, -2f), new(9f, 2f), new(-9f, 2f)],
            [BoneShadow]);
        DrawPolygon([new(-8f, 19f), new(8f, 19f), new(7f, 21f), new(-7f, 21f)],
            [Ink]);
        DrawCircle(new Vector2(0f, -18f), 15f, Ink);
        DrawCircle(new Vector2(0f, -19f), 13f, Bone);
        DrawArc(new Vector2(0f, -19f), 13f, 0f, Mathf.Tau, 32, BoneShadow, 1.5f);
        DrawLine(new Vector2(-8f, -23f), new Vector2(8f, -23f), BoneShadow, 1.2f);
        DrawLine(new Vector2(4f, -19f), new Vector2(10f, -19f), ThreadGlow, 2.4f);
    }

    private void DrawLeg(float width)
    {
        Vector2 knee=LegKneeLocal, ankle=LegAnkleLocal, sole=LegSoleLocal;
        DrawLine(LegHipLocal,knee,Ink,width+2.4f,true);
        DrawLine(knee,ankle,Ink,width+2.4f,true);
        DrawLine(LegHipLocal,knee,Bone,width,true);
        DrawLine(knee,ankle,Bone,width*.86f,true);
        DrawCircle(knee,width*.48f,Ink);
        DrawCircle(knee,width*.30f,BoneShadow);
        Vector2 forward=new(-_legUp.Y,_legUp.X);
        Vector2[] shoe=[sole-forward*5,sole+forward*7,sole+forward*6+_legUp*5,sole-forward*4+_legUp*7];
        DrawPolygon(shoe,[Bone]);
        DrawPolyline([shoe[0],shoe[1],shoe[2],shoe[3],shoe[0]],Ink,1.6f,true);
    }

    private void DrawArm(float width)
    {
        Vector2 elbow = ArmElbowLocal, wrist = ArmWristLocal;
        DrawLine(Vector2.Zero, elbow, Ink, width + 2.4f, true);
        DrawLine(elbow, wrist, Ink, width + 2.4f, true);
        DrawLine(Vector2.Zero, elbow, Bone, width, true);
        DrawLine(elbow, wrist, Bone, width, true);
        DrawCircle(Vector2.Zero, width * .5f, BoneShadow);
        DrawCircle(elbow, width * .52f, Ink);
        DrawCircle(elbow, width * .36f, BoneShadow);
        DrawCircle(wrist, width * .52f, Ink);
        DrawCircle(wrist, width * .38f, Bone);
    }

    private void DrawWeapon()
    {
        if (WeaponTexture != null)
            DrawTextureRectRegion(WeaponTexture, WeaponDrawRect, WeaponSourceRect);
        if (WeaponSecondaryGripContact && WeaponSecondaryGripTexture != null)
            DrawTextureRectRegion(WeaponSecondaryGripTexture, WeaponSecondaryGripDrawRect,
                WeaponSecondaryGripSourceRect);
        // Approved fingers occlude the handle, sharing its actual posed grip,
        // uniform art scale and the rig's single facing mirror.
        if (WeaponGripTexture != null)
            DrawTextureRectRegion(WeaponGripTexture, WeaponGripDrawRect, WeaponGripSourceRect);
    }

    private void DrawMantle()
    {
        Vector2[] restPoints = [new(-17f, -9f), new(13f, -8f), new(20f, 11f), new(13f, 29f),
            new(4f, 24f), new(-4f, 31f), new(-11f, 24f), new(-20f, 28f), new(-17f, 7f)];
        Vector2[] points = MapMantlePoints(restPoints);
        DrawPolygon(points, [ClothBody]);
        // Broad shoulder-to-hem planes sit inside one continuous cloth silhouette.
        DrawPolygon(MapMantlePoints([new(-14f, -6f), new(-7f, -6f), new(-4f, 22f),
                new(-10f, 20f), new(-15f, 23f), new(-13f, 7f)]), [ClothFold]);
        DrawPolygon(MapMantlePoints([new(-6f, -6f), new(5f, -5f), new(8f, 20f),
                new(4f, 20f), new(-2f, 26f), new(-4f, 18f)]), [ClothLight]);
        DrawPolygon(MapMantlePoints([new(6f, -5f), new(11f, -5f), new(16f, 10f),
                new(12f, 24f), new(8f, 21f)]), [ClothShade]);
        DrawPolyline(MapMantleEdges(restPoints, closed: false), ClothLight, 1.2f, true);
        DrawLine(MapMantlePoint(new Vector2(-12f, -3f)),
            MapMantlePoint(new Vector2(11f, -3f)), Brass, 1.4f);
    }

    private void DrawCore()
    {
        DrawCircle(Vector2.Zero, 8f, Ink);
        DrawCircle(Vector2.Zero, 6.5f, Brass);
        DrawCircle(Vector2.Zero, 3.5f, ThreadGlow);
        DrawArc(Vector2.Zero, 6.5f, 0f, Mathf.Tau, 24, BrassLight, 1.2f);
    }

    private void DrawLantern()
    {
        if (LanternTexture != null) DrawTextureRect(LanternTexture, LanternDrawRect, false);
    }

    private void DrawTether()
    {
        DrawPolyline([new(-2f, -1f), new(-7f, 8f), new(-3f, 16f), new(6f, 21f)], Ink, 3.2f);
        DrawPolyline([new(-2f, -1f), new(-7f, 8f), new(-3f, 16f), new(6f, 21f)], ThreadGlow.Darkened(0.2f), 1.2f);
    }

    private void DrawStyleBox(Rect2 rect, Color fill, Color outline)
    {
        DrawRect(rect, fill);
        DrawRect(rect, outline, false, 1.2f);
    }
}
