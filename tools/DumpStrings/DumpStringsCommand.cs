using System.Reflection;
using System.Text;
using Game.Text;
using Game.Shared.Executable;
using Game.Shared.Resources;
using Game.Shared.Text;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
///     Dumps the original game strings as a quoted language overlay file.
/// </summary>
internal sealed class DumpStringsCommand : Command<DumpStringsSettings>
{
    private const ushort FirstSourceStringOffset = 0x04B9;
    private const ushort LastSourceStringOffset = 0x5810;

    /// <inheritdoc />
    public override int Execute(CommandContext context, DumpStringsSettings settings)
    {
        _ = context;

        var executablePath = Path.GetFullPath(settings.ExecutablePath);
        var output = DumpOverlayLines(executablePath);

        if (string.IsNullOrWhiteSpace(settings.OutputPath))
        {
            Console.Write(output);
            return 0;
        }

        var outputPath = Path.GetFullPath(settings.OutputPath);
        File.WriteAllText(outputPath, output, new UTF8Encoding(false));
        AnsiConsole.MarkupLine($"[green]Wrote string overlay dump:[/] {outputPath}");
        return 0;
    }

    private static string DumpOverlayLines(string executablePath)
    {
        var fileInfo = new FileInfo(executablePath);
        var resourceEntry = ResourceEntry.FromFile(fileInfo.FullName, fileInfo.Length);
        var image = MzExecutableImage.From(resourceEntry);
        var source = image.ReadMemoryByDgroupRange(FirstSourceStringOffset, LastSourceStringOffset, "Game string source span")
            .Span;
        var builder = new StringBuilder();
        string? previousSection = null;

        foreach (var entry in EnumerateStringIds())
        {
            var section = GetSectionName(entry.Id);
            if (previousSection is not null && !string.Equals(section, previousSection, StringComparison.Ordinal))
            {
                builder.AppendLine();
            }

            previousSection = section;
            builder.Append(entry.Id);
            builder.Append("=\"");
            builder.Append(EscapeOverlayValue(ReadString(source, entry)));
            builder.AppendLine("\"");
        }

        return builder.ToString();
    }

    private static IEnumerable<StringEntry> EnumerateStringIds()
    {
        return typeof(StringId).GetFields(BindingFlags.Public | BindingFlags.Static)
            .OrderBy(static field => field.MetadataToken)
            .Select(static field => new
            {
                Id = (StringId)field.GetValue(null)!,
                Attribute = field.GetCustomAttribute<StringSourceOffsetAttribute>()
            })
            .Where(static entry => entry is { Id: not StringId.None, Attribute: not null })
            .Select(static entry => new StringEntry(entry.Id, entry.Attribute!.Offset));
    }

    private static string GetSectionName(StringId id)
    {
        var name = id.ToString();
        var separatorIndex = name.IndexOf('_', StringComparison.Ordinal);
        return separatorIndex < 0 ? name : name[..separatorIndex];
    }

    private static string ReadString(ReadOnlySpan<byte> source, StringEntry entry)
    {
        var relativeOffset = entry.SourceOffset - FirstSourceStringOffset;
        if ((uint)relativeOffset >= source.Length)
        {
            throw new InvalidOperationException(
                $"{entry.Id} source offset 0x{entry.SourceOffset:X4} falls outside the loaded game string source span.");
        }

        var slice = source[relativeOffset..];
        var terminatorIndex = slice.IndexOf((byte)0);
        if (terminatorIndex < 0)
        {
            throw new InvalidOperationException(
                $"{entry.Id} source offset 0x{entry.SourceOffset:X4} is not null-terminated inside the loaded source span.");
        }

        return Cp437.Decode(slice[..terminatorIndex]);
    }

    private static string EscapeOverlayValue(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\':
                    builder.Append(@"\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\n':
                    builder.Append(@"\n");
                    break;
                case '\r':
                    builder.Append(@"\r");
                    break;
                case '\t':
                    builder.Append(@"\t");
                    break;
                case '\0':
                    builder.Append(@"\0");
                    break;
                default:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }

    private readonly record struct StringEntry(StringId Id, ushort SourceOffset);
}
