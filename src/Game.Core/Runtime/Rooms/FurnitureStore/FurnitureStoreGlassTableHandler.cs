using Game.Catalogs;
using Game.DataBlock.Interaction;
using Game.DataBlock.Selection;
using Game.Text;

namespace Game.Runtime.Rooms.FurnitureStore;

internal sealed class FurnitureStoreGlassTableHandler(Erbe runtime)
{
    internal void RunPurchase()
    {
        // IDA 0x13B6F..0x13B75: material side effect: latch the reviewed villa-condition bit 0x0080 before the
        // congratulatory purchase prompt runs.
        runtime.State.RawDataBlock.Control.StoryProgress.MarkGlassTablePurchased();

        // IDA 0x13B75..0x13B7B: material side effect: publish house-table state 0x0008 before the prompt.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseTableRecord].State =
            StateId.Workflow.Completed;

        // IDA 0x13B7B..0x13B85: block through the fixed furniture-purchase congratulation line via the shared
        // timed transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.FurnitureStore_FurniturePurchaseCongratulations);

        // IDA 0x13B85..0x13BA7: material side effect: publish the reviewed six-word house-table placement bundle
        // only after the congratulation prompt finishes.
        var tablePlacementRecord =
            runtime.State.RawDataBlock.InteractionDescriptors[InteractionDescriptorCatalog
                .LivingRoomTablePlacementRecord];
        tablePlacementRecord.SourceColumn = 171;
        tablePlacementRecord.SourceRow = 39;
        tablePlacementRecord.WidthPixels = 93;
        tablePlacementRecord.HeightRows = 30;
        tablePlacementRecord.PositionColumnWord = 108;
        tablePlacementRecord.PositionRowWord = 81;
    }
}
