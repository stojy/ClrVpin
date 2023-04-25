using System;
using System.Runtime.InteropServices;

namespace Utils.Console;

public static class ConsoleUtils
{
    public static int InvokeInConsole(Func<int> func)
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
            return func();
        }
        finally
        {
            FreeConsole();
        }
    }

    public static void Success(string message) => ProcessResult(message, 0, ConsoleColor.Green);
    public static void Debug(string message) => ProcessResult(message, null, ConsoleColor.DarkGray);
    public static void Warning(string message, int returnCode) => ProcessResult(message, returnCode, ConsoleColor.Yellow);
    public static void Error(string message, int returnCode) => ProcessResult(message, returnCode, ConsoleColor.Red);

    private static void ProcessResult(string message, int? returnCode = null, ConsoleColor? color = null)
    {
        var exitingColor = System.Console.ForegroundColor;
        if (color != null)
            System.Console.ForegroundColor = color.Value;
        System.Console.WriteLine($"{message}");
        System.Console.ForegroundColor = exitingColor;

        if (returnCode != null)
            Environment.ExitCode = returnCode.Value;
    }

    [DllImport("kernel32")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool FreeConsole();
}