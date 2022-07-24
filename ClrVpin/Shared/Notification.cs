﻿using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;

namespace ClrVpin.Shared;

public class Notification
{
    public string Title { get; set; }
    public string Detail { get; set; }
    public bool DetailIsMonospaced { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsWarning { get; set; }
    public bool IsError { get; set; }
    public bool isConfirmation { get; set; }

    public static async Task ShowSuccess(string dialogHost, string title = null, string detail = null, bool detailIsMonospaced = true)
        => await Show(dialogHost, new Notification { Title = title, Detail = detail, IsSuccess = true, DetailIsMonospaced = detailIsMonospaced });

    public static async Task ShowWarning(string dialogHost, string title = null, string detail = null, bool detailIsMonospaced = true)
        => await Show(dialogHost, new Notification { Title = title, Detail = detail, IsWarning = true, DetailIsMonospaced = detailIsMonospaced });

    public static async Task ShowError(string dialogHost, string title = null, string detail = null, bool detailIsMonospaced = true)
        => await Show(dialogHost, new Notification { Title = title, Detail = detail, IsError = true, DetailIsMonospaced = detailIsMonospaced });

    public static async Task<bool?> ShowConfirmation(string dialogHost, string title = null, string detail = null, bool detailIsMonospaced = true)
    {
        var result = await Show(dialogHost, new Notification { Title = title, Detail = detail, isConfirmation = true, DetailIsMonospaced = detailIsMonospaced });

        return result as bool?;
    }

    private static async Task<object> Show(string dialogHost, Notification notification)
    {
        return await DialogHost.Show(notification, dialogHost);
    }
}