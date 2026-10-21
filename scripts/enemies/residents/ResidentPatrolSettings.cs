using System.Collections.Generic;

namespace Knight.Enemies.Residents;

public sealed record ResidentPatrolSettings(float Speed = 48f, float ArrivalTolerance = 2f)
{
    public IReadOnlyList<string> Validate()
    {
        var errors=new List<string>();
        if(!float.IsFinite(Speed)||Speed<=0) errors.Add("speed");
        if(!float.IsFinite(ArrivalTolerance)||ArrivalTolerance<=0) errors.Add("arrival_tolerance");
        return errors;
    }
}
