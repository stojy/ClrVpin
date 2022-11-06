using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;

namespace ClrVpin.Extensions;

public static class WindowExtensions
{
    public static Size GetCurrentScreenSize(this Window window)
    {
        var screenRect = window.GetCurrentScreenRect();

        return new Size
        {
            Width = screenRect.Width,
            Height = screenRect.Height
        };
    }

    public static Point GetCurrentScreenPosition(this Window window)
    {
        var screenRect = window.GetCurrentScreenRect();

        return new Point
        {
            X = screenRect.X,
            Y = screenRect.Y
        };
    }

    public static void CentreInCurrentScreen(this Window window, Size newSize)
    {
        var screenRect = window.GetCurrentScreenRect();

        // reposition main window to maintain centering when the window size changes
        // - this is only done automatically by WPF during the first window load, not for any subsequent window resizing (e.g. collapsed content, larger images, etc)
        // - https://stackoverflow.com/questions/4019831/how-do-you-center-your-main-window-in-wpf
        window.Left = screenRect.X + (screenRect.Width - newSize.Width) / 2;
        window.Top = screenRect.Y + (screenRect.Height - newSize.Height) / 2;
    }

    private static Rect GetCurrentScreenRect(this Window window)
    {
        // DIU/pixel
        // - background info
        //   - https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/wpf-graphics-rendering-overview?redirectedfrom=MSDN&view=netframeworkdesktop-4.8#visual_rendering_behavior
        //   - https://www.quppa.net/blog/2011/01/07/pixel-measurements-in-wpf/
        // - SW vs HW
        //   - HW DPI: controlled by the HW manufacturer, e.g. Samsung 28" (diagonal) UR55 is 157ppi (3840px / 24.45in)
        //   - SW DPI: windows bases 100% scaling on 96dpi (virtual dots not pixels)
        //     - but also offers a 'screen scale override'.. when a user changes their screen scaling windows changes the virtual DPI as follows..
        //       a. 100% scaling --> 96dpi, i.e. 1 inch requires 96 dots --> a dot is not a pixel, but more dots requires more pixels
        //       b. 150% scaling --> 144dpi, i.e. 1 inch requires 144dpi --> 1 inch requires more physical pixels to be used, hence the inch appears larger
        // - WinForms vs WPF
        //   - WinForms: sizing is based on physical pixels
        //     - it does NOT consider Windows Scaling options
        //     - thus changing windows scaling (SW DPI) does NOT effect window sizing
        //   - WPF: sizing is based on device independent units (DIU)
        //     - DIU is indirectly DPI aware as they are scaled relative to 96dpi (considered as a normalized unit)
        //     - thus changing windows scale makes application visual smaller/larger, because the DIU specified coded in the app will require less/more physical pixels

        // screen position info
        // - https://stackoverflow.com/questions/1927540/how-to-get-the-size-of-the-current-screen-in-wpf
        // - https://stackoverflow.com/questions/254197/how-can-i-get-the-active-screen-dimensions
        // - retrieve screen dimensions (in pixels) via WinForms, since WPF (by design.. refer above) does not use pixels
        var screenInPixels = Screen.FromPoint(new System.Drawing.Point((int)window.Left, (int)window.Top));

        // retrieve windows scaling information from WPF.. DpiScaleX/Y
        // - the dpiScale is normalized so 1.0 = 96dpi
        var dpiScale = VisualTreeHelper.GetDpi(window);

        // convert resolution to WPF screen independent units
        // - WPF DIU = physical pixels / DPI relative to 96dpi
        // - examples
        //   - e.g. 3840 pixel monitor at 150% scaling --> WPF max width = 3840 / 1.5 = 2560diu
        //   - e.g. 3840 pixel monitor at 100% scaling --> WPF max width = 3840 / 1 = 3840diu
        //   - e.g. 1920 pixel monitor at 100% scaling --> WPF max width = 1920 / 1 = 1920diu
        //   --> so a WPF window.Width=1000px on 1920 @ 100% scaling (max width=1920) will be physically LARGER vs 3840 @ 150% scaling (max width=2560)
        return new Rect
        {
            X = (int)(screenInPixels.WorkingArea.X / dpiScale.DpiScaleX),
            Y = (int)(screenInPixels.WorkingArea.Y / dpiScale.DpiScaleY),
            Width = screenInPixels.WorkingArea.Width / dpiScale.DpiScaleX,
            Height = screenInPixels.WorkingArea.Height / dpiScale.DpiScaleY
        };
    }
}