using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ClrVpin.Extensions;
using MaterialDesignThemes.Wpf;

namespace ClrVpin.Home;

// regarding property changed notification.. required for 2 purposes..
// 1. ensure UI responds (obviously)
// 2. prevent memory leaks
//    - a memory leak WILL occur if WPF binds to an object that is NOT a dependency object or implementing INotifyPropertyChanged
//    - because WPF instead uses PropertyDescriptors.AddValueChanged..
//      "causes the CLR to create a **strong reference** from the PropertyDescriptor to the object and in most cases the CLR will keep a reference to the PropertyDescriptor in a **global table**"
//    - https: //stackoverflow.com/questions/18542940/can-bindings-create-memory-leaks-in-wpf

// ReSharper disable once UnusedMember.Global
public partial class HomeWindow
{
    public HomeWindow()
    {
        // initialise encoding to workaround the error "Windows -1252 is not supported encoding name"
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        DataContext = new HomeViewModel(this);

        InitializeComponent();

        Activated += async (_, _) =>
        {
            if (!_activated)
            {
                _activated = true;
                Resources.WalkDictionary();

                Model.ScreenWorkArea = this.GetCurrentScreenSize();
            }

            // guard against multiple window activations
            if (Model.SettingsManager.WasReset && !_wasConfigResetHandled)
            {
                _wasConfigResetHandled = true;
                await DialogHost.Show(new RestartInfo
                {
                    Title = "Your settings have been reset",
                    Detail = "ClrVpin will now be restarted."
                }, "HomeDialog").ContinueWith(_ => Dispatcher.Invoke(Restart));
            }
        };

        Loaded += async (_, _) =>
        {
            if (VersionManagementService.ShouldCheck())
                await VersionManagementService.CheckAndHandle();
        };
    }

    private void ImageMouseDown(object sender, MouseButtonEventArgs e)
    {
        // drag the entire window when the image is dragged
        // - deliberately not applied to the entire window to avoid other draggable content, e.g. padding on buttons, misc text, etc
        // - https://stackoverflow.com/questions/7417739/make-wpf-window-draggable-no-matter-what-element-is-clicked
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private static void Restart()
    {
        Process.Start(Environment.ProcessPath!);
        Application.Current.Shutdown();
    }

    private bool _wasConfigResetHandled;
    private bool _activated;
}