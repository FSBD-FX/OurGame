using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Knight.Prologue.Collections;

public sealed record ResourceRewardDefinition(string StableId, string Route, string Resource, int Quantity, bool IsChest);

// Fixed v1 grants: existing IDs/amounts must not be edited after publication; add new IDs for future grants.
// This is obtained-material accounting, not a spending/crafting or equipment system.
public static class ResourceRewardCatalog
{
    public static IReadOnlyList<ResourceRewardDefinition> All { get; } = AuthorV1();
    public static IReadOnlyList<string> StableIds { get; } = Array.AsReadOnly(All.Select(x=>x.StableId).ToArray());
    private static readonly IReadOnlyDictionary<string,ResourceRewardDefinition> ById =
        All.ToDictionary(x=>x.StableId,StringComparer.Ordinal);
    public static bool TryGet(string id, out ResourceRewardDefinition reward)
    {
        reward=null;
        return id!=null && ById.TryGetValue(id,out reward);
    }
    public static int Total(IEnumerable<string> claimedIds, string resource)
    {
        if(claimedIds==null) throw new InvalidDataException("Resource reward ledger is null.");
        int total=0;
        foreach(string id in claimedIds.Distinct(StringComparer.Ordinal))
        {
            if(!TryGet(id,out var reward)) throw new InvalidDataException("Unknown resource reward ID.");
            if(reward.Resource==resource) total=checked(total+reward.Quantity);
        }
        return total;
    }
    private static IReadOnlyList<ResourceRewardDefinition> AuthorV1()
    {
        var rewards=new List<ResourceRewardDefinition>();
        string[] kinds={"fiber","mineral","salvage","data","echo-memory","echo-memory","mineral","salvage","data","salvage"};
        int[] amounts={3,4,6,3,1,1,5,8,5,10};
        for(int i=0;i<10;i++)
        {
            string route=((char)('A'+i)).ToString();
            string prefix=$"knight.first-gate.branch.{route.ToLowerInvariant()}";
            rewards.Add(new(prefix+".chest",route,kinds[i],amounts[i],true));
            rewards.Add(new(prefix+".resource.01",route,i<6?"mineral":"salvage",2,false));
            rewards.Add(new(prefix+".resource.02",route,i<6?"fiber":"data",1,false));
        }
        for(int n=1;n<=24;n++) rewards.Add(new($"knight.first-gate.main.{n:00}.resource",$"M{n:00}",n<=12?"mineral":"salvage",1,false));
        return rewards.AsReadOnly();
    }
}
