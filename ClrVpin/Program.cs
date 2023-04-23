using System;
using System.Runtime.InteropServices;

namespace ClrVpin;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        //ShowConsole();
        App.Main();
    }

    private static void ShowConsole()
    {
        // refer https://stackoverflow.com/questions/32204195/wpf-and-commandline-app-in-the-same-executable
        // - not specified in the posts, but the console must be created/used BEFORE App.Main is started
        //   --> for reasons unknown, creating in App.OnStartup prevents the console WriteLine from working.. although Console.ReadKey() still works!
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
    }

    [DllImport("kernel32")]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32")]
    private static extern bool FreeConsole();
}