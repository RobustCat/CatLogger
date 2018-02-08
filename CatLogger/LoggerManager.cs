using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using CatLogger.Utilities;

namespace CatLogger
{
    public static class LoggerManager
    {
        public static TextWriterTraceListener _listener;
        private static Timer _logFlushTimer;
        private const int DefaultMaxAllowedLogFiles = 10;
        private static readonly string DefaultLogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CatLogs", "log");

        public static string RootPath
        {
            get
            {
              
                var value = ConfigurationManager.AppSettings["MainModulePath"];
                return !String.IsNullOrEmpty(value) ? value : AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        public static string AppSettings(string key, string defaultValue)
        {
            string value = defaultValue;
            try
            {
                value = ConfigurationManager.AppSettings[key];
            }
            catch (ConfigurationErrorsException ex)
            {
                Logger.WriteLineVerbose("SystemSettingImpl, exception:" + ex);
            }
            return String.IsNullOrEmpty(value) ? (defaultValue ?? String.Empty) : value;
        }
        public static void InitializeLoggers(bool isHorm = false)
        {
            //FOR HORM
            if (!isHorm)
            {
                //todo
                Profiler.ProfilerLevel = ProfilerEnum.Performance;
                //if (ServiceManager.AppSettings("debugPerformance", "0") == "1")
                //{
                //    Profiler.ProfilerLevel = ProfilerEnum.Performance;
                //}
                //if (ServiceManager.AppSettings("debugMemory", "1") == "1")
                //{
                //    Profiler.ProfilerLevel |= ProfilerEnum.Memory;
                //}
            }
            TraceLevel traceLevel = ParseTraceLevel(AppSettings("logLevel", "Off"));

            // Get settings for the log file name and the max allowed log files from the app setting
            string logFileName = DefaultLogPath;

            int maxLogFiles = ParseMaxLogFiles();

            InitializeLoggers(traceLevel, logFileName, maxLogFiles, isHorm);
            LogType logType = ParseLogType(AppSettings("LogType", "Both"));
            Logger.ShowLogType = logType;

            // Create a thread timer to flush logger. In case main dispatcher is hung, this
            // time can flush important logs into log file
            int flushLogIntervalInMs = 5000;
            var logFlushLevel = 1;

            if (logFlushLevel == 1 && flushLogIntervalInMs > 0)
            {
                _logFlushTimer = new Timer(OnFlushLog, null, flushLogIntervalInMs, flushLogIntervalInMs);
            }

        }
        private static int CompareCreateTime(FileInfo file1, FileInfo file2)
        {
            return file1.LastWriteTime.CompareTo(file2.LastWriteTime);
        }
        public static void InitializeLoggers(TraceLevel traceLevel, string logFileName, int maxLogFiles, bool isHorm)
        {
            Logger.LogLevel = traceLevel;
            FileInfo fileInfo = new FileInfo(logFileName);

            Exception createLoggerException = null;

            DirectoryInfo directoryInfo = fileInfo.Directory;
            try
            {
                // Create the directry tree
                if (!directoryInfo.Exists)
                {
                    directoryInfo.Create();
                }
                else
                {
                    // Remove the first files to keep number of log files  in a limited scope
                    FileInfo[] currentFiles = directoryInfo.GetFiles();
                    Array.Sort(currentFiles, CompareCreateTime);
                    for (int i = maxLogFiles - 1; i < currentFiles.Length; i++)
                    {
                        try
                        {
                            currentFiles[i - (maxLogFiles - 1)].Delete();
                        }
                        catch (Exception e)
                        {
                            Logger.WriteLineError("{0}", e);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                createLoggerException = ex;
                directoryInfo = new DirectoryInfo(Path.Combine(DefaultLogPath, @"Log"));
                fileInfo = new FileInfo(Path.Combine(DefaultLogPath, @"Log", "robustLog"));
            }
            // Create the file name
            DateTime currentTime = DateTime.Now;
            string newFileName = string.Format("{0}.{1}", currentTime.ToString("yyyyMMdd_HHmmss"), fileInfo.Name);

            var fullName = Path.Combine(directoryInfo.FullName, newFileName);

            if (isHorm)
                RemoveListeners();

            _listener = new TextWriterTraceListener(fullName, "myListener");
            Trace.Listeners.Add(_listener);
            Logger.FileName = fullName;

            if (!isHorm)
                AddLoggingWindowTraceListener();

            // Write first message            
            Logger.ForceWriteLine("System started, first log message. \r\nLogFile:{0}", newFileName);
            if (createLoggerException != null)
            {
                Logger.ForceWriteLine("Create log failed. ex:{0}", createLoggerException);
            }
        }

        private static void AddLoggingWindowTraceListener()
        {
           
            string loggingWindowEnabled = AppSettings("LoggingWindowEnabled", "0");
            if (loggingWindowEnabled == "1")
            {
                LoggerWindowTraceListener.Active();
            }
        }
        public static void RemoveListeners()
        {
            if (_logFlushTimer != null)
            {
                try
                {
                    _logFlushTimer.Dispose();
                }
                finally
                {
                    _logFlushTimer = null;
                }
            }

            if (_listener != null)
            {
                try
                {
                    Trace.Flush();
                    Trace.Listeners.Remove(_listener);
                    _listener.Close();
                    _listener.Dispose();
                }
                finally
                {
                    _listener = null;
                }
            }
        }

        private static void OnFlushLog(object state)
        {
            Logger.Flush();
        }

        internal static TraceLevel ParseTraceLevel(string enumString)
        {
            TraceLevel result = default(TraceLevel);
            try
            {
                result = (TraceLevel)Enum.Parse(typeof(TraceLevel), enumString);
            }
            catch (Exception)
            {
                Trace.WriteLine(string.Format("Failed to Parse Enum. Invalid enum string {0} is defined for type TraceLevel", enumString));
            }
            return result;
        }

        internal static LogType ParseLogType(string enumString)
        {
            LogType result = default(LogType);
            try
            {
                result = (LogType)Enum.Parse(typeof(LogType), enumString);
            }
            catch (Exception)
            {
                Trace.WriteLine(string.Format("Failed to Parse Enum. Invalid enum string {0} is defined for type LogType", enumString));
            }
            return result;
        }
        private static int ParseMaxLogFiles()
        {
            int maxLogFiles;
            bool isParseSuccess = int.TryParse(
                AppSettings("MaxLogFiles",
                                           DefaultMaxAllowedLogFiles.ToString(CultureInfo.InvariantCulture)),
                                           out maxLogFiles);
            if (!isParseSuccess)
            {
                maxLogFiles = DefaultMaxAllowedLogFiles;
            }
            return maxLogFiles;
        }
    }
}
