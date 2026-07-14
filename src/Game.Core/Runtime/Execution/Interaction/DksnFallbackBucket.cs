namespace Game.Runtime.Execution.Interaction;

/// <summary>
///     Identifies one DKSN missing-hotspot fallback text bucket.
/// </summary>
internal enum DksnFallbackBucket
{
    /// <summary>
    ///     Shared default fallback bucket used across commands.
    /// </summary>
    Default = 0,

    /// <summary>
    ///     Inspect-command fallback bucket.
    /// </summary>
    Inspect = 1,

    /// <summary>
    ///     Use-command fallback bucket.
    /// </summary>
    Use = 2,

    /// <summary>
    ///     Open/close-command fallback bucket.
    /// </summary>
    OpenClose = 3,

    /// <summary>
    ///     Read-command fallback bucket.
    /// </summary>
    Read = 4,

    /// <summary>
    ///     Write-command fallback bucket.
    /// </summary>
    Write = 5,

    /// <summary>
    ///     Take-command fallback bucket.
    /// </summary>
    Take = 6,

    /// <summary>
    ///     Buy-command fallback bucket.
    /// </summary>
    Buy = 7,

    /// <summary>
    ///     Sit/stand-command fallback bucket.
    /// </summary>
    SitStand = 8,

    /// <summary>
    ///     Total number of represented fallback buckets.
    /// </summary>
    BucketCount = 9
}
