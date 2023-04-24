using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
        var tableOption = new Option<FileInfo>("--table", "The table file to inspect") { IsRequired = true };
        tableOption.AddAlias("--t");

        var inspectCommand = new Command("inspect", "Inspect file details");
        inspectCommand.AddAlias("i");
        inspectCommand.AddOption(tableOption);

        var rootCommand = new RootCommand("ClrVpin Command Line Interface");
        rootCommand.AddCommand(inspectCommand);

        inspectCommand.SetHandler(table => { Console.WriteLine($"my table: {table}"); }, tableOption);

        return rootCommand.Invoke(args);
    }

    [DllImport("kernel32")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool FreeConsole();
}