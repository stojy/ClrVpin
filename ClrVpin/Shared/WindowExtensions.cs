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
            
            // winform dimensions are provided in pixels
            var screen = Screen.FromPoint(new Point((int) window.Left, (int) window.Top));
            
            // retrieve windows scaling information
            // - DpiScaleX/Y - multiplying factor relative to 96dpi
            var dpiScale = VisualTreeHelper.GetDpi(window);

            // convert resolution to WPF screen independent units, i.e. all WPF sizing is relative to 96dpi --> BUT, a 'dot' does NOT equate to a physical pixel!
            // - e.g. 3840 pixel monitor at 150% scaling --> WPF width = 3840 / 1.5 = 2560px
            // - e.g. 3840 pixel monitor at 100% scaling --> WPF width = 3840 / 11 = 3840px
            // - e.g. 1920 pixel monitor at 100% scaling --> WPF width = 1920 / 11 = 1920px
            // - i.e. WPF window.Width=1000px on 1920 @ 100% scaling (max width=1920) will be LARGER vs 3840 @ 150% scaling (max width=2560)
            return new Rect {Width = screen.WorkingArea.Width / dpiScale.DpiScaleX, Height = screen.WorkingArea.Height / dpiScale.DpiScaleY};
        }
    }
}