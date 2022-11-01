using System.Windows;
using ClrVpin.Controls;

namespace ClrVpin.Scanner;

public interface IShowViewModel
{
    Window Show(MaterialWindowEx parent);
}