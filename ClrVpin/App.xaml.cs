using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace ClrVpin
{
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Logging.Logger.Info($"ClrVPin started - v{Assembly.GetEntryAssembly()?.GetName().Version}");

            SetupExceptionHandling();
        }

        public static void SetupExceptionHandling()
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