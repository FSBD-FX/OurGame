using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Seamless;
using Knight.Save;
using Knight.Presentation.Fog;

namespace Knight.Tests.Unit;

public static class PrologueWorldProfileTests
{
    private const string Key="knight.first-gate.development.v1";
    private const string Start="knight.first-gate.start",Refuge="knight.first-gate.a-refuge";
    public static void Run(string storage)
    {
        // Catches silently selecting legacy identity/coordinates for the new physical map.
        TestAssert.True(PrologueWorldProfiles.TryResolve(Key,out var profile),"PROFILE_IDENTITY_RED: actual new world profile must resolve");
        TestAssert.True(!PrologueWorldProfiles.TryResolve(null,out _) && !PrologueWorldProfiles.TryResolve("foreign",out _),"unknown profile cannot fall back to legacy");
        TestAssert.True(profile.Id==Key && profile.SaveRuleVersion==1 && profile.DesignBounds==new Rect2(0,0,3072,2048),"new map identity and actual bounds");
        TestAssert.Equal(3,profile.FogFields.Count,"PROFILE_FOG_RED: new bounds plus two real refuge fields, not old map fields");
        TestAssert.True(PrologueFogModel.ResolveProfile(new(192,472),profile.FogFields)==PrologueFogProfile.RestArea &&
            PrologueFogModel.ResolveProfile(new(1760,856),profile.FogFields)==PrologueFogProfile.RestArea &&
            PrologueFogModel.ResolveProfile(new(2400,1600),profile.FogFields)==PrologueFogProfile.Exploration,
            "CPU field resolution uses actual new start/refuge/exploration, no old map region leakage");
        TestAssert.True(profile.SectorIds.SequenceEqual(new[]{PrologueSectorId.CanyonEntrance}),"one real transport container, no four fake sectors");
        TestAssert.True(profile.Start.StableId==Start && profile.Start.WorldPosition==new Vector2(192,472),"new start not old canyon coordinates");
        TestAssert.True(profile.TryResolveCheckpoint(Refuge,out var sector,out var position) && sector==PrologueSectorId.CanyonEntrance && position==new Vector2(1760,856),"A refuge canonicalpoint lives in same container");
        TestAssert.True(profile.CanAdvanceCheckpoint(Start,Refuge) && !profile.CanAdvanceCheckpoint(Refuge,Start) &&
            !profile.CanAdvanceCheckpoint(Start,Start) && !profile.CanAdvanceCheckpoint("unknown",Refuge),"forward identity order, not sector whitelist");
        var data=PrologueSaveData.NewForProfile(profile);
        TestAssert.True(data.LayoutVersion==3 && data.WorldProfileId==Key && data.WorldProfileSaveRuleVersion==1 &&
            data.CheckpointId==Start && data.CheckpointWorldPosition==new Vector2(192,472) && !data.HasActivatedCheckpoint,"profile new game starts before first refuge");
        Invalid(()=>data.Normalize(),"default save reader must reject new profile");
        var draft=data.Normalize(profile);draft.CheckpointId=Refuge;draft.CheckpointWorldX=123;draft.CheckpointWorldY=456;
        var canonical=draft.Normalize(profile);
        TestAssert.True(canonical.HasActivatedCheckpoint && canonical.CheckpointSector=="CanyonEntrance" &&
            canonical.CheckpointWorldPosition==new Vector2(1760,856),"stable ID, not editable saved coordinates, owns respawn");
        foreach(Action<PrologueSaveData> corrupt in new Action<PrologueSaveData>[] {
            x=>x.WorldProfileId="foreign",x=>x.WorldProfileSaveRuleVersion=2,x=>x.LayoutVersion=2,
            x=>x.CheckpointId="canyon.v2.start",x=>x.CheckpointWorldX=float.NaN,
            x=>x.Completed=true,x=>x.BossDefeated=true,x=>x.LegacyCollectionQuotaExempt=true,
            x=>x.BestHumanCompletionSeconds=1,x=>x.BestHumanDeaths=0,
            x=>x.OpenShortcuts.Add("canyon.v2.temple-bell-gate"),x=>x.Discoveries.Add("hidden_alcove")})
        {var bad=data.Normalize(profile);corrupt(bad);Invalid(()=>bad.Normalize(profile),"foreign or formal progress cannot enter development profile");}
        var legacy=PrologueSaveData.NewLayoutV2();
        Invalid(()=>legacy.Normalize(profile),"profile reader rejects legacy identity");
        TestAssert.True(legacy.Normalize().LayoutVersion==2,"legacy normalizer unchanged for valid legacy data");
        var progress=new PrologueProgress(profile.Start.WorldPosition);progress.ConfigureProfile(profile);
        TestAssert.True(progress.TryClaimResourceReward("knight.first-gate.branch.a.chest"),"obtained material uses existing registry");
        TestAssert.True(progress.ActivateCheckpoint(Refuge),"A refuge allowed despite Entrance enum");
        TestAssert.True(!progress.ActivateCheckpoint(Start) && !progress.ActivateCheckpoint(PrologueSectorId.CanyonEntrance,new Vector2(700,700)),"backward and arbitrary coordinates not checkpoints");
        var snapshot=progress.ToSave(data);
        TestAssert.True(snapshot.CheckpointId==Refuge && snapshot.ClaimedRewardIds.Count==1 && data.ClaimedRewardIds.Count==0,"detached snapshot includes profile material ledger");
        progress.Reset();TestAssert.True(progress.RestoreCheckpoint().CheckpointId==Start && progress.ClaimedRewardIds.Count==0,"new game resets to profile start not refuge");
        progress.Restore(snapshot);TestAssert.True(progress.ResourceTotal("fiber")==3 && progress.RestoreCheckpoint().CheckpointId==Refuge,"profile restore preserves material and canonical checkpoint");
        var badRestore=snapshot.Normalize(profile);badRestore.WorldProfileId="foreign";
        Invalid(()=>progress.Restore(badRestore),"foreign restoration rejected before resetting live ledger");
        TestAssert.Equal(3,progress.ResourceTotal("fiber"),"failed restore keeps previous material");
        string path=Path.Combine(storage,"profile-save-context.json");
        var missing=PrologueSaveStore.LoadCandidate(path,profile);
        TestAssert.True(missing.Status==PrologueSaveLoadStatus.Current && missing.Data.CheckpointId==Start && !File.Exists(path),"missing selected profile slot returns new without writing");
        PrologueSaveStore.SaveAtomic(path,snapshot,profile:profile);string sha=Hash(path);
        var loaded=PrologueSaveStore.LoadCandidate(path,profile);
        TestAssert.True(loaded.Status==PrologueSaveLoadStatus.Current && loaded.Data.CheckpointId==Refuge && loaded.Data.ClaimedRewardIds.Count==1 && Hash(path)==sha,"same profile file roundtrip readonly");
        TestAssert.True(PrologueSaveStore.LoadOrNew(path,2).WasRecovered,"legacy loader never accepts candidate slot");
        Invalid(()=>PrologueSaveStore.SaveAtomic(path,snapshot),"missing save context refuses before write");
        TestAssert.Equal(sha,Hash(path),"failed default write keeps exact bytes");
        string old=Path.Combine(storage,"profile-selected-foreign.json");PrologueSaveStore.SaveAtomic(old,legacy);string oldSha=Hash(old);
        TestAssert.True(PrologueSaveStore.LoadCandidate(old,profile).Status==PrologueSaveLoadStatus.Recovered && Hash(old)==oldSha,"foreign file preserved, no legacy migration fallback");
        var malformed=JsonNode.Parse(File.ReadAllText(path))!.AsObject();malformed.Remove("worldProfileSaveRuleVersion");
        string malformedPath=Path.Combine(storage,"profile-malformed.json");File.WriteAllText(malformedPath,malformed.ToJsonString());string malformedSha=Hash(malformedPath);
        TestAssert.True(PrologueSaveStore.LoadCandidate(malformedPath,profile).Status==PrologueSaveLoadStatus.Recovered && Hash(malformedPath)==malformedSha,"omitted candidate rule cannot acquire authority from defaults");
        GD.Print("WORLD_PROFILE_CONTEXT_OK literal_identity=true checkpoint_order=true foreign_readonly=true material_ledger=true old_default=true campaign_runtime=false");
    }
    private static void Invalid(Action action,string message)
    {bool invalid=false;try{action();}catch(InvalidDataException){invalid=true;}TestAssert.True(invalid,message);}
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
