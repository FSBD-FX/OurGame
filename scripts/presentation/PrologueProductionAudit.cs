using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace Knight.Presentation;

public static class PrologueProductionAudit
{
    private static readonly Regex LocalizationKey = new(
        "^[a-z][a-z0-9_]*(\\.[a-z0-9_]+)+$", RegexOptions.CultureInvariant);

    public static IReadOnlyList<string> FindVisibleFallbacks(Node root)
    {
        var failures = new List<string>();
        Visit(root, node =>
        {
            if (node is not CanvasItem item || !IsEffectivelyVisible(item)) return;
            string type = node.GetType().Name;
            string name = node.Name.ToString();
            if (ContainsForbiddenToken(type) || ContainsForbiddenToken(name))
                failures.Add($"{SafePath(node)} [{type}]");
        });
        return failures;
    }

    public static IReadOnlyList<string> FindInvalidVisibleResources(Node root)
    {
        var failures = new List<string>();
        Visit(root, node =>
        {
            if (node is not CanvasItem item || !IsEffectivelyVisible(item)) return;
            string path = SafePath(node);
            if (node is Sprite2D sprite && sprite.Texture == null)
                failures.Add($"{path}: visible Sprite2D has no texture");
            if (node is TextureRect textureRect && textureRect.Texture == null)
                failures.Add($"{path}: visible TextureRect has no texture");
            if (node is Label label)
            {
                if (LocalizationKey.IsMatch(label.Text ?? string.Empty))
                    failures.Add($"{path}: unresolved localization key '{label.Text}'");
                if (label.GetThemeFont("font") == null)
                    failures.Add($"{path}: no valid runtime font");
            }
            if (node is Button button)
            {
                if (LocalizationKey.IsMatch(button.Text ?? string.Empty))
                    failures.Add($"{path}: unresolved localization key '{button.Text}'");
                if (button.GetThemeFont("font") == null)
                    failures.Add($"{path}: no valid runtime font");
            }
        });
        return failures;
    }

    public static bool ValidateUiScale(float scale) => float.IsFinite(scale) && scale >= .8f && scale <= 1.5f;

    private static bool ContainsForbiddenToken(string value) =>
        value.Contains("Placeholder", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Debug", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Graybox", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Fallback", StringComparison.OrdinalIgnoreCase);

    private static bool IsEffectivelyVisible(CanvasItem item)
    {
        if (!item.IsInsideTree() || !item.IsVisibleInTree()) return false;
        Node current = item;
        while (current != null)
        {
            if (current is CanvasItem canvas &&
                (!canvas.Visible || canvas.Modulate.A <= .001f || canvas.SelfModulate.A <= .001f))
                return false;
            current = current.GetParent();
        }
        return true;
    }

    private static void Visit(Node node, Action<Node> visitor)
    {
        visitor(node);
        foreach (Node child in node.GetChildren()) Visit(child, visitor);
    }

    private static string SafePath(Node node) => node.IsInsideTree()
        ? node.GetPath().ToString()
        : node.Name.ToString();
}
