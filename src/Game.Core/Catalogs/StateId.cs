namespace Game.Catalogs;

/// <summary>
///     Canonical semantic state values used by selection-entry state words and descriptor selection states.
/// </summary>
internal static class StateId
{
    /// <summary>
    ///     Empty scene-entry selection state.
    /// </summary>
    internal const ushort Default = 0x0000;

    /// <summary>
    ///     Descriptor state value that disables a selection.
    /// </summary>
    internal const ushort Disabled = 0x0005;

    /// <summary>
    ///     Descriptor state value that disables a selection.
    /// </summary>
    internal const ushort Broken = 0x0006;

    /// <summary>
    ///     Descriptor state value that hides a selection.
    /// </summary>
    internal const ushort Hidden = 0x0009;

    /// <summary>
    ///     High-bit flag that usually marks the opened or otherwise activated variant of a base state.
    /// </summary>
    internal const ushort Open = 0x0080;

    /// <summary>
    ///     Bedroom box base state values that combine with <see cref="Open" />.
    /// </summary>
    internal static class Container
    {
        /// <summary>
        ///     Box base state for the empty variant.
        /// </summary>
        internal const ushort Empty = 0x0001;

        /// <summary>
        ///     Box base state for the full variant.
        /// </summary>
        internal const ushort Filled = 0x0002;
    }

    /// <summary>
    ///     Shared workflow state values used for scheduled/completed progression.
    /// </summary>
    internal static class Workflow
    {
        /// <summary>
        ///     Completed, installed, purchased, or otherwise applied workflow state.
        /// </summary>
        internal const ushort Completed = 0x0008;

        /// <summary>
        ///     Scheduled workflow state.
        /// </summary>
        internal const ushort Scheduled = 0x0009;
    }

    /// <summary>
    ///     Basement heater states.
    /// </summary>
    internal static class Heater
    {
        /// <summary>
        ///     Completed, installed, purchased, or otherwise applied workflow state.
        /// </summary>
        internal const ushort Replay = 0x0008;

        /// <summary>
        ///     Scheduled workflow state.
        /// </summary>
        internal const ushort Stable = 0x0006;
    }

    /// <summary>
    ///     Bedroom-owned semantic state values that are not covered by shared families.
    /// </summary>
    internal static class Bedroom
    {
        /// <summary>
        ///     Bedroom heater off state.
        /// </summary>
        internal const ushort HeaterInactive = 0x0000;

        /// <summary>
        ///     Bedroom heater active state.
        /// </summary>
        internal const ushort HeaterActive = 0x0080;

        /// <summary>
        ///     Bedroom bird alive state.
        /// </summary>
        internal const ushort BirdAlive = 0x000A;

        /// <summary>
        ///     Bedroom bird dead state.
        /// </summary>
        internal const ushort BirdDead = 0x0010;

        /// <summary>
        ///     BTX terminal ready state.
        /// </summary>
        internal const ushort BtxTerminalReady = 0x0008;

        /// <summary>
        ///     Bedroom box contents taken marker state.
        /// </summary>
        internal const ushort BoxContentsTaken = 0x0002;

        /// <summary>
        ///     House painting scheduled state.
        /// </summary>
        internal const ushort PhoneServiceScheduled = 0x0005;
    }

    /// <summary>
    ///     House-owned semantic state values that are not covered by shared families.
    /// </summary>
    internal static class House
    {
        /// <summary>
        ///     Garage vehicle ready state.
        /// </summary>
        internal const ushort CarReady = 0x0080;

        /// <summary>
        ///     Garage vehicle variant that labels the car.
        /// </summary>
        internal const ushort GarageVehicleVariantCar = 0x0002;

        /// <summary>
        ///     Friendly-man visible state.
        /// </summary>
        internal const ushort FriendlyManVisible = 0x0004;

        /// <summary>
        ///     Bicycle base state values that combine with <see cref="Open" />.
        /// </summary>
        internal static class Bicycle
        {
            /// <summary>
            ///     Bicycle base state for the flat-tire variant.
            /// </summary>
            internal const ushort Flat = 0x0001;

            /// <summary>
            ///     Bicycle base state for the ready-tire variant.
            /// </summary>
            internal const ushort Ready = 0x0002;
        }
    }

    /// <summary>
    ///     Kitchen-owned semantic state values that are not covered by shared families.
    /// </summary>
    internal static class Kitchen
    {
        /// <summary>
        ///     Kitchen plate clean state.
        /// </summary>
        internal const ushort DishesClean = 0x0005;

        /// <summary>
        ///     Kitchen refrigerator empty state.
        /// </summary>
        internal const ushort RefrigeratorEmpty = 0x0080;
    }

    /// <summary>
    ///     Garden-owned semantic state values.
    /// </summary>
    internal static class Garden
    {
        /// <summary>
        ///     Garden leaf pile composted state.
        /// </summary>
        internal const ushort LeafPileComposted = 0x0005;
    }

    /// <summary>
    ///     City2-owned semantic state values.
    /// </summary>
    internal static class City2
    {
        /// <summary>
        ///     Posted-letter state value.
        /// </summary>
        internal const ushort LetterSent = 0x0004;
    }
}
