using PropertyChanged;

namespace ClrVpin.Logging;

[AddINotifyPropertyChangedInterface]
public class Log
{
    public Log(Level level, string message)
    {
        Message = message;
        Level = level;
    }

    public Level Level { get; }
    public string Message { get; }
}