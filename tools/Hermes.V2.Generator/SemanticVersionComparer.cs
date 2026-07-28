namespace Hermes.V2.Generator;

using System.Numerics;

internal static class SemanticVersionComparer {
    internal static int Compare(string left, string right) {
        ParsedVersion leftVersion = Parse(left);
        ParsedVersion rightVersion = Parse(right);
        int result = leftVersion.Major.CompareTo(rightVersion.Major);
        if (result != 0) return result;
        result = leftVersion.Minor.CompareTo(rightVersion.Minor);
        if (result != 0) return result;
        result = leftVersion.Patch.CompareTo(rightVersion.Patch);
        if (result != 0) return result;
        if (leftVersion.Prerelease.Length == 0) {
            return rightVersion.Prerelease.Length == 0 ? 0 : 1;
        }
        if (rightVersion.Prerelease.Length == 0) return -1;

        int count = Math.Min(leftVersion.Prerelease.Length, rightVersion.Prerelease.Length);
        for (int index = 0; index < count; index++) {
            string leftPart = leftVersion.Prerelease[index];
            string rightPart = rightVersion.Prerelease[index];
            bool leftNumeric = BigInteger.TryParse(leftPart, out BigInteger leftNumber);
            bool rightNumeric = BigInteger.TryParse(rightPart, out BigInteger rightNumber);
            if (leftNumeric && rightNumeric) result = leftNumber.CompareTo(rightNumber);
            else if (leftNumeric != rightNumeric) result = leftNumeric ? -1 : 1;
            else result = string.CompareOrdinal(leftPart, rightPart);
            if (result != 0) return result;
        }

        return leftVersion.Prerelease.Length.CompareTo(rightVersion.Prerelease.Length);
    }

    private static ParsedVersion Parse(string value) {
        string withoutBuild = value.Split('+', 2)[0];
        string[] releaseParts = withoutBuild.Split(['-'], 2);
        string[] numbers = releaseParts[0].Split('.');
        if (numbers.Length != 3
            || !BigInteger.TryParse(numbers[0], out BigInteger major)
            || !BigInteger.TryParse(numbers[1], out BigInteger minor)
            || !BigInteger.TryParse(numbers[2], out BigInteger patch)) {
            throw new ArgumentException($"Invalid semantic version: {value}");
        }

        string[] prerelease =
            releaseParts.Length == 2 ? releaseParts[1].Split('.') : Array.Empty<string>();
        return new ParsedVersion(major, minor, patch, prerelease);
    }

    private sealed record ParsedVersion(
        BigInteger Major,
        BigInteger Minor,
        BigInteger Patch,
        string[] Prerelease);
}
