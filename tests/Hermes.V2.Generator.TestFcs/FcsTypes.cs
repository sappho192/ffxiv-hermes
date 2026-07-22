using System.Runtime.InteropServices;

namespace InteropGenerator.Runtime.Attributes {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class StaticAddressAttribute(string signature, ushort relativeFollowOffset, bool isPointer = false) : Attribute {
        public string Signature { get; } = signature;
        public ushort[] RelativeFollowOffsets { get; } = [relativeFollowOffset];
        public bool IsPointer { get; } = isPointer;
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
    [StructLayout(LayoutKind.Explicit)]
    public struct UIModule {
        [FieldOffset(0x1AC0)] internal nint RaptureLogModule;
        [FieldOffset(0xFEF00)] public nint LastTalkName;
        [FieldOffset(0xFEF68)] public nint LastTalkText;
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
        [FieldOffset(0x18)] public long StringLength;
    }
}
