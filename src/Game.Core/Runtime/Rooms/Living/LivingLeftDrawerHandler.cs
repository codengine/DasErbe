using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.Living;

internal sealed class LivingLeftDrawerHandler(Erbe runtime)
{
    private const int LeftDrawerPhoneBookCollectedStateIndex = 0x05;
    private const ushort LeftDrawerTakePhoneBookTextEntryIndex = 0x0005;
    private const int CarKeyCollectedStateIndex = 0x0F;
    private const ushort LeftDrawerTakeCarKeyTextEntryIndex = 0x000F;

    [FunctionSymbol("sub_12790", 0x12790)]
    internal void RunTakeFromDrawer()
    {
        var drawerState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseLeftDrawerRecord].State;
        var entryStates = runtime.State.RawDataBlock.Control.TransitionTextEntryStates;
        var phoneBookCollectedFlag = entryStates[LeftDrawerPhoneBookCollectedStateIndex];
        var carKeyCollectedFlag = entryStates[CarKeyCollectedStateIndex];

        switch (leftDrawerState: drawerState, leftDrawerPhoneBookCollectedFlag: phoneBookCollectedFlag,
            carKeyCollectedFlag)
        {
            // IDA 0x12793..0x127A9: when the left drawer is open and the phone book still occupies it, publish entry
            // 0x05 through sub_14270 before showing the fixed discovery line about the car key underneath it.
            case (StateId.Open, 0, _):
                runtime.PromptController.SelectTransitionTextEntry(LeftDrawerTakePhoneBookTextEntryIndex);

                // IDA 0x127AA..0x127B4: block through the fixed "car key under the phone book" line after publishing
                // entry 0x05.
                runtime.PromptController.RunTextAnimation(StringId.LivingRoom_LeftDrawerCarKeyReveal);
                return;

            // IDA 0x127B6..0x127C4: once the phone book is gone but the car key byte is still zero, publish entry
            // 0x0F through sub_14270 and return without a blocking prompt.
            case (StateId.Open, _, 0):
                runtime.PromptController.SelectTransitionTextEntry(LeftDrawerTakeCarKeyTextEntryIndex);
                return;

            default:
                return;
        }
    }
}
