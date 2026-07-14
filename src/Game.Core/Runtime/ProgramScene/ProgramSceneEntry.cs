namespace Game.Runtime.ProgramScene;

internal readonly record struct ProgramSceneEntry(
    ushort EntryIndex,
    ushort StateMask,
    ushort RequiredMask,
    short SourceColumn,
    short SourceRow,
    short WidthPixels,
    short HeightRows,
    short DestinationColumn,
    short DestinationRow);
