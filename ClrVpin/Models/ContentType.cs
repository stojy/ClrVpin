﻿using System.Collections.Generic;
using System.Linq;
using PropertyChanged;

namespace ClrVpin.Models
{
    [AddINotifyPropertyChangedInterface]
    public class ContentType
    {
        public string Description { get; set; }
        public string Folder { get; set; }   
        public string Extensions { get; set; }
        public bool IsDatabase { get; set; }
        public string Tip { get; set; }

        public IEnumerable<string> ExtensionsList => Extensions.Split(",").Select(x => x.Trim());
    }
}