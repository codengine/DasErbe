namespace Game.Shared.Diagnostics;

/// <summary>
///     Enabled log channels.
/// </summary>
public sealed class ChannelSelection
{
    private readonly HashSet<LoggingChannel> _enabledChannels;

    private ChannelSelection(HashSet<LoggingChannel> enabledChannels)
    {
        _enabledChannels = enabledChannels;
    }

    private static ChannelSelection All => new(AllChannels());

    /// <summary>
    ///     Parses a comma-separated channel list. Empty input enables all channels. <c>all</c> enables
    ///     every channel, and entries prefixed with <c>-</c> remove channels from the current set.
    /// </summary>
    /// <param name="rawValue">The channel list to parse.</param>
    /// <returns>The parsed channel selection.</returns>
    public static ChannelSelection Parse(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return All;
        }

        HashSet<LoggingChannel>? enabledChannels = null;
        foreach (var token in SplitTokens(rawValue))
        {
            if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
            {
                enabledChannels = AllChannels();
                continue;
            }

            if (token.StartsWith('-') && token.Length > 1)
            {
                enabledChannels ??= AllChannels();
                if (Enum.TryParse(token[1..], true, out LoggingChannel channelToMute))
                {
                    enabledChannels.Remove(channelToMute);
                }

                continue;
            }

            if (!Enum.TryParse(token, true, out LoggingChannel channelToEnable))
            {
                continue;
            }

            enabledChannels ??= [];
            enabledChannels.Add(channelToEnable);
        }

        return enabledChannels is null ? All : new ChannelSelection(enabledChannels);
    }

    /// <summary>
    ///     Returns whether a channel is enabled.
    /// </summary>
    /// <param name="channel">The channel to test.</param>
    public bool IsEnabled(LoggingChannel channel)
    {
        return _enabledChannels.Contains(channel);
    }

    /// <summary>
    ///     Mutes channels from a comma-separated list. <c>all</c> clears the current selection. Unknown
    ///     channels leave the current selection unchanged and return an error.
    /// </summary>
    /// <param name="rawValue">A comma-separated list of channels to mute.</param>
    /// <param name="selection">The updated selection when successful.</param>
    /// <param name="errorMessage">The parse error when it fails.</param>
    public bool TryMuteCsv(string rawValue, out ChannelSelection selection, out string? errorMessage)
    {
        return TryApply(rawValue, false, out selection, out errorMessage);
    }

    /// <summary>
    ///     Unmutes channels from a comma-separated list. <c>all</c> enables every channel. Unknown
    ///     channels leave the current selection unchanged and return an error.
    /// </summary>
    /// <param name="rawValue">A comma-separated list of channels to unmute.</param>
    /// <param name="selection">The updated selection when successful.</param>
    /// <param name="errorMessage">The parse error when it fails.</param>
    public bool TryUnmuteCsv(string rawValue, out ChannelSelection selection, out string? errorMessage)
    {
        return TryApply(rawValue, true, out selection, out errorMessage);
    }

    /// <summary>
    ///     Returns the current channel list in a stable format. Full and empty selections become
    ///     <c>all</c> and <c>none</c>.
    /// </summary>
    public string Describe()
    {
        var allChannels = AllChannels();
        if (_enabledChannels.SetEquals(allChannels))
        {
            return "all";
        }

        if (_enabledChannels.Count == 0)
        {
            return "none";
        }

        return string.Join(',',
            _enabledChannels.OrderBy(static channel => channel.ToString(), StringComparer.OrdinalIgnoreCase)
                .Select(static channel => channel.ToString()));
    }

    private bool TryApply(string rawValue, bool unmute, out ChannelSelection selection, out string? errorMessage)
    {
        var updatedChannels = new HashSet<LoggingChannel>(_enabledChannels);
        foreach (var token in SplitTokens(rawValue))
        {
            if (string.Equals(token, "all", StringComparison.OrdinalIgnoreCase))
            {
                if (unmute)
                {
                    updatedChannels = AllChannels();
                }
                else
                {
                    updatedChannels.Clear();
                }

                continue;
            }

            if (!Enum.TryParse(token, true, out LoggingChannel channel))
            {
                selection = this;
                errorMessage = $"Unknown log channel '{token}'.";
                return false;
            }

            if (unmute)
            {
                updatedChannels.Add(channel);
            }
            else
            {
                updatedChannels.Remove(channel);
            }
        }

        selection = new ChannelSelection(updatedChannels);
        errorMessage = null;
        return true;
    }

    private static HashSet<LoggingChannel> AllChannels()
    {
        return Enum.GetValues<LoggingChannel>().ToHashSet();
    }

    private static string[] SplitTokens(string rawValue)
    {
        return rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
