using System.Diagnostics.CodeAnalysis;

namespace Game.Runtime.Execution;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal enum InteractionButton : ushort
{
    Inspect = 0x0000,
    Use = 0x0001,
    OpenClose = 0x0002,
    GoTo = 0x0003,
    Read = 0x0004,
    Take = 0x0005,
    Write = 0x0006,
    Buy = 0x0007,
    SitStand = 0x0008,
    Save = 0x0009,
    With = 0x000A,
    ConfirmYes = 0x000B,
    ConfirmNo = 0x000C,
    Back = 0x000D,
    Empty = 0x000E,
    PreviousText = 0x000F,
    NextText = 0x0010,
    EmptySelection0 = 0x0011,
    EmptySelection1 = 0x0012,
    EmptySelection2 = 0x0013,
    EmptySelection3 = 0x0014,
    EmptySelection4 = 0x0015,
    EmptySelection5 = 0x0016
}
