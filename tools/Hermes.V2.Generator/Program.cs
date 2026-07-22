namespace Hermes.V2.Generator;

using System.Diagnostics;
using System.Text.RegularExpressions;

internal static partial class Program {
    private const string FcsRepository = "https://github.com/aers/FFXIVClientStructs.git";
    private const string GeneratorRepository = "https://github.com/sappho192/ffxiv-hermes.git";

    private static int Main(string[] args) {
        try {
            if (args.Length == 0) {
                throw new ArgumentException("Expected command: generate, validate, or revision.");
            }

            Dictionary<string, string> options = ParseOptions(args.Skip(1));
            return args[0] switch {
                "generate" => Generate(options),
                "validate" => Validate(options),
                "revision" => Revision(options),
                _ => throw new ArgumentException($"Unknown command: {args[0]}")
            };
        }
        catch (Exception exception) {
            Console.Error.WriteLine($"error: {exception.Message}");
            return 1;
        }
    }

    private static int Generate(IReadOnlyDictionary<string, string> options) {
        string repositoryRoot = FullPath(Required(options, "repository-root"));
        string fcsDirectory = FullPath(Required(options, "fcs-directory"));
        string fcsCommit = RequiredSha(options, "fcs-commit");
        string generatorCommit = RequiredSha(options, "generator-commit");
        string outputPath = FullPath(Required(options, "output"));
        string schemaPath = FullPath(options.GetValueOrDefault("schema", Path.Combine(repositoryRoot, "schemas", "hermes-v2.schema.json")));
        string assemblyPath = FullPath(options.GetValueOrDefault(
            "fcs-assembly",
            Path.Combine(fcsDirectory, "bin", "Release", "FFXIVClientStructs.dll")));

        string checkoutCommit = Git(fcsDirectory, "rev-parse", "HEAD");
        if (!string.Equals(fcsCommit, checkoutCommit, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"FCS checkout is {checkoutCommit}, expected {fcsCommit}.");
        }

        string generatorCheckoutCommit = Git(repositoryRoot, "rev-parse", "HEAD");
        if (!string.Equals(generatorCommit, generatorCheckoutCommit, StringComparison.Ordinal)) {
            throw new InvalidOperationException($"Generator checkout is {generatorCheckoutCommit}, expected {generatorCommit}.");
        }

        ExtractedMetadata metadata = FcsMetadataExtractor.Extract(assemblyPath);
        Validation validation = CreateValidation(options);
        HermesManifest manifest = new(
            2,
            new Compatibility(Required(options, "minimum-sharlayan-version"), 1),
            new Source(FcsRepository, fcsCommit, GeneratorRepository, generatorCommit),
            new Platform("ffxiv_dx11.exe", "x64"),
            new Roots(new FrameworkRoot(metadata.Pattern, metadata.RelativeFollowOffset, metadata.IsPointer)),
            new Resources(
                new ChatLogResource("framework", metadata.UiModuleOffset, metadata.RaptureLogModuleOffset, metadata.IndexVectorOffset, metadata.DataVectorOffset),
                new TalkResource(
                    "framework",
                    "lastStandardTalk",
                    metadata.UiModuleOffset,
                    metadata.TalkNameOffset,
                    metadata.TalkTextOffset,
                    new Utf8StringLayout(metadata.StringPointerOffset, metadata.BufferUsedOffset, metadata.StringLengthOffset))),
            validation);

        byte[] bytes = CanonicalJson.Serialize(manifest);
        ManifestValidator.Validate(bytes, schemaPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, bytes);

        string revision = CanonicalJson.Revision(bytes);
        string summary = ManifestDiff.Create(bytes, options.GetValueOrDefault("diff-against"), revision);
        if (options.TryGetValue("summary", out string? summaryPath)) {
            string fullSummaryPath = FullPath(summaryPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullSummaryPath)!);
            File.WriteAllText(fullSummaryPath, summary.Replace("\r\n", "\n", StringComparison.Ordinal));
        }

        Console.WriteLine(revision);
        return 0;
    }

    private static int Validate(IReadOnlyDictionary<string, string> options) {
        string manifestPath = FullPath(Required(options, "manifest"));
        string schemaPath = FullPath(Required(options, "schema"));
        byte[] bytes = File.ReadAllBytes(manifestPath);
        ManifestValidator.Validate(bytes, schemaPath);
        Console.WriteLine(CanonicalJson.Revision(bytes));
        return 0;
    }

    private static int Revision(IReadOnlyDictionary<string, string> options) {
        Console.WriteLine(CanonicalJson.Revision(File.ReadAllBytes(FullPath(Required(options, "manifest")))));
        return 0;
    }

    private static Validation CreateValidation(IReadOnlyDictionary<string, string> options) {
        string status = Required(options, "validation-status");
        return status switch {
            "candidate" => new Validation(status),
            "live-verified" => new Validation(
                status,
                Required(options, "game-version"),
                RequiredHex(options, "executable-sha256", 64),
                RequiredSha(options, "verifier-commit")),
            _ => throw new ArgumentException("validation-status must be candidate or live-verified.")
        };
    }

    private static Dictionary<string, string> ParseOptions(IEnumerable<string> arguments) {
        string[] values = arguments.ToArray();
        Dictionary<string, string> options = new(StringComparer.Ordinal);
        for (int index = 0; index < values.Length; index += 2) {
            if (!values[index].StartsWith("--", StringComparison.Ordinal) || index + 1 >= values.Length) {
                throw new ArgumentException($"Invalid option near '{values[index]}'. Options use --name value.");
            }

            options.Add(values[index][2..], values[index + 1]);
        }

        return options;
    }

    private static string Required(IReadOnlyDictionary<string, string> options, string key) {
        return options.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required option --{key}.");
    }

    private static string RequiredSha(IReadOnlyDictionary<string, string> options, string key) {
        return RequiredHex(options, key, 40);
    }

    private static string RequiredHex(IReadOnlyDictionary<string, string> options, string key, int length) {
        string value = Required(options, key);
        if (value.Length != length || !LowerHexRegex().IsMatch(value)) {
            throw new ArgumentException($"--{key} must be {length} lowercase hexadecimal characters.");
        }

        return value;
    }

    private static string Git(string repository, params string[] arguments) {
        ProcessStartInfo startInfo = new("git") { RedirectStandardOutput = true, RedirectStandardError = true };
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add($"safe.directory={repository.Replace('\\', '/')}");
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(repository);
        foreach (string argument in arguments) {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start git.");
        string output = process.StandardOutput.ReadToEnd().Trim();
        string error = process.StandardError.ReadToEnd().Trim();
        process.WaitForExit();
        return process.ExitCode == 0 ? output : throw new InvalidOperationException($"git failed: {error}");
    }

    private static string FullPath(string path) => Path.GetFullPath(path);

    [GeneratedRegex("^[0-9a-f]+$", RegexOptions.CultureInvariant)]
    private static partial Regex LowerHexRegex();
}
