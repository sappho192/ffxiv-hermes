namespace Hermes.V2.Generator;

using System.Text.Json.Nodes;

internal enum PublicationAction {
    Noop,
    Generate,
    RecordOnly,
    Publish,
    Reconcile,
}

internal static class PublicationPlanner {
    internal static PublicationAction BeforeGeneration(
        string fcsCommit,
        string auditFcsCommit,
        string latestFcsCommit,
        bool forceRegenerate) {
        if (forceRegenerate) {
            return PublicationAction.Generate;
        }

        if (string.Equals(fcsCommit, auditFcsCommit, StringComparison.Ordinal)) {
            return PublicationAction.Noop;
        }

        return string.Equals(fcsCommit, latestFcsCommit, StringComparison.Ordinal)
            ? PublicationAction.Reconcile
            : PublicationAction.Generate;
    }

    internal static PublicationAction AfterGeneration(byte[] currentBytes, byte[] generatedBytes) {
        JsonNode current = JsonNode.Parse(currentBytes)
            ?? throw new InvalidOperationException("Current manifest is empty.");
        JsonNode generated = JsonNode.Parse(generatedBytes)
            ?? throw new InvalidOperationException("Generated manifest is empty.");

        bool unchanged = JsonNode.DeepEquals(current["roots"], generated["roots"])
            && JsonNode.DeepEquals(current["resources"], generated["resources"]);
        return unchanged ? PublicationAction.RecordOnly : PublicationAction.Publish;
    }

    internal static string WorkflowValue(PublicationAction action) => action switch {
        PublicationAction.Noop => "noop",
        PublicationAction.Generate => "generate",
        PublicationAction.RecordOnly => "record-only",
        PublicationAction.Publish => "publish",
        PublicationAction.Reconcile => "reconcile",
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    internal static void RequireIdenticalImmutableBytes(byte[] existing, byte[] candidate) {
        if (!existing.AsSpan().SequenceEqual(candidate)) {
            throw new InvalidOperationException(
                "Existing immutable audit record has different bytes.");
        }
    }
}
