using System;
using System.Threading.Tasks;
using System.Windows;
using NLog;

namespace ClrVpin
{
    public partial class App
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _logger.Info("App.xaml starting..");

            SetupExceptionHandling();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HandleError(s, (Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

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
                var assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                var message = "Unhandled exception detected\n\n"+
                              $"Message: {exception.Message}\n" +
                              $"Assembly: {assembly}\n" +
                              $"Sender: {sender}\n" + 
                              $"Source: {source}";

                _logger.Error(exception, message);
                MessageBox.Show(MainWindow!, $"{message}\n\n{exception}", "An Error Has Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception in HandleError");
            }
            finally
            {
                Shutdown();
            }
        }
    }
}
