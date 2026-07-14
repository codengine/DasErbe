using Game.Catalogs;
using Game.Shared.Diagnostics;

namespace Game.Runtime.Execution;

internal sealed class InteractionHandlerRouter(Erbe runtime, SaveScreenController saveScreenController)
{
    internal void DispatchInteractionOrThrow(string selectionContext,
        ushort selectedInteractionIndex,
        int handlerByteOffset,
        InteractionHandlerId handlerId)
    {
        if (handlerId == InteractionHandlerId.None)
        {
            return;
        }

        GameLog.Write(LoggingChannel.Runtime,
            $"sub_14C83 callback dispatch {selectionContext} handlerByteOffset=+0x{handlerByteOffset:X2} handler=0x{(ushort)handlerId:X4} selectionGlobal=0x{selectedInteractionIndex:X4}");

        if (handlerId == InteractionHandlerId.Shared_ToggleSelectionDataState)
        {
            saveScreenController.DispatchToggleSelectionDataState(selectedInteractionIndex);
            return;
        }

        if (runtime.InventoryOverlay.TryDispatchInteraction(handlerId) ||
            runtime.BasementRoom.TryDispatchInteraction(handlerId) ||
            runtime.KitchenRoom.TryDispatchInteraction(handlerId) ||
            runtime.GardenRoom.TryDispatchInteraction(handlerId) ||
            runtime.IngesShop.TryDispatchInteraction(handlerId) || runtime.CityRoom.TryDispatchInteraction(handlerId) ||
            runtime.City2Room.TryDispatchInteraction(handlerId) ||
            runtime.HouseRoom.TryDispatchInteraction(handlerId) ||
            runtime.LivingRoom.TryDispatchInteraction(handlerId) ||
            runtime.BedroomRoom.TryDispatchInteraction(handlerId, selectedInteractionIndex) ||
            runtime.FurnitureStore.TryDispatchInteraction(handlerId))
        {
            return;
        }

        throw new NotImplementedException($"No dispatcher found for handlerId 0x{(ushort)handlerId:X4}");
    }
}
