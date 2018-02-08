using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CatLogger.Models;

namespace CatLogger.Utilities
{
    public class LoggerWindowTraceListener : TraceListener
    {        
        public const int MaxMessageNumber = 10000;
        private string _latestMessage;
        public static object Mutex = new object();
        public static List<LoggingMessage> LoggingMessages { get; set; }
        
        public LoggerWindowTraceListener()
        {
            _latestMessage = string.Empty;    
            LoggingMessages = new List<LoggingMessage>();
        }
    
        public override void Write(string message)
        {
            _latestMessage = message;            
        }

        public override void WriteLine(string message)
        {
            _latestMessage = message;
            AddMessageToList();
        }

        /// <summary>
        /// Active - add a listener ot trace, the listener will be active for listening logging
        /// </summary>
        public static void Active()
        {            
            bool notExist = true;
            if(Trace.Listeners.Count > 0)
            {
                const string typeName = "LoggerWindowTraceListener";
                foreach (var listener in Trace.Listeners)
                {                    
                    if (typeName == listener.GetType().Name)
                    {
                        notExist = false;
                        break;
                    }
                }
            }

            if (notExist)
            {
                var loggerWindowListener = new LoggerWindowTraceListener();
                Trace.Listeners.Add(loggerWindowListener);
            }
        }
 
        private void AddMessageToList()
        {
            lock (Mutex)
            {                
                if (LoggingMessages.Count > MaxMessageNumber)
                {
                    //clear half items if message number is larger than Max Message Number
                    LoggingMessages = LoggingMessages.Where((value, index) => index >= (MaxMessageNumber / 2)).ToList();
                }
                //Add message
                var loggingMessage = new LoggingMessage { TraceLevel = Logger.CurrentLoggertraceLevel, Content = _latestMessage };
                LoggingMessages.Add(loggingMessage);  
            }                                                                  
        }       
    }
}
