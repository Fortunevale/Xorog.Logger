namespace Xorog.Logger;

public class LogMessageEventArgs : EventArgs
{
    public LoggerObjects.LogEntry LogEntry { get; set; }

}