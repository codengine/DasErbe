using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Input;
using Game.State;

namespace Game.Runtime.Execution;

internal sealed class InteractionSession
{
    internal readonly byte[] TextBuffer = new byte[0x100];
    internal byte ConfirmationPassCount;
    internal RuntimeInputEvent LastInput;
    internal InteractionButton PrimaryButton;
    internal InteractiveSelectionDescriptor PrimaryDescriptor;
    internal InteractionDescriptorRef PrimaryDescriptorRef;
    internal SelectionEntryData PrimaryRecord = null!;
    internal ushort PrimarySelectedInteractionIndex;
    internal ushort PrimarySelectedInteractionLocalIndex = ushort.MaxValue;
    internal InteractiveSelectionDescriptor SecondaryDescriptor;
    internal InteractionDescriptorRef SecondaryDescriptorRef;
    internal SelectionEntryData SecondaryRecord = null!;
    internal ushort SecondarySelectedInteractionIndex;
    internal ushort SecondarySelectedInteractionLocalIndex = ushort.MaxValue;
    internal InteractiveProgramStatePhase SelectionPhase;

    internal void Begin()
    {
        TextBuffer[0] = 0;
        SelectionPhase = InteractiveProgramStatePhase.PrimaryControlSelection;
        ConfirmationPassCount = 0;
        LastInput = RuntimeInputEvent.None;
        PrimarySelectedInteractionLocalIndex = ushort.MaxValue;
        SecondarySelectedInteractionLocalIndex = ushort.MaxValue;
        PrimarySelectedInteractionIndex = 0;
        SecondarySelectedInteractionIndex = 0;
        PrimaryDescriptorRef = InteractionDescriptorRef.None;
        SecondaryDescriptorRef = InteractionDescriptorRef.None;
        PrimaryButton = InteractionButton.Empty;
        PrimaryDescriptor = default;
        SecondaryDescriptor = default;
        PrimaryRecord = null!;
        SecondaryRecord = null!;
    }

    internal void Reset()
    {
        Begin();
    }
}
