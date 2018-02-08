using System;
using System.Diagnostics;
using Test.LogWindow;

namespace CatLogger
{
    [Flags]
    public enum ProfilerEnum
    {
        /// <summary>
        /// hard coded to output this
        /// </summary>
        AuditTrial = 0,
        Performance = 0x1,
        Memory = 0x2,

        // TODO others
    }

    public class Profiler
    {
        public static ProfilerEnum ProfilerLevel { get; set; }
    }

    public interface IProfiler : IDisposable
    {

    }


    public abstract class ProfilerBase
    {
        public string Message { get; private set; }

        public ProfilerBase(string message)
        {
            Message = message;
        }

        public ProfilerBase(string format, params object[] args)
        {
            Message = string.Format(format, args);
        }
    }

    /// <summary>
    /// For performance, it must be logged after operation, while it can log a message depending on log level before operation
    /// </summary>
    public class Performance : ProfilerBase, IProfiler
    {
        private CpuClock _enterTime = new CpuClock();
        public Performance(string message)
            : base(message)
        {
            if ((Profiler.ProfilerLevel & ProfilerEnum.Performance) != 0)
            {
                //Logger.WriteLineInfo("Perf: {0} starting", Message);
                Trace.Indent();
            }
            else
            {
                //Logger.WriteLineVerbose("{0}", Message);
            }
        }

        public Performance(string format, params object[] args)
            : base(format, args)
        {
            if ((Profiler.ProfilerLevel & ProfilerEnum.Performance) != 0)
            {
                //Logger.WriteLineInfo("Perf: {0} starting", Message);
                Trace.Indent();
            }
            else
            {
                // Logger.WriteLineVerbose("{0}", Message);
            }
        }

        public void Dispose()
        {
            if ((Profiler.ProfilerLevel & ProfilerEnum.Performance) != 0)
            {
                var intervals = _enterTime.TotalMilliSeconds;
                Trace.Unindent();
                Logger.WriteLineInfo("Perf: {0} started. {1} ms", Message, intervals);
            }
        }
        /// <summary>
        /// Gets the total intervals of the performance object since it's started.
        /// </summary>
        public TimeSpan Intervals
        {
            get { return TimeSpan.FromMilliseconds(_enterTime.TotalMilliSeconds); }
        }
    }

    /// <summary>
    /// For audit trial, it must be logged at first, while it can log a message depending on log level after operation
    /// </summary>
    public class AuditTrial : ProfilerBase, IProfiler
    {
        private DateTime _enterTime;
        private long _originalMemoryInByte;

        private const double MB = 1000 * 1024;

        public AuditTrial(string title)
            : base(title)
        {
            _enterTime = DateTime.Now;

            // Logger.ForceWriteLine("AuditTrial: {0}", Message);
            Trace.Indent();
            if ((Profiler.ProfilerLevel & ProfilerEnum.Memory) != 0)
            {
                _originalMemoryInByte = Process.GetCurrentProcess().PrivateMemorySize64;
            }
        }

        public AuditTrial(string format, params object[] args)
            : base(format, args)
        {
            _enterTime = DateTime.Now;

            //Logger.ForceWriteLine("AuditTrial: {0}", Message);
            Trace.Indent();
            if ((Profiler.ProfilerLevel & ProfilerEnum.Memory) != 0)
            {
                _originalMemoryInByte = Process.GetCurrentProcess().PrivateMemorySize64;
            }
        }

        public void Dispose()
        {
            Trace.Unindent();
            var intervals = DateTime.Now - _enterTime;
            if ((Profiler.ProfilerLevel & ProfilerEnum.Performance) != 0)
            {
                // Logger.WriteLineInfo("Perf: {0} done. {1} ms", Message, intervals.TotalMilliseconds);
            }

            if ((Profiler.ProfilerLevel & ProfilerEnum.Memory) != 0)
            {
                long current = Process.GetCurrentProcess().PrivateMemorySize64;
                double delta = (current - _originalMemoryInByte) / MB;
                // Logger.ForceWriteLine("EndAuditTrial: {0}, {1} ms, commit mem: {2:F2} MB, delta:{3:F2} MB", Message, intervals.TotalMilliseconds, current / MB, delta);
            }
            else
            {
                var total = intervals.TotalMilliseconds;
                if (total > 50)
                {
                    // Logger.ForceWriteLine("EndAuditTrial: {0}, {1} ms", Message, total);
                }
                else
                {
                    //Logger.ForceWriteLine("EndAuditTrial: {0}", Message);
                }
            }
        }
    }
}
