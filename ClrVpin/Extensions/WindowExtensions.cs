using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Point = System.Drawing.Point;

namespace ClrVpin.Extensions
{
    public static class WindowExtensions
    {
        public static Rect GetCurrentScreenWorkArea(this Window window)
        {
            // DIU/pixel background info
            // - https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/wpf-graphics-rendering-overview?redirectedfrom=MSDN&view=netframeworkdesktop-4.8#visual_rendering_behavior
            // - https://www.quppa.net/blog/2011/01/07/pixel-measurements-in-wpf/
            // - Win Forms: apps are non-DPI aware, i.e. their layout is based on physical pixels, thus changing the scaling has no effect
            // - WPF: apps are (indirectly) DPI aware, i.e. their layout is based on DIU (device independent units) --> thus changing scale makes application visual smaller/larger
            // - when a user changes their screen scaling.. windows changes the DPI as follows..
            //   a. 100% scaling --> 1.0 (96dpi)
            //   b. 150% scaling --> 1.5 (144dpi) --> i.e. an inch requires more physical pixels, hence the inch appears larger
            // - WPF's DIU is DPI aware, and will thus draw applications at different size when scaling is changed
            
            // screen position info
            // - https://stackoverflow.com/questions/1927540/how-to-get-the-size-of-the-current-screen-in-wpf
            // - https://stackoverflow.com/questions/254197/how-can-i-get-the-active-screen-dimensions

            // winform dimensions are provided in physical pixels
            var screen = Screen.FromPoint(new Point((int)window.Left, (int)window.Top));

            // retrieve windows scaling information.. DpiScaleX/Y
            // - multiplying factor is relative to 96dpi
            // - examples
            var dpiScale = VisualTreeHelper.GetDpi(window);

            // convert resolution to WPF screen independent units
            // - WPF sizing deliberately does NOT match HW pixels.. the scaling setup is taken into account --> thus apps are DELIBERATELY smaller/bigger based on the DPI
            // - examples
            //   - e.g. 3840 pixel monitor at 150% scaling --> WPF max width = 3840 / 1.5 = 2560px
            //   - e.g. 3840 pixel monitor at 100% scaling --> WPF max width = 3840 / 1 = 3840px
            //   - e.g. 1920 pixel monitor at 100% scaling --> WPF max width = 1920 / 1 = 1920px
            //   --> so a WPF window.Width=1000px on 1920 @ 100% scaling (max width=1920) will be physically LARGER vs 3840 @ 150% scaling (max width=2560)
            return new Rect { Width = screen.WorkingArea.Width / dpiScale.DpiScaleX, Height = screen.WorkingArea.Height / dpiScale.DpiScaleY };
        }

        public static Point GetCurrentScreenPosition(this Window window)
        {
            // retrieve current screen via winform - dimensions provided in pixels
            var screen = Screen.FromPoint(new Point((int)window.Left, (int)window.Top));

            // convert position to WPF screen independent units
            var dpiScale = VisualTreeHelper.GetDpi(window);

            return new Point { X = (int)(screen.WorkingArea.X / dpiScale.DpiScaleX), Y = (int)(screen.WorkingArea.Y / dpiScale.DpiScaleY) };
        }
    }
}