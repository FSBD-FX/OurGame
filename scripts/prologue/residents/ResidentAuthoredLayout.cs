using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using Knight.Build.Prologue;
using Knight.Prologue.Seamless;

namespace Knight.Prologue.Residents;

// Authored numbers are not native geometry, combat or final-art certification.
public sealed record ResidentAuthoredSite(ResidentPopulationDefinition Definition, Rect2 Occupancy);

public static class ResidentAuthoredLayout
{
    public const string ResourcePath = "res://assets/data/residents/canyon-resident-layout-v001.json";
    public const string PayloadSha256 = "AEACB6915FD16F9BDEE4ECC93FC59AD55D028EAE40E05EFCE83926831762DF18";
    public const string SourceContextSha256 = "376E243A4D9DF7363A97769F2F6A062853B1DBF52EC698D017035B703B119707";

    // No fallback to another drive, test storage, receipt or caller-provided path.
    public static IReadOnlyList<ResidentAuthoredSite> Load() => Parse(
        FileAccess.GetFileAsBytes(ResourcePath), PayloadSha256, SourceContextSha256);

    public static IReadOnlyList<ResidentAuthoredSite> Parse(ReadOnlySpan<byte> bytes,
        string expectedDigest, string expectedContext)
    {
        if (bytes.Length == 0 || bytes.Length > 1024 * 1024 || !Digest(expectedDigest) || !Digest(expectedContext))
            throw Invalid("envelope");
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), expectedDigest, StringComparison.OrdinalIgnoreCase))
            throw Invalid("digest");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            var root = document.RootElement;
            Fields(root, "kind", "schema", "contextSha256", "sites");
            if (root.GetProperty("kind").GetString() != "final-joint" || root.GetProperty("schema").GetInt32() != 1 ||
                root.GetProperty("contextSha256").GetString() != expectedContext) throw Invalid("schema_context");
            var rows = root.GetProperty("sites");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 50) throw Invalid("count");
            var result = new List<ResidentAuthoredSite>(50);
            for (int i = 0; i < 50; i++)
            {
                var row = rows[i]; var slot = CanyonResidentCatalog.All[i];
                Fields(row, "stableId", "home", "territory", "points", "occupancy");
                if (row.GetProperty("stableId").GetString() != slot.StableId) throw Invalid("identity_order");
                Vector2 home = Point(row.GetProperty("home"));
                Rect2 territory = Rectangle(row.GetProperty("territory")), occupancy = Rectangle(row.GetProperty("occupancy"));
                if (territory != CanyonWorldLayout.Nodes.Single(x => x.Id == slot.RouteNode).WorldBounds)
                    throw Invalid("original_territory");
                var route = row.GetProperty("points");
                if (route.ValueKind != JsonValueKind.Array || route.GetArrayLength() is < 1 or > 16) throw Invalid("points");
                Vector2[] points = route.EnumerateArray().Select(Point).ToArray();
                if (points[0] != home || points.Any(p => p.X < territory.Position.X || p.Y < territory.Position.Y ||
                    p.X > territory.End.X || p.Y > territory.End.Y)) throw Invalid("home_points");
                if (slot.Archetype == ResidentArchetype.Turret ? points.Length != 1 :
                    points.Length < 3 || points[^1] != home || points.Distinct().Count() < 2) throw Invalid("closed_or_stationary");
                result.Add(new(slot with { Placement = new ResidentPlacementCandidate(home, territory, points) }, occupancy));
            }
            return result.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException)
        { throw new ArgumentException("resident_layout.json", error); }
    }

    private static void Fields(JsonElement value, params string[] required)
    {
        if (value.ValueKind != JsonValueKind.Object) throw Invalid("object");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in value.EnumerateObject())
            if (!seen.Add(field.Name) || !required.Contains(field.Name, StringComparer.Ordinal)) throw Invalid("fields");
        if (seen.Count != required.Length) throw Invalid("fields");
    }
    private static Vector2 Point(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2) throw Invalid("point");
        float x = value[0].GetSingle(), y = value[1].GetSingle();
        if (!float.IsFinite(x) || !float.IsFinite(y)) throw Invalid("point_finite");
        return new(x, y);
    }
    private static Rect2 Rectangle(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 4) throw Invalid("rectangle");
        float x = value[0].GetSingle(), y = value[1].GetSingle(), w = value[2].GetSingle(), h = value[3].GetSingle();
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(w) || !float.IsFinite(h) ||
            w <= 0 || h <= 0 || !float.IsFinite(x + w) || !float.IsFinite(y + h)) throw Invalid("rectangle_finite");
        return new(x, y, w, h);
    }
    private static bool Digest(string value) => value?.Length == 64 && value.All(Uri.IsHexDigit);
    private static ArgumentException Invalid(string code) => new("resident_layout." + code);
}
