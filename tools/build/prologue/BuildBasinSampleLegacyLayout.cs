using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Knight.Prologue;
using Knight.Prologue.Sample;
using Knight.Prologue.Seamless;

namespace Knight.Build.Prologue;

/// <summary>One-shot capture, not a second gameplay builder. Never overwrites any destination.</summary>
public partial class BuildBasinSampleLegacyLayout : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            string[] hashes =
            [
                "BA7049D79791F39E6724718CF135279BE22B53C3B5AB1A072E0EE0FCC91644E0",
                "0E500C5D8D9E7E6F4780A2E1F98BA8A915CE988999E3FA03F4346D3F8557A8B7",
                "D8A4D81226C98C2977CA16F34A82CE0A68E5A5326A75431B7DCB6E87546977C3",
                "E14E5B64A434F7B18D31E2821C25EA54302EB304D0388A8012BBF0AA4F37865B",
                "741F78674DD57B1BD1E2001777FE70192D94AC09AD8AD6CD0481163F279CB370"
            ];
            // Check the whole destination set and source identities before creating any file.
            foreach (PrologueSectorId id in Enum.GetValues<PrologueSectorId>())
            {
                string destination = ProjectSettings.GlobalizePath(BasinSampleLegacyLayout.FrozenScenePath(id));
                if (File.Exists(destination)) throw new IOException("Frozen snapshot already exists: " + destination);
                string source = ProjectSettings.GlobalizePath($"res://scenes/prologue/sectors/{id}.tscn");
                if (Hash(source) != hashes[(int)id]) throw new IOException("Not the approved pre-v2 source: " + source);
            }
            Directory.CreateDirectory(ProjectSettings.GlobalizePath("res://scenes/prologue/sample/legacy"));
            foreach (PrologueSectorId id in Enum.GetValues<PrologueSectorId>())
            {
                string sourcePath = $"res://scenes/prologue/sectors/{id}.tscn";
                string frozenPath = BasinSampleLegacyLayout.FrozenScenePath(id);
                // Copying original serialized bytes retains every packed property and external
                // runtime/art reference; overwrite:false is also a final atomic creation guard.
                File.Copy(ProjectSettings.GlobalizePath(sourcePath), ProjectSettings.GlobalizePath(frozenPath), false);
                Node source = GD.Load<PackedScene>(sourcePath).Instantiate();
                Node frozen = GD.Load<PackedScene>(frozenPath).Instantiate();
                try
                {
                    if (Fingerprint(source) != Fingerprint(frozen) ||
                        Hash(ProjectSettings.GlobalizePath(frozenPath)) != hashes[(int)id])
                        throw new InvalidDataException("Packed snapshot equivalence failed: " + id);
                    GD.Print($"FROZEN_CAPTURE_OK id={id} source={sourcePath} destination={frozenPath} sha256={hashes[(int)id]} packed_geometry_collision_profiles_equivalent=True");
                }
                finally { source.Free(); frozen.Free(); }
            }
            Quit(0);
        }
        catch (Exception error) { GD.PushError(error.ToString()); Quit(1); }
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static string Fingerprint(Node root)
    {
        var result = new StringBuilder();
        Visit(root);
        return result.ToString();
        void Visit(Node node)
        {
            result.Append(root.GetPathTo(node)).Append('|').Append(node.GetType().FullName);
            if (node is Node2D spatial) result.Append('|').Append(spatial.Transform);
            if (node is CollisionObject2D body) result.Append('|').Append(body.CollisionLayer).Append('|').Append(body.CollisionMask);
            if (node is CollisionShape2D collision)
            {
                result.Append('|').Append(collision.Disabled).Append('|').Append(collision.OneWayCollision);
                foreach (var property in collision.Shape.GetPropertyList())
                {
                    string name = property["name"].AsString();
                    if (name is "size" or "radius" or "height" or "points" or "segments")
                        result.Append('|').Append(name).Append('=').Append(collision.Shape.Get(name));
                }
            }
            if (node.Name == "GrappleSurfaceProfile")
                foreach (var property in node.GetPropertyList())
                {
                    string name = property["name"].AsString();
                    Variant value = node.Get(name);
                    if (value.VariantType != Variant.Type.Object)
                        result.Append('|').Append(name).Append('=').Append(value);
                }
            result.AppendLine();
            foreach (Node child in node.GetChildren()) Visit(child);
        }
    }
}
