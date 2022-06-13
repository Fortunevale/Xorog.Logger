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
    internal LoggerProvider(Logger logger)
    {
        _logger = logger;
    }

    private Logger _logger { get; set; }

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
