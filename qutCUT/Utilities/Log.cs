using Microsoft.Extensions.Logging;

namespace qutCUT.Utilities;

public static class Log
{
    private static ILoggerFactory? _factory;

    public static void Bootstrap(ILoggerFactory factory) => _factory = factory;

    public static ILogger App          => Get("app");
    public static ILogger Editor       => Get("editor");
    public static ILogger Export       => Get("export");
    public static ILogger Preview      => Get("preview");
    public static ILogger Agent        => Get("agent");
    public static ILogger Generation   => Get("generation");
    public static ILogger Project      => Get("project");
    public static ILogger Transcription => Get("transcription");
    public static ILogger Search       => Get("search");
    public static ILogger Mcp          => Get("mcp");

    private static ILogger Get(string category) =>
        _factory?.CreateLogger(category) ?? NullLogger(category);

    private static ILogger NullLogger(string category) =>
        Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
}
