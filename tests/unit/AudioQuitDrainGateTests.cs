using System;
using Godot;

namespace Knight.Tests.Unit;

public static class AudioQuitDrainGateTests
{
    public static void Run()
    {
        var assembly = typeof(Knight.Audio.PrologueAudioDirector).Assembly;
        Type gateType = assembly.GetType("Knight.Audio.AudioQuitDrainGate");
        Type inputType = assembly.GetType("Knight.Audio.AudioQuitDrainObservation");
        TestAssert.True(gateType != null && inputType != null,
            "APPLICATION_QUIT_GATE_RED: actual mixer/owner/deadline gate is missing");
        object New(double age = .04d) => Activator.CreateInstance(gateType!, age, 10UL)!;
        string Step(object gate, ulong ms, ulong frame, double age, bool owner = true,
            bool silent = true, bool released = true)
        {
            object input = Activator.CreateInstance(inputType!, ms, frame, age, owner, silent, released)!;
            object result = gateType!.GetMethod("Observe")!.Invoke(gate, [input])!;
            return result.GetType().GetProperty("State")!.GetValue(result)!.ToString()!;
        }
        void Want(object gate, string state, ulong ms, ulong frame, double age,
            bool owner = true, bool silent = true, bool released = true) =>
            TestAssert.Equal(state, Step(gate, ms, frame, age, owner, silent, released),
                "APPLICATION_QUIT_GATE_RED: independent cleanup-boundary expectation");

        object g = New();
        Want(g, "Waiting", 0, 10, .001d); // Duplicate cannot invent a mix.
        Want(g, "Waiting", 1, 11, .05d);
        Want(g, "Waiting", 2, 12, .001d); // Actual wrap, but no post-mix cleanup yet.
        Want(g, "Waiting", 3, 12, .002d); // Duplicate cannot count cleanup.
        Want(g, "Waiting", 4, 13, .003d);
        Want(g, "Ready", 5, 14, .004d);
        Want(g, "Ready", 6, 15, .005d, owner: false); // Stable terminal result.

        g = New();
        Want(g, "Waiting", 1, 11, .001d, silent: false);
        Want(g, "Waiting", 2, 12, .002d, silent: false);
        Want(g, "Waiting", 3, 13, .003d, silent: false);
        Want(g, "Ready", 4, 14, .004d);

        g = New();
        Want(g, "Waiting", 1, 11, .001d, released: false);
        Want(g, "Waiting", 2, 12, .002d, released: false);
        Want(g, "Waiting", 3, 13, .003d, released: false);
        Want(g, "Ready", 4, 14, .004d);

        g = New();
        Want(g, "Cancelled", 0, 10, .04d, owner: false);
        Want(g, "Cancelled", 1, 11, .001d);
        g = New();
        Want(g, "Waiting", 1999, 11, .05d);
        Want(g, "TimedOut", 2000, 12, .001d);
        Want(g, "TimedOut", 2001, 13, .002d);
        g = New();
        Want(g, "Waiting", 1, 11, .04d); // Equal ages are not a mix.
        Want(g, "Waiting", 1999, 11, .001d);
        Want(g, "TimedOut", 2000, 11, .001d);
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, -.001d })
        {
            g = New(); Want(g, "InvalidObservation", 1, 11, bad);
            g = New(bad); Want(g, "InvalidObservation", 1, 11, .001d);
        }
        g = New(); Want(g, "InvalidObservation", 1, 9, .001d);
        g = New();
        Want(g, "Waiting", 5, 11, .05d);
        Want(g, "InvalidObservation", 4, 12, .001d);
        GD.Print("APPLICATION_QUIT_GATE_OK distinct_frames=true mix_then_cleanup=true owner=true deadline=true invalid=true terminal=true");
    }
}
