namespace Knight.Mechanisms;

public interface IThreadInteractionReceiver
{
    bool TryReceiveThread(ThreadInteraction interaction);
}
