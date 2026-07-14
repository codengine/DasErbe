namespace Game.Catalogs;

internal enum InteractionDescriptorTable : byte
{
    None = 0,
    SceneEntry = 1,
    TransitionSelection = 2,
    InteractiveRegion = 3
}

internal readonly record struct InteractionDescriptorRef(InteractionDescriptorTable Table, ushort Index)
{
    internal static InteractionDescriptorRef None => default;

    internal bool IsEmpty => Table == InteractionDescriptorTable.None;

    internal static InteractionDescriptorRef InteractiveRegion(ushort index)
    {
        return new InteractionDescriptorRef(InteractionDescriptorTable.InteractiveRegion, index);
    }

    internal static InteractionDescriptorRef SceneEntry(ushort index)
    {
        return new InteractionDescriptorRef(InteractionDescriptorTable.SceneEntry, index);
    }

    internal static InteractionDescriptorRef TransitionSelection(ushort index)
    {
        return new InteractionDescriptorRef(InteractionDescriptorTable.TransitionSelection, index);
    }
}
