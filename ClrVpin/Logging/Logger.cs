using System;
using System.Collections.ObjectModel;
using NLog;

namespace ClrVpin.Logging
{
    public class Logger
    {
        public static ObservableCollection<Log> Logs { get; } = new ObservableCollection<Log>();

        public static void Info(string message)
        {
            _logger.Info(message);
            Add(Level.Info, message);
        }

        public static void Warn(string message)
        {
            _logger.Warn(message);
            Add(Level.Warn, message);
        }

        public static void Error(Exception exception, string message)
        {
            _logger.Error(exception, message);
            Add(Level.Error, $"{message}\n{exception}");
        }

        public static void Error(string message)
        {
            _logger.Error(message);
            Add(Level.Error, message);
        }

        private static void Add(Level level, string message) => Logs.Add(new Log(level, message));

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    }
}