using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace CatLogger
{
    [Obfuscation(Exclude = true)]
    public enum LogType
    {
        Both,
        DotNet,
        Native
    }
    public class Logger
    {
        #region Fields and Events (2) 

        // Fields (2) 

        private static Dictionary<string, TraceLevel> _filterAssemblies = new Dictionary<string, TraceLevel>();
        private static TraceLevel _traceLevel;
        private static object _mutex = new object();
        private const double BytesPerGB = 1000 * 1000 * 1024;
        private static DriveInfo _driveInfo;
        private static string _fileName;
        #endregion Fields and Events 

        #region Properties (4) 

        /// <summary>
        /// Note Lock the type of Logger before you call this property
        /// </summary>
        public static Dictionary<string, TraceLevel> AssemblyFilters
        {
            get { return _filterAssemblies; }
        }

        public static bool Filter { get; set; }

        public static string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                _driveInfo = new DriveInfo(_fileName);
            }
        }

        /// <summary>
        /// Indicate whether the logger shall be ignored
        /// This is used when logger level is verbose and some cyclically code is running(e.g., cineloop progress) which create lot of log info.
        /// </summary>
        public static bool Ignore { get; set; }

        /// <summary>
        /// Indicate whether the logger shall be ignored
        /// This is used when some cyclically code is running(e.g., read native temperature) which create lot of log info.
        /// </summary>
        public static bool IgnoreNative { get; set; }

        public static TraceLevel LogLevel
        {
            get { return _traceLevel; }
            set
            {
                if (_traceLevel != value)
                {
                    lock (_mutex)
                    {
                        _traceLevel = value;
                    }
                }
            }
        }

        [ThreadStatic]
        public static TraceLevel CurrentLoggertraceLevel;
        /// <summary>
        /// True to show the assembly and method names for the source of the log, false to disable this.
        /// </summary>
        public static bool ShowPath { get; set; }

        /// <summary>
        /// True to log native trace result, false to not log native trace result.
        /// There are some issue when step into .net code when native trace is forwarded to Logger, so before debuging, you can off this flag.
        /// </summary>
        public static LogType ShowLogType { get; set; }

        #endregion Properties 

        #region Methods (6) 

        static Logger()
        {
            LogTime = true;
        }
        // Methods (6) 

        // NOTE: Please keep this format!
        private static readonly string _infoTag = "I ; ";
        private static readonly string _debguTag = "D ; ";
        private static readonly string _warnTag = "W ; ";
        private static readonly string _errorTag = "E ; ";
        private static readonly string _verboseTag = "V ; ";
        private static readonly string _fatalTag = "F ; ";
        private const string NativeLogHeader = "NAT:";

        private static int _lastFlush;
        private static int _logFlushLevel = 1;

        public static void WriteLineError(string message)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Error, message, null, 2);
        }
        public static void WriteLineError(string format, object arg1)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Error, format, new[] { arg1 }, 2);
        }
        public static void WriteLineError(string format, object arg1, object arg2)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Error, format, new[] { arg1, arg2 }, 2);
        }
        public static void WriteLineError(string format, params object[] args)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Error, format, args, 2);
        }

        [Conditional("DEBUG")]
        public static void WriteLineWarning(string message)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Warning, message, null, 2);
        }
        [Conditional("DEBUG")]
        public static void WriteLineWarning(string format, object arg1)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Warning, format, new[] { arg1 }, 2);
        }
        [Conditional("DEBUG")]
        public static void WriteLineWarning(string format, object arg1, object arg2)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Warning, format, new[] { arg1, arg2 }, 2);
        }
        [Conditional("DEBUG")]
        public static void WriteLineWarning(string format, params object[] args)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Warning, format, args, 2);
        }

        //[Conditional("DEBUG")]
        public static void WriteLineInfo(string message)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Info, message, null, 2);
        }
        // [Conditional("DEBUG")]
        public static void WriteLineInfo(string format, object arg1)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Info, format, new[] { arg1 }, 2);
        }
        // [Conditional("DEBUG")]
        public static void WriteLineInfo(string format, object arg1, object arg2)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Info, format, new[] { arg1, arg2 }, 2);
        }

        //[Conditional("DEBUG")]
        public static void WriteLineInfo(string format, params object[] args)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Info, format, args, 2);
        }

        [Conditional("DEBUG")]
        public static void WriteLineVerbose(string message)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Verbose, message, null, 2);
        }
        [Conditional("DEBUG")]
        public static void WriteLineVerbose(string format, object arg1)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Verbose, format, new[] { arg1 }, 2);
        }
        [Conditional("DEBUG")]
        public static void WriteLineVerbose(string format, object arg1, object arg2)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Verbose, format, new[] { arg1, arg2 }, 2);
        }

        [Conditional("DEBUG")]
        public static void WriteLineVerbose(string format, params object[] args)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Verbose, format, args, 2);
        }

        /// <summary>
        /// Write the log information no matter the trace level.
        /// </summary>
        /// <param name="message"></param>
        /// <remarks>Only use this if necessary. Else there will be too much log information during debugging</remarks>
        public static void ForceWriteLine(string message)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Off, message, null, 2);
        }

        public static void ForceWriteLine(string format, object arg1)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Off, format, new[] { arg1 }, 2);
        }

        public static void ForceWriteLine(string format, object arg1, object arg2)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Off, format, new[] { arg1, arg2 }, 2);
        }

        public static void ForceWriteLine(string format, params object[] args)
        {
            WriteLineIf(ShowLogType != LogType.Native, TraceLevel.Off, format, args, 2);
        }

        public static void ForceWriteLineIf(bool condition, string format, params object[] args)
        {
            WriteLineIf(condition && ShowLogType != LogType.Native, TraceLevel.Off, format, args, 2);
        }

        [Conditional("DEBUG")]
        public static void WriteLineInfoIf(bool condition, string format, params object[] args)
        {
            WriteLineIf(condition, TraceLevel.Info, format, args, 2);
        }

        [Conditional("DEBUG")]
        public static void WriteLineWarningIf(bool condition, string format, params object[] args)
        {
            WriteLineIf(condition, TraceLevel.Warning, format, args, 2);
        }

        public static void WriteLineErrorIf(bool condition, string format, params object[] args)
        {
            WriteLineIf(condition, TraceLevel.Error, format, args, 2);
        }

        [Conditional("DEBUG")]
        public static void WriteLineVerboseIf(bool condition, string format, params object[] args)
        {
            WriteLineIf(condition, TraceLevel.Verbose, format, args, 2);
        }

        private static void WriteLineIf(bool condition, TraceLevel traceLevel, string format, object[] args, int stackLevel)
        {
            if ((!condition) || (ShowLogType == LogType.Native)) return;

            WriteLineIf(traceLevel, format, args, stackLevel);
        }

        public static void WriteNativeLogIf(TraceLevel traceLevel, string format, object[] args)
        {
            if (ShowLogType == LogType.DotNet)
            {
                return;
            }
            WriteLineIf(traceLevel, format, args, 2);
        }

        private static void WriteLineIf(TraceLevel traceLevel, string format, object[] args, int stackLevel = 1)
        {
            if (traceLevel <= _traceLevel)
            {
                try
                {
                    var now = DateTime.Now;

                    StringBuilder sb = new StringBuilder();

                    if (LogTime)
                    {
                        sb.AppendFormat("[{0},{1:d3}]({2})-", now.ToString("HH:mm:ss"), now.Millisecond, AppDomain.GetCurrentThreadId());
                    }
                    string message = args == null ? format : string.Format(format, args);

                    if (traceLevel == TraceLevel.Error)
                    {
                        sb.Append(_errorTag);
                    }
                    else if (traceLevel == TraceLevel.Warning)
                    {
                        sb.Append(_warnTag);
                    }
                    else if (traceLevel == TraceLevel.Info)
                    {
                        sb.Append(_infoTag);
                    }
                    else if (traceLevel == TraceLevel.Verbose)
                    {
                        sb.Append(_verboseTag);
                    }

                    sb.Append(message);

                    CurrentLoggertraceLevel = traceLevel; //Set trace level, Tracelistener need to get the infor

                    Trace.WriteLine(sb.ToString());
                }
                catch (Exception ex)
                {
                    //throw;
                    // There is exception during trace, so just ignore
                }

                if (traceLevel <= TraceLevel.Info)
                {
                    int lastFlush = Environment.TickCount;

                    var previousFlush = _lastFlush;
                    if (traceLevel <= TraceLevel.Error ||
                        previousFlush == 0 ||
                        (lastFlush - previousFlush >= 1000)
                        )
                    {

                        if (_logFlushLevel == 1)
                        {
                            // Avoid flush too frequently
                            Flush();
                        }

                        lock (_mutex)
                        {
                            _lastFlush = lastFlush;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Indicate whether to output time stamp for log message. You can set to false if some debugger tool has already provided this.
        /// </summary>
        public static bool LogTime { get; set; }

        /// <summary>
        /// Force flush, so the information cached in trace listeners will be output to the interesting media(such as debug window or the log file)
        /// </summary>
        public static void Flush()
        {
            lock (_mutex)
            {
                try
                {
                    // Auto Flush the error level
                    foreach (TraceListener listener in Trace.Listeners)
                    {
                        listener.Flush();
                    }
                    _lastFlush = Environment.TickCount;
                }
                catch (IOException)
                {

                }
            }
        }

        #endregion Methods 
    }
}
