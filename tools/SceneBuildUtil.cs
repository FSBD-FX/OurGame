using Godot;

namespace Knight.Tools;

public static class SceneBuildUtil
{
    public static Error PackAndSave(Node root, string resourcePath)
    {
        SetOwners(root, root);
        int expected = Count(root);
        var packed = new PackedScene();
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
