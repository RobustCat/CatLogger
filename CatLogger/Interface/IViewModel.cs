using System.ComponentModel;

namespace CatLogger.Interface
{
    public interface IViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets a brief, friendly description suitable for display in UI.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IViewModel"/> is enabled in the logic and UI.
        /// </summary>
        /// <value>
        /// true if the <see cref="IViewModel"/> is enabled; otherwise, false.
        /// The default value is true.
        /// </value>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the user interface (UI) visibility of this <see cref="IViewModel"/>.
        /// </summary>
        /// <value>
        /// The default value is <c>true</c>.
        /// </value>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets a string which shall represents what's displayed on the control
        /// </summary>
        string Display { get; }
    }
}
