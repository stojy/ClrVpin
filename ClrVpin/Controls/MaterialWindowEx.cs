using System.Windows;
using MaterialDesignExtensions.Controls;
using PropertyChanged;

namespace ClrVpin.Controls;

[AddINotifyPropertyChangedInterface]
public class MaterialWindowEx : MaterialWindow
{
    // workarounds for MaterialDesignExtensions layout issue supporting SizeToContent.WidthAndHeight
    // - refer https://github.com/spiegelp/MaterialDesignExtensions/issues/144
    public MaterialWindowEx()
    {
        // removes unnecessary pixels in window: header, right, and bottom
        UseLayoutRounding = true;
            
        ContentRendered += (_, _) =>
        {
            if (SizeToContent != SizeToContent.Manual)
            {
                // force a SizeToContent change so that WPF/MDE can correctly layout the window to fit the content
                var sizeToContent = SizeToContent;
                SizeToContent = SizeToContent.Manual;
                SizeToContent = sizeToContent;
            }
        };
    }
}