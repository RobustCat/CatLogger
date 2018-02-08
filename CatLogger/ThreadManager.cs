using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace CatLogger
{
    public class ThreadManager
    {
        #region Fields and Events 

        // Fields 

        private static readonly ThreadContextContainer ContextContainer = new ThreadContextContainer();

        #endregion Fields and Events 

        #region Methods 

        // Methods 

        [DllImport("VCL.dll", CharSet = CharSet.Ansi)]
        static extern void InitializeSEHFilter();

        public static Thread Create(string threadName, Func<object, uint> threadProc, object param)
        {
            return Create(threadName, threadProc, param, ApartmentState.MTA);
        }

        public static Thread Create(string threadName, Func<object, uint> threadProc, object param, ThreadPriority priority)
        {
            return Create(threadName, threadProc, param, ApartmentState.MTA, priority);
        }

        public static Thread Create(string threadName, Func<object, uint> threadProc, object param, ApartmentState apartmentState)
        {
            return Create(threadName, threadProc, param, apartmentState, ThreadPriority.Normal);
        }

        public static Thread Create(string threadName, Func<object, uint> threadProc, object param, ApartmentState apartmentState, ThreadPriority priority)
        {
            return Create(threadName, threadProc, param, apartmentState, priority, false);
        }

        public static Thread Create(string threadName, Func<object, uint> threadProc, object param, ApartmentState apartmentState, bool isBackground)
        {
            return Create(threadName, threadProc, param, apartmentState, ThreadPriority.Normal, isBackground);
        }

        public static Thread Create(string threadName, Func<object, uint> threadProc, object param, ApartmentState apartmentState, ThreadPriority priority, bool isBackground)
        {
            var thread = new Thread(OnThreadStarted) { Name = threadName };
            thread.SetApartmentState(apartmentState);
            thread.IsBackground = isBackground;
            thread.Priority = priority;
            var context = new ThreadContext(thread, threadProc, param);
            ContextContainer.Add(context);
            thread.Start(context);
            return thread;
        }

        private static void OnThreadStarted(object obj)
        {
            var contex = (ThreadContext)obj;
            if (contex.Handle != null)
            {
                try
                {
                    var name = contex.Name;
                    Logger.ForceWriteLine("Thread {0} is started", name);
                    contex.Handle(contex.Param);
                    contex.Finish();
                    Logger.ForceWriteLine("Thread {0} is finished", name);
                }
                catch (ThreadAbortException)
                {
                    // This will happen in case caller wants to abort the thread.
                    throw;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    ContextContainer.Remove(contex);
                }
            }
        }

        public static void WaitFor(Thread mThreadHandle)
        {
            ThreadContext context;
            using (ContextContainer.Lock())
            {
                context = ContextContainer.FirstOrDefault(x => x.Thread == mThreadHandle);
            }
            if (context != null)
            {
                WaitForContext(context);
            }
        }

        private static void WaitForContext(ThreadContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            Logger.WriteLineInfo("Thread {0}. Request for finishing", context.Name);

            if (context.Thread.IsAlive)
            {
                context.Wait();
                Logger.WriteLineInfo("Thread {0} is finished", context.Name);
            }
            else
            {
                Logger.WriteLineError("Thread {0} has not been started yet", context.Name);
            }
        }

        public static void WaitFor(string threadName)
        {
            ThreadContext context;
            using (ContextContainer.Lock())
            {
                context = ContextContainer.FirstOrDefault(x => x.Name == threadName);
            }
            if (context != null)
            {
                WaitForContext(context);
            }
        }

        #endregion Methods 
    }

    internal class ThreadContext
    {
        #region Fields and Events 

        // Fields 

        private readonly ManualResetEvent _manualResetEvent;

        #endregion Fields and Events 

        #region Properties 

        public Func<object, uint> Handle { get; private set; }

        public string Name { get; private set; }

        public object Param { get; private set; }

        public Thread Thread { get; private set; }

        #endregion Properties 

        #region Methods 

        // Constructors 

        internal ThreadContext(Thread t, Func<object, uint> h, object p)
        {
            Thread = t;
            Name = t.Name;
            Handle = h;
            Param = p;
            _manualResetEvent = new ManualResetEvent(false);
        }
        // Methods 

        public void Wait()
        {
            _manualResetEvent.WaitOne();
        }

        public void Finish()
        {
            _manualResetEvent.Set();
        }

        #endregion Methods 
    }

    internal class ThreadContextContainer : IEnumerable<ThreadContext>
    {
        #region Fields and Events

        // Fields 

        private readonly object _protector = new object();
        private readonly ConcurrentBag<ThreadContext> _contexts = new ConcurrentBag<ThreadContext>();

        #endregion Fields and Events

        #region Methods

        // Methods 

        public void Add(ThreadContext context)
        {
            if (context != null)
            {
                lock (_protector)
                {
                    _contexts.Add(context);
                    Logger.WriteLineVerbose("ThreadContext {0} is added", context.Name);
                }
            }
        }

        public void Remove(ThreadContext context)
        {
            if (context != null)
            {
                lock (_protector)
                {
                    _contexts.TryTake(out context);
                    Logger.WriteLineVerbose("ThreadContext {0} is removed", context.Name);
                }
            }
        }

        public IEnumerator<ThreadContext> GetEnumerator()
        {
            return _contexts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDisposable Lock()
        {
            return new ThreadContextContainerLocker(this);
        }
        #endregion Methods

        private class ThreadContextContainerLocker : IDisposable
        {
            private readonly ThreadContextContainer _owner;

            public ThreadContextContainerLocker(ThreadContextContainer owner)
            {
                _owner = owner;
                Monitor.Enter(_owner._protector);
            }
            public void Dispose()
            {
                Monitor.Exit(_owner._protector);
            }
        }
    }
}
