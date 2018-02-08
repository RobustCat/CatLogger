using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;
using CatLogger.Interface;
using CatLogger.Models;
using CatLogger.Utilities;

namespace CatLogger.ViewModels
{    
    public class LoggerWindowViewModel : ViewModel
    {
        private const int MaxMessageNumber = 10000;
        private ObservableCollection<LoggingMessage> _loggingMessages = new ObservableCollection<LoggingMessage>();
        private IList<LoggingMessage> _buffLoggingMessages = new List<LoggingMessage>();
        private string _keywords;
        private string _excludeKeywords;
        private LoggingMessage _selectedMessage;
        private LoggingMessage _latestSelectedMessage;
        private string _currentFile;
        private string _errorMessage;
        private DispatcherTimer _timer;
        private DispatcherTimer _searchTimer;
     
        public EventHandler ScrollToSelectedItemHandler;
        public EventHandler SetTopWindowHandler;        

        public ObservableCollection<LoggingMessage> LoggingMessages
        {
            get { return _loggingMessages; }
            set
            {
                if (_loggingMessages != value)
                {
                    _loggingMessages = value;
                    RaisePropertyChanged(() => LoggingMessages);
                }
            }
        }

        public string KeyWords
        {
            get { return _keywords; }
            set
            {
                if (_keywords != value)
                {
                    _keywords = value;
                    RaisePropertyChanged(() => KeyWords);
                }
            }
        }

        public string ExcludeKeyWords
        {
            get { return _excludeKeywords; }
            set
            {
                if (_excludeKeywords != value)
                {
                    _excludeKeywords = value;
                    RaisePropertyChanged(() => ExcludeKeyWords);
                }
            }
        }

