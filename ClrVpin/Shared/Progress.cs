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
    internal class Progress
    {
        public Progress()
        {
            _timer = new Timer(_ => Duration = _durationStopwatch.Elapsed);
            CancelCommand = new ActionCommand(Cancel);
        }

        public bool IsCancelled { get; set; }

        public TimeSpan Duration { get; set; }
        public string Status { get; set; }
        public int Percentage { get; set; }

        public ICommand CancelCommand { get; set; }

        public event Action Cancelled;

        public void Cancel()
        {
            IsCancelled = true;
            Cancelled?.Invoke();
            Close();
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
            _timer.Change(1000, 1000);
        }

        public void Close()
        {
            _window.Close();
            _durationStopwatch.Stop();

            Duration = _durationStopwatch.Elapsed;
            _timer.Change(0, 0);
        }

        public void Update(string status, int percentageComplete)
        {
            Status = status;
            Percentage = percentageComplete;
        }

        private readonly Timer _timer;

        private Window _window;
        private Stopwatch _durationStopwatch;
    }
}