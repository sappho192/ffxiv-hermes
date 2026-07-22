namespace Hermes.V2.Generator;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class CanonicalJson {
    private static readonly JsonSerializerOptions Options = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static byte[] Serialize(HermesManifest manifest) {
        string json = JsonSerializer.Serialize(manifest, Options).Replace("\r\n", "\n", StringComparison.Ordinal);
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json + "\n");
    }

    internal static string Revision(ReadOnlySpan<byte> bytes) {
        return "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
