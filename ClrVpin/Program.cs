using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClrVpin.Shared.Utils;

namespace ClrVpin;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any())
            return RunConsole(args);

        App.Main();
        return 0;
    }

    private static int RunConsole(string[] args)
    {
        try
        {
            // open console via either..
            // a. new console - if started from UI, e.g. explorer
            // b. existing console - if started from a console
            // - refer https://stackoverflow.com/questions/32204195/wpf-and-commandline-app-in-the-same-executable
            // - not specified in the posts, but the console must be created/used BEFORE App.Main is started
            // - not done within App.OnStartup because whilst creates/attaches console works, the WriteLine does not
            if (!AttachConsole(-1)) // -1 = attach parent process
                AllocConsole();

            // setup CLI
            return ProcessCommandLine(args);
        }
        finally
        {
            FreeConsole();
        }
    }

    private static int ProcessCommandLine(string[] args)
    {
        // global
        var pauseOption = new Option<bool>("--pause", "Pause before exit");

        // inspect
        var tableArgument = new Argument<FileInfo>("table", "The table file");
        var inspectCommand = new Command("inspect", "Inspect file details");
        inspectCommand.AddAlias("i");
        inspectCommand.AddArgument(tableArgument);
        inspectCommand.SetHandler((pause, table) => Invoke(() => Inspect(table), pause), pauseOption, tableArgument);

        // root
        var rootCommand = new RootCommand("ClrVpin Command Line Interface");
        rootCommand.AddCommand(inspectCommand);
        rootCommand.AddGlobalOption(pauseOption);

        rootCommand.Invoke(args);

        // using field state because SetHandler doesn't appear to support returning error codes directly from the SetHandler method.. pretty naff :(
        // - perhaps this will be available via the newer AddAction??
        return _returnCode;
    }

    private static void Invoke(Func<int> func, bool pause)
    {
        func();

        if (!pause)
            return;
        
        Debug("\nPress any key to exit..");
        Console.ReadKey();
    }

    // ReSharper disable once UnusedMethodReturnValue.Local - not currently supported by System.CommandLine.. refer _returnCode comment
    private static int Inspect(FileSystemInfo table)
    {
        if (!table.Exists)
            return Error($"Table not found: '{table.Name}'", -1);

        var (_, isSuccess, romName) = TableUtils.GetRom(null, table.FullName, true);
        return isSuccess == false ? Warning("ROM not found in the table script", -2) : Success($"ROM: {romName}");
    }

    private static int Success(string message) => ProcessResult(message, 0, ConsoleColor.Green);
    private static int Debug(string message) => ProcessResult(message, null, ConsoleColor.DarkGray);
    private static int Warning(string message, int returnCode) => ProcessResult(message, returnCode, ConsoleColor.Yellow);
    private static int Error(string message, int returnCode) => ProcessResult(message, returnCode, ConsoleColor.Red);

    private static int ProcessResult(string message, int? returnCode = null, ConsoleColor? color = null)
    {
        var exitingColor = Console.ForegroundColor;
        if (color != null)
            Console.ForegroundColor = color.Value;
        Console.WriteLine($"{message}");
        Console.ForegroundColor = exitingColor;

        if (returnCode != null)
            _returnCode = returnCode.Value;
        return _returnCode;
    }

    [DllImport("kernel32")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool FreeConsole();

    private static int _returnCode; 
}