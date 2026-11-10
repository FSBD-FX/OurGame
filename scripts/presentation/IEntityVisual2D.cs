namespace Knight.Presentation;

public interface IEntityVisual2D
{
    void ApplyFrame(EntityPresentationFrame frame);
    void PlayCue(PresentationCue cue);
    void ClearCueOverride() { }
}
