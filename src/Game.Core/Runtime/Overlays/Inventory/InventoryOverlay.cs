using System.Collections.Frozen;
using Game.Catalogs;
using Game.Runtime.Execution;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Overlays.Inventory;

/// <summary>
///     Owns inventory-item overlay handlers while shared text-animation dispatch remains in
///     <see cref="PromptController" />.
/// </summary>
/// <param name="runtime">Runtime owner that provides shared prompt state and selection tables.</param>
/// <param name="phoneBookViewer">Phone-book overlay viewer for the blocking read-display slice.</param>
internal sealed class InventoryOverlay(Erbe runtime, PhoneBookViewer phoneBookViewer)
{
    private const ushort BallpointPenUseWithWritingPaperTextEntryIndex = 0x0002;
    public const ushort RecycledWritingPaperBuyTextEntryIndex = 0x000D;
    public const ushort LiquidWhippedCreamBuyTextEntryIndex = 0x0007;
    private const ushort StampDiscoveryTextEntryIndex = 0x000E;
    private const ushort CreamAndBowlPreparationTextEntryIndex = 0x0004;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.Inventory_InspectNote, StringId.Inventory_NoteInspection },
            { InteractionHandlerId.Inventory_InspectBrochure, StringId.Inventory_BrochureInspection },
            { InteractionHandlerId.Inventory_ReadBrochure, StringId.Inventory_BrochureRead },
            { InteractionHandlerId.Inventory_InspectBallpointPen, StringId.Inventory_BallpointPenInspection },
            {
                InteractionHandlerId.Inventory_InspectLiquidWhippedCream,
                StringId.Inventory_LiquidWhippedCreamInspection
            },
            {
                InteractionHandlerId.Inventory_InspectRecycledWritingPaper,
                StringId.Inventory_RecycledWritingPaperInspection
            },
            { InteractionHandlerId.Inventory_InspectStamp, StringId.Inventory_StampInspection },
            { InteractionHandlerId.Inventory_InspectWhiteWritingPaper, StringId.Inventory_WhiteWritingPaperInspection },
            { InteractionHandlerId.Inventory_InspectSprayCream, StringId.Inventory_SprayCreamInspection },
            { InteractionHandlerId.Inventory_InspectPhoneBook, StringId.Inventory_PhoneBookInspection },
            { InteractionHandlerId.Inventory_InspectWallet, StringId.Inventory_WalletInspection },
            { InteractionHandlerId.Inventory_InspectCarKey, StringId.Inventory_CarKeyInspection },
            { InteractionHandlerId.Inventory_InspectLighter, StringId.Inventory_LighterInspection }
        }.ToFrozenDictionary();

    private readonly byte[] _extraPhonebookPrompt =
        TextUtils.EncodeNullTerminated(
            "Als du das Telefonbuch zurück legst, siehst du noch eine weitere Telefonnummer: 170286");

    private readonly WhiskWithBowlSuccessHandler _whiskWithBowlSuccessHandler = new(runtime);

    /// <summary>
    ///     Dispatches one inventory-specific handler directly to completion.
    /// </summary>
    /// <param name="handlerId">Resolved handler identifier from the interaction descriptor.</param>
    internal bool TryDispatchInteraction(InteractionHandlerId handlerId)
    {
        if (SimpleTextHandlers.TryGetValue(handlerId, out var stringId))
        {
            runtime.PromptController.RunTextAnimation(stringId);
            return true;
        }

        switch (handlerId)
        {
            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Inventory_UseBallpointPenWithWritingPaper:
                DispatchUseBallpointPenWithWritingPaper();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Inventory_UseStampWithLetter:
                DispatchUseStampWithLetter();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Inventory_UseCreamWithBowl:
                DispatchUseCreamWithBowl();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Inventory_UseWhiskWithBowl:
                DispatchUseWhiskWithBowl();
                return true;

            // handlerByteOffset +0x08 / controlMask 0x0010
            case InteractionHandlerId.Inventory_ReadPhoneBook:
                DispatchReadPhoneBook();
                return true;

            default:
                return false;
        }
    }

    [FunctionSymbol("sub_13BE5", 0x13BE5)]
    private void DispatchUseBallpointPenWithWritingPaper()
    {
        // IDA 0x13BE8..0x13BEC: clear transition-text entry 0x000D before switching the prompt-selection surface to
        // the ballpoint-pen-with-writing-paper entry.
        runtime.State.RawDataBlock.Control.TransitionTextEntryStates[RecycledWritingPaperBuyTextEntryIndex] = 0;

        // IDA 0x13BED..0x13BF4: enable transition-text entry 0x0002 and publish it as the current start index
        // through the shared selectTransitionTextEntry seam sub_14270.
        runtime.PromptController.SelectTransitionTextEntry(BallpointPenUseWithWritingPaperTextEntryIndex);
    }

    [FunctionSymbol("sub_13BF7", 0x13BF7)]
    private void DispatchUseStampWithLetter()
    {
        // IDA 0x13BFA..0x13BFE: clear transition-text entry 0x000E before publishing the prepared-letter staging
        // state on entry 0x0002.
        var enabledFlags = runtime.State.RawDataBlock.Control.TransitionTextEntryStates;
        enabledFlags[StampDiscoveryTextEntryIndex] = 0;

        // IDA 0x13BFF..0x13C08: publish state value 0x02 on transition-text entry 0x0002 and republish that same
        // entry as the current start index. The non-binary value is consumed later by sub_13538.
        enabledFlags[BallpointPenUseWithWritingPaperTextEntryIndex] = 2;
        runtime.State.RawDataBlock.Control.TransitionTextStartIndex =
            (byte)BallpointPenUseWithWritingPaperTextEntryIndex;
    }

    [FunctionSymbol("sub_13C0B", 0x13C0B)]
    private void DispatchUseCreamWithBowl()
    {
        // IDA 0x13C0E..0x13C12: clear transition-text entry 0x0007 before publishing the cream-and-bowl preparation
        // staging state on entry 0x0004.
        var enabledFlags = runtime.State.RawDataBlock.Control.TransitionTextEntryStates;
        enabledFlags[LiquidWhippedCreamBuyTextEntryIndex] = 0;

        // IDA 0x13C13..0x13C1C: publish state value 0x02 on transition-text entry 0x0004 and republish that same
        // entry as the current start index. The non-binary value is consumed later by sub_13C1F.
        enabledFlags[CreamAndBowlPreparationTextEntryIndex] = 2;
        runtime.State.RawDataBlock.Control.TransitionTextStartIndex = (byte)CreamAndBowlPreparationTextEntryIndex;
    }

    [FunctionSymbol("sub_13C1F", 0x13C1F)]
    private void DispatchUseWhiskWithBowl()
    {
        _whiskWithBowlSuccessHandler.RunTransitionTextEffect();
    }

    [FunctionSymbol("sub_13A50", 0x13A50)]
    private void DispatchReadPhoneBook()
    {
        phoneBookViewer.RunDisplayPhonebook();
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.ProgramScene.RenderCurrentScene();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.ProgramScene.RenderCurrentScene();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.PromptController.RunTextAnimation(_extraPhonebookPrompt);
    }
}
