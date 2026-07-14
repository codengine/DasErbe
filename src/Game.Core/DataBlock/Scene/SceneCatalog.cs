using System.Diagnostics.CodeAnalysis;

namespace Game.DataBlock.Scene;

// Scene descriptor indices named from their current scene/asset identity.
// Ambiguous duplicates keep their hex suffix until stronger gameplay semantics are
// recovered.
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("Roslynator", "RCS1213:Remove unused member declaration")]
internal static class SceneCatalog
{
    private const int Empty00 = 0x00;
    private const int Lawyer01 = 0x01;
    private const int Lawyer02 = 0x02;
    internal const int House = 0x03;
    private const int LivingRoom = 0x04;
    private const int Garden = 0x05;
    private const int Garden06 = 0x06;
    private const int Kitchen = 0x07;
    private const int Basement = 0x08;
    internal const int Bedroom = 0x09;
    private const int SnackBar = 0x0A;
    private const int Shop1 = 0x0B;
    private const int Shop2 = 0x0C;
    private const int Goebel = 0x0D;
    private const int Inge = 0x0E;
    private const int Empty0F = 0x0F;
    private const int Car = 0x10;
}
