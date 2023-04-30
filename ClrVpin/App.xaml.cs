using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Xml;
using ClrVpin.Controls;
using ClrVpin.Home;
using ClrVpin.Logging;
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

            base.OnStartup(e);

            Logger.Info($"App started: settings={JsonSerializer.Serialize(SettingsManager.Create().Settings)}");

            SetupExceptionHandling();

            ShowHomeWindow();
        }

        private static void ShowHomeWindow()
        {
            // alternatively this can be done in app.xaml StartupUri="HomeWindow".. but done here in code to remove some of the 'magic' and give us a little more control
            var window = new HomeWindow();
            window.ShowDialog();
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
                                    "- screenshot (if applicable)";
            
            
            var detail = $"Message:       {exception.Message}\n" +
                         $"Type:          {exception.GetType()}\n" +
                         $"Inner Message: {exception.InnerException?.Message}\n" +
                         $"Inner Type:    {exception.InnerException?.GetType()}\n" +
                         $"Assembly:      {assembly}\n" +
                         $"Sender:        {sender}\n" +
                         $"Source:        {source}\n" +
                         $"Stack:\n{exception.StackTrace}\n" +
                         $"Inner Stack:\n{exception.InnerException?.StackTrace}\n";
            detail += $"\n{Logger.GetSystemInfo()}";
            
            detail += $"\n{Logger.GetLogs()}";

            try
            {
                Logger.Error(exception, $"{title}\n{detail}");

                if (Current.MainWindow is MaterialWindowEx window)
                    window.TryShow();

                Notification.ShowError("HomeDialog", title, subTitle, detail, true, true).ContinueWith(_ => SubmitBugAndExit(exception.Message, detail));
            }
            catch (Exception ex)
            {
                // if the material window fails (e.g. HomeWindow doesn't have a DialogHost available yet) then default back to the trusty windows message box
                MessageBox.Show(Current.MainWindow!, $"{title}\n\n{subTitle}\n\n{exception}", "An Error Has Occurred.  ClrVpin will be shutdown.", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error(ex, "Exception in HandleError");
                Environment.Exit(-2);
            }
            //finally
            //{
            //   Environment.Exit(-1);
            //}
        }

        private static void SubmitBugAndExit(string heading, string detail)
        {
            var title = $"Unhandled Error - {heading}";
            var body = @"**Describe the bug**
[A description of the bug]

**To Reproduce**
[Steps to reproduce the bug]

**Expected behavior**
[If applicable, what did you expect to happen]

**Screenshots**
[If applicable, add screenshots]

**Logs**
[If applicable, add the log file (or relevant snippet): c:\ProgramData\ClrVpin\logs\ClrVpin.log]

**Unhandled Error Details**
";

            // markdown/github workaround: using the tilde for code span doesn't respect newlines
            // - pre(formatted) tag presents exactly as written preserving all whitespace (including new lines)
            //   https://developer.mozilla.org/en-US/docs/Web/HTML/Element/pre
            // - on github this is rendered similar to a code block, but within a div that supports horizontal scrolling to make things pretty
            //   refer https://stackoverflow.com/questions/32550310/html-display-new-line-in-code-tag
            //   https://github.github.com/gfm/#code-spans
            body += $"<pre>{detail}</pre>";                  

            // more markdown/github workarounds
            body = body
                .Replace("`", @"\`")                     // escape tilde to avoid being interpreting as code, e.g. used by .net in it's stack trace
                .Replace("\r\n", "<br />")               // change newlines within stacktrace to line breaks
                .Replace("\n", "<br />");                // change other newlines to line breaks    
            
            // escape special URL chars, e.g. %, &, etc
            body = System.Net.WebUtility.UrlEncode(body);

            // truncate submission to fit within github URK max 8k size, refer https://github.com/cli/cli/issues/1575
            body = body[..Math.Min(body.Length, 8_000)]; 

            Process.Start(new ProcessStartInfo($@"https://github.com/stojy/ClrVpin/issues/new?&template=bug_report.md&title={title}&body={body}") { UseShellExecute = true });
             
            Environment.Exit(-1);
        }
    }
}