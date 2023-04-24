using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace ClrVpin;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any())
            return RunConsole();

        App.Main();
        return 0;
    }

    private static int RunConsole()
    {
        // run a console as either..
        // a. new console - if started from UI, e.g. explorer
        // b. existing console - if started from a console
        // - refer https://stackoverflow.com/questions/32204195/wpf-and-commandline-app-in-the-same-executable
        // - not specified in the posts, but the console must be created/used BEFORE App.Main is started
        // - not done within App.OnStartup because whilst creates/attaches console works, the WriteLine does not
        try
        {
            const int attachParentProcess = -1;
            if (AttachConsole(attachParentProcess))
            {
                Console.WriteLine("attached");
                Console.WriteLine("baby");
                Console.ReadKey();
            }
            else
            {
                AllocConsole();
                Console.WriteLine("allocated");
                Console.WriteLine("baby");
                Console.ReadKey();
            }
        }
        finally
        {
            FreeConsole();
        }

        return 0;
    }

    [DllImport("kernel32")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool FreeConsole();
}