﻿using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using ClrVpin.Controls;
using Microsoft.Xaml.Behaviors.Core;

namespace ClrVpin.Logging
{
    public class LoggingViewModel
    {
        public LoggingViewModel()
        {
            LogsView = new ListCollectionView(Logger.Logs);
            File = Logger.File;

            NavigateToFileCommand = new ActionCommand(NavigateToFile);
        }

        public ListCollectionView LogsView { get; }
        public string File { get; }
        public ICommand NavigateToFileCommand { get; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Logs",
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - WindowMargin,
                Height = Model.ScreenWorkArea.Height - top - WindowMargin,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("LoggingTemplate") as DataTemplate
            };
            Window.Show();
        }

        public void Close() => Window.Close();

        private void NavigateToFile() => Process.Start(new ProcessStartInfo(File) { UseShellExecute = true });

        public Window Window { get; private set; }
        private const int WindowMargin = 0;
    }
}