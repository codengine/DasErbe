namespace Game.Display;

internal readonly record struct ProgramSceneBackdropCopyRegion(
    short SourceColumn,
    short SourceRow,
    short SourceWidthPixels,
    short SourceHeightRows,
    short DestinationColumn,
    short DestinationRow,
    short DestinationWidthPixels,
    short DestinationHeightRows,
    short HorizontalStep);
