using System.Text;
using Game.Runtime;
using Game.Shared.Assets;
using Game.Shared.Diagnostics;
using Game.Shared.Text;

namespace Game.Text;

/// <summary>
///     Runtime-owned string catalog loaded from source assets and optional language overlays.
/// </summary>
internal sealed class GameStringCatalog
{
    private static readonly byte[] EmptyCp437String = [0];

    private static readonly StringId[] InteractionResponseStringIds =
    [
        StringId.Interaction_Inspect,
        StringId.Interaction_Use,
        StringId.Interaction_OpenClose,
        StringId.Interaction_GoTo,
        StringId.Interaction_Read,
        StringId.Interaction_TakeFrom,
        StringId.Interaction_Write,
        StringId.Interaction_Buy,
        StringId.Interaction_SitStand,
        StringId.Interaction_Save,
        StringId.Interaction_With,
        StringId.Interaction_ConfirmYes,
        StringId.Interaction_ConfirmNo,
        StringId.Interaction_Blank0,
        StringId.Interaction_Blank1,
        StringId.Interaction_Blank2,
        StringId.Interaction_Blank3,
        StringId.Interaction_EmptySelection,
        StringId.Interaction_EmptySelection,
        StringId.Interaction_EmptySelection,
        StringId.Interaction_EmptySelection,
        StringId.Interaction_EmptySelection,
        StringId.Interaction_EmptySelection
    ];

    private static readonly StringId[] TransitionTextStringIds =
    [
        StringId.Panel_LabelNote,
        StringId.Panel_LabelCertificate,
        StringId.Panel_LabelLetter,
        StringId.Panel_LabelBrochure,
        StringId.Panel_LabelKey,
        StringId.Panel_LabelPhoneBook,
        StringId.Panel_LabelWhisk,
        StringId.Panel_LabelCream,
        StringId.Panel_LabelBallpointPen,
        StringId.Panel_LabelWallet,
        StringId.Panel_LabelLighter,
        StringId.Panel_LabelBicyclePump,
        StringId.Panel_LabelStrawberries,
        StringId.Panel_LabelWritingPaper,
        StringId.Panel_LabelStamp,
        StringId.Panel_LabelCarKey
    ];

    private readonly Dictionary<ushort, StringId> _idsBySourceOffset;
    private readonly StringTable<StringId> _strings;
    private readonly Erbe _runtime;
    private readonly Dictionary<StringId, ushort> _sourceOffsetsById;

    /// <summary>
    ///     Creates and loads the runtime string catalog for the supplied runtime.
    /// </summary>
    /// <param name="runtime">Runtime that owns the source resources and language selection.</param>
    internal GameStringCatalog(Erbe runtime)
    {
        _runtime = runtime;
        _sourceOffsetsById = new Dictionary<StringId, ushort>(runtime.BootData.SourceOffsetsByStringId);
        _idsBySourceOffset = new Dictionary<ushort, StringId>(runtime.BootData.StringIdsBySourceOffset);
        _strings = LoadStrings(runtime);
    }

    private StringTable<StringId> LoadStrings(Erbe runtime)
    {
        var effectiveStrings = new Dictionary<StringId, string>(runtime.BootData.OriginalStrings);
        var overlayStrings = LoadOverlayStrings();
        foreach (var entry in overlayStrings)
        {
            effectiveStrings[entry.Key] = entry.Value;
        }

        return new StringTable<StringId>(effectiveStrings);
    }

    /// <summary>
    ///     Gets the startup intro pages in display order.
    /// </summary>
    internal IReadOnlyList<StringId> IntroPages { get; } =
    [
        StringId.Intro_Page1,
        StringId.Intro_Page2,
        StringId.Intro_Page3,
        StringId.Intro_Page4,
        StringId.Intro_Page5
    ];

    /// <summary>
    ///     Gets the effective text encoded as null-terminated CP437 bytes.
    /// </summary>
    /// <param name="id">String id to resolve.</param>
    internal ReadOnlySpan<byte> GetCp437String(StringId id)
    {
        if (id == StringId.None)
        {
            return EmptyCp437String;
        }

        return _strings.GetCp437String(id);
    }

    /// <summary>
    ///     Gets the original source offset for diagnostics, raw descriptor storage/projection, and dump tooling.
    /// </summary>
    /// <param name="id">String id to inspect.</param>
    internal ushort GetSourceOffset(StringId id)
    {
        return id == StringId.None ? (ushort)0 : _sourceOffsetsById[id];
    }

    /// <summary>
    ///     Resolves an original source DGROUP offset to a runtime string id.
    /// </summary>
    /// <param name="sourceOffset">Original source DGROUP offset.</param>
    internal StringId ResolveSourceOffset(ushort sourceOffset)
    {
        if (sourceOffset == 0)
        {
            return StringId.None;
        }

        return _idsBySourceOffset.TryGetValue(sourceOffset, out var id)
            ? id
            : throw new InvalidOperationException(
                $"Source string offset 0x{sourceOffset:X4} is not represented by {nameof(StringId)}.");
    }

    /// <summary>
    ///     Reads one interaction response string id from the original response source-offset table.
    /// </summary>
    /// <param name="responseTextIndex">Interaction response text index.</param>
    internal static StringId ReadInteractionResponseStringId(ushort responseTextIndex)
    {
        if (responseTextIndex >= InteractionResponseStringIds.Length)
        {
            throw new InvalidOperationException(
                $"Interaction response text entry {responseTextIndex} exceeds the response text table.");
        }

        return InteractionResponseStringIds[responseTextIndex];
    }

