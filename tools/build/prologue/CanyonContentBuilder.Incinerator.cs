using System;
using Godot;
using Knight.Abilities;
using Knight.Mechanisms;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

public static partial class CanyonContentBuilder
{
    // Used by the canonical builder and by the detached, additive authoring
    // transaction. Never rebuilds or relocates the existing basin contents.
    public static void AddIncineratorAnnex(PrologueSector root)
    {
        if (root == null || root.SectorId != PrologueSectorId.CanyonBasin || root.IsInsideTree() ||
            root.Bounds != new Rect2(0,0,5120,720) ||
            root.GetNodeOrNull("Gameplay/Mechanisms/IncineratorPool") != null)
            throw new ArgumentException("Detached expanded Basin without an existing furnace required.");

        Solid(root,"IncineratorUpperEntryA",new(5100,3676,160,24), oneWay:true);
        Solid(root,"IncineratorUpperEntryB",new(5240,3756,140,24));
        Solid(root,"IncineratorSafeGallery",new(5360,3836,800,24), GrappleSurfaceMaterial.TempleMetal);
        Solid(root,"IncineratorWestBank",new(4680,3996,760,24), GrappleSurfaceMaterial.TempleMetal);
        Solid(root,"IncineratorContainmentFloor",new(5440,4296,720,8), GrappleSurfaceMaterial.Forbidden);
        Solid(root,"IncineratorEastBank",new(6160,3996,216,24), GrappleSurfaceMaterial.TempleMetal);
        Solid(root,"IncineratorEastWall",new(6376,3600,24,720), GrappleSurfaceMaterial.Forbidden);
        Solid(root,"IncineratorRoof",new(5360,3600,1040,40), GrappleSurfaceMaterial.Forbidden);
        Solid(root,"IncineratorReturn1",new(6240,3916,136,16), oneWay:true);
        Solid(root,"IncineratorReturn2",new(6240,3836,136,16), oneWay:true);
        Solid(root,"IncineratorReturn3",new(6240,3756,136,16), oneWay:true);
        // Old solid steam steps form a low soffit above the west bank. Do not
        // assume that an80px theoretical jump can pass through that ceiling.
        Solid(root,"IncineratorWestReturn1",new(5140,3916,100,16), oneWay:true);
        Solid(root,"IncineratorWestReturn2",new(5140,3836,100,16), oneWay:true);
        Solid(root,"IncineratorWestReturn3",new(5140,3756,100,16), oneWay:true);

        var pool = new CanyonIncinerator
        {
            Name="IncineratorPool", Position=new Vector2(5800,4158)-new Vector2(1280,3600),
            Size=new Vector2(720,276), PulseSeconds=.1f,
            CollisionLayer=Knight.Combat.CombatCollisionLayers.None,
            CollisionMask=Knight.Combat.CombatCollisionLayers.Player,
            Monitoring=true, Monitorable=false
        };
        pool.AddChild(new CollisionShape2D
        {
            Name="Collision", Shape=new RectangleShape2D { Size=new Vector2(720,276) }
        });
        root.GetNode("Gameplay/Mechanisms").AddChild(pool);

        Node markers=EnsureContainer(root.GetNode("Gameplay"),"MicroScenes");
        Marker2D marker=markers.GetNodeOrNull<Marker2D>("IncineratorAnnex");
        if(marker==null)
        {
            marker=new Marker2D {Name="IncineratorAnnex"};
            marker.SetMeta("local_bounds",new Rect2(3840,0,1280,720));
            marker.SetMeta("optional_subarea",true);
            marker.SetMeta("route_node_id",(int)CanyonRouteNodeId.IncineratorAnnex);
            marker.SetMeta("challenge_kind",-1);
            markers.AddChild(marker);
        }
        // A room-centre guide target would float above the lethal pool.
        marker.Position=new Vector2(6000,3800)-new Vector2(1280,3600);
        GroundEdge(root,"steam-to-incinerator",new(4600,3844,80,72),new(4700,3924,120,72));
        // The return contract uses the new west service steps, not an
        // impossible jump from underneath the old solid steam staircase.
        AddIncineratorUpperContracts(root);

        Node2D readable=root.GetNode<Node2D>("VisualRoot/GameplayReadable");
        var sign=new Node2D {Name="IncineratorAdvanceWarning",Position=new Vector2(5320,3950)-new Vector2(1280,3600)};
        sign.AddChild(new Label
        {
            Name="Warning",Position=new(-140,-32),Text="高温销毁池  ·  ↑ 上层廊桥",
            MouseFilter=Control.MouseFilterEnum.Ignore,Modulate=new Color("d6ae70")
        });
        readable.AddChild(sign);
    }

    internal static void AddIncineratorUpperContracts(PrologueSector root)
    {
        GroundEdge(root,"lift-to-incinerator",new(5000,3528,100,72),new(5160,3604,80,72));
        Edge(root,"incinerator-to-lift",new(5120,3604,100,72),new(5000,3528,100,72));
    }
}
