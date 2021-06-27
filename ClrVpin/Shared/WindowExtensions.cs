using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Point = System.Drawing.Point;

namespace ClrVpin.Shared
{
    public static class WindowExtensions
    {
        public static Rect GetCurrentScreenWorkArea(this Window window)
        {
            // https://stackoverflow.com/questions/1927540/how-to-get-the-size-of-the-current-screen-in-wpf
            // https://stackoverflow.com/questions/254197/how-can-i-get-the-active-screen-dimensions
            var screen = Screen.FromPoint(new Point((int) window.Left, (int) window.Top));
            var dpiScale = VisualTreeHelper.GetDpi(window);

            return new Rect {Width = screen.WorkingArea.Width / dpiScale.DpiScaleX, Height = screen.WorkingArea.Height / dpiScale.DpiScaleY};
        }
    }
}