using System;
using System.Collections.Generic;
using System.Linq;
using Utils.Extensions;

namespace ClrVpin.Models.Feeder;

public static class OnlineFileType
{
    public static OnlineFileTypeEnum GetEnum(string onlineFileType)
    {
        _onlineFileTypeDictionary ??= Enum.GetValues<OnlineFileTypeEnum>().ToDictionary(value => value.GetDescription(), value => value);
        
        return _onlineFileTypeDictionary[onlineFileType];
    }

    private static Dictionary<string, OnlineFileTypeEnum> _onlineFileTypeDictionary;
}