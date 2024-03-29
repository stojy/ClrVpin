﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PropertyChanged;
using Utils;

namespace ClrVpin.Shared;

[AddINotifyPropertyChangedInterface]
internal class ProgressViewModel
{
    public ProgressViewModel(string title = null)
    {
        Title = title;

        _cancellationTokenSource = new CancellationTokenSource();

        CancelCommand = new ActionCommand(Cancel);
    }

    public string Title { get; set; }

    public TimeSpan Duration => _durationStopwatch.Elapsed;
    public double DisplayDurationInSeconds { get; private set; }
    public string Status { get; private set; }
    public string Detail { get; private set; }
    public int Percentage { get; set; }

    public ICommand CancelCommand { get; }

    // ReSharper disable once EventNeverSubscribedTo.Global
    public event Action Cancelled;

    private void Cancel()
    {
        Cancelled?.Invoke();
        _cancellationTokenSource.Cancel();

        Environment.Exit(-1);
    }

    public void Show(Window parentWindow)
    {
        _window = new Window
        {
            Owner = parentWindow,
            Title = "Progress",
            WindowStyle = WindowStyle.None,
            AllowsTransparency=true,
            Background = Brushes.Transparent,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = SizeToContent.WidthAndHeight,
            Content = this,
            Resources = parentWindow.Resources,
            ContentTemplate = parentWindow.FindResource("ProgressTemplate") as DataTemplate
        };
        _window.Show();

        _durationStopwatch = Stopwatch.StartNew();
        _timer = new Timer(_ => DisplayDurationInSeconds = _durationStopwatch.Elapsed.TotalSeconds);

        _timer.Change(1000, 1000);
    }

    public void Close()
    {
        _window.Close();
        _durationStopwatch.Stop();
        _timer.Change(0, 0);
    }

    public void Update(string status, float? ratioComplete = null, string detail = null, int? current = null, int? total = null)
    {
        if (status != null)
            Status = status;

        Current = current;
        Total = total;

        // either use the ratio if provided, else calculate it if the current/total are provided
        if (ratioComplete != null)
            Percentage = (int)(100 * ratioComplete.Value);
        else if (current != null && total != null)
            Percentage = (int)(100 * ((float)current / total));

        Detail = detail;
    }

    public int? Current { get; set; }
    public int? Total { get; set; }

    private readonly CancellationTokenSource _cancellationTokenSource;

    private Timer _timer;

    private Window _window;
    private Stopwatch _durationStopwatch;
}