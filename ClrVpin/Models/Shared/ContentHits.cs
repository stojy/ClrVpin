﻿using PropertyChanged;
using System.Collections.ObjectModel;
using System.Linq;
using ClrVpin.Models.Shared.Enums;

namespace ClrVpin.Models.Shared;

[AddINotifyPropertyChangedInterface]
public class ContentHits
{
    public ContentHits(ContentType contentType)
    {
        ContentType = contentType;
    }

    public ContentType ContentType { get; }
    public ContentTypeEnum Enum => ContentType.Enum;
    public ObservableCollection<Hit> Hits { get; } = new();

    public bool IsSmelly { get; private set; }
    public bool IsMissing { get; private set; }

    public void Add(HitTypeEnum hitType, string path, int? score = null)
    {
        // for missing content.. the path is the description, i.e. desirable file name without an extension
        if (hitType == HitTypeEnum.Missing)
        {
            // display format: <file>.<ext1> (or .<ext2>, .<ext3>)
            var extensions = ContentType.Extensions.Split(",").Select(x => x.Trim().TrimStart('*')).ToList();
            path = @$"{ContentType.Folder}\{path}{extensions.First()}";

            var otherExtensions = extensions.Skip(1).ToList();
            if (otherExtensions.Any())
                path += $" (or {string.Join(", ", otherExtensions)})";
        }

        // always add hit type.. irrespective of whether it's valid or configured
        Hits.Add(new Hit(ContentType.Enum, path, hitType, score));

        IsSmelly = Hits.Any(hit => hit.Type != HitTypeEnum.CorrectName);
        IsMissing = Hits.Any(hit => hit.Type == HitTypeEnum.Missing);
    }

    public override string ToString() => $"ContentType: {ContentType}, Hits: {Hits.Count}, IsSmelly: {IsSmelly}";
}