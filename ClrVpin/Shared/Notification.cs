using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;

namespace ClrVpin.Shared;

public class Notification
{
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public string Detail { get; set; }
    public bool DetailIsMonospaced { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsWarning { get; set; }
    public bool IsError { get; set; }
    public bool IsConfirmation { get; set; }
    public string FalseLabel { get; set; }
    public string TrueLabel { get; set; }
    public bool ShowCloseButton { get; set; }

    public static async Task ShowSuccess(string dialogHost, string title = null, string subTitle = null, string detail = null, bool detailIsMonospaced = true, bool showCloseButton = false)
        => await Show(dialogHost, new Notification { Title = title, SubTitle = subTitle, Detail = detail, IsSuccess = true, DetailIsMonospaced = detailIsMonospaced, ShowCloseButton = showCloseButton});

    public static async Task ShowWarning(string dialogHost, string title = null, string subTitle = null, string detail = null, bool detailIsMonospaced = true, bool showCloseButton = false)
        => await Show(dialogHost, new Notification { Title = title, SubTitle = subTitle, Detail = detail, IsWarning = true, DetailIsMonospaced = detailIsMonospaced, ShowCloseButton = showCloseButton });

    public static async Task ShowError(string dialogHost, string title = null, string subTitle = null, string detail = null, bool detailIsMonospaced = true, bool showCloseButton = false)
        => await Show(dialogHost, new Notification { Title = title, SubTitle = subTitle, Detail = detail, IsError = true, DetailIsMonospaced = detailIsMonospaced, ShowCloseButton = showCloseButton });

    public static async Task<bool?> ShowConfirmation(string dialogHost, string title = null, string subTitle = null, string detail = null, bool detailIsMonospaced = true, string trueLabel = "Yes", string falseLabel = "No")
    {
        var result = await Show(dialogHost, new Notification
        {
            Title = title,
            SubTitle = subTitle,
            Detail = detail,
            IsConfirmation = true,
            DetailIsMonospaced = detailIsMonospaced,
            TrueLabel = trueLabel,
            FalseLabel = falseLabel
        });

        return result as bool?;
    }

    private static async Task<object> Show(string dialogHost, Notification notification) => await DialogHost.Show(notification, dialogHost);
}