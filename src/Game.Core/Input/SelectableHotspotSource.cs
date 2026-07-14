using Game.Catalogs;
using Game.Runtime.ProgramScene;
using Game.Shared.Rendering;
using Game.State;

namespace Game.Input;

/// <summary>
///     Exposes the currently selectable scene hotspots for hotspot overlays.
/// </summary>
/// <param name="scenes">Scene catalog.</param>
/// <param name="state">Live runtime state.</param>
internal sealed class SelectableHotspotSource(ProgramSceneCatalog scenes, RuntimeState state)
{
    /// <summary>
    ///     Captures the currently selectable hotspots for the active scene.
    /// </summary>
    public IReadOnlyList<IntRect> CaptureSelectableHotspots()
    {
        if (!scenes.TryReadStateDescriptor(state.RawDataBlock.Control.ProgramStateId, out var stateDescriptor) ||
            stateDescriptor.SceneEntryCount == 0)
        {
            return [];
        }

        var hotspots = new List<IntRect>(stateDescriptor.SceneEntryCount);
        for (ushort localSelectionIndex = 0;
             localSelectionIndex < stateDescriptor.SceneEntryCount;
             localSelectionIndex++)
        {
            var sceneEntryIndex = (ushort)(stateDescriptor.SceneEntryStartIndex + localSelectionIndex);
            var sceneEntry = scenes.ReadProgramSceneEntry(sceneEntryIndex);
            if (!IsSceneHotspotSelectable(state, sceneEntry))
            {
                continue;
            }

            hotspots.Add(new IntRect(sceneEntry.DestinationColumn,
                sceneEntry.DestinationRow,
                sceneEntry.WidthPixels,
                sceneEntry.HeightRows));
        }

        return hotspots;
    }

    private static bool IsSceneHotspotSelectable(RuntimeState state, ProgramSceneEntry sceneEntry)
    {
        if (sceneEntry is { WidthPixels: 0 } or { HeightRows: 0 })
        {
            return false;
        }

        if (sceneEntry.StateMask is StateId.Disabled or StateId.Hidden)
        {
            return false;
        }

        return state.RawDataBlock.SelectionTable[sceneEntry.EntryIndex].State != StateId.Disabled;
    }
}
