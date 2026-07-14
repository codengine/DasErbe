using System.Reflection;
using Game.Catalogs;
using Game.DataBlock;
using Game.DataBlock.Scene;
using Game.Shared.Executable;
using Game.Shared.Resources.Management;
using Game.Shared.Text;
using Game.Text;

namespace Game.Runtime.Bootstrapping;

/// <summary>
///     Loads the EXE once and projects the runtime-owned boot data snapshot.
/// </summary>
internal static class GameBootDataLoader
{
    /// <summary>
    ///     Loads all executable-derived runtime boot data for the supplied runtime.
    /// </summary>
    /// <param name="resources">Resource manager whose executable image provides the boot data.</param>
    /// <param name="exePath">Canonical path of the EXE used for boot-data loading.</param>
    internal static GameBootData Load(GameResourceManager resources, string exePath)
    {
        var image = resources.GetExecutableImage(exePath);
        var sourceOffsetsByStringId = LoadSourceOffsets();
        var stringIdsBySourceOffset = BuildOffsetIndex(sourceOffsetsByStringId);
        var dataBlockSeed = image.ReadBytesByDgroupOffset(0x089C, DataBlockModel.BlockLength, "Boot data-block seed");

        var originalStrings = LoadOriginalStrings(image, sourceOffsetsByStringId);
        var selectionAnimationFrameScript =
            image.ReadBytesByDgroupOffset(0x55C0, 92, "Selection animation frame-script table");
        var backdropDescriptorSelections = image.ReadWordTable(0x0092, 48, "Backdrop descriptor-selection table");
        var backdropSourceColumns = image.ReadWordTable(0x00F2, 24, "Backdrop source-column table");
        var backdropSourceRows = image.ReadWordTable(0x0122, 24, "Backdrop source-row table");
        var backdropBaseWidths = image.ReadWordTable(0x0152, 24, "Backdrop base-width table");
        var wordRunStreamsBySourceOffset = ReadWordRunStreams(image, dataBlockSeed);
        var carTrafficJamFrameTables = new CarTrafficJamFrameTables(
            image.ReadWordTable(0x5624, 4, "Traffic-jam source-column table"),
            image.ReadWordTable(0x562C, 4, "Traffic-jam source-row table"),
            image.ReadWordTable(0x5634, 4, "Traffic-jam frame-width table"),
            image.ReadWordTable(0x563C, 4, "Traffic-jam frame-height table"));
        return new GameBootData(originalStrings,
            sourceOffsetsByStringId,
            stringIdsBySourceOffset,
            dataBlockSeed,
            ReadInteractionHandlers(image),
            selectionAnimationFrameScript,
            backdropDescriptorSelections,
            backdropSourceColumns,
            backdropSourceRows,
            backdropBaseWidths,
            wordRunStreamsBySourceOffset,
            carTrafficJamFrameTables);
    }

    private static Dictionary<StringId, ushort> LoadSourceOffsets()
    {
        var fields = typeof(StringId).GetFields(BindingFlags.Public | BindingFlags.Static);
        var offsets = new Dictionary<StringId, ushort>(fields.Length);
        foreach (var field in fields)
        {
            var id = (StringId)field.GetValue(null)!;
            if (id == StringId.None)
            {
                continue;
            }

            var attribute = field.GetCustomAttribute<StringSourceOffsetAttribute>() ??
                            throw new InvalidOperationException($"{nameof(StringId)}.{id} is missing a source offset.");
            offsets[id] = attribute.Offset;
        }

        return offsets;
    }

    private static Dictionary<ushort, StringId> BuildOffsetIndex(IReadOnlyDictionary<StringId, ushort> sourceOffsets)
    {
        var idsBySourceOffset = new Dictionary<ushort, StringId>();
        foreach (var (id, offset) in sourceOffsets)
        {
            if (!idsBySourceOffset.TryAdd(offset, id))
            {
                throw new InvalidOperationException(
                    $"{nameof(StringId)} source offset 0x{offset:X4} is assigned to both {idsBySourceOffset[offset]} and {id}.");
            }
        }

        return idsBySourceOffset;
    }

    private static Dictionary<StringId, string> LoadOriginalStrings(MzExecutableImage image,
        Dictionary<StringId, ushort> sourceOffsetsByStringId)
    {
        const ushort dgroupOffset = 0x04B9;
        var source = image.ReadMemoryByDgroupRange(dgroupOffset, 0x5810, "Game string source span").Span;
        var strings = new Dictionary<StringId, string>(sourceOffsetsByStringId.Count);

        foreach (var (id, sourceOffset) in sourceOffsetsByStringId)
        {
            var relativeOffset = sourceOffset - dgroupOffset;
            if ((uint)relativeOffset >= source.Length)
            {
                throw new InvalidOperationException(
                    $"{id} source offset 0x{sourceOffset:X4} falls outside the loaded game string source span.");
            }

            var slice = source[relativeOffset..];
            var terminatorIndex = slice.IndexOf((byte)0);
            if (terminatorIndex < 0)
            {
                throw new InvalidOperationException(
                    $"{id} source offset 0x{sourceOffset:X4} is not null-terminated inside the loaded source span.");
            }

            strings[id] = Cp437.Decode(slice[..terminatorIndex]);
        }

        return strings;
    }

    private static InteractionHandlerId[] ReadInteractionHandlers(MzExecutableImage image)
    {
        var handlerWords = image.ReadWordTable(0x20D0, 4096, "Program-state interaction handler table");
        var handlers = new InteractionHandlerId[handlerWords.Length];
        for (var index = 0; index < handlers.Length; index++)
        {
            handlers[index] = (InteractionHandlerId)handlerWords[index];
        }

        return handlers;
    }

    private static Dictionary<ushort, byte[]> ReadWordRunStreams(MzExecutableImage image,
        ReadOnlySpan<byte> dataBlockSeed)
    {
        var streams = new Dictionary<ushort, byte[]>();
        var sceneSection = dataBlockSeed.Slice(SceneDescriptorSection.Offset, SceneDescriptorSection.Length);
        for (var index = 0; index < SceneDescriptorSection.Count; index++)
        {
            var descriptor = new SceneDescriptorRecord();
            descriptor.ReadFrom(sceneSection.Slice(index * SceneDescriptorRecord.Size, SceneDescriptorRecord.Size));
            if (descriptor.RleWordStreamOffset == 0 || streams.ContainsKey(descriptor.RleWordStreamOffset))
            {
                continue;
            }

            streams[descriptor.RleWordStreamOffset] =
                image.ReadBytesByDgroupOffset(descriptor.RleWordStreamOffset, 1280, "RLE word stream");
        }

        return streams;
    }
}
