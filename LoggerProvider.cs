namespace Xorog.Logger;

public sealed class LoggerProvider : ILoggerProvider
{
    internal LoggerProvider(LoggerClient logger) 
        => this._logger = logger;


    private LoggerClient _logger { get; set; }

    public ILogger CreateLogger(string categoryName) 
        => this._logger;

    public void Dispose() 
        => GC.SuppressFinalize(this);
}
