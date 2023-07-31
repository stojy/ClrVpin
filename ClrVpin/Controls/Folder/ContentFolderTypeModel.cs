using System;
using System.Windows;
using System.Windows.Controls;
using ClrVpin.Models.Shared;
using ClrVpin.Models.Shared.Enums;
using PropertyChanged;
using Utils;

namespace ClrVpin.Controls.Folder;

[AddINotifyPropertyChangedInterface]
public class ContentFolderTypeModel : FolderTypeDetail
{
    public ContentFolderTypeModel(ContentType contentType, Action updatedAction = null)
    {
        ContentType = contentType;
        Folder = contentType.Folder;
        Description = $"{contentType.Description} {(contentType.IsFolderRequired ? "¹" : "²")}";
        IsRequired = contentType.IsFolderRequired;

        // add a validation pattern (checked elsewhere to ensure the base folder matches the description, i.e. to avoid any unexpected folders being specified (e.g. c:\)
        // - refer FilePatternValidationRule
        if (contentType.Category == ContentTypeCategoryEnum.Media)
            PatternValidation = contentType.Description;

        Extensions = string.Join(", ", contentType.Extensions);
        KindredExtensions = string.Join(", ", contentType.KindredExtensions);

        FolderChangedCommandWithParam = new ActionCommand<TextChangedEventArgs>(e =>
        {
            // for display and storage
            contentType.Folder = Folder = GetText(e);

            updatedAction?.Invoke();
        });

        ExtensionsChangedCommandWithParam = new ActionCommand<TextChangedEventArgs>(e =>
        {
            // for display and storage
            contentType.Extensions = Extensions = GetText(e);

            updatedAction?.Invoke();
        });

        KindredExtensionsChangedCommandWithParam = new ActionCommand<TextChangedEventArgs>(_ =>
        {
            // for display and storage
            contentType.KindredExtensions = KindredExtensions;

            updatedAction?.Invoke();
        });

        FolderExplorerCommand = new ActionCommand(() => FolderUtil.Get(Description, Folder, folder =>
        {
            // for display
            Folder = folder;

            // for storage
            contentType.Folder = folder;

            updatedAction?.Invoke();
        }));
    }

    public ActionCommand<TextChangedEventArgs> KindredExtensionsChangedCommandWithParam { get; set; }

    public ActionCommand<TextChangedEventArgs> ExtensionsChangedCommandWithParam { get; set; }

    public ContentType ContentType { get; set; }

    private static string GetText(RoutedEventArgs e) => e.Source is TextBox textBox ? textBox.Text : null;
}