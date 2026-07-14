using System.Diagnostics.CodeAnalysis;

namespace Game.Shared.RE;

/// <summary>
///     Extra tags for a carried-over global symbol or view.
/// </summary>
[Flags]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum GlobalFlags
{
    /// <summary>
    ///     No extra tags.
    /// </summary>
    None = 0,

    /// <summary>
    ///     This member is a semantic view into a packed owner.
    /// </summary>
    PackedView = 1 << 0,

    /// <summary>
    ///     Canonical owner shared by multiple systems or call paths.
    /// </summary>
    SharedOwner = 1 << 1,

    /// <summary>
    ///     Intentional alternate semantic name for the same storage.
    /// </summary>
    AliasView = 1 << 2,

    /// <summary>
    ///     Mirrored saved-state copy of active storage.
    /// </summary>
    SaveMirror = 1 << 3,

    /// <summary>
    ///     Intentionally overlaps another view or storage shape.
    /// </summary>
    Overlayed = 1 << 4,

    /// <summary>
    ///     Runtime latch, one-shot, toggle, or timer-pulse style state.
    /// </summary>
    RuntimeLatch = 1 << 5,

    /// <summary>
    ///     Canonical owner of an indexed table or buffer.
    /// </summary>
    TableOwner = 1 << 6,

    /// <summary>
    ///     Semantic/accessor view into a table owner.
    /// </summary>
    TableView = 1 << 7,

    /// <summary>
    ///     Named view into packed bits of a wider owner.
    /// </summary>
    BitfieldView = 1 << 8,

    /// <summary>
    ///     Directly reflects hardware-facing or API-era state.
    /// </summary>
    HardwareMapped = 1 << 9,

    /// <summary>
    ///     Segment-relative storage contract matters explicitly.
    /// </summary>
    SegmentRelative = 1 << 10,

    /// <summary>
    ///     Persisted or restored across saves/loads.
    /// </summary>
    PersistedState = 1 << 11,

    /// <summary>
    ///     Derived semantic view over canonical backing storage.
    /// </summary>
    DerivedView = 1 << 12,

    /// <summary>
    ///     Marks the canonical owner when multiple views exist.
    /// </summary>
    CanonicalOwner = 1 << 13,

    /// <summary>
    ///     Canonical owner of a raw buffer rather than a semantic table.
    /// </summary>
    BufferOwner = 1 << 14,

    /// <summary>
    ///     Semantic or typed view over a raw buffer owner.
    /// </summary>
    BufferView = 1 << 15,

    /// <summary>
    ///     Storage semantically holds an address, offset, far pointer, or pointer-like value.
    /// </summary>
    PointerLike = 1 << 16
}
