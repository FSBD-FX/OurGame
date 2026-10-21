using Godot;

namespace Knight.Tools;

public static class SceneBuildUtil
{
    // Opt-in transactional path for catalog-bearing main only. Other builders are unchanged.
    public static Error PackAndSaveValidated(Node root, string resourcePath, System.Action<Node> validate)
    {
        if (resourcePath != "res://scenes/prologue/PrologueLevel.tscn" || validate == null)
            throw new System.ArgumentException("Only the catalog-bearing main is admitted.");
        string staging = "res://scenes/prologue/.task6-PrologueLevel-" + System.Guid.NewGuid().ToString("N") + ".tscn";
        try
        {
            Error error = PackAndSave(root, staging);
            if (error != Error.Ok) return error;
            var saved = ResourceLoader.Load<PackedScene>(staging, cacheMode: ResourceLoader.CacheMode.Ignore);
            if (saved == null) return Error.InvalidData;
            Node copy = saved.Instantiate();
            try
            {
                if (Count(copy) != Count(root)) return Error.InvalidData;
                validate(copy);
            }
            finally { copy.Free(); }
            // Same-directory rename is atomic; failed validation never touches the previous main.
            System.IO.File.Move(ProjectSettings.GlobalizePath(staging), ProjectSettings.GlobalizePath(resourcePath), true);
            return Error.Ok;
        }
        finally
        {
            string absolute = ProjectSettings.GlobalizePath(staging);
            if (System.IO.File.Exists(absolute)) System.IO.File.Delete(absolute);
        }
    }

    public static Error PackAndSave(Node root, string resourcePath)
    {
        SetOwners(root, root);
        int expected = Count(root);
        using var packed = new PackedScene();
        Error packError = packed.Pack(root);
        if (packError != Error.Ok) return packError;
        Node copy = packed.Instantiate();
        int actual = Count(copy);
        copy.Free();
        if (actual != expected)
        {
            return Error.InvalidData;
        }
        return ResourceSaver.Save(packed, resourcePath);
    }

    private static void SetOwners(Node node, Node owner)
    {
        foreach (Node child in node.GetChildren())
        {
            child.Owner = owner;
            if (string.IsNullOrEmpty(child.SceneFilePath)) SetOwners(child, owner);
        }
    }

    private static int Count(Node node)
    {
        int count = 1;
        foreach (Node child in node.GetChildren()) count += Count(child);
        return count;
    }
}
