using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xorog.Logger;
public class LoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, Logger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new Logger());
    }

    public void Dispose() => _loggers.Clear();
}
