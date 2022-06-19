using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;

namespace ClrVpin.Shared;

public class Notification
{
    public string Title { get; set; }
    public string Detail { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsWarning { get; set; }
    public bool IsError { get; set; }

    public static async Task ShowSuccess(string dialogHost, string detail) => await Show(dialogHost,  new Notification { Detail = detail, IsSuccess = true});
    public static async Task ShowWarning(string dialogHost, string detail) => await Show(dialogHost,  new Notification { Detail = detail, IsWarning = true});
    public static async Task ShowError(string dialogHost, string detail) => await Show(dialogHost,  new Notification { Detail = detail, IsError = true});

    private static async Task Show(string dialogHost, Notification notification)
    {
        await DialogHost.Show(notification, dialogHost);
    }
}