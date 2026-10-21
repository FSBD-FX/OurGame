using Godot;

namespace Knight.Player;

public sealed class PlayerCombatModel
{
    private readonly PlayerCombatSettings _settings;
    private ulong _nextAttackId;
    private ulong _activeAttackId;
    private AttackDirection _direction;
    private float _elapsed;
    private bool _attacking;

    public PlayerCombatModel(PlayerCombatSettings settings = null)
    {
        _settings = settings ?? new PlayerCombatSettings();
    }

    public bool IsAttacking => _attacking;

    public ulong? TryStart(AttackDirection direction)
    {
        if (_attacking) return null;
        _activeAttackId = ++_nextAttackId;
        _direction = direction;
        _elapsed = 0f;
        _attacking = true;
        return _activeAttackId;
    }

    public AttackFrame Tick(double delta)
    {
        if (!_attacking) return new AttackFrame(0UL, AttackDirection.Horizontal, false, false);
        _elapsed += Mathf.Max(0f, (float)delta);
        float startupEnd = _settings.StartupSeconds;
        float activeEnd = startupEnd + _settings.ActiveSeconds;
        float end = activeEnd + _settings.RecoverySeconds;
        bool active = _elapsed >= startupEnd && _elapsed < activeEnd;
        bool finished = _elapsed >= end;
        AttackFrame frame = new(_activeAttackId, _direction, active, finished);
        if (finished) _attacking = false;
        return frame;
    }

    public void Reset()
    {
        _activeAttackId = 0UL;
        _elapsed = 0f;
        _attacking = false;
    }

    public static AttackDirection ResolveDirection(Vector2 aim, bool airborne, int facing, out int horizontalFacing,
        float aimDeadZone = 0.5f)
    {
        horizontalFacing = ResolveHorizontalFacing(aim, facing, aimDeadZone);
        if (aim.Y < -aimDeadZone) return AttackDirection.Up;
        if (airborne && aim.Y > aimDeadZone) return AttackDirection.Down;
        return AttackDirection.Horizontal;
    }

    public static AttackDirection ResolveDirection(Vector2 aim, bool airborne, int facing, float aimDeadZone = 0.5f) =>
        ResolveDirection(aim, airborne, facing, out _, aimDeadZone);

    public static int ResolveHorizontalFacing(Vector2 aim, int facing, float aimDeadZone = 0.5f)
    {
        if (aim.X < -aimDeadZone) return -1;
        if (aim.X > aimDeadZone) return 1;
        return facing < 0 ? -1 : 1;
    }
}
