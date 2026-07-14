using Game.Text;

namespace Game.Catalogs;

/// <summary>
///     Describes one boot-seeded program state using managed asset and transition text ids.
/// </summary>
/// <param name="SecondAssetId">Secondary scene asset id published by the original descriptor.</param>
/// <param name="FirstAssetId">Primary scene asset id published by the original descriptor.</param>
/// <param name="OptionalTransitionText">Optional transition text associated with the state.</param>
/// <param name="SceneEntryStartIndex">First scene-entry descriptor index for the state.</param>
/// <param name="SceneEntryCount">Number of scene-entry descriptors owned by the state.</param>
/// <param name="RleWordStreamOffset">DGROUP offset of the optional threshold-row RLE stream.</param>
/// <param name="BackdropEnabledFlag">Backdrop-enabled flag published by the state.</param>
/// <param name="VisibleRowsBase">Base visible-row count used by the program-scene renderer.</param>
internal readonly record struct ProgramStateDescriptor(
    AssetId SecondAssetId,
    AssetId FirstAssetId,
    StringId OptionalTransitionText,
    ushort SceneEntryStartIndex,
    ushort SceneEntryCount,
    ushort RleWordStreamOffset,
    byte BackdropEnabledFlag,
    byte VisibleRowsBase);