        public LoggingMessage SelectedMessage
        {
            get { return _selectedMessage; }
            set
            {
                if (_selectedMessage != value)
                {
                    _selectedMessage = value;
                    RaisePropertyChanged(() => SelectedMessage);
                }
            }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    RaisePropertyChanged(() => ErrorMessage);
                }
            }
        }       

        public IList<LogType> LogTypes { get; set; }

        public LogType SelectedLogType { get; set; }

        public IList<TraceLevel> LogLevels { get; set; }

        public TraceLevel SelectedLogLevel { get; set; }
        
        public bool IsOffChecked { get; set; }
        
        public bool IsErrorChecked { get; set; }
        
        public bool IsWarningChecked { get; set; }
        
        public bool IsInfoChecked { get; set; }
        
        public bool IsVerboseChecked { get; set; }      

        public bool IsTopWindowChecked { get; set; }

        public bool IsAutoSelectLastItem { get; set; }

        public bool IsPlugin { get; private set; }

        public LoggerWindowViewModel(bool standalone)
        {            
            ErrorMessage = string.Empty;              
            IsTopWindowChecked = true;
            IsAutoSelectLastItem = false;
            IsPlugin = !standalone;

            var types = (LogType[])Enum.GetValues(typeof(LogType));
            LogTypes = types;
            SelectedLogType = Logger.ShowLogType;

            var levels = (TraceLevel[])Enum.GetValues(typeof(TraceLevel));
            LogLevels = levels;
            SelectedLogLevel = Logger.LogLevel;
            if(standalone)
            {
                SelectedLogLevel = TraceLevel.Verbose;
            }
            RefreshLogLevelCheckboxStatus();
           
            CheckTraceLevel = new DelegateCommand<object>(CheckTraceLevelExecuted);
            ClearAll = new DelegateCommand<object>(ClearAllExecuted);
            KeyWordsChanged = new DelegateCommand<object>(KeyWordsChangedExecuted);            
            CheckTopWindow = new DelegateCommand<object>(CheckTopWindowExecuted);
            CheckAutoSelectLastItem = new DelegateCommand<object>(CheckAutoSelectLastItemExecuted);
            ItemSelectionChanged = new DelegateCommand<object>(ItemSelectionChangedExecuted);
            ShowNativeLogSelectionChanged = new DelegateCommand<object>(ShowNativeLogSelectionChangedExecuted);
            LogLevelSelectionChanged = new DelegateCommand<object>(LogLevelSelectionChangedExecuted);

            if (IsPlugin)
            {
                // _viewModel.OnPullLoggingMessagesTimeout(null, null);
                _timer = new DispatcherTimer(DispatcherPriority.Send);
                _timer.Interval = TimeSpan.FromMilliseconds(100);
                _timer.Tick += OnPullLoggingMessagesTimeout;
                _timer.Start();
                
                _searchTimer = new DispatcherTimer(DispatcherPriority.Send);
                _searchTimer.Interval = TimeSpan.FromMilliseconds(100);
                _searchTimer.Tick += OnInputKeywordsTimeOut;
            }         
        }

        public ICommand CheckTraceLevel { get; private set; }

        public ICommand ClearAll { get; private set; }

        public ICommand KeyWordsChanged { get; private set; }
       
        public ICommand CheckTopWindow { get; private set; }

        public ICommand CheckAutoSelectLastItem { get; private set; }

        public ICommand ItemSelectionChanged { get; private set; }

        public ICommand ShowNativeLogSelectionChanged { get; private set; }

        public ICommand LogLevelSelectionChanged { get; private set; }
         
        private void RefreshLogLevelCheckboxStatus()
        {
            switch (SelectedLogLevel)
            {
                case TraceLevel.Verbose:
                    IsOffChecked = true;
                    IsErrorChecked = true;
                    IsWarningChecked = true;
                    IsInfoChecked = true;
                    IsVerboseChecked = true;
                    break;
                case TraceLevel.Info:
                    IsOffChecked = true;
                    IsErrorChecked = true;
                    IsWarningChecked = true;
                    IsInfoChecked = true;
                    IsVerboseChecked = false;
                    break;
                case TraceLevel.Warning:
                    IsOffChecked = true;
                    IsErrorChecked = true;
                    IsWarningChecked = true;
                    IsInfoChecked = false;
                    IsVerboseChecked = false;
                    break;
                case TraceLevel.Error:                   
                    IsOffChecked = true;
                    IsErrorChecked = true;
                    IsWarningChecked = false;
                    IsInfoChecked = false;
                    IsVerboseChecked = false;
                    break;
                case TraceLevel.Off:
                    IsOffChecked = true;
                    IsErrorChecked = false;
                    IsWarningChecked = false;
                    IsInfoChecked = false;
                    IsVerboseChecked = false;
                    break;
            }

            RaisePropertyChanged(()=>IsOffChecked);
            RaisePropertyChanged(() => IsErrorChecked);
            RaisePropertyChanged(() => IsWarningChecked);
            RaisePropertyChanged(() => IsInfoChecked);
            RaisePropertyChanged(() => IsVerboseChecked);
        }

        private void CheckTraceLevelExecuted(object param)
        {
            if (IsPlugin) //keep in buffer if messages from file
            {
                _buffLoggingMessages.Clear();
            }
            LoggingMessages.Clear();
            OnPullLoggingMessagesTimeout(null, null);   
            if(!IsAutoSelectLastItem)
            {
                HandleScrollToSelectedItem();  
            }
        }

        private void ClearAllExecuted(object param)
        {
            lock (LoggerWindowTraceListener.Mutex)
            {
                if (LoggerWindowTraceListener.LoggingMessages != null && LoggerWindowTraceListener.LoggingMessages.Count > 0)
                {
                    LoggerWindowTraceListener.LoggingMessages.Clear();
                }                
            }

            if (null != _buffLoggingMessages && _buffLoggingMessages.Count > 0)
            {
                _buffLoggingMessages.Clear();
                LoggingMessages.Clear();
                _currentFile = string.Empty;
                FilterMessages();
            }
        }

        /// <summary>
        /// FilterMessages() - filter message via trace level and key words
        /// </summary>
        private void FilterMessages()
        {
            var messages = new List<LoggingMessage>();                          
            if (_buffLoggingMessages != null && _buffLoggingMessages.Count > 0)
            {
                var trimedKeywords = TrimString(_keywords);
                var pattern = KeywordsPattern(trimedKeywords);
                trimedKeywords = TrimString(_excludeKeywords);
                var excludePattern = ExcludeKeywordsPattern(trimedKeywords);
                foreach (var message in _buffLoggingMessages)
                {
                    bool matched = MatchedKeyWordsStatus(false, pattern, message.Content) && MatchedKeyWordsStatus(true, excludePattern, message.Content);
                    if (TraceLevel.Off == message.TraceLevel && IsOffChecked && matched)
                    {
                        messages.Add(message);
                    }

                    if (TraceLevel.Error == message.TraceLevel && IsErrorChecked && matched)
                    {
                        messages.Add(message);
                    }

                    if (TraceLevel.Warning == message.TraceLevel && IsWarningChecked && matched)
                    {
                        messages.Add(message);
                    }

                    if (TraceLevel.Info == message.TraceLevel && IsInfoChecked && matched)
                    {
                        messages.Add(message);
                    }

                    if (TraceLevel.Verbose == message.TraceLevel && IsVerboseChecked && matched)
                    {
                        messages.Add(message);
                    }
                }                    
            }            

            if (messages.Count > 0)
            {
                foreach (var loggingMessage in messages)
                {
                    LoggingMessages.Add(loggingMessage);
                }
                                
                if(IsAutoSelectLastItem)
                {
                    SelectedMessage = messages[messages.Count - 1];
                    HandleScrollToSelectedItem();  
                }
                else
                {
                    SelectedMessage = _latestSelectedMessage;

                    if (SelectedMessage == null || !LoggingMessages.Contains(SelectedMessage))
                    {                        
                        _latestSelectedMessage = SelectedMessage;
                    } 
                }                
            }            
        }

        private void KeyWordsChangedExecuted(object param)
        {
            if (IsPlugin)
            {
                 _searchTimer.Stop();
                 _searchTimer.Start();
            }
            else
            {
                OnInputKeywordsTimeOut(null, null);
            }
                            
        }

        private void OnInputKeywordsTimeOut(object sender, EventArgs eventArgs)
        {
             if (IsPlugin) //keep in buffer if messages from file
            {
                _buffLoggingMessages.Clear();                               
            }
            LoggingMessages.Clear();
            OnPullLoggingMessagesTimeout(null, null);
            if (IsPlugin) 
            {
                _searchTimer.Stop();
            }
        }

        private void HandleScrollToSelectedItem()
        {            
            var handler = ScrollToSelectedItemHandler;
            if (handler != null)
            {
                handler(null, new EventArgs());
            }
            else
            {
                ErrorMessage = "Scroll to selected item handler is null";
            }
        }


        /// <summary>
        /// MatchedKeyWordsStatus - matching key words
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private bool MatchedKeyWordsStatus(bool exclude, string pattern, string message)
        {            
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            if (string.IsNullOrEmpty(pattern))
            {
                return true;
            }
            
            try
            {
                var regex = new Regex(@pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                MatchCollection matches = regex.Matches(message);
                if (!exclude)
                {
                    if (matches.Count > 0)
                    {
                        return true;
                    }

                    return false;
                }

                //Exclude keywords filtering
                if (exclude)
                {
                    if (matches.Count > 0)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception exception)
            {                
                //throw new Exception(exception.Message);
                ErrorMessage = exception.Message + " Please reopen the logger window.";
            }                       

            return false;
        }

        private string KeywordsPattern(string trimedKeywords)
        {
            if(string.IsNullOrEmpty(trimedKeywords))
            {
                return string.Empty;
            }

            var keyWords = trimedKeywords;
            string pattern = keyWords.Replace("(", "\\(").Replace(")", "\\)").Replace("[", "\\[").Replace("]", "\\]"); 
                     
            if (keyWords.IndexOf(" ") > 0)
            {
                keyWords = keyWords.Replace(" ", @".*");
                keyWords = keyWords.Replace("(", "\\(").Replace(")", "\\)").Replace("[", "\\[").Replace("]", "\\]"); 
                pattern = string.Format(@"^.*{0}.*", keyWords);
            }                                    

            return pattern;
        }

        /// <summary>
        /// ExcludeKeywordsPattern
        /// </summary>
        /// <param name="trimedKeywords"></param>
        /// <returns></returns>
        private string ExcludeKeywordsPattern(string trimedKeywords)
        {
            if (string.IsNullOrEmpty(trimedKeywords))
            {
                return string.Empty;
            }

            var keyWords = trimedKeywords;
            string pattern = keyWords.Replace("(", "\\(").Replace(")", "\\)").Replace("[", "\\[").Replace("]", "\\]");
         
            //Exclude keywords filtering            
            if (keyWords.IndexOf(" ") > 0)
            {
                keyWords = keyWords.Replace(" ", "|");
                keyWords = keyWords.Replace("(", "\\(").Replace(")", "\\)").Replace("[", "\\[").Replace("]", "\\]");
                pattern = string.Format(@"\w*({0})\w*", keyWords);
            }
            
            return pattern;
        }

        public void OnPullLoggingMessagesTimeout(object sender, EventArgs eventArgs)
        {
            try
            {
                bool hasNewMessages = false;
                LoggingMessage[] arraylistenerLoggingMessages;
                lock (LoggerWindowTraceListener.Mutex)
                {
                    if (LoggerWindowTraceListener.LoggingMessages == null)
                    {
                        PullLoggingMessagesFromLoggerFile(_currentFile);
                        return;
                    }

                    arraylistenerLoggingMessages = LoggerWindowTraceListener.LoggingMessages.ToArray();
                }

                if (LoggingMessages.Count > LoggerWindowTraceListener.MaxMessageNumber)
                {
                    //clear half items if message number is larger than Max Message Number
                    //LoggerWindowTraceListener.LoggingMessages = LoggerWindowTraceListener.LoggingMessages.Where((value, index) => index >= (MaxMessageNumber / 2)).ToList();
                    var messages = LoggingMessages.Where((value, index) => index >= (MaxMessageNumber / 2)).ToList();
                    LoggingMessages = new ObservableCollection<LoggingMessage>(messages);
                }

                var listListenerLoggingMessages = arraylistenerLoggingMessages.ToList();
                if (listListenerLoggingMessages.Count > 0)
                {
                    if (_buffLoggingMessages.Count > 0)
                    {
                        var lastLoggingMessage = _buffLoggingMessages[_buffLoggingMessages.Count - 1];
                        int lastIndex = listListenerLoggingMessages.IndexOf(lastLoggingMessage);

                        if (lastIndex == -1)
                        {
                            _buffLoggingMessages.Clear();
                        }
                        else if (lastIndex < listListenerLoggingMessages.Count - 1)
                        {
                            _buffLoggingMessages.Clear();
                            int newMessages = listListenerLoggingMessages.Count - lastIndex - 1;
                            var arrayLoggingMessages = new LoggingMessage[newMessages];
                            listListenerLoggingMessages.CopyTo(lastIndex + 1, arrayLoggingMessages, 0, newMessages);
                            _buffLoggingMessages = arrayLoggingMessages.ToList();
                            hasNewMessages = true;
                        }
                    }
                    else
                    {
                        var arrayLoggingMessages = new LoggingMessage[listListenerLoggingMessages.Count];
                        listListenerLoggingMessages.CopyTo(0, arrayLoggingMessages, 0, listListenerLoggingMessages.Count);
                        _buffLoggingMessages = arrayLoggingMessages.ToList();
                        hasNewMessages = true;
                    }
                }

                if (hasNewMessages)
                {
                    FilterMessages();
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// PullLoggingMessagesFromLoggerFile - pull messages from dragging file
        /// </summary>
        /// <param name="fileName"></param>
        public void PullLoggingMessagesFromLoggerFile(string fileName)
        {         
            if(string.IsNullOrEmpty(fileName))
            {
                return;
            }

            try
            {
                ErrorMessage = string.Empty; 
                if(fileName == _currentFile)
                {
                    LoggingMessages.Clear();
                    FilterMessages();        
                    return;
                }

                using (var streamReader = new StreamReader(fileName))
                {                    
                    string line;                    
                    var messages = new List<LoggingMessage>();
                    _buffLoggingMessages.Clear();
                    LoggingMessages.Clear();
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        var message = new LoggingMessage { Content = line, TraceLevel = TraceLevel.Off};
                        if (line.Contains("-I ; "))
                        {
                            message.TraceLevel = TraceLevel.Info;
                        }
                        else if (line.Contains("-W ; "))
                        {
                            message.TraceLevel = TraceLevel.Warning;
                        }
                        else if (line.Contains("-E ; "))
                        {
                            message.TraceLevel = TraceLevel.Error;
                        }
                        else if (line.Contains("-V"))
                        {
                            message.TraceLevel = TraceLevel.Verbose;
                        }
                        messages.Add(message);                        
                    }

                    _currentFile = fileName;
                    if(messages.Count > 0)
                    {                      
                        _buffLoggingMessages = messages;                                                                   
                        FilterMessages();                        
                    }
                }
            }
            catch (Exception e) 
            {
                ErrorMessage = e.Message;                
            }                                         
        }
       
        /// <summary>
        /// TrimString - trim left and right empty character, merge several continous empty characters to one character
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string TrimString(string str)
        {
            if(string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            string trimString = str.Trim();
            int index = trimString.IndexOf(' ');
            if(index > 0)
            {
                StringBuilder sb = new StringBuilder();
                while (index > 0)
                {
                    sb.Append(trimString.Substring(0, index));
                    sb.Append(" ");
                    trimString = trimString.Substring(index + 1).Trim();
                    index = trimString.IndexOf(' ');
                    if (index <= 0)
                    {
                        sb.Append(trimString);
                    }
                }

                return sb.ToString().Trim();
            }
            
            return trimString;            
        }

        private void CheckTopWindowExecuted(object param)
        {
            HandleSetTopWindow(IsTopWindowChecked);          
        }

        private void HandleSetTopWindow(object o)
        {
            var handler = SetTopWindowHandler;
            if (handler != null)
            {
                handler(o, new EventArgs());
            }
            else
            {
                ErrorMessage = "Set top Window handler is null";
            }
        }

        /// <summary>
        /// ItemSelectionChangedExecuted - set last selected item to buffer before fresh
        /// </summary>
        /// <param name="param"></param>
        private void ItemSelectionChangedExecuted(object param)
        {
            if (SelectedMessage != null)
            {
                _latestSelectedMessage = SelectedMessage;
            }      
        }   
     
        /// <summary>
        /// CheckAutoSelectLastItemExecuted - set auto select last item
        /// </summary>
        /// <param name="param"></param>
        private void CheckAutoSelectLastItemExecuted(object param)
        {
            if (IsPlugin) //keep in buffer if messages from file
            {
                _buffLoggingMessages.Clear();
            }
            LoggingMessages.Clear();
            OnPullLoggingMessagesTimeout(null, null);     
        }

        /// <summary>
        /// ShowNativeLogExecuted - set if showing native log
        /// </summary>
        /// <param name="param"></param>
        private void ShowNativeLogSelectionChangedExecuted(object param)
        {
            Logger.ShowLogType = SelectedLogType;            
            _buffLoggingMessages.Clear();
            LoggingMessages.Clear();
            OnPullLoggingMessagesTimeout(null, null);          
        }

        /// <summary>
        /// LogLevelSelectionChangedExecuted - set log level when selection changed
        /// </summary>
        /// <param name="param"></param>
        private void LogLevelSelectionChangedExecuted(object param)
        {
            Logger.LogLevel = SelectedLogLevel;
            RefreshLogLevelCheckboxStatus();
            _buffLoggingMessages.Clear();
            LoggingMessages.Clear();
            OnPullLoggingMessagesTimeout(null, null);          
        }
    }
}