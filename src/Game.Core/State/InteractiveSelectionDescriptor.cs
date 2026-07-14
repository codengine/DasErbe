using Game.DataBlock.Interaction;
using Game.Text;

namespace Game.State;

/// <summary>
///     Executable/runtime interactive-selection descriptor hydrated from descriptor storage.
/// </summary>
/// <param name="SelectionEntryIndex">
///     Persistent selection-entry index that owns the interactive state for this descriptor.
/// </param>
/// <param name="SelectionStateId">
///     Interactive interpretation of descriptor word <c>+0x02</c>.
/// </param>
/// <param name="SelectionText">Selection text used by managed interaction presentation.</param>
/// <param name="NextProgramStateId">Program state reached when the interaction completes.</param>
/// <param name="ResultBackdropColumn">Signed backdrop column used for the interaction result.</param>
/// <param name="ResultBackdropThresholdRow">
///     Signed backdrop threshold row used for the interaction result.
/// </param>
/// <param name="ResultBackdropSelectionRow">
///     Signed backdrop selection row used for the interaction result.
/// </param>
/// <param name="AnimationTargetBackdropColumn">
///     Signed target backdrop column used by follow-up backdrop animation.
/// </param>
/// <param name="AnimationTargetBackdropThresholdRow">
///     Signed target backdrop threshold row used by follow-up backdrop animation.
/// </param>
/// <param name="AnimationTargetBackdropSelectionRow">
///     Signed target backdrop selection row used by follow-up backdrop animation.
/// </param>
/// <param name="WidthPixels">Interactive hotspot width in pixels.</param>
/// <param name="HeightRows">Interactive hotspot height in rows.</param>
/// <param name="LeftColumn">Interactive hotspot left column.</param>
/// <param name="TopRow">Interactive hotspot top row.</param>
/// <remarks>
///     This is the managed interactive seam consumed by runtime interaction logic.
///     It is intentionally narrower and more semantic than
///     <see cref="InteractionDescriptorRecord" />, which remains raw
///     storage with overlapping projections.
/// </remarks>
internal readonly record struct InteractiveSelectionDescriptor(
    ushort SelectionEntryIndex,
    ushort SelectionStateId,
    StringId SelectionText,
    ushort NextProgramStateId,
    short ResultBackdropColumn,
    short ResultBackdropThresholdRow,
    short ResultBackdropSelectionRow,
    short AnimationTargetBackdropColumn,
    short AnimationTargetBackdropThresholdRow,
    short AnimationTargetBackdropSelectionRow,
    ushort WidthPixels = 0,
    ushort HeightRows = 0,
    ushort LeftColumn = 0,
    ushort TopRow = 0);
