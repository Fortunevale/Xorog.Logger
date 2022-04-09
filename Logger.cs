using System.Text;
using System.IO;
using static Xorog.Logger.LoggerObjects;
using Microsoft.Extensions.Logging;

namespace Xorog.Logger;

public class Logger : ILogger
{
    private static bool loggerStarted = false;
    private static LoggerObjects.LogLevel maxLogLevel = LoggerObjects.LogLevel.DEBUG;

    private static string FileName = "";
    private static FileStream OpenedFile { get; set; }

    private readonly static LoggerObjects _loggerObjects = new();

    private static Task RunningLogger = null;

    public static event EventHandler<LogMessageEventArgs> LogRaised;


    /// <summary>
    /// Starts the logger with specified settings
    /// </summary>
    /// <param name="filePath">Where the current logs should be saved to, leave blank if logs shouldnt be saved</param>
    /// <param name="level">The loglevel that should be displayed in the console, does not affect whats written to file</param>
    /// <param name="cleanUpBefore">Clean up old logs before a datetime</param>
    /// <returns>A bool stating if the logger was started</returns>
    public static ILogger StartLogger(string filePath = "", LoggerObjects.LogLevel level = LoggerObjects.LogLevel.DEBUG, DateTime cleanUpBefore = new DateTime(), bool ThrowOnFailedDeletion = false)
    {
        if (loggerStarted)
            throw new Exception($"The logger is already started");

        if (filePath is not "")
        {
            FileName = filePath;
            OpenedFile = File.Open(FileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
        }

        loggerStarted = true;
        maxLogLevel = level;

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
                        LogDebug($"{fi.Name} deleted");
                    }
                }
                catch (Exception ex)
                {
                    if (!ThrowOnFailedDeletion)
                        LogError( $"Couldn't delete log file {b}", ex);
                    else
                        throw new Exception($"Failed to delete {b}: {ex}");
                }
            }
        }

        RunningLogger = Task.Run(async () =>
        {
            while (loggerStarted)
            {
                try
                {
                    if (_loggerObjects.LogsToPost.Count == 0)
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    foreach (var b in _loggerObjects.LogsToPost.ToList())
                    {
                        if (b == null || b.Message == null)
                        {
                            LogWarn($"Missed log message due to garbage collection");
                            _loggerObjects.LogsToPost.Remove(b);
                            continue;
                        }

                        string LogLevelText = b.LogLevel.ToString();

                        if (LogLevelText.Length < 6)
                            LogLevelText += new string(' ', 6 - LogLevelText.Length);

                        ConsoleColor LogLevelColor = ConsoleColor.Gray;

                        LogLevelColor = b.LogLevel switch
                        {
                            LoggerObjects.LogLevel.DEBUG => ConsoleColor.Gray,
                            LoggerObjects.LogLevel.INFO => ConsoleColor.Green,
                            LoggerObjects.LogLevel.WARN => ConsoleColor.Yellow,
                            LoggerObjects.LogLevel.ERROR => ConsoleColor.Red,
                            LoggerObjects.LogLevel.FATAL => ConsoleColor.DarkRed,
                            _ => ConsoleColor.Gray
                        };

                        string LogMessage = b.Message;

                        foreach (var blacklistobject in _loggerObjects.Blacklist)
                            LogMessage = LogMessage.Replace(blacklistobject, new String('*', blacklistobject.Length), StringComparison.CurrentCultureIgnoreCase);

                        if (b.LogLevel == LoggerObjects.LogLevel.TRACE)
                        {
                            if (maxLogLevel == LoggerObjects.LogLevel.TRACE)
                            {
                                Console.ResetColor(); Console.Write($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] ");
                                Console.ForegroundColor = LogLevelColor; Console.Write($"[{LogLevelText}] ");
                                Console.ResetColor(); Console.WriteLine(LogMessage);

                                if (b.Exception is not null)
                                    Console.WriteLine(b.Exception.ToString());
                            }
                        }
                        else if (b.LogLevel == LoggerObjects.LogLevel.DEBUG2)
                        {
                            if (maxLogLevel is LoggerObjects.LogLevel.DEBUG2 or LoggerObjects.LogLevel.TRACE)
                            {
                                Console.ResetColor(); Console.Write($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] ");
                                Console.ForegroundColor = LogLevelColor; Console.Write($"[{LogLevelText}] ");
                                Console.ResetColor(); Console.WriteLine(LogMessage);

                                if (b.Exception is not null)
                                    Console.WriteLine(b.Exception.ToString());
                            }
                        }
                        else if (b.LogLevel == LoggerObjects.LogLevel.DEBUG)
                        {
                            if (maxLogLevel is LoggerObjects.LogLevel.DEBUG or LoggerObjects.LogLevel.DEBUG2 or LoggerObjects.LogLevel.TRACE)
                            {
                                Console.ResetColor(); Console.Write($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] ");
                                Console.ForegroundColor = LogLevelColor; Console.Write($"[{LogLevelText}] ");
                                Console.ResetColor(); Console.WriteLine(LogMessage);

                                if (b.Exception is not null)
                                    Console.WriteLine(b.Exception.ToString());
                            }
                        }
                        else if (b.LogLevel == LoggerObjects.LogLevel.INFO)
                        {
                            if (maxLogLevel is LoggerObjects.LogLevel.DEBUG or LoggerObjects.LogLevel.INFO or LoggerObjects.LogLevel.DEBUG2 or LoggerObjects.LogLevel.TRACE)
                            {
                                Console.ResetColor(); Console.Write($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] ");
                                Console.ForegroundColor = LogLevelColor; Console.Write($"[{LogLevelText}] ");
                                Console.ResetColor(); Console.WriteLine(LogMessage);

                                if (b.Exception is not null)
                                    Console.WriteLine(b.Exception.ToString());
                            }
                        }
                        else if (b.LogLevel == LoggerObjects.LogLevel.WARN)
                        {
                            if (maxLogLevel is LoggerObjects.LogLevel.DEBUG or LoggerObjects.LogLevel.INFO or LoggerObjects.LogLevel.WARN or LoggerObjects.LogLevel.DEBUG2 or LoggerObjects.LogLevel.TRACE)
                            {
                                Console.ResetColor(); Console.Write($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] ");
                                Console.ForegroundColor = LogLevelColor; Console.Write($"[{LogLevelText}] ");
                                Console.ResetColor(); Console.WriteLine(LogMessage);

                                if (b.Exception is not null)
                                    Console.WriteLine(b.Exception.ToString());
                            }
                        }
                        else if (b.LogLevel == LoggerObjects.LogLevel.ERROR)
                        {
                            if (maxLogLevel is LoggerObjects.LogLevel.DEBUG or LoggerObjects.LogLevel.INFO or LoggerObjects.LogLevel.WARN or LoggerObjects.LogLevel.ERROR or LoggerObjects.LogLevel.DEBUG2 or LoggerObjects.LogLevel.TRACE)
                            {
                                Console.ResetColor(); Console.Write($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] ");
                                Console.ForegroundColor = LogLevelColor; Console.Write($"[{LogLevelText}] ");
                                Console.ResetColor(); Console.WriteLine(LogMessage);

                                if (b.Exception is not null)
                                    Console.WriteLine(b.Exception.ToString());
                            }
                        }
                        else if (b.LogLevel == LoggerObjects.LogLevel.FATAL && maxLogLevel >= LoggerObjects.LogLevel.FATAL)
                        {
                            if (maxLogLevel is LoggerObjects.LogLevel.DEBUG or LoggerObjects.LogLevel.INFO or LoggerObjects.LogLevel.WARN or LoggerObjects.LogLevel.ERROR or LoggerObjects.LogLevel.FATAL or LoggerObjects.LogLevel.DEBUG2 or LoggerObjects.LogLevel.TRACE)
                            {
                                Console.ResetColor();
                                Console.ForegroundColor = ConsoleColor.Black; Console.BackgroundColor = LogLevelColor; Console.Write($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] ");
                                Console.Write($"[{LogLevelText}]");
                                Console.ResetColor(); Console.WriteLine($" {LogMessage}");

                                if (b.Exception is not null)
                                    Console.WriteLine(b.Exception.ToString());
                            }
                        }
                        else
                        {
                            Console.ResetColor(); Console.Write($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] ");
                            Console.ForegroundColor = LogLevelColor; Console.Write($"[{LogLevelText}] ");
                            Console.ResetColor(); Console.WriteLine(LogMessage);

                            if (b.Exception is not null)
                                Console.WriteLine(b.Exception.ToString());
                        }

                        _ = Task.Run(() =>
                        {
                            LogRaised?.Invoke(null, new LogMessageEventArgs() { LogEntry = b });
                        });

                        _loggerObjects.LogsToPost.Remove(b);

                        try
                        {
                            Byte[] FileWrite = Encoding.UTF8.GetBytes($"[{b.TimeOfEvent:dd.MM.yyyy HH:mm:ss}] [{LogLevelText}] {LogMessage}\n{(b.Exception is not null ? $"{b.Exception.ToString()}\n" : "")}");
                            if (OpenedFile != null)
                            {
                                await OpenedFile.WriteAsync(FileWrite.AsMemory(0, FileWrite.Length));
                                OpenedFile.Flush();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogFatal($"Couldn't write log to file: {ex}");
                        }

                        GC.KeepAlive(b);
                        GC.KeepAlive(b.LogLevel);
                        GC.KeepAlive(b.Message);
                        GC.KeepAlive(b.TimeOfEvent);
                    }
                }
                catch (Exception ex)
                {
                    Console.ResetColor(); Console.Write($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss} | ??] ");
                    Console.ForegroundColor = ConsoleColor.DarkRed; Console.Write($"[FATAL] ");
                    Console.ResetColor(); Console.WriteLine($"An error occured while logging: {ex}");
                    await Task.Delay(1000);
                    continue;
                }
            }
        });

        GC.KeepAlive(_loggerObjects.LogsToPost);
        return new Logger();
    }



    /// <summary>
    /// Stops the logger
    /// </summary>
    public static void StopLogger()
    {
        loggerStarted = false;
        maxLogLevel = LoggerObjects.LogLevel.DEBUG;
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
    public static void AddBlacklist(string blacklist)
    {
        _loggerObjects.Blacklist.Add(blacklist);
    }



    /// <summary>
    /// Changes the log level
    /// </summary>
    /// <param name="level"></param>
    public static void ChangeLogLevel(LoggerObjects.LogLevel level)
    {
        maxLogLevel = level;
    }



    /// <summary>
    /// Log with debug log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public static void LogDebug(string message, Exception? exception = null)
    {
        _loggerObjects.LogsToPost.Add(new LoggerObjects.LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LoggerObjects.LogLevel.DEBUG,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with info log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public static void LogInfo(string message, Exception? exception = null)
    {
        _loggerObjects.LogsToPost.Add(new LoggerObjects.LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LoggerObjects.LogLevel.INFO,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with warn log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public static void LogWarn(string message, Exception? exception = null)
    {
        _loggerObjects.LogsToPost.Add(new LoggerObjects.LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LoggerObjects.LogLevel.WARN,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with error log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public static void LogError(string message, Exception? exception = null)
    {
        _loggerObjects.LogsToPost.Add(new LoggerObjects.LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LoggerObjects.LogLevel.ERROR,
            Message = message,
            Exception = exception
        });
    }



    /// <summary>
    /// Log with fatal log level
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    public static void LogFatal(string message, Exception? exception = null)
    {
        _loggerObjects.LogsToPost.Add(new LoggerObjects.LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = LoggerObjects.LogLevel.FATAL,
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
        _loggerObjects.LogsToPost.Add(new LoggerObjects.LogEntry
        {
            TimeOfEvent = DateTime.Now,
            LogLevel = logLevel switch
            {
                Microsoft.Extensions.Logging.LogLevel.Debug => LoggerObjects.LogLevel.DEBUG2,
                Microsoft.Extensions.Logging.LogLevel.Trace => LoggerObjects.LogLevel.TRACE,
                Microsoft.Extensions.Logging.LogLevel.Information => LoggerObjects.LogLevel.INFO,
                Microsoft.Extensions.Logging.LogLevel.Warning => LoggerObjects.LogLevel.WARN,
                Microsoft.Extensions.Logging.LogLevel.Error => LoggerObjects.LogLevel.ERROR,
                Microsoft.Extensions.Logging.LogLevel.Critical => LoggerObjects.LogLevel.FATAL,
                Microsoft.Extensions.Logging.LogLevel.None => LoggerObjects.LogLevel.NONE,
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
