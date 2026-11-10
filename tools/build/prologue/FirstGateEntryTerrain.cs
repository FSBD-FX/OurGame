using System;
using System.Collections.Generic;
using Godot;
using Knight.Abilities;
using Knight.Combat;
using Knight.Presentation;
using Knight.Prologue;
using Knight.Prologue.Collections;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

// Independent authoring leaf; not a layout2 campaign or a final-art asset.
public static class FirstGateEntryTerrain
{
    public static readonly Rect2 Bounds = new(0,0,3072,2048);
    public static readonly Vector2 Spawn = new(192,472);
    private const int Grid=32;
    // Negative space is authored independently of the old five sector rectangles.
    private static readonly Rect2[] Passages =
    [
        new(64,0,896,512), new(800,256,480,256),
        new(896,320,384,928), new(128,928,1152,320),
        new(128,1056,320,640), new(320,1376,2560,320),
        new(1216,576,1024,320), new(1984,768,256,480), new(1152,960,1088,288),
        new(1632,1632,128,224)
    ];

    public static PrologueSector Build()=>BuildCore(Bounds,Passages);

    internal static PrologueSector BuildExpandedBase(Rect2 bounds,IReadOnlyList<Rect2> extension)
    {
        if(bounds.Position!=Vector2.Zero || !bounds.Encloses(Bounds) || bounds.Size.X%Grid!=0 || bounds.Size.Y%Grid!=0 || extension==null)
            throw new ArgumentException("Expanded terrain must contain the unchanged entry on its native grid.");
        var passages=new List<Rect2>(Passages);passages.AddRange(extension);
        foreach(var passage in passages)
            if(!PrologueSectorDefinition.FinitePositive(passage) || !bounds.Encloses(passage))
                throw new ArgumentException("Passage must stay inside the actual expanded bounds.");
        return BuildCore(bounds,passages);
    }

    private static PrologueSector BuildCore(Rect2 bounds,IReadOnlyList<Rect2> passages)
    {
        var root=new PrologueSector { Name="FirstGateEntry01To04", SectorId=PrologueSectorId.CanyonEntrance, Bounds=bounds };
        try
        {
            root.SetMeta("first_gate_authoring_version",339);
            root.SetMeta("campaign_integrated",false);
            var visual=new Node2D {Name="VisualRoot"};root.AddChild(visual);
            foreach(var (name,z) in new[]{("Background",-40),("Midground",-20),("GameplayReadable",-2),("Foreground",20),("Effects",30)})
                visual.AddChild(new Node2D {Name=name,ZIndex=z});
            // Reused palette-only prototype backdrop; not a generated/final environment illustration.
            visual.GetNode("Background").AddChild(new Polygon2D {Name="CaveDarkness",
                Polygon=[Vector2.Zero,new(bounds.Size.X,0),bounds.Size,new(0,bounds.Size.Y)],Color=new Color("101c22")});
            var gameplay=new Node2D {Name="Gameplay"};root.AddChild(gameplay);
            foreach(string name in new[]{"Collision","GrappleSurfaces","TraversalContracts","RouteMarkers","FallRecovery","OptionalPaths","ResourceCaches","Mechanisms","Enemies"})
                gameplay.AddChild(new Node2D {Name=name});
            root.AddChild(new Marker2D {Name="Spawn",Position=Spawn});
            var refuge=new CheckpointTrigger {Name="ARefuge",Position=new(1760,856),EntranceId="knight.first-gate.a-refuge",
                CollisionLayer=CombatCollisionLayers.None,CollisionMask=CombatCollisionLayers.Player,Monitoring=true};
            refuge.AddChild(new CollisionShape2D {Name="Collision",Shape=new RectangleShape2D {Size=new(96,112)}});
            refuge.AddChild(new PrologueMechanismVisual {Name="Visual",Kind=MechanismVisualKind.GraveLamp,
                InitialState=MechanismVisualState.Idle,Position=new(-52,-6)});
            gameplay.AddChild(refuge);
            int index=0;
            foreach(Rect2 rock in MergeRockMasses(bounds,passages))
                CanyonContentBuilder.AuthoredLocalSolid(root,$"RockMass{index++:000}",rock,
                    rock.Size.X>=rock.Size.Y?CanyonRockSkinRole.Ground:CanyonRockSkinRole.Wall);
            CanyonContentBuilder.AuthoredLocalSolid(root,"RiftLeftShelf",new(896,736,192,32),CanyonRockSkinRole.Ground);
            CanyonContentBuilder.AuthoredLocalSolid(root,"RiftRightShelf",new(1152,864,128,32),CanyonRockSkinRole.Ground);
            // The below-beam refuge provides a nonlethal recovery route, not a second jump grant.
            CanyonContentBuilder.AuthoredLocalSolid(root,"CatchReturnStep",new(1728,1776,32,80),CanyonRockSkinRole.Ground);
            Mark(root,"Main01",new(192,472)); Mark(root,"Main02",new(1168,816));
            Mark(root,"Main03",new(608,1208)); Mark(root,"Main04",new(2656,1656));
            Mark(root,"BranchAEntry",new(1376,848)); Mark(root,"BranchARejoin",new(1328,1208));
            AddCache(root,"Main01","knight.first-gate.main.01.resource",new(576,496));
            AddCache(root,"Main02","knight.first-gate.main.02.resource",new(1184,848));
            AddCache(root,"Main03","knight.first-gate.main.03.resource",new(704,1232));
            AddCache(root,"Main04","knight.first-gate.main.04.resource",new(2464,1680));
            AddCache(root,"AChest","knight.first-gate.branch.a.chest",new(1760,880));
            AddCache(root,"APile01","knight.first-gate.branch.a.resource.01",new(1504,880));
            AddCache(root,"APile02","knight.first-gate.branch.a.resource.02",new(1888,880));
            return root;
        }
        catch {root.Free();throw;}
    }

