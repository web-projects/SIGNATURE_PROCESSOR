namespace Common.LoggerManager
{
    public enum LoggingEventType
    {
        Debug,
        Information,
        Warning,
        Error,
        Fatal
    };

    public enum LogTarget
    {
        File,
        Database,
        EventLog
    }

    public abstract class LogBase
    {
        protected readonly object lockObj = new object();

        // DEBUG LOGGING
        public abstract System.Threading.Tasks.Task debug(string message);
        // INFO LOGGING
        public abstract System.Threading.Tasks.Task info(string message);
        // WARNING LOGGING
        public abstract System.Threading.Tasks.Task warning(string message);
        // ERROR LOGGING
        public abstract System.Threading.Tasks.Task error(string message);
        // FATAL LOGGING
        public abstract System.Threading.Tasks.Task fatal(string message);
    }
}
