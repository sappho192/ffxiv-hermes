namespace Hermes.V2.Generator;

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;

internal static partial class FcsMetadataExtractor {
    private const string FrameworkType = "FFXIVClientStructs.FFXIV.Client.System.Framework.Framework";
    private const string UiModuleType = "FFXIVClientStructs.FFXIV.Client.UI.UIModule";
    private const string RaptureAtkModuleType = "FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkModule";
    private const string RaptureAtkUnitManagerType = "FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkUnitManager";
    private const string LogModuleType = "FFXIVClientStructs.FFXIV.Component.Log.LogModule";
    private const string Utf8StringType = "FFXIVClientStructs.FFXIV.Client.System.String.Utf8String";
    private const string AtkUnitManagerType = "FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitManager";
    private const string AtkUnitListType = "FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitList";
    private const string AtkUnitBaseType = "FFXIVClientStructs.FFXIV.Component.GUI.AtkUnitBase";
    private const string AtkValueType = "FFXIVClientStructs.FFXIV.Component.GUI.AtkValue";
    private const string AtkModuleType = "FFXIVClientStructs.FFXIV.Component.GUI.AtkModule";
    private const string AtkArrayDataHolderType = "FFXIVClientStructs.FFXIV.Component.GUI.AtkArrayDataHolder";
    private const string AtkArrayDataType = "FFXIVClientStructs.FFXIV.Component.GUI.AtkArrayData";
    private const string NumberArrayDataType = "FFXIVClientStructs.FFXIV.Component.GUI.NumberArrayData";
    private const string StringArrayDataType = "FFXIVClientStructs.FFXIV.Component.GUI.StringArrayData";
    private const string NumberArrayType = "FFXIVClientStructs.FFXIV.Component.GUI.NumberArrayType";
    private const string StringArrayType = "FFXIVClientStructs.FFXIV.Component.GUI.StringArrayType";
    private const string AgentModuleType = "FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentModule";
    private const string AgentHudType = "FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentHUD";
    private const string AgentIdType = "FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentId";
    private const string HudQueuedBattleTalkType = "FFXIVClientStructs.FFXIV.Client.UI.Agent.HudQueuedBattleTalk";
    private const string StaticAddressAttributeType = "InteropGenerator.Runtime.Attributes.StaticAddressAttribute";
    private const string BitFieldAttributeType = "InteropGenerator.Runtime.Attributes.BitFieldAttribute`1";

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
        Type raptureAtkModule = RequiredType(assembly, RaptureAtkModuleType);
        Type raptureAtkUnitManager = RequiredType(assembly, RaptureAtkUnitManagerType);
        Type logModule = RequiredType(assembly, LogModuleType);
        Type utf8String = RequiredType(assembly, Utf8StringType);
        Type atkUnitManager = RequiredType(assembly, AtkUnitManagerType);
        Type atkUnitList = RequiredType(assembly, AtkUnitListType);
        Type atkUnitBase = RequiredType(assembly, AtkUnitBaseType);
        Type atkValue = RequiredType(assembly, AtkValueType);

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

        if (IntPtr.Size != 8) {
            throw new PlatformNotSupportedException("Hermes v2 metadata generation requires an x64 process.");
        }

        int inheritedAtkUnitManagerOffset = GetFieldOffset(raptureAtkUnitManager, "AtkUnitManager");
        if (inheritedAtkUnitManagerOffset != 0) {
            throw new InvalidOperationException("RaptureAtkUnitManager must inherit AtkUnitManager at offset zero.");
        }

        FieldInfo entries = RequiredField(atkUnitList, "_entries");
        int entryCapacity = GetFixedSizeArrayCapacity(entries.FieldType);
        FieldInfo addonName = RequiredField(atkUnitBase, "_name");
        int addonNameCapacity = GetFixedSizeArrayCapacity(addonName.FieldType);

