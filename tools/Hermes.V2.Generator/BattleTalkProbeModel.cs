namespace Hermes.V2.Generator;

using System.Text.Json.Serialization;

internal sealed record BattleTalkProbeLayout(
    [property: JsonPropertyOrder(0)] int SchemaVersion,
    [property: JsonPropertyOrder(1)] string FcsCommit,
    [property: JsonPropertyOrder(2)] BattleTalkProbeUiLayout Ui,
    [property: JsonPropertyOrder(3)] BattleTalkProbeAddonLayout Addon,
    [property: JsonPropertyOrder(4)] BattleTalkProbeArrayLayout Arrays,
    [property: JsonPropertyOrder(5)] BattleTalkProbeAgentHudLayout AgentHud,
    [property: JsonPropertyOrder(6)] Utf8StringLayout Utf8String);

internal sealed record BattleTalkProbeUiLayout(
    [property: JsonPropertyOrder(0)] int UiModuleOffset,
    [property: JsonPropertyOrder(1)] int RaptureAtkModuleOffset);

internal sealed record BattleTalkProbeAddonLayout(
    [property: JsonPropertyOrder(0)] int RaptureAtkUnitManagerOffset,
    [property: JsonPropertyOrder(1)] int AllLoadedUnitsListOffset,
    [property: JsonPropertyOrder(2)] AtkUnitListLayout AtkUnitList,
    [property: JsonPropertyOrder(3)] AtkUnitBaseLayout AtkUnitBase,
    [property: JsonPropertyOrder(4)] AtkValueLayout AtkValue,
    [property: JsonPropertyOrder(5)] string BattleTalkAddonName);

internal sealed record BattleTalkProbeArrayLayout(
    [property: JsonPropertyOrder(0)] int AtkArrayDataHolderOffset,
    [property: JsonPropertyOrder(1)] int NumberArrayCountOffset,
    [property: JsonPropertyOrder(2)] int NumberArraysOffset,
    [property: JsonPropertyOrder(3)] int StringArrayCountOffset,
    [property: JsonPropertyOrder(4)] int StringArraysOffset,
    [property: JsonPropertyOrder(5)] int ArraySizeOffset,
    [property: JsonPropertyOrder(6)] int ArrayUpdateStateOffset,
    [property: JsonPropertyOrder(7)] int NumberValuesOffset,
    [property: JsonPropertyOrder(8)] int StringValuesOffset,
    [property: JsonPropertyOrder(9)] int ManagedStringValuesOffset,
    [property: JsonPropertyOrder(10)] int BattleTalkNumberArrayId,
    [property: JsonPropertyOrder(11)] int BattleTalkStringArrayId);

internal sealed record BattleTalkProbeAgentHudLayout(
    [property: JsonPropertyOrder(0)] int AgentModuleOffset,
    [property: JsonPropertyOrder(1)] int AgentsOffset,
    [property: JsonPropertyOrder(2)] int AgentsCapacity,
    [property: JsonPropertyOrder(3)] int AgentEntrySize,
    [property: JsonPropertyOrder(4)] int HudAgentId,
    [property: JsonPropertyOrder(5)] int QueueOffset,
    [property: JsonPropertyOrder(6)] int QueueCapacity,
    [property: JsonPropertyOrder(7)] int QueueEntrySize,
    [property: JsonPropertyOrder(8)] int IsPendingOffset,
    [property: JsonPropertyOrder(9)] int StyleOffset,
    [property: JsonPropertyOrder(10)] int NameOffset,
    [property: JsonPropertyOrder(11)] int TextOffset,
    [property: JsonPropertyOrder(12)] int ImageOffset,
    [property: JsonPropertyOrder(13)] int SoundOffset,
    [property: JsonPropertyOrder(14)] int EntityIdOffset);

internal sealed record BattleTalkProbeMetadata(
    int AtkArrayDataHolderOffset,
    int NumberArrayCountOffset,
    int NumberArraysOffset,
    int StringArrayCountOffset,
    int StringArraysOffset,
    int ArraySizeOffset,
    int ArrayUpdateStateOffset,
    int NumberValuesOffset,
    int StringValuesOffset,
    int ManagedStringValuesOffset,
    int BattleTalkNumberArrayId,
    int BattleTalkStringArrayId,
    int AgentModuleOffset,
    int AgentsOffset,
    int AgentsCapacity,
    int AgentEntrySize,
    int HudAgentId,
    int QueueOffset,
    int QueueCapacity,
    int QueueEntrySize,
    int IsPendingOffset,
    int StyleOffset,
    int NameOffset,
    int TextOffset,
    int ImageOffset,
    int SoundOffset,
    int EntityIdOffset);
