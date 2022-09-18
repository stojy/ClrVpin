using System.Diagnostics;
using System.Text;
using ClrVpin.Extensions;
using MaterialDesignThemes.Wpf;
using Application = System.Windows.Application;

namespace ClrVpin.Home;

// regarding property changed notification.. required for 2 purposes..
// 1. ensure UI responds (obviously)
// 2. prevent memory leaks
//    - a memory leak WILL occur if WPF binds to an object that is NOT a dependency object or implementing INotifyPropertyChanged
//    - because WPF instead uses PropertyDescriptors.AddValueChanged..
//      "causes the CLR to create a **strong reference** from the PropertyDescriptor to the object and in most cases the CLR will keep a reference to the PropertyDescriptor in a **global table**"
//    - https: //stackoverflow.com/questions/18542940/can-bindings-create-memory-leaks-in-wpf

// ReSharper disable once UnusedMember.Global
public partial class MainWindow
{
    public MainWindow()
    {
        // initialise encoding to workaround the error "Windows -1252 is not supported encoding name"
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        DataContext = new Model(this);

        InitializeComponent();

        Activated += async (_, _) =>
        {
            if (!_activated)
            {
                _activated = true;
                Resources.WalkDictionary();
            }

            Model.ScreenWorkArea = this.GetCurrentScreenWorkArea();

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
            if (VersionManagementView.ShouldCheck()) 
                await VersionManagementView.CheckAndHandle();
        };
    }

    private static void Restart()
    {
        Process.Start(Process.GetCurrentProcess().MainModule!.FileName!);
        Application.Current.Shutdown();
    }

    private bool _wasConfigResetHandled;
    private bool _activated;
}