using Godot;

namespace Knight.Presentation;

public partial class PlayerPlaceholderVisual : Node2D, IEntityVisual2D
{
    private static readonly Color BoneWhite = new("e8dfc6");
    private static readonly Color Copper = new("b87333");
    private static readonly Color ThreadGlow = new("6ad6e8");
    private EntityPresentationFrame _frame = new(EntityVisualState.Idle, 1, true);
    private float _cueFlashSeconds;
    private PresentationCue _lastCue;

    public bool ThreadCastPoseActive => _cueFlashSeconds > 0f && PlayerAnimationMap.IsThreadCastCue(_lastCue);
    public bool ThreadFailedPoseActive => _cueFlashSeconds > 0f && _lastCue == PresentationCue.ThreadFailed;
    public bool ThreadGlowActive => ThreadCastPoseActive || ThreadFailedPoseActive;
    public Vector2 CurrentBodySize => ThreadCastPoseActive
        ? new Vector2(46f, 48f)
        : ThreadFailedPoseActive ? new Vector2(48f, 44f) : ResolveBodySize();
    public Vector2 CurrentWeaponStart => ThreadCastPoseActive
        ? new Vector2(Facing * 13f, -7f)
        : ThreadFailedPoseActive ? new Vector2(Facing * 9f, -3f) : new Vector2(Facing * 16f, 7f);
    public Vector2 CurrentWeaponEnd => ThreadCastPoseActive
        ? new Vector2(Facing * 72f, -21f)
        : ThreadFailedPoseActive
            ? new Vector2(Facing * 43f, 6f)
            : new Vector2(Facing * (16f + DefaultWeaponLength), 7f);

    private int Facing => _frame.Facing < 0 ? -1 : 1;
    private float DefaultWeaponLength => _frame.State == EntityVisualState.Attack ? 54f : 30f;

    public override void _Process(double delta)
    {
        _cueFlashSeconds = Mathf.Max(0f, _cueFlashSeconds - (float)delta);
        QueueRedraw();
    }

    public void ApplyFrame(EntityPresentationFrame frame)
    {
        // The coordinator applies locomotion first and then replays an active thread phase cue.
        // Clearing here makes the first non-thread frame return to its real movement pose without
        // leaving a cue-timer silhouette behind in the next room.
        if (ThreadCastPoseActive || ThreadFailedPoseActive) _cueFlashSeconds = 0f;
        _frame = frame;
        QueueRedraw();
    }

    public void PlayCue(PresentationCue cue)
    {
        _lastCue = cue;
        _cueFlashSeconds = 0.12f;
        QueueRedraw();
    }

    public void ClearCueOverride()
    {
        _cueFlashSeconds = 0f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        int facing = Facing;
        bool threadCast = ThreadCastPoseActive;
        bool threadFailed = ThreadFailedPoseActive;
        Vector2 bodySize = CurrentBodySize;
        Vector2 bodyCenter = threadCast
            ? new Vector2(-facing * 3f, 2f)
            : threadFailed ? new Vector2(-facing * 4f, 5f)
            : _frame.State == EntityVisualState.Dead ? new Vector2(0f, 18f) : Vector2.Zero;
        Color bodyColor = threadFailed
            ? new Color("e6aa98")
            : _cueFlashSeconds > 0f && _lastCue == PresentationCue.Hurt
            ? new Color(1f, 0.88f, 0.72f)
            : BoneWhite;

        DrawRect(new Rect2(bodyCenter - bodySize / 2f, bodySize), bodyColor);
        if (_frame.State != EntityVisualState.Dead)
        {
            Vector2 head = threadCast
                ? new Vector2(facing * 7f, -bodySize.Y / 2f - 2f)
                : threadFailed ? new Vector2(-facing * 2f, -bodySize.Y / 2f + 2f)
                : new Vector2(facing * 3f, -bodySize.Y / 2f);
            DrawCircle(head, 17f, bodyColor);
            if (threadCast)
            {
                DrawLine(new Vector2(-facing * 9f, 22f), new Vector2(-facing * 19f, 38f),
                    bodyColor, 10f, true);
                DrawLine(new Vector2(facing * 7f, 22f), new Vector2(facing * 22f, 36f),
                    bodyColor, 10f, true);
                DrawCircle(new Vector2(-facing * 13f, -9f), 10f, Copper);
                DrawCircle(new Vector2(-facing * 13f, -9f), 5f, ThreadGlow);
                DrawLine(new Vector2(facing * 4f, -1f), CurrentWeaponStart, Copper, 9f, true);
                DrawLine(CurrentWeaponStart, CurrentWeaponEnd, Copper, 9f, true);
                DrawLine(CurrentWeaponStart, CurrentWeaponEnd, ThreadGlow, 3f, true);
                DrawCircle(CurrentWeaponEnd, 3.5f, ThreadGlow);
            }
            else if (threadFailed)
            {
                DrawCircle(new Vector2(-facing * 12f, -8f), 9f, Copper);
                DrawCircle(new Vector2(-facing * 12f, -8f), 4f, new Color("b75a4b"));
                DrawLine(new Vector2(facing * 2f, 1f), CurrentWeaponStart, Copper, 8f, true);
                DrawLine(CurrentWeaponStart, CurrentWeaponEnd, Copper, 7f, true);
                DrawLine(CurrentWeaponStart, CurrentWeaponEnd, new Color("d87862"), 2.5f, true);
                DrawLine(CurrentWeaponEnd + new Vector2(-facing * 4f, -4f),
                    CurrentWeaponEnd + new Vector2(facing * 4f, 4f), new Color("b75a4b"), 2f, true);
            }
            else
            {
                DrawCircle(new Vector2(facing * 8f, 8f), 6f, Copper);
            }
        }

        if (!threadCast && !threadFailed)
            DrawRect(new Rect2(facing > 0 ? 16f : -16f - DefaultWeaponLength, 2f,
                DefaultWeaponLength, 10f), Copper);
        if (threadCast)
            DrawArc(Vector2.Zero, 31f, -1.1f, 1.1f, 14, ThreadGlow, 4f);
        else if (threadFailed)
            DrawArc(Vector2.Zero, 29f, -1.1f, 1.1f, 10, new Color("b75a4b"), 3f);
    }

    private Vector2 ResolveBodySize() => _frame.State switch
    {
        EntityVisualState.Move => new Vector2(42f, 52f),
        EntityVisualState.Rise => new Vector2(36f, 58f),
        EntityVisualState.Fall => new Vector2(44f, 50f),
        EntityVisualState.Dash => new Vector2(58f, 34f),
        EntityVisualState.Attack => new Vector2(40f, 52f),
        EntityVisualState.Hurt => new Vector2(46f, 48f),
        EntityVisualState.Dead => new Vector2(58f, 20f),
        _ => new Vector2(40f, 56f)
    };
}
