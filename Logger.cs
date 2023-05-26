﻿using Newtonsoft.Json;

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
        filePath = filePath.Replace("\\", "/");

        var handler = new Logger();
        handler._provider = new(handler);

        if (handler.loggerStarted)
            throw new Exception($"The logger is already started");

        if (filePath is not "")
        {
            if (filePath.Contains('/'))
            {
                var dirPath = filePath[..filePath.LastIndexOf('/')];

                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
            }

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

                        string LogLevelText = $"{currentLog.LogLevel,-6}";

                        ConsoleColor LogLevelColor;
                        ConsoleColor BackgroundColor;

                        LogLevelColor = currentLog.LogLevel switch
                        {
                            LogLevel.TRACE => ConsoleColor.Gray,
                            LogLevel.DEBUG2 => ConsoleColor.Gray,
                            LogLevel.DEBUG => ConsoleColor.Gray,
                            LogLevel.INFO => ConsoleColor.Cyan,
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

                        string leftOver = currentLog.RawMessage;

                        foreach (var blacklistobject in handler.Blacklist)
                            leftOver = leftOver.Replace(blacklistobject, new String('*', blacklistobject.Length), StringComparison.CurrentCultureIgnoreCase);

                        int currentArg = 0;
                        bool inTemplate = false;
                        bool attemptedParsing = false;
                        List<StringPart> builder = new();
                        
                        while (leftOver.Length > 0)
                        {
                            if (inTemplate)
                            {
                                attemptedParsing = true;
                                if (currentLog.Args?.Length >= currentArg)
                                {
                                    int endIndex = leftOver.IndexOf('}');

                                    object objectToAdd = currentLog.Args[currentArg];
                                    currentArg++;

                                    if (objectToAdd is null)
                                        continue;

                                    if (objectToAdd.GetType() == typeof(int))
                                        builder.Add(new StringPart { String = objectToAdd.ToString(), Color = ConsoleColor.Magenta });
                                    else if (objectToAdd.GetType() == typeof(long))
                                        builder.Add(new StringPart { String = objectToAdd.ToString(), Color = ConsoleColor.Magenta });
                                    else if (objectToAdd.GetType() == typeof(uint))
                                        builder.Add(new StringPart { String = objectToAdd.ToString(), Color = ConsoleColor.Magenta });
                                    else if (objectToAdd.GetType() == typeof(ulong))
                                        builder.Add(new StringPart { String = objectToAdd.ToString(), Color = ConsoleColor.Magenta });
                                    else
                                        builder.Add(new StringPart { String = objectToAdd.ToString(), Color = ConsoleColor.Cyan });

                                    inTemplate = false;

                                    leftOver = leftOver[(endIndex + 1)..];
                                    attemptedParsing = false;
                                    continue; 
                                }
                            }

                            inTemplate = false;

                            int placeholderIndex = leftOver.IndexOf('{');

                            if (placeholderIndex != -1)
                                inTemplate = true;

                            if (placeholderIndex == -1 || placeholderIndex > leftOver.Length)
                                placeholderIndex = leftOver.Length;

                            if (placeholderIndex == 0 && attemptedParsing)
                                placeholderIndex = leftOver.Length;

                            var str = leftOver[..placeholderIndex];
                            if (!string.IsNullOrEmpty(str))
                                builder.Add(new StringPart { String = str });

                            leftOver = leftOver[placeholderIndex..];
                            attemptedParsing = false;
                        }

                        if (handler.maxLogLevel >= currentLog.LogLevel)
                        {
                            Console.ResetColor(); Console.Write($"[{currentLog.TimeOfEvent:dd.MM.yyyy HH:mm:ss:fff}] ");
                            Console.ForegroundColor = LogLevelColor; Console.BackgroundColor = BackgroundColor; Console.Write($"[{LogLevelText}]"); Console.ResetColor(); Console.Write(" ");

                            foreach (StringPart part in builder)
                            {
                                Console.ForegroundColor = part.Color ?? ConsoleColor.White;
                                Console.BackgroundColor = ConsoleColor.Black;

                                Console.Write($"{part.String}");
                            }
                            Console.ResetColor();
                            Console.WriteLine();

                            if (currentLog.Exception is not null)
                                try
                                {
                                    Console.WriteLine(JsonConvert.SerializeObject(currentLog.Exception, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine(currentLog.Exception);
                                }
                        }

                        currentLog.Message = string.Join("", builder.Select(x => x.String));

                        for (int i1 = 0; i1 < builder.Count; i1++)
                        {
                            builder[i1].Dispose();
                        }
                        builder.Clear();

                        _ = Task.Run(() =>
                        {
                            handler.LogRaised?.Invoke(null, new LogMessageEventArgs() { LogEntry = currentLog });
                        });

                        try
                        {
                            if (!handler.FileBlackList.Contains(currentLog.LogLevel))
                            {
                                Byte[] FileWrite = Encoding.UTF8.GetBytes($"[{currentLog.TimeOfEvent:dd.MM.yyyy HH:mm:ss:fff}] [{LogLevelText}] {currentLog.Message}\n{(currentLog.Exception is not null ? $"{currentLog.Exception}\n" : "")}");
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
                    handler.LogError("An exception occurred while trying to display a log message", ex);
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

        RunningLogger?.Dispose();

        RunningLogger = null;

        OpenedFile?.Close();
    }

    /// <summary>
    /// Add strings automatically censor on output to console and file.
    /// </summary>
    /// <param name="blacklist">The strings to censor</param>
    public void AddBlacklist(params string[] blacklist)
    {
        for (int i = 0; i < blacklist.Length; i++)
        {
            Blacklist.Add(blacklist[i]);
        }
    }

    /// <summary>
    /// Add blacklisted log level to not save
    /// </summary>
    /// <param name="levels">The log levels not to save to the log file</param>
    public void AddLogLevelBlacklist(params LogLevel[] levels)
    {
        for (int i = 0; i < levels.Length; i++)
        {
            FileBlackList.Add(levels[i]);
        }
    }

    /// <summary>
    /// Changes the log level
    /// </summary>
    /// <param name="level">The new log level to apply</param>
    public void ChangeLogLevel(LogLevel level) => maxLogLevel = level;

    /// <summary>
    /// Log with none log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogNone(string message, Exception? exception = null, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.NONE,
        RawMessage = message,
        Args = args,
        Exception = exception
    });

    /// <summary>
    /// Log with none log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    public void LogNone(string message, Exception? exception = null) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.NONE,
        RawMessage = message,
        Exception = exception
    });

    /// <summary>
    /// Log with none log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogNone(string message, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.NONE,
        RawMessage = message,
        Args = args
    });

    /// <summary>
    /// Log with trace log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogTrace(string message, Exception? exception = null, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.TRACE,
        RawMessage = message,
        Args = args,
        Exception = exception
    });

    /// <summary>
    /// Log with trace log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    public void LogTrace(string message, Exception? exception = null) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.TRACE,
        RawMessage = message,
        Exception = exception
    });

    /// <summary>
    /// Log with trace log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogTrace(string message, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.TRACE,
        RawMessage = message,
        Args = args,
    });

    /// <summary>
    /// Log with debug2 log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogDebug2(string message, Exception? exception = null, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.DEBUG2,
        RawMessage = message,
        Args = args,
        Exception = exception
    });

    /// <summary>
    /// Log with debug2 log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    public void LogDebug2(string message, Exception? exception = null) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.DEBUG2,
        RawMessage = message,
        Exception = exception
    });

    /// <summary>
    /// Log with debug2 log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogDebug2(string message, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.DEBUG2,
        RawMessage = message,
        Args = args
    });

    /// <summary>
    /// Log with debug log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogDebug(string message, Exception? exception = null, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.DEBUG,
        RawMessage = message,
        Args = args,
        Exception = exception
    });

    /// <summary>
    /// Log with debug log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    public void LogDebug(string message, Exception? exception = null) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.DEBUG,
        RawMessage = message,
        Exception = exception
    });

    /// <summary>
    /// Log with debug log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogDebug(string message, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.DEBUG,
        RawMessage = message,
        Args = args
    });

    /// <summary>
    /// Log with info log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogInfo(string message, Exception? exception = null, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.INFO,
        RawMessage = message,
        Args = args,
        Exception = exception
    });

    /// <summary>
    /// Log with info log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    public void LogInfo(string message, Exception? exception = null) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.INFO,
        RawMessage = message,
        Exception = exception
    });

    /// <summary>
    /// Log with info log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogInfo(string message, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.INFO,
        RawMessage = message,
        Args = args
    });

    /// <summary>
    /// Log with warn log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogWarn(string message, Exception? exception = null, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.WARN,
        RawMessage = message,
        Args = args,
        Exception = exception
    });

    /// <summary>
    /// Log with warn log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    public void LogWarn(string message, Exception? exception = null) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.WARN,
        RawMessage = message,
        Exception = exception
    });

    /// <summary>
    /// Log with warn log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogWarn(string message, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.WARN,
        RawMessage = message,
        Args = args
    });

    /// <summary>
    /// Log with error log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogError(string message, Exception? exception = null, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.ERROR,
        RawMessage = message,
        Args = args,
        Exception = exception
    });

    /// <summary>
    /// Log with error log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    public void LogError(string message, Exception? exception = null) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.ERROR,
        RawMessage = message,
        Exception = exception
    });

    /// <summary>
    /// Log with error log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogError(string message, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.ERROR,
        RawMessage = message,
        Args = args
    });

    /// <summary>
    /// Log with fatal log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogFatal(string message, Exception? exception = null, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.FATAL,
        RawMessage = message,
        Args = args,
        Exception = exception
    });

    /// <summary>
    /// Log with fatal log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="exception">The exception that was caused</param>
    public void LogFatal(string message, Exception? exception = null) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.FATAL,
        RawMessage = message,
        Exception = exception
    });

    /// <summary>
    /// Log with fatal log level
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="args">The objects involved in the event</param>
    public void LogFatal(string message, params object[] args) => LogsToPost.Add(new LogEntry
    {
        TimeOfEvent = DateTime.Now,
        LogLevel = LogLevel.FATAL,
        RawMessage = message,
        Args = args
    });

    /// <summary>
    /// Log with standard Microsoft.Extensions.Logging format
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="logLevel"></param>
    /// <param name="eventId"></param>
    /// <param name="state"></param>
    /// <param name="exception"></param>
    /// <param name="formatter"></param>
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => LogsToPost.Add(new LogEntry
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
            _ => LogLevel.NONE,
        },
        RawMessage = $"[{eventId.Id}] {formatter(state, exception)}",
        Exception = exception
    });

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) 
        => loggerStarted;

    public IDisposable BeginScope<TState>(TState state)
        => default!;
}
