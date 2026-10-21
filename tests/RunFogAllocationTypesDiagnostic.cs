namespace Knight.Tests;

public partial class RunFogAllocationTypesDiagnostic : RunFogCostDiagnostic
{
    protected override bool TrackManagedAllocationTypes => true;
}
