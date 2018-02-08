using System.Runtime.InteropServices;

namespace CatLogger
{
    public class CpuClock
    {
        #region Fields and Events 

        // Fields 

        private static double _cpuFrequency;
        private double _startTime;

        #endregion Fields and Events 

        #region Methods 

        // Constructors 

        public CpuClock()
        {
            long counter;
            QueryPerformanceCounter(out counter);
            _startTime = counter / _cpuFrequency;
        }

        static CpuClock()
        {
            long counter;
            QueryPerformanceFrequency(out counter);
            _cpuFrequency = counter;
        }
        // Methods 

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        /// <summary>
        /// Returns the spent time(as second) after the <c>CpuClock</c> is created
        /// </summary>
        /// <returns></returns>
        public double GetTime()
        {
            long newCounter;
            QueryPerformanceCounter(out newCounter);
            return (newCounter) / _cpuFrequency - _startTime;
        }

        /// <summary>
        /// Returns the spent time(as second) after system start
        /// </summary>
        /// <returns></returns>
        public static double Current
        {
            get
            {
                long newCounter;
                QueryPerformanceCounter(out newCounter);
                return newCounter / (double)_cpuFrequency;
            }
        }

        public double StartTime
        {
            get { return _startTime; }
        }

        public double TotalSeconds
        {
            get { return (Current - _startTime); }
        }

        public double TotalMilliSeconds
        {
            get { return (Current - _startTime) * 1000; }
        }

        public static CpuClock Now
        {
            get
            {
                return new CpuClock();
            }
        }

        #endregion Methods 
    }
}
