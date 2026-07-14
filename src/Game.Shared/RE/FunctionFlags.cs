using System.Diagnostics.CodeAnalysis;

namespace Game.Shared.RE;

/// <summary>
///     Extra tags for a carried-over function symbol.
/// </summary>
[Flags]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum FunctionFlags
{
    /// <summary>
    ///     No extra tags.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Original platform/API boundary is intentionally hosted through MonoGame.
    /// </summary>
    AdaptedForMonoGame = 1 << 0,

    /// <summary>
    ///     Original external dependency behavior is intentionally skipped.
    /// </summary>
    SkippedExternal = 1 << 1,

    /// <summary>
    ///     Original symbol is still represented, but no longer does useful work in the port.
    /// </summary>
    ObsoleteInPort = 1 << 2,

    /// <summary>
    ///     Compiler, CRT, libc, or similar runtime support code.
    /// </summary>
    CompilerRuntime = 1 << 3,

    /// <summary>
    ///     Function directly models an interrupt boundary or interrupt-facing contract.
    /// </summary>
    InterruptBoundary = 1 << 4,

    /// <summary>
    ///     Function contract materially depends on self-modifying code behavior.
    /// </summary>
    SelfModifyingCode = 1 << 5,

    /// <summary>
    ///     Far-call or far-return semantics are part of the original function contract.
    /// </summary>
    FarCallBoundary = 1 << 6,

    /// <summary>
    ///     Managed host-side entry point that represents the runtime handoff in the port.
    /// </summary>
    HostEntryPoint = 1 << 7
}
