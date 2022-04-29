namespace Xorog.Logger;

public class LoggerObjects
{
    internal List<LogEntry> LogsToPost = new();
    internal List<string> Blacklist = new();

    public class LogEntry
    {
        public DateTime TimeOfEvent { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
        public Exception? Exception { get; set; }
    }

    public enum LogLevel
    {
        NONE,
        FATAL,
        ERROR,
        WARN,
        INFO,
        DEBUG,
        DEBUG2,
        TRACE
    }
}
