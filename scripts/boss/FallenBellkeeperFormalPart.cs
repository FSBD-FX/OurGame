using Godot;

namespace Knight.Boss;

public enum FallenBellkeeperPartKind
{
    Body,
    ArmorShell,
    Hammer,
    Chain,
    Core
}

public partial class FallenBellkeeperFormalPart : Node2D
{
    private static readonly Color Ink = new("101416");
    private static readonly Color BodyDark = new("40353a");
    private static readonly Color BodyLight = new("75616a");
    private static readonly Color Brass = new("9f7437");
    private static readonly Color BrassLight = new("d9b968");
    private static readonly Color Patina = new("315b55");
    private static readonly Color CoreGlow = new("75e3d4");

    [Export] public FallenBellkeeperPartKind Kind { get; set; }
    public float ContactIntensity { get; private set; }

    public override void _Ready() => QueueRedraw();

    public void SetContactIntensity(float intensity)
    {
        ContactIntensity = Mathf.Clamp(intensity, 0f, 1f);
        QueueRedraw();
    }

    public override void _Draw()
    {
        switch (Kind)
        {
            case FallenBellkeeperPartKind.Body: DrawBody(); break;
            case FallenBellkeeperPartKind.ArmorShell: DrawArmor(); break;
            case FallenBellkeeperPartKind.Hammer: DrawHammer(); break;
            case FallenBellkeeperPartKind.Chain: DrawChain(); break;
            case FallenBellkeeperPartKind.Core: DrawCore(); break;
        }
    }

    private void DrawBody()
    {
        DrawPolygon([new(-43f, -55f), new(32f, -62f), new(48f, 42f), new(22f, 63f),
            new(-37f, 59f), new(-52f, 23f)], [Ink]);
        DrawPolygon([new(-36f, -49f), new(26f, -55f), new(39f, 38f), new(17f, 54f),
            new(-31f, 51f), new(-44f, 20f)], [Contact(BodyDark)]);
        DrawPolyline([new(-27f, -19f), new(21f, -25f), new(30f, 26f)], BodyLight, 4f);
        DrawCircle(new Vector2(17f, -58f), 15f, Ink);
        DrawCircle(new Vector2(17f, -58f), 11f, new Color("cfc7b4"));
        DrawLine(new Vector2(25f, -59f), new Vector2(34f, -59f), CoreGlow, 3f);
        DrawLine(new Vector2(-27f, 54f), new Vector2(-31f, 74f), Ink, 16f);
        DrawLine(new Vector2(19f, 54f), new Vector2(25f, 74f), Ink, 16f);
    }

    private void DrawArmor()
    {
        Vector2[] outer = [new(-68f, -62f), new(54f, -66f), new(73f, 43f), new(50f, 57f),
            new(2f, 46f), new(-42f, 61f), new(-78f, 34f)];
        DrawPolygon(outer, [Ink]);
        DrawPolygon([new(-60f, -54f), new(47f, -58f), new(64f, 37f), new(46f, 48f),
            new(2f, 38f), new(-39f, 52f), new(-69f, 29f)], [Contact(Brass)]);
        DrawArc(new Vector2(-5f, -3f), 43f, -2.7f, 0.4f, 30, BrassLight, 5f);
        DrawPolyline([new(-51f, -20f), new(-17f, -5f), new(5f, -32f), new(25f, 8f), new(55f, -7f)],
            Patina, 4f);
        DrawLine(new Vector2(-62f, 31f), new Vector2(57f, 35f), Ink, 7f);
    }

    private void DrawHammer()
    {
        DrawLine(new Vector2(-8f, 35f), new Vector2(50f, -28f), Ink, 17f);
        DrawLine(new Vector2(-6f, 32f), new Vector2(48f, -26f), Contact(BrassLight), 8f);
        DrawPolygon([new(21f, -62f), new(76f, -56f), new(85f, -18f), new(35f, -8f)], [Ink]);
        DrawPolygon([new(27f, -56f), new(70f, -51f), new(78f, -23f), new(39f, -15f)], [Contact(Brass)]);
        DrawLine(new Vector2(34f, -49f), new Vector2(67f, -45f), Patina, 4f);
    }

    private void DrawChain()
    {
        for (int index = 0; index < 5; index++)
        {
            Vector2 center = new(index * 15f, index * 7f);
            DrawArc(center, 9f, 0f, Mathf.Tau, 18, Ink, 5f);
            DrawArc(center, 7f, 0f, Mathf.Tau, 18, BrassLight, 2f);
        }
    }

    private void DrawCore()
    {
        DrawCircle(Vector2.Zero, 22f, new Color(0.12f, 0.38f, 0.36f, 0.35f));
        DrawCircle(Vector2.Zero, 15f, Ink);
        DrawCircle(Vector2.Zero, 11f, Contact(CoreGlow));
        DrawArc(Vector2.Zero, 17f, 0f, Mathf.Tau, 32, BrassLight, 4f);
        DrawLine(new Vector2(-22f, 0f), new Vector2(-35f, -14f), CoreGlow, 3f);
        DrawLine(new Vector2(22f, 0f), new Vector2(35f, 14f), CoreGlow, 3f);
    }

    private Color Contact(Color color) => color.Lerp(Colors.White, ContactIntensity);
}
