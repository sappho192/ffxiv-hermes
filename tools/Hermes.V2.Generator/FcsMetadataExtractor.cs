namespace Hermes.V2.Generator;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

internal static partial class FcsMetadataExtractor {
    private const string FrameworkType = "FFXIVClientStructs.FFXIV.Client.System.Framework.Framework";
    private const string UiModuleType = "FFXIVClientStructs.FFXIV.Client.UI.UIModule";
    private const string LogModuleType = "FFXIVClientStructs.FFXIV.Component.Log.LogModule";
    private const string Utf8StringType = "FFXIVClientStructs.FFXIV.Client.System.String.Utf8String";
    private const string StaticAddressAttributeType = "InteropGenerator.Runtime.Attributes.StaticAddressAttribute";

    internal static ExtractedMetadata Extract(string assemblyPath) {
        string fullPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException("The FCS assembly was not found. Build FFXIVClientStructs first.", fullPath);
        }

        string assemblyDirectory = Path.GetDirectoryName(fullPath)!;
        AssemblyLoadContext.Default.Resolving += ResolveDependency;
        try {
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
            return Extract(assembly);
        }
        finally {
            AssemblyLoadContext.Default.Resolving -= ResolveDependency;
        }

        Assembly? ResolveDependency(AssemblyLoadContext context, AssemblyName name) {
            string dependency = Path.Combine(assemblyDirectory, name.Name + ".dll");
            return File.Exists(dependency) ? context.LoadFromAssemblyPath(dependency) : null;
        }
    }

    internal static ExtractedMetadata Extract(Assembly assembly) {
        Type framework = RequiredType(assembly, FrameworkType);
        Type uiModule = RequiredType(assembly, UiModuleType);
        Type logModule = RequiredType(assembly, LogModuleType);
        Type utf8String = RequiredType(assembly, Utf8StringType);

        MethodInfo instance = framework.GetMethod("Instance", BindingFlags.Public | BindingFlags.Static)
                              ?? throw new MissingMethodException(FrameworkType, "Instance");
        object staticAddress = instance.GetCustomAttributes(inherit: false)
            .SingleOrDefault(attribute => attribute.GetType().FullName == StaticAddressAttributeType)
            ?? throw new InvalidOperationException("Framework.Instance has no StaticAddressAttribute.");

        string signature = ReadProperty<string>(staticAddress, "Signature");
        Array offsets = ReadProperty<Array>(staticAddress, "RelativeFollowOffsets");
        if (offsets.Length != 1) {
            throw new InvalidOperationException("Framework.Instance must have exactly one relative follow offset.");
        }

        int relativeFollowOffset = Convert.ToInt32(offsets.GetValue(0));
        bool isPointer = ReadProperty<bool>(staticAddress, "IsPointer");
        string pattern = NormalizePattern(signature);
        if (relativeFollowOffset > pattern.Length / 2) {
            throw new InvalidOperationException("Framework relative follow offset exceeds the signature length.");
        }

        return new ExtractedMetadata(
            pattern,
            relativeFollowOffset,
            isPointer,
            GetFieldOffset(framework, "UIModule"),
            GetFieldOffset(uiModule, "RaptureLogModule"),
            GetFieldOffset(logModule, "LogMessageIndex"),
            GetFieldOffset(logModule, "LogMessageData"),
            GetFieldOffset(uiModule, "LastTalkName"),
            GetFieldOffset(uiModule, "LastTalkText"),
            GetFieldOffset(utf8String, "StringPtr"),
            GetFieldOffset(utf8String, "BufUsed"),
            GetFieldOffset(utf8String, "StringLength"));
    }

    internal static string NormalizePattern(string value) {
        string normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
        if (!PatternRegex().IsMatch(normalized)) {
            throw new InvalidOperationException($"Invalid FCS signature pattern: {value}");
        }

        return normalized;
    }

    private static Type RequiredType(Assembly assembly, string name) {
        return assembly.GetType(name, throwOnError: false)
               ?? throw new TypeLoadException($"Required FCS type was not found: {name}");
    }

    private static int GetFieldOffset(Type type, string fieldName) {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                          ?? throw new MissingFieldException(type.FullName, fieldName);
        FieldOffsetAttribute offset = field.GetCustomAttribute<FieldOffsetAttribute>()
                                      ?? throw new InvalidOperationException($"{type.FullName}.{fieldName} has no FieldOffsetAttribute.");
        return offset.Value;
    }

    private static T ReadProperty<T>(object instance, string propertyName) {
        object? value = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"{instance.GetType().FullName}.{propertyName} is missing or invalid.");
    }

    [GeneratedRegex("^(?:[0-9A-F]{2}|\\?\\?)+$", RegexOptions.CultureInvariant)]
    private static partial Regex PatternRegex();
}
