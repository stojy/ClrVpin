using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using PropertyChanged;
using Utils;

namespace ClrVpin.Shared
{
    [AddINotifyPropertyChangedInterface]
    internal class ProgressViewModel
    {
        public ProgressViewModel()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;
            
            CancelCommand = new ActionCommand(Cancel);
        }

        public CancellationToken CancellationToken { get; set; }

        public bool IsCancelled { get; set; }

        public TimeSpan Duration => _durationStopwatch.Elapsed;
        public TimeSpan DisplayDuration { get; set; }
        public string Status { get; set; }
        public string Detail { get; set; }
        public int Percentage { get; set; }

        public ICommand CancelCommand { get; set; }

        public event Action Cancelled;

        public void Cancel()
        {
            IsCancelled = true;
            
            Cancelled?.Invoke();
            _cancellationTokenSource.Cancel();

            Environment.Exit(-1);
        }

        public void Show(Window parentWindow)
        {
            IsCancelled = false;

            _window = new Window
            {
                Owner = parentWindow,
                Title = "Progress",
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ProgressTemplate") as DataTemplate
            };
            _window.Show();

            _durationStopwatch = Stopwatch.StartNew();
            _timer = new Timer(_ => DisplayDuration = _durationStopwatch.Elapsed);

            _timer.Change(1000, 1000);
        }

        public void Close()
        {
            _window.Close();
            _durationStopwatch.Stop();
            _timer.Change(0, 0);
        }

        public void Update(string status, int? percentageComplete = null, string detail = null)
        {
            if (status != null)
                Status = status;

            if (percentageComplete != null)
                Percentage = percentageComplete.Value;

            Detail = detail;
        }

        private Timer _timer;

        private Window _window;
        private Stopwatch _durationStopwatch;
        private readonly CancellationTokenSource _cancellationTokenSource;
    }
}