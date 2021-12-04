﻿using System.Collections.ObjectModel;
using System.Windows;
using ClrVpin.Controls;
using ClrVpin.Models;
using PropertyChanged;

namespace ClrVpin.Importer
{
    [AddINotifyPropertyChangedInterface]
    public class ImporterResultsViewModel
    {
        public Window Window { get; set; }

        public void Show(Window parentWindow, double left, double top)
        {
            Window = new MaterialWindowEx
            {
                Owner = parentWindow,
                Title = "Results",
                Left = left,
                Top = top,
                Width = Model.ScreenWorkArea.Width - left - 5,
                Height = (Model.ScreenWorkArea.Height - 10) / 2,
                Content = this,
                Resources = parentWindow.Resources,
                ContentTemplate = parentWindow.FindResource("ImporterResultsTemplate") as DataTemplate
            };
            Window.Show();
        }

        public void Close()
        {
            Window.Close();
        }
    }
}