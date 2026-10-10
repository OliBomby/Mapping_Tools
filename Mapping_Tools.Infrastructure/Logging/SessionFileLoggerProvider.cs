using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Mapping_Tools.Infrastructure.Logging;

/// <summary>
///     Writes .NET log events to one file per application session and removes old application logs.
/// </summary>
public sealed class SessionFileLoggerProvider : ILoggerProvider
{
    private static readonly TimeSpan maxAge = TimeSpan.FromDays(7);
    private readonly Lock gate = new();
    private readonly string directory;
    private readonly string startupTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
    private StreamWriter? writer;
    private string? currentPath;
    private bool disposed;

    /// <summary>Creates a retained file sink in the supplied Logs directory.</summary>
    /// <param name="directory">The directory that will contain Mapping Tools log files.</param>
    public SessionFileLoggerProvider(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        this.directory = directory;
        try
        {
            Directory.CreateDirectory(directory);
            Prune();
        }
        catch (Exception exception)
        {
            Trace.TraceError("Could not initialize Mapping Tools logs: {0}", exception);
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose()
    {
        lock (gate)
        {
            disposed = true;
            writer?.Dispose();
            writer = null;
        }
    }

    private void Write(LogLevel level, string category, EventId eventId, string message, Exception? exception)
    {
        lock (gate)
        {
            if (disposed) return;

            try
            {
                DateTimeOffset now = DateTimeOffset.Now;
                EnsureWriter();
                string timestamp = now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
                string singleLineMessage = message.Replace("\r", "\\r", StringComparison.Ordinal)
                    .Replace("\n", "\\n", StringComparison.Ordinal);
                string eventLabel = eventId.Id == 0 && string.IsNullOrEmpty(eventId.Name)
                    ? string.Empty
                    : $" ({eventId})";
                writer!.WriteLine($"{timestamp} [{level}] {category}{eventLabel}: {singleLineMessage}");
                if (exception is not null) writer.WriteLine(exception);
            }
            catch (Exception writeException)
            {
                Trace.TraceError("Could not write Mapping Tools log: {0}", writeException);
            }
        }
    }

    private void EnsureWriter()
    {
        if (writer is not null) return;

        Directory.CreateDirectory(directory);
        for (int collision = 0; ; collision++)
        {
            string prefix = collision == 0 ? startupTimestamp : $"{startupTimestamp}-{collision}";
            string path = Path.Combine(directory, $"{prefix}.runtime.log");
            if (!TryOpenNewFile(path, out var openedWriter)) continue;

            currentPath = path;
            writer = openedWriter;
            break;
        }

        Prune();
    }

    private static bool TryOpenNewFile(string path, out StreamWriter? openedWriter)
    {
        try
        {
            openedWriter = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            {
                AutoFlush = true,
            };
            return true;
        }
        catch (IOException) when (File.Exists(path))
        {
            openedWriter = null;
            return false;
        }
    }

    private void Prune()
    {
        try
        {
            var files = Directory.EnumerateFiles(directory, "*.log")
                .Select(path => new FileInfo(path))
                .Where(file => file.Name.StartsWith("mapping-tools-", StringComparison.Ordinal)
                               || IsSessionLog(file.Name))
                .ToArray();
            var cutoff = DateTime.UtcNow - maxAge;
            foreach (var file in files)
            {
                if (file.FullName == currentPath) continue;
                if (file.LastWriteTimeUtc < cutoff) file.Delete();
            }
        }
        catch (Exception exception)
        {
            Trace.TraceError("Could not prune Mapping Tools logs: {0}", exception);
        }
    }

    private static bool IsSessionLog(string name)
    {
        if (!name.EndsWith(".runtime.log", StringComparison.Ordinal)) return false;

        string stem = name[..^".runtime.log".Length];
        string timestamp = stem.Split(['-', '.'])[0];
        return long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out _);
    }

    private sealed class FileLogger(SessionFileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            try
            {
                provider.Write(logLevel, category, eventId, formatter(state, exception), exception);
            }
            catch (Exception loggingException)
            {
                Trace.TraceError("Could not format Mapping Tools log: {0}", loggingException);
            }
        }
    }

    private sealed class NullScope : IDisposable
    {
        internal static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
