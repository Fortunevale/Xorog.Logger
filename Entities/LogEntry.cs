namespace Xorog.Logger.Entities;

public class LogEntry
{
    public DateTime TimeOfEvent { get; set; }
    public LogLevel LogLevel { get; set; }
    public string Message { get; set; }
    public Exception? Exception { get; set; }
}