using System;
using System.Diagnostics;

namespace Helpers
{
    public class TimeTracker
    {
        private Stopwatch watch = new Stopwatch();

        public void StartTracking()
        {
            StopTracking();

            watch = Stopwatch.StartNew();
        }

        public void StopTracking()
        {
            if (watch.IsRunning)
            {
                watch.Stop();
            }
        }

        public long GetTimeLapsedInMilliSeconds()
        {
            // if still running
            StopTracking();

            return watch.ElapsedMilliseconds;
        }

        public TimeSpan GetTimeLapsed()
        {
            // if still running
            StopTracking();

            return watch.Elapsed;
        }
    }
}
