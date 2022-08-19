namespace Xorog.Logger;

#pragma warning disable IDE1006 // Naming Styles

public class Logger : ILogger
{
    internal Logger() { }

    public LoggerProvider _provider { get; internal set; }

    private bool loggerStarted = false;
    private LogLevel maxLogLevel = LogLevel.DEBUG;

    private string FileName = "";
    private FileStream OpenedFile { get; set; }

    internal List<LogEntry> LogsToPost = new();
    internal List<string> Blacklist = new();
    internal List<LogLevel> FileBlackList = new();

    private Task RunningLogger = null;

    public event EventHandler<LogMessageEventArgs> LogRaised;


    /// <summary>
    /// Starts the logger with specified settings
    /// </summary>
    /// <param name="filePath">Where the current logs should be saved to, leave blank if logs shouldnt be saved</param>
    /// <param name="level">The loglevel that should be displayed in the console, does not affect whats written to file</param>
    /// <param name="cleanUpBefore">Clean up old logs before a datetime</param>
    /// <returns>A bool stating if the logger was started</returns>
    public static Logger StartLogger(string filePath = "", LogLevel level = LogLevel.DEBUG, DateTime cleanUpBefore = new DateTime(), bool ThrowOnFailedDeletion = false)
    {
        var handler = new Logger();
        handler._provider = new(handler);

        if (handler.loggerStarted)
            throw new Exception($"The logger is already started");

        if (filePath is not "")
        {
            handler.FileName = filePath;
            handler.OpenedFile = File.Open(handler.FileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
        }

        handler.loggerStarted = true;
        handler.maxLogLevel = level;

        if (cleanUpBefore != new DateTime())
        {
            foreach (var b in Directory.GetFiles(new FileInfo(filePath).Directory.FullName))
            {
                try
                {
                    FileInfo fi = new(b);
                    if (fi.CreationTimeUtc < cleanUpBefore)
                    {
                        fi.Delete();
                        handler.LogDebug($"{fi.Name} deleted");
                    }
                }
                catch (Exception ex)
                {
                    if (!ThrowOnFailedDeletion)
                        handler.LogError( $"Couldn't delete log file {b}", ex);
                    else
                        throw new Exception($"Failed to delete {b}: {ex}");
                }
            }
        }

        handler.RunningLogger = Task.Run(async () =>
        {
            while (handler.loggerStarted)
            {
                try
                {
                    while (handler.LogsToPost.Count == 0)
                    {
                        Thread.Sleep(10);
                    }

                    for (int i = 0; i < handler.LogsToPost.Count; i++)
                    {
                        var currentLog = handler.LogsToPost[0];
                        handler.LogsToPost.Remove(currentLog);


                        if (currentLog is null)
                        {
                            continue;
                        }

                        string LogLevelText = currentLog.LogLevel.ToString();

                        if (LogLevelText.Length < 6)
                            LogLevelText += new string(' ', 6 - LogLevelText.Length);

                        ConsoleColor LogLevelColor;
                        ConsoleColor BackgroundColor;

                        LogLevelColor = currentLog.LogLevel switch
                        {
                            LogLevel.TRACE => ConsoleColor.Gray,
                            LogLevel.DEBUG2 => ConsoleColor.Gray,
                            LogLevel.DEBUG => ConsoleColor.Gray,
                            LogLevel.INFO => ConsoleColor.Green,
                            LogLevel.WARN => ConsoleColor.Yellow,
                            LogLevel.ERROR => ConsoleColor.Red,
                            LogLevel.FATAL => ConsoleColor.Black,
                            _ => ConsoleColor.Gray
                        };

                        BackgroundColor = currentLog.LogLevel switch
                        {
                            LogLevel.FATAL => ConsoleColor.DarkRed,
                            _ => ConsoleColor.Black
                        };

                        string LogMessage = currentLog.Message;

                        foreach (var blacklistobject in handler.Blacklist)
                            LogMessage = LogMessage.Replace(blacklistobject, new String('*', blacklistobject.Length), StringComparison.CurrentCultureIgnoreCase);

                        if (handler.maxLogLevel >= currentLog.LogLevel)
                        {
                            Console.ResetColor(); Console.Write($"[{currentLog.TimeOfEvent:dd.MM.yyyy HH:mm:ss:fff}] ");
                            Console.ForegroundColor = LogLevelColor; Console.BackgroundColor = BackgroundColor; Console.Write($"[{LogLevelText}]");
                            Console.ResetColor(); Console.WriteLine($" {LogMessage}");

                            if (currentLog.Exception is not null)
                                Console.WriteLine(currentLog.Exception);
                        }

                        _ = Task.Run(() =>
                        {
                            handler.LogRaised?.Invoke(null, new LogMessageEventArgs() { LogEntry = currentLog });
                        });

                        try
                        {
                            if (!handler.FileBlackList.Contains(currentLog.LogLevel))
                            {
                                Byte[] FileWrite = Encoding.UTF8.GetBytes($"[{currentLog.TimeOfEvent:dd.MM.yyyy HH:mm:ss:fff}] [{LogLevelText}] {LogMessage}\n{(currentLog.Exception is not null ? $"{currentLog.Exception}\n" : "")}");
                                if (handler.OpenedFile != null)
                                {
                                    await handler.OpenedFile.WriteAsync(FileWrite.AsMemory(0, FileWrite.Length));
                                    handler.OpenedFile.Flush();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            handler.LogFatal($"Couldn't write log to file: {ex}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    handler.LogError("An exception occured while trying to display a log message", ex);
                    await Task.Delay(1000);
                    continue;
                }
            }
        });

        return handler;
    }



    /// <summary>
    /// Stops the logger
    /// </summary>
    public void StopLogger()
    {
        loggerStarted = false;
        maxLogLevel = LogLevel.DEBUG;
        FileName = "";

        Thread.Sleep(500);

        if (RunningLogger is not null)
            RunningLogger.Dispose();

        RunningLogger = null;

        if (OpenedFile is not null)
            OpenedFile.Close();
    }



    /// <summary>
    /// Add blacklisted string to censor automatically
    /// </summary>
    /// <param name="blacklist"></param>
    public void AddBlacklist(string blacklist)
    {
        Blacklist.Add(blacklist);
    }
    
    /// <summary>
    /// Add blacklisted log level to not save
    /// </summary>
    /// <param name="blacklist"></param>
    public void AddLogLevelBlacklist(LogLevel level)
    {
        FileBlackList.Add(level);
    }



    /// <summary>
    /// Changes the log level
    /// </summary>
    /// <param name="level"></param>
    public void ChangeLogLevel(LogLevel level)
    {
        maxLogLevel = level;
    }



    /// <summary>
    /// Log with none log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void LogNone(string message, Exception? exception = null)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LogLevel.NONE,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with trace log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void LogTrace(string message, Exception? exception = null)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LogLevel.TRACE,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with debug2 log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void LogDebug2(string message, Exception? exception = null)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LogLevel.DEBUG2,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with debug log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void LogDebug(string message, Exception? exception = null)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LogLevel.DEBUG,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with info log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void LogInfo(string message, Exception? exception = null)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LogLevel.INFO,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with warn log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void LogWarn(string message, Exception? exception = null)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LogLevel.WARN,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with error log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void LogError(string message, Exception? exception = null)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LogLevel.ERROR,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with fatal log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public void LogFatal(string message, Exception? exception = null)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LogLevel.FATAL,
            Message = message,
            Exception = exception
        });
    }


    /// <summary>
    /// Log with standard Microsoft.Extensions.Logging format
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="logLevel"></param>
    /// <param name="eventId"></param>
    /// <param name="state"></param>
    /// <param name="exception"></param>
    /// <param name="formatter"></param>
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LogsToPost.Add(new LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.Debug => LogLevel.DEBUG2,
                Microsoft.Extensions.Logging.LogLevel.Trace => LogLevel.TRACE2,
                Microsoft.Extensions.Logging.LogLevel.Information => LogLevel.INFO,
                Microsoft.Extensions.Logging.LogLevel.Warning => LogLevel.WARN,
                Microsoft.Extensions.Logging.LogLevel.Error => LogLevel.ERROR,
                Microsoft.Extensions.Logging.LogLevel.Critical => LogLevel.FATAL,
                Microsoft.Extensions.Logging.LogLevel.None => LogLevel.NONE,
                _ => throw new NotImplementedException()
            },
            Message = $"[{eventId.Id,2}] {formatter(state, exception)}",
            Exception = exception
        });
    }



    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        return loggerStarted;
    }



    public IDisposable BeginScope<TState>(TState state)
    {
        return default!;
    }
}
