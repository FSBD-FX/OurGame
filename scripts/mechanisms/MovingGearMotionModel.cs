using System;
using Godot;

namespace Knight.Mechanisms;

public readonly record struct MovingGearMotionFrame(Vector2 Offset,Vector2 Velocity,
    Vector2 SweepFromOffset,Vector2 SweepToOffset,double Phase01);

// A bounded phase and the exact visited interval of a translated circle. A
// reversal/full cycle cannot collapse its damage envelope to the two endpoints.
public sealed class MovingGearMotionModel
{
    public Vector2 TravelOffset { get; }
    public float PeriodSeconds { get; }
    public MovingGearMotionFrame Current { get; private set; }
    private double _phaseSeconds;

    public MovingGearMotionModel(Vector2 travelOffset,float periodSeconds)
    {
        double length=Math.Sqrt((double)travelOffset.X*travelOffset.X+(double)travelOffset.Y*travelOffset.Y);
        if(!travelOffset.IsFinite() || !double.IsFinite(length) || length<=0 || length>512)
            throw new ArgumentOutOfRangeException(nameof(travelOffset),"Moving gear travel must have finite length in (0,512].");
        if(!float.IsFinite(periodSeconds) || periodSeconds<1 || periodSeconds>20 || length*Math.PI/periodSeconds>600)
            throw new ArgumentOutOfRangeException(nameof(periodSeconds),"Moving gear period must be 1..20 seconds with peak speed at most 600.");
        TravelOffset=travelOffset; PeriodSeconds=periodSeconds;
        Reset();
    }

    public MovingGearMotionFrame Step(double delta)
    {
        double before=Fraction(_phaseSeconds);
        if(!double.IsFinite(delta) || delta<=0)
            return Current=Frame(before,before);

        // Reduce first: adding a huge cycle count must not erase the current
        // fractional phase before the modulus can recover it.
        double oldPhase=_phaseSeconds;
        _phaseSeconds=(oldPhase+delta%PeriodSeconds)%PeriodSeconds;
        double after=Fraction(_phaseSeconds),minimum=Math.Min(before,after),maximum=Math.Max(before,after);
        if(delta>=PeriodSeconds) { minimum=0; maximum=1; }
        else
        {
            double half=PeriodSeconds*.5d,end=oldPhase+delta;
            int first=(int)Math.Floor(oldPhase/half)+1,last=(int)Math.Floor(end/half);
            for(int turn=first;turn<=last;turn++)
                if((turn&1)==0) minimum=0; else maximum=1;
        }
        return Current=Frame(minimum,maximum);
    }

    public void Reset()
    {
        _phaseSeconds=0;
        Current=new(Vector2.Zero,Vector2.Zero,Vector2.Zero,Vector2.Zero,0);
    }

    private double Fraction(double seconds)=>.5d*(1d-Math.Cos(2d*Math.PI*seconds/PeriodSeconds));
    private MovingGearMotionFrame Frame(double minimum,double maximum)
    {
        double phase=_phaseSeconds/PeriodSeconds;
        float velocity=(float)(Math.PI/PeriodSeconds*Math.Sin(2d*Math.PI*phase));
        return new(TravelOffset*(float)Fraction(_phaseSeconds),TravelOffset*velocity,
            TravelOffset*(float)minimum,TravelOffset*(float)maximum,phase);
    }
}
