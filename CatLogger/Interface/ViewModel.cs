using System;

namespace CatLogger.Interface
{
    public abstract class ViewModel : NotificationObject, IViewModel
    {
        #region Fields and Events

        // Fields 
        private string _name;
        private string _description;
        private bool _isEnabled = true;
        private bool _isVisible = true;
        // Events 

        /// <summary>
        /// Notifies that the value for <see cref="IsEnabled"/> property has changed. 
        /// </summary>
        public event EventHandler IsEnabledChanged;

        #endregion Fields and Events

        #region Properties

        // Public 

        protected ViewModel()
        {

        }

        protected ViewModel(string description, bool isEnabled)
        {
            _description = description;
            _isEnabled = isEnabled;
        }

        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged(() => Name);
            }
        }

        /// <summary>
        /// Gets or sets a brief, friendly description suitable for display in UI.
        /// </summary>
        public string Description
        {
            get { return _description; }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnDescriptionChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ViewModel"/> is enabled in the logic and UI.
        /// The default value is true.
        /// </summary>
        /// <value>
        /// true if the <see cref="ViewModel"/> is enabled; otherwise, false.
        /// </value>
        public bool IsEnabled
        {
            get { return GeIsEnabled(); }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnIsEnabledChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicate whether this view model is endabled.
        /// </summary>
        /// <returns></returns>
        /// <remarks>If you choose to override this implementation, make certain that you call the base method.</remarks>
        protected virtual bool GeIsEnabled()
        {
            return _isEnabled;
        }

        public string Display
        {
            get { return GetDisplay(); }
        }

        /// <summary>
        /// Gets a value indicating whether this ViewModel is enabled.(value of field _isEnabled.)
        /// </summary>
        protected bool IsEnabledCore { get { return _isEnabled; } }

        /// <summary>
        /// Gets or sets the user interface (UI) visibility of this <see cref="IViewModel"/>.
        /// </summary>
        /// <value>
        /// The default value is <c>true</c>.
        /// </value>
        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                if (OnIsVisibleChanging(value))
                {
                    _isVisible = value;
                    OnIsVisibleChanged();
                }
            }
        }

        #endregion Properties

        #region Methods

        // Protected 
        protected virtual bool OnIsVisibleChanging(bool isVisible)
        {
            return _isVisible != isVisible;
        }

        /// <summary>
        /// Notifies that the value for <see cref="IsVisible"/> property has changed. 
        /// </summary>
        /// <remarks>
        /// If you choose to override this implementation, make certain that you call the base method.
        /// </remarks>
        protected virtual void OnIsVisibleChanged()
        {
            RaisePropertyChanged(() => IsVisible);
        }

        /// <summary>
        /// Notifies that the value for <see cref="IsEnabled"/> property has changed. 
        /// </summary>
        /// <remarks>
        /// If you choose to override this implementation, make certain that you call the base method.
        /// </remarks>
        protected virtual void OnIsEnabledChanged()
        {
            EventHandler handler = IsEnabledChanged;
            if (handler != null) handler(this, EventArgs.Empty);
            RaisePropertyChanged(() => IsEnabled);
        }
        /// <summary>
        /// Notifies that the value for <see cref="Description"/> property has changed. 
        /// </summary>
        protected virtual void OnDescriptionChanged()
        {
            RaisePropertyChanged(() => Description);
        }

        protected virtual string GetDisplay()
        {
            return Description;
        }
        #endregion Methods
    }
}
