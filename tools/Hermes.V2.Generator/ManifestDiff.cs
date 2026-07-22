namespace Hermes.V2.Generator;

using System.Text;
using System.Text.Json.Nodes;

internal static class ManifestDiff {
    internal static string Create(byte[] currentBytes, string? previousPath, string revision) {
        StringBuilder output = new StringBuilder()
            .AppendLine("# Hermes v2 candidate")
            .AppendLine()
            .AppendLine($"- Resource revision: `{revision}`");

        if (string.IsNullOrWhiteSpace(previousPath) || !File.Exists(previousPath)) {
            return output.AppendLine("- Comparison: no previous manifest supplied").ToString();
        }

        JsonNode? current = JsonNode.Parse(currentBytes);
        JsonNode? previous = JsonNode.Parse(File.ReadAllBytes(previousPath));
        List<string> changes = [];
        Compare("$", previous, current, changes);
        output.AppendLine($"- Changed fields: {changes.Count}").AppendLine();
        if (changes.Count == 0) {
            output.AppendLine("No field changes.");
        }
        else {
            foreach (string change in changes) {
                output.Append("- ").AppendLine(change);
            }
        }

        return output.ToString();
    }

    private static void Compare(string path, JsonNode? before, JsonNode? after, ICollection<string> changes) {
        if (JsonNode.DeepEquals(before, after)) {
            return;
        }

        if (before is JsonObject beforeObject && after is JsonObject afterObject) {
            foreach (string key in beforeObject.Select(property => property.Key).Union(afterObject.Select(property => property.Key)).Order()) {
                Compare($"{path}.{key}", beforeObject[key], afterObject[key], changes);
            }
            return;
        }

        changes.Add($"`{path}`: `{Display(before)}` → `{Display(after)}`");
    }

    private static string Display(JsonNode? value) {
        return value?.ToJsonString().Replace("`", "\\`", StringComparison.Ordinal) ?? "<missing>";
    }
}
