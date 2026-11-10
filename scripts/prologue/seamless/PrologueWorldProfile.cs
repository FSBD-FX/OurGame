using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Presentation.Fog;

namespace Knight.Prologue.Seamless;

public sealed record PrologueProfileCheckpoint(string StableId,PrologueSectorId SectorId,Vector2 WorldPosition,int Order);

// Plain immutable context: saved scenes serialize its strict registry key, not a native Resource.
public sealed class PrologueWorldProfile
{
    public const int LayoutVersion=3;
    public string Id {get;}
    public int SaveRuleVersion=>1;
    public Rect2 DesignBounds {get;}
    public PrologueProfileCheckpoint Start=>Checkpoints[0];
    public IReadOnlyList<PrologueSectorId> SectorIds {get;}
    public IReadOnlyList<PrologueProfileCheckpoint> Checkpoints {get;}
    public IReadOnlyList<PrologueFogField> FogFields {get;}
    public int AuthoredMainSegments {get;}
    public int AuthoredBranches {get;}
    public int MinimapCellSize {get;}
    private readonly Dictionary<string,PrologueProfileCheckpoint> _checkpoints;
    internal PrologueWorldProfile(string id,Rect2 bounds,PrologueSectorId[] sectors,PrologueProfileCheckpoint[] checkpoints,
        int authoredMainSegments=4,int authoredBranches=1,int minimapCellSize=8)
    {
        if(string.IsNullOrWhiteSpace(id) || minimapCellSize is not (8 or 16) || authoredMainSegments is <1 or >24 || authoredBranches is <1 or >10 || !PrologueSectorDefinition.FinitePositive(bounds) ||
            sectors==null || sectors.Length==0 || sectors.Distinct().Count()!=sectors.Length || sectors.Any(x=>!Enum.IsDefined(x)) ||
            checkpoints==null || checkpoints.Length==0 || checkpoints[0]?.Order!=0 ||
            checkpoints.Any(c=>c==null || string.IsNullOrWhiteSpace(c.StableId) || !sectors.Contains(c.SectorId) ||
                !PrologueSectorDefinition.Finite(c.WorldPosition) || !bounds.HasPoint(c.WorldPosition)) ||
            checkpoints.Select(c=>c.StableId).Distinct(StringComparer.Ordinal).Count()!=checkpoints.Length ||
            !checkpoints.Select(c=>c.Order).SequenceEqual(Enumerable.Range(0,checkpoints.Length)))
            throw new ArgumentException("Invalid immutable world profile.");
        Id=id;DesignBounds=bounds;AuthoredMainSegments=authoredMainSegments;AuthoredBranches=authoredBranches;MinimapCellSize=minimapCellSize;
        SectorIds=Array.AsReadOnly((PrologueSectorId[])sectors.Clone());
        Checkpoints=Array.AsReadOnly((PrologueProfileCheckpoint[])checkpoints.Clone());
        _checkpoints=Checkpoints.ToDictionary(c=>c.StableId,StringComparer.Ordinal);
        FogFields=Array.AsReadOnly(new[]{new PrologueFogField(bounds,PrologueFogProfile.Exploration,64f)}
            .Concat(Checkpoints.Select(c=>new PrologueFogField(new(c.WorldPosition-new Vector2(96,64),new Vector2(192,128)),
                PrologueFogProfile.RestArea,32f))).ToArray());
    }
    public bool TryResolveCheckpoint(string id,out PrologueSectorId sector,out Vector2 position)
    {
        sector=default;position=default;
        if(id==null || !_checkpoints.TryGetValue(id,out var checkpoint))return false;
        sector=checkpoint.SectorId;position=checkpoint.WorldPosition;return true;
    }
    public bool CanAdvanceCheckpoint(string current,string next)=>current!=null && next!=null &&
        _checkpoints.TryGetValue(current,out var before) && _checkpoints.TryGetValue(next,out var after) && after.Order>before.Order;
    public void ValidateDefinitions(IReadOnlyList<PrologueSectorDefinition> definitions)
    {
        if(definitions==null || definitions.Count!=SectorIds.Count || definitions.Any(d=>d==null) ||
            !definitions.Select(d=>d.Id).Order().SequenceEqual(SectorIds.Order()))
            throw new ArgumentException("Profile requires exactly its actual transport containers.");
        Rect2 envelope=definitions[0].WorldBounds;
        foreach(var d in definitions)
        {
            if(!PrologueSectorDefinition.FinitePositive(d.WorldBounds) || !DesignBounds.Encloses(d.WorldBounds))
                throw new ArgumentException("Profile container bounds differ from the authored world.");
            envelope=envelope.Merge(d.WorldBounds);
        }
        if(envelope!=DesignBounds)throw new ArgumentException("Profile containers must cover their design envelope.");
        foreach(var c in Checkpoints)
            if(!definitions.Single(d=>d.Id==c.SectorId).WorldBounds.HasPoint(c.WorldPosition))
                throw new ArgumentException("Profile checkpoint must be in its actual container.");
    }
}
public static class PrologueWorldProfiles
{
    private static readonly PrologueWorldProfile FirstGate=new("knight.first-gate.development.v1",new(0,0,3072,2048),
        [PrologueSectorId.CanyonEntrance],
        [new("knight.first-gate.start",PrologueSectorId.CanyonEntrance,new(192,472),0),
         new("knight.first-gate.a-refuge",PrologueSectorId.CanyonEntrance,new(1760,856),1)]);
    private static readonly PrologueWorldProfile Abyss=new("knight.first-gate.abyss.v1",new(0,0,6144,8192),
        [PrologueSectorId.CanyonEntrance],
        [new("knight.first-gate.start",PrologueSectorId.CanyonEntrance,new(192,472),0),
         new("knight.first-gate.a-refuge",PrologueSectorId.CanyonEntrance,new(1760,856),1),
         new("knight.first-gate.abyss-refuge",PrologueSectorId.CanyonEntrance,new(1888,4472),2),
         new("knight.first-gate.bottom-refuge",PrologueSectorId.CanyonEntrance,new(5376,7448),3)],12,6);
    private static readonly PrologueWorldProfile Forecourt=new("knight.first-gate.forecourt.v1",new(0,0,16384,12288),
        [PrologueSectorId.CanyonEntrance],
        [new("knight.first-gate.start",PrologueSectorId.CanyonEntrance,new(192,472),0),
         new("knight.first-gate.a-refuge",PrologueSectorId.CanyonEntrance,new(1760,856),1),
         new("knight.first-gate.abyss-refuge",PrologueSectorId.CanyonEntrance,new(1888,4472),2),
         new("knight.first-gate.bottom-refuge",PrologueSectorId.CanyonEntrance,new(5376,7448),3),
         new("knight.first-gate.forecourt-refuge",PrologueSectorId.CanyonEntrance,new(8896,7448),4),
         new("knight.first-gate.hall-refuge",PrologueSectorId.CanyonEntrance,new(12096,5624),5),
         new("knight.first-gate.gate-approach-refuge",PrologueSectorId.CanyonEntrance,new(13312,8472),6)],24,10,16);
    public static bool TryResolve(string id,out PrologueWorldProfile profile)
    {profile=id==FirstGate.Id?FirstGate:id==Abyss.Id?Abyss:id==Forecourt.Id?Forecourt:null;return profile!=null;}
}
