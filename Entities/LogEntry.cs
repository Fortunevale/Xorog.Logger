namespace Xorog.Logger;

public class LogEntry
{
    internal LogEntry() { }

    internal string RawMessage { get; set; }

    /// <summary>
    /// The time of the event.
    /// </summary>
    public DateTime TimeOfEvent { get; internal set; }

    /// <summary>
    /// The severity of the event.
    /// </summary>
    public CustomLogLevel LogLevel { get; internal set; }

    /// <summary>
    /// The message describing the event.
    /// </summary>
    public string Message { get; internal set; }

    /// <summary>
    /// Any objects involved in creating the event message.
    /// </summary>
    public object[] Args { get; internal set; } = Array.Empty<object>();

    /// <summary>
    /// The exception that's been caused.
    /// </summary>
    public Exception? Exception { get; internal set; }
}