using System.Diagnostics;
using System.Windows.Media;

namespace CatLogger.Models
{
    public class LoggingMessage
    {
        private static int _index = 0;
        public LoggingMessage()
        {
            Index = _index ++;
        }
        public int Index { get; private set; }
        public TraceLevel TraceLevel { get; set; }

        public string Content { get; set; }

        public SolidColorBrush Color
        {
            get
            {
                if (TraceLevel.Error == TraceLevel)
                {
                    return Brushes.Red;
                }

                if (TraceLevel.Warning == TraceLevel)
                {
                    return Brushes.Goldenrod;
                }

                if (TraceLevel.Info == TraceLevel)
                {
                    return Brushes.Blue;
                }

                if (TraceLevel.Verbose == TraceLevel)
                {
                    return Brushes.Purple;
                }

                return Brushes.Black;
            }
        }

        public override string ToString()
        {
            return Content;
        }
    }
}