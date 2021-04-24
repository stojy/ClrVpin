﻿using System.Windows;
using System.Windows.Input;
using PropertyChanged;
using Utils;

namespace ClrVpin.Rebuilder
{
    [AddINotifyPropertyChangedInterface]
    public class Rebuilder
    {
        public Rebuilder()
        {
            StartCommand = new ActionCommand(Start);
        }

        private void Start()
        {
        }

        public ICommand StartCommand { get; set; }

        public void Show(Window parent)
        {
            var window = new Window
            {
                Owner = parent,
                Content = this,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ContentTemplate = parent.FindResource("RebuilderTemplate") as DataTemplate
            };

            window.Show();
            parent.Hide();
            window.Closed += (_, _) => parent.Show();
        }
    }
}