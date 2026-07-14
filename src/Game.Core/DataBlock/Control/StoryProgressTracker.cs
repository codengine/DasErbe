namespace Game.DataBlock.Control;

/// <summary>
///     Owns the persisted story-progress bitfield stored at data-block word <c>+0x17F0</c>.
/// </summary>
internal sealed class StoryProgressTracker
{
    private ushort _flags;

    /// <summary>
    ///     Replaces the tracked story-progress state from its persisted raw value.
    /// </summary>
    /// <param name="rawValue">Persisted story-progress bitfield read from the data block.</param>
    internal void ReadFromRawValue(ushort rawValue)
    {
        _flags = rawValue;
    }

    /// <summary>
    ///     Serializes the current story-progress state to the persisted data-block bitfield.
    /// </summary>
    /// <returns>Current raw bitfield value for data-block persistence.</returns>
    internal ushort ToRawValue()
    {
        return _flags;
    }

    /// <summary>
    ///     Publishes that the prepared letter has been delivered through the city mail slot.
    /// </summary>
    internal void MarkLetterPosted()
    {
        _flags |= 0x0002;
    }

    /// <summary>
    ///     Publishes that the house painting issue has been resolved.
    /// </summary>
    internal void MarkPainterResolved()
    {
        _flags |= 0x0004;
    }

    /// <summary>
    ///     Publishes that the waste-service issue has been resolved.
    /// </summary>
    internal void MarkWasteServiceResolved()
    {
        _flags |= 0x0008;
    }

    /// <summary>
    ///     Publishes that the insulation issue has been resolved.
    /// </summary>
    internal void MarkInsulationResolved()
    {
        _flags |= 0x0010;
    }

    /// <summary>
    ///     Publishes that the heating installation issue has been resolved.
    /// </summary>
    internal void MarkHeatingResolved()
    {
        _flags |= 0x0020;
    }

    /// <summary>
    ///     Publishes that the kitchen plumbing issue has been resolved.
    /// </summary>
    internal void MarkPlumberResolved()
    {
        _flags |= 0x0040;
    }

    /// <summary>
    ///     Publishes that the BTX travel booking branch completed.
    /// </summary>
    /// <remarks>
    ///     This intentionally shares raw bit <c>0x0080</c> with the glass-table purchase branch.
    /// </remarks>
    internal void MarkBtxTravelBooked()
    {
        _flags |= 0x0080;
    }

    /// <summary>
    ///     Publishes that the glass table has been purchased.
    /// </summary>
    /// <remarks>
    ///     This intentionally shares raw bit <c>0x0080</c> with the BTX travel booking branch.
    /// </remarks>
    internal void MarkGlassTablePurchased()
    {
        _flags |= 0x0080;
    }

    /// <summary>
    ///     Publishes that the Christoph Columbus armchair has been purchased.
    /// </summary>
    internal void MarkChristophColumbusArmchairPurchased()
    {
        _flags |= 0x0100;
    }

    /// <summary>
    ///     Publishes that the bed has been purchased.
    /// </summary>
    internal void MarkBedPurchased()
    {
        _flags |= 0x0200;
    }

    /// <summary>
    ///     Publishes that the refrigerator has been purchased.
    /// </summary>
    internal void MarkRefrigeratorPurchased()
    {
        _flags |= 0x0400;
    }

    /// <summary>
    ///     Publishes that the fire extinguisher has been purchased.
    /// </summary>
    internal void MarkFireExtinguisherPurchased()
    {
        _flags |= 0x0800;
    }

    /// <summary>
    ///     Publishes that the bedroom heater issue has been resolved.
    /// </summary>
    internal void MarkBedroomHeaterResolved()
    {
        _flags |= 0x1000;
    }

    /// <summary>
    ///     Returns whether every story-progress prerequisite for the friendly-man certificate line is satisfied.
    /// </summary>
    /// <returns><see langword="true" /> when the certificate branch may run; otherwise <see langword="false" />.</returns>
    internal bool MeetsFriendlyManCertificateRequirements()
    {
        return (_flags & 0x1FFE) == 0x1FFE;
    }

    /// <summary>
    ///     Returns whether the friendly-man flight-decision bit is already latched.
    /// </summary>
    /// <returns><see langword="true" /> when the shared advance path should skip the confirmation prompt.</returns>
    internal bool HasFriendlyManFlightDecision()
    {
        return (_flags & 0x0001) != 0;
    }

    /// <summary>
    ///     Returns the house-condition inspection progress derived from the relevant story-progress subset.
    /// </summary>
    /// <returns>Semantic progress state consumed by the house inspection prompt selector.</returns>
    internal HouseConditionInspectionProgress GetHouseConditionInspectionProgress()
    {
        var relevantFlags = (ushort)(_flags & 0x1FF4);
        if (relevantFlags == 0x1FF4)
        {
            return HouseConditionInspectionProgress.Complete;
        }

        return relevantFlags == 0 ? HouseConditionInspectionProgress.None : HouseConditionInspectionProgress.Partial;
    }
}

/// <summary>
///     Semantic progress state used when choosing the house-condition inspection text branch.
/// </summary>
internal enum HouseConditionInspectionProgress
{
    /// <summary>
    ///     None of the tracked house-condition milestones are complete.
    /// </summary>
    None,

    /// <summary>
    ///     Some, but not all, tracked house-condition milestones are complete.
    /// </summary>
    Partial,

    /// <summary>
    ///     Every tracked house-condition milestone is complete.
    /// </summary>
    Complete
}
