using System;

namespace Knight.Audio;

public enum AudioQuitDrainState { Waiting, Ready, Cancelled, TimedOut, InvalidObservation }

public readonly record struct AudioQuitDrainObservation(ulong ElapsedMilliseconds, ulong ProcessFrame,
    double MixAgeSeconds, bool OwnerValid, bool PlayersSilent, bool OwnedStreamsReleased);

public readonly record struct AudioQuitDrainResult(AudioQuitDrainState State, ulong ElapsedMilliseconds,
    int ObservedProcessFrames, bool MixObserved);

// Policy only: actual observations are supplied by the live audio owner.
public sealed class AudioQuitDrainGate
{
    public const ulong DeadlineMilliseconds = 2000;
    private ulong _lastFrame, _lastElapsed;
    private double _previousMixAge;
    private int _postMixFrames, _frames;
    private bool _mixObserved;
    public AudioQuitDrainResult Result { get; private set; }

    public AudioQuitDrainGate(double initialMixAgeSeconds, ulong initialProcessFrame)
    {
        _lastFrame = initialProcessFrame;
        _previousMixAge = initialMixAgeSeconds;
        Result = new(ValidAge(initialMixAgeSeconds) ? AudioQuitDrainState.Waiting :
            AudioQuitDrainState.InvalidObservation, 0, 0, false);
    }

    public AudioQuitDrainResult Observe(AudioQuitDrainObservation observation)
    {
        if (Result.State != AudioQuitDrainState.Waiting) return Result;
        if (!observation.OwnerValid) return End(AudioQuitDrainState.Cancelled, observation.ElapsedMilliseconds);
        if (!ValidAge(observation.MixAgeSeconds) || observation.ProcessFrame < _lastFrame ||
            observation.ElapsedMilliseconds < _lastElapsed)
            return End(AudioQuitDrainState.InvalidObservation, observation.ElapsedMilliseconds);
        _lastElapsed = observation.ElapsedMilliseconds;
        if (_lastElapsed >= DeadlineMilliseconds) return End(AudioQuitDrainState.TimedOut, _lastElapsed);
        if (observation.ProcessFrame == _lastFrame) return End(AudioQuitDrainState.Waiting, _lastElapsed);
        _lastFrame = observation.ProcessFrame;
        _frames++;
        if (_mixObserved) _postMixFrames++;
        else if (observation.MixAgeSeconds < _previousMixAge) _mixObserved = true;
        _previousMixAge = observation.MixAgeSeconds;
        return End(_mixObserved && _postMixFrames >= 2 && observation.PlayersSilent &&
            observation.OwnedStreamsReleased ? AudioQuitDrainState.Ready : AudioQuitDrainState.Waiting, _lastElapsed);
    }

    private AudioQuitDrainResult End(AudioQuitDrainState state, ulong elapsed) =>
        Result = new(state, elapsed, _frames, _mixObserved);
    private static bool ValidAge(double age) => double.IsFinite(age) && age >= 0d;
}
