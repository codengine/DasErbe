using Game.DataBlock;
using Game.Runtime.Rooms.House;

namespace Game.Runtime.Bootstrapping;

/// <summary>
///     Seeds the runtime-owned data block from boot data.
/// </summary>
internal static class BootDataBlockSeeder
{
    private const ushort DataBlockSeedSourceOffset = 0x089C;

    /// <summary>
    ///     Copies the boot-seeded data block into the runtime state.
    /// </summary>
    /// <param name="runtime">Runtime whose state receives the seeded data.</param>
    internal static void SeedDataBlock(Erbe runtime)
    {
        if (runtime.BootData.DataBlockSeed.Length != DataBlockModel.BlockLength)
        {
            throw new InvalidOperationException(
                $"Boot data-block seed at DGROUP offset 0x{DataBlockSeedSourceOffset:X4} exceeds the executable image.");
        }

        // Ownership collapse note: the original caller passes DS:089C directly. The managed runtime keeps a runtime-owned
        // copy of that boot-seeded block so later mutations can persist through the same sub_11761 seam.
        runtime.State.Initialize(runtime.BootData.DataBlockSeed);
        HouseFriendlyManVisibilityState.Synchronize(runtime);
    }
}
