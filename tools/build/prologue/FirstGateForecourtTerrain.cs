using Godot;
using Knight.Presentation;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

// Separate physical greybox candidate. No claim of deployed combat, final art
// or completed first-level gate/Boss logic is made by these reserved spaces.
public static class FirstGateForecourtTerrain
{
    public static readonly Rect2 Bounds=new(0,0,16384,12288);
    private static readonly Rect2[] Continuation=
    [
        new(5504,7168,1376,320), //13 actually cuts the sealed12 wall
        new(6624,7296,1984,192), //14 narrow mountain tunnel
        new(8384,6528,1280,960), //15 buried tall forecourt
        new(9344,5504,960,1984), //16 column climb
        new(10048,5280,1536,384),new(11328,5280,512,384), //17/18
        new(11584,4800,1664,864),new(11904,4032,512,1632), //19 hall and west rise
        new(12160,4032,1856,384), //20 high gallery
        new(13760,4192,512,3392), //21 separate descending machine shaft
        new(11648,7264,2624,320), //22 lower westward return
        new(11648,7424,512,1088),new(11648,8192,2944,320), //23 down then east
        new(14272,7936,1536,576), //24 reserved Boss forecourt, gate ends at15808
        new(6976,6816,1664,256),new(6976,6976,256,512),new(8384,6976,256,512), //G14→15
        new(8640,5120,256,1664),new(8640,5120,2240,256),new(10624,5216,256,448), //H15→17 above16
        new(13952,4032,832,256),new(14528,4192,256,3392),new(14080,7328,704,256), //I20→22 outside21
        new(12160,8352,256,1088),new(12160,9184,2048,256),new(13952,8352,256,1088) //J returns before Boss, never beyond gate
    ];
    public static PrologueSector Build()
    {
        var root=FirstGateAbyssTerrain.BuildExpanded(Bounds,Continuation);
        try
        {
            root.Name="FirstGateForecourt01To24";root.SetMeta("first_gate_authoring_version",345);
            void Solid(string name,Rect2 rect)=>CanyonContentBuilder.AuthoredLocalSolid(root,name,rect,CanyonRockSkinRole.Ground);
            Solid("Column16LowerCatch",new(9344,7040,384,32));
            Solid("Column16EastCatch",new(9920,6592,384,32));
            Solid("Column16WestCatch",new(9344,6144,384,32));
            Solid("Column16UpperLanding",new(9920,5664,384,32));
            // Keep a central tooth, but leave the lower eastbound and upper
            // westbound side-grab corridors clear for the real72px actor.
            // The old576px spine intercepted the actual second K at6744.64.
            Solid("Column16DividingSpine",new(9760,6336,64,192));
            Solid("Hall19RiseLower",new(11904,5216,192,32));
            Solid("Hall19RiseUpper",new(12224,4768,192,32));
            for(int i=0;i<5;i++)Solid($"Shaft21Catch{i+1}",new(i%2==0?13760:14080,4896+i*576,192,32));
            Solid("Approach23Catch",new(11648,8032,192,32));
            for(int i=0;i<3;i++)Solid($"BranchHCatch{i+1}",new(8640,5952+i*544,128,32));
            for(int i=0;i<5;i++)Solid($"BranchICatch{i+1}",new(i%2==0?14528:14656,4768+i*576,128,32));
            Solid("BranchJWestCatch",new(12160,8928,128,32));
            Solid("BranchJEastCatch",new(14080,8928,128,32));
            foreach(var (n,at) in new (int,Vector2)[]{(13,new(6048,7472)),(14,new(7424,7472)),(15,new(8896,7472)),
                (16,new(9920,5648)),(17,new(10880,5648)),(18,new(11552,5648)),(19,new(12544,5648)),
                (20,new(13248,4400)),(21,new(14016,7568)),(22,new(12288,7568)),(23,new(13312,8496)),(24,new(15552,8496))})
            {
                FirstGateEntryTerrain.Mark(root,$"Main{n:00}",at-new Vector2(0,24));
                FirstGateEntryTerrain.AddCache(root,$"Main{n:00}",$"knight.first-gate.main.{n:00}.resource",at);
            }
            FirstGateAbyssTerrain.AddBranch(root,"G",new(7488,7056),new(7360,7056),new(7584,7056));
            FirstGateAbyssTerrain.AddBranch(root,"H",new(9216,5360),new(9024,5360),new(9344,5360));
            FirstGateAbyssTerrain.AddBranch(root,"I",new(14656,7568),new(14368,7568),new(14528,7568));
            FirstGateAbyssTerrain.AddBranch(root,"J",new(13184,9424),new(12672,9424),new(13696,9424));
            FirstGateAbyssTerrain.AddCheckpoint(root,"ForecourtRefuge","knight.first-gate.forecourt-refuge",new(8896,7448));
            FirstGateAbyssTerrain.AddCheckpoint(root,"ReceivingHallRefuge","knight.first-gate.hall-refuge",new(12096,5624));
            FirstGateAbyssTerrain.AddCheckpoint(root,"GateApproachRefuge","knight.first-gate.gate-approach-refuge",new(13312,8472));
            FirstGateEntryTerrain.Mark(root,"BossForecourtReservation",new(15072,8472));
            FirstGateEntryTerrain.Mark(root,"TrueGateReservation",new(15744,8472));
            return root;
        }
        catch{root.Free();throw;}
    }
}
