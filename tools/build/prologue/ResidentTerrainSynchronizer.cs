using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Knight.Abilities;
using Knight.Presentation;
using Knight.Presentation.Minimap;
using Knight.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

// Fixed authoring operation; never runs during gameplay or mutates cached scenes.
public static class ResidentTerrainSynchronizer
{
    public static MinimapTerrainCatalog ApplyCatalog(PrologueLevelController target,IReadOnlyList<PrologueSectorDefinition> geometry)
    {
        if(target==null||target.IsInsideTree()||target.GetParent()!=null||target.IsQueuedForDeletion()||geometry==null||geometry.Count!=5||
            geometry.Any(x=>x==null)||geometry.Select(x=>x.Id).Distinct().Count()!=5||
            target.GetNodeOrNull<SeamlessPrologueWorld>("WorldRoot/Sectors") is not SeamlessPrologueWorld world||world.Definitions.Count!=5||
            target.GetNodeOrNull<PrologueMinimapController>("MinimapController") is not PrologueMinimapController minimap||minimap.Model!=null)
            throw new ArgumentException("Detached normal five-sector parent and geometry required.");
        foreach(var definition in world.Definitions)
        {
            var source=geometry.SingleOrDefault(x=>x.Id==definition.Id);
            if(source==null||definition.Scene==null||definition.Scene.ResourcePath!=PrologueCanyonBuildParts.ScenePath(definition.Id)||
                source.Validate().Count!=0||definition.WorldOffset!=source.WorldOffset||definition.Bounds!=source.Bounds||
                definition.PreviousValue!=source.PreviousValue||definition.NextValue!=source.NextValue||
                !definition.NeighborValues.SequenceEqual(source.NeighborValues)||!definition.KeyInteractionBounds.SequenceEqual(source.KeyInteractionBounds))
                throw new ArgumentException("Geometry cannot change normal sector identities, links or interaction bounds.");
        }
        var catalog=MinimapTerrainCatalogBuilder.Build(geometry);
        if(catalog.SolidCount!=143){catalog.Dispose();throw new ArgumentException("Matching resident terrain must contain all143 physical solids.");}
        minimap.Catalog=catalog;
        return catalog; // Caller owns only this new resource; old catalog remains borrowed.
    }

