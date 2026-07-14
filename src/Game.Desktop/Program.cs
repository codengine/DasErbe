using Game.Desktop;
using Game.Shared.Diagnostics;

if (!GameHostCommandLine.TryParse(args, out var options))
{
    return 1;
}

if (!DesktopLogOptionsResolver.TryCreate(options, out var logOptions, out var logErrorMessage))
{
    GameHostCommandLineParser.WriteRunHelp(Console.Error, logErrorMessage);
    return 1;
}

var logger = SerilogGameLogFactory.CreateLogger("Das Erbe", logOptions);
GameLog.Initialize(logger, logOptions);
GameLog.Write(LoggingChannel.Program, "Starting desktop host.");

try
{
    if (!DesktopRuntimeFactory.TryCreate(options, out var runtime, out var errorMessage))
    {
        GameLog.Error(LoggingChannel.Program, $"Runtime creation failed. {errorMessage}");
        Console.Error.WriteLine(errorMessage);
        return 1;
    }

    using var host = new DesktopGameHost(runtime, "Das Erbe");
    host.Run();
    return 0;
}
finally
{
    GameLog.Shutdown();
}
