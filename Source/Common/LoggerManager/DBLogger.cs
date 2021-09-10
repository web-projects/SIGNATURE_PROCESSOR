using System.Threading.Tasks;

namespace Common.LoggerManager
{
    public class DBLogger : LogBase
    {
        string connectionString = string.Empty;

        public string filePath = "";

        public DBLogger(string fName)
        {
            filePath = fName;
        }

        // DEBUG LOGGING
        public override System.Threading.Tasks.Task debug(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the database
            }
            return Task.FromResult(0);
        }
        // INFO LOGGING
        public override System.Threading.Tasks.Task info(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the database
            }
            return Task.FromResult(0);
        }
        // WARNING LOGGING
        public override System.Threading.Tasks.Task warning(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the database
            }
            return Task.FromResult(0);
        }
        // ERROR LOGGING
        public override System.Threading.Tasks.Task error(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the database
            }
            return Task.FromResult(0);
        }
        // FATAL LOGGING
        public override System.Threading.Tasks.Task fatal(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the database
            }
            return Task.FromResult(0);
        }
    }
}
