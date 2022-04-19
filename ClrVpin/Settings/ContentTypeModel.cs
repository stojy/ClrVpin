using System;
using System.Windows;
using System.Windows.Controls;
using ClrVpin.Controls.FolderSelection;
using ClrVpin.Models;
using PropertyChanged;
using Utils;

namespace ClrVpin.Settings
{
    [AddINotifyPropertyChangedInterface]
    public class ContentTypeModel : FolderTypeDetail
    {
        public ContentTypeModel(ContentType contentType, Action updatedAction = null)
        {
            ContentType = contentType;
            Folder = contentType.Folder;
            Description = contentType.Description;

            // add a validation pattern (checked elsewhere to ensure the base folder matches the description, i.e. to avoid any unexpected folders being specified (e.g. c:\)
            // - refer FilePatternValidation
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

            KindredExtensionsChangedCommandWithParam = new ActionCommand<TextChangedEventArgs>(e =>
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

        private static string GetText(RoutedEventArgs e) =>
            // workaround (aka hack) to cater for when a ValidationRule fires..
            // - UI is updated nicely with a warning (e.g. folder must be specified), but alas the underlying binding source is not updated.. i.e. user's interaction is ignored :(
            // - e.g. using clear button on Material styled TextBox fires the validation
            // - sequence..
            //   a. ValidationRule fires (refer xaml) - which potentially rejecting the change
            //   b. TextChanged bubbled event caught here - fortunately, this occurs irrespective of whether the ValidationRule result
            e.Source is TextBox textBox ? textBox.Text : null;
    }
}