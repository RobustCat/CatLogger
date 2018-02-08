using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using CatLogger;
using CatLogger.Interface;

namespace Test.LogWindow.LogSetting
{
    /// <summary>
    /// LogSettingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LogSettingWindow 
    {
        private ObservableCollection<FilterViewModel> _logFilters = new ObservableCollection<FilterViewModel>();
        public LogSettingWindow()
        {
            InitializeComponent();

            //if (ServiceManager.Instance != null)
            //{
            //    var uiService = ServiceManager.Instance.GetService<IUiService>();
            //    Owner = (Window) uiService.OwnerWindow;
            //}
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            var levels = (TraceLevel[])Enum.GetValues(typeof(TraceLevel));
            _logLevel.ItemsSource = levels;
            _logLevel.SelectedItem = Logger.LogLevel;

            _logFilterList.ItemsSource = _logFilters;

            showNativeLogButton.IsChecked = Logger.ShowLogType != LogType.DotNet;
            RefreshListClick(this, null);
        }
        
        private void _logLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TraceLevel level = (TraceLevel)_logLevel.SelectedItem;
            Logger.LogLevel = level;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GC.Collect();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Logger.ShowPath = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Logger.ShowPath = false;
        }

        private void RefreshListClick(object sender, RoutedEventArgs e)
        {
            _logFilters.Clear();
            lock(typeof(Logger))
            {
                foreach(var assemblyname in Logger.AssemblyFilters.ToList())
                {
                    _logFilters.Add( new FilterViewModel(assemblyname.Key, assemblyname.Value));
                }
            }
        }

        private void ResetListClick(object sender, RoutedEventArgs e)
        {
            foreach (var filter in _logFilters)
            {
                filter.TraceLevel = Logger.LogLevel;
            }
        }

        private void DisableAllClick(object sender, RoutedEventArgs e)
        {
            foreach (var filter in _logFilters)
            {
                filter.TraceLevel = TraceLevel.Off;
            }
        }

        private void OnDumpClick(object sender, RoutedEventArgs e)
        {
            //object[] allServices = ServiceManager.Instance.GetServices();
            //if (allServices != null)
            //{
            //    string dumpInfo = string.Format("{0}, start to dump the registered event handlers for all services", DateTime.Now.ToLongTimeString());
                
            //    DebugHelper debugHelper = new DebugHelper();
            //    foreach (var service in allServices)
            //    {
            //        try
            //        {
            //            dumpInfo += debugHelper.DumpAllEventHandlers(service);
            //        }
            //        catch (Exception ex)
            //        {
            //            Logger.WriteLineError("{0}", ex);
            //        }
            //    }

            //    Logger.ForceWriteLine(dumpInfo);
            //    Logger.Flush();
            //}
        }

