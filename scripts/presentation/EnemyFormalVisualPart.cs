using Godot;

namespace Knight.Presentation;

public enum EnemyFormalPartKind
{
    RustCorruption,
    RustBody,
    RustShell,
    RustWeapon,
    MothCorruption,
    MothFarWing,
    MothBody,
    MothLamp,
    LaborerCorruption,
    LaborerBell,
    LaborerBody,
    LaborerFeet,
    LaborerWeapon,
    MothNearWing
}

/// <summary>
/// Real renderer mounted directly at each ordinary-enemy replacement slot.
/// Runtime pose is owned by this node, so future art inserted below the stable
/// slot inherits the same phase-driven transform without changing gameplay code.
/// </summary>
public partial class EnemyFormalVisualPart : Node2D
{
    private static readonly Color Ink = new("111416");
    private static readonly Color Rust = new("74402c");
    private static readonly Color RustLight = new("ad6e42");
    private static readonly Color Bone = new("d8d0b9");
    private static readonly Color Warning = new("d7a84a");
    private static readonly Color Wing = new("294b4c");
    private static readonly Color WingEdge = new("74a6a0");
    private static readonly Color Brass = new("b88a42");
    private static readonly Color BrassLight = new("dfbf70");
    private static readonly Color Glow = new("75d9cf");
    private static readonly Color LaborerBody = new("574438");

    [Export] public EnemyFormalPartKind Kind { get; set; }
    public float ContactIntensity { get; private set; }
    public float HurtFlashIntensity { get; private set; }

    public override void _Ready() => QueueRedraw();

    public void SetFeedback(float contactIntensity, float hurtFlashIntensity)
    {
        ContactIntensity = Mathf.Clamp(contactIntensity, 0f, 1f);
        HurtFlashIntensity = Mathf.Clamp(hurtFlashIntensity, 0f, 1f);
        QueueRedraw();
    }

    public override void _Draw()
    {
        switch (Kind)
        {
            case EnemyFormalPartKind.RustCorruption:
                DrawCircle(Vector2.Zero, 10f, new Color("315b55", .32f));
                break;
            case EnemyFormalPartKind.RustBody:
                DrawRustBody();
                break;
            case EnemyFormalPartKind.RustShell:
                DrawRustShell();
                break;
            case EnemyFormalPartKind.RustWeapon:
                DrawLine(Vector2.Zero, new Vector2(38f, -2f), Ink, 6f);
                DrawLine(Vector2.Zero, new Vector2(38f, -2f), Bone, 2.2f);
                break;
            case EnemyFormalPartKind.MothCorruption:
                DrawCircle(Vector2.Zero, 11f, new Color("315b55", .30f));
                break;
            case EnemyFormalPartKind.MothFarWing:
                DrawMothWing(-1);
                break;
            case EnemyFormalPartKind.MothNearWing:
                DrawMothWing(1);
                break;
            case EnemyFormalPartKind.MothBody:
                DrawMothBody();
                break;
            case EnemyFormalPartKind.MothLamp:
                DrawCircle(new Vector2(0f, 1f), 7f, Ink);
                DrawCircle(new Vector2(0f, 1f), 4.5f, Glow);
                break;
            case EnemyFormalPartKind.LaborerCorruption:
                DrawCircle(Vector2.Zero, 12f, new Color("315b55", .28f));
                break;
            case EnemyFormalPartKind.LaborerBell:
                DrawLaborerBell();
                break;
            case EnemyFormalPartKind.LaborerBody:
                DrawLaborerBody();
                break;
            case EnemyFormalPartKind.LaborerFeet:
                DrawLine(new Vector2(-14f, 6f), new Vector2(-16f, 24f), Ink, 11f);
                DrawLine(new Vector2(14f, 6f), new Vector2(17f, 24f), Ink, 11f);
                break;
            case EnemyFormalPartKind.LaborerWeapon:
                DrawLaborerWeapon();
                break;
        }
    }

    private Color Feedback(Color color) =>
        color.Lerp(Colors.White, Mathf.Max(ContactIntensity, HurtFlashIntensity));

    private void DrawRustBody()
    {
        DrawPolygon([new(-25f, -17f), new(-13f, -29f), new(12f, -27f), new(27f, -11f),
            new(22f, 15f), new(-22f, 15f), new(-30f, 2f)], [Ink]);
        DrawLine(new Vector2(-15f, 12f), new Vector2(-17f, 22f), Ink, 6f);
        DrawLine(new Vector2(12f, 12f), new Vector2(14f, 22f), Ink, 6f);
    }

