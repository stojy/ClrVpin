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
            
            // winform dimensions are provided in physical pixels
            var screen = Screen.FromPoint(new Point((int) window.Left, (int) window.Top));
            
            // retrieve windows scaling information
            // - DpiScaleX/Y - multiplying factor relative to 96dpi
            var dpiScale = VisualTreeHelper.GetDpi(window);

            // convert resolution to WPF screen independent units
            // - WPF sizing does NOT match HW pixels.. the scaling setup is taken into account
            // - examples
            //   - e.g. 3840 pixel monitor at 150% scaling --> WPF max width = 3840 / 1.5 = 2560px
            //   - e.g. 3840 pixel monitor at 100% scaling --> WPF max width = 3840 / 1 = 3840px
            //   - e.g. 1920 pixel monitor at 100% scaling --> WPF max width = 1920 / 1 = 1920px
            //   --> so a WPF window.Width=1000px on 1920 @ 100% scaling (max width=1920) will be physically LARGER vs 3840 @ 150% scaling (max width=2560)
            // - according to SO post.. it's somehow(?) relative to 96dpi.. but i don't see how this can be true!
            return new Rect {Width = screen.WorkingArea.Width / dpiScale.DpiScaleX, Height = screen.WorkingArea.Height / dpiScale.DpiScaleY};
        }
    }
}