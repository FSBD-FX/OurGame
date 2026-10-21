namespace Knight.Prologue.Collections;

public enum FragmentCollectionResult
{
    Collected, AlreadyCollected, InvalidId, SaveFailed,
    // A paused, dead, title or recovering session is not a failed disk write.
    Unavailable
}
