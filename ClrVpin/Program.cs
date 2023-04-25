using System;
using System.Linq;
using Utils.Console;

namespace ClrVpin;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Any())
            return ConsoleUtils.InvokeInConsole(() => Cli.CliHandler.Invoke(args));

        App.Main();
        return 0;
    }
}