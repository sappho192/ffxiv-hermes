namespace Hermes.V2.Generator;

using System.Text.Json.Serialization;

internal sealed record HermesManifest(
    [property: JsonPropertyOrder(0)] int SchemaVersion,
    [property: JsonPropertyOrder(1)] Compatibility Compatibility,
    [property: JsonPropertyOrder(2)] Source Source,
    [property: JsonPropertyOrder(3)] Platform Platform,
    [property: JsonPropertyOrder(4)] Roots Roots,
    [property: JsonPropertyOrder(5)] Resources Resources,
    [property: JsonPropertyOrder(6)] Validation Validation);

internal sealed record Compatibility(
    [property: JsonPropertyOrder(0)] string MinimumSharlayanVersion,
    [property: JsonPropertyOrder(1)] int PointerResolverVersion);

internal sealed record Source(
    [property: JsonPropertyOrder(0)] string FcsRepository,
    [property: JsonPropertyOrder(1)] string FcsCommit,
    [property: JsonPropertyOrder(2)] string GeneratorRepository,
    [property: JsonPropertyOrder(3)] string GeneratorCommit);

internal sealed record Platform(
    [property: JsonPropertyOrder(0)] string Process,
    [property: JsonPropertyOrder(1)] string Architecture);

internal sealed record Roots([property: JsonPropertyOrder(0)] FrameworkRoot Framework);

internal sealed record FrameworkRoot(
    [property: JsonPropertyOrder(0)] string Pattern,
    [property: JsonPropertyOrder(1)] int RelativeFollowOffset,
    [property: JsonPropertyOrder(2)] bool IsPointer);

internal sealed record Resources(
    [property: JsonPropertyOrder(0)] ChatLogResource ChatLog,
    [property: JsonPropertyOrder(1)] TalkResource Talk,
    [property: JsonPropertyOrder(2)] CurrentTalkResource CurrentTalk);

internal sealed record ChatLogResource(
    [property: JsonPropertyOrder(0)] string Root,
    [property: JsonPropertyOrder(1)] int UiModuleOffset,
    [property: JsonPropertyOrder(2)] int RaptureLogModuleOffset,
    [property: JsonPropertyOrder(3)] int IndexVectorOffset,
    [property: JsonPropertyOrder(4)] int DataVectorOffset);

internal sealed record TalkResource(
    [property: JsonPropertyOrder(0)] string Root,
    [property: JsonPropertyOrder(1)] string Semantics,
    [property: JsonPropertyOrder(2)] int UiModuleOffset,
    [property: JsonPropertyOrder(3)] int NameOffset,
    [property: JsonPropertyOrder(4)] int TextOffset,
    [property: JsonPropertyOrder(5)] Utf8StringLayout Utf8String);

internal sealed record Utf8StringLayout(
    [property: JsonPropertyOrder(0)] int StringPointerOffset,
    [property: JsonPropertyOrder(1)] int BufferUsedOffset,
    [property: JsonPropertyOrder(2)] string LengthSource);

internal sealed record CurrentTalkResource(
    [property: JsonPropertyOrder(0)] string Root,
    [property: JsonPropertyOrder(1)] string Semantics,
    [property: JsonPropertyOrder(2)] int UiModuleOffset,
    [property: JsonPropertyOrder(3)] int RaptureAtkModuleOffset,
    [property: JsonPropertyOrder(4)] int RaptureAtkUnitManagerOffset,
    [property: JsonPropertyOrder(5)] int AllLoadedUnitsListOffset,
    [property: JsonPropertyOrder(6)] AtkUnitListLayout AtkUnitList,
    [property: JsonPropertyOrder(7)] AtkUnitBaseLayout AtkUnitBase,
    [property: JsonPropertyOrder(8)] AtkValueLayout AtkValue,
    [property: JsonPropertyOrder(9)] string AddonName,
    [property: JsonPropertyOrder(10)] int TextValueIndex,
    [property: JsonPropertyOrder(11)] int NameValueIndex);

internal sealed record AtkUnitListLayout(
    [property: JsonPropertyOrder(0)] int EntriesOffset,
    [property: JsonPropertyOrder(1)] int CountOffset,
    [property: JsonPropertyOrder(2)] int Capacity,
    [property: JsonPropertyOrder(3)] int EntrySize);

internal sealed record AtkUnitBaseLayout(
    [property: JsonPropertyOrder(0)] int NameOffset,
    [property: JsonPropertyOrder(1)] int NameCapacity,
    [property: JsonPropertyOrder(2)] int VisibilityStateOffset,
    [property: JsonPropertyOrder(3)] uint VisibilityMask,
    [property: JsonPropertyOrder(4)] int ReadinessOffset,
    [property: JsonPropertyOrder(5)] uint ReadinessMask,
    [property: JsonPropertyOrder(6)] int AtkValuesPointerOffset,
    [property: JsonPropertyOrder(7)] int AtkValuesCountOffset);

internal sealed record AtkValueLayout(
    [property: JsonPropertyOrder(0)] int Size,
    [property: JsonPropertyOrder(1)] int TypeOffset,
    [property: JsonPropertyOrder(2)] int ValueOffset,
    [property: JsonPropertyOrder(3)] IReadOnlyList<int> AllowedStringTypes);

internal sealed record Validation(
    [property: JsonPropertyOrder(0)] string Status,
    [property: JsonPropertyOrder(1), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? GameVersion = null,
    [property: JsonPropertyOrder(2), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ExecutableSha256 = null,
    [property: JsonPropertyOrder(3), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? VerifierCommit = null);

internal sealed record ExtractedMetadata(
    string Pattern,
    int RelativeFollowOffset,
    bool IsPointer,
    int UiModuleOffset,
    int RaptureLogModuleOffset,
    int IndexVectorOffset,
    int DataVectorOffset,
    int TalkNameOffset,
    int TalkTextOffset,
    int StringPointerOffset,
    int BufferUsedOffset,
    int RaptureAtkModuleOffset,
    int RaptureAtkUnitManagerOffset,
    int AllLoadedUnitsListOffset,
    int AtkUnitListEntriesOffset,
    int AtkUnitListCountOffset,
    int AtkUnitListCapacity,
    int AtkUnitListEntrySize,
    int AddonNameOffset,
    int AddonNameCapacity,
    int AddonVisibilityStateOffset,
    uint AddonVisibilityMask,
    int AddonReadinessOffset,
    uint AddonReadinessMask,
    int AtkValuesPointerOffset,
    int AtkValuesCountOffset,
    int AtkValueSize,
    int AtkValueTypeOffset,
    int AtkValueValueOffset,
    int ManagedStringType);
