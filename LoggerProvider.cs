namespace Xorog.Logger;

public sealed class LoggerProvider : ILoggerProvider
{
    internal LoggerProvider(LoggerClient logger)
    {
        _logger = logger;
    }


    private LoggerClient _logger { get; set; }

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
