using Game.Catalogs;
using Game.State;

namespace Game.Runtime.Execution.Interaction;

internal readonly record struct InteractionSelectionResolution(
    ushort SelectionLocalIndex,
    ushort SelectedInteractionIndex,
    InteractionDescriptorRef DescriptorRef,
    InteractiveSelectionDescriptor Descriptor);
