using System;
using Godot;
using Knight.Prologue.Seamless;
using Knight.World;

namespace Knight.Enemies.Residents;

/// <summary>Actual enabled void shapes only, never potential hazard envelopes or a home-height threshold.</summary>
public static class ResidentVoidContact
{
    public static bool Crossed(CharacterBody2D actor,CollisionShape2D body,Transform2D before)
    {
        if(!Live(actor)||!Live(body)||body.Disabled||body.Shape==null) return false;
        Transform2D after=body.GlobalTransform;
        if(before.X!=after.X||before.Y!=after.Y)
            throw new InvalidOperationException("Resident void sweep requires a fixed body basis.");
        SeamlessPrologueWorld world=Ancestor<SeamlessPrologueWorld>(actor);
        if(world!=null&&!Mounted(world,Ancestor<PrologueSector>(actor))) return false;
        using Godot.Collections.Array zones=(Godot.Collections.Array)actor.GetTree().GetNodesInGroup(KillZone.ResidentVoidGroup);
        for(int i=0;i<zones.Count;i++)
        {
            if(zones[i].AsGodotObject() is not KillZone zone||!Live(zone)||!zone.Monitoring||
               zone.GetWorld2D()!=actor.GetWorld2D()||!ReferenceEquals(world,Ancestor<SeamlessPrologueWorld>(zone))) continue;
            if(world!=null&&!Mounted(world,Ancestor<PrologueSector>(zone))) continue;
            using Godot.Collections.Array children=(Godot.Collections.Array)zone.GetChildren();
            for(int j=0;j<children.Count;j++)
            {
                if(children[j].AsGodotObject() is not CollisionShape2D boundary||!Live(boundary)||
                   boundary.Disabled||boundary.Shape==null||boundary.GetParent()!=zone) continue;
                Transform2D pose=boundary.GlobalTransform;
                // Native initial/end contacts are explicit: no assumption that a
                // swept query includes initial overlap. All resources are borrowed.
                if(body.Shape.Collide(before,boundary.Shape,pose)||
                   body.Shape.CollideWithMotion(before,after.Origin-before.Origin,boundary.Shape,pose,Vector2.Zero)||
                   body.Shape.Collide(after,boundary.Shape,pose)) return true;
            }
        }
        return false;
    }

    private static bool Mounted(SeamlessPrologueWorld world,PrologueSector sector) =>
        Live(world)&&Live(sector)&&sector.GetParent()==world&&!sector.HasMeta(SeamlessPrologueWorld.StagedSectorMeta)&&
        world.TryGetMountedSector(sector.SectorId,out PrologueSector current)&&ReferenceEquals(current,sector);

    private static T Ancestor<T>(Node node) where T:Node
    {
        for(Node current=node;current!=null;current=current.GetParent()) if(current is T found)return found;
        return null;
    }
    private static bool Live(Node node)
    {
        if(!GodotObject.IsInstanceValid(node)||!node.IsInsideTree())return false;
        for(Node current=node;current!=null;current=current.GetParent())if(current.IsQueuedForDeletion())return false;
        return true;
    }
}
