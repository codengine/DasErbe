namespace Game.Shared.Runtime;

/// <summary>
///     Deterministic pseudo-random generator for gameplay state.
/// </summary>
/// <remarks>
///     The state is explicit, so save/load can reproduce the same sequence. Zero is remapped to the default seed to keep
///     the generator running.
/// </remarks>
public sealed class RandomSource
{
    private const uint DefaultSeed = 0xA341316Cu;
    private uint _state = DefaultSeed;

    /// <summary>
    ///     Returns the next value in the range [0, <paramref name="maxExclusive" />).
    /// </summary>
    /// <param name="maxExclusive">The exclusive upper bound.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxExclusive" /> is not positive.</exception>
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "The maximum must be positive.");
        }

        return (int)(NextUInt32() % (uint)maxExclusive);
    }
    private uint NextUInt32()
    {
        var x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x == 0 ? DefaultSeed : x;
        return _state;
    }
}