    public static Shape2D[] Apply(PrologueSector target, PrologueSector donor)
    {
        if(target==null||donor==null||target==donor||target.SectorId!=donor.SectorId||target.Bounds!=donor.Bounds||
            target.GetParent()!=null||donor.GetParent()!=null||target.IsInsideTree()||donor.IsInsideTree()||target.IsQueuedForDeletion()||donor.IsQueuedForDeletion())
            throw new ArgumentException("Two distinct detached matching authoring sectors required.");
        string[] names=target.SectorId switch
        {
            PrologueSectorId.CanyonDescent=>new[]{"ArchiveResidentLowerFloor","ArchiveResidentUpperFloor","ArchiveResidentUpperBackWall","FrozenOuterUpperWall","FrozenSentryRecessWall","FrozenOuterLowerWall","FrozenUpperResidentShelf","FrozenLowerResidentShelf"},
            PrologueSectorId.CanyonBasin=>new[]{"GearResidentLeftShelf","GearWestClimbWall","GearWestLaunchShelf","GearResidentRightShelf","SteamResidentBay"},
            PrologueSectorId.CanyonAscent=>new[]{"PendulumResidentFloor","PendulumResidentRoof","PendulumResidentBackWall"},
            _=>throw new ArgumentException("Only the three approved resident terrain sectors are admitted.")
        };
        var collision=target.GetNodeOrNull("Gameplay/Collision");var readable=target.GetNodeOrNull("VisualRoot/GameplayReadable");
        if(collision==null||readable==null)throw new ArgumentException("Missing target terrain containers.");
        var operations=new List<(StaticBody2D Body,CanyonPlatformVisual Skin)>();
        int existing=0;
        // Preflight the complete delta before allocating or attaching any node.
        foreach(string name in names)
        {
            if(donor.GetNodeOrNull("Gameplay/Collision/"+name) is not StaticBody2D body||donor.GetNodeOrNull("VisualRoot/GameplayReadable/"+name) is not CanyonPlatformVisual skin)
                throw new ArgumentException("Missing or foreign donor terrain: "+name);
            Validate(body,skin,donor);
            bool hasBody=collision.HasNode(name),hasSkin=readable.HasNode(name);
            if(hasBody!=hasSkin)throw new ArgumentException("Partial target terrain pair: "+name);
            if(hasBody)
            {
                if(collision.GetNode(name) is not StaticBody2D currentBody||readable.GetNode(name) is not CanyonPlatformVisual currentSkin)
                    throw new ArgumentException("Foreign existing terrain: "+name);
                Validate(currentBody,currentSkin,target);
                if(currentBody.Position!=body.Position||((RectangleShape2D)currentBody.GetNode<CollisionShape2D>("Collision").Shape).Size!=((RectangleShape2D)body.GetNode<CollisionShape2D>("Collision").Shape).Size||
                    currentSkin.SurfaceMaterial!=skin.SurfaceMaterial||currentSkin.CatchShelf!=skin.CatchShelf||currentSkin.UseCanyonDepthStaging!=skin.UseCanyonDepthStaging||
                    !currentSkin.GetNode<Polygon2D>("ReadableEdge").Polygon.SequenceEqual(skin.GetNode<Polygon2D>("ReadableEdge").Polygon))
                    throw new ArgumentException("Existing terrain conflicts with generator: "+name);
                existing++;
            }
            operations.Add((body,skin));
        }
        if(existing!=0&&existing!=names.Length)throw new ArgumentException("Partial terrain publication is not a valid input.");
        StaticBody2D abyss=null;CanyonPlatformVisual abyssSkin=null;RectangleShape2D abyssShape=null;
        StaticBody2D sourceAbyss=null;CanyonPlatformVisual sourceAbyssSkin=null;
        if(target.SectorId==PrologueSectorId.CanyonDescent)
        {
            abyss=target.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/AbyssExit2");abyssSkin=target.GetNodeOrNull<CanyonPlatformVisual>("VisualRoot/GameplayReadable/AbyssExit2");
            sourceAbyss=donor.GetNodeOrNull<StaticBody2D>("Gameplay/Collision/AbyssExit2");sourceAbyssSkin=donor.GetNodeOrNull<CanyonPlatformVisual>("VisualRoot/GameplayReadable/AbyssExit2");
            if(abyss==null||abyssSkin==null||sourceAbyss==null||sourceAbyssSkin==null)throw new ArgumentException("Missing approved AbyssExit2 pair.");
            Validate(abyss,abyssSkin,target);Validate(sourceAbyss,sourceAbyssSkin,donor);
            abyssShape=(RectangleShape2D)abyss.GetNode<CollisionShape2D>("Collision").Shape;
            if(sourceAbyss.Position!=new Vector2(748,2240)||sourceAbyssSkin.SkinSize!=new Vector2(216,40)||
                !((abyss.Position==new Vector2(740,2240)&&abyssShape.Size==new Vector2(200,40))||(abyss.Position==new Vector2(748,2240)&&abyssShape.Size==new Vector2(216,40))))
                throw new ArgumentException("Unexpected old or generated AbyssExit2 geometry.");
        }
        var owned=new List<Shape2D>();
        if(existing==0)
        {
            foreach(var operation in operations)
            {
                var body=(StaticBody2D)operation.Body.Duplicate((int)Node.DuplicateFlags.Scripts);
                var skin=(CanyonPlatformVisual)operation.Skin.Duplicate((int)Node.DuplicateFlags.Scripts);
                var shape=new RectangleShape2D {Size=((RectangleShape2D)operation.Body.GetNode<CollisionShape2D>("Collision").Shape).Size};owned.Add(shape);
                body.GetNode<CollisionShape2D>("Collision").Shape=shape;
                collision.AddChild(body);readable.AddChild(skin);
                foreach(var node in Walk(body).Concat(Walk(skin)))node.Owner=target;
                if(body.Name!=operation.Body.Name||skin.Name!=operation.Skin.Name)throw new InvalidOperationException("Unexpected auto-renaming during terrain commit.");
            }
        }
        if(abyss!=null&&abyssShape.Size!=new Vector2(216,40))
        {
            var shape=(RectangleShape2D)abyssShape.Duplicate();shape.ResourceSceneUniqueId="";shape.Size=new(216,40);owned.Add(shape);
            abyss.GetNode<CollisionShape2D>("Collision").Shape=shape;abyss.Position=sourceAbyss.Position;abyssSkin.Position=sourceAbyssSkin.Position;
            abyssSkin.SkinSize=sourceAbyssSkin.SkinSize;abyssSkin.GetNode<Polygon2D>("ReadableEdge").Polygon=(Vector2[])sourceAbyssSkin.GetNode<Polygon2D>("ReadableEdge").Polygon.Clone();
        }
        return owned.ToArray();
    }

