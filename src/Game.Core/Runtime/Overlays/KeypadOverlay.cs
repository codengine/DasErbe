using Game.Catalogs;
using Game.Display;
using Game.Input;
using Game.Shared.Input;
using Game.Shared.RE;
using Game.Shared.Text;

namespace Game.Runtime.Overlays;

/// <summary>
///     Owns the shared keypad overlay and its managed input buffer.
/// </summary>
/// <param name="runtime">Owning runtime state.</param>
internal sealed class KeypadOverlay(Erbe runtime)
{
    private const ushort CodeEntryPanelSourceColumn = 0x0024;
    private const ushort CodeEntryPanelSourceRow = 0x004C;
    private const ushort CodeEntryPanelWidthPixels = 0x0058;
    private const ushort CodeEntryPanelHeightRows = 0x0036;
    private const ushort CodeEntryPanelDestinationColumn = 0x00D9;
    private const ushort CodeEntryPanelDestinationRow = 0x008F;
    private const ushort CodeEntryPanelTextColumn = 0x000B;
    private const ushort CodeEntryPanelTextRow = 0x0083;
    private const ushort CodeEntryPanelFirstRegionIndex = 0x0002;
    private const ushort CodeEntryPanelRegionCount = 0x000C;
    private const ushort CodeEntryPanelResetSelectionLocalIndex = 0x0009;
    private const ushort CodeEntryPanelConfirmSelectionLocalIndex = 0x000B;
    private const int CurrentInputPrefixLength = 3;
    private const int CurrentInputMaximumWriteIndexExclusive = 9;
    private const int CurrentInputBufferLength = 0x000A;
    private readonly byte[] _currentInputBuffer = new byte[CurrentInputBufferLength];
    private int _currentInputWriteIndex = CurrentInputPrefixLength;

    internal string CurrentInput =>
        Cp437.Decode(_currentInputBuffer.AsSpan(CurrentInputPrefixLength,
            _currentInputWriteIndex - CurrentInputPrefixLength));

    /// <summary>
    ///     Runs the shared keypad overlay to completion.
    /// </summary>
    [FunctionSymbol("sub_125EA", 0x125EA)]
    internal void RunCodeEntryPanel()
    {
        var codeEntryPanelRegion = new DisplayCopyRegion(checked((short)CodeEntryPanelWidthPixels),
            checked((short)CodeEntryPanelHeightRows),
            checked((short)CodeEntryPanelSourceRow),
            checked((short)CodeEntryPanelSourceColumn),
            checked((short)CodeEntryPanelDestinationRow),
            checked((short)CodeEntryPanelDestinationColumn));
        ResetCurrentInputBuffer();

        // IDA 0x125F5..0x1263C: blit the decoded keypad panel from the flow-local panel source surface twice and
        // advance the pointer overlay after each pass before entering the blocking selection loop.
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            codeEntryPanelRegion);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            codeEntryPanelRegion);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        while (true)
        {
            var selectedLocalIndex = -1;

            // IDA 0x1263F..0x126BA adapted: advance the transition band, render the mutable entered-input buffer at the
            // fixed prompt row, then apply either pointer-driven keypad selection or keyboard digit/edit/confirm input
            // until the confirm local index 0x0B is chosen.
            runtime.TransitionEffect.AdvanceTransitionEffect();
            runtime.TextCursor.SeedTextBlockCursor(CodeEntryPanelTextColumn, CodeEntryPanelTextRow);
            runtime.TextRenderer.RenderTextBlock(_currentInputBuffer);

            var inputEvent = runtime.InputAdapter.PollInputEvent();
            if (inputEvent == RuntimeInputEvent.PrimaryClick)
            {
                selectedLocalIndex = runtime.InteractiveRegions.FindFirstSelectablePanelRegionAtPointer(
                    CodeEntryPanelFirstRegionIndex,
                    CodeEntryPanelRegionCount);
                if (selectedLocalIndex != -1)
                {
                    switch (selectedLocalIndex)
                    {
                        case CodeEntryPanelResetSelectionLocalIndex:
                            ResetCurrentInputBuffer();
                            break;
                        case < CodeEntryPanelConfirmSelectionLocalIndex
                            when _currentInputWriteIndex < CurrentInputMaximumWriteIndexExclusive:
                            _currentInputBuffer[_currentInputWriteIndex++] =
                                ReadFirstSelectionCharacter((ushort)selectedLocalIndex);
                            break;
                    }

                    _currentInputBuffer[_currentInputWriteIndex] = 0;
                }
            }
            else if (inputEvent.IsKeyboardKey(InputKey.Enter))
            {
                selectedLocalIndex = CodeEntryPanelConfirmSelectionLocalIndex;
            }
            else if (inputEvent.IsKeyboardKey(InputKey.Backspace) && _currentInputWriteIndex > CurrentInputPrefixLength)
            {
                _currentInputWriteIndex--;
                _currentInputBuffer[_currentInputWriteIndex] = 0;
            }
            else if (inputEvent.KeyStroke.Character is { } digitCharacter and >= '0' and <= '9' &&
                     _currentInputWriteIndex < CurrentInputMaximumWriteIndexExclusive)
            {
                _currentInputBuffer[_currentInputWriteIndex++] = checked((byte)digitCharacter);
                _currentInputBuffer[_currentInputWriteIndex] = 0;
            }

            runtime.PointerOverlay.AdvancePointerOverlayFrame();
            if (selectedLocalIndex == CodeEntryPanelConfirmSelectionLocalIndex)
            {
                return;
            }
        }
    }

    /// <summary>
    ///     Reads one entered keypad digit, or zero when the requested digit has not been entered.
    /// </summary>
    /// <param name="characterIndex">Zero-based entered digit index.</param>
    /// <returns>The entered digit byte, or zero when the index is beyond the current input.</returns>
    internal byte GetCurrentInputCharacterAtOrZero(int characterIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(characterIndex);

        var currentInputLength = _currentInputWriteIndex - CurrentInputPrefixLength;
        if (characterIndex >= currentInputLength)
        {
            return 0;
        }

        return _currentInputBuffer[CurrentInputPrefixLength + characterIndex];
    }

    private void ResetCurrentInputBuffer()
    {
        Array.Clear(_currentInputBuffer);
        _currentInputBuffer[0] = 0x20;
        _currentInputBuffer[1] = 0x3A;
        _currentInputBuffer[2] = 0x20;
        _currentInputWriteIndex = CurrentInputPrefixLength;
    }

    private byte ReadFirstSelectionCharacter(ushort selectionLocalIndex)
    {
        var interactiveRegionIndex = checked((ushort)(CodeEntryPanelFirstRegionIndex + selectionLocalIndex));
        var descriptor =
            runtime.Interactions.ReadInteractiveSelectionDescriptor(
                InteractionDescriptorRef.InteractiveRegion(interactiveRegionIndex));
        var selectionText = runtime.Strings.GetCp437String(descriptor.SelectionText);
        if (selectionText.Length == 0 || selectionText[0] == 0)
        {
            throw new InvalidOperationException(
                $"sub_125EA requires a non-empty leading selection character for local index 0x{selectionLocalIndex:X4}.");
        }

        return selectionText[0];
    }
}
