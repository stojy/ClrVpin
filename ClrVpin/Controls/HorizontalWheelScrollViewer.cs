using System.Windows.Controls;
using System.Windows.Input;

namespace ClrVpin.Controls
{
    /// <summary>
    /// Horizontal wheel scrolling.  Required because the base control only supports vertical scrolling by default!
    /// </summary>
    public class HorizontalWheelScrollViewer : ScrollViewer
    {
        public HorizontalWheelScrollViewer()
        {
            // added these as defaults to avoid need for assigning in xaml every time
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            // explicitly translate wheel to left/right movements
            if (e.Delta < 0)
                ScrollInfo.MouseWheelRight();
            else 
                ScrollInfo.MouseWheelLeft();

            e.Handled = true;
        }
    }
}
