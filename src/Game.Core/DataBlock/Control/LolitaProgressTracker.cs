namespace Game.DataBlock.Control;

/// <summary>
///     Owns the persisted Lolita-heart progress bitfield stored at data-block word <c>+0x17F2</c>.
/// </summary>
internal sealed class LolitaProgressTracker
{
    private ushort _flags;

    /// <summary>
    ///     Replaces the tracked Lolita-heart progress state from its persisted raw value.
    /// </summary>
    /// <param name="rawValue">Persisted Lolita-heart progress bitfield read from the data block.</param>
    internal void ReadFromRawValue(ushort rawValue)
    {
        _flags = rawValue;
    }

    /// <summary>
    ///     Serializes the current Lolita-heart progress state to the persisted data-block bitfield.
    /// </summary>
    /// <returns>Current raw bitfield value for data-block persistence.</returns>
    internal ushort ToRawValue()
    {
        return _flags;
    }

    /// <summary>
    ///     Publishes that the Euroflop ordering milestone has been completed.
    /// </summary>
    internal void MarkEuroflopProductOrdered()
    {
        _flags |= 0x0001;
    }

    /// <summary>
    ///     Publishes that the strawberry-picking milestone has been completed.
    /// </summary>
    internal void MarkStrawberriesTaken()
    {
        _flags |= 0x0002;
    }

    /// <summary>
    ///     Publishes that the whipped-cream preparation milestone has been completed.
    /// </summary>
    internal void MarkWhippedCreamPrepared()
    {
        _flags |= 0x0004;
    }

    /// <summary>
    ///     Publishes that the Lolita telephone contact milestone has been completed.
    /// </summary>
    internal void MarkLolitaContacted()
    {
        _flags |= 0x0008;
    }

    /// <summary>
    ///     Returns whether every tracked Lolita-heart prerequisite has been completed.
    /// </summary>
    /// <returns><see langword="true" /> when all four tracked milestones are present.</returns>
    internal bool HasCompletedLolitaHeartPrerequisites()
    {
        return (_flags & 0x000F) == 0x000F;
    }
}
