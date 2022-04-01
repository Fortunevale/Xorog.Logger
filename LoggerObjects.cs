namespace Xorog.Logger;

public class LoggerObjects
{
    internal List<LogEntry> LogsToPost = new();

    public class LogEntry
    {
        public DateTime TimeOfEvent { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
    }

    public enum LogLevel
    {
        FATAL,
        ERROR,
        WARN,
        INFO,
        DEBUG,
        NONE
    }
}
