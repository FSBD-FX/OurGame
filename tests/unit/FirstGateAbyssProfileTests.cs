using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Godot;
using Knight.Prologue.Seamless;
using Knight.Save;

namespace Knight.Tests.Unit;

public static class FirstGateAbyssProfileTests
{
    public static PrologueWorldProfile Run(string storage)
    {
        TestAssert.True(PrologueWorldProfiles.TryResolve("knight.first-gate.abyss.v1",out var profile),
            "ABYSS_PROFILE_RED: expanded saved parent needs its own strict profile, not changed v1 bounds");
        TestAssert.True(PrologueWorldProfiles.TryResolve("knight.first-gate.development.v1",out var old) && old.DesignBounds==new Rect2(0,0,3072,2048),"existing saved v340 parent still resolves original bounds");
        TestAssert.True(profile.DesignBounds==new Rect2(0,0,6144,8192) && profile.Checkpoints.Count==4 && profile.FogFields.Count==5,
            "expanded context owns actual large geometry and four real recovery points");
        foreach(var (main,branches) in new[]{(0,1),(25,1),(12,0),(12,11)})
        {
            bool rejected=false;
            try{_ = new PrologueWorldProfile("invalid-ui-range",profile.DesignBounds,new[]{PrologueSectorId.CanyonEntrance},profile.Checkpoints.ToArray(),main,branches);}
            catch(ArgumentException){rejected=true;}
            TestAssert.True(rejected,"authored scope cannot advertise absent/out-of-plan main or branch range");
        }
        foreach(var (id,at) in new[]{("knight.first-gate.start",new Vector2(192,472)),("knight.first-gate.a-refuge",new Vector2(1760,856)),
            ("knight.first-gate.abyss-refuge",new Vector2(1888,4472)),("knight.first-gate.bottom-refuge",new Vector2(5376,7448))})
            TestAssert.True(profile.TryResolveCheckpoint(id,out var sector,out var point) && sector==PrologueSectorId.CanyonEntrance && point==at,"canonical expanded checkpoint "+id);
        string file=Path.Combine(storage,"abyss-profile-contract.json");
        PrologueSaveStore.SaveAtomic(file,PrologueSaveData.NewForProfile(profile),profile:profile);
        string hash=Hash(file);
        TestAssert.True(!PrologueSaveStore.LoadCandidate(file,profile).Data.WasRecovered,"selected expanded schema4/rule1 reads its actual slot");
        TestAssert.True(PrologueSaveStore.LoadCandidate(file,old).Data.WasRecovered && Hash(file)==hash,"v1 cannot silently claim or rewrite expanded slot");
        string previous=Path.Combine(storage,"v340-profile-contract.json");
        PrologueSaveStore.SaveAtomic(previous,PrologueSaveData.NewForProfile(old),profile:old);
        hash=Hash(previous);
        TestAssert.True(PrologueSaveStore.LoadCandidate(previous,profile).Data.WasRecovered && Hash(previous)==hash,"expanded profile does not migrate borrowed v340 progress");
        GD.Print("ABYSS_PROFILE_OK old_v1_preserved=true separate_slot=true checkpoints=4 fog_fields=5 cross_readonly=true");
        return profile;
    }
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