    private void DrawRustShell()
    {
        Vector2 offset = new(2f, 2f);
        DrawPolygon([new Vector2(-22f, -15f) + offset, new Vector2(-11f, -26f) + offset,
            new Vector2(10f, -24f) + offset, new Vector2(23f, -10f) + offset,
            new Vector2(18f, 10f) + offset, new Vector2(-19f, 10f) + offset,
            new Vector2(-26f, 0f) + offset], [Feedback(Rust)]);
        DrawPolyline([new Vector2(-19f, -13f) + offset, new Vector2(16f, -12f) + offset,
            new Vector2(20f, -2f) + offset], RustLight, 3f);
        DrawPolyline([new Vector2(-15f, -4f) + offset,
            new Vector2(19f, -3f) + offset], Ink, 3f);
        DrawCircle(new Vector2(16f, -13f) + offset, 4.2f, Warning);
        DrawCircle(new Vector2(17f, -13f) + offset, 1.8f, Ink);
    }

    private void DrawMothWing(int side)
    {
        Vector2 origin = new(side * 7f, -5f);
        Vector2[] points = side < 0
            ? [origin, origin + new Vector2(-34f, -18f), origin + new Vector2(-43f, 2f),
                origin + new Vector2(-22f, 19f)]
            : [origin, origin + new Vector2(34f, -18f), origin + new Vector2(43f, 2f),
                origin + new Vector2(22f, 19f)];
        DrawPolygon(points, [Ink]);
        Vector2[] inner = side < 0
            ? [origin + new Vector2(-3f, 0f), origin + new Vector2(-31f, -13f),
                origin + new Vector2(-36f, 1f), origin + new Vector2(-20f, 14f)]
            : [origin + new Vector2(3f, 0f), origin + new Vector2(31f, -13f),
                origin + new Vector2(36f, 1f), origin + new Vector2(20f, 14f)];
        DrawPolygon(inner, [Wing]);
        DrawPolyline(inner, WingEdge, 1.7f, true);
    }

    private void DrawMothBody()
    {
        DrawPolygon([new(0f, -24f), new(13f, -5f), new(8f, 20f), new(0f, 27f),
            new(-8f, 20f), new(-13f, -5f)], [Ink]);
        DrawPolygon([new(0f, -20f), new(9f, -4f), new(5f, 16f), new(0f, 22f),
            new(-5f, 16f), new(-9f, -4f)], [Feedback(new Color("c39a4c"))]);
        DrawLine(new Vector2(0f, 20f), new Vector2(-7f, 31f), WingEdge, 2f);
        DrawLine(new Vector2(0f, 20f), new Vector2(7f, 31f), WingEdge, 2f);
    }

    private void DrawLaborerBody()
    {
        DrawPolygon([new(-24f, -38f), new(19f, -37f), new(27f, 28f), new(-27f, 28f)], [Ink]);
        DrawPolygon([new(-20f, -34f), new(15f, -33f), new(21f, 24f), new(-22f, 24f)],
            [Feedback(LaborerBody)]);
        DrawCircle(new Vector2(10f, -42f), 11f, Ink);
        DrawCircle(new Vector2(10f, -42f), 8f, Bone);
        DrawLine(new Vector2(17f, -43f), new Vector2(22f, -43f), Glow, 2.5f);
    }

    private void DrawLaborerWeapon()
    {
        DrawLine(Vector2.Zero, new Vector2(56f, 0f), Ink, 11f);
        DrawLine(new Vector2(7f, 0f), new Vector2(55f, 0f), BrassLight, 5f);
        DrawCircle(new Vector2(55f, 0f), 12f, Ink);
        DrawCircle(new Vector2(55f, 0f), 8f, Brass);
    }

    private void DrawLaborerBell()
    {
        Vector2 offset = new(-1f, -7f);
        DrawPolygon([new Vector2(-34f, -31f) + offset, new Vector2(22f, -34f) + offset,
            new Vector2(35f, 19f) + offset, new Vector2(-41f, 19f) + offset], [Ink]);
        DrawPolygon([new Vector2(-29f, -26f) + offset, new Vector2(18f, -29f) + offset,
            new Vector2(29f, 14f) + offset, new Vector2(-35f, 14f) + offset], [Brass]);
        DrawArc(new Vector2(-4f, -5f) + offset, 22f, .1f, 3f, 20, BrassLight, 3f);
        DrawLine(new Vector2(-35f, 14f) + offset, new Vector2(29f, 14f) + offset, Ink, 5f);
    }
}
