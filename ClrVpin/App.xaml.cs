using System;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using ClrVpin.Models.Settings;

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
            Thread.CurrentThread.CurrentCulture = ci;

            // Ensure the current culture passed into bindings is the OS culture.  By default, WPF uses en-US as the culture, regardless of the system settings.
            // - https://stackoverflow.com/questions/520115/stringformat-localization-issues-in-wpf
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
            
            base.OnStartup(e);

            Logging.Logger.Info($"App started: settings={JsonSerializer.Serialize(SettingsManager.Create().Settings)}");

            SetupExceptionHandling();
        }

        private static void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HandleError(s, (Exception) e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            Current.DispatcherUnhandledException += (s, e) =>
            {
                HandleError(s, e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = false;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                HandleError(s, e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private static void HandleError(object sender, Exception exception, string source)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly().GetName();
                var message = "Unhandled exception detected\n\n" +
                              $"Message: {exception.Message}\n" +
                              $"Assembly: {assembly}\n" +
                              $"Sender: {sender}\n" +
                              $"Source: {source}";

                Logging.Logger.Error(exception, message);

                MessageBox.Show(Current.MainWindow!, $"{message}\n\n{exception}", "An Error Has Occurred.  ClrVpin will be shutdown.", MessageBoxButton.OK, MessageBoxImage.Error);

                // can't use the fancy material-ui dialog because it requires a visual tree with a DialogHost available
                //DialogHost.Show(new Message
                //{
                //    Title = "An error has occurred. Shutting down..",
                //    Detail = message
                //}).ContinueWith(_ => Environment.Exit(-1));
            }
            catch (Exception ex)
            {
                Logging.Logger.Error(ex, "Exception in HandleError");
            }
            finally
            {
                Environment.Exit(-1);
            }
        }
    }
}