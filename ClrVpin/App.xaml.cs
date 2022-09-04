using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using ClrVpin.Models.Settings;
using Notification = ClrVpin.Shared.Notification;

namespace ClrVpin
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // override culture format so that the date picker format can be controlled
            // - https://stackoverflow.com/a/3869415/227110
            var ci = CultureInfo.CreateSpecificCulture(CultureInfo.CurrentCulture.Name);
            ci.DateTimeFormat.ShortDatePattern = "d/M/yyyy";
            ci.DateTimeFormat.ShortTimePattern = "HH:mm:ss";
            Thread.CurrentThread.CurrentCulture = ci;

            // Ensure the current culture passed into bindings is the OS culture.  By default, WPF uses en-US as the culture, regardless of the system settings.
            // - https://stackoverflow.com/questions/520115/stringformat-localization-issues-in-wpf
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

            WalkDictionary(Resources);
            base.OnStartup(e);

            Logging.Logger.Info($"App started: settings={JsonSerializer.Serialize(SettingsManager.Create().Settings)}");

            SetupExceptionHandling();
        }

        private static void WalkDictionary(ResourceDictionary resources)
        {
            // required for .net 3.5 to fix a lazy hydration strong reference memory leak
            // - https://stackoverflow.com/questions/6857355/memory-leak-when-using-sharedresourcedictionary
            //   http: //blog.lexique-du-net.com/index.php?post/2011/03/27/What-Dynamic-resources-creates-Memory-leaks-in-WPF-3.5-%28SP1%29
            // - load every resource in every resource dictionary to ensure no references to non-hydrated resources are made
            // - whilst no longer required for .net4, it's still useful (especially in combination with SharedResourceDictionary)..
            //   a. invoke resource initialization during app startup instead of on demand (i.e. when user is clicking through the UI)
            //   b. validate resources to find errors *before* they are used
            // - BUT NOT USED BECAUSE RESOURCES ARE DEFINED IN HOME.XAML INSTEAD OF APP.XAML --> refer app.xaml
            
            // ReSharper disable once UnusedVariable
            foreach (var unused in resources)
            {
            }
            
            foreach (var rd in resources.MergedDictionaries)
                WalkDictionary(rd);
        }

        private static void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                HandleError(s, (Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            };

            Current.DispatcherUnhandledException += (s, e) =>
            {
                HandleError(s, e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                HandleError(s, e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private static void HandleError(object sender, Exception exception, string source)
        {
            var assembly = Assembly.GetExecutingAssembly().GetName();
            const string title = "Sorry, something has gone wrong :(";
            const string subTitle = "Exiting this dialog will close the application and open the bug report web page. Please include the following information..\n" +
                                    "- steps to reproduce\n" +
                                    "- screenshot (if applicable)\n" +
                                    "- relevant portion of the log file: c:\\ProgramData\\ClrVpin\\logs\\ClrVpin.log";
            var detail = $"Message:  {exception.Message}\n" +
                         $"Assembly: {assembly}\n" +
                         $"Sender:   {sender}\n" +
                         $"Source:   {source}\n" +
                         $"Stack:\n{exception.StackTrace}";

            try
            {
                Logging.Logger.Error(exception, $"{title}\n{detail}");

                Current.MainWindow!.Show();

                Notification.ShowError("HomeDialog", title, subTitle, detail, true, true).ContinueWith(_ => SubmitBugAndExit());
            }
            catch (Exception ex)
            {
                // if the material window fails (e.g. MainWindow doesn't have a DialogHost available yet) then default back to the trusty windows message box
                MessageBox.Show(Current.MainWindow!, $"{title}\n\n{subTitle}\n\n{exception}", "An Error Has Occurred.  ClrVpin will be shutdown.", MessageBoxButton.OK, MessageBoxImage.Error);
                Logging.Logger.Error(ex, "Exception in HandleError");
                Environment.Exit(-2);
            }
            //finally
            //{
            //   Environment.Exit(-1);
            //}
        }

        private static void SubmitBugAndExit()
        {
            Process.Start(new ProcessStartInfo(@"https://github.com/stojy/ClrVpin/issues/new?assignees=&labels=&template=bug_report.md&title=Unhandled Error - <add summary description here>") { UseShellExecute = true });

            Environment.Exit(-1);
        }
    }
}