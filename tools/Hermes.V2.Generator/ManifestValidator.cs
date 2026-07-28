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
        if (pointer % 8 != 0 || used % 8 != 0 || pointer >= used
            || utf8["lengthSource"]!.GetValue<string>() != "bufferUsedMinusNull") {
            throw new InvalidOperationException("Utf8String pointer and BufUsed offsets must be ordered, x64-aligned, and use BufUsed minus null.");
        }

        JsonNode current = root["resources"]!["currentTalk"]!;
        if (current["uiModuleOffset"]!.GetValue<int>() != chat["uiModuleOffset"]!.GetValue<int>()) {
            throw new InvalidOperationException("CHATLOG, Talk, and CurrentTalk must use the same UIModule offset.");
        }

        JsonNode unitList = current["atkUnitList"]!;
        int entriesOffset = unitList["entriesOffset"]!.GetValue<int>();
        int countOffset = unitList["countOffset"]!.GetValue<int>();
        int capacity = unitList["capacity"]!.GetValue<int>();
        int entrySize = unitList["entrySize"]!.GetValue<int>();
        if (entrySize != 8 || entriesOffset % entrySize != 0
            || countOffset != entriesOffset + (capacity * entrySize)) {
            throw new InvalidOperationException("AtkUnitList entries must be a contiguous fixed array of x64 pointers followed by Count.");
        }

        JsonNode addon = current["atkUnitBase"]!;
        int nameOffset = addon["nameOffset"]!.GetValue<int>();
        int nameCapacity = addon["nameCapacity"]!.GetValue<int>();
        int valuesPointerOffset = addon["atkValuesPointerOffset"]!.GetValue<int>();
        if (nameOffset + nameCapacity > valuesPointerOffset || valuesPointerOffset % 8 != 0
            || addon["visibilityMask"]!.GetValue<uint>() == 0
            || addon["readinessMask"]!.GetValue<uint>() == 0) {
            throw new InvalidOperationException("AtkUnitBase name, state masks, and AtkValues pointer layout are inconsistent.");
        }

        JsonNode value = current["atkValue"]!;
        int valueSize = value["size"]!.GetValue<int>();
        int typeOffset = value["typeOffset"]!.GetValue<int>();
        int valueOffset = value["valueOffset"]!.GetValue<int>();
        if (typeOffset + sizeof(int) > valueSize || valueOffset % 8 != 0 || valueOffset + 8 > valueSize) {
            throw new InvalidOperationException("AtkValue type and value fields must fit within each x64 AtkValue entry.");
        }

        int textIndex = current["textValueIndex"]!.GetValue<int>();
        int nameIndex = current["nameValueIndex"]!.GetValue<int>();
        if (textIndex < 0 || nameIndex < 0 || textIndex == nameIndex) {
            throw new InvalidOperationException("CurrentTalk text and name AtkValue indexes must be distinct and non-negative.");
        }

        JsonNode? battleTalk = root["resources"]!["battleTalk"];
        if (battleTalk != null) {
            string minimumVersion =
                root["compatibility"]!["minimumSharlayanVersion"]!.GetValue<string>();
            if (SemanticVersionComparer.Compare(minimumVersion, "9.2.0") < 0) {
                throw new InvalidOperationException(
                    "BattleTalk manifests must require Sharlayan 9.2.0 or newer.");
            }

            string[] sharedOffsets = [
                "uiModuleOffset",
                "raptureAtkModuleOffset",
                "raptureAtkUnitManagerOffset",
                "allLoadedUnitsListOffset",
            ];
            foreach (string property in sharedOffsets) {
                if (battleTalk[property]!.GetValue<int>() != current[property]!.GetValue<int>()) {
                    throw new InvalidOperationException($"BattleTalk and CurrentTalk must share {property}.");
                }
            }

            if (!JsonNode.DeepEquals(battleTalk["atkUnitList"], current["atkUnitList"])) {
                throw new InvalidOperationException("BattleTalk and CurrentTalk must share the AtkUnitList layout.");
            }

            JsonNode battleAddon = battleTalk["addon"]!;
            string[] sharedAddonOffsets = [
                "nameOffset",
                "nameCapacity",
                "visibilityStateOffset",
                "visibilityMask",
                "readinessOffset",
                "readinessMask",
            ];
            foreach (string property in sharedAddonOffsets) {
                if (battleAddon[property]!.ToJsonString() != addon[property]!.ToJsonString()) {
                    throw new InvalidOperationException($"BattleTalk and CurrentTalk must share addon {property}.");
                }
            }

            JsonNode holder = battleTalk["arrayDataHolder"]!;
            int numberArraysOffset = holder["numberArraysOffset"]!.GetValue<int>();
            int stringArraysOffset = holder["stringArraysOffset"]!.GetValue<int>();
            int numberValuesOffset = battleTalk["numberValuesOffset"]!.GetValue<int>();
            int stringValuesOffset = battleTalk["stringValuesOffset"]!.GetValue<int>();
            JsonNode arrayData = battleTalk["arrayData"]!;
            if (numberArraysOffset % 8 != 0
                || stringArraysOffset % 8 != 0
                || numberValuesOffset % 8 != 0
                || stringValuesOffset % 8 != 0
                || arrayData["sizeOffset"]!.GetValue<int>() + sizeof(int)
                    > arrayData["updateStateOffset"]!.GetValue<int>()) {
                throw new InvalidOperationException("BattleTalk array headers and value pointers are inconsistent.");
            }
        }

        string validationStatus = root["validation"]!["status"]!.GetValue<string>();
        if (validationStatus == "generated"
            && SemanticVersionComparer.Compare(
                root["compatibility"]!["minimumSharlayanVersion"]!.GetValue<string>(),
                "9.2.1") < 0) {
            throw new InvalidOperationException(
                "Generated manifests must require Sharlayan 9.2.1 or newer.");
        }
    }

    [GeneratedRegex("^(?:[0-9A-F]{2}|\\?\\?)+$", RegexOptions.CultureInvariant)]
    private static partial Regex PatternRegex();
}
