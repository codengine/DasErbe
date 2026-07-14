namespace Game.Display;

internal record struct DisplayCopyRegion(
    short WidthPixels,
    short HeightRows,
    short SourceRow,
    short SourceColumn,
    short DestinationRow,
    short DestinationColumn);
