using Godot;
using System.Collections.Generic;
using Knight.Combat;
using Knight.Mechanisms;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public static class FirstGateAbyssTerrain
{
    public static readonly Rect2 Bounds=new(0,0,6144,8192);
    private static readonly Rect2[] Extension=
    [
        new(2816,1376,1472,320), new(3968,1536,320,704), //05 upper/lower gallery
        new(1600,1920,2688,320), new(1536,2080,448,2432), //06 waist /07 shaft
        new(1792,4192,2688,320), new(4160,4352,320,736), //08 rightward/down
        new(1024,4768,3456,320), new(1024,4928,320,800), //09 leftward /10 entry
        new(1024,5408,2944,320), new(3648,5568,320,1088), //10 across /second well
        new(1024,6336,2944,320), new(1024,6496,320,992), //11 forest /bottom turn
        new(1024,7168,4608,320), //12 towards mountain, still sealed before13
        new(4288,1376,1216,320),new(5184,1536,320,704),new(4096,1920,1408,320), //B
        new(512,2080,1152,320),new(512,2240,320,704),new(512,2624,1344,320), //C
        new(512,3264,1216,320),new(512,3424,320,1664),new(512,4768,896,320), //D
        new(256,4768,1088,320),new(256,4928,320,800),new(256,5408,1088,320), //E
        new(256,6336,1088,320),new(256,6496,320,992),new(256,7168,1088,320) //F
    ];
    public static PrologueSector Build()=>BuildExpanded(Bounds,System.Array.Empty<Rect2>());

    internal static PrologueSector BuildExpanded(Rect2 bounds,IReadOnlyList<Rect2> continuation)
    {
        var passages=new List<Rect2>(Extension);passages.AddRange(continuation);
        var root=FirstGateEntryTerrain.BuildExpandedBase(bounds,passages);
        try
        {
            root.Name="FirstGateAbyss01To12";root.SetMeta("first_gate_authoring_version",341);
            void Solid(string name,Rect2 rect)=>CanyonContentBuilder.AuthoredLocalSolid(root,name,rect,CanyonRockSkinRole.Ground);
            Solid("Gallery05Catch",new(3968,1984,160,32));
            Solid("Waist06ChoiceBridge",new(1664,2240,320,32));
            Solid("Shaft07SideRest",new(1664,3584,320,32));
            Solid("Corridor09ChoiceBridge",new(1024,5088,192,32));
            Solid("Well10EntryCatch",new(1184,5408,160,32));
            Solid("Well10FarCatch",new(3648,6112,160,32));
            Solid("Forest11ChoiceBridge",new(1152,6656,192,32));
            Solid("Forest11LowCatch",new(1024,7072,160,32));
            Solid("BranchECatch",new(256,5408,160,32));
            Solid("BranchFCatch",new(256,7072,160,32));
            for(int i=0;i<3;i++)Solid($"Forest11StoneSpine{i+1}",new(2016+i*512,6560,64,96));
            AddWind(root,"Main07Updraft",new(1536,2240,448,2240));
            AddWind(root,"BranchDUpdraft",new(512,3584,320,1472));
            AddCheckpoint(root,"AbyssRefuge","knight.first-gate.abyss-refuge",new(1888,4472));
            AddCheckpoint(root,"BottomRefuge","knight.first-gate.bottom-refuge",new(5376,7448));
            foreach(var (n,at) in new (int,Vector2)[]{(5,new(4048,1968)),(6,new(2688,2224)),(7,new(1760,3568)),(8,new(4352,5072)),
                (9,new(1600,5072)),(10,new(1600,5712)),(11,new(1600,6640)),(12,new(5184,7472))})
            {
                FirstGateEntryTerrain.Mark(root,$"Main{n:00}",at-new Vector2(0,24));
                FirstGateEntryTerrain.AddCache(root,$"Main{n:00}",$"knight.first-gate.main.{n:00}.resource",at);
            }
            AddBranch(root,"B",new(4992,1680),new(4608,1680),new(4992,2224));
            AddBranch(root,"C",new(704,2928),new(1024,2384),new(1280,2928));
            AddBranch(root,"D",new(704,5072),new(1024,3568),new(928,5072));
            AddBranch(root,"E",new(416,5712),new(768,5072),new(928,5712));
            AddBranch(root,"F",new(416,7472),new(768,6640),new(928,7472));
            return root;
        }
        catch{root.Free();throw;}
    }
    internal static void AddBranch(PrologueSector root,string letter,Vector2 chest,Vector2 first,Vector2 last)
    {
        string prefix=$"knight.first-gate.branch.{letter.ToLowerInvariant()}";
        FirstGateEntryTerrain.AddCache(root,letter+"Chest",prefix+".chest",chest);
        FirstGateEntryTerrain.AddCache(root,letter+"Pile01",prefix+".resource.01",first);
        FirstGateEntryTerrain.AddCache(root,letter+"Pile02",prefix+".resource.02",last);
    }
    internal static void AddCheckpoint(PrologueSector root,string name,string id,Vector2 at)
    {
        var checkpoint=new CheckpointTrigger {Name=name,EntranceId=id,Position=at,CollisionLayer=CombatCollisionLayers.None,
            CollisionMask=CombatCollisionLayers.Player,Monitoring=true};
        checkpoint.AddChild(new CollisionShape2D {Name="Collision",Shape=new RectangleShape2D {Size=new(96,112)}});
        checkpoint.AddChild(new PrologueMechanismVisual {Name="Visual",Kind=MechanismVisualKind.GraveLamp,Position=new(-52,-6)});
        root.GetNode("Gameplay").AddChild(checkpoint);
    }
    private static void AddWind(PrologueSector root,string name,Rect2 rect)
    {
        var wind=new CanyonUpdraft {Name=name,Position=rect.Position,Bounds=new(Vector2.Zero,rect.Size),DescentSpeed=500,BrakingAcceleration=3000,ZIndex=2};
        var cue=new Label {Name="EntryCue",Position=new(24,-64),Text="↑ 上升气流 · 下落减缓",MouseFilter=Control.MouseFilterEnum.Ignore};
        cue.AddThemeFontSizeOverride("font_size",16);wind.AddChild(cue);
        root.GetNode("Gameplay/Mechanisms").AddChild(wind);
    }
}
