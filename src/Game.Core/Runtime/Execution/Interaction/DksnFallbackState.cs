namespace Game.Runtime.Execution.Interaction;

/// <summary>
///     Tracks which DKSN fallback lines have already been shown for each bucket during the current runtime session.
/// </summary>
internal sealed class DksnFallbackState
{
    private readonly ulong[] _shownLineMasks = new ulong[(int)DksnFallbackBucket.BucketCount];

    /// <summary>
    ///     Reads the shown-line bitmask for one fallback bucket.
    /// </summary>
    /// <param name="bucket">Bucket whose shown-line bitmask should be returned.</param>
    /// <returns>The shown-line bitmask for the requested bucket.</returns>
    internal ulong ReadShownLineMask(DksnFallbackBucket bucket)
    {
        return _shownLineMasks[(int)bucket];
    }

    /// <summary>
    ///     Marks one bucket line as already shown.
    /// </summary>
    /// <param name="bucket">Bucket that owns the selected line.</param>
    /// <param name="lineIndex">Zero-based line index within the bucket.</param>
    internal void MarkLineShown(DksnFallbackBucket bucket, int lineIndex)
    {
        _shownLineMasks[(int)bucket] |= 1UL << lineIndex;
    }

    /// <summary>
    ///     Clears the shown-line bitmask for one bucket.
    /// </summary>
    /// <param name="bucket">Bucket whose shown-line history should be cleared.</param>
    internal void ClearBucket(DksnFallbackBucket bucket)
    {
        _shownLineMasks[(int)bucket] = 0;
    }
}
