using System.Runtime.InteropServices;

namespace InteropGenerator.Runtime.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StaticAddressAttribute(string signature, ushort relativeFollowOffset, bool isPointer = false) : Attribute {
        public string Signature { get; } = signature;
        public ushort[] RelativeFollowOffsets { get; } = [relativeFollowOffset];
        public bool IsPointer { get; } = isPointer;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class BitFieldAttribute<T>(string name, int index, int length = 1) : Attribute {
        public string Name { get; } = name;
        public int Index { get; } = index;
        public int Length { get; } = length;
    }
}

namespace FFXIVClientStructs.FFXIV.Client.System.Framework {
    using InteropGenerator.Runtime.Attributes;

    [StructLayout(LayoutKind.Explicit)]
    public struct Framework {
        [FieldOffset(0x2B68)] public nint UIModule;

        [StaticAddress("48 8b 1d ?? ?? ?? ?? 8b 7c 24", 3, isPointer: true)]
        public static unsafe Framework* Instance() => null;
    }
}

namespace FFXIVClientStructs.FFXIV.Client.UI {
    using FFXIVClientStructs.FFXIV.Component.GUI;

    [StructLayout(LayoutKind.Explicit)]
    public struct UIModule {
        [FieldOffset(0x1AC0)] internal nint RaptureLogModule;
        [FieldOffset(0xD2670)] internal RaptureAtkModule RaptureAtkModule;
        [FieldOffset(0xFEF00)] public nint LastTalkName;
        [FieldOffset(0xFEF68)] public nint LastTalkText;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RaptureAtkModule {
        [FieldOffset(0)] public AtkModule AtkModule;
        [FieldOffset(0x12400)] public Agent.AgentModule AgentModule;
        [FieldOffset(0x13420)] public RaptureAtkUnitManager RaptureAtkUnitManager;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct RaptureAtkUnitManager {
        [FieldOffset(0)] public AtkUnitManager AtkUnitManager;
    }

}

namespace FFXIVClientStructs.FFXIV.Client.UI.Agent {
    using FFXIVClientStructs.FFXIV.Client.System.String;

    public enum AgentId : uint {
        Hud = 4,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xFE8)]
    internal struct FixedSizeArray509Pointers {
        [FieldOffset(0)] public nint First;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xE80)]
    internal struct FixedSizeArray16HudQueuedBattleTalk {
        [FieldOffset(0)] public HudQueuedBattleTalk First;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AgentModule {
        [FieldOffset(0x20)] internal FixedSizeArray509Pointers _agents;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AgentHUD {
        [FieldOffset(0x3650)] internal FixedSizeArray16HudQueuedBattleTalk _queuedBattleTalks;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0xE8)]
    public struct HudQueuedBattleTalk {
        [FieldOffset(0)] public bool IsPending;
        [FieldOffset(0x2)] public byte Style;
        [FieldOffset(0x8)] public Utf8String Name;
        [FieldOffset(0x70)] public Utf8String Text;
        [FieldOffset(0xDC)] public uint Image;
        [FieldOffset(0xE0)] public int Sound;
        [FieldOffset(0xE4)] public uint EntityId;
    }
}

namespace FFXIVClientStructs.FFXIV.Component.Log {
    [StructLayout(LayoutKind.Explicit)]
    public struct LogModule {
        [FieldOffset(0x48)] public nint LogMessageIndex;
        [FieldOffset(0x60)] public nint LogMessageData;
    }
}

namespace FFXIVClientStructs.FFXIV.Client.System.String {
    [StructLayout(LayoutKind.Explicit)]
    public struct Utf8String {
        [FieldOffset(0x00)] public nint StringPtr;
        [FieldOffset(0x10)] public long BufUsed;
    }
}

namespace FFXIVClientStructs.FFXIV.Component.GUI {
    using InteropGenerator.Runtime.Attributes;

    public enum AtkUnitBaseVisibilityState : byte {
        None = 0,
        IsVisible = 1 << 1,
    }

    public enum AtkValueType {
        Undefined = 0,
        String = 0x8,
        Managed = 0x20,
        ManagedString = Managed | String,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x800)]
    internal struct FixedSizeArray256Pointers {
        [FieldOffset(0)] public nint First;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x20)]
    internal struct FixedSizeArray32Bytes {
        [FieldOffset(0)] public byte First;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AtkUnitManager {
        [FieldOffset(0x6900)] public AtkUnitList AllLoadedUnitsList;
    }

    public enum NumberArrayType {
        BattleTalk = 38,
    }

    public enum StringArrayType {
        BattleTalk = 35,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AtkModule {
        [FieldOffset(0x1BA8)] public AtkArrayDataHolder AtkArrayDataHolder;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AtkArrayDataHolder {
        [FieldOffset(0)] public short NumberArrayCount;
        [FieldOffset(0x2)] public short StringArrayCount;
        [FieldOffset(0x18)] public nint NumberArrays;
        [FieldOffset(0x30)] public nint StringArrays;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct AtkArrayData {
        [FieldOffset(0x8)] public int Size;
        [FieldOffset(0x1F)] public byte UpdateState;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct NumberArrayData {
        [FieldOffset(0)] public AtkArrayData AtkArrayData;
        [FieldOffset(0x28)] public nint IntArray;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct StringArrayData {
        [FieldOffset(0)] public AtkArrayData AtkArrayData;
        [FieldOffset(0x28)] public nint StringArray;
        [FieldOffset(0x30)] public nint ManagedStringArray;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x810)]
    public struct AtkUnitList {
        [FieldOffset(0x8)] internal FixedSizeArray256Pointers _entries;
        [FieldOffset(0x808)] public ushort Count;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x238)]
    public struct AtkUnitBase {
        [FieldOffset(0x8)] internal FixedSizeArray32Bytes _name;
        [FieldOffset(0x178)] public nint AtkValues;

        [BitField<AtkUnitBaseVisibilityState>("VisibilityState", 20, 4)]
        [FieldOffset(0x198)]
        public uint Flags198;

        [BitField<bool>("IsReady", 0)]
        [FieldOffset(0x1A1)]
        public byte Flags1A1;

        [FieldOffset(0x1E2)] public ushort AtkValuesCount;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x10)]
    public struct AtkValue {
        [FieldOffset(0)] public AtkValueType Type;
        [FieldOffset(0x8)] public nint String;
    }
}