    /// <summary>
    ///     Reads one transition-panel string id from the original transition source-offset table.
    /// </summary>
    /// <param name="textEntryIndex">Transition text-entry index.</param>
    internal static StringId ReadTransitionTextStringId(short textEntryIndex)
    {
        if ((ushort)textEntryIndex >= TransitionTextStringIds.Length)
        {
            throw new InvalidOperationException(
                $"Transition text entry {textEntryIndex} exceeds the 16-entry state-transition text table.");
        }

        return TransitionTextStringIds[textEntryIndex];
    }

    /// <summary>
    ///     Formats a string id and source offset for diagnostics.
    /// </summary>
    /// <param name="id">String id to format.</param>
    internal string FormatStringId(StringId id)
    {
        return id == StringId.None ? "None" : $"{id}@0x{GetSourceOffset(id):X4}";
    }

    private Dictionary<StringId, string> LoadOverlayStrings()
    {
        var strings = new Dictionary<StringId, string>();
        var language = _runtime.Language;
        if (string.IsNullOrWhiteSpace(language))
        {
            return strings;
        }

        var overlayPath = $"{language}.txt";
        if (!_runtime.Resources.TryResolve(overlayPath, out var entry))
        {
            GameLog.Warning(LoggingChannel.Files,
                $"String overlay '{overlayPath}' was requested but could not be resolved; using original text.");
            return strings;
        }

        string overlayText;
        using (var reader = new StreamReader(entry.OpenRead(), Encoding.UTF8, true))
        {
            overlayText = reader.ReadToEnd();
        }

        var lineNumber = 0;
        foreach (var rawLine in overlayText.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                LogOverlayIssue(overlayPath, lineNumber, "missing key/value separator");
                continue;
            }

            var keyText = line[..separatorIndex].Trim();
            if (!Enum.TryParse<StringId>(keyText, true, out var id) || id == StringId.None)
            {
                LogOverlayIssue(overlayPath, lineNumber, $"unknown string id '{keyText}'");
                continue;
            }

            if (!TryReadQuotedOverlayValue(line[(separatorIndex + 1)..], overlayPath, lineNumber, out var escapedValue))
            {
                continue;
            }

            var value = UnescapeOverlayValue(escapedValue, overlayPath, lineNumber);
            if (value.EndsWith('\0'))
            {
                LogOverlayIssue(overlayPath, lineNumber, "trimmed trailing null terminator from overlay value");
                value = value.TrimEnd('\0');
            }

            if (value.Contains('\0', StringComparison.Ordinal))
            {
                LogOverlayIssue(overlayPath, lineNumber, "removed embedded null terminator from overlay value");
                value = value.Replace("\0", string.Empty, StringComparison.Ordinal);
            }

            var roundTrip = Cp437.Decode(Cp437.Encode(value));
            if (!string.Equals(roundTrip, value, StringComparison.Ordinal))
            {
                LogOverlayIssue(overlayPath,
                    lineNumber,
                    $"value for {id} contains characters that are not exactly representable in CP437");
            }

            strings[id] = value;
        }

        return strings;
    }

    private static bool TryReadQuotedOverlayValue(string rawValue,
        string overlayPath,
        int lineNumber,
        out string escapedValue)
    {
        escapedValue = string.Empty;
        var openingQuoteIndex = 0;
        while (openingQuoteIndex < rawValue.Length && char.IsWhiteSpace(rawValue[openingQuoteIndex]))
        {
            openingQuoteIndex++;
        }

        if (openingQuoteIndex >= rawValue.Length || rawValue[openingQuoteIndex] != '"')
        {
            LogOverlayIssue(overlayPath, lineNumber, "overlay value must be enclosed in double quotes");
            return false;
        }

        var closingQuoteIndex = FindClosingOverlayQuote(rawValue, openingQuoteIndex);
        if (closingQuoteIndex < 0)
        {
            LogOverlayIssue(overlayPath, lineNumber, "overlay value is missing its closing double quote");
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rawValue[(closingQuoteIndex + 1)..]))
        {
            LogOverlayIssue(overlayPath, lineNumber, "ignored trailing text after closing double quote");
        }

        escapedValue = rawValue[(openingQuoteIndex + 1)..closingQuoteIndex];
        return true;
    }

    private static int FindClosingOverlayQuote(string value, int openingQuoteIndex)
    {
        var escaped = false;
        for (var index = openingQuoteIndex + 1; index < value.Length; index++)
        {
            var character = value[index];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            switch (character)
            {
                case '\\':
                    escaped = true;
                    continue;
                case '"':
                    return index;
            }
        }

        return -1;
    }

    private static string UnescapeOverlayValue(string value, string overlayPath, int lineNumber)
    {
        var builder = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (character != '\\' || index + 1 >= value.Length)
            {
                builder.Append(character);
                continue;
            }

            index++;
            switch (value[index])
            {
                case '\\':
                    builder.Append('\\');
                    break;
                case '"':
                    builder.Append('"');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case '0':
                    builder.Append('\0');
                    break;
                default:
                    LogOverlayIssue(overlayPath, lineNumber, $"unknown escape '\\{value[index]}'");
                    builder.Append(value[index]);
                    break;
            }
        }

        return builder.ToString();
    }

    private static void LogOverlayIssue(string overlayPath, int lineNumber, string issue)
    {
        GameLog.Warning(LoggingChannel.Files, $"String overlay '{overlayPath}' line {lineNumber}: {issue}.");
    }
}
