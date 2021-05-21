using System;
using System.Collections.ObjectModel;
using System.IO;
using NLog;
using NLog.Targets;

namespace ClrVpin.Logging
{
    public class Logger
    {
        public static ObservableCollection<Log> Logs { get; } = new ObservableCollection<Log>();
        public static string File { get; }

        static Logger()
        {
            File = GetLogFile();
        }
        
        private static string GetLogFile()
        {
            var fileTarget = (FileTarget)LogManager.Configuration.FindTargetByName("Service");
            
            // Need to set timestamp here if filename uses date. e.g. filename="${basedir}/logs/${shortdate}/trace.log"
            var logEventInfo = new LogEventInfo { TimeStamp = DateTime.Now };
            var fileName = fileTarget.FileName.Render(logEventInfo);
            var path = Path.GetFullPath(fileName);

            return path;
        }

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