using System;
using System.Windows;

namespace CatLogger.Controls
{
    public class UiServiceAwareWindow : Window
    {
        private bool _recover;

        protected bool Recover
        {
            get { return _recover; }
            set
            {
                if (_recover != value)
                {
                    _recover = value;

                    //if (_recover)
                    //{
                    //    var uiService = ServiceManager.Instance.GetService<IUiService>();
                    //    if (uiService != null)
                    //    {
                    //        //        uiService.UnRegisterWindow(this);
                    //    }
                    //    Closed -= OnClosed;
                    //}
                    //else
                    //{
                    //    var uiService = ServiceManager.Instance.GetService<IUiService>();
                    //    if (uiService != null)
                    //    {
                    //        //          uiService.RegisterWindow(this);
                    //    }
                    //    Closed += OnClosed;
                    //}
                }
            }
        }

        public UiServiceAwareWindow()
        {
            //Closed += OnClosed;
            //_recover = false;
            //var uiService = ServiceManager.Instance.GetService<IUiService>();
            //if (uiService != null)
            //{
            //    //uiService.RegisterWindow(this);
            //}
        }

        private void OnClosed(object sender, EventArgs eventArgs)
        {
            //Closed -= OnClosed;
            //var uiService = ServiceManager.Instance.GetService<IUiService>();
            //if (uiService != null)
            //{
            //    //uiService.UnRegisterWindow(this);
            //}
        }
    }
}
