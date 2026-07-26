namespace Hermes.V2.Generator.Tests;

using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

using Xunit;

public sealed class GeneratorTests {
    [Fact]
    public void ExtractsRequiredFcsMetadata() {
        Assembly fixture = typeof(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework).Assembly;

        ExtractedMetadata result = FcsMetadataExtractor.Extract(fixture);

        Assert.Equal("488B1D????????8B7C24", result.Pattern);
        Assert.Equal(3, result.RelativeFollowOffset);
        Assert.True(result.IsPointer);
        Assert.Equal(0x2B68, result.UiModuleOffset);
        Assert.Equal(0x1AC0, result.RaptureLogModuleOffset);
        Assert.Equal(0x48, result.IndexVectorOffset);
        Assert.Equal(0x60, result.DataVectorOffset);
        Assert.Equal(0xFEF00, result.TalkNameOffset);
        Assert.Equal(0xFEF68, result.TalkTextOffset);
        Assert.Equal(0, result.StringPointerOffset);
        Assert.Equal(0x10, result.BufferUsedOffset);
        Assert.Equal(0xD2670, result.RaptureAtkModuleOffset);
        Assert.Equal(0x13420, result.RaptureAtkUnitManagerOffset);
        Assert.Equal(0x6900, result.AllLoadedUnitsListOffset);
        Assert.Equal(0x8, result.AtkUnitListEntriesOffset);
        Assert.Equal(0x808, result.AtkUnitListCountOffset);
        Assert.Equal(256, result.AtkUnitListCapacity);
        Assert.Equal(8, result.AtkUnitListEntrySize);
        Assert.Equal(0x8, result.AddonNameOffset);
        Assert.Equal(32, result.AddonNameCapacity);
        Assert.Equal(0x198, result.AddonVisibilityStateOffset);
        Assert.Equal(0x00200000U, result.AddonVisibilityMask);
        Assert.Equal(0x1A1, result.AddonReadinessOffset);
        Assert.Equal(0x01U, result.AddonReadinessMask);
        Assert.Equal(0x178, result.AtkValuesPointerOffset);
        Assert.Equal(0x1E2, result.AtkValuesCountOffset);
        Assert.Equal(0x10, result.AtkValueSize);
        Assert.Equal(0, result.AtkValueTypeOffset);
        Assert.Equal(0x8, result.AtkValueValueOffset);
        Assert.Equal(0x28, result.ManagedStringType);
    }

    [Fact]
    public void ExtractsBattleTalkProbeMetadata() {
        Assembly fixture = typeof(FFXIVClientStructs.FFXIV.Client.System.Framework.Framework).Assembly;

        BattleTalkProbeMetadata result = FcsMetadataExtractor.ExtractBattleTalkProbe(fixture);

        Assert.Equal(0x1BA8, result.AtkArrayDataHolderOffset);
        Assert.Equal(0, result.NumberArrayCountOffset);
        Assert.Equal(0x18, result.NumberArraysOffset);
        Assert.Equal(0x2, result.StringArrayCountOffset);
        Assert.Equal(0x30, result.StringArraysOffset);
        Assert.Equal(0x8, result.ArraySizeOffset);
        Assert.Equal(0x1F, result.ArrayUpdateStateOffset);
        Assert.Equal(0x28, result.NumberValuesOffset);
        Assert.Equal(0x28, result.StringValuesOffset);
        Assert.Equal(0x30, result.ManagedStringValuesOffset);
        Assert.Equal(38, result.BattleTalkNumberArrayId);
        Assert.Equal(35, result.BattleTalkStringArrayId);
        Assert.Equal(0x12400, result.AgentModuleOffset);
        Assert.Equal(0x20, result.AgentsOffset);
        Assert.Equal(509, result.AgentsCapacity);
        Assert.Equal(8, result.AgentEntrySize);
        Assert.Equal(4, result.HudAgentId);
        Assert.Equal(0x3650, result.QueueOffset);
        Assert.Equal(16, result.QueueCapacity);
        Assert.Equal(0xE8, result.QueueEntrySize);
        Assert.Equal(0, result.IsPendingOffset);
        Assert.Equal(0x2, result.StyleOffset);
        Assert.Equal(0x8, result.NameOffset);
        Assert.Equal(0x70, result.TextOffset);
        Assert.Equal(0xDC, result.ImageOffset);
        Assert.Equal(0xE0, result.SoundOffset);
        Assert.Equal(0xE4, result.EntityIdOffset);
    }

    [Theory]
    [InlineData("48 8b-1d ??", "488B1D??")]
    [InlineData("488B1D????????", "488B1D????????")]
    public void NormalizesPatterns(string input, string expected) {
        Assert.Equal(expected, FcsMetadataExtractor.NormalizePattern(input));
    }

    [Fact]
    public void CanonicalOutputIsByteIdenticalAndHasOneLfNewline() {
        HermesManifest manifest = CreateManifest();

        byte[] first = CanonicalJson.Serialize(manifest);
        byte[] second = CanonicalJson.Serialize(manifest);

        Assert.Equal(first, second);
        string json = Encoding.UTF8.GetString(first);
        Assert.DoesNotContain("\r", json);
        Assert.EndsWith("}\n", json);
        Assert.False(json.EndsWith("}\n\n", StringComparison.Ordinal));
        Assert.Equal(CanonicalJson.Revision(first), CanonicalJson.Revision(second));
    }

