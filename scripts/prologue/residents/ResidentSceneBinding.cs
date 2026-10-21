using System;
using Godot;

namespace Knight.Prologue.Residents;

// Saved data only; a loader must configure the owning actor BEFORE AddChild.
public partial class ResidentSceneBinding : Node
{
    [Export] public ResidentSubtypeProfile Profile { get; set; }
    [Export] public ResidentSceneState PresentationState { get; set; } = ResidentSceneState.ExplicitPlaceholder;

    public static CharacterBody2D Instantiate(PackedScene scene, ResidentSubtypeDefinition expected,
        ResidentPopulationDefinition slot, bool allowPlaceholder = false)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(slot);
        if (!GodotObject.IsInstanceValid(scene) || !Enum.IsDefined(expected.SceneState) ||
            expected.SceneState == ResidentSceneState.Unimplemented || string.IsNullOrWhiteSpace(expected.ScenePath) ||
            Normalize(scene.ResourcePath) != Normalize(expected.ScenePath) ||
            Normalize(expected.ScenePath) == Normalize(expected.ProbeScenePath))
            throw new ArgumentException("Load the distinct saved subtype leaf, not an unimplemented slot or probe alias.");

        Node root = scene.Instantiate();
        try
        {
            ResidentSceneBinding binding = root.GetNodeOrNull<ResidentSceneBinding>("ResidentBinding");
            if (root is not CharacterBody2D actor || CountBindings(root) != 1 || binding?.Profile == null ||
                !Enum.IsDefined(binding.PresentationState) || binding.PresentationState != expected.SceneState ||
                binding.PresentationState == ResidentSceneState.Unimplemented ||
                !allowPlaceholder && binding.PresentationState != ResidentSceneState.Implemented)
                throw new ArgumentException("Require one exported binding and explicit admission for placeholder visuals.");
            binding.Profile.ConfigureActor(actor, expected, slot);
            return actor;
        }
        catch { root.Free(); throw; }
    }

    private static string Normalize(string path) => path?.Replace('\\', '/') ?? "";
    private static int CountBindings(Node root)
    {
        int count = root is ResidentSceneBinding ? 1 : 0;
        foreach (Node child in root.GetChildren()) count += CountBindings(child);
        return count;
    }
}
