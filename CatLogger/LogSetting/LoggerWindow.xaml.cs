using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CatLogger.Models;
using CatLogger.Utilities;
using CatLogger.ViewModels;

namespace CatLogger.LogSetting
{
    /// <summary>
    /// LoggerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoggerWindow 
    {
        private static LoggerWindow _self;
        private static Thread _loggerWindowThread;
        private static object _mutex = new object();
        private static bool standalone = false;
        private int dragEnterNumber;
        private LoggerWindowViewModel _viewModel;
       
        public LoggerWindow()
        {            
            InitializeComponent();

            _viewModel = new LoggerWindowViewModel(standalone);
            _viewModel.ScrollToSelectedItemHandler = ScrollToSelectedItem;
            _viewModel.SetTopWindowHandler = SetTopWindow;
            
            DataContext = _viewModel;
            dragEnterNumber = 0;
        }               

        /// <summary>
        /// ShowEx - Start a thread to show logger window
        /// </summary>        
        public static void ShowEx()
        {
            LoggerWindowTraceListener.Active();if (_loggerWindowThread == null)
            {
                _loggerWindowThread = ThreadManager.Create("ShowLogEx", ShowWindowThread, null, ApartmentState.STA);
            }
        }

        private static uint ShowWindowThread(object args)
        {
            _self = new LoggerWindow();
            _self.Topmost = true;
            _self.Closed += OnWindowClosed;
            _self.ShowDialog();
            return 0;
        }

        public static void ShowWindowEx(EventHandler closeHandler)
        {
            if (_loggerWindowThread == null)
            {
                _loggerWindowThread = ThreadManager.Create("ShowWindowEx", ShowWindowThreadWithClodeHandler, closeHandler, ApartmentState.STA);            
            }
        }

        private static uint ShowWindowThreadWithClodeHandler(object param)
        {
            var closeHandler = (EventHandler)param;
            standalone = true;
            _self = new LoggerWindow();            
            _self.Closed += OnWindowClosed;
            _self.Closed += closeHandler;           
            _self.ShowDialog();
            return 0;
        }
       
        private void ScrollToSelectedItem(object sender, EventArgs e)
        {
            lock (_mutex)
            {
                if (_self != null)
                {
                    Dispatcher dispatcher = Dispatcher.FromThread(_loggerWindowThread);
                    if (dispatcher != null)
                    {
                        dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(ScrollToSelectedItem));
                    }
                }               
            }
        }

        private void ScrollToSelectedItem()
        {
            lock (_mutex)
            {
                if (_self != null)
                {
                    if (lstLogger.Items.Count > 0)
                    {
                        lstLogger.ScrollIntoView(_viewModel.SelectedMessage == null
                                                     ? lstLogger.Items[lstLogger.Items.Count - 1]
                                                     : lstLogger.SelectedItem);
                    }
                }
            }
        }

        private void SetTopWindow(object sender, EventArgs e)
        {
            lock (_mutex)
            {
                if (_self != null)
                {
                    Dispatcher dispatcher = Dispatcher.FromThread(_loggerWindowThread);
                    if (dispatcher != null)
                    {
                        dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action<bool>(SetTopWindow), (bool)sender);
                    }
                }
            }
        }

        private void SetTopWindow(bool isTopWindow = false)
        {
            lock (_mutex)
            {
                if (_self != null)
                {
                    _self.Topmost = isTopWindow;
                }
            }
        }              

        /// <summary>
        /// Executed event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            lock (_mutex)
            {
                if (lstLogger.SelectedItems != null && lstLogger.SelectedItems.Count > 0)
                {
                    // 8M
                    StringBuilder sb = new StringBuilder(8000000);
                    List<LoggingMessage> messages = new List<LoggingMessage>();
                    foreach (var item in lstLogger.SelectedItems)
                    {
                        messages.Add((LoggingMessage)item);
                    }
                    messages.Sort(OnCompare);
                    foreach (var item in messages)
                    {
                        sb.AppendLine(item.Content);
                    }
                    Clipboard.SetText(sb.ToString());
                }
            }
        }

        private int OnCompare(LoggingMessage x, LoggingMessage y)
        {
            return x.Index - y.Index;
        }

        /// <summary>
        /// CanExecute event handler. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CanCopyExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            lock (_mutex)
            {                 
                // If a item selected, then set CanExecute to true. 
                if (lstLogger.SelectedItem != null)
                {
                    e.CanExecute = true;
                }
                    // if there is not a item selected, then set CanExecute to false. 
                else
                {
                    e.CanExecute = false;
                }
            }
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {           
        }

        private static void OnWindowClosed(object sender, EventArgs e)
        {
            lock (_mutex)
            {
                _self = null;
                _loggerWindowThread = null;
            }
        }

        /// <summary>
        /// Request to close the window
        /// </summary>
        public static void CloseEx()
        {
            lock (_mutex)
            {
                if (_self != null)
                {
                    Dispatcher dispatcher = Dispatcher.FromThread(_loggerWindowThread);
                    if (dispatcher != null)
                    {
                        dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(CloseMe));
                    }
                }
            }
        }

        private static void CloseMe()
        {
            lock (_mutex)
            {
                if (_self != null)
                {
                    _self.Close();
                }
            }
        }       

        private void lstLogger_DragEnter(object sender, DragEventArgs e)
        {
            if(dragEnterNumber > 0)
            {
                dragEnterNumber--;
                return;
            }
            var filepaths = (string[]) e.Data.GetData(DataFormats.FileDrop, false);
            if(filepaths.Length == 1)
            {
                //if (File.Exists(filepaths[0]))
                {
                    const int MaxLength = 100000000;
                    var fileInfo = new FileInfo(filepaths[0]);
                    if(fileInfo.Length > MaxLength)
                    {
                        _viewModel.ErrorMessage = "This file size is larger than 100M, please select a smaller one!";
                    }
                    else
                    {
                        _viewModel.PullLoggingMessagesFromLoggerFile(filepaths[0]);
                    }
                }               
            }
            else
            {
                _viewModel.ErrorMessage = "Please dragging one file!";                
            }

            dragEnterNumber++;
        }       
    }
}
