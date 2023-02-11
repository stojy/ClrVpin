using System.Collections.Generic;
using Utils.Extensions;

namespace ClrVpin.Models.Shared;

public static class ContentTypeEnumExtensions
{
    public static readonly IEnumerable<ContentTypeEnum> ImportantContentTypes = new[]
    {
        ContentTypeEnum.Tables, ContentTypeEnum.Backglasses, ContentTypeEnum.WheelImages, ContentTypeEnum.TableVideos, ContentTypeEnum.BackglassVideos
    };

    public static bool IsImportant(this ContentTypeEnum contentTypeEnum)
    {
        return contentTypeEnum.In(ImportantContentTypes);
    }
}