    internal static void Mark(PrologueSector root,string name,Vector2 center)=>
        root.GetNode("Gameplay/RouteMarkers").AddChild(new Marker2D {Name=name,Position=center});

    internal static void AddCache(PrologueSector root,string name,string id,Vector2 center)
    {
        if(!ResourceRewardCatalog.TryGet(id,out var reward))throw new ArgumentException("Unregistered entry resource.");
        var cache=new PrologueResourceCache {Name=name,Position=center,RewardId=id};
        var available=new Node2D {Name="AvailableVisual"};cache.AddChild(available);
        // Existing runtime's replaceable hooks, explicitly only prototype silhouettes.
        float w=reward.IsChest?36:20;
        available.AddChild(new Polygon2D {Name="Prototype",Polygon=[new(-w/2,-16),new(w/2,-16),new(w/2,0),new(-w/2,0)],
            Color=reward.IsChest?new Color("b19b62"):new Color("718f81")});
        cache.AddChild(new Node2D {Name="ClaimedVisual",Visible=false});
        cache.AddChild(new Label {Name="Status",Position=new(-64,-44),Size=new(128,24),MouseFilter=Control.MouseFilterEnum.Ignore});
        root.GetNode("Gameplay/ResourceCaches").AddChild(cache);
    }

    private static IReadOnlyList<Rect2> MergeRockMasses(Rect2 bounds,IReadOnlyList<Rect2> passages)
    {
        int width=(int)bounds.Size.X/Grid,height=(int)bounds.Size.Y/Grid;
        var result=new List<Rect2>();
        var previous=new Dictionary<(int Start,int Length),int>();
        for(int y=0;y<height;y++)
        {
            var current=new Dictionary<(int,int),int>();
            int x=0;
            while(x<width)
            {
                if(IsPassage(x,y,passages)){x++;continue;}
                int start=x;while(x<width && !IsPassage(x,y,passages))x++;
                var key=(start,x-start);
                if(previous.TryGetValue(key,out int at))
                {
                    Rect2 r=result[at];r.Size+=new Vector2(0,Grid);result[at]=r;current.Add(key,at);
                }
                else {current.Add(key,result.Count);result.Add(new(start*Grid,y*Grid,(x-start)*Grid,Grid));}
            }
            previous=current;
        }
        return result.AsReadOnly();
    }
    private static bool IsPassage(int x,int y,IReadOnlyList<Rect2> passages)
    {
        Vector2 center=new((x+.5f)*Grid,(y+.5f)*Grid);
        foreach(Rect2 passage in passages)if(passage.HasPoint(center))return true;
        return false;
    }
}
