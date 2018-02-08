using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;

namespace CatLogger
{
    public abstract class NotificationObject : DependencyObject, INotifyPropertyChanged
    {
        #region Fields and Events

        // Fields 

        private NotificationTransaction _transaction;
        // Events 

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary> 
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        #endregion Fields and Events

        #region Methods

        // Methods 

        /// <summary>
        /// Extracts the property name from a property expression.
        /// </summary>
        /// <typeparam name="T">The object type containing the property specified in the expression.</typeparam>
        /// <param name="propertyExpression">The property expression (e.g. p => p.PropertyName)</param>
        /// <returns>The name of the property.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="propertyExpression"/> is null.</exception>
        /// <exception cref="MemberExpression">Thrown when the expression is:<br/>
        ///     Not a <see cref="ArgumentNullException"/><br/>
        ///     The <see cref="MemberExpression"/> does not represent a property.<br/>
        ///     Or, the property is static.
        /// </exception>
        public static string ExtractPropertyName<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
            {
                throw new ArgumentNullException("propertyExpression");
            }
            MemberExpression memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
            {
                throw new ArgumentException(string.Format("Expression is invalid"));
            }
            PropertyInfo property = memberExpression.Member as PropertyInfo;
            if (property == null)
            {
                throw new ArgumentException(string.Format("Expression is invalid"));
            }
            if (property.GetGetMethod(true).IsStatic)
            {
                throw new ArgumentException(string.Format("Expression is invalid"));
            }
            return memberExpression.Member.Name;
        }

        // Methods
        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (_transaction != null && _transaction.Counter > 0)
            {
                _transaction.RaisePropertyChanged(propertyName);
            }
            else
            {
                InnerRaisePropertyChanged(propertyName);
            }
        }

        private void InnerRaisePropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Raises this object's PropertyChanged event for each of the properties.
        /// </summary>
        /// <param name="propertyNames">The properties that have a new value.</param>
        protected void RaisePropertyChanged(params string[] propertyNames)
        {
            if (propertyNames == null)
            {
                throw new ArgumentNullException("propertyNames");
            }
            foreach (string name in propertyNames)
            {
                RaisePropertyChanged(name);
            }
        }

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <typeparam name="T">The type of the property that has a new value</typeparam>
        /// <param name="propertyExpression">A Lambda expression representing the property that has a new value.</param>
        protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            string propertyName = ExtractPropertyName(propertyExpression);
            RaisePropertyChanged(propertyName);
        }

        /// <summary>
        ///  Create an instance of locker which will prevent PropertyChanged notification until it's disposed
        /// </summary>
        /// <returns>An instance of locker</returns>
        public IDisposable CreateTransaction()
        {
            if (_transaction != null)
            {
                _transaction.Counter++;
            }
            else
            {
                _transaction = new NotificationTransaction(this);
            }
            return _transaction;
        }

        #endregion Methods

        #region Nested Classes


        /// <summary>
        ///     A Updated event Locker.
        ///     Delay raise Updated untill disposed.
        /// </summary>
        private class NotificationTransaction : IDisposable
        {
            #region Fields and Events

            // Fields 

            private NotificationObject _owner;
            private readonly List<string> _modifiedProperties;

            #endregion Fields and Events

            #region Properties

            /// <summary>
            ///     Gets or sets the counter, when referenced, counter will be increased, when disposed, counter will be decreased.
            ///     If counter reaches 0 after dispose, the lock will be unlocked and notificaton shall be sent.
            /// </summary>
            public int Counter { get; set; }

            #endregion Properties

            #region Methods

            // Constructors 

            public NotificationTransaction(NotificationObject owner)
            {
                _owner = owner;
                if (_owner == null)
                {
                    throw new ArgumentNullException("owner");
                }
                _modifiedProperties = new List<string>();
                Counter++;
            }
            // Methods 

            public void RaisePropertyChanged(string propertyName)
            {
                if (!_modifiedProperties.Contains(propertyName))
                {
                    _modifiedProperties.Add(propertyName);
                }
            }

            void IDisposable.Dispose()
            {
                Counter--;
                if (Counter > 0) return;
                if (_modifiedProperties.Count > 0)
                {
                    foreach (var modifiedProperty in _modifiedProperties)
                    {
                        _owner.InnerRaisePropertyChanged(modifiedProperty);
                    }
                }
                _owner._transaction = null;
                _owner = null;
            }

            #endregion Methods
        }
        #endregion Nested Classes


        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                var msg = "Invalid property name: " + propertyName;

                if (ThrowOnInvalidPropertyName)
                    throw new InvalidOperationException(msg);
                Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected bool ThrowOnInvalidPropertyName { get; set; }

        #endregion
    }
}