    [Fact]
    public void RepositoryFixturePassesSchema() {
        string root = FindRepositoryRoot();
        string manifest = Path.Combine(root, "v2", "fixtures", "manifest.valid.json");
        string schema = Path.Combine(root, "schemas", "hermes-v2.schema.json");

        ManifestValidator.Validate(File.ReadAllBytes(manifest), schema);
    }

    [Fact]
    public void SchemaRejectsMissingRequiredResource() {
        string root = FindRepositoryRoot();
        string schema = Path.Combine(root, "schemas", "hermes-v2.schema.json");
        JsonNode node = JsonNode.Parse(CanonicalJson.Serialize(CreateManifest()))!;
        node["resources"]!.AsObject().Remove("currentTalk");

        Assert.Throws<InvalidOperationException>(() => ManifestValidator.Validate(Encoding.UTF8.GetBytes(node.ToJsonString()), schema));
    }

    [Fact]
    public void SchemaRejectsLegacyStringLengthOffset() {
        string root = FindRepositoryRoot();
        string schema = Path.Combine(root, "schemas", "hermes-v2.schema.json");
        JsonNode node = JsonNode.Parse(CanonicalJson.Serialize(CreateManifest()))!;
        node["resources"]!["talk"]!["utf8String"]!["stringLengthOffset"] = 0x18;

        Assert.Throws<InvalidOperationException>(() => ManifestValidator.Validate(Encoding.UTF8.GetBytes(node.ToJsonString()), schema));
    }

    [Fact]
    public void OptionalBattleTalkPassesAndRejectsInvalidArraySemantics() {
        string root = FindRepositoryRoot();
        string schema = Path.Combine(root, "schemas", "hermes-v2.schema.json");
        HermesManifest baseline = CreateManifest();
        BattleTalkResource resource = new(
            "framework",
            "currentBattleTalk",
            0x2B68,
            0xD2670,
            0x13420,
            0x6900,
            new AtkUnitListLayout(0x8, 0x808, 256, 8),
            new AddonVisibilityLayout(0x8, 32, 0x198, 0x00200000, 0x1A1, 0x01),
            "_BattleTalk",
            0x1BA8,
            new AtkArrayDataHolderLayout(0, 0x18, 0x2, 0x30),
            new AtkArrayDataLayout(0x8, 0x1F),
            0x28,
            0x28,
            38,
            35,
            0,
            0,
            1,
            "visibilityOrContentGeneration");
        HermesManifest manifest = baseline with {
            Compatibility = new Compatibility("9.2.0", 1),
            Resources = baseline.Resources with { BattleTalk = resource },
        };

        byte[] bytes = CanonicalJson.Serialize(manifest);
        ManifestValidator.Validate(bytes, schema);

        JsonNode invalid = JsonNode.Parse(bytes)!;
        invalid["resources"]!["battleTalk"]!["textIndex"] = 0;
        Assert.Throws<InvalidOperationException>(
            () => ManifestValidator.Validate(Encoding.UTF8.GetBytes(invalid.ToJsonString()), schema));
    }

    [Fact]
    public void RepositoryCanonicalJsonFilesUseLfWithoutBom() {
        string root = FindRepositoryRoot();
        string[] paths = [
            Path.Combine(root, "schemas", "hermes-v2.schema.json"),
            Path.Combine(root, "v2", "fixtures", "manifest.valid.json"),
        ];

        foreach (string path in paths) {
            byte[] bytes = File.ReadAllBytes(path);
            Assert.False(bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble));
            Assert.DoesNotContain((byte)'\r', bytes);
            Assert.Equal((byte)'\n', bytes[^1]);
        }
    }

    private static HermesManifest CreateManifest() {
        return new HermesManifest(
            2,
            new Compatibility("9.1.2", 1),
            new Source(
                "https://github.com/aers/FFXIVClientStructs.git",
                new string('a', 40),
                "https://github.com/sappho192/ffxiv-hermes.git",
                new string('b', 40)),
            new Platform("ffxiv_dx11.exe", "x64"),
            new Roots(new FrameworkRoot("488B1D????????8B7C24", 3, true)),
            new Resources(
                new ChatLogResource("framework", 0x2B68, 0x1AC0, 0x48, 0x60),
                new TalkResource(
                    "framework",
                    "lastStandardTalk",
                    0x2B68,
                    0xFEF00,
                    0xFEF68,
                    new Utf8StringLayout(0, 0x10, "bufferUsedMinusNull")),
                new CurrentTalkResource(
                    "framework",
                    "currentStandardTalk",
                    0x2B68,
                    0xD2670,
                    0x13420,
                    0x6900,
                    new AtkUnitListLayout(0x8, 0x808, 256, 8),
                    new AtkUnitBaseLayout(0x8, 32, 0x198, 0x00200000, 0x1A1, 0x01, 0x178, 0x1E2),
                    new AtkValueLayout(0x10, 0, 0x8, [0x28]),
                    "Talk",
                    0,
                    1)),
            new Validation("candidate"));
    }

    private static string FindRepositoryRoot() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Hermes.V2.slnx"))) {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
