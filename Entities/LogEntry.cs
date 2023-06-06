namespace Xorog.Logger;

public class LogEntry
{
    internal string RawMessage { get; set; }

    public DateTime TimeOfEvent { get; set; }
    public CustomLogLevel LogLevel { get; set; }
    public string Message { get; set; }
    public object[] Args { get; set; }
    public Exception? Exception { get; set; }
}