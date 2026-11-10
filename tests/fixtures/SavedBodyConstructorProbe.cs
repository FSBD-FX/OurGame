using Godot;

namespace Knight.Tests.Fixtures;

public partial class SavedBodyConstructorProbe : CharacterBody2D
{
    public static int Constructions { get; private set; }
    public static bool AddUnexpectedBody { get; set; }
    public SavedBodyConstructorProbe()
    {
        Constructions++;
        if (AddUnexpectedBody)
            AddChild(new CollisionShape2D { Name = "ConstructorAddedBody", Shape = new CircleShape2D { Radius = 4 } });
    }
}
