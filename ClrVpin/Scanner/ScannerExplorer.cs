﻿using System.Collections.ObjectModel;
using System.Windows;
using ClrVpin.Models;
using PropertyChanged;

namespace ClrVpin.Scanner
{
    [AddINotifyPropertyChangedInterface]
    public class ScannerExplorer
    {
        public ScannerExplorer(ObservableCollection<Game> games)
        {
            Games = games;
        }

        public void Show(Window parentWindow, double left, double top, double height)
        {
            _window = new Window
            {
                Owner = parentWindow,
                Title = "Scanner Explorer",
                Left = left,
                Top = top,
                MinHeight = 500,
                Height = height,
                MinWidth = 400,
                Content = this,
                ContentTemplate = parentWindow.Owner.FindResource("ScannerExplorerTemplate") as DataTemplate
            };
            _window.Show();
        }

        public void Close() => _window.Close();

        private Window _window;
        public ObservableCollection<Game> Games { get; }
    }
}