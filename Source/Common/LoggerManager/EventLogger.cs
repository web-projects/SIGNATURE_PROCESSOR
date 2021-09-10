using System.Diagnostics;
using System.Threading.Tasks;

namespace Common.LoggerManager
{
    public class EventLogger : LogBase
    {
        public string filePath = "";

        public EventLogger(string fName)
        {
            filePath = fName;
        }

        // DEBUG LOGGING
        public override System.Threading.Tasks.Task debug(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the event log
            }
            return Task.FromResult(0);
        }
        // INFO LOGGING
        public override System.Threading.Tasks.Task info(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the event log
                EventLog eventLog = new EventLog("");
                eventLog.Source = "[APP]EventLog";
                eventLog.WriteEntry(message);
            }
            return Task.FromResult(0);
        }
        // WARNING LOGGING
        public override System.Threading.Tasks.Task warning(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the event log
            }
            return Task.FromResult(0);
        }
        // ERROR LOGGING
        public override System.Threading.Tasks.Task error(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the event log
            }
            return Task.FromResult(0);
        }
        // FATAL LOGGING
        public override System.Threading.Tasks.Task fatal(string message)
        {
            lock (lockObj)
            {
                //TODO: code to log data to the event log
            }
            return Task.FromResult(0);
        }
    }
}