    private static IEnumerable<Node> Walk(Node root){yield return root;foreach(Node child in root.GetChildren())foreach(Node node in Walk(child))yield return node;}
    private static void Validate(StaticBody2D body,CanyonPlatformVisual skin,PrologueSector root)
    {
        if(body.GetChildCount()!=2||body.GetNodeOrNull("GrappleSurfaceProfile") is not GrappleSurface2D profile||
            body.GetNodeOrNull("Collision") is not CollisionShape2D collider||collider.Shape is not RectangleShape2D rect||rect.Size.X<=0||rect.Size.Y<=0||
            !float.IsFinite(rect.Size.X)||!float.IsFinite(rect.Size.Y)||!float.IsFinite(body.Position.X)||!float.IsFinite(body.Position.Y)||
            body.CollisionLayer!=33||body.CollisionMask!=0||collider.Disabled||collider.OneWayCollision||collider.Transform!=Transform2D.Identity||
            profile.Position!=Vector2.Zero||!profile.UseCollisionHitPoint||profile.Material!=skin.SurfaceMaterial||skin.Position!=body.Position||skin.SkinSize!=rect.Size||skin.SectorId!=root.SectorId||
            body.Scale!=Vector2.One||body.Rotation!=0||body.Skew!=0||skin.Scale!=Vector2.One||skin.Rotation!=0||skin.Skew!=0||
            skin.GetChildCount()!=1||skin.GetNodeOrNull("ReadableEdge") is not Polygon2D edge||
            !body.HasMeta(MinimapTerrainCatalogBuilder.StructuralMarker)||body.GetMeta(MinimapTerrainCatalogBuilder.StructuralMarker).AsString()!=MinimapTerrainCatalogBuilder.Provenance)
            throw new ArgumentException("Inconsistent generated body/profile/rectangle/skin/edge.");
        float halfWidth=rect.Size.X/2;
        if(edge.Position!=new Vector2(0,-rect.Size.Y/2+3)||!edge.Polygon.SequenceEqual(new[]{new Vector2(-halfWidth,-3),new Vector2(halfWidth,-3),new Vector2(halfWidth,3),new Vector2(-halfWidth,3)}))
            throw new ArgumentException("Generated support edge must match its physical rectangle.");
        foreach(var node in Walk(body).Concat(Walk(skin)))
        {
            using var groups=(Godot.Collections.Array)node.GetGroups();
            if(node.UniqueNameInOwner||node.SceneFilePath!=""||groups.Count!=0)throw new ArgumentException("Terrain delta cannot import groups, unique names or nested scenes.");
        }
    }
}
