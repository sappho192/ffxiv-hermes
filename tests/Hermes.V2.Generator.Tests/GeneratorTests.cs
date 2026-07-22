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
        Assert.Equal(0x18, result.StringLengthOffset);
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
        node["resources"]!.AsObject().Remove("talk");

        Assert.Throws<InvalidOperationException>(() => ManifestValidator.Validate(Encoding.UTF8.GetBytes(node.ToJsonString()), schema));
    }

    private static HermesManifest CreateManifest() {
        return new HermesManifest(
            2,
            new Compatibility("8.1.0", 1),
            new Source(
                "https://github.com/aers/FFXIVClientStructs.git",
                new string('a', 40),
                "https://github.com/sappho192/ffxiv-hermes.git",
                new string('b', 40)),
            new Platform("ffxiv_dx11.exe", "x64"),
            new Roots(new FrameworkRoot("488B1D????????8B7C24", 3, true)),
            new Resources(
                new ChatLogResource("framework", 0x2B68, 0x1AC0, 0x48, 0x60),
                new TalkResource("framework", "lastStandardTalk", 0x2B68, 0xFEF00, 0xFEF68, new Utf8StringLayout(0, 0x10, 0x18))),
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