        private void ShowNativeLogButton_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = (CheckBox) sender;
            Logger.ShowLogType = checkBox.IsChecked.GetValueOrDefault() ? LogType.Both : LogType.DotNet;
        }
    }

    [MarkupExtensionReturnType(typeof(Array))]
    public class TraceLevelEnumToItemsSourceExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(typeof(TraceLevel));
        }
    }
    public interface IDebugDump
    {
        string DumpEventHandlers();
    }
    public class DebugHelper
    {
        public string DumpAllEventHandlers(object target)
        {
            if (target == null) return string.Empty;

            EventInfo[] events = target.GetType().GetEvents(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
            StringBuilder dumpInfo = new StringBuilder();
            dumpInfo.AppendFormat("object: {0}\r\n", target.GetType().Name);
            if (events != null && events.Length != 0)
            {
                dumpInfo.Append("events-----------------\r\n");
                foreach (var eventInfo in events)
                {
                    string info = DumpEventHandlers(target, eventInfo.Name);
                    dumpInfo.Append(info);
                }
            }

            if (target is IDebugDump)
            {
                dumpInfo.AppendLine("Additional dump info");
                dumpInfo.AppendLine(((IDebugDump)target).DumpEventHandlers());
            }

            //// Also dump all properties
            //try
            //{
            //    PropertyInfo[] properties = target.GetType().GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
            //    if (properties.Length > 0)
            //    {
            //        dumpInfo.Append("\r\n Properties-----------------\r\n");
            //        //foreach (var propertyInfo in properties)
            //        //{
            //        //    if (propertyInfo != null)
            //        //    {
            //        //        try
            //        //        {
            //        //            object complexProperty = propertyInfo.GetValue(target, null);
            //        //            if (complexProperty == null) continue;

            //        //            string info = DumpAllEventHandlers(complexProperty);
            //        //            dumpInfo.Append(info);
            //        //        }
            //        //        catch (Exception ex)
            //        //        {
            //        //            dumpInfo.AppendLine(ex.Message);
            //        //        }
            //        //    }
            //        //}
            //    }
            //}
            //catch (Exception ex)
            //{
            //    dumpInfo.AppendLine(ex.Message);
            //}

            dumpInfo.AppendLine("-----------\r\n");

            return dumpInfo.ToString();
        }

        public string DumpEventHandlers(object target, string eventName)
        {
            StringBuilder dumpInfo = new StringBuilder();

            dumpInfo.AppendFormat("Event: {0}", eventName);
            Delegate[] delegates = GetObjectEventList(target, eventName);
            if (delegates == null || delegates.Length == 0)
            {
                dumpInfo.Append(" no handlers\r\n");
            }
            else
            {
                dumpInfo.AppendLine();
                foreach (var item in delegates)
                {
                    string info = DumpEventHandlers(item);
                    dumpInfo.AppendLine(info);
                }
            }
            return dumpInfo.ToString();
        }

        private string DumpEventHandlers(Delegate delegateItem)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("   Target: {0}              Method: {1}", delegateItem.Target == null ? "null" : delegateItem.Target.GetType().FullName, delegateItem.Method.Name);
            return sb.ToString();
        }


        /// <summary>  
        /// 获取对象事件 zgke@sina.com qq:116149  
        /// </summary>  
        /// <param name="myObject">对象</param>  
        /// <param name="eventName">事件名</param>  
        /// <returns>委托列</returns>  
        private Delegate[] GetObjectEventList(object myObject, string eventName)
        {
            System.Reflection.FieldInfo fieldInfo = myObject.GetType().GetField(eventName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (fieldInfo == null)
            {
                return null;
            }
            object fieldValue = fieldInfo.GetValue(myObject);

            if (fieldValue != null && fieldValue is Delegate)
            {
                Delegate objectDelegate = (Delegate)fieldValue;
                return objectDelegate.GetInvocationList();
            }
            return null;
        }

        ///// <summary>  
        ///// 获取控件事件  zgke@sina.com qq:116149  
        ///// </summary>  
        ///// <param name="control">对象</param>  
        ///// <param name="eventName">事件名 EventClick EventDoubleClick </param>  
        ///// <returns>委托列</returns>  
        //private Delegate[] GetObjectEventList(Control control, string eventName)
        //{
        //    PropertyInfo propertyInfo = control.GetType().GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
        //    if (propertyInfo != null)
        //    {
        //        object eventList = propertyInfo.GetValue(control, null);
        //        if (eventList != null && eventList is EventHandlerList)
        //        {
        //            EventHandlerList eventHandlerList = (EventHandlerList)eventList;
        //            FieldInfo fieldInfo = (typeof(Control)).GetField(eventName, BindingFlags.Static | BindingFlags.NonPublic);
        //            if (fieldInfo == null) return null;
        //            Delegate objectDelegate = eventHandlerList[fieldInfo.GetValue(control)];
        //            if (objectDelegate == null) return null;
        //            return objectDelegate.GetInvocationList();
        //        }
        //    }
        //    return null;
        //}
    }
    public class FilterViewModel : ViewModel
    {
        public FilterViewModel(string name, TraceLevel level)
        {
            _assemblyName = name;
            _traceLevel = level;
        }
        private string _assemblyName;
        public string AssemblyName
        {
            get { return _assemblyName; }
        }

        private TraceLevel _traceLevel;
        public TraceLevel TraceLevel
        {
            get { return _traceLevel; }
            set
            {
                if (_traceLevel != value)
                {
                    _traceLevel = value;
                    lock (typeof(Logger))
                    {
                        if (Logger.AssemblyFilters.ContainsKey(AssemblyName))
                        {
                            Logger.AssemblyFilters[AssemblyName] = value;
                        }
                     }
                    RaisePropertyChanged(() => this.TraceLevel);
                }
            }
        }
    }
}
