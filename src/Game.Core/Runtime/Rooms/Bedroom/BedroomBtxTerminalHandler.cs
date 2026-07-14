using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.Bedroom;

internal sealed class BedroomBtxTerminalHandler(Erbe runtime)
{
    internal void RunBtxTerminalMenu()
    {
        // IDA 0x132D7..0x13516: the reviewed BTX branch only runs while the bedroom computer state word is 0x0008.
        // Every other state falls back to the same keyboard line used by sub_12E82's ready-state branch.
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomKeyboardRecord].State !=
            StateId.Bedroom.BtxTerminalReady)
        {
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_BrokenKeyboardInspection);
            return;
        }

        LoadDisplayBackdropIntoFullScreenSourceSurface();

        while (true)
        {
            DrawBtxMenuScreen(MainMenu);
            var input = ReadMenuInputSnapshot();
            switch (input.HasAdditionalCharacters)
            {
                case false when input.FirstCharacter == (byte)'0':
                    RestoreSceneAfterBtxTerminal();
                    return;
                case true:
                    continue;
                default:
                    switch (input.FirstCharacter)
                    {
                        case (byte)'1':
                            RunBrokenWingsMenu();
                            break;
                        case (byte)'2':
                            RunBundesbahnMenu();
                            break;
                        case (byte)'3':
                            RunEuroflopMenu();
                            break;
                    }

                    break;
            }
        }
    }

    /// <summary>
    ///     Clears the BTX terminal screen, writes five runtime text lines, and publishes the buffer twice.
    /// </summary>
    /// <param name="lines">Lines to draw</param>
    [FunctionSymbol("sub_13203", 0x13203)]
    private void DrawBtxMenuScreen(StringId[] lines)
    {
        runtime.State.Presentation.Display.CurrentDrawColorIndex = 0;

        for (var i = 0; i < 2; i++)
        {
            runtime.DisplayPrimitives.FillRectangleWithCurrentColor(0, 0, 319, 127);
            runtime.TextCursor.SeedTextBlockCursor(0, 0);

            for (var index = 0; index < lines.Length; index++)
            {
                if (index > 0)
                {
                    runtime.TextCursor.AdvanceTextBlockCursorToNextRow();
                }

                runtime.TextRenderer.RenderStringBlock(lines[index]);
            }

            runtime.PointerOverlay.AdvancePointerOverlayFrame();
        }
    }

    private void RunBrokenWingsMenu()
    {
        while (true)
        {
            DrawBtxMenuScreen(BrokenWingsMenu);
            var input = ReadMenuInputSnapshot();
            if (input is { HasAdditionalCharacters: false, FirstCharacter: (byte)'0' })
            {
                return;
            }

            switch (input.FirstCharacter)
            {
                case (byte)'1':
                case (byte)'3':
                    runtime.PromptController.RunTextAnimation(StringId.BtxTerminal_BrokenWingsFlightResponse);
                    continue;
                case (byte)'2':
                    var hasConfirmed = runtime.PromptController.RunTextAnimationWithConfirmationPrompt(
                        StringId.BtxTerminal_BrokenWingsConfirmationPrompt);

                    if (hasConfirmed && !input.HasAdditionalCharacters)
                    {
                        runtime.PromptController.QueueOzoneAlternateSceneTransition(
                            StringId.BtxTerminal_BrokenWingsQueuedFollowupText,
                            2);
                        return;
                    }

                    continue;
                default:
                    continue;
            }
        }
    }

    private void RunBundesbahnMenu()
    {
        while (true)
        {
            DrawBtxMenuScreen(BundesbahnMenu);
            var input = ReadMenuInputSnapshot();
            if (input.HasAdditionalCharacters || input.FirstCharacter > (byte)'3')
            {
                continue;
            }

            if (input.FirstCharacter == (byte)'0')
            {
                return;
            }

            runtime.PromptController.RunTextAnimation(StringId.BtxTerminal_BundesbahnBookingResponse);
            runtime.State.RawDataBlock.Control.StoryProgress.MarkBtxTravelBooked();
            return;
        }
    }

    private void RunEuroflopMenu()
    {
        while (true)
        {
            DrawBtxMenuScreen(EuroflopMenu);
            var input = ReadMenuInputSnapshot();
            switch (input.FirstCharacter)
            {
                case (byte)'1':
                    runtime.PromptController.RunTextAnimation(StringId.BtxTerminal_EuroflopProductOption1Response);
                    break;
                case (byte)'3':
                    runtime.PromptController.RunTextAnimation(StringId.BtxTerminal_EuroflopProductOption3Response);
                    break;
            }

            if (input.HasAdditionalCharacters)
            {
                continue;
            }

            if (input.FirstCharacter == (byte)'0')
            {
                return;
            }

            if (input.FirstCharacter != (byte)'2')
            {
                continue;
            }

            runtime.PromptController.RunTextAnimation(StringId.BtxTerminal_EuroflopProductOption2Response);
            runtime.State.RawDataBlock.Control.LolitaProgress.MarkEuroflopProductOrdered();
            return;
        }
    }

    private void LoadDisplayBackdropIntoFullScreenSourceSurface()
    {
        var displayBackdropLoadBuffer =
            runtime.ContentFileLoader.LoadOrThrow(AssetId.DisplayBackdrop);
        runtime.LbmDecoder.DecodeIntoBuffer(displayBackdropLoadBuffer,
            runtime.FullScreenSourceSurface.Buffer,
            DisplayCompatibilityState.StrideBytes);
    }

    private KeypadInputSnapshot ReadMenuInputSnapshot()
    {
        runtime.KeypadOverlay.RunCodeEntryPanel();
        return new KeypadInputSnapshot(runtime.KeypadOverlay.GetCurrentInputCharacterAtOrZero(0),
            runtime.KeypadOverlay.GetCurrentInputCharacterAtOrZero(1));
    }

    private void RestoreSceneAfterBtxTerminal()
    {
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.ProgramScene.RenderCurrentScene();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.ProgramScene.RenderCurrentScene();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    private readonly record struct KeypadInputSnapshot(byte FirstCharacter, byte SecondCharacter)
    {
        internal bool HasAdditionalCharacters => SecondCharacter != 0;
    }

    #region BtxMenus

    private static readonly StringId[] MainMenu =
    [
        StringId.BtxTerminal_BtxMainMenuTitle,
        StringId.BtxTerminal_BtxMainMenuBrokenWingsOption,
        StringId.BtxTerminal_BtxMainMenuBundesbahnOption,
        StringId.BtxTerminal_BtxMainMenuEuroflopOption,
        StringId.BtxTerminal_BtxMainMenuExitOption
    ];

    private static readonly StringId[] BrokenWingsMenu =
    [
        StringId.BtxTerminal_BrokenWingsMenuTitle,
        StringId.BtxTerminal_BrokenWingsFlightOption1,
        StringId.BtxTerminal_BrokenWingsFlightOption2,
        StringId.BtxTerminal_BrokenWingsFlightOption3,
        StringId.BtxTerminal_BtxSubmenuExitOption
    ];

    private static readonly StringId[] BundesbahnMenu =
    [
        StringId.BtxTerminal_BundesbahnMenuTitle,
        StringId.BtxTerminal_BundesbahnClassOption1,
        StringId.BtxTerminal_BundesbahnClassOption2,
        StringId.BtxTerminal_BundesbahnClassOption3,
        StringId.BtxTerminal_BtxSubmenuExitOption
    ];

    private static readonly StringId[] EuroflopMenu =
    [
        StringId.BtxTerminal_EuroflopMenuTitle,
        StringId.BtxTerminal_EuroflopProductOption1,
        StringId.BtxTerminal_EuroflopProductOption2,
        StringId.BtxTerminal_EuroflopProductOption3,
        StringId.BtxTerminal_BtxSubmenuExitOption
    ];

    #endregion
}
