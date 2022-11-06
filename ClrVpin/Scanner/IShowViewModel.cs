using System;
using System.Windows;

namespace ClrVpin.Scanner;

public interface IShowViewModel
{
    Window Show(Window parent);
    
    public Action<bool> ProgressChanged { get; set; }
}