        FieldInfo atkValueType = RequiredField(atkValue, "Type");
        int managedStringType = Convert.ToInt32(Enum.Parse(atkValueType.FieldType, "ManagedString"));

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
            GetFieldOffset(uiModule, "RaptureAtkModule"),
            GetFieldOffset(raptureAtkModule, "RaptureAtkUnitManager"),
            GetFieldOffset(atkUnitManager, "AllLoadedUnitsList"),
            GetFieldOffset(entries),
            GetFieldOffset(atkUnitList, "Count"),
            entryCapacity,
            IntPtr.Size,
            GetFieldOffset(addonName),
            addonNameCapacity,
            GetFieldOffset(atkUnitBase, "Flags198"),
            GetBitMask(atkUnitBase, "Flags198", "VisibilityState", "IsVisible"),
            GetFieldOffset(atkUnitBase, "Flags1A1"),
            GetBitMask(atkUnitBase, "Flags1A1", "IsReady"),
            GetFieldOffset(atkUnitBase, "AtkValues"),
            GetFieldOffset(atkUnitBase, "AtkValuesCount"),
            GetStructSize(atkValue),
            GetFieldOffset(atkValueType),
            GetFieldOffset(atkValue, "String"),
            managedStringType);
    }

    internal static BattleTalkProbeMetadata ExtractBattleTalkProbe(Assembly assembly) {
        Type raptureAtkModule = RequiredType(assembly, RaptureAtkModuleType);
        Type atkModule = RequiredType(assembly, AtkModuleType);
        Type holder = RequiredType(assembly, AtkArrayDataHolderType);
        Type arrayData = RequiredType(assembly, AtkArrayDataType);
        Type numberArrayData = RequiredType(assembly, NumberArrayDataType);
        Type stringArrayData = RequiredType(assembly, StringArrayDataType);
        Type numberArrayType = RequiredType(assembly, NumberArrayType);
        Type stringArrayType = RequiredType(assembly, StringArrayType);
        Type agentModule = RequiredType(assembly, AgentModuleType);
        Type agentHud = RequiredType(assembly, AgentHudType);
        Type agentId = RequiredType(assembly, AgentIdType);
        Type queuedBattleTalk = RequiredType(assembly, HudQueuedBattleTalkType);

        FieldInfo agents = RequiredField(agentModule, "_agents");
        FieldInfo queue = RequiredField(agentHud, "_queuedBattleTalks");
        return new BattleTalkProbeMetadata(
            GetFieldOffset(atkModule, "AtkArrayDataHolder"),
            GetFieldOffset(holder, "NumberArrayCount"),
            GetFieldOffset(holder, "NumberArrays"),
            GetFieldOffset(holder, "StringArrayCount"),
            GetFieldOffset(holder, "StringArrays"),
            GetFieldOffset(arrayData, "Size"),
            GetFieldOffset(arrayData, "UpdateState"),
            GetFieldOffset(numberArrayData, "IntArray"),
            GetFieldOffset(stringArrayData, "StringArray"),
            GetFieldOffset(stringArrayData, "ManagedStringArray"),
            Convert.ToInt32(Enum.Parse(numberArrayType, "BattleTalk")),
            Convert.ToInt32(Enum.Parse(stringArrayType, "BattleTalk")),
            GetFieldOffset(raptureAtkModule, "AgentModule"),
            GetFieldOffset(agents),
            GetFixedSizeArrayCapacity(agents.FieldType),
            IntPtr.Size,
            Convert.ToInt32(Enum.Parse(agentId, "Hud")),
            GetFieldOffset(queue),
            GetFixedSizeArrayCapacity(queue.FieldType),
            GetStructSize(queuedBattleTalk),
            GetFieldOffset(queuedBattleTalk, "IsPending"),
            GetFieldOffset(queuedBattleTalk, "Style"),
            GetFieldOffset(queuedBattleTalk, "Name"),
            GetFieldOffset(queuedBattleTalk, "Text"),
            GetFieldOffset(queuedBattleTalk, "Image"),
            GetFieldOffset(queuedBattleTalk, "Sound"),
            GetFieldOffset(queuedBattleTalk, "EntityId"));
    }

    internal static BattleTalkProbeMetadata ExtractBattleTalkProbe(string assemblyPath) {
        string fullPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullPath)) {
            throw new FileNotFoundException("The FCS assembly was not found. Build FFXIVClientStructs first.", fullPath);
        }

        string assemblyDirectory = Path.GetDirectoryName(fullPath)!;
        AssemblyLoadContext.Default.Resolving += ResolveDependency;
        try {
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
            return ExtractBattleTalkProbe(assembly);
        }
        finally {
            AssemblyLoadContext.Default.Resolving -= ResolveDependency;
        }

        Assembly? ResolveDependency(AssemblyLoadContext context, AssemblyName name) {
            string dependency = Path.Combine(assemblyDirectory, name.Name + ".dll");
            return File.Exists(dependency) ? context.LoadFromAssemblyPath(dependency) : null;
        }
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
        return GetFieldOffset(RequiredField(type, fieldName));
    }

    private static FieldInfo RequiredField(Type type, string fieldName) {
        return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
               ?? throw new MissingFieldException(type.FullName, fieldName);
    }

    private static int GetFieldOffset(FieldInfo field) {
        FieldOffsetAttribute offset = field.GetCustomAttribute<FieldOffsetAttribute>()
                                      ?? throw new InvalidOperationException($"{field.DeclaringType?.FullName}.{field.Name} has no FieldOffsetAttribute.");
        return offset.Value;
    }

    private static int GetStructSize(Type type) {
        int explicitSize = type.StructLayoutAttribute?.Size ?? 0;
        return explicitSize > 0 ? explicitSize : Marshal.SizeOf(type);
    }

    private static int GetFixedSizeArrayCapacity(Type type) {
        Match match = FixedSizeArrayTypeRegex().Match(type.Name);
        return match.Success && int.TryParse(match.Groups[1].Value, out int capacity) && capacity > 0
            ? capacity
            : throw new InvalidOperationException($"{type.FullName} is not a recognized FCS fixed-size array type.");
    }

    private static uint GetBitMask(Type type, string fieldName, string bitFieldName, string? enumMember = null) {
        FieldInfo field = RequiredField(type, fieldName);
        object attribute = field.GetCustomAttributes(inherit: false)
            .SingleOrDefault(candidate =>
                candidate.GetType().IsGenericType
                && candidate.GetType().GetGenericTypeDefinition().FullName == BitFieldAttributeType
                && ReadProperty<string>(candidate, "Name") == bitFieldName)
            ?? throw new InvalidOperationException($"{type.FullName}.{fieldName} has no bit field named {bitFieldName}.");

        int index = ReadProperty<int>(attribute, "Index");
        int length = ReadProperty<int>(attribute, "Length");
        ulong value = enumMember == null
            ? (1UL << length) - 1
            : Convert.ToUInt64(Enum.Parse(attribute.GetType().GetGenericArguments()[0], enumMember));
        if (index < 0 || length <= 0 || index + length > 32 || value >= (1UL << length)) {
            throw new InvalidOperationException($"{type.FullName}.{fieldName}.{bitFieldName} has an invalid bit field layout.");
        }

        return checked((uint)(value << index));
    }

    private static T ReadProperty<T>(object instance, string propertyName) {
        object? value = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);
        return value is T typed
            ? typed
            : throw new InvalidOperationException($"{instance.GetType().FullName}.{propertyName} is missing or invalid.");
    }

    [GeneratedRegex("^(?:[0-9A-F]{2}|\\?\\?)+$", RegexOptions.CultureInvariant)]
    private static partial Regex PatternRegex();

    [GeneratedRegex("^FixedSizeArray([1-9][0-9]*)", RegexOptions.CultureInvariant)]
    private static partial Regex FixedSizeArrayTypeRegex();
}
