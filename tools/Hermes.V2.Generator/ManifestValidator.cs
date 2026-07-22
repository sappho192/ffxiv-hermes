namespace Hermes.V2.Generator;

using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

using Json.Schema;

internal static partial class ManifestValidator {
    private static readonly ConcurrentDictionary<string, JsonSchema> Schemas = new(StringComparer.OrdinalIgnoreCase);

    internal static void Validate(byte[] manifestBytes, string schemaPath) {
        JsonNode instance = JsonNode.Parse(manifestBytes)
                            ?? throw new InvalidOperationException("Manifest JSON is empty.");
        string fullSchemaPath = Path.GetFullPath(schemaPath);
        JsonSchema schema = Schemas.GetOrAdd(fullSchemaPath, path => JsonSchema.FromText(File.ReadAllText(path)));
        using JsonDocument document = JsonDocument.Parse(manifestBytes);
        EvaluationResults results = schema.Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (!results.IsValid) {
            throw new InvalidOperationException("Manifest does not satisfy schemas/hermes-v2.schema.json.");
        }

        ValidateSemanticRules(instance);
    }

    private static void ValidateSemanticRules(JsonNode instance) {
        JsonObject root = instance.AsObject();
        string pattern = root["roots"]!["framework"]!["pattern"]!.GetValue<string>();
        int relativeOffset = root["roots"]!["framework"]!["relativeFollowOffset"]!.GetValue<int>();
        if (!PatternRegex().IsMatch(pattern) || relativeOffset > pattern.Length / 2) {
            throw new InvalidOperationException("Framework pattern and relative follow offset are inconsistent.");
        }

        JsonNode chat = root["resources"]!["chatLog"]!;
        int index = chat["indexVectorOffset"]!.GetValue<int>();
        int data = chat["dataVectorOffset"]!.GetValue<int>();
        if (index % 8 != 0 || data % 8 != 0 || Math.Abs(data - index) < 24) {
            throw new InvalidOperationException("CHATLOG vectors must be x64-aligned, non-overlapping StdVector layouts.");
        }

        JsonNode talk = root["resources"]!["talk"]!;
        if (talk["uiModuleOffset"]!.GetValue<int>() != chat["uiModuleOffset"]!.GetValue<int>()) {
            throw new InvalidOperationException("CHATLOG and Talk must use the same UIModule offset.");
        }

        JsonNode utf8 = talk["utf8String"]!;
        int pointer = utf8["stringPointerOffset"]!.GetValue<int>();
        int used = utf8["bufferUsedOffset"]!.GetValue<int>();
        int length = utf8["stringLengthOffset"]!.GetValue<int>();
        if (pointer % 8 != 0 || used % 8 != 0 || length % 8 != 0 || !(pointer < used && used < length)) {
            throw new InvalidOperationException("Utf8String offsets must be ordered and x64-aligned.");
        }
    }

    [GeneratedRegex("^(?:[0-9A-F]{2}|\\?\\?)+$", RegexOptions.CultureInvariant)]
    private static partial Regex PatternRegex();
}
