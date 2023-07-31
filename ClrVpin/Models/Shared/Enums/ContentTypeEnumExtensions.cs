using ClrVpin.Models.Settings;
using Utils.Extensions;

namespace ClrVpin.Models.Shared.Enums;

public static class ContentTypeEnumExtensions
{
    public static bool IsImportant(this ContentTypeEnum contentTypeEnum)
    {
        return contentTypeEnum.In(StaticSettings.ImportantContentTypes);
    }
}