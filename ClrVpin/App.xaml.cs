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

            Logging.Logger.Info("Starting ClrVPin..");

            SetupExceptionHandling();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HandleError(s, (Exception) e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
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

        private void HandleError(object sender, Exception exception, string source)
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
                MessageBox.Show(MainWindow!, $"{message}\n\n{exception}", "An Error Has Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Logging.Logger.Error(ex, "Exception in HandleError");
            }
            finally
            {
                Shutdown();
            }
        }
    }
}