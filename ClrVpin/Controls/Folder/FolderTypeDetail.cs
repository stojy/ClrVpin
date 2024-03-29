﻿using System;
using System.Windows.Input;
using PropertyChanged;

namespace ClrVpin.Controls.Folder
{
    [AddINotifyPropertyChangedInterface]
    [Serializable]
    public class FolderTypeDetail
    {
        public string Folder { get; set; }
        public string Description { get; set; }
        public string Extensions { get; set; }
        public string KindredExtensions { get; set; }
        
        public string PatternValidation { get; set; }

        public bool IsRequired { get; set; }

        public ICommand FolderExplorerCommand { get; protected init; }
        public ICommand FolderChangedCommandWithParam { get; set; }
    }
}