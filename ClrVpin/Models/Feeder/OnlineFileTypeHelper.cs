using System;
using System.Collections.Generic;
using System.Linq;
using Utils.Extensions;

namespace ClrVpin.Models.Feeder;

public static class OnlineFileTypeHelper
{
    public static OnlineFileTypeEnum GetEnum(string stringType)
    {
        _typeDictionary ??= Enum.GetValues<OnlineFileTypeEnum>().ToDictionary(value => value.GetDescription(), value => value);
        
        return _typeDictionary[stringType];
    }

    private static Dictionary<string, OnlineFileTypeEnum> _typeDictionary;
}