namespace ClrVpin.Logging
{
    public class Log
    {
        public Log(Level level, string message)
        {
            Message = message;
            Level = level;
        }

        public Level Level { get; }
        public string Message { get; internal set; }
    }
}