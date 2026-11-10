using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Godot;
using Knight.Build.Prologue;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Save;

namespace Knight.Tests.Unit;

public static class ResourceRewardTests
{
    private const string A = "knight.first-gate.branch.a.chest";
    public static void Run()
    {
        TestAssert.Equal(54, ResourceRewardCatalog.All.Count, "resource reward catalog defines54 actual grants");
        TestAssert.Equal(54, ResourceRewardCatalog.StableIds.Distinct(StringComparer.Ordinal).Count(), "stable reward identities unique");
        TestAssert.True(ResourceRewardCatalog.TryGet(A, out var a) && a.Resource == "fiber" && a.Quantity == 3 && a.IsChest,
            "A grants3 fiber independently of scene-editable amounts");
        TestAssert.True(!ResourceRewardCatalog.TryGet(null, out _) && !ResourceRewardCatalog.TryGet("unknown", out _), "unknown grants fail closed");
        foreach ((string kind, int count) in new[] { ("fiber",9), ("mineral",33), ("salvage",44), ("data",12), ("echo-memory",2) })
            TestAssert.Equal(count, ResourceRewardCatalog.Total(ResourceRewardCatalog.StableIds,kind), "literal all54 total " + kind);
        TestAssert.Equal(3, ResourceRewardCatalog.Total(new[] { A,A },"fiber"), "duplicated receipt never double credits");
        Invalid(() => ResourceRewardCatalog.Total(new[] { "forged" },"fiber"), "unknown accounting identity");

        var progress = new PrologueProgress();
        TestAssert.True(progress.TryClaimResourceReward(A), "first claim enters independent reward ledger");
        TestAssert.True(!progress.TryClaimResourceReward(A) && !progress.TryClaimResourceReward("forged"), "repeat/unknown claims do not grant");
        TestAssert.Equal(3,progress.ResourceTotal("fiber"),"actual progress exposes obtained resources");
        TestAssert.Equal(0,progress.CollectedFragmentIds.Count,"material is not a quota fragment");
        var basis=PrologueSaveData.NewLayoutV2();
        var candidate=progress.ToSave(basis);
        TestAssert.True(basis.ClaimedRewardIds.Count==0 && candidate.ClaimedRewardIds.SequenceEqual(new[]{A}), "reward candidate detached from basis");
        var clone=candidate.Normalize();
        candidate.ClaimedRewardIds.Clear();
        TestAssert.True(clone.ClaimedRewardIds.SequenceEqual(new[]{A}) && progress.ClaimedRewardIds.Count==1, "normalize and live ledger are detached");
        var restored=new PrologueProgress(); restored.Restore(clone);
        TestAssert.Equal(3,restored.ResourceTotal("fiber"),"restore preserves amount through registered ID");
        var bad=clone.Normalize(); bad.ClaimedRewardIds.Add("forged");
        Invalid(()=>restored.Restore(bad),"invalid restore refuses before reset");
        TestAssert.Equal(3,restored.ResourceTotal("fiber"),"failed restore keeps previous live ledger");
        bad=clone.Normalize(); bad.ClaimedRewardIds=null;
        Invalid(()=>bad.Normalize(),"explicit null ledger invalid");
        restored.Reset();
        TestAssert.True(restored.ClaimedRewardIds.Count==0 && restored.ResourceTotal("fiber")==0,"new game clears rewards");
        TestAssert.True(QuestFragmentCatalog.Definitions.Count==9 && QuestFragmentCatalog.RequiredCount==6,"original9/min6 unchanged");
        TestAssert.True(FirstGateCampaignCatalog.Candidate.Rewards.All(r=>ResourceRewardCatalog.TryGet(r.StableId,out var d) &&
            d.Route==r.Route && d.Resource==r.Resource && d.Quantity==r.Quantity && d.IsChest==r.IsChest), "builder consumes same runtime grant contract");

        var paths=IsolatedPrologueFixture.CreatePaths("resource-reward-models");
        PrologueSaveStore.SaveAtomic(paths.SavePath,clone);
        var loaded=PrologueSaveStore.LoadOrNew(paths.SavePath,2);
        TestAssert.True(!loaded.WasRecovered && loaded.ClaimedRewardIds.SequenceEqual(new[]{A}),"actual disk roundtrip retains ledger");
        string original=File.ReadAllText(paths.SavePath);
        JsonObject old=JsonNode.Parse(original)!.AsObject(); old.Remove("claimedRewardIds");
        string oldPath=Path.Combine(paths.CaseRoot,"old-v4.json"); File.WriteAllText(oldPath,old.ToJsonString());
        string oldSha=Hash(oldPath);
        var oldLoaded=PrologueSaveStore.LoadOrNew(oldPath,2);
        TestAssert.True(!oldLoaded.WasRecovered && oldLoaded.ClaimedRewardIds.Count==0 && oldLoaded.Version==4,"genuine oldv4 omission is valid empty, not recovery");
        TestAssert.Equal(oldSha,Hash(oldPath),"oldv4 read never rewrites source");
        foreach (JsonNode node in new JsonNode[] { null, JsonValue.Create(5), new JsonArray("forged") })
        {
            JsonObject wire=JsonNode.Parse(original)!.AsObject(); wire["claimedRewardIds"]=node;
            string path=Path.Combine(paths.CaseRoot,"bad-"+Guid.NewGuid().ToString("N")+".json");
            File.WriteAllText(path,wire.ToJsonString()); string sha=Hash(path);
            TestAssert.True(PrologueSaveStore.LoadOrNew(path,2).WasRecovered,"bad ledger rejected rather than granting");
            TestAssert.Equal(sha,Hash(path),"bad slot is preserved");
        }
        foreach(int version in new[]{2,3})
        {
            JsonObject wire=JsonNode.Parse(original)!.AsObject(); wire["version"]=version;
            string path=Path.Combine(paths.CaseRoot,$"injected-schema{version}.json");
            File.WriteAllText(path,wire.ToJsonString()); string sha=Hash(path);
            var legacy=PrologueSaveStore.LoadOrNew(path);
            TestAssert.True(!legacy.WasRecovered && legacy.ClaimedRewardIds.Count==0,"oldschema cannot forge future resource progress");
            TestAssert.Equal(sha,Hash(path),"migration inspection preserves original");
        }
        GD.Print("RESOURCE_REWARD_MODELS_OK definitions=54 fixed_totals=9/33/44/12/2 ids_only=true old_v4=true legacy_injection_cleared=true fragments=9 quota=6 runtime_placement=false human=false");
    }
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Invalid(Action action,string message)
    {
        bool caught=false;try{action();}catch(InvalidDataException){caught=true;}
        TestAssert.True(caught,message);
    }
}
