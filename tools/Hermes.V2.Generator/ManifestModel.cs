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
    [property: JsonPropertyOrder(1)] TalkResource Talk);

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
    [property: JsonPropertyOrder(2)] int StringLengthOffset);

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
    int StringLengthOffset);
