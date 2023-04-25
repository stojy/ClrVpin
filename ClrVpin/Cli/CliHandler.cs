using System;
using System.CommandLine;
using System.IO;
using ClrVpin.Shared.Utils;
using Utils.Console;

namespace ClrVpin.Cli;

public static class CliHandler
{
    public static int Invoke(string[] args)
    {
        // global
        var pauseOption = new Option<bool>("--pause", "Pause before exit");

        // inspect
        var tableArgument = new Argument<FileInfo>("table", "The table file");
        var inspectCommand = new Command("inspect", "Inspect the table file details");
        inspectCommand.AddAlias("i");
        inspectCommand.AddArgument(tableArgument);
        inspectCommand.SetHandler((pause, table) => InvokeWithPause(() => Inspect(table), pause), pauseOption, tableArgument);

        // root
        var rootCommand = new RootCommand("ClrVpin Command Line Interface");
        rootCommand.AddCommand(inspectCommand);
        rootCommand.AddGlobalOption(pauseOption);

        rootCommand.Invoke(args);

        // using field state because SetHandler doesn't appear to support returning error codes directly from the SetHandler method.. pretty naff :(
        // - perhaps this will be available via the newer AddAction??
        return Environment.ExitCode;
    }

    private static void InvokeWithPause(Action action, bool pause)
    {
        action();

        if (!pause)
            return;

        ConsoleUtils.Debug("\nPress any key to exit..");
        Console.ReadKey();
    }

    // ReSharper disable once UnusedMethodReturnValue.Local - not currently supported by System.CommandLine.. refer _returnCode comment
    private static void Inspect(FileSystemInfo table)
    {
        if (!table.Exists)
        {
            ConsoleUtils.Error($"Table not found: '{table.Name}'", -1);
            return;
        }

        var (_, isSuccess, romName) = TableUtils.GetRom(null, table.FullName, true);
        if (isSuccess == false)
            ConsoleUtils.Warning("ROM not found in the table script", -2);
        else
            ConsoleUtils.Success($"ROM: {romName}");
    }
}