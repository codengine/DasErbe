using Game.Catalogs;
using Game.DataBlock.Selection;

namespace Game.Runtime.Rooms.House;

/// <summary>
///     Owns the persistent visibility state for the exterior friendly-man scene entry.
/// </summary>
internal static class HouseFriendlyManVisibilityState
{
    private const ushort HouseFriendlyManSceneEntry = 0x0041;

    /// <summary>
    ///     Publishes the friendly-man selection state from the durable villa-condition progress flags.
    /// </summary>
    /// <param name="runtime">Runtime whose runtime-owned data block receives the corrected state.</param>
    internal static void Synchronize(Erbe runtime)
    {
        var control = runtime.State.RawDataBlock.Control;
        if (!control.StoryProgress.MeetsFriendlyManCertificateRequirements())
        {
            runtime.Interactions.SetSelectionState(InteractionDescriptorRef.SceneEntry(HouseFriendlyManSceneEntry),
                StateId.Disabled);
            return;
        }

        runtime.Interactions.SetSelectionState(InteractionDescriptorRef.SceneEntry(HouseFriendlyManSceneEntry),
            StateId.House.FriendlyManVisible);
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.FriendlyManRecord].State =
            StateId.House.FriendlyManVisible;
    }
}
