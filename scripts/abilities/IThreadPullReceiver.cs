using Godot;

namespace Knight.Abilities;

public interface IThreadPullReceiver
{
    void ApplyThreadPull(Vector2 velocity);
}
