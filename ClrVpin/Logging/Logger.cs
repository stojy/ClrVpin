using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using ByteSizeLib;
using Microsoft.VisualBasic.Devices;
using NLog;
using NLog.Targets;
using Utils.Extensions;

namespace ClrVpin.Logging
{
    public static class Logger
    {
        static Logger()
        {
            _dispatch = Dispatcher.CurrentDispatcher;
            File = GetLogFile();

            Info($"\n\n{GetSystemInfo()}");
        }

        public static string GetSystemInfo()
        {
            var currentProcess = Process.GetCurrentProcess();
            var computerInfo = new ComputerInfo();
            var systemInfo = "---------- System Info ----------\n" +
                             $"Start Time:            {currentProcess.StartTime}\n" +
                             $"App:                   {currentProcess.ProcessName}\n" +
                             $"File Version:          {currentProcess.MainModule?.FileVersionInfo.FileVersion}\n" +
                             $"Product Version:       {currentProcess.MainModule?.FileVersionInfo.ProductVersion}\n" +
                             $"Executable:            {currentProcess.MainModule?.FileName}\n" +
                             $"Command Line Args:     {Environment.GetCommandLineArgs().StringJoin(" ")}\n" +
                             $"Processor Type:        {Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")}\n" + // https://en.wikichip.org/wiki/intel/cpuid
                             $"Processor Count:       {Environment.ProcessorCount}\n" +
                             $"Free Physical Memory:  {ByteSize.FromBytes(computerInfo.AvailablePhysicalMemory).ToString("0.#")}\n" +
                             $"Free Virtual Memory:   {ByteSize.FromBytes(computerInfo.AvailableVirtualMemory).ToString("0.#")}\n" +
                             $"OS:                    {Environment.OSVersion}\n" +
                             $"64 bit:                {Environment.Is64BitOperatingSystem}\n" +
                             $"CLI Version:           {Environment.Version}\n" +
                             "---------------------------------";
            return systemInfo;
        }

        public static ObservableCollection<Log> Logs { get; } = new();
        public static string File { get; }

        public static void Debug(string message, bool isDiagnostic = false)
        {
            if (IsIgnored(isDiagnostic))
                return;

            _logger.Debug(message);
            Add(Level.Debug, message);
        }

        public static void Info(string message, bool isDiagnostic = false, bool isHighlight = false)
        {
            if (IsIgnored(isDiagnostic))
                return;

            _logger.Info(message);
            Add(isHighlight ? Level.InfoHighlight : Level.Info, message);
        }

        public static void Warn(string message, bool isDiagnostic = false)
        {
            if (IsIgnored(isDiagnostic))
                return;

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

        public static void Clear()
        {
            _dispatch.BeginInvoke(() => Logs.Clear());
        }

        private static bool IsIgnored(bool isDiagnostic) => isDiagnostic && Model.Settings?.EnableDiagnosticLogging == false;

        private static string GetLogFile()
        {
            var fileTarget = (FileTarget)LogManager.Configuration.FindTargetByName("Service");

            // Need to set timestamp here if filename uses date. e.g. filename="${basedir}/logs/${shortdate}/trace.log"
            var logEventInfo = new LogEventInfo { TimeStamp = DateTime.Now };
            var fileName = fileTarget.FileName.Render(logEventInfo);
            var path = Path.GetFullPath(fileName);

            return path;
        }

        private static void Add(Level level, string message)
        {
            _dispatch.BeginInvoke(() => Logs.Add(new Log(level, message)));
        }

        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private static readonly Dispatcher _dispatch;
    }
}