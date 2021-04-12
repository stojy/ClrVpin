using System;
using System.Threading.Tasks;
using System.Windows;

namespace ClrVpin
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SetupExceptionHandling();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                HandleError((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                HandleError(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = false;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                HandleError(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void HandleError(Exception exception, string source)
        {
            var message = $"Unhandled exception ({source})";
            try
            {
                var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = $"Unhandled exception in {assemblyName}\n\n{exception.Message}\n\n{exception}";

                MessageBox.Show(MainWindow!, message, "An Error Has Occurred", MessageBoxButton.OK, MessageBoxImage.Error);
                
                //LogicalTreeHelper()

            }
            catch (Exception ex)
            {
                //_logger.Error(ex, "Exception in HandleError");
            }
            finally
            {
                Shutdown();
                //_logger.Error(exception, message);
            }
        }
    }
}
